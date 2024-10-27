using System.Collections.Generic;
using System.Linq;
using Godot;

public partial class Map : Node2D
{
    [Signal]
    public delegate void SetLabelEventHandler(string labelName, string text);

    [Signal]
    public delegate void SetMapSizeEventHandler(int size);

    private int _seed = -1;
    private int _size = 8;
    private List<Vector2I> _solutionPath;
    private Vector2 _tileSize;
    public Game Game;

    [Export] public Vector2 RenderSize = new(500, 500);

    private void InitMap()
    {
        Reset(Game.Tiles);
        QueueRedraw();
        _solutionPath = null;

        var reds = Game.Tiles.Cast<TileType>().Count(t => t == TileType.Red);
        var blues = Game.Tiles.Cast<TileType>().Count(t => t == TileType.Blue);

        EmitSignal(SignalName.SetMapSize, _size);
        EmitSignal(SignalName.SetLabel, HUDLabels.Reds.ToString("F"), reds);
        EmitSignal(SignalName.SetLabel, HUDLabels.Blues.ToString("F"), blues);
        EmitSignal(SignalName.SetLabel, HUDLabels.Cost.ToString("F"), 0);
        EmitSignal(SignalName.SetLabel, HUDLabels.Iterations.ToString("F"), 0);
    }

    public void InitExampleMapHandler()
    {
        GD.Print("Init Example Map");
        Game = new Game();
        InitMap();
    }

    public void InitRandomMapHandler()
    {
        GD.Print("Init Random Map with size " + _size);
        Game = new Game(Game.RandomTiles(_size, _seed));
        InitMap();
    }

    public void ChangeMapSizeHandler(int size)
    {
        _size = size;
        GD.Print("Map size changed to " + size);
    }

    public void ChangeSeedHandler(int seed)
    {
        _seed = seed;
        GD.Print("Seed changed to " + seed);
    }

    public void SolveQ2Handler()
    {
        GD.Print("Solving Q2...");
        (var path, var cost, var iterations) = Game.FindPathQ2();
        GD.Print(string.Join(", ", path.ToArray()));
        GD.Print(path.Count);
        _solutionPath = path;
        QueueRedraw();
        EmitSignal(SignalName.SetLabel, HUDLabels.Cost.ToString("F"), cost);
        EmitSignal(SignalName.SetLabel, HUDLabels.Iterations.ToString("F"), iterations);
    }

    public void SolveQ3Handler()
    {
        GD.PushWarning("Not implemented yet.");
    }

    private static Color GetColor(TileType type)
    {
        switch (type)
        {
            case TileType.Blue:
                return Colors.DeepSkyBlue;
            case TileType.Red:
                return Colors.Tomato;
            default:
                return Colors.LightGray;
        }
    }

    /// <summary>
    ///     根据 <c>TileType</c> 数组重置 <c>Map</c>
    /// </summary>
    /// <param name="tiles"></param>
    public void Reset(TileType[,] tiles = null)
    {
        var size = new Vector2I(tiles.GetLength(0), tiles.GetLength(1));
        _tileSize = new Vector2(RenderSize.X / size.X, RenderSize.Y / size.Y);

        // Remove all children
        foreach (var child in GetChildren()) child.QueueFree();

        // Create new tiles
        for (var x = 0; x < size.X; x++)
        for (var y = 0; y < size.Y; y++)
        {
            var tile = new Tile();
            tile.Position = GetTilePosition(new Vector2I(x, y));
            tile.TileColor = GetColor(tiles[x, y]);
            tile.TileSize = _tileSize;
            tile.Name = $"Tile{x}-{y}";
            AddChild(tile);
        }
    }

    public void SetTile(Vector2I pos, TileType type)
    {
        var tile = GetNode<Tile>($"Tile{pos.X}-{pos.Y}");
        tile.TileColor = GetColor(type);
    }

    public Vector2 GetTilePosition(Vector2I pos)
    {
        return new Vector2(pos.X + 0.5f, pos.Y + 0.5f) * _tileSize - RenderSize / 2;
    }


    private void OnGameBoardChanged()
    {
        GD.Print("Game Board Changed");
        Reset(Game.Tiles);
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }

    public override void _Draw()
    {
        if (_solutionPath == null || _solutionPath.Count == 0) return;

        // Draw head and tail
        DrawCircle(GetTilePosition(_solutionPath[0]), _tileSize.X / 6.0f, Colors.Green);
        DrawCircle(GetTilePosition(_solutionPath[^1]), _tileSize.X / 6.0f, Colors.Green);

        // Draw path
        DrawPolyline(_solutionPath.Select(p => GetTilePosition(p)).ToArray(), Colors.Green, _tileSize.X / 12.0f);
    }
}