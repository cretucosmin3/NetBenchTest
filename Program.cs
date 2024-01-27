using System;
using System.Linq;
using System.Threading;
using NetBenchTest.Common.Utilities;
using NetBenchTest.Networking;
using NetBenchTest.Networking.Transport;

namespace NetBenchTest;

public class Program
{
    static void Main(params string[] args)
    {
        if (!args.Any())
        {
            Console.Write("Press s = server or c = client...");
            ConsoleKeyInfo key = Console.ReadKey();
            args = new string[] { key.KeyChar.ToString() };
        }

        var command = args[0];

        if (command == "s")
            new Receiver();

        if (command == "c")
            new Transmitter();
    }
}