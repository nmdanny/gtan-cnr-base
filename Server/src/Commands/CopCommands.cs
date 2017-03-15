using GTAIdentity.Models;
using GTANetworkServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAIdentity
{
    public sealed partial class ScriptMain
    {
        [Command("arrest","Usage: /arrest",Alias = "ar")]
        public async Task Arrest(Client caller)
        {
            await CrimeModule.ArrestNearby(caller);
        }
    }
}
