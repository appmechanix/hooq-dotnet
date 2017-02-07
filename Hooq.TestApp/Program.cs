using System;
using System.Threading;

namespace Hooq.TestApp
{
    class Program
    {
        private static readonly QueueProcessor QueueProcessor = new QueueProcessor("APIKey", "QueueKey");
        private static readonly CancellationTokenSource CancellationTokenSource = new CancellationTokenSource();

        static void Main()
        {
            QueueProcessor.Watch<StripeWebhook>(CancellationTokenSource.Token, HasMessagesCallback, NoMessagesCallback);

            while (!CancellationTokenSource.IsCancellationRequested)
            {
                Thread.Sleep(1000 * 60);
            }
        }

        private static void NoMessagesCallback()
        {
            Console.WriteLine("No messages");
        }

        private static bool HasMessagesCallback(HooqBodyReponse<StripeWebhook> hooqResponse)
        {
            Console.WriteLine("StripeID: {0}", hooqResponse.Body.Id);
            return true;
        }
    }
}
