using Laptop.Models;
using Microsoft.AspNetCore.Identity;

namespace Laptop.Security
{
    public static class PasswordSecurity
    {
        private static readonly PasswordHasher<User> Hasher = new();

        public static string HashPassword(User user, string password)
        {
            return Hasher.HashPassword(user, password);
        }

        public static bool VerifyPassword(User user, string providedPassword, out bool needsUpgrade)
        {
            needsUpgrade = false;

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                return false;
            }

            PasswordVerificationResult verificationResult;
            try
            {
                verificationResult = Hasher.VerifyHashedPassword(user, user.Password, providedPassword);
            }
            catch (FormatException)
            {
                verificationResult = PasswordVerificationResult.Failed;
            }

            if (verificationResult == PasswordVerificationResult.Success)
            {
                return true;
            }

            if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                needsUpgrade = true;
                return true;
            }

            // Support legacy plain-text passwords already stored in the database.
            if (user.Password == providedPassword)
            {
                needsUpgrade = true;
                return true;
            }

            return false;
        }
    }
}
