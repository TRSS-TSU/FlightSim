using System;
using UnityEngine;

public static class MapThemeRuntime
{
    public static MapTileTheme Current { get; private set; } = MapTileTheme.TacticalGray;

    public static event Action<MapTileTheme> OnChanged;

    public static void Set(MapTileTheme theme)
    {
        Current = theme;
        Debug.Log($"[MapThemeRuntime] Selected map theme: {theme}");
        OnChanged?.Invoke(Current);
    }

    public static string GetTilesFolder()
    {
        switch (Current)
        {
            case MapTileTheme.DarkNd:
                return "tiles_nd_fms_dark_nd_z14";

            case MapTileTheme.TacticalGray:
            default:
                return "tiles_nd_fms_tactical_gray_z14";
        }
    }

    public static string GetTileChunksFolder()
    {
        switch (Current)
        {
            case MapTileTheme.DarkNd:
                return "mapchunks_nd_fms_dark_nd_z14_16x16";

            case MapTileTheme.TacticalGray:
            default:
                return "mapchunks_nd_fms_tactical_gray_z14_16x16";
        }
    }
}
