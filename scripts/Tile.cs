using Godot;

[Tool]
public partial class Tile : Node2D
{
    // event OnTileClicked
    public delegate void OnTileClicked(string tileName);

    [Export] public Area2D Area2D;
    [Export] public Color BoarderColor = Colors.Black;
    [Export] public float BoarderWidth = 2.0f;

    [Export] public CollisionShape2D CollisionShape2D;
    [Export] public Color TileColor = Colors.LightGray;

    public string TileName = "Tile";
    [Export] public Vector2 TileSize = new(10.0f, 10.0f);
    public event OnTileClicked TileClicked;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        ZIndex = -1;
        QueueRedraw();
        CollisionShape2D.Shape = new RectangleShape2D { Size = TileSize };
        Area2D.InputEvent += OnArea2DInputEvent;
        TileClicked += _ => QueueRedraw();
    }

    private void OnArea2DInputEvent(Node viewport, InputEvent @event, long idx)
    {
        if (@event is InputEventMouseButton mouseButtonEvent && mouseButtonEvent.Pressed &&
            mouseButtonEvent.ButtonIndex == MouseButton.Left)
        {
            GD.Print("Clicked on tile: " + TileName);
            TileClicked?.Invoke(TileName);
        }
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }


    public override void _Draw()
    {
        var rect = new Rect2(-TileSize.X / 2, -TileSize.Y / 2, TileSize.X, TileSize.Y);
        DrawRect(rect, TileColor);
        DrawRect(rect, BoarderColor, false, BoarderWidth);
    }
}