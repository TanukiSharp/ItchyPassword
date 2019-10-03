using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;

namespace PasswordGenerator
{
    class Program
    {
        private const int MinPasswordLength = 4;
        private const int DefaultPasswordLength = 64;

        private byte[] MakeAtLeast8Bytes(byte[] salt)
        {
            if (salt == null)
                throw new ArgumentNullException(nameof(salt));

            if (salt.Length >= 8)
                return salt;

            byte[] result = new byte[8];

            for (int i = 0; i < result.Length; i += salt.Length)
                Array.Copy(salt, 0, result, i, Math.Min(salt.Length, result.Length - i));

            return result;
        }

        private byte[] GeneratePassword(string privateKey, string publicKey)
        {
            if (publicKey == null)
                throw new ArgumentNullException(nameof(publicKey));
            if (privateKey == null)
                throw new ArgumentNullException(nameof(privateKey));

            if (publicKey.Length == 0)
                throw new ArgumentException($"Argument '{nameof(publicKey)}' is invalid. Must not be empty.", nameof(publicKey));
            if (privateKey.Length == 0)
                throw new ArgumentException($"Argument '{nameof(publicKey)}' is invalid. Must not be empty.", nameof(publicKey));

            byte[] password = Encoding.UTF8.GetBytes(privateKey);
            byte[] salt = Encoding.UTF8.GetBytes(publicKey);

            using var algorithm = new Rfc2898DeriveBytes(password, MakeAtLeast8Bytes(salt), 100_000, HashAlgorithmName.SHA512);

            return algorithm.GetBytes(32);
        }

        private static void WriteLineColor(ConsoleColor color, string text)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(text);
            Console.ResetColor();
        }

        private int GetPasswordLength()
        {
            int passwordLength;

            while (true)
            {
                Console.Write($"Output password length (default to {DefaultPasswordLength}): ");
                string passwordLengthStr = Console.ReadLine();

                if (passwordLengthStr.Length == 0)
                {
                    passwordLength = DefaultPasswordLength;
                    break;
                }
                else if (int.TryParse(passwordLengthStr, out passwordLength))
                {
                    if (passwordLength < MinPasswordLength)
                    {
                        WriteLineColor(ConsoleColor.Yellow, $"Password length too short, must be greater than or equal to {MinPasswordLength}.");
                        continue;
                    }
                    break;
                }
                WriteLineColor(ConsoleColor.Yellow, "Invalid length input, please retry.");
            }

            return passwordLength;
        }

        private string ToCustomBase(byte[] bytes, string alphabet)
        {
            var number = new BigInteger(bytes, true);
            var alphabetLength = new BigInteger(alphabet.Length);

            var result = new StringBuilder();

            while (number > BigInteger.Zero)
            {
                number = BigInteger.DivRem(number, alphabetLength, out BigInteger remainder);
                int index = int.Parse(remainder.ToString());

                result.Append(alphabet[index]);
            }

            return result.ToString();
        }

        private void Run()
        {
            //var passwordReader = new ConsolePasswordReader();

            //Console.Write("Input private part: ");
            //string? privatePart = passwordReader.Read();
            //passwordReader.Clear();
            //Console.WriteLine();

            //if (privatePart == null)
            //{
            //    WriteLineColor(ConsoleColor.Red, "Aborted.");
            //    return;
            //}

            //if (privatePart.Length == 0)
            //{
            //    WriteLineColor(ConsoleColor.Red, "Private part missing, aborted.");
            //    return;
            //}

            //Console.Write("Input public part: ");
            //string publicPart = Console.ReadLine();

            //if (publicPart.Length == 0)
            //{
            //    WriteLineColor(ConsoleColor.Red, "Public part missing, aborted.");
            //    return;
            //}

            string privatePart = "furet";
            string publicPart = "en-mousse";

            //int passwordLength = GetPasswordLength();
            int passwordLength = 128;

            byte[] passwordBytes = GeneratePassword(privatePart, publicPart);

            string base64Password = Convert.ToBase64String(passwordBytes, Base64FormattingOptions.None);
            string base16Password = string.Concat(passwordBytes.Select(x => x.ToString("x2")));

            Console.WriteLine("Generated password:");
            Console.WriteLine($"b16: {base16Password.Substring(0, Math.Min(passwordLength, base16Password.Length))}");
            Console.WriteLine($"b64: {base64Password.Substring(0, Math.Min(passwordLength, base64Password.Length))}");

            string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789`~!@#$%^&*()_-=+[{]{|;:'\",<.>/?";
            Console.WriteLine($"b{alphabet.Length}: {ToCustomBase(passwordBytes, alphabet)}");
        }

        private static void PrintHeader()
        {
            string version = Assembly.GetEntryAssembly()!
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()!
                .InformationalVersion;

            Console.WriteLine($"PasswordGenerator v{version}");
            Console.WriteLine();
        }

        static void Main()
        {
            PrintHeader();
            new Program().Run();
        }
    }
}
