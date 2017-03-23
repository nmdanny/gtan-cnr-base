using Common.Logging;
using GTANetworkServer;
using GTANetworkShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAIdentity.Util
{
    public static class APIUtils
    {

        public static Client GetClient(this API api,NetHandle handle)
        {
            foreach (var player in api.getAllPlayers())
            {
                if (player.handle == handle)
                    return player;
            }
            return null;
        }

        public static void ClientLog(this ILog logger,Client client,string msg)
        {
            logger.Info($"{client.name} got error: {msg}");
            client.sendChatMessage($"~r~ERROR:~s~ {msg}");
        }
    }
}
