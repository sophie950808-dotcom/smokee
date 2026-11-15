using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class C2OpeningDialogSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/C2_OpeningDialogTest.unity";
    private const string PrefabPath = "Assets/_Project/Prefabs/C2OpeningDialog.prefab";
    private const string FontPath = "Assets/_Project/UI/Fonts/NotoSansTC-VariableFont_wght SDF.asset";

    [MenuItem("C2/建立開場對話測試場景")]
    private static void SetUp()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            throw new InvalidOperationException("請先停止播放再建立 C2 對話框。");

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath || SceneManager.sceneCount != 1)
        {
            if (Enumerable.Range(0, SceneManager.sceneCount).Any(i => SceneManager.GetSceneAt(i).isDirty))
                throw new InvalidOperationException("請先處理目前尚未儲存的場景，再執行 C2 設定。");

            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        GameObject[] roots = scene.GetRootGameObjects();
        if (roots.Any(root => root.GetComponentInChildren<C2OpeningDialog>(true)))
        {
            Debug.Log("C2 測試場景已有開場對話框。");
            return;
        }

        if (AssetDatabase.LoadMainAssetAtPath(PrefabPath))
            throw new InvalidOperationException("已有同名 C2 Prefab，請先檢查其用途。");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (!font)
            throw new InvalidOperationException("找不到專案使用的 Noto Sans TC 字型。");

        GameObject canvasObject = roots.FirstOrDefault(root => root.name == "OpeningDialogCanvas");
        if (!canvasObject)
            canvasObject = new GameObject("OpeningDialogCanvas", typeof(RectTransform));
        if (!canvasObject.GetComponent<RectTransform>())
            throw new InvalidOperationException("OpeningDialogCanvas 缺少 RectTransform。");

        Canvas canvas = canvasObject.GetComponent<Canvas>() ?? canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>() ?? canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        if (!canvasObject.GetComponent<GraphicRaycaster>())
            canvasObject.AddComponent<GraphicRaycaster>();

        if (!roots.Any(root => root.GetComponentInChildren<EventSystem>(true)))
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        RectTransform panel = CreateRect("C2OpeningDialog", canvas.transform, new Vector2(0.16f, 0.09f),
            new Vector2(0.84f, 0.09f), Vector2.zero, new Vector2(0, 292));
        panel.gameObject.AddComponent<Image>().color = new Color32(24, 29, 35, 249);
        C2OpeningDialog dialog = panel.gameObject.AddComponent<C2OpeningDialog>();

        CreateText("Speaker", panel, new Vector2(0, 0.73f), new Vector2(1, 0.91f),
            new Vector2(42, 0), new Vector2(-42, 0), font, "NPC", 28, TextAlignmentOptions.MidlineLeft)
            .color = new Color32(180, 197, 205, 255);
        CreateText("Question", panel, new Vector2(0, 0.43f), new Vector2(1, 0.73f),
            new Vector2(42, 0), new Vector2(-42, 0), font, "要來一根香菸嗎？", 54, TextAlignmentOptions.MidlineLeft);
        Button accept = CreateButton("AcceptSmoke", panel, 0, 0.5f, 42, -10,
            font, "接受香菸", new Color32(70, 76, 85, 255), dialog.AcceptSmoke);
        Button refuse = CreateButton("RefuseSmoke", panel, 0.5f, 1, 10, -42,
            font, "拒絕香菸", new Color32(42, 91, 85, 255), dialog.RefuseSmoke);
        Navigation acceptNavigation = accept.navigation;
        acceptNavigation.mode = Navigation.Mode.Explicit;
        acceptNavigation.selectOnRight = refuse;
        accept.navigation = acceptNavigation;
        Navigation refuseNavigation = refuse.navigation;
        refuseNavigation.mode = Navigation.Mode.Explicit;
        refuseNavigation.selectOnLeft = accept;
        refuse.navigation = refuseNavigation;

        PrefabUtility.SaveAsPrefabAssetAndConnect(panel.gameObject, PrefabPath, InteractionMode.UserAction,
            out bool prefabSaved);
        if (!prefabSaved)
            throw new InvalidOperationException("Unity 無法儲存 C2 對話框 Prefab。");

        TMP_Text result = CreateText("C2PreviewResult", canvas.transform, new Vector2(0.16f, 0.09f),
            new Vector2(0.84f, 0.09f), Vector2.zero, new Vector2(0, 116), font, "",
            42, TextAlignmentOptions.Center);
        result.gameObject.SetActive(false);
        C2OpeningDialogPreview preview = canvasObject.AddComponent<C2OpeningDialogPreview>();
        SerializedObject previewFields = new SerializedObject(preview);
        previewFields.FindProperty("_dialog").objectReferenceValue = panel.GetComponent<C2OpeningDialog>();
        previewFields.FindProperty("_result").objectReferenceValue = result;
        previewFields.ApplyModifiedPropertiesWithoutUndo();

        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Unity 無法儲存 C2 測試場景。");

        Debug.Log("C2 開場對話框和測試場景已儲存。", panel);
    }

    [MenuItem("C2/驗證接受與拒絕按鈕")]
    private static void VerifyChoices()
    {
        C2OpeningDialog dialog = UnityEngine.Object.FindFirstObjectByType<C2OpeningDialog>();
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath || !dialog)
            throw new InvalidOperationException("請在 C2 測試場景進入 Play Mode，並保持對話框開啟。");

        TMP_Text result = dialog.transform.parent.Find("C2PreviewResult").GetComponent<TMP_Text>();
        VerifyChoice(dialog, result, "AcceptSmoke", "已選擇：接受香菸");
        dialog.gameObject.SetActive(true);
        VerifyChoice(dialog, result, "RefuseSmoke", "已選擇：拒絕香菸");
        Debug.Log("C2 驗證通過：接受香菸、拒絕香菸皆能送出各自的選擇。");
    }

    private static void VerifyChoice(C2OpeningDialog dialog, TMP_Text result, string buttonName, string expected)
    {
        RectTransform button = (RectTransform)dialog.transform.Find(buttonName);
        Canvas.ForceUpdateCanvases();
        var click = new PointerEventData(EventSystem.current)
        {
            button = PointerEventData.InputButton.Left,
            position = RectTransformUtility.WorldToScreenPoint(null, button.TransformPoint(button.rect.center))
        };

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(click, hits);
        if (hits.Count == 0 || hits[0].gameObject != button.gameObject)
            throw new InvalidOperationException($"游標無法點到 {buttonName}。");

        ExecuteEvents.Execute(button.gameObject, click, ExecuteEvents.pointerClickHandler);
        if (dialog.gameObject.activeSelf || !result.gameObject.activeSelf || result.text != expected)
            throw new InvalidOperationException($"{buttonName} 未送出預期選擇。");
    }

    private static RectTransform CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
        return rect;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, TMP_FontAsset font, string label, float size, TextAlignmentOptions alignment)
    {
        RectTransform rect = CreateRect(name, parent, anchorMin, anchorMax, offsetMin, offsetMax);
        TextMeshProUGUI text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.font = font;
        text.text = label;
        text.fontSize = size;
        text.alignment = alignment;
        text.raycastTarget = false;
        return text;
    }

    private static Button CreateButton(string name, Transform parent, float left, float right,
        float leftPadding, float rightPadding, TMP_FontAsset font, string label, Color color, UnityAction onClick)
    {
        RectTransform rect = CreateRect(name, parent, new Vector2(left, 0.13f), new Vector2(right, 0.13f),
            new Vector2(leftPadding, 0), new Vector2(rightPadding, 78));
        Image background = rect.gameObject.AddComponent<Image>();
        background.color = color;
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = background;
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        colors.colorMultiplier = 1.2f;
        colors.normalColor = new Color(0.83f, 0.83f, 0.83f);
        button.colors = colors;
        CreateText("Label", rect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, font,
            label, 35, TextAlignmentOptions.Center);
        UnityEventTools.AddPersistentListener(button.onClick, onClick);
        return button;
    }
}
