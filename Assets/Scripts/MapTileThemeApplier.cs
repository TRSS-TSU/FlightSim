using UnityEngine;

[DefaultExecutionOrder(-100)]
public class MapTileThemeApplier : MonoBehaviour
{
    [SerializeField]
    private LocalTileGrid tileGrid;

    private void Awake()
    {
        if (!tileGrid)
        {
            tileGrid = FindFirstObjectByType<LocalTileGrid>();
        }

        if (!tileGrid)
        {
            Debug.LogWarning(
                "[MapTileThemeApplier] No LocalTileGrid found. Map theme not applied."
            );
            return;
        }

        string folder = MapThemeRuntime.GetTilesFolder();
        tileGrid.tilesFolder = folder;
    }
}
