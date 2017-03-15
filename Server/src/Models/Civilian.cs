using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAIdentity.Models
{
    [Serializable]
    public class Civilian : Character
    {
        public Wanted WantedLevel { get; set; }
        public decimal Cash { get; set; }
        public Job Job { get; set; }

        public Civilian(string name,Account account) : base(name,account) { }
        public Civilian(string name,Guid account) : base(name,account) { }

        public override string GetStats()
        {
            var info = base.GetStats() + "$\n" +
                $"Wanted level: {WantedLevel}, Cash: {Cash}";
            return info;
        }

        public override string ToString()
        {
            return $"Civilian {Name}";
        }

        protected Civilian() : base()
        {
        }
    }

    [Serializable]
    public enum Wanted
    {
        None = 0,
        Ticketable = 1,
        Arrestable = 2,
        KillAuthorized = 3
    }
    
    [Serializable]
    public enum Job
    {
        Unemployed = 0,
        Thief = 1,

    }

}
