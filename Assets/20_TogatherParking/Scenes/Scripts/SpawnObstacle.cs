using System.Collections.Generic;
using UnityEngine;

public class SpawnObstacle : MonoBehaviour
{
    [System.Serializable]
    public struct ObstacleInfo
    {
        public GameObject prefab;
        public Vector2 sizeXZ; // size.x = width(x), size.y = depth(z)
    }

    public List<ObstacleInfo> obstacleList;

    public Vector2 installMin = new Vector2(0f, 1f);
    public Vector2 installMax = new Vector2(40f, 19f);

    public Vector2 carStart = new Vector2(3f, 3f);
    public Vector2 carEnd = new Vector2(37f, 17.5f);

    public float cellSize = 2f;

    public ObjectPool objectPool;

    int gridWidth;
    int gridHeight;

    // Forced second cell for initial corner logic (kept from original)
    readonly Vector2Int mustInclude = new Vector2Int(1, 0);

    // Directions (L R D U)
    static readonly Vector2Int[] Directions =
    {
        Vector2Int.left, Vector2Int.right, Vector2Int.down, Vector2Int.up
    };

    void Start()
    {
        // Preload obstacles
        if (objectPool != null)
        {
            foreach (var o in obstacleList)
                objectPool.Preload(o.prefab, 10, transform);
        }

        int level = 1;
        if (GameManager.Instance != null)
            level = GameManager.Instance.CurrentLevel;

        gridWidth = Mathf.CeilToInt((installMax.x - installMin.x) / cellSize);
        gridHeight = Mathf.CeilToInt((installMax.y - installMin.y) / cellSize);

        var path = FindPathWithRandomWeight(level);
        PlaceObstacles(path);
    }

    List<Vector2Int> FindPathWithRandomWeight(int level)
    {
        const int safetyAttempts = 200;

        List<Vector2Int> bestPath = null;
        int bestTurnDiff = int.MaxValue;

        for (int i = 0; i < safetyAttempts; i++)
        {
            var (path, turns) = TryFindPath(level);
            if (path == null) continue;

            int diff = Mathf.Abs(turns - level);
            if (diff < bestTurnDiff)
            {
                bestTurnDiff = diff;
                bestPath = path;
            }

            if (turns == level)
            {
                Debug.Log($"코너 {level}개 정확히 생성 성공 (attempt {i})");
                return path;
            }
        }

        Debug.LogWarning($"정확한 코너 {level}개 실패 → 가장 가까운 경로 반환 (turnDiff {bestTurnDiff})");
        return bestPath;
    }

