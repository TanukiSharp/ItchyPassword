using System.Diagnostics;
using System.Text;
using ItchyPassword.Core;

const int Encrypted = 1;
const int Clear = 2;

if (args.Length == 0)
{
    Console.WriteLine("Pass a file as argument, empty file to start.");
    return -1;
}

if (args.Length > 1)
{
    Console.WriteLine("Pass only one file as argument, empty file to start.");
    return -1;
}

Console.WriteLine("Is file content encrypted ?");
Console.WriteLine("1. Yes (default)");
Console.WriteLine("2. No");
Console.WriteLine();
Console.Write("> ");

string? response = Console.ReadLine();

if (response == null)
{
    Console.WriteLine("Invalid input, exiting.");
    return -2;
}

if (response == "")
    response = Encrypted.ToString();

if (int.TryParse(response, out int contentType) == false || contentType < Encrypted || contentType > Clear)
{
    Console.WriteLine("Invalid input, exiting.");
    return -2;
}

byte[] fileContent = File.ReadAllBytes(args[0]);
byte[] clearData;

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
string workingFile = Path.ChangeExtension(args[0], $".tmp{ext}");

if (contentType == Encrypted)
{
    Console.WriteLine("Decrypting file content...");
    clearData = Crypto.Decrypt(fileContent, Encoding.UTF8.GetBytes(masterKey));
}
else
{
    clearData = fileContent;
}

File.WriteAllBytes(workingFile, clearData);

Process? p = Process.Start(new ProcessStartInfo(workingFile)
{
    UseShellExecute = true
});

if (p == null)
{
    Console.WriteLine("Failed to run input file.");
    return -1;
}

Console.WriteLine("Opened file with application.");
p.WaitForExit();
Console.WriteLine("Application closed.");

fileContent = File.ReadAllBytes(workingFile);

Console.WriteLine("Encrypting file content...");

byte[] encryptedData = Crypto.Encrypt(fileContent, Encoding.UTF8.GetBytes(masterKey));

File.WriteAllBytes(args[0], encryptedData);
File.Delete(workingFile);

Console.WriteLine("File protected.");

return 0;
