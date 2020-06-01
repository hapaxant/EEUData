using System;
using System.Threading.Tasks;

namespace EEUniverse.Library
{
    /// <summary>
    /// Provides a default implementation for interacting with the Everybody Edits Universe™ servers
    /// </summary>
    public class Connection : IConnection
    {
        public event EventHandler<Message> OnMessage;

        protected readonly IClient _client;
        protected readonly ConnectionScope _scope;

        public Connection(IClient client, ConnectionScope scope, string worldId = "")
        {
            _client = client;
            _scope = scope;

            client.OnMessage += (s, message) =>
            {
                if (message.Scope != scope)
                    return;

                OnMessage?.Invoke(this, message);
            };

            if (scope == ConnectionScope.World)
            {
                if (string.IsNullOrEmpty(worldId))
                    throw new ArgumentNullException($"{nameof(worldId)} should not be null or empty when the scope is world.");

                ((IConnection)this).Send(new Message(ConnectionScope.Lobby, 0, "world", worldId));
            }
        }

        public void Send(MessageType type, params object[] data) => SendAsync(new Message(_scope, type, data)).GetAwaiter().GetResult();
        public void Send(Message message) => SendAsync(message).ConfigureAwait(false).GetAwaiter().GetResult();
        public Task SendAsync(MessageType type, params object[] data) => _client.SendAsync(_scope, type, data);
        public Task SendAsync(Message message) => _client.SendAsync(message);
    }
}