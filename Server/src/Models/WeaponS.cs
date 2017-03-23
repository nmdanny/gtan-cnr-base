using GTANetworkServer;
using GTANetworkShared;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GTAIdentity.Models
{
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
