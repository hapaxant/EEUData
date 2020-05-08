using System;
//using System.IO;
//using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace EEUniverse.Library
{
    /// <summary>
    /// The default implementation for a client connecting to Everybody Edits Universe™
    /// /// </summary>
    public class Client : IClient
    {
        ///// <summary>
        ///// An event that raises when the client receives a message.
        ///// </summary>

        public event EventHandler<Message> OnMessage;

        ///// <summary>
        ///// An event that raises when the connection to the server is lost.
        ///// </summary>
        public event EventHandler<CloseEventArgs> OnDisconnect;

        ///// <summary>
        ///// The server to connect to.
        ///// </summary>
        public string MultiplayerHost { get; set; } = "wss://game.ee-universe.com";

        /// <summary>
        /// The maximum amount of data the internal MemoryStream buffer can be before it forcilby shrinks itself.
        /// </summary>
        //public int MaxBuffer { get; set; } = 1024 * 50; // 51.2 kb
        public int MaxBuffer { get; set; } = 1024 * 200; // 204.8 kb?

        /// <summary>
        /// The minimum amount of data the client should allocate before deserializing a message.
        /// </summary>
        public int MinBuffer { get; set; } = 4096; // 4 kb

        //private Thread _messageReceiverThread;
        //private readonly ClientWebSocket _socket;
        /// <summary>
        /// The underlying socket of the client
        /// </summary>
        public WebSocket Socket;
        protected string _token;

        /// <summary>
        /// Initializes a new client.
        /// </summary>
        /// <param name="token">The JWT to connect with.</param>
        public Client() { }
        public Client(string token)
        {
            //_socket = new ClientWebSocket();
            _token = token;
        }

        ///// <summary>
        ///// Establishes a connection with the server and starts listening for messages.
        ///// </summary>
        public void Connect() => ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();
        //public void Connect() => ConnectAsync().GetAwaiter().GetResult();

        ///// <summary>
        ///// Establishes a connection with the server and starts listening for messages.
        ///// </summary>
        protected string _connectUrl = "/?a=";
        public Task ConnectAsync()
        {
            Socket = new WebSocket(/*new Uri(*/$"{MultiplayerHost}{_connectUrl}{_token}"/*)*/);
            Socket.OnMessage += _socket_OnMessage;
            Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            var tcs = new TaskCompletionSource<object>();
            Socket.OnOpen += (o, e) => tcs.SetResult(null);
            Socket.OnClose += (o, e) =>
            {
                OnDisconnect?.Invoke(o, new CloseEventArgs() { Reason = e.Reason, WasClean = e.WasClean, WebSocketError = (CloseStatusCode)e.Code/*this is hopefully correct (it probably isn't)*/ });
                if (!tcs.Task.IsCompleted) { var t = $"{e.Code} {e.Reason} {e.WasClean}"; tcs.SetException(new InvalidOperationException(t)); }
            };
            Socket.OnError += (o, e) => { if (!tcs.Task.IsCompleted) { /*var t = $"{e.Exception} {e.Message}";*/ tcs.SetException(new AggregateException(e.Message, e.Exception)); } OnError?.Invoke(this, new ErrorEventArgs(e.Exception, e.Message)); };

            Socket.ConnectAsync();
            return tcs.Task;
        }
        public class ErrorEventArgs : EventArgs { protected internal ErrorEventArgs(Exception ex, string msg) { this.Exception = ex; this.Message = msg; } public Exception Exception { get; } public string Message { get; } }
        public event EventHandler<ErrorEventArgs> OnError;

        public void Dispose()
        {
            Cleanup();
            Socket?.Close(CloseStatusCode.NoStatus);
        }
        public void DisposeAsync()
        {
            Cleanup();
            Socket?.CloseAsync(CloseStatusCode.NoStatus);

            // WebSocketException: The remote party closed the WebSocket connection without completing the close handshake.
            // ^ this is thrown when WebSocketCLoseStatus.NormalClosure is used.
            //await Socket.CloseAsync(WebSocketCloseStatus.Empty, statusDescription: null, cancellationToken: default).ConfigureAwait(false);
            //Socket.Dispose();
        }
        private void Cleanup()
        {
            OnDisconnect = null;
            OnError = null;
            OnMessage = null;
            //Socket.OnClose
            //Socket?.OnError = null;
            //Socket?.OnOpen = null;
            //Socket?.OnMessage = null;
        }

        ///// <summary>
        ///// Sends a message to the server.
        ///// </summary>
        ///// <param name="scope">The scope of the message.</param>
        ///// <param name="type">The type of the message.</param>
        ///// <param name="data">An array of data to be sent.</param>
        public void Send(ConnectionScope scope, MessageType type, params object[] data) => SendAsync(new Message(scope, type, data)).GetAwaiter().GetResult();

        ///// <summary>
        ///// Sends a message to the server as an asynchronous operation.
        ///// </summary>
        ///// <param name="scope">The scope of the message.</param>
        ///// <param name="type">The type of the message.</param>
        ///// <param name="data">An array of data to be sent.</param>
        public Task SendAsync(ConnectionScope scope, MessageType type, params object[] data) => SendAsync(new Message(scope, type, data));

        ///// <summary>
        ///// Sends a message to the server.
        ///// </summary>
        ///// <param name="message">The message to send.</param>
        public void Send(Message message) => SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();

        ///// <summary>
        ///// Sends a message to the server as an asynchronous operation.
        ///// </summary>
        ///// <param name="message">The message to send.</param>
        public Task SendAsync(Message message) => SendRawAsync(Serializer.Serialize(message));

        /// <summary>
        /// Sends a message to the server.<br />Use with caution.
        /// </summary>
        /// <param name="bytes">The buffer containing the message to be sent.</param>
        public void SendRaw(ArraySegment<byte> bytes) => SendRawAsync(bytes.Array).GetAwaiter().GetResult();

        /// <summary>
        /// Sends a message to the server as an asynchronous operation.<br />Use with caution.
        /// </summary>
        /// <param name="bytes">The buffer containing the message to be sent.</param>
        public Task<bool> SendRawAsync(byte[] bytes)
        {
            var tcs = new TaskCompletionSource<bool>();
            Socket.SendAsync(bytes, (status) => { System.Diagnostics.Trace.Assert(status, "SendRawAsync"); tcs.SetResult(status); });
            return tcs.Task;
        }
        //public Task SendRawAsync(ArraySegment<byte> bytes) => _socket.SendAsync(bytes, WebSocketMessageType.Binary, true, CancellationToken.None);
        //public Task SendRawAsync(byte[] bytes) => SendRawAsync(new ArraySegment<byte>(bytes));

        ///// <summary>
        ///// Creates a connection with the lobby.
        ///// </summary>
        public IConnection CreateLobbyConnection() => new Connection(this, ConnectionScope.Lobby);

        /// <summary>
        /// Creates a connection with the specified world.
        /// </summary>
        /// <param name="worldId">The world id to connect to.</param>
        public IConnection CreateWorldConnection(string worldId) => new Connection(this, ConnectionScope.World, worldId);

        private void _socket_OnMessage(object sender, MessageEventArgs e)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (e.Type == Opcode.Close)
#pragma warning restore CS0618 // Type or member is obsolete
                OnDisconnect?.Invoke(this, new CloseEventArgs
                {
                    WasClean = true,
                    WebSocketError = CloseStatusCode.Normal,
                    Reason = "Disconnected gracefully"
                });

            var message = Serializer.Deserialize(e.RawData);
            OnMessage?.Invoke(this, message);
        }
    }
}
