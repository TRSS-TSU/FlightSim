using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PfdAirspeedTapeBuilder
{
    const string ScenePath = "Assets/Scenes/Master_FMS.unity";
    const string RootName = "ADI_Airspeed";
    const string BackupName = "ADI_Airspeed_Dial_Backup";
    const float PixelsPerKnot = 4f;
    static readonly Color Cyan = new Color(0f, 0.95f, 1f, 1f);

    [MenuItem("Tools/FMS/PFD/Rebuild Airspeed Tape")]
    public static void Rebuild()
    {
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Transform root = FindTransform(scene, RootName);
        if (!root)
        {
            Debug.LogError($"[PFD Airspeed] Could not find {RootName} in {ScenePath}.");
            return;
        }

        BackupLegacyDial(root);
        RemoveReplacement(root);

        RectTransform viewport = CreateRect("Airspeed_Tape_Viewport", root, Vector2.zero, new Vector2(194f, 642f));
        Image viewportBackground = viewport.gameObject.AddComponent<Image>();
        viewportBackground.color = new Color(0.015f, 0.025f, 0.04f, 0.94f);
        viewportBackground.raycastTarget = false;
        viewport.gameObject.AddComponent<RectMask2D>();

        RectTransform tapeStrip = CreateRect("Airspeed_Tape_Strip", viewport, Vector2.zero, new Vector2(194f, 1800f));
        BuildTapeMarks(tapeStrip);

        RectTransform selectedBug = CreateRect("SelectedSpeedBug_OnTape", viewport, Vector2.zero, new Vector2(34f, 8f));
        SetAnchoredX(selectedBug, 76f);
        Image selectedBugImage = selectedBug.gameObject.AddComponent<Image>();
        selectedBugImage.color = Cyan;
        selectedBugImage.raycastTarget = false;

        RectTransform pointerBox = CreateRect("CurrentSpeedPointer_Box", root, new Vector2(-34f, 0f), new Vector2(126f, 58f));
        Image pointerImage = pointerBox.gameObject.AddComponent<Image>();
        pointerImage.color = new Color(0.015f, 0.02f, 0.025f, 1f);
        pointerImage.raycastTarget = false;
        Outline pointerOutline = pointerBox.gameObject.AddComponent<Outline>();
        pointerOutline.effectColor = Color.white;
        pointerOutline.effectDistance = new Vector2(2f, -2f);

        TMP_Text currentText = CreateText("CurrentSpeedText", pointerBox, Vector2.zero, new Vector2(116f, 52f), "000", 40f, Color.red);
        currentText.fontStyle = FontStyles.Bold;

        RectTransform topReadout = CreateRect("SelectedSpeedTopReadout", root, new Vector2(-36f, 286f), new Vector2(124f, 44f));
        Image topBackground = topReadout.gameObject.AddComponent<Image>();
        topBackground.color = new Color(0.015f, 0.025f, 0.04f, 0.95f);
        topBackground.raycastTarget = false;
        Outline topOutline = topReadout.gameObject.AddComponent<Outline>();
        topOutline.effectColor = Cyan;
        topOutline.effectDistance = new Vector2(1f, -1f);

        RectTransform topBug = CreateRect("SelectedSpeedBug_Top", topReadout, new Vector2(-48f, 0f), new Vector2(12f, 12f));
        Image topBugImage = topBug.gameObject.AddComponent<Image>();
        topBugImage.color = Cyan;
        topBugImage.raycastTarget = false;

        TMP_Text selectedText = CreateText("SelectedSpeedText", topReadout, new Vector2(14f, 0f), new Vector2(88f, 38f), "000", 30f, Cyan);
        selectedText.fontStyle = FontStyles.Bold;

        PfdAirspeedTapeDriver driver = root.GetComponent<PfdAirspeedTapeDriver>();
        if (!driver)
            driver = Undo.AddComponent<PfdAirspeedTapeDriver>(root.gameObject);

        driver.bus = Object.FindFirstObjectByType<FlightDataBus>(FindObjectsInactive.Include);
        driver.targets = driver.bus && driver.bus.targets
            ? driver.bus.targets
            : Object.FindFirstObjectByType<SimTargets>(FindObjectsInactive.Include);
        driver.viewport = viewport;
        driver.tapeStrip = tapeStrip;
        driver.currentSpeedPointerBox = pointerBox;
        driver.currentSpeedText = currentText;
        driver.selectedSpeedBug = selectedBug;
        driver.selectedSpeedTopReadout = topReadout;
        driver.selectedSpeedText = selectedText;
        driver.pixelsPerKnot = PixelsPerKnot;
        driver.tapeZeroIasY = 0f;
        driver.smooth = 12f;
        driver.bugClampPaddingPx = 8f;

        EditorUtility.SetDirty(driver);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root.gameObject;
        Debug.Log("[PFD Airspeed] Backed up the legacy dial and rebuilt the airspeed tape UI.");
    }

    static void BackupLegacyDial(Transform root)
    {
        Transform backup = root.Find(BackupName);
        Transform legacy = root.Find("ADI_Airspeed_Dial");
        if (!backup && legacy)
        {
            Undo.RecordObject(legacy.gameObject, "Back up legacy airspeed dial");
            legacy.name = BackupName;
            legacy.gameObject.SetActive(false);
        }
        else if (backup)
        {
            backup.gameObject.SetActive(false);
        }
    }

    static void RemoveReplacement(Transform root)
    {
        string[] replacementNames =
        {
            "Airspeed_Tape_Viewport",
            "CurrentSpeedPointer_Box",
            "SelectedSpeedTopReadout"
        };

        foreach (string replacementName in replacementNames)
        {
            Transform existing = root.Find(replacementName);
            if (existing)
                Undo.DestroyObjectImmediate(existing.gameObject);
        }
    }

    static void BuildTapeMarks(RectTransform tapeStrip)
    {
        RectTransform tickMarks = CreateRect("TickMarks", tapeStrip, Vector2.zero, tapeStrip.sizeDelta);
        RectTransform speedLabels = CreateRect("SpeedLabels", tapeStrip, Vector2.zero, tapeStrip.sizeDelta);

        for (int speed = 0; speed <= 450; speed += 5)
        {
            bool major = speed % 20 == 0;
            bool medium = speed % 10 == 0;
            float width = major ? 34f : medium ? 24f : 15f;

            RectTransform tick = CreateRect($"Tick_{speed:000}", tickMarks, new Vector2(0f, speed * PixelsPerKnot), new Vector2(width, 2f));
            SetAnchoredX(tick, 96f - width * 0.5f);
            Image tickImage = tick.gameObject.AddComponent<Image>();
            tickImage.color = Color.white;
            tickImage.raycastTarget = false;

            if (major)
            {
                TMP_Text label = CreateText($"Label_{speed:000}", speedLabels, new Vector2(48f, speed * PixelsPerKnot), new Vector2(82f, 28f), speed.ToString("000"), 23f, Color.white);
                label.alignment = TextAlignmentOptions.MidlineRight;
            }
        }
    }

    static RectTransform CreateRect(string name, Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
        go.layer = parent.gameObject.layer;
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
        return rect;
    }

    static TMP_Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, string value, float fontSize, Color color)
    {
        RectTransform rect = CreateRect(name, parent, anchoredPosition, size);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;
        return text;
    }

    static void SetAnchoredX(RectTransform rect, float x)
    {
        rect.anchoredPosition = new Vector2(x, rect.anchoredPosition.y);
    }

    static Transform FindTransform(Scene scene, string objectName)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            Transform match = FindTransform(root.transform, objectName);
            if (match)
                return match;
        }

        return null;
    }

    static Transform FindTransform(Transform current, string objectName)
    {
        if (current.name == objectName)
            return current;

        for (int i = 0; i < current.childCount; i++)
        {
            Transform match = FindTransform(current.GetChild(i), objectName);
            if (match)
                return match;
        }

        return null;
    }
}
