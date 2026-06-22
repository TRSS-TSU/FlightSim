using UnityEngine;

public static class DrawerGroup
{
    private static MonoBehaviour currentOpen;

    public static void RequestOpen(IDrawerController requester)
    {
        var requesterMb = requester as MonoBehaviour;
        if (!requesterMb)
            return;

        if (currentOpen && currentOpen != requesterMb)
        {
            if (currentOpen is IDrawerController prev)
                prev.SnapClosed();
        }

        currentOpen = requesterMb;
        Debug.Log($"RequestOpen: {requesterMb.name}");
    }

    public static void NotifyClosed(IDrawerController requester)
    {
        var requesterMb = requester as MonoBehaviour;
        if (!requesterMb)
            return;

        if (currentOpen == requesterMb)
            currentOpen = null;
    }

    public static void Clear()
    {
        currentOpen = null;
    }
}
