using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using TgSharp.TL;
using TgSharp.TL.Account;
using TgSharp.TL.Auth;
using TgSharp.TL.Contacts;
using TgSharp.TL.Help;
using TgSharp.TL.Messages;
using TgSharp.TL.Upload;
using TgSharp.Core.Auth;
using TgSharp.Core.Exceptions;
using TgSharp.Core.MTProto.Crypto;
using TgSharp.Core.Network;
using TgSharp.Core.Network.Exceptions;
using TgSharp.Core.Utils;
using TLAuthorization = TgSharp.TL.Auth.TLAuthorization;

namespace TgSharp.Core
{
    public class TelegramClient : IDisposable
    {
        private MtProtoSender sender;
        private WssTransport transport;
        private readonly string apiHash;
        private readonly int apiId;
        private readonly string sessionUserId;
        private readonly ISessionStore store;
        private List<TLDcOption> dcOptions;
        private readonly DataCenterIPVersion dcIpVersion;

        public Session Session { get; private set; }

        /// <summary>
        /// Creates a new TelegramClient
        /// </summary>
        /// <param name="apiId">The API ID provided by Telegram. Get one at https://my.telegram.org </param>
        /// <param name="apiHash">The API Hash provided by Telegram. Get one at https://my.telegram.org </param>
        /// <param name="store">An ISessionStore object that will handle the session</param>
        /// <param name="sessionUserId">The name of the session that tracks login info about this TelegramClient connection</param>
        /// <param name="handler">A delegate to invoke when a connection is needed and that will return a TcpClient that will be used to connect</param>
        /// <param name="dcIpVersion">Indicates the preferred IpAddress version to use to connect to a Telegram server</param>
        public TelegramClient(int apiId, string apiHash,
            DataCenterIPVersion dcIpVersion = DataCenterIPVersion.Default,
            ISessionStore store = null,
            string sessionUserId = "session"
            )
        {
            if (apiId == default(int))
                throw new MissingApiConfigurationException("API_ID");
            if (string.IsNullOrEmpty(apiHash))
                throw new MissingApiConfigurationException("API_HASH");

            if (store == null)
                store = new FileSessionStore();
            this.store = store;

            this.apiHash = apiHash;
            this.apiId = apiId;
            this.dcIpVersion = dcIpVersion;

            this.sessionUserId = sessionUserId;
        }

        public async Task ConnectAsync()
        {
            await ConnectInternalAsync(false);
        }

        private async Task ConnectInternalAsync(bool reconnect = false)
        {
            Session = SessionFactory.TryLoadOrCreateNew(store, sessionUserId);
            transport = new WssTransport(Session.DataCenter.Address, Session.DataCenter.Port, Session.DataCenter.DataCenterId);
            await transport.ConnectAsync();

            if (Session.AuthKey == null || reconnect)
            {
                var result = await new Authenticator(transport).DoAuthentication().ConfigureAwait(false);
                Session.AuthKey = result.AuthKey;
                Session.TimeOffset = result.TimeOffset;
            }

            sender = new MtProtoSender(transport, store, Session);

            //set-up layer
            var config = new TLRequestGetConfig();
            var request = new TLRequestInitConnection()
            {
                ApiId = apiId,
                AppVersion = "1.0.0",
                DeviceModel = "PC",
                LangCode = "en",
                Query = config,
                SystemVersion = "Win 10.0",
                SystemLangCode = "en",
                LangPack = ""
            };
            var invokewithLayer = new TLRequestInvokeWithLayer() { Layer = 121, Query = request };
            var response = await sender.Request(invokewithLayer).ConfigureAwait(false);

            dcOptions = ((TLConfig)response).DcOptions.ToList();
        }

        private async Task ReconnectToDcAsync(int dcId)
        {
            if (dcOptions == null || !dcOptions.Any())
                throw new InvalidOperationException($"Can't reconnect. Establish initial connection first.");

            TLExportedAuthorization exported = null;
            if (Session.TLUser != null)
            {
                TLRequestExportAuthorization exportAuthorization = new TLRequestExportAuthorization() { DcId = dcId };
                exported = await SendRequestAsync(exportAuthorization).ConfigureAwait(false);
            }

            IEnumerable<TLDcOption> dcs;
            if (dcIpVersion == DataCenterIPVersion.OnlyIPv6)
                dcs = dcOptions.Where(d => d.Id == dcId && d.Ipv6); // selects only ipv6 addresses 	
            else if (dcIpVersion == DataCenterIPVersion.OnlyIPv4)
                dcs = dcOptions.Where(d => d.Id == dcId && !d.Ipv6); // selects only ipv4 addresses
            else
                dcs = dcOptions.Where(d => d.Id == dcId); // any

            dcs = dcs.Where(d => !d.MediaOnly);

            TLDcOption dc;
            if (dcIpVersion != DataCenterIPVersion.Default)
            {
                if (!dcs.Any())
                    throw new Exception($"Telegram server didn't provide us with any IPAddress that matches your preferences. If you chose OnlyIPvX, try switch to PreferIPvX instead.");
                dcs = dcs.OrderBy(d => d.Ipv6);
                dc = dcIpVersion == DataCenterIPVersion.PreferIPv4 ? dcs.First() : dcs.Last(); // ipv4 addresses are at the beginning of the list because it was ordered
            }
            else
                dc = dcs.First();

            var dataCenter = new DataCenter(dcId, dc.IpAddress, dc.Port);
            Session.DataCenter = dataCenter;
            store.Save(Session);

            await ConnectInternalAsync(true).ConfigureAwait(false);

            if (Session.TLUser != null)
            {
                TLRequestImportAuthorization importAuthorization = new TLRequestImportAuthorization() { Id = exported.Id, Bytes = exported.Bytes };
                var imported = await SendRequestAsync(importAuthorization).ConfigureAwait(false);
                OnUserAuthenticated((TLUser)((TLAuthorization)imported).User);
            }
        }

