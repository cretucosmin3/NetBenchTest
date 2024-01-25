
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace NetBenchTest.Networking;

public interface IManagedClient
{
    public void Start();
    public void Stop();
    public void Send(byte[] bytes);
}

public class ManagedClient(TcpClient tcp) : IManagedClient
{
    public TcpClient Tcp { get; } = tcp;

    public bool Connected { get; private set; } = false;
    public int NetworkStateTicks { get; set; } = 5;

    public Action<ManagedClient, byte[]> OnPacketReceived;
    public Action<ManagedClient> OnConnectionEnded;

    private Stopwatch HeartbeatTimer = new Stopwatch();
    private Stopwatch ClientHeartbeatTimer = new Stopwatch();

    private readonly NetworkStream m_Stream = tcp.GetStream();
    private readonly byte[] m_SendBuffer = new byte[ProjectSettings.MaxBufferSize];
    private readonly byte[] m_ReceiveBuffer = new byte[ProjectSettings.MaxBufferSize];

    private TcpState GetState(TcpClient tcpClient)
    {
        var connectionInfo = IPGlobalProperties.GetIPGlobalProperties()
          .GetActiveTcpConnections()
          .SingleOrDefault(x => x.LocalEndPoint.Equals(tcpClient.Client.LocalEndPoint));

        return connectionInfo != null ? connectionInfo.State : TcpState.Unknown;
    }

    public void Start()
    {
        Connected = true;

        Task.Run(async () =>
        {
            try
            {
                Send([1]);
                HeartbeatTimer.Restart();
                ClientHeartbeatTimer.Restart();

                while (Connected)
                {
                    int byteCount = m_Stream.Read(m_ReceiveBuffer, 0, m_ReceiveBuffer.Length);

                    if (byteCount > 1)
                        CompleteRead(byteCount);

                    if (byteCount == 1)
                    {
                        ReceivedHeartbeat();
                    }

                    // if (ClientHeartbeatTimer.ElapsedMilliseconds > 4000)
                    //     Connected = false;

                    // if (HeartbeatTimer.ElapsedMilliseconds > 1500)
                    // {
                    //     HeartbeatTimer.Restart();
                    //     Send([1]);
                    // }

                    await Task.Delay(NetworkStateTicks);
                }

                Console.WriteLine($"Connection closed with state: {GetState(Tcp)}");
            }
            catch (IOException e)
            {
                Console.WriteLine($"Managed client IO error: {e.Message}");
                Tcp.Close();
            }
            finally
            {
                Tcp.Close();
                OnConnectionEnded?.Invoke(this);
            }
        });
    }

    public void Stop()
    {
        Send([0]);
        Tcp?.Close();
        Connected = false;
    }

    public void Send(byte[] bytes)
    {
        if (!Connected || !Tcp.Connected || !m_Stream.CanWrite) return;

        try
        {
            Array.Copy(bytes, m_SendBuffer, bytes.Length);
            m_Stream.BeginWrite(m_SendBuffer, 0, bytes.Length, null, null);
        }
        catch(Exception ex)
        {
            Console.WriteLine($"Error when sending package. {ex.Message}");
            Connected = false;
        }
    }

    private void ReceivedHeartbeat()
    {
        try
        {
            ClientHeartbeatTimer.Restart();

            var received = new byte[1];
            Array.Copy(m_ReceiveBuffer, received, 1);

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

    private void CompleteRead(int bytesReceived)
    {
        try
        {
            var received = new byte[bytesReceived];

            Array.Copy(m_ReceiveBuffer, received, bytesReceived);

            OnPacketReceived?.Invoke(this, received);
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
}