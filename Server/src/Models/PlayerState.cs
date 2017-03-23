using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAIdentity.Models
{
    public enum PlayerState
    {
        Connected = 0,
        AccountLogged = 1,
        CharacterLogged = 2
    }

    public class PlayerStateChange : EventArgs
    {
        public PlayerState PlayerState { get; set; }
        public Character Character { get; set; }
        public Account Account { get; set; }

        public IEnumerable<string> CharacterNames { get; set; }
    }
}
