using System;
using Godot;
using Godot.Collections;

namespace Proximity;

[Tool]
public partial class RealTimer : Node
{
    private double _timeSinceLastTimeout;

    public RealTimer() => Recalculate();

    public event Action Timeout;

    [Export]
    public int SamplesPerPacket
    {
        get;
        set => UpdateAndRecalculate(value, out field);
    } = 512;

    [Export]
    public float SamplesPerSecond
    {
        get;
        set => UpdateAndRecalculate(value, out field);
    } = 44100;

    [Export]
    public float PacketIntervalMultiplier
    {
        get;
        set => UpdateAndRecalculate(value, out field);
    } = 1;

    [Export] public bool Paused { get; set; }

    [Export] public double PacketsPerSecond { get; set; }

    [Export] public double WaitTime { get; set; }

    public override void _ValidateProperty(Dictionary property)
    {
        var propertyName = property["name"].AsStringName();

        if (propertyName == PropertyName.PacketsPerSecond || propertyName == PropertyName.WaitTime)
        {
            var usage = (PropertyUsageFlags) property["usage"].AsInt64();
            property["usage"] = (long) (usage | PropertyUsageFlags.ReadOnly);
        }
    }

    private void UpdateAndRecalculate<T>(in T value, out T field)
    {
        field = value;
        Recalculate();
    }

    private void Recalculate()
    {
        float packetInterval = SamplesPerPacket / SamplesPerSecond;
        WaitTime = packetInterval * PacketIntervalMultiplier;
        PacketsPerSecond = 1 / WaitTime;
    }

    public override void _Process(double delta)
    {
        if (Engine.IsEditorHint() || Paused) return;

        _timeSinceLastTimeout += delta;

        while (_timeSinceLastTimeout >= WaitTime)
        {
            _timeSinceLastTimeout -= WaitTime;
            Timeout?.Invoke();
        }
    }
}
