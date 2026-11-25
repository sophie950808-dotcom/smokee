using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.TextCore.LowLevel;

/// <summary>僅建立 C8 Prefab 與既有 TestScene_C 的測試物件。</summary>
internal static class C8DialogueUISetup
{
    private const string ScenePath = "Assets/_Project/Scenes/TestScene_C.unity";
    private const string PrefabPath = "Assets/_Project/Prefabs/DialogueUI.prefab";
    private const string FontPath = "Assets/_Project/UI/Fonts/NotoSansTC-VariableFont_wght SDF.asset";
    private const string C8FontPath = "Assets/_Project/UI/Fonts/C8Dialogue_NotoSansTC SDF.asset";

    [MenuItem("C8/建立並套用 C8 專用中文字型")]
    private static void SetUpC8Font()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先停止播放。");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || SceneManager.sceneCount != 1 || scene.isDirty)
            throw new InvalidOperationException("請開啟已儲存的 TestScene_C；工具不會覆蓋尚未儲存的內容。");
        TMP_FontAsset original = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (!original || !original.sourceFontFile)
            throw new InvalidOperationException("既有中文字型缺少可用的來源字型。");
        DialogueUI ui = UnityEngine.Object.FindFirstObjectByType<DialogueUI>();
        C8DialogueUITest tester = UnityEngine.Object.FindFirstObjectByType<C8DialogueUITest>();
        if (!ui || !tester) throw new InvalidOperationException("找不到既有 C8 原型與測試物件。");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(C8FontPath);
        if (!font)
        {
            font = TMP_FontAsset.CreateFontAsset(original.sourceFontFile, 64, 9,
                GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (!font) throw new InvalidOperationException("無法由既有來源建立 C8 字型。");
            font.name = "C8Dialogue_NotoSansTC SDF";
            font.material.name = "C8Dialogue_NotoSansTC Material";
            // 預先加入 C8 測試與提示所用字元；後續任意台詞仍可動態擴充。
            string source = File.ReadAllText("Assets/_Project/Scripts/UI/C8DialogueUITest.cs")
                + File.ReadAllText("Assets/_Project/Scripts/UI/Editor/C8DialogueUISetup.cs");
            string characters = new string(source.Where(c => !char.IsControl(c)).Distinct().ToArray());
            if (!font.TryAddCharacters(characters, out string missing))
                throw new InvalidOperationException("C8 字型來源不支援部分字元：" + missing);
            AssetDatabase.CreateAsset(font, C8FontPath);
            AssetDatabase.AddObjectToAsset(font.material, font);
            for (int i = 0; i < font.atlasTextures.Length; i++)
            {
                Texture2D texture = font.atlasTextures[i];
                texture.name = "C8Dialogue_NotoSansTC Atlas " + i;
                AssetDatabase.AddObjectToAsset(texture, font);
            }
            EditorUtility.SetDirty(font);
            AssetDatabase.SaveAssetIfDirty(font);
        }

        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            ApplyFont(prefabRoot, font);
            PrefabUtility.SaveAsPrefabAsset(prefabRoot, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(prefabRoot); }
        ApplyFont(ui.gameObject, font);
        ApplyFont(tester.gameObject, font);
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("無法儲存 C8 字型引用。");
        Debug.Log("C8_FONT_READY：已建立並套用獨立中文字型；共用字型保持原樣。Atlas 數量：" + font.atlasTextures.Length);
    }

