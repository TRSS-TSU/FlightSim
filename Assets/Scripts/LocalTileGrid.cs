using System.Collections;
using System.IO;
using UnityEngine;

/// <summary>
/// Offline ND raster tile streamer.
/// Reads: StreamingAssets/{tilesFolder}/{z}/{x}/{y}.png
/// Places tiles in world meters (1 Unity unit = 1 meter) on the XZ plane (y=0).
/// Pages tiles by updating the center (x,y) index as the focus point crosses tile boundaries.
/// </summary>
public class LocalTileGrid : MonoBehaviour
{
    [Header("References")]
    [Tooltip(
        "Parent transform that holds spawned tiles. Recommended: GroundRoot/TileContainer (Rot 0,0,0 / Scale 1,1,1)."
    )]
    public Transform tileParent;

    [Tooltip("Aircraft transform (fallback focus if ndCamera is not set).")]
    public Transform aircraft;

    [Tooltip(
        "ND camera that renders to the square ND RenderTexture (preferred for sizing + focus)."
    )]
    public Camera ndCamera;

    [Tooltip("ScenarioDefinition providing center lat/lon.")]
    public ScenarioDefinition scenario;

    [Tooltip("Prefab: Quad (1x1) + TileContent component.")]
    public GameObject tilePrefab;

    [Header("Tile Source")]
    [Tooltip("Folder under StreamingAssets containing z/x/y.png structure.")]
    public string tilesFolder = "tiles_nd_dark_v1";

    [Header("Paging / Coverage")]
    [Range(0, 6)]
    public int bufferTiles = 3;
    public float rebuildDelay = 0.05f;
    public bool verboseLogs = true;

    // ---- Public (used by other scripts) ----
    [HideInInspector]
    public int z = 14;

    [HideInInspector]
    public int radius = 10;

    [HideInInspector]
    public float tileSizeM;

    // ---- Internals ----
    int lastRangeNm = 20;

    int scenarioCenterX,
        scenarioCenterY; // scenario center tile at current z
    int centerX,
        centerY; // current paging center
    int lastCenterX,
        lastCenterY;
    bool hasLastCenter;

    // World-space anchor for the scenario center tile (meters)
    Vector3 scenarioOriginWorld;

    Coroutine pending;

    void Start()
    {
        if (scenario != null)
            ApplyScenario();
    }

    public void ApplyScenario()
    {
        if (!scenario)
            return;

        // Anchor: if tileParent exists, treat its position as world origin for scenario center tile.
        Transform p = tileParent ? tileParent : transform;
        scenarioOriginWorld = new Vector3(p.position.x, 0f, p.position.z);

        SetNdRangeNm(lastRangeNm);
    }

    /// <summary>
    /// Range → zoom mapping + coverage sizing based on ND camera frustum at ground (y=0),
    /// using the ND RenderTexture aspect (square RT => aspect 1.0).
    /// </summary>
    public void SetNdRangeNm(int rangeNm)
    {
        if (!scenario)
            return;

        lastRangeNm = rangeNm;

        // Range → zoom mapping (your convention)
        z =
            (rangeNm == 20) ? 13
            : (rangeNm == 10) ? 14
            : (rangeNm == 5) ? 14
            : z;

        tileSizeM = WebMercator.MetersPerTile(scenario.centerLatDeg, z);

        // Scenario center tile must be recomputed for THIS zoom
        LatLonToTileXY(
            scenario.centerLatDeg,
            scenario.centerLonDeg,
            z,
            out scenarioCenterX,
            out scenarioCenterY
        );

        // ---- Frustum footprint sizing ----
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
                neededWidthM = 2f * rangeNm * 1852f; // fallback
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
            tilesAcross += 1; // odd
        radius = (tilesAcross - 1) / 2;

        RecomputeCenterFromFocus(forceResetLast: true);

        if (verboseLogs)
        {
            float nominalWidthM = 2f * rangeNm * 1852f;
            Debug.Log(
                $"[ND-Tiles] range={rangeNm} z={z} tileSizeM={tileSizeM:F2} neededWidthM={neededWidthM:F0} nominalWidthM={nominalWidthM:F0} tilesAcross={tilesAcross} radius={radius}"
            );
        }

        Rebuild();

        if (usedFallback)
            Rebuild(); // will rebuild again after rebuildDelay once camera height is updated
    }

