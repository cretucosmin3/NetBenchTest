using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using NetBenchTest.Common;
using NetBenchTest.Common.Utilities;
using NetBenchTest.Networking.Transport;

namespace NetBenchTest.Networking;

public class Client
{
    private TcpClient Tcp;
    private NetworkStream NetStream;

    private Stopwatch HeartbeatTimer = new Stopwatch();
    private Stopwatch ServerHeartbeatTimer = new Stopwatch();

    private readonly byte[] SendBuffer = new byte[ProjectSettings.MaxBufferSize];
    private readonly byte[] ReceiveBuffer = new byte[ProjectSettings.MaxBufferSize];

    public bool Connected { get; private set; } = false;

    public int NetworkStateTicks { get; set; } = 5;

    // Events
    public Action<Client, byte[]> OnPacketReceived;
    public Action<Client> OnConnectionEnded;

    public void Connect(string address, int port)
    {
        Tcp = new TcpClient();
        Tcp.Connect(address, port);
        NetStream = Tcp.GetStream();
        Connected = true;

        Task.Run(() => NetworkLoop());
    }

    public void Disconnect()
    {
        Tcp?.Close();
    }

    public void Send(byte[] packetData)
    {
        if (!Connected || !NetStream.CanWrite) return;

        try
        {
            Array.Copy(packetData, SendBuffer, packetData.Length);
            NetStream.BeginWrite(SendBuffer, 0, packetData.Length, EndWrite, null);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error when sending package. {ex.Message}");
            Connected = false;
        }
    }

    private void EndWrite(IAsyncResult result)
    {
        try
        {
            NetStream.EndWrite(result);
        }
        catch (Exception e)
        {
            Console.WriteLine($"Error while sending packet: {e.Message}");
            Connected = false;
        }
    }

    private void NetworkLoop()
    {
        try
        {
            Send([1]);
            HeartbeatTimer.Restart();
            ServerHeartbeatTimer.Restart();

            while (Connected)
            {
                int byteCount = NetStream.Read(ReceiveBuffer, 0, ReceiveBuffer.Length);

                if (byteCount > 1)
                {
                    CompleteRead(byteCount);
                }

                if (byteCount == 1)
                {
                    ReceivedHeartbeat();
                }

                // if (ServerHeartbeatTimer.ElapsedMilliseconds > 4000)
                //     Connected = false;

                // if (HeartbeatTimer.ElapsedMilliseconds > 1500)
                // {
                //     HeartbeatTimer.Restart();
                //     Send([1]);
                // }

                Thread.Sleep(NetworkStateTicks);
            }

            Console.WriteLine($"Connection closed with state: {GetState(Tcp)}");
        }
        catch (Exception e)
        {
            Console.WriteLine($"Network error: {e.Message}");
        }
        finally
        {
            Disconnect();
            OnConnectionEnded?.Invoke(this);
        }
    }

    private void ReceivedHeartbeat()
    {
        try
        {
            ServerHeartbeatTimer.Restart();

            var received = new byte[1];
            Array.Copy(ReceiveBuffer, received, 1);

            Console.WriteLine($"Received heartbeat: {received[0]}");

            if (received[0] == 0)
            {
                Connected = false;
            }
        }
        catch (Exception e)
        {
            if (e is IOException)
            {
                Console.WriteLine($"Managed client IO error: {e.Message}");
                Tcp.Close();
            }

            Console.WriteLine($"Error while reading the network stream: {e.Message} -> {e.StackTrace}");
        }
    }

    private void CompleteRead(int byteCount)
    {
        try
        {
            byte[] received = new byte[byteCount];
            Array.Copy(ReceiveBuffer, received, byteCount);

            OnPacketReceived?.Invoke(this, received);
        }
        catch (Exception e)
        {
            if (e is IOException)
            {
                Console.WriteLine($"IO error: {e.Message}");
                Tcp.Close();
            }

            Console.WriteLine($"Error while reading the network stream: {e.Message} -> {e.StackTrace}");
        }
    }

    private TcpState GetState(TcpClient tcpClient)
    {
        if (tcpClient == null || tcpClient.Client == null || !tcpClient.Client.Connected)
            return TcpState.Closed;

        var connectionInfo = IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpConnections()
            .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));

        return connectionInfo != null ? connectionInfo.State : TcpState.Unknown;
    }
}