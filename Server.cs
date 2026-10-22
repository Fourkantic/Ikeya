using LiteNetLib;
using LiteNetLib.Utils;
using System;

namespace Ikeya
{
    internal class Server : IDisposable
    {
        private readonly EventBasedNetListener listener;
        private readonly NetManager server;

        public bool IsRunning { get; private set; }

        public Server()
        {
            listener = new EventBasedNetListener();
            server = new NetManager(listener);

            listener.ConnectionRequestEvent += request =>
            {
                if (server.ConnectedPeersCount < 8)
                {
                    request.AcceptIfKey("MyGame-v1");
                }
                else
                {
                    request.Reject();
                }
            };

            listener.PeerConnectedEvent += peer =>
            {
                Console.WriteLine(
                    $"Client connected: {peer.Address}:{peer.Port}");

                var writer = new NetDataWriter();
                writer.Put("Welcome to the server!");

                peer.Send(
                    writer,
                    DeliveryMethod.ReliableOrdered);
            };

            listener.PeerDisconnectedEvent +=
                (peer, disconnectInfo) =>
                {
                    Console.WriteLine(
                        $"Client disconnected: {peer.Address}:{peer.Port}");
                };

            listener.NetworkReceiveEvent +=
                (peer, reader, channel, deliveryMethod) =>
                {
                    try
                    {
                        string message = reader.GetString();

                        Console.WriteLine(
                            $"Received from {peer.Address}:{peer.Port}: {message}");

                        var response = new NetDataWriter();
                        response.Put("Server received: " + message);

                        peer.Send(
                            response,
                            DeliveryMethod.ReliableOrdered);
                    }
                    finally
                    {
                        reader.Recycle();
                    }
                };
        }

        public void Start(int port = 9050)
        {
            if (IsRunning)
                return;

            if (!server.Start(port))
                throw new InvalidOperationException(
                    $"Could not start server on port {port}.");

            IsRunning = true;

            Console.WriteLine(
                $"Server running on UDP port {port}");
        }

        public void Update()
        {
            if (IsRunning)
                server.PollEvents();
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            server.Stop();
            IsRunning = false;

            Console.WriteLine("Server stopped.");
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
