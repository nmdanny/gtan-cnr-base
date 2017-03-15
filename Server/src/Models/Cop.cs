﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GTAIdentity.Models
{
    [Serializable]
    public class Cop : Character
    {
        public Rank Rank { get; set; }
        public string Funds { get; set; }

        public Cop(string name,Guid account) : base(name,account) { }
        public Cop(string name,Account account) : base(name,account) { }
        protected Cop() : base()
        {

        }

        public override string ToString()
        {
            return $"Cop {Name}";
        }
    }

    [Serializable]
    public enum Rank
    {
        Cadet = 0,
        Officer = 1,
        Sergeant = 2,
        Lieutenant = 3
    }
}
