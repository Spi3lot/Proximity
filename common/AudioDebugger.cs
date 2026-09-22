using Godot;

namespace Proximity.Common;

public partial class AudioDebugger : CanvasLayer
{
    private const int MaxHistory = 300;

    private readonly float[] _egressHistory = new float[MaxHistory];
    private readonly float[] _ingressHistory = new float[MaxHistory];
    private readonly float[] _discardedHistory = new float[MaxHistory];

    private RichTextLabel _statsLabel;
    private Control _egressGraph, _ingressGraph, _discardedGraph;
    private int _egressCount, _ingressCount, _discardedCount;
    private int _lastEgressPps, _lastIngressPps, _lastDiscardedPps;
    private int _headIndex;
    private double _ppsTimer;

    public static AudioDebugger Instance { get; private set; }
    public int MaxSamplesPerPacket { get; set; }
    public int PlaybackSkips { get; set; }

    public override void _EnterTree()
    {
        Instance = this;
        BuildUi();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.Escape })
        {
            Visible = !Visible;
        }
    }

    public override void _Process(double delta)
    {
        if (!Visible) return;

        _ppsTimer += delta;

        if (_ppsTimer >= 1.0)
        {
            _lastEgressPps = _egressCount;
            _lastIngressPps = _ingressCount;
            _lastDiscardedPps = _discardedCount;
            _egressCount = _ingressCount = _discardedCount = 0;
            _ppsTimer -= 1.0;
        }

        UpdateStatsText();
        _egressGraph.QueueRedraw();
        _ingressGraph.QueueRedraw();
        _discardedGraph.QueueRedraw();
    }

    public void LogEgress(int sampleSize)
    {
        _egressHistory[_headIndex] = sampleSize;
        _egressCount++;
        AdvanceHead();
    }

    public void LogIngress(int sampleSize)
    {
        _ingressHistory[_headIndex] = sampleSize;
        _ingressCount++;
    }

    public void LogDiscarded(int sampleSize)
    {
        _discardedHistory[_headIndex] = sampleSize;
        _discardedCount++;
    }

    private void AdvanceHead()
    {
        _headIndex = (_headIndex + 1) % MaxHistory;
    }

    private void UpdateStatsText()
    {
        _statsLabel.Text =
            $"[color=green][b]Egress:[/b][/color] {_lastEgressPps} packets / s\n" +
            $"[color=#0088ff][b]Ingress (Success):[/b][/color] {_lastIngressPps} packets / s\n" +
            $"[color=red][b]Ingress (Discarded):[/b] {_lastDiscardedPps} packets / s[/color]\n" +
            $"[b]Playback Skips:[/b] {PlaybackSkips}";
    }

    private void BuildUi()
    {
        var panel = new PanelContainer();
        panel.SetAnchorsPreset(Control.LayoutPreset.TopLeft);
        panel.Position = new Vector2(20, 20);
        panel.Size = new Vector2(400, 450);
        AddChild(panel);

        var vbox = new VBoxContainer();
        panel.AddChild(vbox);

        _statsLabel = new RichTextLabel { BbcodeEnabled = true, CustomMinimumSize = new Vector2(0, 90), ScrollActive = false };
        vbox.AddChild(_statsLabel);

        _egressGraph = CreateGraph(vbox, _egressHistory, new Color(0, 1, 0, 0.8f));
        _ingressGraph = CreateGraph(vbox, _ingressHistory, new Color(0, 0.5f, 1, 0.8f));
        _discardedGraph = CreateGraph(vbox, _discardedHistory, new Color(1, 0, 0, 0.9f));
    }

    private ColorRect CreateGraph(Control parent, float[] history, Color color)
    {
        var graphCanvas = new ColorRect { Color = new Color(0, 0, 0, 0.3f), SizeFlagsVertical = Control.SizeFlags.ExpandFill };

        graphCanvas.Draw += () =>
        {
            var size = graphCanvas.Size;
            float xStep = size.X / MaxHistory;
            float yScale = size.Y / MaxSamplesPerPacket;

            for (int i = 0; i < MaxHistory - 1; i++)
            {
                int indexA = (_headIndex + i) % MaxHistory;
                int indexB = (_headIndex + i + 1) % MaxHistory;

                if (indexA == _headIndex || indexB == _headIndex) continue;

                if (history[indexA] > 0 || history[indexB] > 0)
                {
                    float x1 = i * xStep;
                    float x2 = (i + 1) * xStep;

                    graphCanvas.DrawLine(
                        new Vector2(x1, size.Y - history[indexA] * yScale),
                        new Vector2(x2, size.Y - history[indexB] * yScale),
                        color, 2f);
                }
            }
        };

        parent.AddChild(graphCanvas);
        return graphCanvas;
    }
}
