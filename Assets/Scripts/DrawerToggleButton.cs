using UnityEngine;

public class DrawerToggleButton : MonoBehaviour
{
    private enum DrawerKind
    {
        Auto,
        Fms,
        Map,
    }

    [Header("Drawer Controller")]
    [SerializeField]
    private MonoBehaviour controller;

    [SerializeField]
    private DrawerKind drawerKind = DrawerKind.Auto;

    [Header("Action")]
    [SerializeField]
    private bool openWhenPressed = true;

    [Header("This Drawer Buttons")]
    [SerializeField]
    private GameObject thisOpenButton;

    [SerializeField]
    private GameObject thisCloseButton;

    [Header("Other Drawer Buttons")]
    [SerializeField]
    private GameObject otherOpenButton;

    [SerializeField]
    private GameObject otherCloseButton;

    private void Awake()
    {
        ResolveController();
    }

    public void Press()
    {
        MonoBehaviour resolvedController = ResolveController();
        if (!resolvedController)
        {
            Debug.LogError(
                $"[DrawerToggleButton] Missing {GetExpectedControllerName()} on button panel.",
                this
            );
            return;
        }

        resolvedController.SendMessage("RecalculateTargets", SendMessageOptions.DontRequireReceiver);

        if (openWhenPressed)
        {
            resolvedController.SendMessage("SnapOpen", SendMessageOptions.RequireReceiver);
            SetThisDrawerOpen(true);

            // DrawerGroup closes the other drawer, so update its buttons too.
            SetOtherDrawerOpen(false);
        }
        else
        {
            resolvedController.SendMessage("SnapClosed", SendMessageOptions.RequireReceiver);
            SetThisDrawerOpen(false);
        }
    }

    private MonoBehaviour ResolveController()
    {
        if (IsValidController(controller))
            return controller;

        DrawerKind expectedKind = ResolveDrawerKind();
        controller = FindController(expectedKind);
        return controller;
    }

    private bool IsValidController(MonoBehaviour candidate)
    {
        return candidate is IDrawerController;
    }

    private DrawerKind ResolveDrawerKind()
    {
        if (drawerKind != DrawerKind.Auto)
            return drawerKind;

        string key = (
            name
            + " "
            + GetHierarchyPath(transform)
            + " "
            + (thisOpenButton ? thisOpenButton.name : "")
            + " "
            + (thisCloseButton ? thisCloseButton.name : "")
        ).ToLowerInvariant();

        if (key.Contains("fms"))
            return DrawerKind.Fms;

        if (key.Contains("map"))
            return DrawerKind.Map;

        return DrawerKind.Auto;
    }

    private MonoBehaviour FindController(DrawerKind expectedKind)
    {
        switch (expectedKind)
        {
            case DrawerKind.Fms:
                return FindFirstController<FmsDrawerController>();

            case DrawerKind.Map:
                return FindFirstController<MapDrawerController>();

            default:
                return null;
        }
    }

    private static MonoBehaviour FindFirstController<T>()
        where T : MonoBehaviour, IDrawerController
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<T>(FindObjectsInactive.Include);
#else
        return FindObjectOfType<T>(true);
#endif
    }

    private string GetExpectedControllerName()
    {
        switch (ResolveDrawerKind())
        {
            case DrawerKind.Fms:
                return nameof(FmsDrawerController);

            case DrawerKind.Map:
                return nameof(MapDrawerController);

            default:
                return "drawer controller";
        }
    }

    private static string GetHierarchyPath(Transform t)
    {
        if (!t)
            return "";

        string path = t.name;
        while (t.parent)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }

        return path;
    }

    private void SetThisDrawerOpen(bool isOpen)
    {
        if (thisOpenButton)
            thisOpenButton.SetActive(!isOpen);
        if (thisCloseButton)
            thisCloseButton.SetActive(isOpen);
    }

    private void SetOtherDrawerOpen(bool isOpen)
    {
        if (otherOpenButton)
            otherOpenButton.SetActive(!isOpen);
        if (otherCloseButton)
            otherCloseButton.SetActive(isOpen);
    }
}
