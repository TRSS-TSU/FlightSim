using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class HierarchyJsonExporter
{
    [Serializable]
    private class SceneDump
    {
        public string sceneName;
        public string exportedAt;
        public List<NodeDump> roots = new();
    }

    [Serializable]
    private class NodeDump
    {
        public string name;
        public string path;
        public bool activeSelf;
        public bool activeInHierarchy;
        public string tag;
        public string layer;
        public List<string> components = new();
        public List<NodeDump> children = new();
    }

    [MenuItem("Tools/NAS FMS/Export Hierarchy JSON")]
    public static void Export()
    {
        var scene = UnityEngine.SceneManagement.SceneManager.GetActiveScene();

        var dump = new SceneDump
        {
            sceneName = scene.name,
            exportedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
        };

        foreach (var root in scene.GetRootGameObjects())
            dump.roots.Add(DumpNode(root.transform, root.name));

        string json = JsonUtility.ToJson(dump, true);

        string folder = Path.Combine(Application.dataPath, "../HierarchyExports");
        Directory.CreateDirectory(folder);

        string file = Path.Combine(
            folder,
            $"{scene.name}_Hierarchy_{DateTime.Now:yyyyMMdd_HHmmss}.json"
        );

        File.WriteAllText(file, json);
        Debug.Log($"[HierarchyJsonExporter] Exported hierarchy JSON:\n{file}");

        EditorUtility.RevealInFinder(file);
    }

    private static NodeDump DumpNode(Transform t, string path)
    {
        var go = t.gameObject;

        var node = new NodeDump
        {
            name = go.name,
            path = path,
            activeSelf = go.activeSelf,
            activeInHierarchy = go.activeInHierarchy,
            tag = go.tag,
            layer = LayerMask.LayerToName(go.layer),
        };

        foreach (var c in go.GetComponents<Component>())
            node.components.Add(c ? c.GetType().Name : "Missing Script");

        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            node.children.Add(DumpNode(child, $"{path}/{child.name}"));
        }

        return node;
    }
}
