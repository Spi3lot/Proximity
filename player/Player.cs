using System;

using Godot;

using Proximity.Common;

namespace Proximity.Player;

public partial class Player : RigidBody3D
{
    private AudioEffectCapture _capture;
    private float[] _capturedSamples;

    [Export] public CameraPivot Pivot { get; set; }
    [Export] public RayCast3D FloorRayCast { get; set; }
    [Export] public AudioStreamPlayer3D Speakers { get; set; }
    [Export] public AudioStreamPlayer Microphone { get; set; }
    [Export] public int MaxSamplesPerPacket { get; set; } = 128;
    [Export(hintString: "suffix:Ns")] public float JumpImpulse { get; set; } = 10;
    [Export(hintString: "suffix:Nm")] public float WalkTorque { get; set; } = 500;
    [Export(hintString: "suffix:Nm")] public float SprintTorque { get; set; } = 1000;

    public float CurrentlyDesiredSpeed() => Mathf.Lerp(WalkTorque, SprintTorque, Input.GetActionStrength("sprint"));

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _Ready()
    {
        if (IsMultiplayerAuthority())
        {
            Speakers.QueueFree();
            Microphone.Play();
            _capture = (AudioEffectCapture) AudioServer.GetBusEffect(AudioServer.GetBusIndex("Capture"), 0);
            _capturedSamples = new float[MaxSamplesPerPacket];
            AudioDebugger.Instance.MaxSamplesPerPacket = MaxSamplesPerPacket;
        }
        else
        {
            Speakers.Play();
            Microphone.QueueFree();
            FloorRayCast.QueueFree();
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

        var localXzDirection = Input.GetVector("move_left", "move_right", "move_forward", "move_backward");
        if (localXzDirection.IsZeroApprox()) return;

        float inputStrength = localXzDirection.Length();
        var globalXyzDirection = Pivot.Basis * new Vector3(localXzDirection.X, 0, localXzDirection.Y);
        var globalXzDirection = new Vector3(globalXyzDirection.X, 0, globalXyzDirection.Z);
        var torqueAxis = Vector3.Up.Cross(globalXzDirection.Normalized());
        ApplyTorque(CurrentlyDesiredSpeed() * (float) delta * inputStrength * torqueAxis);
    }

    public override void _UnhandledKeyInput(InputEvent @event)
    {
        if (!IsMultiplayerAuthority()) return;

        if (@event.IsActionPressed("jump") && FloorRayCast.IsColliding())
        {
            ApplyCentralImpulse(new Vector3(0, JumpImpulse, 0));
            GetViewport().SetInputAsHandled();
        }
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
