using System.Diagnostics;
using System.Text;
using ItchyPassword.Core;

const int Encrypt = 1;
const int Decrypt = 2;

if (args.Length != 1)
{
    Console.WriteLine("Pass one (and only one) file as argument.");
    return -1;
}

Console.WriteLine($"Do you want to encrypt or decrypt the file ? [{args[0]}]");
Console.WriteLine("1. Encrypt");
Console.WriteLine("2. Decrypt");
Console.WriteLine();
Console.Write("> ");

string? response = Console.ReadLine();

if (string.IsNullOrWhiteSpace(response))
{
    Console.WriteLine("Invalid input, exiting.");
    return -2;
}

if (int.TryParse(response, out int operationType) == false || operationType < Encrypt || operationType > Decrypt)
{
    Console.WriteLine("Invalid input, exiting.");
    return -2;
}

byte[] fileContent = File.ReadAllBytes(args[0]);
byte[] resultData;

Console.Write("Input master key: ");
Console.Out.Flush();

Console.ForegroundColor = ConsoleColor.White;
Console.BackgroundColor = ConsoleColor.White;

string? masterKey = Console.ReadLine();

Console.ResetColor();

if (masterKey == null || masterKey.Length == 0)
{
    Console.WriteLine("Invalid input, exiting.");
    return -2;
}

Console.WriteLine();
Console.WriteLine($"Master key is {masterKey.Length} characters long.");

string ext = Path.GetExtension(args[0]);
string workingFile = Path.ChangeExtension(args[0], $".output{ext}");

if (operationType == Encrypt)
{
    Console.WriteLine($"Encrypting file content... [{workingFile}]");
    resultData = Crypto.Encrypt(fileContent, Encoding.UTF8.GetBytes(masterKey));
}
else
{
    Console.WriteLine($"Decrypting file content... [{workingFile}]");
    resultData = Crypto.Decrypt(fileContent, Encoding.UTF8.GetBytes(masterKey));
}

File.WriteAllBytes(workingFile, resultData);

return 0;
