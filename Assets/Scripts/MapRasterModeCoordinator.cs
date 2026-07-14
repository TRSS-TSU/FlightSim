using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-90)]
public class MapRasterModeCoordinator : MonoBehaviour
{
    public bool PreviewLoadFinished { get; private set; }

    [Header("Scenario")]
    [SerializeField]
    private ScenarioDefinition fallbackScenario;

    [Header("Individual Tile System")]
    [SerializeField]
    private LocalTileGrid individualTileGrid;

    [SerializeField]
    private GameObject individualTileVisualRoot;

    [Header("Stitched Chunk System")]
    [SerializeField]
    private LocalTileChunkGrid chunkGrid;

    [SerializeField]
    private GameObject chunkVisualRoot;

    [SerializeField]
    private MapLoadingOverlay loadingOverlay;

    private void Start()
    {
        ScenarioDefinition scenario = ScenarioRuntime.Current
            ? ScenarioRuntime.Current
            : fallbackScenario;

        if (!scenario)
        {
            Debug.LogWarning(
                "[MapRasterModeCoordinator] No scenario available. Map raster mode not applied."
            );
            return;
        }

        if (ScenarioRuntime.IsPreview)
            MapThemeRuntime.Set(MapTileTheme.TacticalGray);

        if (scenario.mapRasterLoadMode == MapRasterLoadMode.StitchedChunks)
        {
            StartCoroutine(LoadChunkMap(
                scenario,
                ScenarioRuntime.IsPreview ? "Loading preview map..." : "Loading map..."
            ));
            return;
        }

        ApplyMode(scenario);
    }

    private IEnumerator LoadChunkMap(ScenarioDefinition scenario, string loadingMessage)
    {
        if (!scenario || !chunkGrid)
        {
            Debug.LogWarning("[MapRasterModeCoordinator] Chunk map unavailable: missing scenario or chunk grid.");
            PreviewLoadFinished = true;
            yield break;
        }

        ApplyStitchedChunks(scenario);

        loadingOverlay?.Show(loadingMessage);
        yield return chunkGrid.BuildChunks(
            scenario,
            (loaded, total) => loadingOverlay?.SetProgress(loaded, total)
        );
        loadingOverlay?.Hide();
        PreviewLoadFinished = true;

        if (!chunkGrid.IsLoaded)
            Debug.LogWarning("[MapRasterModeCoordinator] Chunk map did not complete loading.");
    }

    public void ApplyMode(ScenarioDefinition scenario)
    {
        if (!scenario)
        {
            Debug.LogWarning("[MapRasterModeCoordinator] ApplyMode skipped: scenario is missing.");
            return;
        }

        switch (scenario.mapRasterLoadMode)
        {
            case MapRasterLoadMode.StitchedChunks:
                ApplyStitchedChunks(scenario);
                break;

            case MapRasterLoadMode.IndividualTiles:
            default:
                ApplyIndividualTiles(scenario);
                break;
        }
    }

    private void ApplyIndividualTiles(ScenarioDefinition scenario)
    {
        if (individualTileVisualRoot)
            individualTileVisualRoot.SetActive(true);

        if (chunkVisualRoot)
            chunkVisualRoot.SetActive(false);

        if (individualTileGrid)
        {
            individualTileGrid.enabled = true;
            individualTileGrid.tilesFolder = MapThemeRuntime.GetTilesFolder();
        }

        if (chunkGrid)
            chunkGrid.enabled = false;

    }

    private void ApplyStitchedChunks(ScenarioDefinition scenario)
    {
        if (individualTileVisualRoot)
            individualTileVisualRoot.SetActive(false);

        if (chunkVisualRoot)
            chunkVisualRoot.SetActive(true);

        if (individualTileGrid)
        {
            individualTileGrid.enabled = false;
            individualTileGrid.tilesFolder = MapThemeRuntime.GetTilesFolder();
        }

        if (!chunkGrid)
        {
            Debug.LogWarning(
                "[MapRasterModeCoordinator] Chunk mode selected, but chunkGrid is not assigned."
            );
            return;
        }

        chunkGrid.enabled = true;
        chunkGrid.chunksFolder = MapThemeRuntime.GetTileChunksFolder();

    }
}
