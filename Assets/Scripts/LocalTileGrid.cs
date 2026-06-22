using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public enum TileLoadMode
{
    DisabledAtStartup,
    FixedRoutePreload,
    RuntimePagingLegacy,
}

/// <summary>
/// Offline ND raster tile renderer.
/// Reads StreamingAssets/{tilesFolder}/{z}/{x}/{y}.png and places tiles in world meters on y=0.
/// </summary>
public class LocalTileGrid : MonoBehaviour
{
    [Header("Load Mode")]
    public TileLoadMode loadMode = TileLoadMode.FixedRoutePreload;

    [Header("References")]
    public Transform tileParent;
    public Transform aircraft;
    public Camera ndCamera;
    public ScenarioDefinition scenario;
    public GameObject tilePrefab;

    [Header("Tile Source")]
    public string tilesFolder = "tiles_nd_fms_v2";

    [Header("Paging / Coverage")]
    [Range(0, 6)]
    public int bufferTiles = 3;
    public float rebuildDelay = 0.05f;

    [Header("Training Scale")]
    [Tooltip("Compresses map tile spacing/size for training gameplay. Do not scale TileContainer.")]
    [Range(0.02f, 1f)]
    public float trainingWorldScale = 0.04f;

    public bool verboseLogs = true;

    [HideInInspector]
    public int z = 14;

    [HideInInspector]
    public int radius = 10;

    [HideInInspector]
    public float tileSizeM;

    private int lastRangeNm = 20;

    private int scenarioCenterX;
    private int scenarioCenterY;
    private int centerX;
    private int centerY;
    private int lastCenterX;
    private int lastCenterY;
    private bool hasLastCenter;

    private Vector3 scenarioOriginWorld;
    private Coroutine pending;

    private float ScaledTileSizeM => tileSizeM * trainingWorldScale;

    private void Start()
    {
        if (scenario != null && loadMode != TileLoadMode.DisabledAtStartup)
            ApplyScenario();
    }

    public void ApplyScenario()
    {
        if (!scenario)
            return;

        Transform p = tileParent ? tileParent : transform;
        scenarioOriginWorld = new Vector3(p.position.x, 0f, p.position.z);

        SetNdRangeNm(lastRangeNm);
    }

    public void SetNdRangeNm(int rangeNm)
    {
        if (!scenario)
            return;

        lastRangeNm = rangeNm;

        z =
            (rangeNm == 20) ? 14
            : (rangeNm == 10) ? 14
            : (rangeNm == 5) ? 14
            : z;

        tileSizeM = WebMercator.MetersPerTile(scenario.centerLatDeg, z);

        LatLonToTileXY(
            scenario.centerLatDeg,
            scenario.centerLonDeg,
            z,
            out scenarioCenterX,
            out scenarioCenterY
        );

        float neededWidthM;
        bool usedFallback = false;

        if (ndCamera != null)
        {
            float camH = Mathf.Abs(ndCamera.transform.position.y);
            float aspect = 1f;
            if (ndCamera.targetTexture != null && ndCamera.targetTexture.height > 0)
                aspect = (float)ndCamera.targetTexture.width / ndCamera.targetTexture.height;

            if (camH < 1f)
            {
                usedFallback = true;
                neededWidthM = 2f * rangeNm * 1852f;
            }
            else
            {
                float halfH = Mathf.Tan(ndCamera.fieldOfView * Mathf.Deg2Rad * 0.5f) * camH;
                float fullH = halfH * 2f;
                float fullW = fullH * aspect;
                neededWidthM = Mathf.Max(fullH, fullW);
            }

            if (verboseLogs)
                Debug.Log(
                    $"[ND-Frustum] rtAspect={aspect:F3} camH={camH:F0} usedFallback={usedFallback} neededWidthM={neededWidthM:F0}"
                );
        }
        else
        {
            neededWidthM = 2f * rangeNm * 1852f;
        }

        int neededAcross = Mathf.CeilToInt(neededWidthM / tileSizeM);
        int tilesAcross = neededAcross + bufferTiles * 2;
        if ((tilesAcross & 1) == 0)
            tilesAcross += 1;
        radius = (tilesAcross - 1) / 2;

        RecomputeCenterFromFocus(forceResetLast: true);

        if (loadMode != TileLoadMode.RuntimePagingLegacy)
            return;

        Rebuild();

        if (usedFallback)
            Rebuild();
    }

    private void Update()
    {
        if (loadMode != TileLoadMode.RuntimePagingLegacy)
            return;
        if (!scenario || tileSizeM <= 0.1f)
            return;

        Vector3 focus = GetFocusPos();
        Vector3 d = focus - scenarioOriginWorld;

        int offX = Mathf.RoundToInt(d.x / ScaledTileSizeM);
        int offY = -Mathf.RoundToInt(d.z / ScaledTileSizeM);

        int cx = scenarioCenterX + offX;
        int cy = scenarioCenterY + offY;

        if (!hasLastCenter)
        {
            centerX = cx;
            centerY = cy;
            lastCenterX = cx;
            lastCenterY = cy;
            hasLastCenter = true;
            return;
        }

        if (cx != lastCenterX || cy != lastCenterY)
        {
            centerX = cx;
            centerY = cy;
            lastCenterX = cx;
            lastCenterY = cy;
            Rebuild();
        }
    }

