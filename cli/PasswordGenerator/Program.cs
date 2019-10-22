using System;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using PasswordGenerator.Core;

namespace PasswordGenerator
{
    class Program
    {
        private const int MinPasswordLength = 4;
        private const int DefaultPasswordLength = 64;

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

        private void Run()
        {
            var passwordReader = new ConsolePasswordReader();

            Console.Write("Input private part: ");
            string? privatePart = passwordReader.Read();
            passwordReader.Clear();
            Console.WriteLine();

            if (privatePart == null)
            {
                WriteLineColor(ConsoleColor.Red, "Aborted.");
                return;
            }

            if (privatePart.Length == 0)
            {
                WriteLineColor(ConsoleColor.Red, "Private part missing, aborted.");
                return;
            }

            Console.Write("Input public part: ");
            string publicPart = Console.ReadLine();

            if (publicPart.Length == 0)
            {
                WriteLineColor(ConsoleColor.Red, "Public part missing, aborted.");
                return;
            }

            int passwordLength = GetPasswordLength();

            byte[] passwordBytes = Generator.GeneratePassword(privatePart, publicPart, Generator.DefaultIterations, Generator.DefaultHashAlgorithm, "Password");

            Console.WriteLine("Generated password:");
 
            Console.WriteLine($"b16: {passwordBytes.ToBase16().Truncate(passwordLength)}");
            Console.WriteLine($"b64: {passwordBytes.ToBase64().Truncate(passwordLength)}");

            string alphabet = "!\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[]^_`abcdefghijklmnopqrstuvwxyz{|}~";
            Console.WriteLine($"b{alphabet.Length}: {passwordBytes.ToCustomBase(alphabet)}");
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
