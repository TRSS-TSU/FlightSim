using UnityEngine;

public static class DrawerGroup
{
    private static IDrawerController currentOpen;

    public static void RequestOpen(IDrawerController requester)
    {
        var prev = currentOpen;
        if (currentOpen != null && currentOpen != requester)
            currentOpen.SnapClosed();

        currentOpen = requester;
        Debug.Log($"RequestOpen: {requester} (was {prev})");
    }

    public static void NotifyClosed(IDrawerController requester)
    {
        if (currentOpen == requester)
            currentOpen = null;
    }
}
