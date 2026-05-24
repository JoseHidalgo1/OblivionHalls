using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class LevelGenerator : MonoBehaviour
{
    [Header("Size")]
    public int width = 64;
    public int height = 48;
    public int tileSize = 1;

    [Header("Rooms")]
    public int roomCount = 8;
    public int minRoomSize = 4;
    public int maxRoomSize = 10;

    [Header("Prefabs")]
    public GameObject floorPrefab;
    public GameObject wallPrefab;
    public GameObject enemyPrefab;
    public GameObject bossPrefab;
    public GameObject playerPrefab;

    [Header("Generation")]
    public bool autoGenerateInEditor = false;
    public int seed = 0;

    private Transform mapRoot;

    [ContextMenu("Generate Level")]
    public void Generate()
    {
        ClearPrevious();
        if (seed != 0)
            Random.InitState(seed);

        mapRoot = new GameObject("Level").transform;
        mapRoot.SetParent(transform, false);

        bool[,] map = new bool[width, height]; // true = floor

        List<Rect> rooms = new List<Rect>();

        for (int i = 0; i < roomCount; i++)
        {
            int rw = Random.Range(minRoomSize, maxRoomSize + 1);
            int rh = Random.Range(minRoomSize, maxRoomSize + 1);
            int rx = Random.Range(1, Mathf.Max(2, width - rw - 1));
            int ry = Random.Range(1, Mathf.Max(2, height - rh - 1));

            Rect room = new Rect(rx, ry, rw, rh);
            bool overlaps = false;
            foreach (var r in rooms)
            {
                if (r.Overlaps(room))
                {
                    overlaps = true; break;
                }
            }
            if (!overlaps)
            {
                rooms.Add(room);
                CarveRoom(map, room);
            }
        }

        // Connect rooms by centers
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int prev = Center(rooms[i - 1]);
            Vector2Int cur = Center(rooms[i]);
            CarveCorridor(map, prev, cur);
        }

        // Place floor and walls
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3 pos = new Vector3((x - width / 2f) * tileSize, (y - height / 2f) * tileSize, 0f);
                if (map[x, y])
                {
                    if (floorPrefab != null)
                        Instantiate(floorPrefab, pos, Quaternion.identity, mapRoot);
                    else
                        CreateQuad(pos, mapRoot, new Color(0.45f, 0.45f, 0.45f));
                }
                else
                {
                    // If adjacent to floor, create a wall
                    if (HasAdjacentFloor(map, x, y))
                    {
                        if (wallPrefab != null)
                            Instantiate(wallPrefab, pos, Quaternion.identity, mapRoot);
                        else
                            CreateQuad(pos, mapRoot, new Color(0.15f, 0.15f, 0.15f));
                    }
                }
            }
        }

        // Place enemies in rooms (excluding the first room)
        for (int i = 1; i < rooms.Count; i++)
        {
            Vector2Int c = Center(rooms[i]);
            Vector3 pos = new Vector3((c.x - width / 2f) * tileSize, (c.y - height / 2f) * tileSize, 0f);
            if (enemyPrefab != null)
                Instantiate(enemyPrefab, pos, Quaternion.identity, mapRoot);
        }

        // Place boss in last room
        if (rooms.Count > 0)
        {
            Vector2Int bc = Center(rooms[rooms.Count - 1]);
            Vector3 bpos = new Vector3((bc.x - width / 2f) * tileSize, (bc.y - height / 2f) * tileSize, 0f);
            if (bossPrefab != null)
                Instantiate(bossPrefab, bpos, Quaternion.identity, mapRoot);
        }

        // Place player in first room
        if (rooms.Count > 0)
        {
            Vector2Int pc = Center(rooms[0]);
            Vector3 ppos = new Vector3((pc.x - width / 2f) * tileSize, (pc.y - height / 2f) * tileSize, 0f);
            if (playerPrefab != null)
                Instantiate(playerPrefab, ppos, Quaternion.identity, mapRoot);
        }
    }

    private void ClearPrevious()
    {
        // Remove previous generated children
        var existing = transform.Find("Level");
        if (existing != null)
        {
            if (Application.isPlaying)
                Destroy(existing.gameObject);
            else
                DestroyImmediate(existing.gameObject);
        }
    }

    private void CarveRoom(bool[,] map, Rect room)
    {
        for (int x = (int)room.xMin; x < (int)room.xMax; x++)
        {
            for (int y = (int)room.yMin; y < (int)room.yMax; y++)
            {
                if (x >= 0 && x < width && y >= 0 && y < height)
                    map[x, y] = true;
            }
        }
    }

    private void CarveCorridor(bool[,] map, Vector2Int a, Vector2Int b)
    {
        Vector2Int current = a;
        while (current.x != b.x)
        {
            if (current.x < b.x) current.x++; else current.x--;
            if (current.x >= 0 && current.x < width && current.y >= 0 && current.y < height)
                map[current.x, current.y] = true;
        }
        while (current.y != b.y)
        {
            if (current.y < b.y) current.y++; else current.y--;
            if (current.x >= 0 && current.x < width && current.y >= 0 && current.y < height)
                map[current.x, current.y] = true;
        }
    }

    private Vector2Int Center(Rect r)
    {
        int cx = Mathf.FloorToInt(r.x + r.width / 2f);
        int cy = Mathf.FloorToInt(r.y + r.height / 2f);
        return new Vector2Int(cx, cy);
    }

    private bool HasAdjacentFloor(bool[,] map, int x, int y)
    {
        int[] dx = { -1, 1, 0, 0 };
        int[] dy = { 0, 0, -1, 1 };
        for (int i = 0; i < 4; i++)
        {
            int nx = x + dx[i];
            int ny = y + dy[i];
            if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                if (map[nx, ny]) return true;
        }
        return false;
    }

    private void CreateQuad(Vector3 pos, Transform parent, Color color)
    {
        GameObject q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        q.transform.SetParent(parent, false);
        q.transform.localPosition = pos;
        q.transform.localScale = Vector3.one * tileSize;
        var rend = q.GetComponent<Renderer>();
        rend.sharedMaterial = new Material(Shader.Find("Sprites/Default"));
        rend.sharedMaterial.color = color;
    }

    private void OnValidate()
    {
        if (!Application.isPlaying && autoGenerateInEditor)
        {
            Generate();
        }
    }
}
