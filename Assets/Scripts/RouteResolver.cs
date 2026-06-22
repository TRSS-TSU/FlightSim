using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Snapshot of route + nav state captured immediately BEFORE a route mutation.
/// Passed to RouteResolver.Resolve() after the mutation and scene rebuild so the
/// resolver can map old navigation context onto the new waypoint array.
/// </summary>
public struct RouteContinuitySnapshot
{
    /// <summary>Ordered ident list from Model.ActiveRoute at snapshot time.</summary>
    public List<string> oldRouteIdents;

    /// <summary>NavAutopilot.activeIndex at snapshot time.</summary>
    public int oldActiveIndex;

    /// <summary>Ident of the FROM waypoint (route[oldActiveIndex-1]), or "" if at index 0.</summary>
    public string oldFromIdent;

    /// <summary>Ident of the TO waypoint (route[oldActiveIndex]), or "" if route was empty.</summary>
    public string oldToIdent;

    /// <summary>Aircraft position projected onto the XZ plane at snapshot time.</summary>
    public Vector3 aircraftFlatPos;

    /// <summary>Aircraft forward direction projected onto the XZ plane and normalized at snapshot time.</summary>
    public Vector3 aircraftFlatFwd;
}

/// <summary>
/// Deterministic five-tier active-leg resolver for post-rebuild route continuity.
///
/// Given a pre-mutation snapshot, the rebuilt route list, and the rebuilt waypoint
/// Transform array, returns the index that NavAutopilot.activeIndex should be set to.
///
/// Tiers (highest precedence first):
///   1. Identical route reapply  — same ident sequence, old index still valid → reuse it
///   2. Same FROM→TO leg pair    — pair found in new route → return that TO index
///   3. TO ident survives        — choose best geometric match (unpassed, min XTK, along-track)
///   4. TO deleted, downstream   — advance to first new-route entry after old active position
///   5. Full replacement         — segment-based scan (XTK + along-track), not nearest-waypoint
///
/// Self-contained (no MonoBehaviour dependency); can be unit-tested independently.
/// </summary>
public static class RouteResolver
{
    // ─────────────────────────────────────────────────────────────────────────
    // Public entry point
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Resolve the best active-leg index after a route rebuild.
    /// Call immediately after FlightPlan.RebuildRoute(); assign result to NavAutopilot.activeIndex.
    /// </summary>
    public static int Resolve(
        RouteContinuitySnapshot snap,
        List<ScenarioDefinition.WaypointDef> newRoute,
        Transform[] newWaypoints)
    {
        if (newRoute == null || newRoute.Count == 0
            || newWaypoints == null || newWaypoints.Length == 0)
            return 0;

        // ── Tier 1: Identical route reapply ──────────────────────────────────
        // If the new ident sequence exactly matches the old one and the old
        // active index is still in range, keep it — no heading change.
        if (snap.oldRouteIdents != null
            && newRoute.Count == snap.oldRouteIdents.Count
            && snap.oldActiveIndex >= 0
            && snap.oldActiveIndex < newWaypoints.Length)
        {
            bool same = true;
            for (int i = 0; i < newRoute.Count && same; i++)
                same = string.Equals(newRoute[i].ident, snap.oldRouteIdents[i],
                    System.StringComparison.OrdinalIgnoreCase);
            if (same)
                return snap.oldActiveIndex;
        }

        // ── Tier 2: Same FROM→TO leg pair ────────────────────────────────────
        // Handles inserts/deletes anywhere — the pair is the strongest identity.
        if (!string.IsNullOrEmpty(snap.oldToIdent))
        {
            // Index-0 special case: no FROM leg
            if (string.IsNullOrEmpty(snap.oldFromIdent)
                && string.Equals(newRoute[0].ident, snap.oldToIdent,
                    System.StringComparison.OrdinalIgnoreCase))
                return 0;

            for (int i = 1; i < newRoute.Count; i++)
            {
                if (string.Equals(newRoute[i].ident, snap.oldToIdent,
                        System.StringComparison.OrdinalIgnoreCase)
                    && string.Equals(newRoute[i - 1].ident, snap.oldFromIdent,
                        System.StringComparison.OrdinalIgnoreCase))
                    return i;
            }
        }

        // ── Tier 3: TO ident survives (leg pair gone, e.g. FROM deleted) ─────
        // If multiple occurrences exist, pick the best geometric match.
        if (!string.IsNullOrEmpty(snap.oldToIdent))
        {
            var candidates = new List<int>();
            for (int i = 0; i < newRoute.Count; i++)
            {
                if (string.Equals(newRoute[i].ident, snap.oldToIdent,
                    System.StringComparison.OrdinalIgnoreCase))
                    candidates.Add(i);
            }

            if (candidates.Count == 1)
                return candidates[0];

            if (candidates.Count > 1)
            {
                int best = candidates[0];
                float bestScore = float.MaxValue;
                foreach (int ci in candidates)
                {
                    float s = ScoreSegmentCandidate(ci, snap, newWaypoints);
                    if (s < bestScore) { bestScore = s; best = ci; }
                }
                return best;
            }
        }

        // ── Tier 4: TO waypoint deleted — advance downstream ─────────────────
        // Find the first new-route entry that was downstream of oldActiveIndex
        // in the old route (preserves forward-route progression).
        if (snap.oldRouteIdents != null && snap.oldRouteIdents.Count > 0)
        {
            var oldPos = new Dictionary<string, int>(System.StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < snap.oldRouteIdents.Count; i++)
            {
                if (!oldPos.ContainsKey(snap.oldRouteIdents[i]))
                    oldPos[snap.oldRouteIdents[i]] = i;
            }

            for (int i = 0; i < newRoute.Count; i++)
            {
                if (oldPos.TryGetValue(newRoute[i].ident, out int op)
                    && op > snap.oldActiveIndex)
                    return i;
            }
        }

        // ── Tier 5: Full replacement — segment-based scan ────────────────────
        // Evaluate every inbound leg segment (i-1 → i). Prefer unpassed legs;
        // score by XTK + along-track distance. Avoids nearest-waypoint pitfall
        // on curved routes that loop back toward origin.
        float bestScore5 = float.MaxValue;
        int bestIdx5 = 0;
        for (int i = 1; i < newWaypoints.Length; i++)
        {
            float s = ScoreSegmentCandidate(i, snap, newWaypoints);
            if (s < bestScore5) { bestScore5 = s; bestIdx5 = i; }
        }
        return bestIdx5;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Private helpers
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Score the inbound leg segment ending at waypoint index <paramref name="i"/>.
    ///
    /// Returns float.MaxValue if the aircraft has clearly passed the waypoint
    /// (dot-product test on the perpendicular plane at the TO waypoint).
    ///
    /// Otherwise returns a composite score (lower = better):
    ///   XTK_abs * 1.0  +  along_track_to_TO * 0.01  +  |i - oldActiveIndex| * 0.001
    /// </summary>
    private static float ScoreSegmentCandidate(
        int i,
        RouteContinuitySnapshot snap,
        Transform[] waypoints)
    {
        if (i <= 0 || i >= waypoints.Length) return float.MaxValue;
        if (!waypoints[i - 1] || !waypoints[i])  return float.MaxValue;

        Vector3 A  = Vector3.ProjectOnPlane(waypoints[i - 1].position, Vector3.up);
        Vector3 B  = Vector3.ProjectOnPlane(waypoints[i].position,     Vector3.up);
        Vector3 AB = B - A;

        if (AB.sqrMagnitude < 1f) return float.MaxValue;

        Vector3 ABn = AB.normalized;
        Vector3 toB = B - snap.aircraftFlatPos;

        // Perpendicular-plane passed test: if aircraft is past B along the leg, skip.
        if (Vector3.Dot(toB, ABn) < 0f) return float.MaxValue;

        // Cross-track error (unsigned)
        Vector3 AP  = snap.aircraftFlatPos - A;
        float   xtk = Mathf.Abs(Vector3.Cross(ABn, AP).y);

        // Along-track remaining to B (distance component)
        float along = toB.magnitude;

        // Index proximity as a tiebreaker (prefer legs near the old active position)
        float idxProximity = Mathf.Abs(i - snap.oldActiveIndex);

        return xtk * 1f + along * 0.01f + idxProximity * 0.001f;
    }
}
