using System;
using System.Threading;
using System.Threading.Tasks;
using TgSharp.Core.Network;

namespace TgSharp.Core.Auth
{
    public class Authenticator
    {
        private readonly MtProtoPlainSender sender;
        private TaskCompletionSource<Step3_Response> completionSource;
        public Authenticator(WssTransport transport)
        {
            sender = new(transport);
            completionSource = new TaskCompletionSource<Step3_Response>();
        }

        public Task<Step3_Response> DoAuthentication()
        {
            //TODO: event hell?

            var step1 = new Step1_PQRequest();

            //race condition ? move randomization in consturctor

            sender.OnResponseReceived = (response) => Sender_OnStep1ResponseReceived(step1, response); 
            sender.Send(step1.ToBytes());


            return completionSource.Task;
        }

        private void Sender_OnStep1ResponseReceived(Step1_PQRequest step1, byte[] response)
        {
            var step1Response = step1.FromBytes(response);

            var step2 = new Step2_DHExchange();
            sender.OnResponseReceived = (response) => Sender_OnStep2ResponseReceived(step2, response);
            sender.Send(step2.ToBytes(
                    step1Response.Nonce,
                    step1Response.ServerNonce,
                    step1Response.Fingerprints,
                    step1Response.Pq));
        }

        private void Sender_OnStep2ResponseReceived(Step2_DHExchange step2, byte[] response)
        {
            var step2Response = step2.FromBytes(response);


            var step3 = new Step3_CompleteDHExchange();
            sender.OnResponseReceived = (response) => Sender_OnStep3ResponseReceived(step3, response);
            sender.Send(step3.ToBytes(
                    step2Response.Nonce,
                    step2Response.ServerNonce,
                    step2Response.NewNonce,
                    step2Response.EncryptedAnswer));
        }

        private void Sender_OnStep3ResponseReceived(Step3_CompleteDHExchange step3, byte[] response)
        {
            completionSource.SetResult(step3.FromBytes(response));
        }
    }
}