    private static void ApplyFont(GameObject root, TMP_FontAsset font)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            text.font = font;
            text.fontSharedMaterial = font.material;
            EditorUtility.SetDirty(text);
            if (PrefabUtility.IsPartOfPrefabInstance(text))
                PrefabUtility.RecordPrefabInstancePropertyModifications(text);
        }
    }

    [MenuItem("C8/在 Play Mode 顯示測試對話")]
    private static void Preview()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("請先在 TestScene_C 進入 Play Mode。");
        UnityEngine.Object.FindFirstObjectByType<C8DialogueUITest>().PlaySample();
    }

    [MenuItem("C8/建立 TestScene_C 對話原型")]
    private static void SetUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先停止播放。");
        if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
            throw new InvalidOperationException("目前場景有尚未儲存的內容，請先自行處理；工具不會覆蓋。");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath))
            throw new InvalidOperationException("DialogueUI Prefab 已存在，工具不會覆蓋。");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (!font) throw new InvalidOperationException("找不到現有中文 TMP 字型。");
        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || SceneManager.sceneCount != 1)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        GameObject[] roots = scene.GetRootGameObjects();
        if (roots.Any(r => r.GetComponentInChildren<DialogueUI>(true)))
            throw new InvalidOperationException("場景中已有 DialogueUI，工具不會重複建立。");
        Canvas host = roots.SelectMany(r => r.GetComponentsInChildren<Canvas>(true))
            .FirstOrDefault(c => c.name == "OpeningDialogCanvas");
        if (!host || !host.GetComponent<GraphicRaycaster>())
            throw new InvalidOperationException("找不到既有 OpeningDialogCanvas 或 GraphicRaycaster。");
        if (!roots.Any(r => r.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true)))
            throw new InvalidOperationException("場景缺少既有 EventSystem。");

        RectTransform root = Rect("DialogueUI", host.transform, Vector2.zero, Vector2.one);
        Canvas overlay = root.gameObject.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 200;
        root.gameObject.AddComponent<GraphicRaycaster>();
        DialogueUI ui = root.gameObject.AddComponent<DialogueUI>();
        RectTransform panel = Rect("DialoguePanel", root, Vector2.zero, Vector2.one);
        Image blocker = panel.gameObject.AddComponent<Image>();
        blocker.color = new Color(0, 0, 0, 0.15f);
        RectTransform box = Rect("Background", panel, new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.34f));
        Image background = box.gameObject.AddComponent<Image>();
        background.color = new Color(0.1f, 0.13f, 0.18f, 0.98f);
        background.raycastTarget = false;
        TMP_Text speaker = Text("Speaker", box, new Vector2(0.03f, 0.73f), new Vector2(0.97f, 0.96f), font, "", 34);
        TMP_Text text = Text("DialogueText", box, new Vector2(0.03f, 0.22f), new Vector2(0.97f, 0.73f), font, "", 36);
        TMP_Text hint = Text("ClickHint", box, new Vector2(0.03f, 0.02f), new Vector2(0.97f, 0.20f), font, "左鍵或空白鍵：下一句", 25);
        hint.alignment = TextAlignmentOptions.Right;
        SetFields(ui, ("_dialoguePanel", panel.gameObject), ("_speakerText", speaker), ("_dialogueText", text));
        panel.gameObject.SetActive(false);
        GameObject prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(root.gameObject, PrefabPath, InteractionMode.UserAction);
        if (!prefab) throw new InvalidOperationException("無法建立 DialogueUI Prefab。");

        RectTransform testRoot = Rect("C8DialogueTests", host.transform, Vector2.zero, Vector2.one);
        Canvas testCanvas = testRoot.gameObject.AddComponent<Canvas>();
        testCanvas.overrideSorting = true;
        testCanvas.sortingOrder = 150;
        testRoot.gameObject.AddComponent<GraphicRaycaster>();
        C8DialogueUITest tester = testRoot.gameObject.AddComponent<C8DialogueUITest>();
        TMP_Text status = Text("Status", testRoot, new Vector2(0.05f, 0.80f), new Vector2(0.95f, 0.88f), font,
            "C8 功能原型：選擇播放測試，或執行自動驗證。", 30);
        status.color = Color.white;
        Button play = Button("PlaySample", testRoot, new Vector2(0.12f, 0.9f), new Vector2(0.45f, 0.98f), font, "播放測試對話");
        Button checks = Button("RunChecks", testRoot, new Vector2(0.55f, 0.9f), new Vector2(0.88f, 0.98f), font, "自動驗證");
        SetFields(tester, ("_dialogueUI", ui), ("_status", status), ("_panel", panel.gameObject), ("_speaker", speaker), ("_text", text));
        UnityEventTools.AddPersistentListener(play.onClick, new UnityAction(tester.PlaySample));
        UnityEventTools.AddPersistentListener(checks.onClick, new UnityAction(tester.RunChecks));
        if (!EditorSceneManager.SaveScene(scene)) throw new InvalidOperationException("無法儲存 TestScene_C。");
        Debug.Log("C8_SETUP_COMPLETE：已建立 DialogueUI Prefab，並儲存既有 TestScene_C。未修改其他場景。");
    }

    [MenuItem("C8/在 Play Mode 執行自動驗證")]
    private static void Check()
    {
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath)
            throw new InvalidOperationException("請先在 TestScene_C 進入 Play Mode。");
        C8DialogueUITest tester = UnityEngine.Object.FindFirstObjectByType<C8DialogueUITest>();
        if (!tester) throw new InvalidOperationException("找不到 C8 測試元件。");
        tester.RunChecks();
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

    private static Button Button(string name, Transform parent, Vector2 min, Vector2 max, TMP_FontAsset font, string label)
    {
        RectTransform rect = Rect(name, parent, min, max);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = new Color(0.12f, 0.2f, 0.3f, 1);
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.navigation = new Navigation { mode = Navigation.Mode.None };
        TMP_Text text = Text("Label", rect, Vector2.zero, Vector2.one, font, label, 30);
        text.alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static void SetFields(UnityEngine.Object target, params (string name, UnityEngine.Object value)[] fields)
    {
        var serialized = new SerializedObject(target);
        foreach (var field in fields) serialized.FindProperty(field.name).objectReferenceValue = field.value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
