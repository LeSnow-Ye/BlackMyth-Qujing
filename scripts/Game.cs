using System.Collections.Generic;
using System.Linq;
using Godot;

public enum TileType
{
    Wall,
    Blue,
    Red
}

public class AStarNode
{
    private static int nextId;
    public readonly int Id;
    public int CostG;
    public int CostH;
    public AStarNode Parent;
    public Vector2I Position;

    public AStarNode(Vector2I pos, AStarNode parent, int g, int h)
    {
        Id = nextId++;
        Position = pos;
        Parent = parent;
        CostG = g;
        CostH = h;
    }

    public int CostF => CostG + CostH;
}

public class Game
{
    public Vector2I EndPos;

    // -------- Basic properties --------
    public Vector2I Size = new(8, 8);
    public Vector2I StartPos = new(0, 0);
    public TileType[,] Tiles;

    // public delegate void BoardChanged();
    // public event BoardChanged OnBoardChanged;

    public Game(TileType[,] tiles = null)
    {
        Tiles = tiles == null ? ExampleTiles() : tiles;
        Size = new Vector2I(Tiles.GetLength(0), Tiles.GetLength(1));
        EndPos = new Vector2I(Size.X - 1, Size.Y - 1);

        // OnBoardChanged?.Invoke();
    }


    /// <summary>
    ///     示例 Map
    /// </summary>
    /// <returns>
    ///     <c>TileType[,] Tiles</c>
    /// </returns>
    public static TileType[,] ExampleTiles()
    {
        var tiles = new int[8, 8]
        {
            { 2, 2, 0, 0, 0, 1, 0, 0 },
            { 0, 1, 2, 2, 0, 0, 0, 0 },
            { 0, 2, 1, 0, 0, 2, 0, 0 },
            { 0, 0, 1, 1, 2, 0, 1, 0 },
            { 0, 0, 2, 0, 2, 1, 0, 0 },
            { 0, 0, 1, 1, 1, 2, 2, 0 },
            { 0, 1, 0, 0, 2, 0, 1, 1 },
            { 0, 0, 0, 0, 1, 1, 2, 1 }
        };

        var result = new TileType[8, 8];
        for (var x = 0; x < 8; x++)
        for (var y = 0; y < 8; y++)
            result[x, y] = (TileType)tiles[y, x];

        return result;
    }

    /// <summary>
    ///     生成随机 Map。目前只支持正方形 Map。对角线密度最高，向两边递减。
    /// </summary>
    /// <param name="size">Map 大小</param>
    /// <param name="density">填充密度</param>
    /// <returns>
    ///     <c>TileType[,] Tiles</c>
    /// </returns>
    public static TileType[,] RandomTiles(int size, long seed = -1, float density = 1.7f)
    {
        if (seed >= 0) GD.Seed((ulong)seed);

        var result = new TileType[size, size];
        for (var x = 0; x < size; x++)
        for (var y = 0; y < size; y++)
        {
            var offset = Mathf.Abs(x - y);
            var p = offset == 0 ? 1 : density / (1 + offset);
            if (GD.Randf() > p)
                result[x, y] = TileType.Wall;
            else
                result[x, y] = (TileType)(GD.Randi() % 2 + 1);
        }

        return result;
    }

    public void SetTile(Vector2I pos, TileType type)
    {
        if (pos.X < 0 || pos.X >= Size.X || pos.Y < 0 || pos.Y >= Size.Y)
            return;

        Tiles[pos.X, pos.Y] = type;
    }

    // -------- Pathfinding --------

    private int Heuristic(Vector2I pos)
    {
        return Mathf.Abs(pos.X - EndPos.X) + Mathf.Abs(pos.Y - EndPos.Y);
    }

    private List<Vector2I> GetNeighbours(Vector2I pos)
    {
        var neighbours = new List<Vector2I>();
        var potentialNeighbours = new List<Vector2I>
        {
            new(pos.X + 1, pos.Y),
            new(pos.X - 1, pos.Y),
            new(pos.X, pos.Y + 1),
            new(pos.X, pos.Y - 1)
        };

        foreach (var neighbour in potentialNeighbours)
            if (neighbour.X >= 0 && neighbour.X < Size.X &&
                neighbour.Y >= 0 && neighbour.Y < Size.Y &&
                Tiles[neighbour.X, neighbour.Y] != TileType.Wall)
                neighbours.Add(neighbour);

        return neighbours;
    }

    private List<Vector2I> ReconstructPath(AStarNode node)
    {
        var path = new List<Vector2I>();
        while (node != null)
        {
            path.Add(node.Position);
            node = node.Parent;
        }

        path.Reverse();
        return path;
    }

    private int CalculateCost(TileType currentTile, TileType neighbourTile)
    {
        return currentTile == neighbourTile ? 0 : 1;
    }

    public (List<Vector2I> path, int cost, ulong iterations) FindPathQ2()
    {
        ulong iterations = 0;

        var openSet = new SortedSet<AStarNode>(Comparer<AStarNode>.Create((a, b) =>
        {
            var compare = a.CostF.CompareTo(b.CostF);
            if (compare == 0) compare = a.Id.CompareTo(b.Id);
            return compare;
        }));

        var startNode = new AStarNode(StartPos, null, 0, Heuristic(StartPos));
        openSet.Add(startNode);

        var cameFrom = new Dictionary<Vector2I, AStarNode>();
        var gScore = new Dictionary<Vector2I, int> { [StartPos] = 0 };

        while (openSet.Count > 0)
        {
            // GD.Print(string.Join(", ", openSet.Select(n => n.Position + " " + n.CostF).ToArray()));

            var currentNode = openSet.Min;
            openSet.Remove(currentNode);

            if (currentNode.Position == EndPos)
                return (ReconstructPath(currentNode), currentNode.CostG, iterations);

            foreach (var neighbour in GetNeighbours(currentNode.Position))
            {
                iterations++;
                var tentativeGScore = gScore[currentNode.Position] +
                                      CalculateCost(Tiles[currentNode.Position.X, currentNode.Position.Y],
                                          Tiles[neighbour.X, neighbour.Y]);
                // GD.Print("neighbour: " + neighbour + " tentativeGScore: " + tentativeGScore);

                if (!gScore.ContainsKey(neighbour) || tentativeGScore < gScore[neighbour])
                {
                    cameFrom[neighbour] = currentNode;
                    gScore[neighbour] = tentativeGScore;
                    var neighbourNode = new AStarNode(neighbour, currentNode, tentativeGScore, Heuristic(neighbour));

                    if (!openSet.Any(n => n.Position == neighbour))
                        openSet.Add(neighbourNode);
                }
            }
        }

        return (new List<Vector2I>(), -1, iterations); // No path found
    }
}