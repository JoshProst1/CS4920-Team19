using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
internal static class InventoryUiSceneSetup
{
    static InventoryUiSceneSetup()
    {
        EditorApplication.delayCall += SetupActiveScene;
        EditorSceneManager.sceneOpened += OnSceneOpened;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        EditorApplication.delayCall += SetupActiveScene;
    }

    private static void SetupActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene scene = SceneManager.GetActiveScene();
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .FirstOrDefault(item => item.gameObject.scene == scene);
        GameObject player = GameObject.Find("Player");
        if (canvas == null || player == null) return;

        BackpackInventory inventory = player.GetComponent<BackpackInventory>();
        if (inventory == null) return;

        bool changed = false;
        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null && scaler.uiScaleMode == CanvasScaler.ScaleMode.ConstantPixelSize)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            changed = true;
        }

        GameObject panel = FindOrCreate("Inventory Panel", canvas.transform, out bool panelCreated);
        changed |= panelCreated;
        if (panelCreated)
        {
            panel.AddComponent<Image>().color = new Color(0.055f, 0.07f, 0.07f, 0.96f);
            RectTransform rect = panel.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(720f, 570f);
            Undo.RegisterCreatedObjectUndo(panel, "Create inventory panel");
        }

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text heading = FindOrCreateText("Inventory Heading", panel.transform, font, out bool headingCreated);
        if (headingCreated)
        {
            RectTransform rect = heading.rectTransform;
            rect.anchorMin = new Vector2(0.05f, 0.86f);
            rect.anchorMax = new Vector2(0.95f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            heading.text = "BACKPACK";
            heading.fontSize = 30;
            heading.fontStyle = FontStyle.Bold;
            heading.alignment = TextAnchor.MiddleLeft;
            heading.color = Color.white;
            heading.raycastTarget = false;
            changed = true;
        }

        GameObject slotsRoot = FindOrCreate("Inventory Slots", panel.transform, out bool slotsRootCreated);
        if (slotsRootCreated)
        {
            RectTransform rect = slotsRoot.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.05f, 0.05f);
            rect.anchorMax = new Vector2(0.95f, 0.84f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            changed = true;
        }

        InventoryStackRow[] rows = new InventoryStackRow[5];
        for (int i = 0; i < rows.Length; i++)
        {
            GameObject row = FindOrCreate("Inventory Slot " + (i + 1), slotsRoot.transform, out bool rowCreated);
            Image rowImage = row.GetComponent<Image>();
            if (rowImage == null) rowImage = row.AddComponent<Image>();
            InventoryStackRow rowView = row.GetComponent<InventoryStackRow>();
            if (rowView == null) rowView = row.AddComponent<InventoryStackRow>();
            if (rowCreated)
            {
                RectTransform rowRect = row.GetComponent<RectTransform>();
                rowRect.anchorMin = new Vector2(0f, 1f);
                rowRect.anchorMax = new Vector2(1f, 1f);
                rowRect.pivot = new Vector2(0.5f, 1f);
                rowRect.anchoredPosition = new Vector2(0f, -i * 92f);
                rowRect.sizeDelta = new Vector2(0f, 82f);
                rowImage.color = new Color(0.13f, 0.16f, 0.16f, 0.96f);
                rowImage.raycastTarget = false;
            }

            Image icon = FindOrCreateImage("Ore Icon", row.transform, out bool iconCreated);
            if (iconCreated)
            {
                RectTransform iconRect = icon.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0f, 0.5f);
                iconRect.anchoredPosition = new Vector2(12f, 0f);
                iconRect.sizeDelta = new Vector2(58f, 58f);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
            }

            Text label = FindOrCreateText("Ore Name and Count", row.transform, font, out bool labelCreated);
            if (labelCreated)
            {
                RectTransform labelRect = label.rectTransform;
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(88f, 4f);
                labelRect.offsetMax = new Vector2(-12f, -4f);
                label.text = "Empty";
                label.fontSize = 22;
                label.alignment = TextAnchor.MiddleLeft;
                label.color = Color.white;
                label.raycastTarget = false;
            }

            SerializedObject rowData = new SerializedObject(rowView);
            bool missingRowRefs = rowData.FindProperty("oreIcon").objectReferenceValue != icon
                || rowData.FindProperty("oreLabel").objectReferenceValue != label;
            if (missingRowRefs)
            {
                rowData.FindProperty("oreIcon").objectReferenceValue = icon;
                rowData.FindProperty("oreLabel").objectReferenceValue = label;
                rowData.ApplyModifiedProperties();
                changed = true;
            }

            rows[i] = rowView;
            changed |= rowCreated || iconCreated || labelCreated;
        }

        InventoryPanelUI panelController = canvas.GetComponent<InventoryPanelUI>();
        if (panelController == null)
        {
            panelController = Undo.AddComponent<InventoryPanelUI>(canvas.gameObject);
            changed = true;
        }

        SerializedObject panelData = new SerializedObject(panelController);
        bool missingPanelRefs = panelData.FindProperty("inventory").objectReferenceValue != inventory
            || panelData.FindProperty("panel").objectReferenceValue != panel
            || panelData.FindProperty("headingText").objectReferenceValue != heading
            || panelData.FindProperty("rows").arraySize != rows.Length;
        if (missingPanelRefs)
        {
            panelData.FindProperty("inventory").objectReferenceValue = inventory;
            panelData.FindProperty("panel").objectReferenceValue = panel;
            panelData.FindProperty("headingText").objectReferenceValue = heading;
            SerializedProperty rowArray = panelData.FindProperty("rows");
            rowArray.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; i++)
            {
                rowArray.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            }
            panelData.ApplyModifiedProperties();
            changed = true;
        }

        if (panelCreated) panel.SetActive(false);
        if (changed) EditorSceneManager.MarkSceneDirty(scene);
    }

    private static GameObject FindOrCreate(string name, Transform parent, out bool created)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            created = false;
            return existing.gameObject;
        }

        GameObject result = new GameObject(name, typeof(RectTransform));
        result.transform.SetParent(parent, false);
        created = true;
        return result;
    }

    private static Text FindOrCreateText(string name, Transform parent, Font font, out bool created)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            created = false;
            Text existingText = existing.GetComponent<Text>();
            existingText.font = font;
            return existingText;
        }

        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(textObject, "Create inventory text");
        created = true;
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        return text;
    }

    private static Image FindOrCreateImage(string name, Transform parent, out bool created)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            created = false;
            return existing.GetComponent<Image>();
        }

        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(imageObject, "Create inventory icon");
        created = true;
        return imageObject.GetComponent<Image>();
    }
}