    (List<Vector2Int> path, int turns) TryFindPath(int level)
    {
        Vector2Int start = WorldToGrid(carStart);
        Vector2Int end = WorldToGrid(carEnd);

        // Allocate arrays (fast, avoids dictionary overhead)
        float[,] gScore = new float[gridWidth, gridHeight];
        int[,] turnScore = new int[gridWidth, gridHeight];
        Vector2Int[,] parent = new Vector2Int[gridWidth, gridHeight];
        bool[,] closed = new bool[gridWidth, gridHeight];

        // Initialize
        for (int x = 0; x < gridWidth; x++)
        {
            for (int y = 0; y < gridHeight; y++)
            {
                gScore[x, y] = float.MaxValue;
                turnScore[x, y] = 0;
                parent[x, y] = new Vector2Int(-1, -1);
            }
        }

        // Force initial corner relation
        if (InBounds(mustInclude))
        {
            parent[start.x, start.y] = mustInclude;
            gScore[mustInclude.x, mustInclude.y] = 0f;
            gScore[start.x, start.y] = 1f;
        }

        // Pre-close neighbors of mustInclude except start (original behavior)
        if (InBounds(mustInclude))
        {
            foreach (var nei in GetNeighbors(mustInclude))
            {
                if (nei != start)
                    closed[nei.x, nei.y] = true;
            }
        }

        MinHeap open = new MinHeap(64);
        open.Push(new Node(start, gScore[start.x, start.y] + Heuristic(start, end)));

        bool strictMode = level > 15; // preserves original branch intent

        while (open.Count > 0)
        {
            Node node = open.Pop();
            Vector2Int current = node.Pos;

            // Skip outdated entries
            float fExpected = gScore[current.x, current.y] + Heuristic(current, end);
            if (node.F != fExpected) continue;

            if (current == end)
            {
                var path = Reconstruct(parent, current);
                return (path, turnScore[current.x, current.y]);
            }

            if (closed[current.x, current.y]) continue;
            closed[current.x, current.y] = true;

            var neighbors = GetNeighbors(current);
            List<Vector2Int> approved = null;

            foreach (var neighbor in neighbors)
            {
                if (closed[neighbor.x, neighbor.y]) continue;

                // Turn detection
                bool hasPrev = parent[current.x, current.y].x >= 0;
                bool isTurn = false;
                if (hasPrev)
                {
                    Vector2Int prev = parent[current.x, current.y];
                    Vector2Int dirPrev = current - prev;
                    Vector2Int dirNow = neighbor - current;
                    isTurn = dirPrev != dirNow;
                }

                // C-shape closure only in strictMode
                if (strictMode && hasPrev && IsClosingCShape(parent, current, neighbor))
                {
                    closed[neighbor.x, neighbor.y] = true;
                    continue;
                }

                int newTurnCount = turnScore[current.x, current.y] + (isTurn ? 1 : 0);
                if (newTurnCount > level) continue;

                // Weights
                float straightWeight = 0f;
                float randomWeight = 0f;
                float leftBias = 0f;

                if (newTurnCount < level)
                {
                    if (!isTurn) straightWeight = Random.Range(0f, 3f);
                    randomWeight = Random.Range(0f, 2f);

                    Vector2Int dirNow = neighbor - current;
                    if (strictMode && dirNow == Vector2Int.left)
                        leftBias = -5f; // keep original left bias only for strict branch
                }

                float tentativeG = gScore[current.x, current.y] + 1f + straightWeight + randomWeight + leftBias;

                if (tentativeG < gScore[neighbor.x, neighbor.y])
                {
                    gScore[neighbor.x, neighbor.y] = tentativeG;
                    parent[neighbor.x, neighbor.y] = current;
                    turnScore[neighbor.x, neighbor.y] = newTurnCount;

                    float f = tentativeG + Heuristic(neighbor, end);
                    open.Push(new Node(neighbor, f));

                    if (strictMode)
                    {
                        // Collect approved only in strict mode (branch with random closing)
                        approved ??= new List<Vector2Int>(4);
                        approved.Add(neighbor);
                    }
                }
            }

            // Strict mode: keep only one approved neighbor randomly, close rest
            if (strictMode && approved != null && approved.Count > 1)
            {
                int keepIndex = Random.Range(0, approved.Count);
                Vector2Int keep = approved[keepIndex];
                for (int i = 0; i < approved.Count; i++)
                {
                    if (i != keepIndex)
                        closed[approved[i].x, approved[i].y] = true;
                }
                // Close non-approved neighbors
                foreach (var nei in neighbors)
                {
                    bool wasApproved = false;
                    for (int i = 0; i < approved.Count; i++)
                    {
                        if (approved[i] == nei)
                        {
                            wasApproved = true;
                            break;
                        }
                    }
                    if (!wasApproved)
                        closed[nei.x, nei.y] = true;
                }
            }
        }

        return (null, 999);
    }

    bool InBounds(Vector2Int p) => p.x >= 0 && p.x < gridWidth && p.y >= 0 && p.y < gridHeight;

