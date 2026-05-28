using UnityEngine;

public class DrawerToggleButton : MonoBehaviour
{
    [Header("Drawer Controller")]
    [SerializeField]
    private MonoBehaviour controller;

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

    public void Press()
    {
        if (!controller)
        {
            Debug.LogError("[DrawerToggleButton] Missing controller.", this);
            return;
        }

        controller.SendMessage("RecalculateTargets", SendMessageOptions.DontRequireReceiver);

        if (openWhenPressed)
        {
            controller.SendMessage("SnapOpen", SendMessageOptions.RequireReceiver);
            SetThisDrawerOpen(true);

            // DrawerGroup closes the other drawer, so update its buttons too.
            SetOtherDrawerOpen(false);
        }
        else
        {
            controller.SendMessage("SnapClosed", SendMessageOptions.RequireReceiver);
            SetThisDrawerOpen(false);
        }
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
