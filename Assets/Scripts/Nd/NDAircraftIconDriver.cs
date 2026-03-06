using UnityEngine;

public class NDAircraftIconDriver : MonoBehaviour
{
    [Header("Sources")]
    public Transform aircraftRoot; // _AircraftRoot

    [Header("Options")]
    public bool northUp = true; // ND is north-up for now
    public float rotationOffsetDeg = 0f; // use if sprite points up/right/etc

    void LateUpdate()
    {
        if (aircraftRoot == null)
            return;

        // Unity yaw: 0 = +Z (north), increases clockwise → matches ND nicely
        float headingDeg = aircraftRoot.eulerAngles.y;

        // For north-up ND: icon rotates with aircraft
        float iconDeg = northUp ? headingDeg : 0f;

        // UI rotates around Z axis
        transform.localRotation = Quaternion.Euler(0f, 0f, -iconDeg + rotationOffsetDeg);
    }
}
