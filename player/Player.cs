using Godot;

namespace Proximity.Player;

public partial class Player : RigidBody3D
{
    [Export] public CameraPivot Pivot { get; set; }
    [Export] public RayCast3D FloorRayCast { get; set; }
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
        if (!IsMultiplayerAuthority())
        {
            FloorRayCast.QueueFree();
            Freeze = true;
            GetNode<CollisionShape3D>("CollisionShape3D").Disabled = true;
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
}
