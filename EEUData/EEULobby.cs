using EEUniverse.Library;

namespace EEUData
{
    public class EEULobby : Connection
    {
        public EEULobby(IClient client, ConnectionScope scope, string worldId = "") : base(client, scope, worldId) { }

        public LobbyWorld[] LoadRooms(string type = "world")
        {
            var m = _client.ReceiveMessage(new Message(_scope, MessageType.LoadRooms, type), x => x.Type == MessageType.LoadRooms);

            var wnum = m.Count / 3;
            LobbyWorld[] worlds = new LobbyWorld[wnum];
            for (int i = 0; i < wnum * 3; i += 3)
            {
                var mo = m.GetObject(i + 2);
                worlds[i / 3] = new LobbyWorld((string)m[i], (string)mo["n"], (int)mo["p"], (int)mo["v"], (int)m[i + 1]);
            }

            return worlds;
        }
        public LobbyStats LoadStats()
        {
            var m = _client.ReceiveMessage(new Message(_scope, MessageType.LoadStats), x => x.Type == MessageType.LoadStats);
            return new LobbyStats((int)m[0], (int)m[1]);
        }
    }
    public enum WorldVisibility
    {
        Public = 0,
        Unlisted = 1,
        Friends = 2,
        Private = 3,
    }
    public struct LobbyWorld
    {
        public LobbyWorld(string id, string name, int plays, int visibility, int online = 0) : this(id, name, plays, (WorldVisibility)visibility, online) { }
        public LobbyWorld(string id, string name, int plays, WorldVisibility visibility, int online = 0)
        {
            this.Id = id;
            this.Name = name;
            this.Plays = plays;
            this.Visibility = visibility;
            this.Online = online;
        }
        public string Id { get; }
        public string Name { get; }
        public int Plays { get; }
        public WorldVisibility Visibility { get; }
        public int Online { get; }

        public override string ToString() =>
$@"{nameof(Id)}:{Id}
{nameof(Name)}:{Name}
{nameof(Plays)}:{Plays}
{nameof(Visibility)}:{Visibility}" + ((Online <= 0) ? "" : $@"
{nameof(Online)}:{Online}");
    }
    public struct LobbyStats
    {
        public LobbyStats(int online, int worlds)
        {
            this.Online = online;
            this.Worlds = worlds;
        }

        public int Online { get; }
        public int Worlds { get; }

        public override string ToString() =>
$@"{Online} Online
{Worlds} Worlds";
    }
}
