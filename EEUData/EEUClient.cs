using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EEUniverse.Library;
using EEUData;
using System.Linq;
using System.Threading;

namespace EEUData
{
    public class EEUClient : Client
    {
        public EEUClient(string token)
        {
            _token = token;
        }

        private readonly ManualResetEventSlim _gotSelfInfo = new ManualResetEventSlim(false, 0);
        private const int _timeout = 5000;
        private SelfInfo _selfInfo;
        public SelfInfo SelfInfo { get { if (_gotSelfInfo.Wait(_timeout)) return _selfInfo; throw new TimeoutException("Server did not send SelfInfo yet"); } protected set => _selfInfo = value; }

        public new void Connect()
        {
            this.OnMessage += EEUClient_OnMessage;

            base.Connect();
        }

        public new Task ConnectAsync() => Task.Run(() => Connect());

        private void EEUClient_OnMessage(object sender, Message m)
        {
            if (m.Type == MessageType.SelfInfo)
            {
                this.OnMessage -= EEUClient_OnMessage;

                int i = 0;
                LobbyWorld[] worlds;
                int wnum;
                _selfInfo = new SelfInfo((string)m[i++], (int)m[i++], (int)m[i++], (int)m[i++], worlds = new LobbyWorld[wnum = (int)m[i++]]);
                for (int j = 0; j < wnum; j++)
                {
                    var mo = m.GetObject(i++);
                    worlds[j] = new LobbyWorld((string)mo["i"], (string)mo["n"], (int)mo["p"], (int)mo["v"]);
                }
                _gotSelfInfo.Set();
            }
        }

        public new EEUConnection CreateWorldConnection(string worldId) => new EEUConnection(this, ConnectionScope.World, worldId);
        public new EEULobby CreateLobbyConnection() => new EEULobby(this, ConnectionScope.Lobby);
    }
    public struct SelfInfo
    {
        public SelfInfo(string username, int energy, int stardust, int jewels, LobbyWorld[] ownedWorlds)
        {
            this.Username = username;
            this.Energy = energy;
            this.Stardust = stardust;
            this.Jewels = jewels;
            this.OwnedWorlds = ownedWorlds;
        }

        public string Username { get; }
        public int Energy { get; }
        public int Stardust { get; }
        public int Jewels { get; }
        public LobbyWorld[] OwnedWorlds { get; }

        public override string ToString() =>
$@"{nameof(Username)}:{Username}
{nameof(Energy)}:{Energy}
{nameof(Stardust)}:{Stardust}
{nameof(Jewels)}:{Jewels}" + String.Join("", OwnedWorlds.Select(x => Environment.NewLine + x.ToString()));
    }
    internal static class Exts
    {
        internal static Message ReceiveMessage(this IClient _client, Message send, Func<Message, bool> recv, int timeout = 5000, bool throwOnTimeout = true)
        {
            using (var mre = new ManualResetEventSlim(false, 0))
            {
                Message ret = null;
                void onmsg(object o, Message m)
                {
                    if (recv(m))
                    {
                        if (ret == null)
                        {
                            ret = m;
                            _client.OnMessage -= onmsg;
                            mre?.Set();
                        }
                    }
                }
                _client.OnMessage += onmsg;
                _client.Send(send);
                var flag = mre.Wait(timeout);
                _client.OnMessage -= onmsg;
                if (!flag)
                {
                    if (throwOnTimeout) throw new TimeoutException("server did not respond in time");
                    else return null;
                }
                else return ret;
            }
        }
    }
}
