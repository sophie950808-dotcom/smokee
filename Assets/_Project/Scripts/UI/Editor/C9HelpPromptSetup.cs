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

/// <summary>只在既有 TestScene_C 建立 C9 原型，並綁定原有 HUD 提示。</summary>
internal static class C9HelpPromptSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/TestScene_C.unity";
    private const string PrefabPath = "Assets/_Project/Prefabs/HelpPromptUI.prefab";
    private const string FontPath = "Assets/_Project/UI/Fonts/C9UI_NotoSansTC SDF.asset";

    [MenuItem("C9/在 TestScene_C 建立幫忙視窗測試")]
    private static void SetUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Stop Play Mode before setup.");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("Unsaved scene changes detected. Setup will not overwrite them.");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) || AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath))
            throw new InvalidOperationException("C9 assets already exist. Setup will not overwrite them.");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || SceneManager.sceneCount != 1) scene = EditorSceneManager.OpenScene(ScenePath);
        if (UnityEngine.Object.FindFirstObjectByType<HelpPromptUI>())
            throw new InvalidOperationException("A HelpPromptUI already exists in the scene.");
        PlayerController[] players = UnityEngine.Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        HUD[] huds = UnityEngine.Object.FindObjectsByType<HUD>(FindObjectsSortMode.None);
        if (players.Length != 1 || huds.Length != 1 || !GameManager.Instance && !UnityEngine.Object.FindFirstObjectByType<GameManager>())
            throw new InvalidOperationException("C9 needs the existing single test player, HUD and GameManager.");
        PlayerController player = players[0];
        PlayerInteractor interactor = player.GetComponent<PlayerInteractor>();
        if (!interactor) throw new InvalidOperationException("The existing player has no PlayerInteractor.");
        Canvas host = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .First(c => c.name == "OpeningDialogCanvas");
        var hudFields = new SerializedObject(huds[0]);
        TMP_Text prompt = hudFields.FindProperty("_interactPromptText").objectReferenceValue as TMP_Text;
        if (!prompt) throw new InvalidOperationException("Existing HUD interaction text is missing.");

        TMP_FontAsset font = CreateFont();
        RectTransform root = Rect("HelpPromptUI", host.transform, Vector2.zero, Vector2.one);
        Canvas canvas = root.gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 300;
        root.gameObject.AddComponent<GraphicRaycaster>();
        HelpPromptUI ui = root.gameObject.AddComponent<HelpPromptUI>();
        RectTransform panel = Rect("HelpPromptPanel", root, Vector2.zero, Vector2.one);
        Image blocker = panel.gameObject.AddComponent<Image>();
        blocker.color = new Color(0, 0, 0, 0.65f);
        RectTransform box = Rect("WindowBackground", panel, new Vector2(0.20f, 0.33f), new Vector2(0.80f, 0.67f));
        Image background = box.gameObject.AddComponent<Image>();
        background.color = new Color(0.10f, 0.14f, 0.20f);
        background.raycastTarget = false;
        TMP_Text question = Text("Question", box, new Vector2(0.06f, 0.48f), new Vector2(0.94f, 0.94f), font, "", 38);
        question.alignment = TextAlignmentOptions.Center;
        Button accept = Button("AcceptButton", box, new Vector2(0.08f, 0.10f), new Vector2(0.46f, 0.36f), font, "幫忙");
        Button refuse = Button("RefuseButton", box, new Vector2(0.54f, 0.10f), new Vector2(0.92f, 0.36f), font, "拒絕");
        Fields(ui, ("_panel", panel.gameObject), ("_questionText", question), ("_acceptButton", accept), ("_refuseButton", refuse));
        panel.gameObject.SetActive(false);
        if (!PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.UserAction))
            throw new InvalidOperationException("Cannot save HelpPromptUI prefab.");

        // 只修改場景實例的引用與提示字型，不改 HUD 腳本或共用 HUD Prefab。
        hudFields.FindProperty("_playerInteractor").objectReferenceValue = interactor;
        hudFields.ApplyModifiedPropertiesWithoutUndo();
        PrefabUtility.RecordPrefabInstancePropertyModifications(huds[0]);
        prompt.font = font;
        prompt.fontSharedMaterial = font.material;
        EditorUtility.SetDirty(prompt);
        PrefabUtility.RecordPrefabInstancePropertyModifications(prompt);

        var fixture = new GameObject("C9HelpPromptTests");
        C9HelpPromptTest tester = fixture.AddComponent<C9HelpPromptTest>();
        C9TestInteractable a = Target("C9Target_A", fixture.transform, new Vector3(1000, 100, 1000),
            "按 E 詢問測試目標甲", "測試目標甲：願意幫忙嗎？");
        C9TestInteractable b = Target("C9Target_B", fixture.transform, new Vector3(1000, 110, 1000),
            "按 E 詢問測試目標乙", "測試目標乙：要接受這個測試請求嗎？");
        RectTransform controls = Rect("C9TestControls", host.transform, Vector2.zero, Vector2.one);
        Canvas testCanvas = controls.gameObject.AddComponent<Canvas>();
        testCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        testCanvas.overrideSorting = true;
        testCanvas.sortingOrder = 250;
        controls.gameObject.AddComponent<GraphicRaycaster>();
        TMP_Text status = Text("C9Status", controls, new Vector2(0.04f, 0.42f), new Vector2(0.96f, 0.48f), font,
            "C9：可測試幫忙視窗、按 E 互動，或執行自動驗證。", 25);
        Button demo = Button("ShowDemo", controls, new Vector2(0.04f, 0.51f), new Vector2(0.32f, 0.58f), font, "測試幫忙視窗");
        Button interaction = Button("PrepareInteraction", controls, new Vector2(0.36f, 0.51f), new Vector2(0.64f, 0.58f), font, "準備 E 互動測試");
        Button checks = Button("RunChecks", controls, new Vector2(0.68f, 0.51f), new Vector2(0.96f, 0.58f), font, "C9 自動驗證");
        UnityEventTools.AddPersistentListener(demo.onClick, tester.ShowDemo);
        UnityEventTools.AddPersistentListener(interaction.onClick, tester.PrepareInteraction);
        UnityEventTools.AddPersistentListener(checks.onClick, tester.RunChecks);
        Fields(tester, ("_ui", ui), ("_panel", panel.gameObject), ("_question", question), ("_accept", accept), ("_refuse", refuse),
            ("_player", player), ("_interactor", interactor), ("_prompt", prompt), ("_targetA", a), ("_targetB", b), ("_status", status));
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("Cannot save TestScene_C.");
        Debug.Log("C9_SETUP_COMPLETE: HelpPromptUI prefab, dedicated font, existing HUD binding and test-only interactables. No new player or core manager.");
    }

    [MenuItem("C9/在 Play Mode 執行自動驗證")]
    private static void Check() => Tester().RunChecks();
    [MenuItem("C9/在 Play Mode 顯示幫忙視窗")]
    private static void Demo() => Tester().ShowDemo();
    [MenuItem("C9/在 Play Mode 準備 E 互動")]
    private static void Interact() => Tester().PrepareInteraction();
    private static C9HelpPromptTest Tester()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("Enter Play Mode in TestScene_C first.");
        return UnityEngine.Object.FindFirstObjectByType<C9HelpPromptTest>();
    }

    private static TMP_FontAsset CreateFont()
    {
        Font source = AssetDatabase.LoadAssetAtPath<Font>("Assets/_Project/UI/Fonts/NotoSansTC-VariableFont_wght.ttf");
        if (!source) throw new InvalidOperationException("Existing Noto Sans TC source is missing.");
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(source, 64, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
        if (!font) throw new InvalidOperationException("Cannot create C9 font.");
        font.name = "C9UI_NotoSansTC SDF";
        font.material.name = "C9UI_NotoSansTC Material";
        string text = "";
        foreach (string file in new[] { "C9HelpPromptTest.cs", "C9TestInteractable.cs", "Editor/C9HelpPromptSetup.cs" })
            text += File.ReadAllText("Assets/_Project/Scripts/UI/" + file);
        if (!font.TryAddCharacters(new string(text.Where(c => !char.IsControl(c)).Distinct().ToArray()), out string missing))
            throw new InvalidOperationException("C9 font is missing characters: " + missing);
        AssetDatabase.CreateAsset(font, FontPath);
        AssetDatabase.AddObjectToAsset(font.material, font);
        for (int i = 0; i < font.atlasTextures.Length; i++)
        {
            font.atlasTextures[i].name = "C9UI Atlas " + i;
            AssetDatabase.AddObjectToAsset(font.atlasTextures[i], font);
        }
        EditorUtility.SetDirty(font);
        AssetDatabase.SaveAssetIfDirty(font);
        return font;
    }

    private static C9TestInteractable Target(string name, Transform parent, Vector3 position, string prompt, string question)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);
        root.transform.position = position;
        root.AddComponent<BoxCollider>().isTrigger = true;
        C9TestInteractable target = root.AddComponent<C9TestInteractable>();
        var fields = new SerializedObject(target);
        fields.FindProperty("_prompt").stringValue = prompt;
        fields.FindProperty("_question").stringValue = question;
        fields.ApplyModifiedPropertiesWithoutUndo();
        return target;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.gameObject.layer = 5;
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
        image.color = new Color(0.13f, 0.30f, 0.38f);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        Text("Label", rect, Vector2.zero, Vector2.one, font, value, 30).alignment = TextAlignmentOptions.Center;
        return button;
    }
    private static void Fields(UnityEngine.Object target, params (string name, UnityEngine.Object value)[] fields)
    {
        var data = new SerializedObject(target);
        foreach (var field in fields) data.FindProperty(field.name).objectReferenceValue = field.value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
