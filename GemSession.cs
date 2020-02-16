using System;
using System.Net.Sockets;

// TODO: Switch to using IO.Pipelines
// http://www.rickyterrell.com/?p=154
// https://devblogs.microsoft.com/dotnet/system-io-pipelines-high-performance-io-in-net/
// https://github.com/davidfowl/TcpEcho
// https://github.com/davidfowl/DocsStaging
// https://github.com/gfoidl/TcpEcho/tree/generic-host/src
// https://github.com/lukethenuke/TcpEcho
// https://github.com/StephenClearyExamples/TcpEcho
// https://github.com/msbasanth/Pipelines.TcpEcho.Protobuf
// https://github.com/aspnet/KestrelHttpServer/blob/master/src/Kestrel.Transport.Sockets/SocketTransport.cs
// https://github.com/aspnet/AspNetCore/tree/master/src/Servers/Kestrel
// https://www.nuget.org/packages/System.IO.Pipelines/

namespace GEMEchoServer
{
    internal sealed class GemSession : IDisposable
    {
        public GemSession(GemServer server)
        { 
            Server = server;
            ReceiveEventArgs = new SocketAsyncEventArgs();
            ReceiveEventArgs.Completed += OnAsyncCompleted;
            Id = Guid.NewGuid();
            Packet = new Bin48NetTime();
            ReceiveEventArgs.SetBuffer(new byte[BufferSize]);
            Lock = new object();
        }
        
        ~GemSession()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (disposing)
            {
                Socket.Dispose();
                ReceiveEventArgs.Dispose();
            }

            Disposed = true;
        }

        internal void Connect(Socket socket)
        {
            // Attach to the connected socket
            lock (Lock)
            {
                Socket = socket;
                Peer = Socket.RemoteEndPoint.ToString();
                Console.WriteLine($"{Peer} : Connected");
            }

            // Start reading data
            Receive();
        }

        public void Close()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
            Socket.Dispose();
        }

        public void Disconnect()
        {
            Close();

            lock (Lock)
            { 
                Console.WriteLine($"{Peer} : Disconnected");
                
                // Did the client disconnect before completing a packet
                if (!Packet.IsEmpty() && !Packet.IsComplete())
                {
                    HandleBadPacket();
                }
            }

            // Disconnect from the server
            Server.Disconnect(this);
        }

        private void Receive()
        {
            if (!Socket.ReceiveAsync(ReceiveEventArgs))
                ProcessReceive(ReceiveEventArgs);
        }

        private void OnReceived(Memory<byte> buffer)
        {
            lock (Lock)
            {
                Console.WriteLine($"{Peer} : Received : {buffer.Length}");

                // Reset error on receiving data
                BadPacketState = false;

                // Append data to packet
                if (!Packet.Append(buffer))
                {
                    HandleBadPacket();
                    return;
                }

                // Do we have all the packet data
                if (!Packet.IsComplete())
                    return;
                
                // Unpack the data
                if (!Packet.Unpack())
                {
                    HandleBadPacket();
                    return;
                }

                // Got a packet
                PacketCount(1, 0);
                Server.ProcessPacket(Packet);
                Packet.Reset();
            }
        }

        private void HandleBadPacket()
        {
            lock (Lock)
            {
                // Avoid multiple failure reports on the same error
                if (BadPacketState)
                    return;

                // Error state
                BadPacketState = true;

                // Something went wrong
                Console.WriteLine($"{Peer} : Bad Packet : {Packet}");
                PacketCount(0, 1);

                // Reset packet
                Packet.Reset();
            }
        }

        private void PacketCount(int good, int bad)
        {
            lock (Lock)
            {
                GoodPackets += good;
                BadPackets += bad;
                Console.WriteLine($"{Peer} : Good Packets : {GoodPackets}, Bad Packets : {BadPackets}");
                Server.PacketCount(good, bad);
            }
        }

        private void ProcessReceive(SocketAsyncEventArgs eventArgs)
        {
            // Handle received data
            if (eventArgs.BytesTransferred > 0)
            {
                OnReceived(eventArgs.MemoryBuffer.Slice(0, eventArgs.BytesTransferred));
            }

            if (eventArgs.SocketError == SocketError.Success)
            {
                if (eventArgs.BytesTransferred > 0)
                    // Read more data
                    Receive();
                else
                    // No data and no error is a disconnect
                    Disconnect();
            }
            else
            {
                Console.WriteLine($"{Peer} : Receive error : {eventArgs.SocketError}");
                Disconnect();
            }
        }

        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs eventArgs)
        {
            if (eventArgs.LastOperation == SocketAsyncOperation.Receive)
                    ProcessReceive(eventArgs);
        }

        private readonly SocketAsyncEventArgs ReceiveEventArgs;
        private Socket Socket;
        private readonly GemServer Server;
        internal Guid Id;
        private string Peer;
        private readonly Bin48NetTime Packet;
        private int GoodPackets;
        private int BadPackets;
        private readonly object Lock;
        private bool BadPacketState;
        private bool Disposed;

        private const int BufferSize = 2048;
    }
}