using Godot;

namespace Proximity.Player;

public partial class CameraPivot : Node3D
{
    [Export] public CameraSpringArm3D SpringArm { get; set; }
    [Export] public float PitchSensitivity { get; set; }
    [Export] public float YawSensitivity { get; set; }

    public override void _Ready()
    {
        if (!IsMultiplayerAuthority())
        {
            QueueFree();
            return;
        }

        if (GetWindow().HasFocus())
        {
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }

        SpringArm.Camera.MakeCurrent();
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventMouseMotion mouseMotionEvent) return;

        Rotation = new Vector3(
            Mathf.Clamp(Rotation.X - mouseMotionEvent.Relative.Y * YawSensitivity, -1.57f, 1.57f),
            Rotation.Y - mouseMotionEvent.Relative.X * PitchSensitivity,
            Rotation.Z);
    }

    public override void _Notification(int what)
    {
        Input.MouseMode = (long) what switch
        {
            NotificationWMWindowFocusIn => Input.MouseModeEnum.Captured,
            NotificationWMWindowFocusOut => Input.MouseModeEnum.Visible,
            _ => Input.MouseMode
        };
    }
}
