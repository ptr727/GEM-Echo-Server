using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace GEMEchoServer
{
    internal sealed class GemServer : IDisposable
    {
        public GemServer(IPAddress address, int port)
        {
            Endpoint = new IPEndPoint(address, port);
            Socket = new Socket(Endpoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
            AcceptEventArgs = new SocketAsyncEventArgs();
            AcceptEventArgs.Completed += OnAsyncCompleted;
            Sessions = new ConcurrentDictionary<Guid, GemSession>();
            Gems = new ConcurrentDictionary<string, Gem>();
            Lock = new object();
        }

        ~GemServer()
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
                AcceptEventArgs.Dispose();
            }

            Disposed = true;
        }

        public void Start()
        {
            // Bind to the listening endpoint
            Socket.Bind(Endpoint);
            Endpoint = (IPEndPoint)Socket.LocalEndPoint;
            Peer = Endpoint.ToString();

            // Listen and accept connections
            Stopping = false;
            Socket.Listen(ListenBacklog);
            Console.WriteLine($"{Peer} : Listening for connections");
            StartAccept(AcceptEventArgs);
        }

        public void Stop()
        {
            // Close listening socket
            Stopping = true;
            Socket.Close();
            Socket.Dispose();

            // Close all client connections
            foreach (GemSession session in Sessions.Values)
            { 
                session.Close();
                session.Dispose();
            }
            Sessions.Clear();
        }

        private void StartAccept(SocketAsyncEventArgs eventArgs)
        {
            // Accept connections and process if synchronously completed
            eventArgs.AcceptSocket = null;
            if (!Socket.AcceptAsync(eventArgs))
                ProcessAccept(eventArgs);
        }

        private void ProcessAccept(SocketAsyncEventArgs eventArgs)
        {
            // Create a client session
            if (eventArgs.SocketError == SocketError.Success)
            {
                // Create a new session and connect to accepted socket
                GemSession session = new GemSession(this);
                Sessions.TryAdd(session.Id, session);
                session.Connect(eventArgs.AcceptSocket);
            }
            else
                Console.WriteLine($"{Peer} : Accept failure : {eventArgs.SocketError}");

            // Accept again unless stopping
            if (!Stopping)
                StartAccept(eventArgs);
        }

        internal void Disconnect(GemSession session)
        {
            // TODO : How to delete and GC the client after it disconnects?
            Sessions.TryRemove(session.Id, out GemSession _);
        }

        private void OnAsyncCompleted(object sender, SocketAsyncEventArgs eventArgs)
        {
            // Process accept completion
            if (eventArgs.LastOperation == SocketAsyncOperation.Accept)
                ProcessAccept(eventArgs);
        }

        public void PacketCount(int good, int bad)
        {
            lock (Lock)
            { 
                GoodPackets += good;
                BadPackets += bad;
                Console.WriteLine($"Total Good Packets : {GoodPackets}, Total Bad Packets : {BadPackets}");
            }
        }

        public void ProcessPacket(Bin48NetTime packet)
        {
            Console.WriteLine(packet.Gem.ToString());

            // Save the previous value keyed by GEM serial number
            // Make a deep copy so that the values are not modified as the packet is reused
            Gems.TryAdd(packet.Gem.SerialNumber, new Gem(packet.Gem));
        }

        private string Peer;
        private IPEndPoint Endpoint;
        private readonly SocketAsyncEventArgs AcceptEventArgs;
        private readonly Socket Socket;
        private volatile bool Stopping;
        private int GoodPackets;
        private int BadPackets;
        private readonly ConcurrentDictionary<Guid, GemSession> Sessions;
        private readonly ConcurrentDictionary<string, Gem> Gems;
        private readonly object Lock;
        private bool Disposed;

        private const int ListenBacklog = 16;
    }
}