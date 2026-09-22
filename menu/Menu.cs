using Godot;

namespace Proximity;

public partial class Menu : Control
{
    [Export] public LineEdit AddressLineEdit { get; set; }
    [Export] public LineEdit PortLineEdit { get; set; }
    [Export] public Button UpnpButton { get; set; }

    private async void OnHostButtonPressed()
    {
        if (await NetworkManager.Instance.CreateServer(ushort.Parse(PortLineEdit.Text), UpnpButton.ButtonPressed))
        {
            ChangeSceneToGame();
        }
    }

    private void OnJoinButtonPressed()
    {
        if (NetworkManager.Instance.CreateClient(AddressLineEdit.Text, ushort.Parse(PortLineEdit.Text)))
        {
            ChangeSceneToGame();
        }
    }

    private void ChangeSceneToGame()
    {
        GetTree().CallDeferred(SceneTree.MethodName.ChangeSceneToFile, "res://game.tscn");
    }
}
