using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;

public partial class Map : Node2D
{
    [Signal]
    public delegate void NoPathFoundEventHandler();

    [Signal]
    public delegate void SetLabelEventHandler(string labelName, string text);

    [Signal]
    public delegate void SetMapSizeEventHandler(int size);

    private bool _animation;

    private int _seed = -1;
    private int _size = 8;
    private List<Vector2I> _solutionPath;
    private Tile[,] _tiles;
    private Vector2 _tileSize;
    [Export] public Gradient AnimationGradient;
    [Export] public float AnimationInterval = 0.075f;

    public Game Game = new();

    [Export] public Vector2 RenderSize = new(500, 500);
    [Export] public PackedScene TileScene;

    private void InitMap()
    {
        ResetTiles(Game.Tiles);
        QueueRedraw();
        _solutionPath = null;
        Game.GScores = new Dictionary<Vector2I, int>();

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
        Game.Init();
        InitMap();
    }

    public void InitRandomMapHandler()
    {
        GD.Print("Init Random Map with size " + _size);
        Game.Init(Game.RandomTiles(_size, _seed));
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

    public void ToggleAnimationHandler(bool animation)
    {
        _animation = animation;
    }


    private async Task Solve(bool canTransform = false)
    {
        if (_solutionPath != null && _solutionPath.Count > 0) InitMap();

        var (path, cost, iterations) = await Game.FindPath(canTransform, _animation ? AnimationInterval : 0);

        GD.Print(string.Join(", ", path.ToArray()));
        GD.Print(path.Count);
        _solutionPath = path;
        QueueRedraw();
        EmitSignal(SignalName.SetLabel, HUDLabels.Cost.ToString("F"), cost);
        EmitSignal(SignalName.SetLabel, HUDLabels.Iterations.ToString("F"), iterations);
        if (cost == -1)
            EmitSignal(SignalName.NoPathFound);
    }

    public async void SolveQ2Handler()
    {
        GD.Print("Solving Q2...");
        await Solve();
    }

    public async void SolveQ3Handler()
    {
        GD.Print("Solving Q3...");
        await Solve(true);
    }

    /// <summary>
    ///     根据 <c>TileType</c> 数组重置 <c>Map</c>
    /// </summary>
    /// <param name="tiles"></param>
    public void ResetTiles(TileType[,] tiles)
    {
        var size = new Vector2I(tiles.GetLength(0), tiles.GetLength(1));
        var currentSize = new Vector2I(_tiles?.GetLength(0) ?? 0, _tiles?.GetLength(1) ?? 0);
        if (size == currentSize)
        {
            for (var x = 0; x < size.X; x++)
            for (var y = 0; y < size.Y; y++)
            {
                _tiles[x, y].TileType = tiles[x, y];
                _tiles[x, y].IsTransformed = false;
                _tiles[x, y].QueueRedraw();
            }

            return;
        }

        _tileSize = new Vector2(RenderSize.X / size.X, RenderSize.Y / size.Y);
        _tiles = new Tile[size.X, size.Y];

        // Remove all children
        foreach (var child in GetChildren()) child.QueueFree();

        // Create new tiles
        for (var x = 0; x < size.X; x++)
        for (var y = 0; y < size.Y; y++)
        {
            var tile = TileScene.Instantiate() as Tile;
            tile.Position = GetTilePosition(new Vector2I(x, y));
            tile.TileType = tiles[x, y];
            tile.TileSize = _tileSize;
            tile.TilePosition = new Vector2I(x, y);
            tile.TileClicked += OnTileClicked;
            _tiles[x, y] = tile;
            AddChild(tile);
        }
    }

    private void OnTileClicked(Vector2I tilePosition)
    {
        var previousTile = Game.Tiles[tilePosition[0], tilePosition[1]];
        var newTile = (TileType)(((int)previousTile + 1) % 3);
        Game.Tiles[tilePosition[0], tilePosition[1]] = newTile;
        InitMap();
    }

    public Vector2 GetTilePosition(Vector2I pos)
    {
        return new Vector2(pos.X + 0.5f, pos.Y + 0.5f) * _tileSize - RenderSize / 2;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Game.OnGScoreUpdated += () => QueueRedraw();
        InitExampleMapHandler();
    }

    public override void _Draw()
    {
        // GScores
        if (_animation && Game.GScores != null && Game.GScores.Count > 0)
        {
            var maxGScore = Game.GScores.Max(g => g.Value);
            foreach (var (pos, gscore) in Game.GScores)
            {
                var tilePos = GetTilePosition(pos);
                var (width, height) = _tileSize / 3.0f;
                var rect = new Rect2(tilePos.X - width / 2, tilePos.Y - height / 2, width, height);
                DrawRect(rect, AnimationGradient.Sample(gscore / (float)maxGScore));
            }
        }

        // Start and End
        DrawString(ThemeDB.FallbackFont, GetTilePosition(Game.StartPos) + new Vector2(-0.7f, 0.3f) * _tileSize / 2.0f,
            "\ud83c\udfef", HorizontalAlignment.Center, fontSize: 32);
        DrawString(ThemeDB.FallbackFont, GetTilePosition(Game.EndPos) + new Vector2(-0.7f, 0.3f) * _tileSize / 2.0f,
            "\ud83d\uded5", HorizontalAlignment.Center, fontSize: 32);


        if (_solutionPath == null || _solutionPath.Count == 0) return;

        // Draw head and tail
        DrawCircle(GetTilePosition(_solutionPath[0]), _tileSize.X / 6.0f, Colors.Green);
        DrawCircle(GetTilePosition(_solutionPath[^1]), _tileSize.X / 6.0f, Colors.Green);

        // Draw path
        DrawPolyline(_solutionPath.Select(p => GetTilePosition(p)).ToArray(), Colors.Green, _tileSize.X / 12.0f);

        // Transformed tiles
        for (var index = 0; index < _solutionPath.Count; index++)
        {
            var pos = _solutionPath[index];
            if (Game.Tiles[pos.X, pos.Y] == TileType.Wall)
            {
                var prePos = _solutionPath[index - 1];
                _tiles[pos.X, pos.Y].TileType = Game.Tiles[prePos.X, prePos.Y];
                _tiles[pos.X, pos.Y].IsTransformed = true;
                _tiles[pos.X, pos.Y].QueueRedraw();
            }
        }
    }
}