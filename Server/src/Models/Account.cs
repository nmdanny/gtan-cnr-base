using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GTAIdentity.Util;

namespace GTAIdentity.Models
{
    public class Account
    {
        public Guid Id { get; set;  }
        public string Username { get; set; }
        public string SaltedPassword { get; set; }
        public DateTime RegistrationDate { get; set; }
        public DateTime LastLoggedDate { get; set; }
        public Role Role { get; set; }
        private Account()
        {

        }
        public Account(string username, string plainPassword, Role role = Role.PlainUser)
        {
            this.Id = Guid.NewGuid();
            this.Username = username;
            this.SaltedPassword = PasswordDerivation.Derive(plainPassword);
            this.RegistrationDate = DateTime.Now;
            this.LastLoggedDate = DateTime.Now;
        }
        public bool VerifyLogin(string plainPassword)
        {
            return PasswordDerivation.Verify(this.SaltedPassword, plainPassword);
        }

        public override bool Equals(object obj)
        {
            var acc = obj as Account;
            if (acc == null)
                return false;
            return acc.Id == Id;
        }

        public override int GetHashCode()
        {
            return (17 * GetType().GetHashCode()) * 23 + Id.GetHashCode();
        }
    }   
    public enum Role
    {
        PlainUser = 0,
        Moderator = 1,
        Admin = 2,
        SuperAdmin = 3,
        RootAccess = 4
    }

}