        private async Task<T> RequestWithDcMigration<T>(TLMethod<T> request)
        {
            if (sender == null)
                throw new InvalidOperationException("Not connected!");

            var completed = false;
            while (!completed)
            {
                try
                {
                    return await sender.Request(request).ConfigureAwait(false);
                }
                catch (DataCenterMigrationException e)
                {
                    if (Session.DataCenter.DataCenterId == e.DC)
                    {
                        throw new Exception($"Telegram server replied requesting a migration to DataCenter {e.DC} when this connection was already using this DataCenter", e);
                    }

                    await ReconnectToDcAsync(e.DC).ConfigureAwait(false);
                }
            }

            return default(T);
        }

        public bool IsUserAuthorized()
        {
            return Session.TLUser != null;
        }

        public async Task<string> SendCodeRequestAsync(string phoneNumber)
        {
            if (String.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentNullException(nameof(phoneNumber));

            var request = new TLRequestSendCode() { PhoneNumber = phoneNumber, ApiId = apiId, ApiHash = apiHash, Settings = new TLCodeSettings { } };

            var response = await RequestWithDcMigration(request).ConfigureAwait(false);

            return response.PhoneCodeHash;
        }

        public async Task<TLUser> MakeAuthAsync(string phoneNumber, string phoneCodeHash, string code, string firstName = "", string lastName = "")
        {
            if (String.IsNullOrWhiteSpace(phoneNumber))
                throw new ArgumentNullException(nameof(phoneNumber));

            if (String.IsNullOrWhiteSpace(phoneCodeHash))
                throw new ArgumentNullException(nameof(phoneCodeHash));

            if (String.IsNullOrWhiteSpace(code))
                throw new ArgumentNullException(nameof(code));

            var request = new TLRequestSignIn() { PhoneNumber = phoneNumber, PhoneCodeHash = phoneCodeHash, PhoneCode = code };

            var response = await RequestWithDcMigration(request).ConfigureAwait(false);

            if (response is TLAuthorization)
            {
                OnUserAuthenticated((TLUser)((TLAuthorization)response).User);
                return (TLUser)((TLAuthorization)response).User;
            }
            else
            {
                var signUpRequest = new TLRequestSignUp() { PhoneNumber = phoneNumber, PhoneCodeHash = phoneCodeHash, FirstName = firstName, LastName = lastName };
                var signUpResponse = await RequestWithDcMigration(signUpRequest).ConfigureAwait(false);
                OnUserAuthenticated((TLUser)((TLAuthorization)signUpResponse).User);
                return (TLUser)((TLAuthorization)signUpResponse).User;
            }
        }

        public async Task<T> SendRequestAsync<T>(TLMethod<T> methodToExecute)
        {
            return await RequestWithDcMigration(methodToExecute).ConfigureAwait(false);
        }

        internal async Task<T> SendAuthenticatedRequestAsync<T>(TLMethod<T> methodToExecute)
        {
            if (!IsUserAuthorized())
                throw new InvalidOperationException("Authorize user first!");

            return await SendRequestAsync<T>(methodToExecute)
                .ConfigureAwait(false);
        }

        public async Task<TLUser> UpdateUsernameAsync(string username)
        {
            var req = new TLRequestUpdateUsername { Username = username };

            return (TLUser)await SendAuthenticatedRequestAsync(req)
                .ConfigureAwait(false);
        }

        public async Task<bool> CheckUsernameAsync(string username)
        {
            var req = new TLRequestCheckUsername { Username = username };

            return await SendAuthenticatedRequestAsync<bool>(req)
                .ConfigureAwait(false);
        }

        public async Task<TLImportedContacts> ImportContactsAsync(IReadOnlyList<TLInputPhoneContact> contacts)
        {
            var req = new TLRequestImportContacts { Contacts = new TLVector<TLInputPhoneContact>(contacts) };

            return await SendAuthenticatedRequestAsync<TLImportedContacts>(req)
                .ConfigureAwait(false);
        }

        public async Task<TLAbsUpdates> DeleteContactsAsync(IReadOnlyList<TLAbsInputUser> users)
        {
            var req = new TLRequestDeleteContacts { Id = new TLVector<TLAbsInputUser>(users) };

            var response = await SendAuthenticatedRequestAsync(req)
                .ConfigureAwait(false);

            return response;
        }

        public async Task<TLContacts> GetContactsAsync()
        {
            var req = new TLRequestGetContacts() { Hash = 0 };

            return (TLContacts)await SendAuthenticatedRequestAsync(req)
                .ConfigureAwait(false);
        }

        public async Task<TLAbsUpdates> SendMessageAsync(TLAbsInputPeer peer, string message)
        {
            return await SendAuthenticatedRequestAsync<TLAbsUpdates>(
                    new TLRequestSendMessage()
                    {
                        Peer = peer,
                        Message = message,
                        RandomId = Helpers.GenerateRandomLong()
                    })
                .ConfigureAwait(false);
        }

        public async Task<Boolean> SendTypingAsync(TLAbsInputPeer peer)
        {
            var req = new TLRequestSetTyping()
            {
                Action = new TLSendMessageTypingAction(),
                Peer = peer
            };
            return await SendAuthenticatedRequestAsync<Boolean>(req)
                .ConfigureAwait(false);
        }

        public async Task<TLAbsDialogs> GetUserDialogsAsync(int offsetDate = 0, int offsetId = 0, TLAbsInputPeer offsetPeer = null, int limit = 100)
        {
            if (offsetPeer == null)
                offsetPeer = new TLInputPeerSelf();

            var req = new TLRequestGetDialogs()
            {
                OffsetDate = offsetDate,
                OffsetId = offsetId,
                OffsetPeer = offsetPeer,
                Limit = limit
            };
            return await SendAuthenticatedRequestAsync<TLAbsDialogs>(req)
                .ConfigureAwait(false);
        }

        public async Task<TLAbsUpdates> SendUploadedPhoto(TLAbsInputPeer peer, TLAbsInputFile file, string message)
        {
            if (String.IsNullOrEmpty(message))
            {
                throw new ArgumentNullException(nameof(message));
            }

            return await SendAuthenticatedRequestAsync(new TLRequestSendMedia()
            {
                RandomId = Helpers.GenerateRandomLong(),
                Background = false,
                ClearDraft = false,
                Media = new TLInputMediaUploadedPhoto() { File = file },
                Peer = peer
            })
                .ConfigureAwait(false);
        }

        public async Task<TLAbsUpdates> SendUploadedDocument(
            TLAbsInputPeer peer, TLAbsInputFile file, string mimeType, TLVector<TLAbsDocumentAttribute> attributes)
        {
            return await SendAuthenticatedRequestAsync<TLAbsUpdates>(new TLRequestSendMedia()
            {
                RandomId = Helpers.GenerateRandomLong(),
                Background = false,
                ClearDraft = false,
                Media = new TLInputMediaUploadedDocument()
                {
                    File = file,
                    MimeType = mimeType,
                    Attributes = attributes
                },
                Peer = peer
            })
                .ConfigureAwait(false);
        }

        public async Task<TLFile> GetFile(TLAbsInputFileLocation location, int filePartSize, int offset = 0)
        {
            TLFile result = (TLFile)await SendAuthenticatedRequestAsync(new TLRequestGetFile
            {
                Location = location,
                Limit = filePartSize,
                Offset = offset
            })
                .ConfigureAwait(false);
            return result;
        }

        public async Task SendPingAsync()
        {
            await sender.SendPingAsync()
                .ConfigureAwait(false);
        }

        public async Task<TLAbsMessages> GetHistoryAsync(TLAbsInputPeer peer, int offsetId = 0, int offsetDate = 0, int addOffset = 0, int limit = 100, int maxId = 0, int minId = 0)
        {
            var req = new TLRequestGetHistory()
            {
                Peer = peer,
                OffsetId = offsetId,
                OffsetDate = offsetDate,
                AddOffset = addOffset,
                Limit = limit,
                MaxId = maxId,
                MinId = minId
            };
            return await SendAuthenticatedRequestAsync<TLAbsMessages>(req)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// Serch user or chat. API: contacts.search#11f812d8 q:string limit:int = contacts.Found;
        /// </summary>
        /// <param name="q">User or chat name</param>
        /// <param name="limit">Max result count</param>
        /// <returns></returns>
        public async Task<TLFound> SearchUserAsync(string q, int limit = 10)
        {
            var r = new TL.Contacts.TLRequestSearch
            {
                Q = q,
                Limit = limit
            };

            return await SendAuthenticatedRequestAsync<TLFound>(r)
                .ConfigureAwait(false);
        }

        private void OnUserAuthenticated(TLUser TLUser)
        {
            Session.TLUser = TLUser;
            Session.SessionExpires = int.MaxValue;

            this.store.Save(Session);
        }

        public bool IsConnected
        {
            get
            {
                if (transport == null)
                    return false;
                return transport.IsConnected();
            }
        }

        public void Dispose()
        {
            if (transport != null)
            {
                transport.Dispose();
                transport = null;
            }
        }
    }
}
