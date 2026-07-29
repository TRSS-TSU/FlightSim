using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Profiling;

public class LocalTileChunkGrid : MonoBehaviour
{
    [Serializable]
    private class ChunkManifest
    {
        public int zoom;
        public int chunkTileSize;
        public ChunkEntry[] chunks;
    }

    [Serializable]
    private class ChunkEntry
    {
        public string file;
        public int xStart;
        public int yStart;
        public int tileCountX;
        public int tileCountY;
    }

    private sealed class LoadedChunk
    {
        public GameObject gameObject;
        public TileContent content;
    }

    [Header("References")]
    public Transform chunkParent;
    public Transform aircraft;
    public NDRangeState rangeState;
    public ScenarioDefinition scenario;
    public GameObject chunkPrefab;

    [Header("Chunk Source")]
    public string chunksFolder = "mapchunks_nd_fms_tactical_gray_z14_4x4";

    [Header("Training Scale")]
    [Tooltip("Must match LocalTileGrid / FlightPlan training scale for map/route alignment.")]
    [Range(0.02f, 1f)]
    public float trainingWorldScale = 0.04f;

    [Header("Paging")]
    [Min(0)]
    public int bufferChunks = 1;

    [Min(1)]
    public int maxResidentChunks = 25;

    [Header("Range Layers")]
    [Min(1)]
    public int nearRangeNm = 2;

    [Header("Flight-Path Prefetch")]
    [Range(0f, 1f)]
    public float forwardPrefetchLead = 0.35f;

    [Header("Loading")]
    public bool autoLoadOnStart = false;

    [Min(1)]
    public int yieldEveryChunks = 2;

    public bool verboseLogs = true;

    [HideInInspector]
    public int z = 14;

    [HideInInspector]
    public float tileSizeM;

    private readonly Dictionary<Vector2Int, ChunkEntry> entriesByKey = new();
    private readonly Dictionary<Vector2Int, LoadedChunk> loadedChunks = new();
    private ChunkManifest manifest;
    private Coroutine pending;
    private Vector2Int lastCenterChunkKey;
    private bool hasLastCenter;
    private int chunkTileSize = 4;
    private int scenarioCenterX;
    private int scenarioCenterY;
    private Vector3 scenarioOriginWorld;
    private int loadCount;
    private int unloadCount;
    private int activeRangeNm = 10;

    public bool IsLoading { get; private set; }
    public bool IsLoaded { get; private set; }
    public int ResidentChunkCount => loadedChunks.Count;
    public long EstimatedResidentTextureBytes { get; private set; }

    private float ScaledTileSizeM => tileSizeM * trainingWorldScale;
    private float ScaledChunkSizeM => ScaledTileSizeM * chunkTileSize;

    private void Awake()
    {
        if (!rangeState)
            rangeState = FindFirstObjectByType<NDRangeState>();

        if (!aircraft)
        {
            LocalTileGrid tileGrid = FindFirstObjectByType<LocalTileGrid>();
            aircraft = tileGrid ? tileGrid.aircraft : null;
        }
    }

    private void OnEnable()
    {
        MapThemeRuntime.OnChanged += HandleThemeChanged;
        if (rangeState != null)
            rangeState.OnRangeChanged += HandleRangeChanged;
    }

    private void OnDisable()
    {
        MapThemeRuntime.OnChanged -= HandleThemeChanged;
        if (rangeState != null)
            rangeState.OnRangeChanged -= HandleRangeChanged;

        if (pending != null)
            StopCoroutine(pending);

        pending = null;
        ReleaseAllChunks();
        manifest = null;
        entriesByKey.Clear();
        hasLastCenter = false;
        IsLoading = false;
        IsLoaded = false;
    }

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
            StopCoroutine(pending);

        ReleaseAllChunks();
        manifest = null;
        entriesByKey.Clear();
        hasLastCenter = false;
        scenario = s;
        activeRangeNm = rangeState != null ? rangeState.CurrentRangeNm : activeRangeNm;
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

