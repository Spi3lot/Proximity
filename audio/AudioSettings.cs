using Godot;

namespace Proximity.Audio;

public partial class AudioSettings : Node
{
    public AudioSettings()
    {
        Instance = this;
    }

    public static AudioSettings Instance { get; private set; }

    [Export] public float SoundMetersPerSecond { get; private set; } = 343.0f;
    [Export] public float JitterBufferSeconds { get; private set; } = 0.2f;
    [Export] public int MaxSamplesPerPacket { get; private set; } = 128;
}
