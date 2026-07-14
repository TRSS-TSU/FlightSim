using UnityEngine;

public class ImageVisibilityController : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private GameObject imageObject;

    [Header("Initial State")]
    [SerializeField] private bool visibleOnStart = true;

    private void Start()
    {
        SetImageVisible(visibleOnStart);
    }

    public void ToggleImage()
    {
        if (imageObject == null)
        {
            Debug.LogWarning(
                $"{nameof(ImageVisibilityController)}: No image object assigned.",
                this);

            return;
        }

        imageObject.SetActive(!imageObject.activeSelf);
    }

    public void ShowImage()
    {
        SetImageVisible(true);
    }

    public void HideImage()
    {
        SetImageVisible(false);
    }

    private void SetImageVisible(bool visible)
    {
        if (imageObject != null)
            imageObject.SetActive(visible);
    }
}