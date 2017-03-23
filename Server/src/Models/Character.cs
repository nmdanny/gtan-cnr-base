using GTANetworkServer;
using GTANetworkShared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAIdentity.Models
{
    /// <summary>
    /// A Character is a persistent representation of an ingame client, meant to be synchronized from and to a <see cref="Client"/>.
    /// </summary>
    public class Character
    {
        
        public static readonly Vector3S DefaultLocation = new Vector3S(0,0,0);

        public Guid Id { get; set; }
        public Guid AccountId { get; set; }

        public string Name { get; set; }
        public Vector3S Location { get; set; }
        public Vector3S Rotation { get; set; }
        public int Health { get; set; } = 100;
        public int Armor { get; set; } = 0;
        public Dictionary<WeaponHash, WeaponS> Weapons { get; set; } = new Dictionary<WeaponHash, WeaponS>();
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime LastSyncedAt { get; set; }

        public Character(string name,Client client,Account account) : this(name,client,account.Id) { }
        public Character(string name,Client client,Guid accountId)
        {
            Id = Guid.NewGuid();
            Name = name;
            AccountId = accountId;
            CreatedAt = DateTime.Now;
            UpdateFrom(client);
        }
        protected Character()
        {
            
        }
        public virtual void ApplyTo(Client client)
        {
            client.position = Location ?? new Vector3(67.8051453,12.3775034,69.2144);
            client.rotation = Rotation ?? new Vector3(0,0,168.847839);
            client.health = Health > 0 ? Health : 100;
            client.armor = Armor;
            client.name = Name;
            client.nametag = Name;
            client.nametagVisible = true;
            client.nametagColor = new GTANetworkServer.Constant.Color(255, 255, 255, 255);
            foreach (var wep in Weapons.Values)
            {
                client.giveWeapon(wep.Weapon, wep.Ammo, false, true);
                client.setWeaponTint(wep.Weapon, wep.Tint);
                foreach (var component in wep.Components)
                    client.setWeaponComponent(wep.Weapon, component);
            }
        }
        public virtual void UpdateFrom(Client client)
        {
            Health = client.health;
            Armor = client.armor;
            Location = client.position;
            Rotation = client.rotation;
            LastSyncedAt = DateTime.Now;
            Weapons = client.weapons.ToDictionary(hash => hash, hash => new WeaponS(hash)
            {
                Tint = client.getWeaponTint(hash),
                Components = client.GetAllWeaponComponents(hash)
            });
            
        }

        public virtual string GetStats()
        {
            return $"Name: {Name}, Health: {Health}, Armor: {Armor}\n" +
                $"Character created at {CreatedAt}, last synced at {LastSyncedAt}";
        }

        public override string ToString()
        {
            return $"Character {Name}";
        }

        public override bool Equals(object obj)
        {
            var chara = obj as Character;
            if (chara == null)
                return false;
            return chara.Id == Id;
        }
        public override int GetHashCode()
        {
            return (17 * GetType().GetHashCode()) * 23 + Id.GetHashCode();
        }

    }
}
