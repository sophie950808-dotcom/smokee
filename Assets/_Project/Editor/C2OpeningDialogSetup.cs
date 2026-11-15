using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

internal static class C2OpeningDialogSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/TestScene_C.unity";
    private const string PrefabPath = "Assets/_Project/Prefabs/C2OpeningDialog.prefab";
    private const string FontPath = "Assets/_Project/UI/Fonts/NotoSansTC-VariableFont_wght SDF.asset";

    [MenuItem("C2/在 TestScene_C 設定開場對話測試")]
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
            C2OpeningDialogPreview existingPreview = roots
                .Select(root => root.GetComponentInChildren<C2OpeningDialogPreview>(true))
                .FirstOrDefault(preview => preview);
            if (!existingPreview)
                throw new InvalidOperationException("找不到 C2 測試場景的結果顯示元件。");

            if (ConnectTestRouteManager(scene, existingPreview))
                SaveTestScene(scene);
            Debug.Log("C2 測試場景已連接 RouteManager。");
            return;
        }

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (!prefab || !prefab.GetComponent<C2OpeningDialog>())
            throw new InvalidOperationException("找不到 C2 開場對話框 Prefab。");

        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        if (!font)
            throw new InvalidOperationException("找不到專案使用的 Noto Sans TC 字型。");

        GameObject canvasObject = roots.FirstOrDefault(root => root.name == "OpeningDialogCanvas");
        if (!canvasObject)
            canvasObject = new GameObject("OpeningDialogCanvas", typeof(RectTransform));
        if (!canvasObject.GetComponent<RectTransform>())
            throw new InvalidOperationException("OpeningDialogCanvas 缺少 RectTransform。");

        if (!canvasObject.TryGetComponent(out Canvas canvas))
            canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        if (!canvasObject.TryGetComponent(out CanvasScaler scaler))
            scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        if (!canvasObject.GetComponent<GraphicRaycaster>())
            canvasObject.AddComponent<GraphicRaycaster>();

        if (!roots.Any(root => root.GetComponentInChildren<EventSystem>(true)))
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject panel = (GameObject)PrefabUtility.InstantiatePrefab(prefab, canvas.transform);

        TMP_Text result = CreateText("C2PreviewResult", canvas.transform, new Vector2(0.16f, 0.09f),
            new Vector2(0.84f, 0.09f), Vector2.zero, new Vector2(0, 116), font, "",
            42, TextAlignmentOptions.Center);
        result.gameObject.SetActive(false);
        C2OpeningDialogPreview preview = canvasObject.AddComponent<C2OpeningDialogPreview>();
        SerializedObject previewFields = new SerializedObject(preview);
        previewFields.FindProperty("_result").objectReferenceValue = result;
        previewFields.ApplyModifiedPropertiesWithoutUndo();
        ConnectTestRouteManager(scene, preview);
        SaveTestScene(scene);

        Debug.Log("C2 開場對話測試已儲存於 TestScene_C。", panel);
    }

    private static bool ConnectTestRouteManager(Scene scene, C2OpeningDialogPreview preview)
    {
        RouteManager[] managers = scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<RouteManager>(true))
            .ToArray();
        if (managers.Length > 1)
            throw new InvalidOperationException("C2 測試場景只能有一個 RouteManager。");

        RouteManager manager = managers.Length == 1
            ? managers[0]
            : new GameObject("RouteManager").AddComponent<RouteManager>();
        var previewFields = new SerializedObject(preview);
        previewFields.FindProperty("_routeManager").objectReferenceValue = manager;
        return previewFields.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SaveTestScene(Scene scene)
    {
        if (!EditorSceneManager.SaveScene(scene))
            throw new InvalidOperationException("Unity 無法儲存 C2 測試場景。");
    }

    [MenuItem("C2/驗證接受與拒絕按鈕")]
    private static void VerifyChoices()
    {
        C2OpeningDialog dialog = UnityEngine.Object.FindFirstObjectByType<C2OpeningDialog>();
        if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path != ScenePath || !dialog)
            throw new InvalidOperationException("請在 TestScene_C 進入 Play Mode，並保持 C2 對話框開啟。");

        TMP_Text result = dialog.transform.parent.Find("C2PreviewResult").GetComponent<TMP_Text>();
        VerifyChoice(dialog, result, "AcceptSmoke", OpeningRoute.AcceptSmoke, "已選擇：接受香菸");
        dialog.gameObject.SetActive(true);
        VerifyChoice(dialog, result, "RefuseSmoke", OpeningRoute.RefuseSmoke, "已選擇：拒絕香菸");
        Debug.Log("C2 驗證通過：RouteManager 已收到接受香菸和拒絕香菸，並發出對應的路線事件。");
    }

    private static void VerifyChoice(C2OpeningDialog dialog, TMP_Text result, string buttonName,
        OpeningRoute expectedRoute, string expectedText)
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
        if (dialog.gameObject.activeSelf || RouteManager.Instance.ChosenRoute != expectedRoute
            || !result.gameObject.activeSelf || result.text != expectedText)
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
}
