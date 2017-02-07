using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using RestSharp;

namespace Hooq
{
    public class QueueProcessor
    {
        private readonly HooqOptions _options;
        private IRestClient _restClient;

        /// <summary>
        /// Creates a new queue processor
        /// </summary>
        /// <param name="apiKey">Your API key</param>
        /// <param name="queueKey">Your queue key</param>
        public QueueProcessor(string apiKey, string queueKey)
        {
            _options = new HooqOptions(apiKey, queueKey);
            Setup();
        }

        /// <summary>
        /// Creates a new queue processor
        /// </summary>
        /// <param name="apiKey">Your API key</param>
        /// <param name="queueKey">Your queue key</param>
        /// <param name="take">Number of items to pull from queue at once</param>
        public QueueProcessor(string apiKey, string queueKey, int take)
        {
            _options = new HooqOptions(apiKey, queueKey) { Take = take };
            Setup();
        }

        /// <summary>
        /// Creates a new queue processor
        /// </summary>
        /// <param name="apiKey">Your API key</param>
        /// <param name="queueKey">Your queue key</param>
        /// <param name="take">Number of items to pull from queue at once</param>
        /// <param name="timeout">Time to hide the message from the queue</param>
        public QueueProcessor(string apiKey, string queueKey, int take, int timeout)
        {
            _options = new HooqOptions(apiKey, queueKey)
            {
                Take = take,
                Timeout = timeout
            };
            Setup();
        }

        /// <summary>
        /// Creates a new queue processor
        /// </summary>
        /// <param name="options">A populated HooqQueueOptions model</param>
        public QueueProcessor(HooqOptions options)
        {
            _options = options;
            Setup();
        }

        /// <summary>
        /// Sets up RestSharp to make requests to Hooq
        /// </summary>
        private void Setup()
        {
            _restClient = new RestClient($"{_options.Protocol}://{_options.Server}/out/{_options.ApiKey}/{_options.QueueKey}");
            _restClient.AddHandler("application/json", new HooqJsonConvertor());
        }

        /// <summary>
        /// Watch the Hooq Queue for events
        /// </summary>
        /// <typeparam name="T">Expected model body</typeparam>
        /// <param name="cancellationToken">Cancellation token, used to stop checking</param>
        /// <param name="hasMessagesCallback">Called when there's a message</param>
        /// <param name="noMessagesCallback">Called when no message, optional</param>
        /// <param name="errorCallback">Called when there has been an error, optional</param>
        public async void Watch<T>(CancellationToken cancellationToken, Func<HooqBodyReponse<T>, bool> hasMessagesCallback, Action noMessagesCallback = null, Action<HttpStatusCode, string> errorCallback = null)
        {
            var taskFactory = new TaskFactory();
            await taskFactory.StartNew
            (
                () => CheckQueue(cancellationToken, hasMessagesCallback, noMessagesCallback, errorCallback),
                cancellationToken
            );
        }

        private void CheckQueue<T>(CancellationToken cancellationToken, Func<HooqBodyReponse<T>, bool> hasMessagesCallback, Action noMessagesCallback, Action<HttpStatusCode, string> errorCallback)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                IRestRequest restRequest = new RestRequest(Method.GET);
                IRestResponse<List<HooqInternalResponse>> response = _restClient.Execute<List<HooqInternalResponse>>(restRequest);

                if (response.StatusCode != HttpStatusCode.OK)
                {
                    errorCallback?.Invoke(response.StatusCode, response.ErrorMessage);
                }

                if (!response.Data.Any())
                {
                    noMessagesCallback?.Invoke();
                }
                else
                {
                    foreach (HooqInternalResponse hooqResponse in response.Data)
                    {
                        HooqBodyReponse<T> innerResponse = JsonConvert.DeserializeObject<HooqBodyReponse<T>>(hooqResponse.MessageText);

                        if (hasMessagesCallback(innerResponse))
                        {
                            MarkMessageAsDone(hooqResponse);
                        }
                    }
                }

                Thread.Sleep(_options.Interval * 1000);
            }
        }

        private void MarkMessageAsDone(HooqInternalResponse internalResponseData)
        {
            IRestRequest request = new RestRequest($"{internalResponseData.MessageId}/{internalResponseData.PopReceipt}", Method.DELETE);
            _restClient.Execute(request);
        }
    }
}