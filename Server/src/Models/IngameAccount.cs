using System.Linq;

using GTANetworkServer;
using GTAIdentity.Models;
using GTAIdentity.Modules;

namespace GTAIdentity.Models
{
    public class IngameAccount
    {
        public Client Client { get;  }
        public Account Account { get; }
        public IngameAccount(Client client,Account account)
        {
            Client = client;
            Account = account;
        }
        public override string ToString()
        {
            return $"IngameAccount client-name: {Client.name} username: {Account.Username}";
        }
        public override bool Equals(object obj)
        {
            if (obj is IngameAccount ac2)
            {
                return ac2.Client == Client && ac2.Account == Account;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return (17 * Client.GetHashCode()) * 24 + Account.GetHashCode();
        }
    }

}