    bool IsClosingCShape(Vector2Int[,] parent, Vector2Int current, Vector2Int neighbor)
    {
        Vector2Int prev = parent[current.x, current.y];
        if (prev.x < 0) return false;
        Vector2Int prevPrev = parent[prev.x, prev.y];
        if (prevPrev.x < 0) return false;

        Vector2Int dirPrevPrev = prev - prevPrev;
        Vector2Int dirPrev = current - prev;
        Vector2Int dirNow = neighbor - current;

        if (dirPrev == dirPrevPrev) return false;
        return dirNow == -dirPrevPrev;
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        // Manhattan
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        List<Vector2Int> list = new List<Vector2Int>(4);
        for (int i = 0; i < Directions.Length; i++)
        {
            Vector2Int n = cell + Directions[i];
            if (n.x >= 0 && n.x < gridWidth && n.y >= 0 && n.y < gridHeight)
                list.Add(n);
        }
        return list;
    }

    List<Vector2Int> Reconstruct(Vector2Int[,] parent, Vector2Int current)
    {
        List<Vector2Int> path = new List<Vector2Int>(64);
        path.Add(current);
        while (true)
        {
            Vector2Int p = parent[current.x, current.y];
            if (p.x < 0) break;
            path.Add(p);
            current = p;
        }
        path.Reverse();
        return path;
    }

    Vector2Int WorldToGrid(Vector2 world)
    {
        int x = Mathf.Clamp(
            Mathf.FloorToInt((world.x - installMin.x) / cellSize),
            0, gridWidth - 1);
        int y = Mathf.Clamp(
            Mathf.FloorToInt((world.y - installMin.y) / cellSize),
            0, gridHeight - 1);
        return new Vector2Int(x, y);
    }

    Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(
            installMin.x + gridPos.x * cellSize + cellSize * 0.5f,
            installMin.y + gridPos.y * cellSize + cellSize * 0.5f
        );
    }

    void PlaceObstacles(List<Vector2Int> path)
    {
        if (path == null || obstacleList == null || obstacleList.Count == 0) return;

        HashSet<Vector2Int> pathSet = new HashSet<Vector2Int>(path);
        HashSet<Vector2Int> candidates = new HashSet<Vector2Int>();

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int cell = path[i];
            foreach (var n in GetNeighbors(cell))
            {
                if (!pathSet.Contains(n))
                    candidates.Add(n);
            }
        }

        foreach (var cell in candidates)
        {
            Vector2 worldPos = GridToWorld(cell);
            var obstacle = obstacleList[Random.Range(0, obstacleList.Count)];
            GameObject obj = objectPool.Get(obstacle.prefab, transform);
            obj.transform.position = new Vector3(worldPos.x, 0f, worldPos.y);
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(transform, true);
        }
    }

    // ---------------- Min-Heap (priority queue) ----------------
    struct Node
    {
        public Vector2Int Pos;
        public float F;
        public Node(Vector2Int p, float f)
        {
            Pos = p;
            F = f;
        }
    }

    class MinHeap
    {
        List<Node> data;
        public int Count => data.Count;

        public MinHeap(int capacity) { data = new List<Node>(capacity); }

        public void Push(Node n)
        {
            data.Add(n);
            SiftUp(data.Count - 1);
        }

        public Node Pop()
        {
            int last = data.Count - 1;
            Node root = data[0];
            data[0] = data[last];
            data.RemoveAt(last);
            if (data.Count > 0) SiftDown(0);
            return root;
        }

        void SiftUp(int i)
        {
            while (i > 0)
            {
                int parent = (i - 1) >> 1;
                if (data[parent].F <= data[i].F) break;
                Swap(parent, i);
                i = parent;
            }
        }

        void SiftDown(int i)
        {
            int count = data.Count;
            while (true)
            {
                int left = (i << 1) + 1;
                if (left >= count) break;
                int right = left + 1;
                int smallest = (right < count && data[right].F < data[left].F) ? right : left;
                if (data[i].F <= data[smallest].F) break;
                Swap(i, smallest);
                i = smallest;
            }
        }

        void Swap(int a, int b)
        {
            Node tmp = data[a];
            data[a] = data[b];
            data[b] = tmp;
        }
    }
}