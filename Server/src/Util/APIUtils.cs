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
    }
}
