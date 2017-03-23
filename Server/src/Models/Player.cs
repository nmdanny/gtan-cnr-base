
using GTANetworkServer;

namespace GTAIdentity.Models
{
    public class Player
    {
        public Client Client { get; }
        public Character Character { get; }
        public Player(Client client,Character character)
        {
            Client = client;
            Character = character;
        }
        public override bool Equals(object obj)
        {
            if (obj is Player p2)
            {
                return Client == p2.Client && Character == p2.Character;
            }
            return false;
        }
        public override int GetHashCode()
        {
            return (17 * Client.GetHashCode()) * 24 + Character.GetHashCode();
        }
        public override string ToString()
        {
            return $"Player client-name: {Client.name} character-name: {Character.Name}";
        }
    }

}
