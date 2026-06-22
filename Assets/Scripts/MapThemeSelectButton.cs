using UnityEngine;

public class MapThemeSelectButton : MonoBehaviour
{
    [Header("Theme Selection")]
    [SerializeField]
    private MapTileTheme theme = MapTileTheme.TacticalGray;

    [Header("UI Flow")]
    [SerializeField]
    private GameObject deactivateAfterSelection;

    [SerializeField]
    private GameObject activateAfterSelection;

    public void SelectTheme()
    {
        MapThemeRuntime.Set(theme);

        if (deactivateAfterSelection)
            deactivateAfterSelection.SetActive(false);

        if (activateAfterSelection)
            activateAfterSelection.SetActive(true);
    }
}
