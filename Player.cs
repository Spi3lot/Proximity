using System;

using Godot;

namespace Proximity;

public partial class Player : CharacterBody2D
{
    private AudioEffectCapture _capture;

    [Export] public AudioListener2D Listener { get; set; }
    [Export] public AudioStreamPlayer2D Speakers { get; set; }
    [Export] public AudioStreamPlayer Microphone { get; set; }
    [Export] public RealTimer AudioTransmissionTimer { get; set; }
    [Export] public float Speed { get; set; } = 1000;

    public override void _EnterTree()
    {
        SetMultiplayerAuthority(int.Parse(Name));
    }

    public override void _Ready()
    {
        var generator = (AudioStreamGenerator) Speakers.Stream;
        generator.MixRate = AudioServer.GetMixRate();

        if (IsMultiplayerAuthority())
        {
            Listener.MakeCurrent();
            Speakers.QueueFree();
            Microphone.Play();
            _capture = (AudioEffectCapture) AudioServer.GetBusEffect(AudioServer.GetBusIndex("Capture"), 0);

            float mixRate = AudioServer.GetMixRate();
            float[] floats = new float[Mathf.CeilToInt(AudioTransmissionTimer.SamplesPerPacket)];
            AudioTransmissionTimer.SamplesPerSecond = mixRate;

            AudioTransmissionTimer.Timeout += _ =>
            {
                int framesToSend = Mathf.Min(_capture.GetFramesAvailable(), floats.Length);
                var vectors = _capture.GetBuffer(framesToSend);

                for (int i = 0; i < vectors.Length; i++)
                {
                    floats[i] = (vectors[i].X + vectors[i].Y) / 2;
                }

                Rpc(MethodName.OutputVoice, floats[..framesToSend], mixRate);
            };
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
    }

    [Rpc(CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.UnreliableOrdered)]
    private void OutputVoice(float[] floats, float mixRate)
    {
        var playback = (AudioStreamGeneratorPlayback) Speakers.GetStreamPlayback();
        if (playback is null || playback.GetFramesAvailable() < floats.Length) return;

        Span<Vector2> vectors = stackalloc Vector2[floats.Length];

        for (int i = 0; i < floats.Length; i++)
        {
            vectors[i] = new Vector2(floats[i], floats[i]);
        }

        var generator = (AudioStreamGenerator) Speakers.Stream;
        generator.MixRate = mixRate;
        playback.PushBuffer(vectors);
    }
}
