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

    private int CostH(Vector2I pos)
    {
        return 0;
        // return Mathf.Abs(pos.X - EndPos.X) + Mathf.Abs(pos.Y - EndPos.Y); // Heuristic
    }

    private List<Vector2I> GetPotentialNeighbors(Vector2I pos)
    {
        var result = new List<Vector2I>();
        foreach (var dir in new Vector2I[] { new(1, 0), new(-1, 0), new(0, 1), new(0, -1) })
        {
            var npos = pos + dir;
            if (npos.X >= 0 && npos.X < Size.X && npos.Y >= 0 && npos.Y < Size.Y)
                result.Add(npos);
        }

        return result;
    }

    private List<Vector2I> GetNeighbours(Vector2I pos, bool canTransform = false)
    {
        var neighbours = new List<Vector2I>();
        foreach (var neighbour in GetPotentialNeighbors(pos))
            if (Tiles[neighbour.X, neighbour.Y] != TileType.Wall)
            {
                neighbours.Add(neighbour);
            }
            else
            {
                if (Tiles[pos.X, pos.Y] != TileType.Wall && canTransform &&
                    GetPotentialNeighbors(neighbour).Any(n => n != pos && Tiles[n.X, n.Y] != TileType.Wall))
                    neighbours.Add(neighbour);
            }

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

    public (List<Vector2I> path, int cost, ulong iterations) FindPath(bool canTransform = false)
    {
        ulong iterations = 0;

        var openSet = new SortedSet<AStarNode>(Comparer<AStarNode>.Create((a, b) =>
        {
            var compare = a.CostF.CompareTo(b.CostF);
            if (compare == 0) compare = a.Id.CompareTo(b.Id);
            return compare;
        }));

        var startNode = new AStarNode(StartPos, null, 0, CostH(StartPos));
        openSet.Add(startNode);

        var gScore = new Dictionary<Vector2I, int> { [StartPos] = 0 };

        while (openSet.Count > 0)
        {
            // GD.Print(string.Join(", ", openSet.Select(n => n.Position + " " + n.CostF).ToArray()));

            var currentNode = openSet.Min;
            var currentTile = Tiles[currentNode.Position.X, currentNode.Position.Y];

            openSet.Remove(currentNode);

            if (currentNode.Position == EndPos)
                return (ReconstructPath(currentNode), currentNode.CostG, iterations);

            foreach (var neighbour in GetNeighbours(currentNode.Position, canTransform))
            {
                iterations++;
                var tentativeGScore = gScore[currentNode.Position];
                if (canTransform && Tiles[neighbour.X, neighbour.Y] == TileType.Wall)
                {
                    tentativeGScore += 2;
                }
                else if (currentTile == TileType.Wall)
                {
                    var cameFromPos = currentNode.Parent.Position;
                    tentativeGScore += Tiles[cameFromPos.X, cameFromPos.Y] == Tiles[neighbour.X, neighbour.Y] ? 0 : 1;
                }
                else
                {
                    tentativeGScore += currentTile == Tiles[neighbour.X, neighbour.Y] ? 0 : 1;
                }


                // GD.Print("neighbour: " + neighbour + " tentativeGScore: " + tentativeGScore);

                if (!gScore.ContainsKey(neighbour) || tentativeGScore < gScore[neighbour])
                {
                    gScore[neighbour] = tentativeGScore;
                    var neighbourNode = new AStarNode(neighbour, currentNode, tentativeGScore, CostH(neighbour));

                    if (openSet.All(n => n.Position != neighbour))
                        openSet.Add(neighbourNode);
                }
            }
        }

        return (new List<Vector2I>(), -1, iterations); // No path found
    }
}