using System.Linq;

using GTANetworkServer;
using GTAIdentity.Models;
using GTAIdentity.Modules;

namespace GTAIdentity.Models
{
    /// <summary>
    /// An representation of a <see cref="Client"/> that is logged into an <see cref="Account"/>, and may be logged into a <see cref="Character"/>.
    /// </summary>
    public class LoggedPlayer
    {
        public Client Client { get; }
        public Account Account { get; }
        public Character Character { get; set; }
        public bool ClientConnected { get => !Client.handle.IsNull; }
        public bool CharacterLogged { get => Character != null; }
        public LoggedPlayer(Client client,Account account)
        {
            Client = client;
            Account = account;
        }

        public override bool Equals(object obj)
        {
            var lp = obj as LoggedPlayer;
            if (lp == null)
                return false;
            return lp.Client.Equals(Client) && lp.Account.Id.Equals(Account.Id);
        }
        public override int GetHashCode()
        {
            return Client.GetHashCode() * 17 + Account.Id.GetHashCode();
        }

        public override string ToString()
        {
            var info = $"[Logged Client name: {Client.name}, Account name: {Account.Username}";
            if (CharacterLogged)
                info += $", Character name: {Character.Name}]";
            else info += "]";
            return info;
        }
    }
}
