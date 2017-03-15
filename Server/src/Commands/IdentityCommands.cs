using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GTANetworkServer;
using GTAIdentity.Modules;
namespace GTAIdentity
{
    /* The purpose of this partial class is to delegate the commands to their implementors (which lie in the IdentityModule),
     * because commands aren't recognized in classes that aren't a Script.
     */
    public sealed partial class ScriptMain
    {
        [Command("register", "Usage: /register username password", SensitiveInfo = true, GreedyArg = true, Alias = "signup,subscribe")]
        public void ClientRegister(Client client, string username, string password)
        {
            Identity.ClientRegister(client, username, password);
        }

        [Command("login", "Usage: /login username password", SensitiveInfo = true, GreedyArg = true, Alias = "signin")]
        public void ClientLogin(Client client, string username, string password)
        {
            Identity.ClientLogin(client, username, password);
        }

        [Command("logout", "Usage: /logout", Alias = "signout")]
        public void ClientLogout(Client client)
        {
            Identity.ClientLogout(client);
        }

        [Command("whoami", "Usage: /whoami")]
        public void Whoami(Client client)
        {
            Identity.Whoami(client);
        }

        [Command("createCharacter", "Usage: /createCharacter character_name")]
        public void CharacterRegister(Client client,string charName)
        {
            Identity.CharacterRegister(client, charName);
        }

        [Command("enterCharacter", "Usage: /enterCharacter character_name")]
        public void CharacterEnter(Client client,string charName)
        {
            Identity.CharacterEnter(client, charName);
        }

        [Command("leaveCharacter", "Usage: /characterLeave")]
        public void CharacterLeave(Client client)
        {
            Identity.CharacterLeave(client);
        }
    }
}
