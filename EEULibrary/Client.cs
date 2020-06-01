using System;
//using System.IO;
//using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using WebSocketSharp;

namespace EEUniverse.Library
{
    public class Client : IClient
    {
        public event EventHandler<Message> OnMessage;

        public event EventHandler<CloseEventArgs> OnDisconnect;

        public string MultiplayerHost { get; set; } = "wss://game.ee-universe.com";

        public WebSocket Socket;
        protected string _token;

        public bool Connected { get => (Socket != null) ? (Socket.ReadyState == WebSocketState.Open) : (false); }

        public Client() { }
        public Client(string token)
        {
            _token = token;
        }

        public virtual void Connect() => ConnectAsync().ConfigureAwait(false).GetAwaiter().GetResult();

        protected string _connectUrl = "/?a=";
        public virtual Task ConnectAsync()
        {
            Socket = new WebSocket($"{MultiplayerHost}{_connectUrl}{_token}");
            Socket.OnMessage += _socket_OnMessage;
            Socket.SslConfiguration.EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12;
            var tcs = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            EventHandler onOpen = null;
            onOpen = new EventHandler((object o, EventArgs e) => { tcs.SetResult(null); Socket.OnOpen -= onOpen; });
            Socket.OnOpen += onOpen;
            Socket.OnClose += (o, e) =>
            {
                if (!tcs.Task.IsCompleted) { tcs.SetException(new InvalidOperationException($"{e.Code} {e.Reason} {e.WasClean}")); }
                OnDisconnect?.Invoke(this, new CloseEventArgs() { Reason = e.Reason, WasClean = e.WasClean, WebSocketError = (CloseStatusCode)e.Code/*this is hopefully correct (it probably isn't)*/ });
            };
            Socket.OnError += (o, e) =>
            {
                if (!tcs.Task.IsCompleted) { tcs.SetException(new AggregateException(e.Message, e.Exception)); }
                OnError?.Invoke(this, new ErrorEventArgs(e.Exception, e.Message));
            };

            Socket.ConnectAsync();
            return tcs.Task;
        }
        public class ErrorEventArgs : EventArgs { internal ErrorEventArgs(Exception ex, string msg) { this.Exception = ex; this.Message = msg; } public Exception Exception { get; } public string Message { get; } }
        public event EventHandler<ErrorEventArgs> OnError;

        public virtual void Dispose() => Dispose(true);
        public void Dispose(bool cleanup = true)
        {
            if (cleanup) Cleanup();
            Socket?.Close(CloseStatusCode.NoStatus);
        }
        public virtual void DisposeAsync() => DisposeAsync(true);
        public void DisposeAsync(bool cleanup = true)
        {
            if (cleanup) Cleanup();
            Socket?.CloseAsync(CloseStatusCode.NoStatus);
        }
        private void Cleanup()
        {
            OnDisconnect = null;
            OnError = null;
            OnMessage = null;
        }

        public virtual void Send(ConnectionScope scope, MessageType type, params object[] data) => SendAsync(new Message(scope, type, data)).ConfigureAwait(false).GetAwaiter().GetResult();
        public virtual Task SendAsync(ConnectionScope scope, MessageType type, params object[] data) => SendAsync(new Message(scope, type, data));
        public virtual void Send(Message message) => SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
        public virtual Task SendAsync(Message message) => SendRawAsync(Serializer.Serialize(message));
        public virtual void SendRaw(ArraySegment<byte> bytes) => SendRawAsync(bytes.Array).ConfigureAwait(false).GetAwaiter().GetResult();
        public virtual Task<bool> SendRawAsync(byte[] bytes)
        {
            var tcs = new TaskCompletionSource<bool>();
            Socket.SendAsync(bytes, (status) => { tcs.SetResult(status); System.Diagnostics.Trace.Assert(status, "SendRawAsync"); });
            return tcs.Task;
        }

        /// <summary>
        /// Creates a connection with the lobby.
        /// </summary>
        public virtual IConnection CreateLobbyConnection() => new Connection(this, ConnectionScope.Lobby);

        /// <summary>
        /// Creates a connection with the specified world.
        /// </summary>
        /// <param name="worldId">The world id to connect to.</param>
        public virtual IConnection CreateWorldConnection(string worldId) => new Connection(this, ConnectionScope.World, worldId);

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
