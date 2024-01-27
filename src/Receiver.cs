using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NetBenchTest.Common.Utilities;
using NetBenchTest.Networking;
using NetBenchTest.Networking.Transport;

namespace NetBenchTest;

public class Receiver
{
    public Receiver()
    {
        Console.WriteLine($"Starting Server...");
        PrintLocalIPv4();

        var server = new Server();

        float totalByteCount = 0;

        byte[] response = new byte[2];

        server.OnPacketReceived += (ManagedClient client, byte[] data) =>
        {
            totalByteCount += data.Length;

            Console.Clear();
            Console.WriteLine($"Total {BytesUtil.WithSizeSuffix(totalByteCount)} Mb/s");

            client.Send(response);
        };

        try
        {
            server.Start();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error server start: {ex.Message}");
        }

        Thread.Sleep(Timeout.Infinite);
    }

    public static void PrintLocalIPv4()
    {
        string hostName = Dns.GetHostName();
        var hostEntry = Dns.GetHostEntry(hostName);

        Console.WriteLine(hostName);

        foreach (var ip in hostEntry.AddressList)
        {
            // if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            // {
                Console.WriteLine("::: " + ip);
                break;
            // }
        }
    }
}