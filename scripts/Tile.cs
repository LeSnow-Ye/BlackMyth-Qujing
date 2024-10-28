using Godot;

[Tool]
public partial class Tile : Node2D
{
    // event OnTileClicked
    public delegate void OnTileClicked(Vector2I tilePosition);

    [Export] public Area2D Area2D;
    [Export] public Color BoarderColor = Colors.Black;
    [Export] public float BoarderWidth = 2.0f;

    [Export] public CollisionShape2D CollisionShape2D;

    public int GValue = -1;
    public bool IsTransformed = false;
    public int MaxGValue = -1;
    public Vector2I TilePosition;

    [Export] public Vector2 TileSize = new(10.0f, 10.0f);

    public TileType TileType = TileType.Wall;
    public Color TileColor => GetColor(TileType, IsTransformed);
    public event OnTileClicked TileClicked;

    private static Color GetColor(TileType type, bool trans = false)
    {
        switch (type)
        {
            case TileType.Blue:
                return trans ? Colors.LightSkyBlue : Colors.DeepSkyBlue;
            case TileType.Red:
                return trans ? Colors.Pink : Colors.Tomato;
            default:
                return Colors.LightGray;
        }
    }

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
            GD.Print("Clicked on tile: " + TilePosition);
            TileClicked?.Invoke(TilePosition);
        }
    }

    public override void _Draw()
    {
        var rect = new Rect2(-TileSize.X / 2, -TileSize.Y / 2, TileSize.X, TileSize.Y);
        DrawRect(rect, TileColor);
        DrawRect(rect, BoarderColor, false, BoarderWidth);
    }
}