using System;
using System.Diagnostics;
using System.Threading;
using NetBenchTest.Common;
using NetBenchTest.Common.Utilities;
using NetBenchTest.Networking;
using NetBenchTest.Networking.Transport;

namespace NetBenchTest;

public class Transmitter
{
    private readonly string SERVER_IP = ProjectSettings.IPV4;
    private readonly int PORT = 5000;

    private bool CanSendNext = true;

    private long TotalSent = 0;
    private long TotalBytesSent = 0;

    private float Lag = 0;
    private float SentPerSecond = 0;
    private float BytesPerSecond = 0;

    private float SentPerSecondStatic = 0;
    private float Mbps = 0;
    private float AveragedLag = 0;

    private Stopwatch Timer = new();
    private Stopwatch LagTimer = new();

    public Transmitter()
    {
        Console.WriteLine($"Starting Client...");

        Console.WriteLine("Press any key to start!");
        Console.ReadKey();

        var client = new Client();
        client.Connect(SERVER_IP, PORT);

        client.OnConnectionEnded += (client) => Console.WriteLine("Connection ended");

        byte[] SamplePacket = new byte[ProjectSettings.MaxBufferSize - 1];

        client.OnPacketReceived += (Client client, byte[] data) =>
        {
            LagTimer.Stop();
            CanSendNext = true;

            BytesPerSecond += SamplePacket.Length;
            TotalBytesSent += SamplePacket.Length;

            CalculateStats();
            PrintStats();
        };

        Timer.Restart();
        while (client.Connected)
        {
            LagTimer.Restart();
            client.Send(SamplePacket);

            CanSendNext = false;

            while (!CanSendNext) { }
        }

        Console.ReadKey();
    }

    private void CalculateStats()
    {
        TotalSent++;
        SentPerSecond++;
        Lag += LagTimer.ElapsedMilliseconds;

        if (Timer.ElapsedMilliseconds >= 1000)
        {
            Mbps = BytesPerSecond;
            SentPerSecondStatic = SentPerSecond;
            AveragedLag = Lag / SentPerSecond;

            SentPerSecond = 0;
            BytesPerSecond = 0;
            Lag = 0;

            Timer.Restart();
        }
    }

    private void PrintStats()
    {
        Console.Clear();
        Console.WriteLine($"Total Requests Sent \t: {TotalSent}");
        Console.WriteLine($"Total MB \t\t: {BytesUtil.WithSizeSuffix(TotalBytesSent, 2)}");
        Console.WriteLine($"Sent Per Second \t: {SentPerSecondStatic}");
        Console.WriteLine($"Avg Lag Per Second \t: {AveragedLag:0.00} ms");
        Console.WriteLine($"Speed \t\t\t: {BytesUtil.WithSizeSuffix(Mbps)}/s ({BytesUtil.WithSizeSuffix(Mbps * 8)})");
    }
}