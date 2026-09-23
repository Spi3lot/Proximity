using System;

using Godot;

using Proximity.Common;

namespace Proximity.Player;

public partial class Player : CharacterBody2D
{
    private AudioEffectCapture _capture;
    private float[] _capturedSamples;

    [Export] public AudioListener2D Listener { get; set; }
    [Export] public AudioStreamPlayer2D Speakers { get; set; }
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
            Listener.MakeCurrent();
            Speakers.QueueFree();
            Microphone.Play();
            _capture = (AudioEffectCapture) AudioServer.GetBusEffect(AudioServer.GetBusIndex("Capture"), 0);
            _capturedSamples = new float[MaxSamplesPerPacket];
            AudioDebugger.Instance.MaxSamplesPerPacket = MaxSamplesPerPacket;
        }
        else
        {
            Listener.ClearCurrent();
            Speakers.Play();
            Microphone.QueueFree();
        }
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        Velocity = Speed * Input.GetVector("ui_left", "ui_right", "ui_up", "ui_down");
        MoveAndSlide();

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
