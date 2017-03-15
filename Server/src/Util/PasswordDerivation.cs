using System;
using System.Linq;
using System.Security.Cryptography;

namespace GTAIdentity.Util
{
    public static class PasswordDerivation
    {
        public const int defaultSaltSize = 16;
        public const int defaultKeySize = 16;
        public const int defaultIterations = 10000;

        public static string Derive(string plainPassword, int saltSize = defaultSaltSize, int iterations = defaultIterations, int keySize = defaultKeySize)
        {
            using (var derive = new Rfc2898DeriveBytes(plainPassword, saltSize: saltSize, iterations: iterations))
            {
                var b64Pwd = Convert.ToBase64String(derive.GetBytes(keySize));
                var b64Salt = Convert.ToBase64String(derive.Salt);
                return string.Join(":", b64Salt, iterations.ToString(), keySize.ToString(), b64Pwd);
            }
        }
        public static bool Verify(string saltedPassword, string plainPassword)
        {
            System.Diagnostics.Debug.Assert(!string.IsNullOrEmpty(saltedPassword),"Error verifying password: Salted password is null.");
            System.Diagnostics.Debug.Assert(!string.IsNullOrWhiteSpace(plainPassword),"Error verifying password: Plain password is null.");
            var passwordParts = saltedPassword.Split(':');
            System.Diagnostics.Debug.Assert(passwordParts.Length == 4, "Salted password must contain 4 elements delimited by a ':'.");
            var salt = Convert.FromBase64String(passwordParts[0]);
            var iters = int.Parse(passwordParts[1]);
            var keySize = int.Parse(passwordParts[2]);
            var pwd = Convert.FromBase64String(passwordParts[3]);
            using (var derive = new Rfc2898DeriveBytes(plainPassword, salt: salt, iterations: iters))
            {
                var hashedInput = derive.GetBytes(keySize);
                return hashedInput.SequenceEqual(pwd);
            }
        }
    }
}
