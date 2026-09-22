using System;
using System.Diagnostics;

using Godot;
using Godot.Collections;

namespace Proximity;

[Tool]
[Icon("res://addons/at-icons/node/wind.svg")]
public partial class RealTimer : Node
{
    private long _lastFrameTicks;
    private long _lastTimeoutTicks;
    private long _accumulatedTicks;

    public RealTimer() => Recalculate();

    public event Action<TimeSpan> Timeout;

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

    [Export] public double PacketsPerSecond { get; private set; }

    [Export] public double WaitTime
    {
        get => _waitTimeSpan.TotalSeconds;
        private set => _waitTimeSpan = TimeSpan.FromSeconds(value);
    }

    private TimeSpan _waitTimeSpan;

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

    public override void _Process(double _)
    {
        if (Engine.IsEditorHint() || Paused)
        {
            _lastFrameTicks = 0;
            return;
        }

        long currentTimestamp = Stopwatch.GetTimestamp();

        if (_lastFrameTicks == 0)
        {
            _lastFrameTicks = currentTimestamp;
            _lastTimeoutTicks = currentTimestamp;
            return;
        }

        long elapsedTicks = currentTimestamp - _lastFrameTicks;
        _lastFrameTicks = currentTimestamp;
        _accumulatedTicks += elapsedTicks;

        while (_accumulatedTicks >= _waitTimeSpan.Ticks)
        {
            long ticksSinceLastTimeout = currentTimestamp - _lastTimeoutTicks;
            Timeout?.Invoke(new TimeSpan(ticksSinceLastTimeout));
            _lastTimeoutTicks = currentTimestamp;
            _accumulatedTicks -= _waitTimeSpan.Ticks;
        }
    }
}
