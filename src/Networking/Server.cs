using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using NetBenchTest.Common;
using NetBenchTest.Common.Utilities;
using NetBenchTest.Networking.Transport;

namespace NetBenchTest.Networking;

public class Server
{
    const int PORT_NO = 5000;
    // const string SERVER_IP = ProjectSettings.IPV4;

    private List<ManagedClient> Clients = new List<ManagedClient>();

    public Action<ManagedClient, byte[]> OnPacketReceived;

    private TcpListener Listener;
    private bool isRunning;

    public void Start()
    {
        IPAddress localAddress = IPAddress.Parse(ProjectSettings.IPV4);
        Listener = new TcpListener(localAddress, PORT_NO);
        Listener.Start();
        isRunning = true;

        Console.WriteLine("Server is listening for clients...");

        Task.Run(() => ReceiveConnectionsLoop());
    }

    private void ReceiveConnectionsLoop()
    {
        while (isRunning)
        {
            try
            {
                TcpClient tcp = Listener.AcceptTcpClient(); // Synchronous call, but in a separate task

                ManagedClient mClient = new(tcp);

                mClient.OnPacketReceived += OnPacketReceived;
                mClient.OnConnectionEnded += ClientDisconnected;

                mClient.Start();

                Clients.Add(mClient);
                Console.WriteLine($"New client connected!");
            }
            catch (Exception e)
            {
                if (isRunning) // Only log errors if server is supposed to be running
                {
                    Console.WriteLine($"Error on connection received: {e.Message}");
                }
            }
        }
    }

    public void Stop()
    {
        isRunning = false;
        Listener.Stop();

        foreach (var client in Clients)
        {
            client.Stop();
        }

        Clients.Clear();
        Console.WriteLine("Server stopped.");
    }

    public void ClientDisconnected(ManagedClient client)
    {
        Clients.Remove(client);
        Console.WriteLine("Client disconnected.");
    }
}