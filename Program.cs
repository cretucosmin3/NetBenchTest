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
            Console.WriteLine("No command given.");
            Console.Write("type 'server' or 'client':");
            args = [Console.ReadLine()];
        }

        var command = args[0];

        if (command == "s")
            new Receiver();

        if (command == "c")
            new Transmitter();
    }
}