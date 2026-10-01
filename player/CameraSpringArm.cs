using Godot;

namespace Proximity.Player;

public partial class CameraSpringArm : SpringArm3D
{
	[Export] public Camera3D Camera { get; set; }
	[Export] public float MinSpringLength { get; set; }
	[Export] public float MaxSpringLength { get; set; }

	public override void _UnhandledInput(InputEvent @event)
	{
		if (@event is not InputEventMouseButton mouseButtonEvent) return;

		switch (mouseButtonEvent.ButtonIndex)
		{
			case MouseButton.WheelUp:
				SpringLength = Mathf.Max(MinSpringLength, SpringLength - mouseButtonEvent.Factor);
				GetViewport().SetInputAsHandled();
				break;
			case MouseButton.WheelDown:
				SpringLength = Mathf.Min(MaxSpringLength, SpringLength + mouseButtonEvent.Factor);
				GetViewport().SetInputAsHandled();
				break;
		}
	}
}