        chunksFolder = ResolveChunksFolder(activeRangeNm);
        string manifestPath = Path.Combine(Application.streamingAssetsPath, chunksFolder, "manifest.json");
        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning($"[LocalTileChunkGrid] Missing manifest: {manifestPath}");
            IsLoading = false;
            yield break;
        }

        manifest = JsonUtility.FromJson<ChunkManifest>(File.ReadAllText(manifestPath));
        if (manifest == null || manifest.chunks == null || manifest.chunks.Length == 0)
        {
            Debug.LogWarning($"[LocalTileChunkGrid] Manifest has no chunks: {manifestPath}");
            IsLoading = false;
            yield break;
        }

        scenario = s;
        z = manifest.zoom > 0 ? manifest.zoom : s.baseZoom;
        chunkTileSize = Mathf.Max(1, manifest.chunkTileSize);
        tileSizeM = WebMercator.MetersPerTile(s.centerLatDeg, z);

        Transform parent = chunkParent ? chunkParent : transform;
        scenarioOriginWorld = new Vector3(parent.position.x, 0f, parent.position.z);
        LatLonToTileXY(s.centerLatDeg, s.centerLonDeg, z, out scenarioCenterX, out scenarioCenterY);

        entriesByKey.Clear();
        foreach (ChunkEntry entry in manifest.chunks)
        {
            if (entry != null)
                entriesByKey[new Vector2Int(entry.xStart, entry.yStart)] = entry;
        }

        yield return SynchronizeVisibleChunks(onProgress);
        pending = null;
    }

    private void Update()
    {
        if (!IsLoaded || IsLoading || pending != null || manifest == null || !scenario)
            return;

        Vector2Int center = GetStreamingCenterChunkKey();
        if (!hasLastCenter)
        {
            lastCenterChunkKey = center;
            hasLastCenter = true;
            return;
        }

        if (center != lastCenterChunkKey)
        {
            lastCenterChunkKey = center;
            pending = StartCoroutine(SynchronizeVisibleChunks());
        }
    }

    private IEnumerator SynchronizeVisibleChunks(Action<int, int> onProgress = null)
    {
        IsLoading = true;
        IsLoaded = false;

        Vector2Int center = GetStreamingCenterChunkKey();
        lastCenterChunkKey = center;
        hasLastCenter = true;

        HashSet<Vector2Int> required = BuildRequiredChunkSet(center);
        Transform parent = chunkParent ? chunkParent : transform;

        List<Vector2Int> stale = new();
        foreach (Vector2Int key in loadedChunks.Keys)
        {
            if (!required.Contains(key))
                stale.Add(key);
        }

        foreach (Vector2Int key in stale)
            ReleaseChunk(key);

        int completed = 0;
        int missing = 0;
        int total = required.Count;

        foreach (Vector2Int key in required)
        {
            if (!loadedChunks.ContainsKey(key))
            {
                if (entriesByKey.TryGetValue(key, out ChunkEntry entry) && TryInstantiateChunk(entry, parent, out LoadedChunk loaded))
                {
                    loadedChunks.Add(key, loaded);
                    loadCount++;
                }
                else
                {
                    missing++;
                }
            }

            completed++;
            onProgress?.Invoke(completed, total);

            if (completed % Mathf.Max(1, yieldEveryChunks) == 0)
                yield return null;
        }

        IsLoaded = loadedChunks.Count > 0 && missing == 0;
        IsLoading = false;
        pending = null;
        RecalculateDiagnostics();

        if (verboseLogs)
        {
            Debug.Log(
                $"[LocalTileChunkGrid] Paged loader={chunkTileSize}x{chunkTileSize} " +
                $"range={activeRangeNm} resident={ResidentChunkCount} " +
                $"estimatedTextureBytes={EstimatedResidentTextureBytes} loads={loadCount} " +
                $"unloads={unloadCount} requested={total} missing={missing}"
            );
        }
    }

    private HashSet<Vector2Int> BuildRequiredChunkSet(Vector2Int center)
    {
        int radius = CalculateChunkRadius();
        int maxRadius = Mathf.Max(0, (Mathf.FloorToInt(Mathf.Sqrt(maxResidentChunks)) - 1) / 2);
        if (radius > maxRadius)
        {
            if (verboseLogs)
                Debug.LogWarning(
                    $"[LocalTileChunkGrid] Camera request radius={radius} exceeds resident budget " +
                    $"max={maxResidentChunks}; clamping to radius={maxRadius}."
                );
            radius = maxRadius;
        }

        var result = new HashSet<Vector2Int>();
        Vector2Int step = new(chunkTileSize, chunkTileSize);
        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            Vector2Int key = center + new Vector2Int(dx * step.x, dy * step.y);
            if (entriesByKey.ContainsKey(key))
                result.Add(key);
        }

        return result;
    }

    private int CalculateChunkRadius()
    {
        float neededWidthM = 2f * 20f * 1852f;
        Camera camera = ResolveNdCamera();
        if (camera)
        {
            float camH = Mathf.Abs(camera.transform.position.y);
            float aspect = camera.targetTexture && camera.targetTexture.height > 0
                ? (float)camera.targetTexture.width / camera.targetTexture.height
                : 1f;

            if (camH >= 1f)
            {
                float halfH = Mathf.Tan(camera.fieldOfView * Mathf.Deg2Rad * 0.5f) * camH;
                neededWidthM = Mathf.Max(halfH * 2f, halfH * 2f * aspect);
            }
        }

        float visibleHalfChunks = neededWidthM / (2f * ScaledChunkSizeM);
        return Mathf.Max(0, Mathf.CeilToInt(visibleHalfChunks) + bufferChunks);
    }

    private Vector2Int GetFocusChunkKey()
    {
        Vector3 focus = GetFocusPos();
        Vector3 delta = focus - scenarioOriginWorld;
        int tileX = scenarioCenterX + Mathf.RoundToInt(delta.x / ScaledTileSizeM);
        int tileY = scenarioCenterY - Mathf.RoundToInt(delta.z / ScaledTileSizeM);

        int x = Mathf.FloorToInt((float)(tileX - scenarioCenterX) / chunkTileSize) * chunkTileSize + scenarioCenterX;
        int y = Mathf.FloorToInt((float)(tileY - scenarioCenterY) / chunkTileSize) * chunkTileSize + scenarioCenterY;

        foreach (ChunkEntry entry in manifest.chunks)
        {
            if (entry != null && entry.xStart == x && entry.yStart == y)
                return new Vector2Int(x, y);
        }

        Vector2Int nearest = new(scenarioCenterX, scenarioCenterY);
        float nearestDistance = float.MaxValue;
        foreach (Vector2Int key in entriesByKey.Keys)
        {
            float distance = (key.x - tileX) * (key.x - tileX) + (key.y - tileY) * (key.y - tileY);
            if (distance < nearestDistance)
            {
                nearest = key;
                nearestDistance = distance;
            }
        }

        return nearest;
    }

    private Vector2Int GetStreamingCenterChunkKey()
    {
        Vector2Int current = GetFocusChunkKey();
        Transform flightAircraft = ResolveAircraft();
        Vector2Int forwardStep = GetForwardChunkStep(flightAircraft);
        if (!flightAircraft || forwardStep == Vector2Int.zero)
            return current;

        Vector3 forward = flightAircraft.forward;
        forward.y = 0f;
        forward.Normalize();
        float distanceAhead = Vector3.Dot(GetFocusPos() - ChunkCenterWorld(current), forward);
        float leadDistance = ScaledChunkSizeM * forwardPrefetchLead;
        if (distanceAhead >= (ScaledChunkSizeM * 0.5f) - leadDistance)
        {
            Vector2Int ahead = current + forwardStep;
            if (entriesByKey.ContainsKey(ahead))
                return ahead;
        }

        return current;
    }

    private Vector2Int GetForwardChunkStep(Transform flightAircraft)
    {
        if (!flightAircraft)
            return Vector2Int.zero;

        Vector3 forward = flightAircraft.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f)
            return Vector2Int.zero;

        if (Mathf.Abs(forward.x) >= Mathf.Abs(forward.z))
            return new Vector2Int(forward.x >= 0f ? chunkTileSize : -chunkTileSize, 0);

        return new Vector2Int(0, forward.z >= 0f ? -chunkTileSize : chunkTileSize);
    }

    private Vector3 ChunkCenterWorld(Vector2Int key)
    {
        if (!entriesByKey.TryGetValue(key, out ChunkEntry entry))
            return scenarioOriginWorld;

        float dtx = entry.xStart + (entry.tileCountX * 0.5f) - scenarioCenterX;
        float dty = entry.yStart + (entry.tileCountY * 0.5f) - scenarioCenterY;
        return scenarioOriginWorld + new Vector3(
            dtx * ScaledTileSizeM,
            0f,
            -dty * ScaledTileSizeM
        );
    }

    private bool TryInstantiateChunk(ChunkEntry chunk, Transform parent, out LoadedChunk loaded)
    {
        loaded = null;
        if (chunk == null || string.IsNullOrWhiteSpace(chunk.file))
            return false;

        string path = Path.Combine(Application.streamingAssetsPath, chunksFolder, chunk.file.Replace("/", Path.DirectorySeparatorChar.ToString()));
        if (!File.Exists(path))
            return false;

        GameObject go = Instantiate(chunkPrefab, parent);
        float chunkCenterTileX = chunk.xStart + (chunk.tileCountX * 0.5f);
        float chunkCenterTileY = chunk.yStart + (chunk.tileCountY * 0.5f);
        float dtx = chunkCenterTileX - scenarioCenterX;
        float dty = chunkCenterTileY - scenarioCenterY;
        float widthM = chunk.tileCountX * ScaledTileSizeM;
        float depthM = chunk.tileCountY * ScaledTileSizeM;

        go.transform.localScale = new Vector3(widthM, depthM, 1f);
        go.transform.position = scenarioOriginWorld + new Vector3(dtx * ScaledTileSizeM, 0f, -dty * ScaledTileSizeM);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.name = $"chunk z{z} {chunk.xStart}_{chunk.yStart}";

        TileContent content = go.GetComponent<TileContent>();
        if (!content)
        {
            Destroy(go);
            return false;
        }

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D texture = new(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes, true))
        {
            Destroy(texture);
            Destroy(go);
            return false;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        content.SetTexture(texture);
        loaded = new LoadedChunk { gameObject = go, content = content };
        return true;
    }

    private void ReleaseChunk(Vector2Int key)
    {
        if (!loadedChunks.TryGetValue(key, out LoadedChunk loaded))
            return;

        loaded.content?.ReleaseRuntimeAssets();
        if (loaded.gameObject)
            Destroy(loaded.gameObject);

        loadedChunks.Remove(key);
        unloadCount++;
    }

    private void ReleaseAllChunks()
    {
        foreach (Vector2Int key in new List<Vector2Int>(loadedChunks.Keys))
            ReleaseChunk(key);

        EstimatedResidentTextureBytes = 0;
    }

    private void RecalculateDiagnostics()
    {
        long total = 0;
        foreach (LoadedChunk loaded in loadedChunks.Values)
        {
            if (loaded.content && loaded.content.RuntimeTexture)
                total += Profiler.GetRuntimeMemorySizeLong(loaded.content.RuntimeTexture);
        }

        EstimatedResidentTextureBytes = total;
    }

    private void HandleThemeChanged(MapTileTheme _)
    {
        if (isActiveAndEnabled && scenario)
            LoadScenario(scenario);
    }

    private void HandleRangeChanged(int rangeNm)
    {
        bool layerChanged = IsFarRange(activeRangeNm) != IsFarRange(rangeNm);
        activeRangeNm = rangeNm;
        if (layerChanged && isActiveAndEnabled && scenario)
            LoadScenario(scenario);
    }

    private string ResolveChunksFolder(int rangeNm)
    {
        return IsFarRange(rangeNm)
            ? MapThemeRuntime.GetFarTileChunksFolder()
            : MapThemeRuntime.GetTileChunksFolder();
    }

    private bool IsFarRange(int rangeNm) => rangeNm > nearRangeNm;

    private Transform ResolveAircraft()
    {
        if (aircraft)
            return aircraft;

        LocalTileGrid tileGrid = FindFirstObjectByType<LocalTileGrid>();
        return tileGrid ? tileGrid.aircraft : null;
    }

    private Camera ResolveNdCamera()
    {
        LocalTileGrid tileGrid = FindFirstObjectByType<LocalTileGrid>();
        return tileGrid ? tileGrid.ndCamera : null;
    }

    private Vector3 GetFocusPos()
    {
        Camera camera = ResolveNdCamera();
        if (camera)
            return camera.transform.position;

        LocalTileGrid tileGrid = FindFirstObjectByType<LocalTileGrid>();
        if (tileGrid && tileGrid.aircraft)
            return tileGrid.aircraft.position;

        return scenarioOriginWorld;
    }

    public static void LatLonToTileXY(double latDeg, double lonDeg, int zoom, out int x, out int y)
    {
        double latRad = latDeg * Math.PI / 180.0;
        int n = 1 << zoom;
        double xf = (lonDeg + 180.0) / 360.0 * n;
        double yf = (1.0 - Math.Log(Math.Tan(latRad) + (1.0 / Math.Cos(latRad))) / Math.PI) / 2.0 * n;
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
