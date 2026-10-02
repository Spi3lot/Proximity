using System;

using Godot;

using Proximity.Common;

namespace Proximity.Audio;

public partial class Voip : Node3D
{
    private AudioEffectCapture _capture;
    private float[] _capturedSamples;

    [Export] public AudioListener3D Listener { get; set; }
    [Export] public AudioStreamPlayer3D Speakers { get; set; }
    [Export] public AudioStreamPlayer Microphone { get; set; }
    [Export] public int MaxSamplesPerPacket { get; set; } = 128;

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
            Listener.QueueFree();
            Speakers.Play();
            Microphone.QueueFree();
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
