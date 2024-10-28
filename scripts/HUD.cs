using Godot;

public enum HUDLabels
{
    Blues,
    Reds,
    Cost,
    Iterations
}

public partial class HUD : CanvasLayer
{
    [Signal]
    public delegate void ChangeMapSizeEventHandler(int size);

    [Signal]
    public delegate void ChangeSeedEventHandler(int seed);

    [Signal]
    public delegate void InitExampleMapEventHandler();

    [Signal]
    public delegate void InitRandomMapEventHandler();

    [Signal]
    public delegate void SolveQ2EventHandler();

    [Signal]
    public delegate void SolveQ3EventHandler();

    [Signal]
    public delegate void ToggleAnimationEventHandler(bool animation);

    [Export] public AnimationPlayer NoPathPlayer;

    public void InitExampleMapHandler()
    {
        EmitSignal(SignalName.InitExampleMap);
    }

    public void InitRandomMapHandler()
    {
        EmitSignal(SignalName.InitRandomMap);
    }

    public void ChangeMapSizeHandler(float size)
    {
        EmitSignal(SignalName.ChangeMapSize, (int)size);
    }

    public void ChangeSeedHandler(float seed)
    {
        EmitSignal(SignalName.ChangeSeed, (int)seed);
    }

    public void ToggleAnimationHandler(bool animation)
    {
        EmitSignal(SignalName.ToggleAnimation, animation);
    }

    public void SolveQ2Handler()
    {
        EmitSignal(SignalName.SolveQ2);
    }

    public void SolveQ3Handler()
    {
        EmitSignal(SignalName.SolveQ3);
    }

    public void SetMapSize(int size)
    {
        GD.Print("Setting map size to " + size);
        var sizeSpinBox = FindChild("SizeSpinBox") as SpinBox;
        sizeSpinBox.Value = size;
    }

    public void SetLabel(string labelName, string text)
    {
        GD.Print("Setting label: " + labelName + " to " + text);
        var labelNode = FindChild(labelName) as Label;
        labelNode.Text = labelName + ": " + text;
    }

    public void PlayNoPathAnimation()
    {
        NoPathPlayer.Play("fade_out");
    }
}