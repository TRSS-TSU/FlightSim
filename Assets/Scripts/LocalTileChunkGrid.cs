using System;
using System.Collections;
using System.IO;
using UnityEngine;

public class LocalTileChunkGrid : MonoBehaviour
{
    [Serializable]
    private class ChunkManifest
    {
        public string theme;
        public int zoom;
        public int sourceTileSizePx;
        public int chunkTileSize;
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;
        public int chunksWide;
        public int chunksTall;
        public int expectedChunks;
        public int writtenChunks;
        public int missingSourceTileCount;
        public ChunkEntry[] chunks;
    }

    [Serializable]
    private class ChunkEntry
    {
        public string file;
        public int xStart;
        public int xEnd;
        public int yStart;
        public int yEnd;
        public int tileCountX;
        public int tileCountY;
        public int widthPx;
        public int heightPx;
        public bool hasMissingSourceTiles;
    }

    [Header("References")]
    public Transform chunkParent;
    public ScenarioDefinition scenario;
    public GameObject chunkPrefab;

    [Header("Chunk Source")]
    public string chunksFolder = "mapchunks_nd_fms_tactical_gray_z14_16x16";

    [Header("Training Scale")]
    [Tooltip("Must match LocalTileGrid / FlightPlan training scale for map/route alignment.")]
    [Range(0.02f, 1f)]
    public float trainingWorldScale = 0.04f;

    [Header("Loading")]
    public bool autoLoadOnStart = false;

    [Min(1)]
    public int yieldEveryChunks = 2;

    public bool verboseLogs = true;

    [HideInInspector]
    public int z = 14;

    [HideInInspector]
    public float tileSizeM;

    private int scenarioCenterX;
    private int scenarioCenterY;
    private Vector3 scenarioOriginWorld;
    private Coroutine pending;

    public bool IsLoading { get; private set; }
    public bool IsLoaded { get; private set; }

    private float ScaledTileSizeM => tileSizeM * trainingWorldScale;

    private void Start()
    {
        if (!autoLoadOnStart)
            return;

        ScenarioDefinition s = scenario ? scenario : ScenarioRuntime.Current;
        if (s)
            LoadScenario(s);
    }

    public void LoadScenario(ScenarioDefinition s)
    {
        if (!s)
        {
            Debug.LogWarning("[LocalTileChunkGrid] Load skipped: scenario is missing.");
            return;
        }

        if (pending != null)
        {
            StopCoroutine(pending);
            pending = null;
        }

        IsLoading = false;
        IsLoaded = false;
        pending = StartCoroutine(BuildChunks(s));
    }

    public IEnumerator BuildChunks(ScenarioDefinition s, Action<int, int> onProgress = null)
    {
        if (!s)
            yield break;

        IsLoading = true;
        IsLoaded = false;

        if (!chunkPrefab)
        {
            Debug.LogWarning("[LocalTileChunkGrid] chunkPrefab is not assigned.");
            IsLoading = false;
            yield break;
        }

        chunksFolder = MapThemeRuntime.GetTileChunksFolder();

        string manifestPath = Path.Combine(
            Application.streamingAssetsPath,
            chunksFolder,
            "manifest.json"
        );

        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning($"[LocalTileChunkGrid] Missing manifest: {manifestPath}");
            IsLoading = false;
            yield break;
        }

        string manifestJson = File.ReadAllText(manifestPath);
        ChunkManifest manifest = JsonUtility.FromJson<ChunkManifest>(manifestJson);

        if (manifest == null || manifest.chunks == null || manifest.chunks.Length == 0)
        {
            Debug.LogWarning($"[LocalTileChunkGrid] Manifest has no chunks: {manifestPath}");
            IsLoading = false;
            yield break;
        }

        scenario = s;
        z = manifest.zoom > 0 ? manifest.zoom : s.baseZoom;
        tileSizeM = WebMercator.MetersPerTile(s.centerLatDeg, z);

        Transform parent = chunkParent ? chunkParent : transform;
        scenarioOriginWorld = new Vector3(parent.position.x, 0f, parent.position.z);

        LatLonToTileXY(s.centerLatDeg, s.centerLonDeg, z, out scenarioCenterX, out scenarioCenterY);

        ClearChunks(parent);

        int found = 0;
        int missing = 0;
        int total = manifest.chunks.Length;

        for (int i = 0; i < total; i++)
        {
            ChunkEntry chunk = manifest.chunks[i];

            if (TryInstantiateChunk(chunk, parent))
                found++;
            else
                missing++;

            onProgress?.Invoke(i + 1, total);

            if ((i + 1) % yieldEveryChunks == 0)
                yield return null;
        }

        IsLoaded = found > 0 && missing == 0;
        IsLoading = false;
        pending = null;
    }

    private bool TryInstantiateChunk(ChunkEntry chunk, Transform parent)
    {
        if (chunk == null || string.IsNullOrWhiteSpace(chunk.file))
            return false;

        string path = Path.Combine(
            Application.streamingAssetsPath,
            chunksFolder,
            chunk.file.Replace("/", Path.DirectorySeparatorChar.ToString())
        );

        if (!File.Exists(path))
            return false;

        GameObject go = Instantiate(chunkPrefab, parent);

        float chunkCenterTileX = (chunk.xStart + chunk.xEnd) * 0.5f;
        float chunkCenterTileY = (chunk.yStart + chunk.yEnd) * 0.5f;

        float dtx = chunkCenterTileX - scenarioCenterX;
        float dty = chunkCenterTileY - scenarioCenterY;

        float widthM = chunk.tileCountX * ScaledTileSizeM;
        float depthM = chunk.tileCountY * ScaledTileSizeM;

        go.transform.localScale = new Vector3(widthM, depthM, 1f);
        go.transform.position =
            scenarioOriginWorld + new Vector3(dtx * ScaledTileSizeM, 0f, -dty * ScaledTileSizeM);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.name = $"chunk z{z} {chunk.xStart}_{chunk.yStart}";

        TileContent tc = go.GetComponent<TileContent>();
        if (tc != null)
        {
            byte[] bytes = File.ReadAllBytes(path);

            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            tex.LoadImage(bytes);
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.filterMode = FilterMode.Bilinear;

            tc.SetTexture(tex);
        }

        return true;
    }

    private void ClearChunks(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    public static void LatLonToTileXY(double latDeg, double lonDeg, int zoom, out int x, out int y)
    {
        double latRad = latDeg * Math.PI / 180.0;
        int n = 1 << zoom;

        double xf = (lonDeg + 180.0) / 360.0 * n;
        double yf =
            (1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n;

        x = Mathf.Clamp((int)Math.Floor(xf), 0, n - 1);
        y = Mathf.Clamp((int)Math.Floor(yf), 0, n - 1);
    }

    [ContextMenu("TEST Load Current Scenario Chunks")]
    private void TestLoadCurrentScenarioChunks()
    {
        ScenarioDefinition s = scenario ? scenario : ScenarioRuntime.Current;

        if (!s)
        {
            Debug.LogWarning("[LocalTileChunkGrid] TEST failed: no scenario assigned or active.");
            return;
        }

        LoadScenario(s);
    }
}
