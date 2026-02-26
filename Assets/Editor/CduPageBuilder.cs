using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using TMPro;
using System.Collections.Generic;   // List<>, HashSet<>

/// <summary>
/// Unity Editor window that clones the Index page child hierarchy (Title_Line + Body)
/// into all empty CDU page GameObjects under FMS_Text_Display.
///
/// Menu: FMS → CDU Page Builder
/// </summary>
public class CduPageBuilder : EditorWindow
{
    private GameObject           _template;   // drag in Index
    private readonly List<GameObject> _targets = new();
    private Vector2              _scroll;

    // Non-page siblings to skip during auto-scan
    private static readonly HashSet<string> ExcludedNames =
        new HashSet<string>(System.StringComparer.OrdinalIgnoreCase)
    {
        "Shared_IO"
    };

    [MenuItem("FMS/CDU Page Builder")]
    static void Open() => GetWindow<CduPageBuilder>("CDU Page Builder").Show();

    // ─────────────────────────────────────────────────────────────────────────
    // GUI
    // ─────────────────────────────────────────────────────────────────────────

    private void OnGUI()
    {
        EditorGUILayout.LabelField("CDU Page Hierarchy Builder", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Template source
        _template = (GameObject)EditorGUILayout.ObjectField(
            "Template (Index)", _template, typeof(GameObject), true);

        EditorGUILayout.HelpBox(
            "Drag the Index page GameObject into Template, then Scan to find empty pages.",
            MessageType.Info);

        EditorGUILayout.Space();

        // Scan
        if (GUILayout.Button("Scan Scene for Empty Pages"))
            ScanScene();

        // Target list
        EditorGUILayout.LabelField($"Pages to build ({_targets.Count}):",
            EditorStyles.miniBoldLabel);

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(220));
        foreach (var t in _targets)
            EditorGUILayout.ObjectField(t, typeof(GameObject), true);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // Build button
        EditorGUI.BeginDisabledGroup(!_template || _targets.Count == 0);
        GUI.backgroundColor = new Color(0.4f, 0.9f, 0.4f);
        if (GUILayout.Button("Build All Hierarchies", GUILayout.Height(32)))
            BuildAll();
        GUI.backgroundColor = Color.white;
        EditorGUI.EndDisabledGroup();

        if (_targets.Count == 0 && _template)
        {
            EditorGUILayout.HelpBox(
                "No empty pages found — all siblings may already have a Title_Line child.",
                MessageType.Warning);
        }
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scan
    // ─────────────────────────────────────────────────────────────────────────

    private void ScanScene()
    {
        _targets.Clear();

        if (!_template)
        {
            Debug.LogWarning("[CduPageBuilder] Assign a Template page first.");
            return;
        }

        Transform parent = _template.transform.parent;
        if (!parent)
        {
            Debug.LogWarning("[CduPageBuilder] Template has no parent — cannot find siblings.");
            return;
        }

        foreach (Transform sibling in parent)
        {
            if (sibling.gameObject == _template)      continue;   // skip template itself
            if (ExcludedNames.Contains(sibling.name)) continue;   // skip Shared_IO etc.
            if (!sibling.Find("Title_Line"))                       // no hierarchy = empty
                _targets.Add(sibling.gameObject);
        }

        Debug.Log($"[CduPageBuilder] Found {_targets.Count} page(s) to build.");
        Repaint();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Build
    // ─────────────────────────────────────────────────────────────────────────

    private void BuildAll()
    {
        if (!_template || _targets.Count == 0) return;

        Undo.SetCurrentGroupName("Build CDU Page Hierarchies");
        int group = Undo.GetCurrentGroup();

        foreach (var target in _targets)
            BuildHierarchy(_template.transform, target.transform);

        Undo.CollapseUndoOperations(group);
        EditorSceneManager.MarkSceneDirty(_template.scene);

        Debug.Log($"[CduPageBuilder] Successfully built hierarchy for {_targets.Count} page(s).");

        // Re-scan to update the list (pages now have Title_Line so they'll disappear)
        ScanScene();
    }

    /// <summary>
    /// Clones Title_Line and Body from <paramref name="source"/> into <paramref name="target"/>,
    /// clearing all TMP_Text content so scripts can populate at runtime.
    /// </summary>
    private static void BuildHierarchy(Transform source, Transform target)
    {
        // Remove any existing children first
        for (int i = target.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(target.GetChild(i).gameObject);

        // Clone each top-level child of the template (Title_Line, Body)
        foreach (Transform srcChild in source)
        {
            // worldPositionStays=false → clone sits at the same local position as original
            GameObject clone = Instantiate(srcChild.gameObject, target, false);
            clone.name = srcChild.name;
            Undo.RegisterCreatedObjectUndo(clone, "CDU Hierarchy");

            // Wipe all TMP text — the page view scripts fill content at runtime
            foreach (var tmp in clone.GetComponentsInChildren<TMP_Text>(true))
                tmp.text = "";
        }

        EditorUtility.SetDirty(target.gameObject);
    }
}
