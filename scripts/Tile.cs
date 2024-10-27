using Godot;

[Tool]
public partial class Tile : Node2D
{
    [Export] public Color BoarderColor = Colors.Black;
    [Export] public float BoarderWidth = 2.0f;
    [Export] public Color TileColor = Colors.LightGray;
    [Export] public Vector2 TileSize = new(10.0f, 10.0f);

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ZIndex = -1;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        QueueRedraw();
    }


    public override void _Draw()
    {
        var rect = new Rect2(-TileSize.X / 2, -TileSize.Y / 2, TileSize.X, TileSize.Y);
        DrawRect(rect, TileColor);
        DrawRect(rect, BoarderColor, false, BoarderWidth);
    }
}