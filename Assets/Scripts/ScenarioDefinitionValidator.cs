using System;
using System.Collections.Generic;
using UnityEngine;

public static class ScenarioDefinitionValidator
{
    public static bool Validate(ScenarioDefinition s, out string report)
    {
        var known = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var w in s.waypoints)
            if (!string.IsNullOrWhiteSpace(w.ident))
                known.Add(w.ident.Trim());

        var missing = new List<string>();

        void CheckList(string label, List<string> list)
        {
            foreach (var id in list)
            {
                var key = (id ?? "").Trim();
                if (key.Length == 0)
                    continue;
                if (!known.Contains(key))
                    missing.Add($"{label}: {key}");
            }
        }

        CheckList("Prefill", s.prefillRouteIdents);
        CheckList("RNAV25L", s.rnav25LFixes);

        report =
            missing.Count == 0
                ? $"[ScenarioValidator] OK: '{s.scenarioTitle}' ({known.Count} waypoints)"
                : "[ScenarioValidator] MISSING:\n - " + string.Join("\n - ", missing);

        return missing.Count == 0;
    }
}
