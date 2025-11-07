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

        // 2. 1회 탐색으로 레벨 코너 수 맞추기
        List<Vector2Int> path = FindPathWithRandomWeight(level);

        // 3. 장애물 배치
        PlaceObstacles(path);
    }

    List<Vector2Int> FindPathWithRandomWeight(int level)
    {
        Vector2Int start = WorldToGrid(carStart);
        Vector2Int end = WorldToGrid(carEnd);
        Vector2Int mustInclude = new Vector2Int(1, 0);

        var open = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore = new Dictionary<Vector2Int, float> { [start] = 0 };
        var fScore = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, end) };
        var turnScore = new Dictionary<Vector2Int, int> { [start] = 0 };

        System.Random rand = new System.Random();

        while (open.Count > 0)
        {
            open.Sort((a, b) =>
                fScore.GetValueOrDefault(a, float.MaxValue)
                .CompareTo(fScore.GetValueOrDefault(b, float.MaxValue)));

            var current = open[0];
            open.RemoveAt(0);

            // 목표 도달
            if (current == end)
            {
                var path = ReconstructPath(cameFrom, current);
                if (path.Count == 0 || path[0] != mustInclude)
                    path.Insert(0, mustInclude);
                Debug.Log(turnScore[current]);
                return path;
            }

            foreach (var neighbor in GetNeighbors(current))
            {
                bool hasPrev = cameFrom.ContainsKey(current);
                bool isTurn = false;

                if (hasPrev)
                {
                    Vector2Int prev = cameFrom[current];
                    Vector2Int dirPrev = (current - prev);
                    Vector2Int dirNow = (neighbor - current);
                    isTurn = (dirPrev != dirNow);
                }

                int newTurnCount = turnScore[current] + (isTurn ? 1 : 0);

                if (newTurnCount > level)
                    continue;

                // 1) 방향 유지일 때 추가 가중치 (0~3)
                float straightWeight = 0f;
                if (!isTurn)
                {
                    straightWeight = Random.Range(0f, 3f);
                }

                // 2) 기존 randomWeight → turn이 아직 남아있을 때만 적용
                float randomWeight = (newTurnCount < level)
                    ? Random.Range(0f, 2f)
                    : 0.0f;

                // 총비용
                float tentativeG = gScore[current] + 1 + straightWeight + randomWeight;

                if (tentativeG < gScore.GetValueOrDefault(neighbor, float.MaxValue))
                {
                    cameFrom[neighbor] = current;
                    gScore[neighbor] = tentativeG;
                    fScore[neighbor] = tentativeG + Heuristic(neighbor, end);
                    turnScore[neighbor] = newTurnCount;

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