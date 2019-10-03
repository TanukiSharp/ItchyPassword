using System;
using System.Text;

namespace PasswordGenerator
{
    public class ConsolePasswordReader
    {
        private readonly StringBuilder sb = new StringBuilder();

        private int writtenLength = 0;

        public void Clear()
        {
            for (int i = 0; i < writtenLength; i++)
                Console.Write("\b \b");
        }

        private void UpdateDisplay()
        {
            Clear();

            string lengthString = sb.Length.ToString();
            writtenLength = lengthString.Length;

            Console.Write(lengthString);
        }

        public string? Read()
        {
            while (true)
            {
                ConsoleKeyInfo consoleKeyInfo = Console.ReadKey(true);

                if (consoleKeyInfo.Modifiers == 0)
                {
                    if (consoleKeyInfo.Key == ConsoleKey.Escape)
                        return null;
                    else if (consoleKeyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (sb.Length > 0)
                        {
                            sb.Remove(sb.Length - 1, 1);
                            UpdateDisplay();
                        }
                        continue;
                    }
                    else if (consoleKeyInfo.Key == ConsoleKey.Enter)
                        break;
                }

                char keyChar = consoleKeyInfo.KeyChar;

                if (keyChar >= 32 && keyChar <= 126)
                {
                    sb.Append(keyChar);
                    UpdateDisplay();
                }
                else
                    Console.Beep();
            }

            return sb.ToString();
        }
    }
}
