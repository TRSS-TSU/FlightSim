using TMPro;
using UnityEngine;

public class HudDebug : MonoBehaviour
{
    public FlightDataBus bus;
    public SimTargets targets;
    public TMP_Text hud;

    public NavAutopilot nav;

    void Update()
    {
        if (!hud)
            return;

        if (!bus || !targets)
        {
            hud.text =
                $"HudDebug missing refs: bus={(bus ? "OK" : "NULL")} targets={(targets ? "OK" : "NULL")}";
            return;
        }

        hud.text =
            $"SPD {bus.ias:0} kt   (T {targets.targetIasKt:0} kt)\n"
            + $"ALT {bus.alt:0} ft   (T {targets.targetAltFtMsl:0} ft)\n"
            + $"HDG {bus.hdg:0}°     (T {targets.targetHdgDeg:0}°)\n"
            + $"VSI {bus.vsi:0} fpm\n";

        if (nav)
            hud.text += $"WP {nav.activeIndex}  {bus.dist:0}m  BRG {bus.brg:0}°";
    }
}
