using UnityEngine;

[DefaultExecutionOrder(-90)]
public class MapRasterModeCoordinator : MonoBehaviour
{
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

    [Header("Debug")]
    [SerializeField]
    private bool verboseLogs = true;

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

        ApplyMode(scenario);
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

        if (verboseLogs)
        {
            Debug.Log(
                $"[MapRasterModeCoordinator] Mode=IndividualTiles folder={MapThemeRuntime.GetTilesFolder()}"
            );
        }
    }

    private void ApplyStitchedChunks(ScenarioDefinition scenario)
    {
        if (individualTileVisualRoot)
            individualTileVisualRoot.SetActive(false);

        if (chunkVisualRoot)
            chunkVisualRoot.SetActive(true);

        if (individualTileGrid)
        {
            individualTileGrid.enabled = true;
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
        chunkGrid.LoadScenario(scenario);

        if (verboseLogs)
        {
            Debug.Log(
                $"[MapRasterModeCoordinator] Mode=StitchedChunks folder={MapThemeRuntime.GetTileChunksFolder()}"
            );
        }
    }
}
