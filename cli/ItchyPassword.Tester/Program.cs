using System;
using System.Security.Cryptography;
using ItchyPassword.Core;

namespace ItchyPassword.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            //byte[] privatePart = "a483ec2b6a773fc0b1bc1030c6356606ac64c712130da68a0d5082c797c7735679c63619451a3e412a5671a81669d9f553ae1a56d9cea158925a8f6aa3285155".FromBase16();
            //byte[] publicPart = "c2424e1857bc5dd90265ce83bc878ccd6326cbf4d9761ba652945146dbb20be5c44597530388e3fc3c0b4ef6d88089af2d240d211c615f73420ce20a6a33703e".FromBase16();

            //string generatedPassword = Crypto.GeneratePassword(privatePart, publicPart, "Password").ToBase16();

            //if (generatedPassword != "72f63b96bd7483b0cce1a242206c7457ecbe34878ec9e1e971c95802f03d13aebe94c7148a4f1b577d2927112108d9c476cba8c9e5feaddacace983ee08b68e6")
            //    throw new Exception();

            // **************************************************************************************************

            //var rng = RandomNumberGenerator.Create();

            //byte[] privatePartBytes = new byte[64];
            //rng.GetBytes(privatePartBytes);
            //byte[] publicPartBytes = new byte[64];
            //rng.GetBytes(publicPartBytes);
            //byte[] passwordBytes = Crypto.GeneratePassword(privatePartBytes, publicPartBytes, "Password");

            //string privatePart = privatePartBytes.ToBase16();
            //string publicPart = publicPartBytes.ToBase16();
            //string password = passwordBytes.ToBase16();

            //Console.WriteLine($"privatePart: {privatePart}");
            //Console.WriteLine($"publicPart: {publicPart}");
            //Console.WriteLine($"password: {password}");

            // **************************************************************************************************

            ////Crypto.GeneratePassword("abc", "testtest", Crypto.DefaultIterations, Crypto.DefaultHashAlgorithm, "Password");
            ////Crypto.GeneratePassword("abc", "testtest", Crypto.DefaultIterations, Crypto.DefaultHashAlgorithm, "Password");
            ////Crypto.GeneratePassword("abc", "testtest", Crypto.DefaultIterations, Crypto.DefaultHashAlgorithm, "Password");

            //var clear = new Span<byte>(new byte[] { 1, 2, 3, 4, 50, 6, 7, 8, 9, 100, 11, 12, 13, 14, 150, 16, 17 });
            //var password = new byte[] { 201, 202, 203, 204, 205, 206, 207, 208, 209, 210, 211, 212, 213, 214, 215, 216 };

            ////var encrypted = new Span<byte>(new byte[clear.Length + 12 + 16]);
            ////Crypto.Encrypt(clear, password, encrypted);
            //var encrypted = Crypto.Encrypt(clear, password);

            ////var encrypted = new Span<byte>(new byte[] { 145, 217, 78, 105, 28, 80, 35, 82, 127, 243, 40, 102, 165, 207, 38, 5, 160, 39, 154, 88, 205, 85, 35, 90, 148, 201, 81, 209, 227, 140, 20, 210, 250, 75, 84, 193, 209, 88, 203, 200, 163, 244, 61, 193, 217 });

            ////var decrypted = new Span<byte>(new byte[clear.Length]);
            ////Crypto.Decrypt(encrypted, password, decrypted);
            //var decrypted = Crypto.Decrypt(encrypted, password);

            //if (clear.SequenceEqual(decrypted) == false)
            //    throw new Exception();

            // **************************************************************************************************

            using var rng = RandomNumberGenerator.Create();

            byte[] privatePartBytes = new byte[256];
            rng.GetBytes(privatePartBytes);

            byte[] password = new byte[256];
            rng.GetBytes(password);

            byte[] decrypted = Crypto.Decrypt(Crypto.Encrypt(privatePartBytes, password), password);

            if (new Span<byte>(privatePartBytes).SequenceEqual(decrypted) == false)
                throw new Exception();
        }
    }
}
