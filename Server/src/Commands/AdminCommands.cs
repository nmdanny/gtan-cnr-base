using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTANetworkServer;
using GTAIdentity.Modules;
using GTAIdentity.Util;

namespace GTAIdentity
{
    /* The purpose of this partial class is to delegate the commands to their implementors (which lie in the AdminModule),
     * because commands aren't recognized in classes that aren't a Script.
     */
    public sealed partial class ScriptMain
    {
        [Command("banp", @"Usage: /banp player duration [duration-units] [reason]
Example:
   /banp Trump 24 hours Teamkilling players.
You may omit the reason and the duration units(defaults to hours) or just the reason.
Example:
  /banp Troll 36
  /banp Troll 72 hours
Duration units may be: 'seconds', 'minutes', 'hours', 'days'
Note: it will match any player with a name or social club handle that includes 'player'. If multiple matching players are found, no ban will be issued.", GreedyArg = true)]
        public void BanPlayer(Client caller, string player, int duration, string durationUnits = "hour", string reason = "No reason given")
        {
            AdminModule.BanPlayer(caller, player, duration, durationUnits, reason);
        }

        [Command("asyncTest")]
        public async Task DebugAsyncFlow(Client caller)
        {
            Log.Debug("Performing async test.");
            try
            {
                var response = await API.RequestResponseFlow(caller, "test", "test_response", args: new object[] { "1 plus 1 equals", 2 });
                caller.sendChatMessage($"Response is " + response);
                Log.Debug($"AsyncFlow response is " + response);
            }
            catch (TimeoutException ex)
            {
                caller.sendChatMessage($"Response has timed out: {ex.Message}");
                Log.Error($"Response timeout.", ex);
            }
        }
    }
}
