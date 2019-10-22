using System;
using PasswordGenerator.Core;

namespace PasswordGenerator.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            Generator.GeneratePassword("abc", "testtest", Generator.DefaultIterations, Generator.DefaultHashAlgorithm, "Password");
            Generator.GeneratePassword("abc", "testtest", Generator.DefaultIterations, Generator.DefaultHashAlgorithm, "Password");
            Generator.GeneratePassword("abc", "testtest", Generator.DefaultIterations, Generator.DefaultHashAlgorithm, "Password");
        }
    }
}
