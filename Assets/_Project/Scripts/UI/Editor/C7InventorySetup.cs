using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

/// <summary>以 Unity API 建立 C7 原型、資料與測試；不改寫資產文字。</summary>
internal static class C7InventorySetup
{
    private const string ScenePath = "Assets/_Project/Scenes/TestScene_C.unity";
    private const string PrefabPath = "Assets/_Project/Prefabs/InventoryUI.prefab";
    private const string FontPath = "Assets/_Project/UI/Fonts/C7Inventory_NotoSansTC SDF.asset";

    [MenuItem("C7/建立背包原型並清除舊問答空殼")]
    private static void SetUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("請先停止播放。");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("場景有尚未儲存內容，工具不會覆蓋。");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            throw new InvalidOperationException("C7 Prefab 已存在，工具不會重複建立。");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || SceneManager.sceneCount != 1) scene = EditorSceneManager.OpenScene(ScenePath);
        if (UnityEngine.Object.FindFirstObjectByType<Inventory>()) throw new InvalidOperationException("場景已有背包。");
        Canvas host = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .First(c => c.name == "OpeningDialogCanvas");
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/Player.prefab");
        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/UI/Fonts/NotoSansTC-VariableFont_wght.ttf");
        if (!playerPrefab || !sourceFont || !GameObject.Find("C3HUDTestData"))
            throw new InvalidOperationException("測試場景或既有玩家／字型來源不完整。");

        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(sourceFont, 64, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        font.name = "C7Inventory_NotoSansTC SDF";
        font.material.name = "C7Inventory_NotoSansTC Material";
        string allText = File.ReadAllText("Assets/_Project/Scripts/UI/Editor/C7InventorySetup.cs")
            + File.ReadAllText("Assets/_Project/Scripts/UI/C7InventoryTest.cs")
            + File.ReadAllText("Assets/_Project/Scripts/UI/InventoryUI.cs");
        if (!font.TryAddCharacters(new string(allText.Where(c => !char.IsControl(c)).Distinct().ToArray()), out string missing))
            throw new InvalidOperationException("C7 字型缺字：" + missing);
        AssetDatabase.CreateAsset(font, FontPath);
        AssetDatabase.AddObjectToAsset(font.material, font);
        for (int i = 0; i < font.atlasTextures.Length; i++)
        {
            font.atlasTextures[i].name = "C7Inventory Atlas " + i;
            AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
        }
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssetIfDirty(font);
        EnsureFolder("Assets/_Project/ScriptableObjects/Items");
        HealthItemData[] data = {
            Data("NicotinePatch", ItemType.NicotinePatch, "尼古丁貼片", "第三階段使用：回復 30 HP；滿血時不消耗。"),
            Data("NicotineGum", ItemType.NicotineGum, "尼古丁口嚼錠", "第三階段使用：清除附近的 BOSS 菸圈與菸炮煙霧。"),
            Data("NicotineSpray", ItemType.NicotineSpray, "尼古丁戒菸噴霧", "可重複使用；每次使用後有冷卻時間。")
        };

        RectTransform root = Rect("InventoryUI", host.transform, Vector2.zero, Vector2.one);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.overrideSorting = true;
        canvas.sortingOrder = 120;
        Inventory inventory = root.gameObject.AddComponent<Inventory>();
        InventoryUI ui = root.gameObject.AddComponent<InventoryUI>();
        var fields = new SerializedObject(ui);
        fields.FindProperty("_inventory").objectReferenceValue = inventory;
        SerializedProperty slots = fields.FindProperty("_slots");
        slots.arraySize = 3;
        Image sprayCooldown = null;
        for (int i = 0; i < 3; i++)
        {
            float x = 0.04f + i * 0.31f;
            RectTransform slot = Rect("Slot_" + data[i].Type, root, new Vector2(x, 0.025f), new Vector2(x + 0.30f, 0.16f));
            Image background = slot.gameObject.AddComponent<Image>();
            background.raycastTarget = false;
            TMP_Text name = Text("Name", slot, new Vector2(0.22f, 0.48f), new Vector2(0.98f, 0.98f), font, data[i].DisplayName, 25);
            TMP_Text count = Text("Count", slot, new Vector2(0.22f, 0.03f), new Vector2(0.98f, 0.48f), font, "× 0", 25);
            Image icon = Rect("IconPlaceholder", slot, new Vector2(0.03f, 0.2f), new Vector2(0.18f, 0.8f)).gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            SerializedProperty item = slots.GetArrayElementAtIndex(i);
            item.FindPropertyRelative("Data").objectReferenceValue = data[i];
            item.FindPropertyRelative("Name").objectReferenceValue = name;
            item.FindPropertyRelative("Count").objectReferenceValue = count;
            item.FindPropertyRelative("Icon").objectReferenceValue = icon;
            item.FindPropertyRelative("Background").objectReferenceValue = background;
            if (i == 2)
            {
                sprayCooldown = Rect("CooldownFill", slot, Vector2.zero, Vector2.one).gameObject.AddComponent<Image>();
                sprayCooldown.color = new Color(0, 0, 0, 0.45f);
                sprayCooldown.raycastTarget = false;
                sprayCooldown.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                sprayCooldown.type = Image.Type.Filled;
                sprayCooldown.fillMethod = Image.FillMethod.Horizontal;
                sprayCooldown.fillAmount = 0;
                // 覆蓋底色即可，不蓋住文字。
                sprayCooldown.transform.SetSiblingIndex(1);
            }
        }
        TMP_Text description = Text("Description", root, new Vector2(0.04f, 0.17f), new Vector2(0.68f, 0.23f), font, "背包目前沒有道具", 25);
        TMP_Text cooldown = Text("CooldownText", root, new Vector2(0.68f, 0.17f), new Vector2(0.96f, 0.23f), font, "尚未取得噴霧", 25);
        fields.FindProperty("_description").objectReferenceValue = description;
        fields.FindProperty("_cooldownText").objectReferenceValue = cooldown;
        fields.FindProperty("_cooldownFill").objectReferenceValue = sprayCooldown;
        fields.ApplyModifiedPropertiesWithoutUndo();
        if (!PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.UserAction))
            throw new InvalidOperationException("無法儲存背包 Prefab。");

        GameObject fixture = new GameObject("C7InventoryTests");
        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, fixture.transform);
        player.name = "C7TestPlayer";
        player.transform.position = new Vector3(1000, 0, 1000);
        PlayerController controller = player.GetComponent<PlayerController>();
        controller.enabled = false; // 固定測試攝影機，不讓測試角色移動或轉動視角。
        player.GetComponent<PlayerAttack>().enabled = false;
        PrefabUtility.RecordPrefabInstancePropertyModifications(controller);
        PrefabUtility.RecordPrefabInstancePropertyModifications(player.GetComponent<PlayerAttack>());
        PlayerEffects effects = player.GetComponent<PlayerEffects>();
        var inventoryFields = new SerializedObject(inventory);
        inventoryFields.FindProperty("_player").objectReferenceValue = controller;
        inventoryFields.FindProperty("_effects").objectReferenceValue = effects;
        inventoryFields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(inventory);
        PhaseManager phase = UnityEngine.Object.FindFirstObjectByType<PhaseManager>();
        if (!phase) phase = fixture.AddComponent<PhaseManager>();
        C7InventoryTest tester = fixture.AddComponent<C7InventoryTest>();
        RectTransform controls = Rect("C7TestControls", host.transform, Vector2.zero, Vector2.one);
        Canvas testCanvas = controls.gameObject.AddComponent<Canvas>();
        testCanvas.overrideSorting = true;
        testCanvas.sortingOrder = 160;
        controls.gameObject.AddComponent<GraphicRaycaster>();
        TMP_Text status = Text("C7Status", controls, new Vector2(0.04f, 0.63f), new Vector2(0.96f, 0.70f), font, "C7：空背包。可先自動驗證，或領取測試道具。", 26);
        Button demo = Button("GiveDemoItems", controls, new Vector2(0.08f, 0.72f), new Vector2(0.45f, 0.79f), font, "領取三種測試道具");
        Button checks = Button("RunChecks", controls, new Vector2(0.55f, 0.72f), new Vector2(0.92f, 0.79f), font, "C7 自動驗證");
        UnityEventTools.AddPersistentListener(demo.onClick, tester.GiveDemoItems);
        UnityEventTools.AddPersistentListener(checks.onClick, tester.RunChecks);
        var testFields = new SerializedObject(tester);
        foreach (var entry in new (string name, UnityEngine.Object value)[] {
            ("_inventory", inventory), ("_player", controller), ("_health", player.GetComponent<PlayerHealth>()),
            ("_effects", effects), ("_phase", phase), ("_status", status), ("_cooldownFill", sprayCooldown) })
            testFields.FindProperty(entry.name).objectReferenceValue = entry.value;
        testFields.ApplyModifiedPropertiesWithoutUndo();
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("無法儲存 TestScene_C。");

        // 已檢查沒有資產或實際程式引用；刪除交由 Unity 處理 .meta。
        foreach (string name in new[] { "ChestController", "QuizManager", "QuizData" })
        {
            string path = "Assets/_Project/Scripts/Quiz/" + name + ".cs";
            if (!AssetDatabase.DeleteAsset(path)) throw new InvalidOperationException("無法刪除舊空殼：" + path);
        }
        Debug.Log("C7_SETUP_COMPLETE: inventory prefab, three item data assets, dedicated font, TestScene_C fixtures and removal of unused quiz scripts.");
    }

    [MenuItem("C7/在 Play Mode 執行自動驗證")]
    private static void Check() => Tester().RunChecks();
    [MenuItem("C7/在 Play Mode 領取測試道具")]
    private static void Demo() => Tester().GiveDemoItems();
    private static C7InventoryTest Tester()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("請先在 TestScene_C 按 Play。");
        return UnityEngine.Object.FindFirstObjectByType<C7InventoryTest>();
    }
    private static HealthItemData Data(string filename, ItemType type, string name, string info)
    {
        var data = ScriptableObject.CreateInstance<HealthItemData>();
        data.Type = type;
        data.DisplayName = name;
        data.InfoText = info;
        AssetDatabase.CreateAsset(data, "Assets/_Project/ScriptableObjects/Items/" + filename + ".asset");
        return data;
    }
    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int slash = path.LastIndexOf('/');
        string parent = path.Substring(0, slash);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, path.Substring(slash + 1));
    }
    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        return rect;
    }
    private static TMP_Text Text(string name, Transform parent, Vector2 min, Vector2 max, TMP_FontAsset font, string value, float size)
    {
        TextMeshProUGUI text = Rect(name, parent, min, max).gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = size;
        text.text = value;
        text.color = Color.white;
        text.raycastTarget = false;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        return text;
    }
    private static Button Button(string name, Transform parent, Vector2 min, Vector2 max, TMP_FontAsset font, string value)
    {
        RectTransform rect = Rect(name, parent, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.1f, 0.25f, 0.32f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Text("Label", rect, Vector2.zero, Vector2.one, font, value, 26).alignment = TextAlignmentOptions.Center;
        return button;
    }
}
