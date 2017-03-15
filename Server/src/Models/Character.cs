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
        
        public Guid Id { get; set; }
        public Guid AccountId { get; set; }

        public string Name { get; set; }
        public Vector3S Location { get; set; }
        public Vector3S Rotation { get; set; }
        public int Health { get; set; }
        public int Armor { get; set; }
        public Dictionary<WeaponHash, WeaponS> Weapons { get; set; } = new Dictionary<WeaponHash, WeaponS>();
        public DateTime CreatedAt { get; set; }
        public DateTime LastSyncedAt { get; set; }

        public Character(string name,Account account) : this(name,account.Id) { }
        public Character(string name,Guid accountId)
        {
            Id = Guid.NewGuid();
            Name = name;
            AccountId = accountId;
            CreatedAt = DateTime.Now;
            LastSyncedAt = DateTime.Now;
        }
        protected Character()
        {
            
        }
        public virtual void ApplyTo(Client client)
        {
            client.position = Location;
            client.rotation = Rotation;
            client.health = Health;
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

    [Serializable]
    public class Vector3S
    {
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }

        protected Vector3S() { }
        public Vector3S(float x,float y,float z)
        {
            X = x; Y = y; Z = z;
        }
        public Vector3S(Vector3 vec)
        {
            X = vec.X;
            Y = vec.Y;
            Z = vec.Z;
        }

        public static implicit operator Vector3(Vector3S v)
        {
            return new Vector3(v.X, v.Y, v.Z);
        }
        public static implicit operator Vector3S(Vector3 v)
        {
            return new Vector3S(v.X, v.Y, v.Z);
        }

        public override bool Equals(object obj)
        {
            var vec = obj as Vector3S;
            if (vec == null)
                return false;
            return vec.X == X && vec.Y == Y && vec.Z == Z;
        }

        public override int GetHashCode()
        {
            return (17 * (X.GetHashCode()) * 23 + Y.GetHashCode()) * 23 + Z.GetHashCode();
        }
    }

    [Serializable]
    public class WeaponS
    {
        public WeaponHash Weapon { get; set; }
        public int Ammo { get; set; } = 100; // TODO: find a way to get ammo when saving a WeaponS
        public IList<WeaponComponent> Components { get; set; } = new List<WeaponComponent>();
        public WeaponTint Tint { get; set; } = WeaponTint.Normal;
        public WeaponS(WeaponHash hash)
        {
            Weapon = hash;
        }
        protected WeaponS() { }

        public override bool Equals(object obj)
        {
            var weaponS = obj as WeaponS;
            if (weaponS == null)
                return false;
            return Weapon == weaponS.Weapon && Ammo == weaponS.Ammo && Components.SequenceEqual(weaponS.Components) && Tint == weaponS.Tint;
        }

        public override int GetHashCode()
        {
            return (((17 * Weapon.GetHashCode()) * 23) + Ammo.GetHashCode() * 23) + Components.GetHashCode() * 23 + Tint.GetHashCode();
        }
    }
}
