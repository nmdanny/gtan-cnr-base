using GTANetworkServer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
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



        
    }



}
