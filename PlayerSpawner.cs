using Godot;

namespace Proximity;

public partial class PlayerSpawner : MultiplayerSpawner
{
    [Export] public PackedScene PlayerScene { get; set; }

    public override void _Ready()
    {
        if (!Multiplayer.IsServer()) return;

        Multiplayer.PeerConnected += SpawnPlayer;
        SpawnPlayer(1);
    }

    public override void _ExitTree()
    {
        if (!Multiplayer.IsServer()) return;

        Multiplayer.PeerConnected -= SpawnPlayer;
    }

    public void SpawnPlayer(long id)
    {
        if (!Multiplayer.IsServer()) return;

        var player = PlayerScene.Instantiate();
        player.Name = id.ToString();
        GetNode(SpawnPath).CallDeferred(Node.MethodName.AddChild, player);
    }
}
