using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MapLoadingOverlay : MonoBehaviour
{
    public GameObject root;
    public TMP_Text statusText;
    public TMP_Text progressText;
    public Slider progressSlider;

    private void Awake()
    {
        Hide();
    }

    public void Show(string message)
    {
        if (root)
            root.SetActive(true);
        if (statusText)
            statusText.SetText(message ?? "");
        SetProgress(0, 0);
    }

    public void SetProgress(int loaded, int total)
    {
        if (progressText)
            progressText.SetText(total > 0 ? $"{loaded} / {total}" : "");

        if (progressSlider)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = Mathf.Max(1, total);
            progressSlider.value = Mathf.Clamp(loaded, 0, Mathf.Max(1, total));
        }
    }

    public void Hide()
    {
        if (root)
            root.SetActive(false);
    }
}