    void Update()
    {
        if (!scenario || tileSizeM <= 0.1f)
            return;

        Vector3 focus = GetFocusPos();
        Vector3 d = focus - scenarioOriginWorld;

        int offX = Mathf.RoundToInt(d.x / tileSizeM);
        int offY = -Mathf.RoundToInt(d.z / tileSizeM); // Unity +Z north, slippy y increases south

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

    void RecomputeCenterFromFocus(bool forceResetLast)
    {
        Vector3 focus = GetFocusPos();
        Vector3 d = focus - scenarioOriginWorld;

        int offX = Mathf.RoundToInt(d.x / tileSizeM);
        int offY = -Mathf.RoundToInt(d.z / tileSizeM);

        centerX = scenarioCenterX + offX;
        centerY = scenarioCenterY + offY;

        if (forceResetLast)
        {
            lastCenterX = centerX;
            lastCenterY = centerY;
            hasLastCenter = true;
        }
    }

    Vector3 GetFocusPos()
    {
        if (ndCamera != null)
            return ndCamera.transform.position;
        if (aircraft != null)
            return aircraft.position;
        return scenarioOriginWorld;
    }

    public void Rebuild()
    {
        if (pending != null)
            StopCoroutine(pending);
        pending = StartCoroutine(RebuildAfterDelay());
    }

    IEnumerator RebuildAfterDelay()
    {
        yield return new WaitForSeconds(rebuildDelay);
        BuildTiles();
        pending = null;
    }

    void BuildTiles()
    {
        int found = 0,
            missing = 0;
        Transform parent = tileParent ? tileParent : transform;

        for (int i = parent.childCount - 1; i >= 0; i--)
            Destroy(parent.GetChild(i).gameObject);

        if (verboseLogs)
        {
            int across = 2 * radius + 1;
            Debug.Log(
                $"[TilePaging] center=({centerX},{centerY}) z={z} rendered={across}x{across} range={lastRangeNm}NM"
            );
        }

        for (int dx = -radius; dx <= radius; dx++)
        for (int dy = -radius; dy <= radius; dy++)
        {
            int x = centerX + dx;
            int y = centerY + dy;

            string path = Path.Combine(
                Application.streamingAssetsPath,
                tilesFolder,
                z.ToString(),
                x.ToString(),
                y + ".png"
            );

            if (!File.Exists(path))
            {
                missing++;
                continue;
            }

            found++;

            GameObject go = Instantiate(tilePrefab, parent);

            int dtx = x - scenarioCenterX;
            int dty = y - scenarioCenterY;

            go.transform.localScale = new Vector3(tileSizeM, tileSizeM, 1f);
            go.transform.position =
                scenarioOriginWorld + new Vector3(dtx * tileSizeM, 0f, -dty * tileSizeM);
            go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            go.name = $"tile z{z} {x}_{y}";

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
        }

        Debug.Log(
            $"[LocalTileGrid] scenario='{(scenario ? scenario.name : "null")}' z={z} center=({centerX},{centerY}) tileSizeM={tileSizeM:F2}m"
        );
        Debug.Log($"[LocalTileGrid] Built tiles: found={found}, missing={missing}, z={z}.");
    }

    static void LatLonToTileXY(double latDeg, double lonDeg, int zoom, out int x, out int y)
    {
        double latRad = latDeg * Mathf.Deg2Rad;
        int n = 1 << zoom;
        x = (int)((lonDeg + 180.0) / 360.0 * n);
        y = (int)(
            (
                1.0
                - System.Math.Log(System.Math.Tan(latRad) + 1.0 / System.Math.Cos(latRad))
                    / System.Math.PI
            )
            / 2.0
            * n
        );
    }
}
