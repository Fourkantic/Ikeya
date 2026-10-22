using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using LiteNetLib;
using LiteNetLib.Utils;

namespace Ikeya
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        // Server components
        private Server _server;

        // Client networking components (so the game can talk to the server)
        private EventBasedNetListener _clientListener;
        private NetManager _client;
        private NetPeer _serverPeer;

        // square / Sprite variables
        private Texture2D _squareTexture;
        private Vector2 _position;
        private float _speed = 250f; // Movement speed in pixels per second

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            // 1. Start the Server inside the game
            _server = new Server();
            _server.Start(9050);

            // 2. Initialize the Client to connect to the server
            _clientListener = new EventBasedNetListener();
            _client = new NetManager(_clientListener);
            _client.Start();

            // Setup client event listeners
            _clientListener.PeerConnectedEvent += peer =>
            {
                _serverPeer = peer;
                System.Diagnostics.Debug.WriteLine("Client successfully connected to the local server!");

                // Send an initial handshake message
                var writer = new NetDataWriter();
                writer.Put("Client Connected Successfully");
                peer.Send(writer, DeliveryMethod.ReliableOrdered);
            };

            _clientListener.NetworkReceiveEvent += (peer, reader, channel, deliveryMethod) =>
            {
                string message = reader.GetString();
                System.Diagnostics.Debug.WriteLine($"Message from server: {message}");
                reader.Recycle();
            };

            // Connect to our own server running on localhost port 9050 using the key defined in the server
            _client.Connect("127.0.0.1", 9050, "MyGame-v1");

            // Starting position of the square in the center of the screen
            _position = new Vector2(_graphics.PreferredBackBufferWidth / 2f, _graphics.PreferredBackBufferHeight / 2f);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _squareTexture = Content.Load<Texture2D>("Textures/square");
            
            _squareTexture = new Texture2D(GraphicsDevice, 1, 1);
            _squareTexture.SetData(new[] { Color.White });   
        }

        protected override void Update(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var kState = Keyboard.GetState();

            // Movement Logic (WASD or Arrow Keys)
            Vector2 movement = Vector2.Zero;
            if (kState.IsKeyDown(Keys.W) || kState.IsKeyDown(Keys.Up)) movement.Y -= 1;
            if (kState.IsKeyDown(Keys.S) || kState.IsKeyDown(Keys.Down)) movement.Y += 1;
            if (kState.IsKeyDown(Keys.A) || kState.IsKeyDown(Keys.Left)) movement.X -= 1;
            if (kState.IsKeyDown(Keys.D) || kState.IsKeyDown(Keys.Right)) movement.X += 1;

            if (movement != Vector2.Zero)
            {
                movement.Normalize();
                _position += movement * _speed * deltaTime;

                //// Send position updates to the server when pressing Spacebar
                //if (kState.IsKeyDown(Keys.Space) && _serverPeer != null)
                //{
                //    var writer = new NetDataWriter();
                //    writer.Put($"square moved to X:{(int)_position.X}, Y:{(int)_position.Y}");
                //    _serverPeer.Send(writer, DeliveryMethod.ReliableOrdered);
                //}
            }

            // Pump network events for both server and client
            _server.Update();
            _client.PollEvents();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _spriteBatch.Begin();
            // Draw the square (tinted Red if it's the fallback texture, or normal if it's a real image)
            _spriteBatch.Draw(_squareTexture, _position, null, Color.White, 0f, Vector2.Zero, new Vector2(32, 32), SpriteEffects.None, 0f);
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _client?.Stop();
                _server?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}