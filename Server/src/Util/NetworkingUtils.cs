using GTANetworkServer;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAIdentity.Util
{
    public static class NetworkingUtils
    {
        internal static JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        /// <summary>
        /// A task performing a request-response interaction with a specific client, by triggering a client side event,
        /// and then waiting for him to trigger a server-side event in response.
        /// </summary>
        /// <param name="api">An API instance, Note that it must not be API.shared, as that one doesn't support events!</param>
        /// <param name="client">The client we're interacting with</param>
        /// <param name="clientEventName">The name of the client-side event to be triggered on the client.</param>
        /// <param name="serverEventName">The name of the server-side event that we're expecting</param>
        /// <param name="millisecondsTimeout">The timeout(in milliseconds) before our task fails.</param>
        /// <param name="args">Arguments to pass to the client-side event.</param>
        /// <param name="data">The data we're sending to the client event argument.</param>
        /// <typeparam name="TData">The type of the request input argument</typeparam>
        /// <typeparam name="TResult">The type of the response result</typeparam>
        /// <returns>A task containing the arguments passed by the client's response.</returns>
        /// <exception cref="TimeoutException">Thrown when the client's response timeouts.</exception>
        public static async Task<TResult> RequestResponseFlow<TResult, TData>(this API api, Client client, string clientEventName, string serverEventName, int millisecondsTimeout = 1000, TData data = null) where TData : class
        {
            var completionSource = new TaskCompletionSource<TResult>();
            API.ServerEventTrigger handler = null;
            handler = (sender, evName, responseArgs) =>
            {
                if (sender == client && evName == serverEventName)
                {
                    var responseData = JsonConvert.DeserializeObject<TResult>((string)responseArgs[0], serializerSettings);
                    completionSource.SetResult(responseData);
                    api.onClientEventTrigger -= handler;
                }
            };
            api.onClientEventTrigger += handler;
            api.triggerClientEvent(client, clientEventName, JsonConvert.SerializeObject(data), serializerSettings);
            return await completionSource.Task.TimeoutAfter(millisecondsTimeout);

        }
        /// <summary>
        /// Performs a typed request interaction with a client by triggering a client side event,
        /// without waiting for a response.
        /// </summary>
        /// <typeparam name="TData">The type of the request input argument</typeparam>
        /// <param name="api">An API instance, Note that it must not be API.shared, as that one doesn't support events!</param>
        /// <param name="client">The client we're interacting with</param>
        /// <param name="clientEventName">The name of the client-side event to be triggered on the client.</param>
        /// <param name="data">The data we're sending to the client event argument.</param>
        public static void RequestResponseFlow<TData>(this API api, Client client, string clientEventName, TData data = null) where TData : class
        {
            api.triggerClientEvent(client, clientEventName, JsonConvert.SerializeObject(data, serializerSettings));
        }



        public static IObservable<(Client, string, object[])> OnClientEventTrigger(this API api)
        {
            return Observable.FromEvent<(Client, string, object[])>(
            h => api.onClientEventTrigger += (sender, evName, args) =>
            {
                h((sender, evName, args));
            },
            h => api.onClientEventTrigger -= (sender, evName, args) =>
            {
                h((sender, evName, args));
            });
        }

        public static IObservable<object[]> OnClientEventTrigger(this API api, Client client, string eventName)
        {
            return api.OnClientEventTrigger().Where((tuple) =>
            {
                var (sender, evName, args) = tuple;
                return sender == client && evName == eventName;
            }).Select(tuple => tuple.Item3);
        }
        public static IObservable<TResult> OnClientEventTrigger<TResult>(this API api, Client client, string eventName)
        {
            return api.OnClientEventTrigger(client, eventName).Select(objs => JsonConvert.DeserializeObject<TResult>((string)objs[0], serializerSettings));
        }

        public static IObservable<(Client, object[])> OnClientEventTrigger(this API api, string eventName)
        {
            return api.OnClientEventTrigger().Where((tuple) =>
            {
                var (sender, evName, args) = tuple;
                return evName == eventName;
            }).Select(tuple => (tuple.Item1, tuple.Item3));
        }

        public static IObservable<(Client, TResult)> OnClientEventTrigger<TResult>(this API api, string eventName)
        {
            return api.OnClientEventTrigger(eventName).Select((tuple) =>
            {
                return (tuple.Item1, JsonConvert.DeserializeObject<TResult>((string)tuple.Item2[0]));
            });
        }


        public static IObservable<(string, object[])> OnClientEventTrigger(this API api, Client client)
        {
            return api.OnClientEventTrigger().Where((tuple) =>
            {
                var (sender, evName, args) = tuple;
                return sender == client;
            }).Select(tuple => (tuple.Item2, tuple.Item3));
        }

        public static IObservable<(string, TData)> OnClientEventTrigger<TData>(this API api, Client client)
        {
            return api.OnClientEventTrigger(client).Select(tuple =>
            {
                return (tuple.Item1, JsonConvert.DeserializeObject<TData>((string)tuple.Item2[0]));
            });
        }
    }
}
