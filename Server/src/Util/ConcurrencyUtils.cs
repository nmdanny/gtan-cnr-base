using GTANetworkServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GTAIdentity.Util
{
    public static class ConcurrencyUtils
    {
        public static async Task<T> TimeoutAfter<T>(this Task<T> task,int millisecondsTimeout)
        {
            var cts = new CancellationTokenSource();
            var whichFinishedFirst = await Task.WhenAny(task, Task.Delay(millisecondsTimeout, cts.Token));
            if (whichFinishedFirst == task)
            {
                cts.Cancel();
                // we await the task, instead of accessing its Result, to propogate cancellation or errors.
                return await task;
            }
            else
                throw new TimeoutException();
            
        }
        public static async Task TimeoutAfter(this Task task,int millisecondsTimeout)
        {
            var cts = new CancellationTokenSource();
            var whichFinishedFirst = await Task.WhenAny(task, Task.Delay(millisecondsTimeout, cts.Token));
            if (whichFinishedFirst == task)
            {
                cts.Cancel();
                await task;
            }
            else
                throw new TimeoutException();
        }


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
        /// <returns>A task containing the arguments passed by the client's response.</returns>
        /// <exception cref="TimeoutException">Thrown when the client's response timeouts.</exception>
        public static async Task<object[]> RequestResponseFlow(this API api, Client client, string clientEventName, string serverEventName, int millisecondsTimeout = 1000,params object[] args)
        {
            var completionSource = new TaskCompletionSource<object[]>();
            API.ServerEventTrigger handler = (sender, evName, responseArgs) =>
            {
                if (sender == client && evName == serverEventName)
                {
                    completionSource.SetResult(args);
                }
            };
            api.onClientEventTrigger += handler;
            api.triggerClientEvent(client, clientEventName, args);
            try
            {
                var result = await completionSource.Task.TimeoutAfter(millisecondsTimeout);
                return result;
            }
            finally
            {
                api.onClientEventTrigger -= handler;
            }
        }
    }
}
