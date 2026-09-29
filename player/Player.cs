using System;

using Godot;

using Proximity.Common;

namespace Proximity.Player;

public partial class Player : RigidBody3D
{
    private AudioEffectCapture _capture;
    private float[] _capturedSamples;

    [Export] public Camera3D Camera { get; set; }
    [Export] public AudioStreamPlayer3D Speakers { get; set; }
    [Export] public AudioStreamPlayer Microphone { get; set; }
    [Export] public int MaxSamplesPerPacket { get; set; } = 128;
    [Export] public float Speed { get; set; } = 1000;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _Ready()
    {
        if (IsMultiplayerAuthority())
        {
            Camera.MakeCurrent();
            Speakers.QueueFree();
            Microphone.Play();
            _capture = (AudioEffectCapture) AudioServer.GetBusEffect(AudioServer.GetBusIndex("Capture"), 0);
            _capturedSamples = new float[MaxSamplesPerPacket];
            AudioDebugger.Instance.MaxSamplesPerPacket = MaxSamplesPerPacket;
        }
        else
        {
            Camera.QueueFree();
            Speakers.Play();
            Microphone.QueueFree();
            Freeze = true;
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        while (_capture.GetFramesAvailable() > 0)
        {
            int samplesToSend = Mathf.Min(_capture.GetFramesAvailable(), MaxSamplesPerPacket);
            var capturedFrames = _capture.GetBuffer(samplesToSend);

            for (int i = 0; i < samplesToSend; i++)
            {
                _capturedSamples[i] = (capturedFrames[i].X + capturedFrames[i].Y) / 2;
            }

            Rpc(MethodName.OutputVoice, _capturedSamples[..samplesToSend], AudioServer.GetMixRate());
            AudioDebugger.Instance.LogEgress(samplesToSend);
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        var localXzDirection = Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        var globalXzDirection = Camera.Basis * new Vector3(localXzDirection.X, 0, localXzDirection.Y);
        var torqueAxis = Vector3.Up.Cross(globalXzDirection);
        ApplyTorque(Speed * (float) delta * torqueAxis);
    }

    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void OutputVoice(float[] samples, float mixRate)
    {
        var playback = (AudioStreamGeneratorPlayback) Speakers.GetStreamPlayback();
        AudioDebugger.Instance.LogSkips(playback.GetSkips());

        if (playback.GetFramesAvailable() < samples.Length)
        {
            AudioDebugger.Instance.LogDiscarded(samples.Length);
            return;
        }

        AudioDebugger.Instance.LogIngress(samples.Length);
        Span<Vector2> frames = stackalloc Vector2[samples.Length];

        for (int i = 0; i < samples.Length; i++)
        {
            frames[i] = new Vector2(samples[i], samples[i]);
        }

        var generator = (AudioStreamGenerator) Speakers.Stream;
        generator.MixRate = mixRate;
        playback.PushBuffer(frames);
    }
}
