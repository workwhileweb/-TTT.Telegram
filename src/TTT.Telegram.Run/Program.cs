using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;
using Newtonsoft.Json;
using TgSharp.Core;
using TgSharp.TL;
using TgSharp.TL.Channels;
using TgSharp.TL.Messages;
using TgSharp.TL.Upload;
using TLChatFull = TgSharp.TL.Messages.TLChatFull;

namespace TTT.Telegram.Run
{
    internal class Program
    {
        public static FileSessionStore GetSessionStore(string phone)
        {
            var exeFile = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            var exeFolder = Path.GetDirectoryName(exeFile);
            if (exeFolder is null) return null;
            var dataFolder = Path.Combine(exeFolder, "data");
            var telegramFolder = Path.Combine(dataFolder, "telegram");
            var sessionFolder = Path.Combine(telegramFolder, phone);
            if (!Directory.Exists(sessionFolder)) Directory.CreateDirectory(sessionFolder);

            var sessionFolderNfo = new DirectoryInfo(sessionFolder);
            var sessionStore = new FileSessionStore(sessionFolderNfo);

            return sessionStore;
        }


        private static async Task<TelegramClient> GetClientAsync()
        {
            const string phone = "+84982250783";
            var sessionStore = GetSessionStore(phone);
            const string apiHash = "40acf77731548d3a11f2f4c25615ae24";
            var client = new TelegramClient(989501, apiHash,DataCenterIPVersion.OnlyIPv4,
                sessionStore, phone);
            await client.ConnectAsync();

            if (client.Session.TLUser != null) return client;

            var hash = await client.SendCodeRequestAsync(phone);
            Console.WriteLine("authen code?:");
            var code = Console.ReadLine();
            await client.MakeAuthAsync(phone, hash, code);

            return client;
        }
        // ReSharper disable once UnusedMember.Local
        private static async Task<List<TLMessage>> GetMessages(TelegramClient client)
        {
            if (await client.GetUserDialogsAsync() is not TLDialogs dialogs) return null;

            var result = new List<TLMessage>();

            foreach (var dialog in dialogs.Dialogs.Cast<TLDialog>().Where(dlg => dlg.Peer is TLPeerChannel && dlg.UnreadCount > 0))
            {
                if (dialog.Peer is not TLPeerChannel peer) continue;
                var chat = dialogs.Chats.OfType<TLChannel>().First(x => x.Id == peer.ChannelId);
                if (chat.AccessHash is null) continue;
                var target = new TLInputPeerChannel { ChannelId = chat.Id, AccessHash = (long)chat.AccessHash };
                var hist = await client.GetHistoryAsync(target, 0, -1, dialog.UnreadCount);
                var messages = (hist as TLChannelMessages)?.Messages;
                if (messages == null) continue;
                result.AddRange(messages.Select(m => m as TLMessage));
            }

            return result;
        }

        /// <summary>
        ///     https://github.com/sochix/TLSharp/pull/926/commits/92ca5a9ac112db86f193dbee1107404fd6126263
        ///     https://github.com/sochix/TLSharp/issues/934
        ///     https://github.com/sochix/TLSharp/pull/926
        ///     https://github.com/sochix/TLSharp/issues/461
        ///     https://stackoverflow.com/questions/61313792/tlsharp-fetch-group-list-issue
        /// </summary>
        /// <param name="client"></param>
        /// <param name="groupName"></param>
        /// <returns></returns>
        // ReSharper disable once UnusedMember.Local
        private static async Task<List<TLUser>> GetChatInfo(TelegramClient client, string groupName)
        {
            var dialogs = (TLDialogs)await client.GetUserDialogsAsync();
            var main = dialogs.Chats.OfType<TLChannel>()
                .FirstOrDefault(c => c.Title == groupName);

            if (main?.AccessHash is null) return null;

            var req = new TLRequestGetFullChannel
            {
                Channel = new TLInputChannel
                {
                    AccessHash = main.AccessHash.Value,
                    ChannelId = main.Id
                }
            };

            var res = await client.SendRequestAsync<TLChatFull>(req);

            var result = new List<TLUser>();

            //we have to do this in slices
            var offset = 0;
            while (offset < (res.FullChat as TLChannelFull)?.ParticipantsCount)
            {
                var pReq = new TLRequestGetParticipants
                {
                    Channel = new TLInputChannel { AccessHash = main.AccessHash.Value, ChannelId = main.Id },
                    Filter = new TLChannelParticipantsRecent(),
                    Limit = 200,
                    Offset = offset
                };
                var pRes = await client.SendRequestAsync<TLChannelParticipants>(pReq);
                result.AddRange(pRes.Users.Cast<TLUser>());
                offset += 200;
                await Task.Delay(500);
            }

            return result;
        }


        // ReSharper disable once UnusedMember.Local
        private static async Task TalkUser(TelegramClient client, int userId, string message)
        {
            await client.SendMessageAsync(new TLInputPeerUser { UserId = userId }, message);
        }

        // ReSharper disable once UnusedMember.Local
        private static async Task<TLAbsUpdates> TalkGroup(TelegramClient client, string titleContains, string message)
        {
            var dialogs = (TLDialogsSlice)await client.GetUserDialogsAsync();

            var chat = dialogs.Chats
                .OfType<TLChat>()
                .FirstOrDefault(c => c.Title.Contains(titleContains));
            if (chat is null) return null;

            return await client.SendMessageAsync(new TLInputPeerChat { ChatId = chat.Id }, message);
        }

        // ReSharper disable once UnusedMember.Local
        private static async Task<TLUser> FindUserAsync(TelegramClient client, string phone)
        {
            var contacts = await client.GetContactsAsync();
            return contacts.Users.OfType<TLUser>()
                .FirstOrDefault(x => x.Phone != null && x.Phone.Contains(phone));
        }

        private static async Task PingForeverAsync(TelegramClient client, TimeSpan delay, CancellationToken cancel)
        {
            while (!cancel.IsCancellationRequested)
            {
                await client.SendPingAsync(cancel);
                await Task.Delay(delay, cancel);
            }
        }

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var client = await GetClientAsync();

            var actEmails = new ActionModule("/telegram/talk/group/", HttpVerbs.Post, async ctx =>
            {
                var json = await ctx.GetRequestBodyAsStringAsync();
                var document = JsonConvert.DeserializeObject<TalkGroupConfig>(json);
                if (document is null)
                {
                    await ctx.SendDataAsync(new { Message = "ERROR" });
                    return;
                }

                var result = await TalkGroup(client, document.TitleContains, document.Message);
                await ctx.SendDataAsync(new { Message = "OK", result });
            });

            using var web = new WebServer(o => o
                    .WithUrlPrefix("http://localhost:10001/")
                    .WithMode(HttpListenerMode.EmbedIO))
                .WithLocalSessionManager()
                .WithModule(actEmails);

            var cancel = new CancellationTokenSource();
            var pingForever = PingForeverAsync(client, TimeSpan.FromSeconds(10), cancel.Token);
            await web.RunAsync(cancel.Token);
            cancel.Cancel();
            pingForever.Dispose();
            client.Dispose();
        }
    }
}