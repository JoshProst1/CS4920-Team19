using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class SellUiSceneSetup
{
    [MenuItem("Tools/Mining/Set Up Sell UI")]
    private static void SetupActiveScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Stop Play mode before setting up the sale UI.");
            return;
        }

        Scene scene = SceneManager.GetActiveScene();
        GameObject platform = GameObject.Find("Sell Platform");
        GameObject player = GameObject.Find("Player");
        Canvas canvas = Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
            .FirstOrDefault(item => item.gameObject.scene == scene);
        if (platform == null || player == null || canvas == null) return;

        Text moneyText = Resources.FindObjectsOfTypeAll<Text>()
            .FirstOrDefault(text => text.gameObject.scene == scene && text.name == "Money");
        if (moneyText == null) return;

        CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
        if (scaler != null)
        {
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
        }

        GameObject overlay = GetOrCreate("Sell Confirmation Overlay", canvas.transform, typeof(RectTransform), typeof(Image));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.GetComponent<Image>().color = new Color(0.015f, 0.02f, 0.02f, 0.7f);

        GameObject panel = GetOrCreate("Sell Confirmation Panel", overlay.transform, typeof(RectTransform), typeof(Image));
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(620f, 300f);
        panel.GetComponent<Image>().color = new Color(0.09f, 0.105f, 0.1f, 0.98f);

        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Text details = GetOrCreateText("Sale Details", panel.transform, font);
        RectTransform detailsRect = details.GetComponent<RectTransform>();
        detailsRect.anchorMin = new Vector2(0.05f, 0.38f);
        detailsRect.anchorMax = new Vector2(0.95f, 0.95f);
        detailsRect.offsetMin = Vector2.zero;
        detailsRect.offsetMax = Vector2.zero;
        details.text = "Sell stone?";
        details.fontSize = 30;
        details.fontStyle = FontStyle.Bold;
        details.alignment = TextAnchor.MiddleCenter;
        details.horizontalOverflow = HorizontalWrapMode.Wrap;
        details.verticalOverflow = VerticalWrapMode.Overflow;
        details.color = Color.white;
        details.raycastTarget = false;

        Button confirm = GetOrCreateButton("Confirm Sale", panel.transform, font, new Vector2(-105f, -92f), new Color(0.19f, 0.45f, 0.35f));
        Button cancel = GetOrCreateButton("Cancel Sale", panel.transform, font, new Vector2(105f, -92f), new Color(0.27f, 0.29f, 0.29f));

        BackpackInventory inventory = player.GetComponent<BackpackInventory>();
        if (inventory == null) inventory = Undo.AddComponent<BackpackInventory>(player);
        SerializedObject inventoryData = new SerializedObject(inventory);
        inventoryData.FindProperty("moneyText").objectReferenceValue = moneyText;
        inventoryData.ApplyModifiedProperties();

        BoxCollider solidCollider = platform.GetComponents<BoxCollider>().FirstOrDefault(collider => !collider.isTrigger);
        if (solidCollider == null) solidCollider = Undo.AddComponent<BoxCollider>(platform);
        solidCollider.isTrigger = false;
        solidCollider.size = new Vector3(1f, 10f, 1f);

        BoxCollider triggerCollider = platform.GetComponents<BoxCollider>().FirstOrDefault(collider => collider.isTrigger);
        if (triggerCollider == null) triggerCollider = Undo.AddComponent<BoxCollider>(platform);
        triggerCollider.isTrigger = true;
        triggerCollider.size = new Vector3(3f, 30f, 3f);

        Rigidbody body = platform.GetComponent<Rigidbody>();
        if (body == null) body = Undo.AddComponent<Rigidbody>(platform);
        body.isKinematic = true;
        body.useGravity = false;

        SellPlatform sellPlatform = platform.GetComponent<SellPlatform>();
        if (sellPlatform == null) sellPlatform = Undo.AddComponent<SellPlatform>(platform);
        SerializedObject saleData = new SerializedObject(sellPlatform);
        saleData.FindProperty("oreCatalog").objectReferenceValue = Resources.Load<OreCatalog>("Ores/OreCatalog");
        saleData.FindProperty("confirmationPanel").objectReferenceValue = overlay;
        saleData.FindProperty("confirmationText").objectReferenceValue = details;
        saleData.FindProperty("confirmButton").objectReferenceValue = confirm;
        saleData.FindProperty("cancelButton").objectReferenceValue = cancel;
        saleData.ApplyModifiedProperties();

        overlay.SetActive(false);
        EditorSceneManager.MarkSceneDirty(scene);
    }

    private static GameObject GetOrCreate(string name, Transform parent, params System.Type[] components)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.gameObject;

        GameObject created = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(created, "Create sell confirmation UI");
        created.transform.SetParent(parent, false);
        return created;
    }

    private static Text GetOrCreateText(string name, Transform parent, Font font)
    {
        GameObject textObject = GetOrCreate(name, parent, typeof(RectTransform), typeof(Text));
        Text text = textObject.GetComponent<Text>();
        text.font = font;
        return text;
    }

    private static Button GetOrCreateButton(string name, Transform parent, Font font, Vector2 position, Color color)
    {
        GameObject buttonObject = GetOrCreate(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(190f, 58f);
        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;

        Text label = GetOrCreateText("Label", buttonObject.transform, font);
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
        label.text = name;
        label.fontSize = 22;
        label.fontStyle = FontStyle.Bold;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        return button;
    }
}
