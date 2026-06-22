using System;

public static class WebMercator
{
    const double R = 6378137.0; // meters (WGS84)

    // meters per 256px tile at given latitude/zoom
    public static float MetersPerTile(double latDeg, int zoom)
    {
        double latRad = latDeg * Math.PI / 180.0;
        double metersPerPixel = (2.0 * Math.PI * R * Math.Cos(latRad)) / (256.0 * (1 << zoom));
        return (float)(metersPerPixel * 256.0);
    }
}
