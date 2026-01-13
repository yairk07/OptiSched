using System.Security.Cryptography;
using System.Text;

public class PasswordHelper
{
    // Hashes a password using SHA256 algorithm and returns hexadecimal string representation
    public static string HashPassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return string.Empty;

        using (SHA256 sha256Hash = SHA256.Create())
        {
            // Compute hash from UTF-8 encoded password bytes
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));

            // Convert hash bytes to hexadecimal string
            StringBuilder builder = new StringBuilder();
            for (int i = 0; i < bytes.Length; i++)
            {
                builder.Append(bytes[i].ToString("x2"));
            }
            return builder.ToString();
        }
    }
}




