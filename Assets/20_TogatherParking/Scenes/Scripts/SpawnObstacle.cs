using System.Collections.Generic;
using UnityEngine;

public class SpawnObstacle : MonoBehaviour
{
    [System.Serializable]
    public struct ObstacleInfo
    {
        public GameObject prefab;
        public Vector2 sizeXZ; // size.x = width (x), size.y = depth (z)
    }

    public List<ObstacleInfo> obstacleList;

    public Vector2 installMin = new Vector2(0f, 1f);     // x, z
    public Vector2 installMax = new Vector2(40f, 19f);   // x, z

    public Vector2 carStart = new Vector2(3f, 3f);       // x, z
    public Vector2 carEnd = new Vector2(37f, 17.5f);     // x, z

    public float cellSize = 2f; // 격자 셀 크기

    int gridWidth, gridHeight;

    public ObjectPool objectPool; // 인스펙터에서 할당

    void Start()
    {
        // 오브젝트 풀 미리 생성
        foreach (var obstacle in obstacleList)
        {
            objectPool.Preload(obstacle.prefab, 10, transform);
        }

        // GameManager에서 레벨 받아오기
        int level = 1;
        if (GameManager.Instance != null)
            level = GameManager.Instance.CurrentLevel;
        Debug.Log(level);
        // 1. 격자 생성
        gridWidth = Mathf.CeilToInt((installMax.x - installMin.x) / cellSize);
        gridHeight = Mathf.CeilToInt((installMax.y - installMin.y) / cellSize);

        // 2. 경로 찾기 (A* 알고리즘, 난이도 반영)
        List<Vector2Int> path = FindPathWithExactTurns(level);

        // 3. 장애물 배치
        PlaceObstacles(path);
    }

    List<Vector2Int> FindPathWithExactTurns(int requiredTurns)
    {
        // 레벨 1(턴 1개)일 때만 L자 경로 생성
        if (requiredTurns == 1)
        {
            Vector2Int start = WorldToGrid(carStart);
            Vector2Int end = WorldToGrid(carEnd);
            var path = new List<Vector2Int>();

            // y축(세로)로 먼저 이동
            for (int y = start.y; y != end.y; y += (end.y > start.y ? 1 : -1))
                path.Add(new Vector2Int(start.x, y));
            // x축(가로)로 이동
            for (int x = start.x; x != end.x; x += (end.x > start.x ? 1 : -1))
                path.Add(new Vector2Int(x, end.y));
            // 마지막 도착점 추가
            path.Add(end);
            Vector2Int mustInclude = new Vector2Int(1, 0);
            // (1,0)이 경로에 없으면 맨 앞에 추가
            if (path.Count == 0 || path[0] != mustInclude)
                path.Insert(0, mustInclude);

            return path;
        }
        List<Vector2Int> matchedPath = null;
        int minTurnDiff = int.MaxValue;
        int tryCount = 50;

        for (int i = 0; i < tryCount; i++)
        {
            List<Vector2Int> path = FindPathWithRandomWeight(i);
            if (path.Count == 0) continue;

            int turns = CountTurns(path);
            int turnDiff = Mathf.Abs(turns - requiredTurns);

            if (turns == requiredTurns)
                return path;

            if (turnDiff < minTurnDiff)
            {
                minTurnDiff = turnDiff;
                matchedPath = path;
            }
        }
        return matchedPath ?? new List<Vector2Int>();
    }

    int CountTurns(List<Vector2Int> path)
    {
        if (path == null || path.Count < 2) return 0;
        int turns = 0;
        Vector2Int prevDir = path[1] - path[0];
        for (int i = 2; i < path.Count; i++)
        {
            Vector2Int dir = path[i] - path[i - 1];
            if (dir != prevDir)
            {
                turns++;
                prevDir = dir;
            }
        }
        return turns;
    }

    List<Vector2Int> FindPathWithRandomWeight(int seedOffset)
    {
        Vector2Int start = WorldToGrid(carStart);
        Vector2Int end = WorldToGrid(carEnd);
        Vector2Int mustInclude = new Vector2Int(1, 0);

        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, end) };

        System.Random rand = new System.Random(System.DateTime.Now.Millisecond + seedOffset);

        while (open.Count > 0)
        {
            open.Sort((a, b) => fScore.GetValueOrDefault(a, float.MaxValue).CompareTo(fScore.GetValueOrDefault(b, float.MaxValue)));
            var current = open[0];
            if (current == end)
            {
                List<Vector2Int> path = ReconstructPath(cameFrom, current);

                // (1,0)이 경로에 없으면 맨 앞에 추가
                if (path.Count == 0 || path[0] != mustInclude)
                    path.Insert(0, mustInclude);

                return path;
            }

            open.RemoveAt(0);

            foreach (var neighbor in GetNeighbors(current))
            {
                float randomWeight = (float)rand.NextDouble() * 2.0f;
                float tentativeG = gScore[current] + 1 + randomWeight;
                if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, end);
                    if (!open.Contains(neighbor))
                        open.Add(neighbor);
                }
            }
        }
        return new List<Vector2Int>();
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y);
    }

    List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        var neighbors = new List<Vector2Int>();
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = cell.x + dx[i];
            int ny = cell.y + dy[i];
            if (nx >= 0 && nx < gridWidth && ny >= 0 && ny < gridHeight)
                neighbors.Add(new Vector2Int(nx, ny));
        }
        return neighbors;
    }

    List<Vector2Int> ReconstructPath(Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int current)
    {
        var path = new List<Vector2Int> { current };
        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }
        path.Reverse();
        return path;
    }

    Vector2Int WorldToGrid(Vector2 world)
    {
        int x = Mathf.Clamp(Mathf.FloorToInt((world.x - installMin.x) / cellSize), 0, gridWidth - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((world.y - installMin.y) / cellSize), 0, gridHeight - 1);
        return new Vector2Int(x, y);
    }

    Vector2 GridToWorld(Vector2Int gridPos)
    {
        return new Vector2(
            installMin.x + gridPos.x * cellSize + cellSize / 2f,
            installMin.y + gridPos.y * cellSize + cellSize / 2f
        );
    }

    void PlaceObstacles(List<Vector2Int> path)
    {
        HashSet<Vector2Int> pathSet = new HashSet<Vector2Int>(path);
        HashSet<Vector2Int> obstacleCandidates = new HashSet<Vector2Int>();

        foreach (var cell in path)
        {
            foreach (var neighbor in GetNeighbors(cell))
            {
                if (!pathSet.Contains(neighbor))
                {
                    obstacleCandidates.Add(neighbor);
                }
            }
        }

        foreach (var cell in obstacleCandidates)
        {
            Vector2 worldPos = GridToWorld(cell);

            ObstacleInfo obstacle = obstacleList[Random.Range(0, obstacleList.Count)];

            GameObject obj = objectPool.Get(obstacle.prefab, transform);
            obj.transform.position = new Vector3(worldPos.x, 0f, worldPos.y);
            obj.transform.rotation = Quaternion.identity;
            obj.transform.SetParent(transform);
        }
    }
}