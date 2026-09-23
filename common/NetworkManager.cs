using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

namespace Proximity.Common;

public partial class NetworkManager : Node
{
    private readonly List<int> _upnpPortMappings = [];

    private Upnp _upnp;

    public static NetworkManager Instance { get; private set; }

    public ENetMultiplayerPeer Peer { get; private set; }

    public NetworkManager() => Instance = this;

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            _upnpPortMappings.ForEach(port => GD.Print($"Deleting mapping of port {port}: {(Upnp.UpnpResult) _upnp.DeletePortMapping(port)}"));
            _upnp?.Dispose();
            GetTree().Quit();
        }
    }

    public async Task<bool> CreateServer(ushort port, bool upnp)
    {
        Peer = new ENetMultiplayerPeer();
        var error = Peer.CreateServer(port);

        if (error != Error.Ok)
        {
            GD.PushError($"Failed to create server: {error}");
            return false;
        }

        if (upnp && (!await Task.Run(SetupUpnp) || !AddUpnpPortMapping(port)))
        {
            return false;
        }

        Multiplayer.MultiplayerPeer = Peer;
        return true;
    }

    public bool CreateClient(string address, ushort port)
    {
        Peer = new ENetMultiplayerPeer();
        var error = Peer.CreateClient(address, port);

        if (error != Error.Ok)
        {
            GD.PushError($"Failed to connect to server: {error}");
            return false;
        }

        Multiplayer.MultiplayerPeer = Peer;
        return true;
    }

    public bool SetupUpnp()
    {
        _upnp = new Upnp();

        GD.Print("Discovering UPnP devices...");
        var discoverResult = (Upnp.UpnpResult) _upnp.Discover();

        if (discoverResult != Upnp.UpnpResult.Success)
        {
            GD.PrintErr($"UPnP discovery failed: {discoverResult}");
            return false;
        }

        if (_upnp.GetGateway() is null || !_upnp.GetGateway().IsValidGateway())
        {
            GD.PrintErr("UPnP: No valid gateway found");
            return false;
        }

        GD.Print($"UPnP success! External IP: {_upnp.QueryExternalAddress()}");
        return true;
    }

    public bool AddUpnpPortMapping(int port)
    {
        var result = (Upnp.UpnpResult) _upnp.AddPortMapping(port, port, "Proximity");

        if (result != Upnp.UpnpResult.Success)
        {
            GD.PrintErr($"UPnP port mapping failed: {result}");
            return false;
        }

        GD.Print($"Mapped UPnP port {port} successfully!");
        _upnpPortMappings.Add(port);
        return true;
    }
}
