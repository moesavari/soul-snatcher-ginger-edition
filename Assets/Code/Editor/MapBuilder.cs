#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapBuilder : EditorWindow
{

    [Header("Scene")]
    [SerializeField] private Grid _grid;
    [SerializeField] private Tilemap _ground;
    [SerializeField] private Tilemap _floor;
    [SerializeField] private Tilemap _walls;

    [Header("Map Size")]
    [SerializeField] private int _width = 160;
    [SerializeField] private int _height = 160;
    [SerializeField] private bool _centerOnOrigin = true;

    [Header("Ground Tiles")]
    [SerializeField] private TileBase _grassInnerBase;
    [SerializeField] private List<TileBase> _grassInnerVar = new();
    [SerializeField, Range(0, 0.2f)] private float _grassInnerVarChance = 0.02f;

    [SerializeField] private TileBase _grassOuterBase;
    [SerializeField] private List<TileBase> _grassOuterVar = new();
    [SerializeField, Range(0, 0.4f)] private float _grassOuterVarChance = 0.12f;

    [Header("Inner Area (Temple Zone)")]
    [SerializeField] private int _innerMarginX = 24;
    [SerializeField] private int _innerMarginY = 24;

    [Header("Floor / Wall Tiles")]
    [SerializeField] private TileBase _floorTile;
    [SerializeField] private TileBase _wallTile;
    [SerializeField] private int _wallThickness = 1;
    [SerializeField] private float _ruinChance = 0.06f;

    [Header("Central Courtyard")]
    [SerializeField] private int _courtyardWidth = 36;
    [SerializeField] private int _courtyardHeight = 26;
    [SerializeField] private int _courtyardWallInset = 2;

    [Header("Rooms (BSP)")]
    [SerializeField] private int _bspMinRoomW = 10;
    [SerializeField] private int _bspMinRoomH = 8;
    [SerializeField] private int _bspMaxRoomW = 28;
    [SerializeField] private int _bspMaxRoomH = 22;
    [SerializeField] private int _bspSplits = 4;
    [SerializeField] private int _roomPadding = 2;

    [Header("Corridors")]
    [SerializeField] private int _corridorWidth = 2;

    [Header("Build")]
    [SerializeField] private int _seed = 7;

    [MenuItem("SoulSnatched/Map Builder")]
    public static void Open()
    {
        var w = GetWindow<MapBuilder>("Map Builder");
        w.minSize = new Vector2(520, 720);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Scene", EditorStyles.boldLabel);
        _grid = (Grid)EditorGUILayout.ObjectField("Grid", _grid, typeof(Grid), true);
        _ground = (Tilemap)EditorGUILayout.ObjectField("Tilemap Ground", _ground, typeof(Tilemap), true);
        _floor = (Tilemap)EditorGUILayout.ObjectField("Tilemap Floor", _floor, typeof(Tilemap), true);
        _walls = (Tilemap)EditorGUILayout.ObjectField("Tilemap Walls", _walls, typeof(Tilemap), true);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Map Size", EditorStyles.boldLabel);
        _width = EditorGUILayout.IntSlider("Width (X)", _width, 64, 512);
        _height = EditorGUILayout.IntSlider("Height (Y)", _height, 64, 512);
        _centerOnOrigin = EditorGUILayout.Toggle("Center on (0,0)", _centerOnOrigin);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Ground Tiles", EditorStyles.boldLabel);
        _grassInnerBase = (TileBase)EditorGUILayout.ObjectField("Inner Base", _grassInnerBase, typeof(TileBase), false);
        SerializedObject so1 = new SerializedObject(this);
        EditorGUILayout.PropertyField(so1.FindProperty("_grassInnerVar"), true);
        so1.ApplyModifiedProperties();
        _grassInnerVarChance = EditorGUILayout.Slider("Inner Var Chance", _grassInnerVarChance, 0f, 0.2f);

        _grassOuterBase = (TileBase)EditorGUILayout.ObjectField("Outer Base", _grassOuterBase, typeof(TileBase), false);
        SerializedObject so2 = new SerializedObject(this);
        EditorGUILayout.PropertyField(so2.FindProperty("_grassOuterVar"), true);
        so2.ApplyModifiedProperties();
        _grassOuterVarChance = EditorGUILayout.Slider("Outer Var Chance", _grassOuterVarChance, 0f, 0.4f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Inner Area (Temple Zone)", EditorStyles.boldLabel);
        _innerMarginX = Mathf.Max(0, EditorGUILayout.IntField("Inner Margin X", _innerMarginX));
        _innerMarginY = Mathf.Max(0, EditorGUILayout.IntField("Inner Margin Y", _innerMarginY));

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Floors / Walls", EditorStyles.boldLabel);
        _floorTile = (TileBase)EditorGUILayout.ObjectField("Floor Tile", _floorTile, typeof(TileBase), false);
        _wallTile = (TileBase)EditorGUILayout.ObjectField("Wall Tile", _wallTile, typeof(TileBase), false);
        _wallThickness = Mathf.Clamp(EditorGUILayout.IntField("Wall Thickness", _wallThickness), 1, 3);
        _ruinChance = EditorGUILayout.Slider("Ruin Chance", _ruinChance, 0f, 0.3f);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Central Courtyard", EditorStyles.boldLabel);
        _courtyardWidth = Mathf.Max(8, EditorGUILayout.IntField("Width", _courtyardWidth));
        _courtyardHeight = Mathf.Max(6, EditorGUILayout.IntField("Height", _courtyardHeight));
        _courtyardWallInset = Mathf.Clamp(EditorGUILayout.IntField("Wall Inset", _courtyardWallInset), 0, 8);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Rooms (BSP)", EditorStyles.boldLabel);
        _bspMinRoomW = Mathf.Max(6, EditorGUILayout.IntField("Min Room W", _bspMinRoomW));
        _bspMinRoomH = Mathf.Max(5, EditorGUILayout.IntField("Min Room H", _bspMinRoomH));
        _bspMaxRoomW = Mathf.Max(_bspMinRoomW, EditorGUILayout.IntField("Max Room W", _bspMaxRoomW));
        _bspMaxRoomH = Mathf.Max(_bspMinRoomH, EditorGUILayout.IntField("Max Room H", _bspMaxRoomH));
        _bspSplits = Mathf.Clamp(EditorGUILayout.IntField("Splits", _bspSplits), 1, 10);
        _roomPadding = Mathf.Clamp(EditorGUILayout.IntField("Room Padding", _roomPadding), 0, 6);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Corridors", EditorStyles.boldLabel);
        _corridorWidth = Mathf.Clamp(EditorGUILayout.IntField("Corridor Width", _corridorWidth), 1, 4);

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Build", EditorStyles.boldLabel);
        _seed = EditorGUILayout.IntField("Seed", _seed);

        EditorGUILayout.Space();
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Build / Rebuild", GUILayout.Height(34))) Build();
        if (GUILayout.Button("Clear", GUILayout.Height(34))) { _ground?.ClearAllTiles(); _floor?.ClearAllTiles(); _walls?.ClearAllTiles(); }
        EditorGUILayout.EndHorizontal();
    }

    private void Build()
    {
        if (_grid == null || _ground == null || _floor == null || _walls == null)
        {
            Debug.LogWarning("Assign Grid, Ground, Floor, and Walls tilemaps.");
            return;
        }
        if (_grassInnerBase == null || _grassOuterBase == null || _floorTile == null || _wallTile == null)
        {
            Debug.LogWarning("Assign base tiles for inner/outer grass, floor, and wall.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(_grid.gameObject, "Build Temple Map");
        _ground.ClearAllTiles();
        _floor.ClearAllTiles();
        _walls.ClearAllTiles();

        var rng = new System.Random(_seed);

        int sx = _centerOnOrigin ? -_width / 2 : 0;
        int sy = _centerOnOrigin ? -_height / 2 : 0;
        int ex = sx + _width - 1;
        int ey = sy + _height - 1;

        var inner = new RectInt(
            sx + _innerMarginX,
            sy + _innerMarginY,
            Mathf.Max(1, _width - _innerMarginX * 2),
            Mathf.Max(1, _height - _innerMarginY * 2)
        );

        for (int y = sy; y <= ey; y++)
            for (int x = sx; x <= ex; x++)
            {
                bool inInner = inner.Contains(new Vector2Int(x, y));
                var baseT = inInner ? _grassInnerBase : _grassOuterBase;
                var list = inInner ? _grassInnerVar : _grassOuterVar;
                float c = inInner ? _grassInnerVarChance : _grassOuterVarChance;

                var t = baseT;
                if (list.Count > 0 && rng.NextDouble() < c)
                    t = list[rng.Next(0, list.Count)];
                _ground.SetTile(new Vector3Int(x, y, 0), t);
            }

        var court = CenteredRect(inner, _courtyardWidth, _courtyardHeight);
        FillRect(_floor, court, _floorTile);

        var courtWalls = Expand(court, _courtyardWallInset + _wallThickness, _courtyardWallInset + _wallThickness);
        DrawRect(_walls, _wallTile, courtWalls, _wallThickness, rng, _ruinChance);

        var roomRects = GenerateBspRooms(inner, court, rng);

        foreach (var r in roomRects)
        {
            FillRect(_floor, r, _floorTile);
            DrawRect(_walls, _wallTile, Expand(r, _wallThickness, _wallThickness), _wallThickness, rng, _ruinChance);
        }

        var nodes = new List<Vector2Int>();
        Vector2Int courtCenter = new Vector2Int(court.x + court.width / 2, court.y + court.height / 2);
        nodes.Add(courtCenter);
        foreach (var r in roomRects) nodes.Add(new Vector2Int(r.x + r.width / 2, r.y + r.height / 2));

        foreach (var e in BuildMst(nodes, rng))
            DrawLCorridor(_floor, _floorTile, e.a, e.b, _corridorWidth);

        foreach (var e in BuildMst(nodes, rng))
            OutlineLCorridorWithWalls(_walls, _wallTile, e.a, e.b, _corridorWidth, _wallThickness, rng, _ruinChance);

        Debug.Log($"Temple map built: {_width}x{_height} | Inner {inner.width}x{inner.height} | Rooms {roomRects.Count}");
    }

    private List<RectInt> GenerateBspRooms(RectInt inner, RectInt exclude, System.Random rng)
    {

        var initial = new List<RectInt> { inner };

        var work = new List<RectInt>();
        foreach (var r in initial)
            work.AddRange(SubtractRect(r, Expand(exclude, 4, 4)));

        for (int i = 0; i < _bspSplits; i++)
        {
            var next = new List<RectInt>();
            foreach (var r in work)
            {

                bool splitVert = r.width > r.height ? true : r.height > r.width ? false : rng.Next(0, 2) == 0;

                if (splitVert && r.width >= _bspMinRoomW * 2 + 4)
                {
                    int cut = rng.Next(r.xMin + _bspMinRoomW, r.xMax - _bspMinRoomW);
                    var left = new RectInt(r.xMin, r.yMin, cut - r.xMin, r.height);
                    var right = new RectInt(cut, r.yMin, r.xMax - cut, r.height);
                    next.Add(left); next.Add(right);
                }
                else if (!splitVert && r.height >= _bspMinRoomH * 2 + 4)
                {
                    int cut = rng.Next(r.yMin + _bspMinRoomH, r.yMax - _bspMinRoomH);
                    var bot = new RectInt(r.xMin, r.yMin, r.width, cut - r.yMin);
                    var top = new RectInt(r.xMin, cut, r.width, r.yMax - cut);
                    next.Add(bot); next.Add(top);
                }
                else
                {
                    next.Add(r);
                }
            }
            work = next;
        }

        var rooms = new List<RectInt>();
        foreach (var r in work)
        {
            int rx = r.xMin + _roomPadding;
            int ry = r.yMin + _roomPadding;
            int rw = Mathf.Clamp(r.width - _roomPadding * 2, _bspMinRoomW, _bspMaxRoomW);
            int rh = Mathf.Clamp(r.height - _roomPadding * 2, _bspMinRoomH, _bspMaxRoomH);
            if (rw < _bspMinRoomW || rh < _bspMinRoomH) continue;

            rw = Mathf.Clamp(rw - rng.Next(0, 3), _bspMinRoomW, _bspMaxRoomW);
            rh = Mathf.Clamp(rh - rng.Next(0, 3), _bspMinRoomH, _bspMaxRoomH);

            var rr = CenteredRect(new RectInt(rx, ry, r.width - _roomPadding * 2, r.height - _roomPadding * 2), rw, rh);

            if (rr.Overlaps(Expand(_courtyardCache, 2, 2))) continue;

            rooms.Add(rr);
        }
        return rooms;
    }

    private RectInt _courtyardCache;

    private RectInt CenteredRect(RectInt container, int w, int h)
    {
        int cw = Mathf.Min(container.width, w);
        int ch = Mathf.Min(container.height, h);
        int x = container.xMin + (container.width - cw) / 2;
        int y = container.yMin + (container.height - ch) / 2;
        var r = new RectInt(x, y, cw, ch);
        _courtyardCache = r;
        return r;
    }

    private void DrawLCorridor(Tilemap tm, TileBase tile, Vector2Int a, Vector2Int b, int width)
    {

        bool horizFirst = (a.x + a.y + b.x + b.y) % 2 == 0;

        if (horizFirst)
        {
            DrawThickLine(tm, tile, new Vector3Int(a.x, a.y, 0), new Vector3Int(b.x, a.y, 0), width);
            DrawThickLine(tm, tile, new Vector3Int(b.x, a.y, 0), new Vector3Int(b.x, b.y, 0), width);
        }
        else
        {
            DrawThickLine(tm, tile, new Vector3Int(a.x, a.y, 0), new Vector3Int(a.x, b.y, 0), width);
            DrawThickLine(tm, tile, new Vector3Int(a.x, b.y, 0), new Vector3Int(b.x, b.y, 0), width);
        }
    }

    private void OutlineLCorridorWithWalls(
        Tilemap walls, TileBase wallTile,
        Vector2Int a, Vector2Int b,
        int corridorWidth, int wallThickness,
        System.Random rng, float ruinChance)
    {

        bool horizFirst = (a.x + a.y + b.x + b.y) % 2 == 0;

        if (horizFirst)
        {
            var segH = ToRect(new Vector3Int(Mathf.Min(a.x, b.x), a.y, 0), Mathf.Abs(b.x - a.x) + 1, corridorWidth);
            var segV = ToRect(new Vector3Int(b.x, Mathf.Min(a.y, b.y), 0), corridorWidth, Mathf.Abs(b.y - a.y) + 1);
            DrawRect(walls, wallTile, Expand(segH, wallThickness, wallThickness), wallThickness, rng, ruinChance);
            DrawRect(walls, wallTile, Expand(segV, wallThickness, wallThickness), wallThickness, rng, ruinChance);
        }
        else
        {
            var segV = ToRect(new Vector3Int(a.x, Mathf.Min(a.y, b.y), 0), corridorWidth, Mathf.Abs(b.y - a.y) + 1);
            var segH = ToRect(new Vector3Int(Mathf.Min(a.x, b.x), b.y, 0), Mathf.Abs(b.x - a.x) + 1, corridorWidth);
            DrawRect(walls, wallTile, Expand(segV, wallThickness, wallThickness), wallThickness, rng, ruinChance);
            DrawRect(walls, wallTile, Expand(segH, wallThickness, wallThickness), wallThickness, rng, ruinChance);
        }
    }

    private RectInt ToRect(Vector3Int start, int w, int h) => new RectInt(start.x, start.y, w, h);

    private struct Edge { public Vector2Int a, b; public Edge(Vector2Int A, Vector2Int B) { a = A; b = B; } }
    private List<Edge> BuildMst(List<Vector2Int> nodes, System.Random rng)
    {
        var edges = new List<Edge>();
        var inTree = new HashSet<Vector2Int>();
        var frontier = new List<Vector2Int>();

        var start = nodes[rng.Next(nodes.Count)];
        inTree.Add(start);
        AddNeighbors(nodes, start, frontier);

        while (frontier.Count > 0)
        {
            var n = frontier[rng.Next(frontier.Count)];
            frontier.Remove(n);
            if (inTree.Contains(n)) continue;

            var best = Closest(n, inTree);
            edges.Add(new Edge(best, n));
            inTree.Add(n);
            AddNeighbors(nodes, n, frontier);
        }
        return edges;
    }

    private void AddNeighbors(List<Vector2Int> all, Vector2Int p, List<Vector2Int> outList)
    {
        foreach (var q in all)
        {
            if (q == p) continue;

            if (!outList.Contains(q)) outList.Add(q);
        }
    }

    private Vector2Int Closest(Vector2Int p, HashSet<Vector2Int> set)
    {
        Vector2Int best = default;
        int bestDist = int.MaxValue;
        foreach (var s in set)
        {
            int d = Mathf.Abs(s.x - p.x) + Mathf.Abs(s.y - p.y);
            if (d < bestDist) { best = s; bestDist = d; }
        }
        return best;
    }

    private RectInt Expand(RectInt r, int ex, int ey) =>
        new RectInt(r.xMin - ex, r.yMin - ey, r.width + ex * 2, r.height + ey * 2);

    private void DrawThickLine(Tilemap tm, TileBase tile, Vector3Int a, Vector3Int b, int thickness)
    {
        if (a.x == b.x)
        {
            int y0 = Mathf.Min(a.y, b.y), y1 = Mathf.Max(a.y, b.y);
            for (int y = y0; y <= y1; y++)
                for (int dx = -thickness / 2; dx <= thickness / 2; dx++)
                    tm.SetTile(new Vector3Int(a.x + dx, y, 0), tile);
        }
        else if (a.y == b.y)
        {
            int x0 = Mathf.Min(a.x, b.x), x1 = Mathf.Max(a.x, b.x);
            for (int x = x0; x <= x1; x++)
                for (int dy = -thickness / 2; dy <= thickness / 2; dy++)
                    tm.SetTile(new Vector3Int(x, a.y + dy, 0), tile);
        }
        else
        {

            DrawThickLine(tm, tile, a, new Vector3Int(b.x, a.y, 0), thickness);
            DrawThickLine(tm, tile, new Vector3Int(b.x, a.y, 0), b, thickness);
        }
    }

    private void DrawRect(Tilemap tm, TileBase tile, RectInt r, int thickness, System.Random rng, float ruinChance)
    {

        for (int x = r.xMin; x < r.xMax; x++)
            for (int t = 0; t < thickness; t++)
            {
                if (rng.NextDouble() > ruinChance) tm.SetTile(new Vector3Int(x, r.yMin + t, 0), tile);
                if (rng.NextDouble() > ruinChance) tm.SetTile(new Vector3Int(x, r.yMax - 1 - t, 0), tile);
            }

        for (int y = r.yMin; y < r.yMax; y++)
            for (int t = 0; t < thickness; t++)
            {
                if (rng.NextDouble() > ruinChance) tm.SetTile(new Vector3Int(r.xMin + t, y, 0), tile);
                if (rng.NextDouble() > ruinChance) tm.SetTile(new Vector3Int(r.xMax - 1 - t, y, 0), tile);
            }
    }

    private void FillRect(Tilemap tm, RectInt r, TileBase tile)
    {
        for (int y = r.yMin; y < r.yMax; y++)
            for (int x = r.xMin; x < r.xMax; x++)
                tm.SetTile(new Vector3Int(x, y, 0), tile);
    }

    private List<RectInt> SubtractRect(RectInt r1, RectInt r2)
    {
        var outRects = new List<RectInt>();
        if (!r1.Overlaps(r2)) { outRects.Add(r1); return outRects; }

        if (r2.yMax < r1.yMax)
            outRects.Add(new RectInt(r1.xMin, r2.yMax, r1.width, r1.yMax - r2.yMax));

        if (r2.yMin > r1.yMin)
            outRects.Add(new RectInt(r1.xMin, r1.yMin, r1.width, r2.yMin - r1.yMin));

        int yMin = Mathf.Max(r1.yMin, r2.yMin);
        int yMax = Mathf.Min(r1.yMax, r2.yMax);
        if (r2.xMin > r1.xMin)
            outRects.Add(new RectInt(r1.xMin, yMin, r2.xMin - r1.xMin, yMax - yMin));

        if (r2.xMax < r1.xMax)
            outRects.Add(new RectInt(r2.xMax, yMin, r1.xMax - r2.xMax, yMax - yMin));

        for (int i = outRects.Count - 1; i >= 0; i--)
            if (outRects[i].width <= 0 || outRects[i].height <= 0)
                outRects.RemoveAt(i);

        return outRects;
    }
}
#endif
