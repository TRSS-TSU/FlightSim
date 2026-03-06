using UnityEngine;

public class NdZoomController : MonoBehaviour
{
    public Camera ndCamera;
    public LocalTileGrid tileGrid;

    // 1 NM = 1852 m. Ortho size is half-height (meters).
    const float NM_TO_M = 1852f;

    [System.Serializable]
    public struct ZoomLevel
    {
        public int z;
        public int radius; // tiles from center
        public float rangeNm; // across (approx square)
    }

    public ZoomLevel z16 = new ZoomLevel
    {
        z = 16,
        radius = 31,
        rangeNm = 20f,
    };
    public ZoomLevel z15 = new ZoomLevel
    {
        z = 15,
        radius = 16,
        rangeNm = 10f,
    };
    public ZoomLevel z14 = new ZoomLevel
    {
        z = 14,
        radius = 8,
        rangeNm = 5f,
    };

    public int active = 16;

    void Start() => Apply(active);

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
            Apply(16);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            Apply(15);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            Apply(14);
    }

    public void Apply(int z)
    {
        ZoomLevel lvl =
            (z == 16) ? z16
            : (z == 15) ? z15
            : z14;
        active = lvl.z;

        if (ndCamera)
            ndCamera.orthographicSize = (lvl.rangeNm * NM_TO_M) / 2f;

        if (tileGrid)
        {
            tileGrid.z = lvl.z;
            tileGrid.radius = lvl.radius;
            tileGrid.Rebuild();
        }
    }
}