    public IEnumerator BuildFixedTileSet(
        ScenarioDefinition scenario,
        IReadOnlyList<Vector2Int> tileIndexes,
        int zoom,
        Action<int, int> onProgress = null
    )
    {
        if (!scenario || tileIndexes == null)
            yield break;

        Debug.Log(
            $"[LocalTileGrid] BuildFixedTileSet ENTER folder={tilesFolder} zoom={zoom} "
                + $"tileIndexes={(tileIndexes == null ? -1 : tileIndexes.Count)} loadMode={loadMode}"
        );

        if (pending != null)
        {
            StopCoroutine(pending);
            pending = null;
        }

        this.scenario = scenario;
        z = zoom;
        tileSizeM = WebMercator.MetersPerTile(scenario.centerLatDeg, z);

        Transform parent = tileParent ? tileParent : transform;
        scenarioOriginWorld = new Vector3(parent.position.x, 0f, parent.position.z);

        LatLonToTileXY(
            scenario.centerLatDeg,
            scenario.centerLonDeg,
            z,
            out scenarioCenterX,
            out scenarioCenterY
        );

        ClearTiles(parent);

        int found = 0;
        int missing = 0;
        int total = tileIndexes.Count;

        if (verboseLogs)
            Debug.Log($"[LocalTileGrid] Fixed preload started: {total} requested tiles");

        for (int i = 0; i < total; i++)
        {
            Vector2Int tile = tileIndexes[i];
            if (TryInstantiateTile(tile.x, tile.y, z, parent))
                found++;
            else
                missing++;

            onProgress?.Invoke(i + 1, total);

            if ((i + 1) % 8 == 0)
                yield return null;
        }

        if (verboseLogs)
            Debug.Log($"[LocalTileGrid] Fixed preload complete: found={found} missing={missing}");
    }

    public void Rebuild()
    {
        if (loadMode != TileLoadMode.RuntimePagingLegacy)
            return;

        if (pending != null)
            StopCoroutine(pending);
        pending = StartCoroutine(RebuildAfterDelay());
    }

    private IEnumerator RebuildAfterDelay()
    {
        yield return new WaitForSeconds(rebuildDelay);

        if (loadMode == TileLoadMode.RuntimePagingLegacy)
            BuildTiles();

        pending = null;
    }

    private void BuildTiles()
    {
        int found = 0;
        int missing = 0;
        Transform parent = tileParent ? tileParent : transform;

        ClearTiles(parent);

        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            int x = centerX + dx;
            int y = centerY + dy;

            if (TryInstantiateTile(x, y, z, parent))
                found++;
            else
                missing++;
        }

        if (verboseLogs)
            Debug.Log($"[LocalTileGrid] Legacy rebuild complete: found={found} missing={missing}");
    }

    private bool TryInstantiateTile(int x, int y, int zoom, Transform parent)
    {
        if (!tilePrefab)
        {
            if (verboseLogs)
                Debug.LogWarning("[LocalTileGrid] tilePrefab is not assigned.");
            return false;
        }

        string path = Path.Combine(
            Application.streamingAssetsPath,
            tilesFolder,
            zoom.ToString(),
            x.ToString(),
            y + ".png"
        );

        if (!File.Exists(path))
            return false;

        GameObject go = Instantiate(tilePrefab, parent);

        int dtx = x - scenarioCenterX;
        int dty = y - scenarioCenterY;

        go.transform.localScale = new Vector3(ScaledTileSizeM, ScaledTileSizeM, 1f);
        go.transform.position =
            scenarioOriginWorld + new Vector3(dtx * ScaledTileSizeM, 0f, -dty * ScaledTileSizeM);
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        go.name = $"tile z{zoom} {x}_{y}";

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

    private void ClearTiles(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);
    }

    private void RecomputeCenterFromFocus(bool forceResetLast)
    {
        Vector3 focus = GetFocusPos();
        Vector3 d = focus - scenarioOriginWorld;

        int offX = Mathf.RoundToInt(d.x / ScaledTileSizeM);
        int offY = -Mathf.RoundToInt(d.z / ScaledTileSizeM);

        centerX = scenarioCenterX + offX;
        centerY = scenarioCenterY + offY;

        if (forceResetLast)
        {
            lastCenterX = centerX;
            lastCenterY = centerY;
            hasLastCenter = true;
        }
    }

    private Vector3 GetFocusPos()
    {
        if (ndCamera != null)
            return ndCamera.transform.position;
        if (aircraft != null)
            return aircraft.position;
        return scenarioOriginWorld;
    }

    public static void LatLonToTileXY(double latDeg, double lonDeg, int zoom, out int x, out int y)
    {
        double latRad = latDeg * Mathf.Deg2Rad;
        int n = 1 << zoom;
        x = (int)((lonDeg + 180.0) / 360.0 * n);
        y = (int)((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);
    }
}
