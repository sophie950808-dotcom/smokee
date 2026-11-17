using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// A 的 I1 整合工具（Unity 上方選單 Tools/Smokee）。
/// 一鍵組裝 GameScene：換上 Player.prefab、放 GameManager／RouteManager、HUD／暴露警示文字、
/// 結算畫面、EventSystem、死巷出口。可以重複執行，已經放過的東西不會放第二份。
/// </summary>
public static class GameSceneSetupTools
{
    private const string GameScenePath = "Assets/_Project/Scenes/GameScene.unity";
    private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Player.prefab";
    private const string HudPrefabPath = "Assets/_Project/Prefabs/HUD.prefab";
    private const string WarningPrefabPath = "Assets/_Project/Prefabs/ExposureWarningText.prefab";
    private const string ResultPrefabPath = "Assets/_Project/Prefabs/ResultScreen.prefab";
    private const string TestSceneCPath = "Assets/_Project/Scenes/TestScene_C.unity";
    private const string ResultCanvasName = "ResultCanvas";
    private const string UiCanvasName = "GameUI_Canvas";
    private const string AlleyExitName = "AlleyExit_Trigger";

    /// <summary>選單：I1 #5。組裝 GameScene，完成後存檔並顯示做了哪些事。</summary>
    [MenuItem("Tools/Smokee/I1 - 組裝 GameScene")]
    public static void AssembleGameSceneMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(GameScenePath) == null)
        {
            EditorUtility.DisplayDialog("找不到場景", "找不到 " + GameScenePath + "。", "好");
            return;
        }

        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
        GameObject hudPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
        GameObject warningPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(WarningPrefabPath);
        if (playerPrefab == null || hudPrefab == null || warningPrefab == null)
        {
            EditorUtility.DisplayDialog("找不到 Prefab",
                "請確認以下 Prefab 都存在：\n" + PlayerPrefabPath + "\n" + HudPrefabPath + "\n" + WarningPrefabPath, "好");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        List<string> report = new List<string>();
        List<string> warnings = new List<string>();

        // 1. 換上 Player.prefab（放在 B 臨時玩家的位置）
        Transform playerTransform = SetupPlayer(scene, playerPrefab, report, warnings);

        // 2. 移除 Main Camera 上的 CameraFollow（攝影機改由 PlayerController 控制）
        // 用名稱找（不直接引用 B 的臨時腳本，之後刪掉那些腳本時這個工具才不會編譯錯誤）
        foreach (Component follow in FindByTypeName(scene, "CameraFollow"))
        {
            report.Add("移除 " + follow.gameObject.name + " 上的 CameraFollow 元件");
            Object.DestroyImmediate(follow);
        }

        // 3. OpeningSequence 的「玩家物件」欄位指向新玩家
        if (playerTransform != null)
        {
            foreach (OpeningSequence opening in FindInScene<OpeningSequence>(scene))
            {
                SerializedObject serializedOpening = new SerializedObject(opening);
                serializedOpening.FindProperty("player").objectReferenceValue = playerTransform;
                serializedOpening.ApplyModifiedPropertiesWithoutUndo();
                report.Add("OpeningSequence 的玩家欄位指向新的 Player");
            }
        }

        // 4. GameManager、RouteManager
        if (FindInScene<GameManager>(scene).Count == 0)
        {
            new GameObject("GameManager").AddComponent<GameManager>();
            report.Add("放入 GameManager（開發者模式未勾選）");
        }
        if (FindInScene<RouteManager>(scene).Count == 0)
        {
            new GameObject("RouteManager").AddComponent<RouteManager>();
            report.Add("放入 RouteManager");
        }

        // 5. UI：HUD、暴露警示文字（放在 Canvas 底下）、EventSystem
        if (FindInScene<HUD>(scene).Count == 0)
        {
            PrefabUtility.InstantiatePrefab(hudPrefab, scene);
            report.Add("放入 HUD Prefab");
        }
        if (FindInScene<ExposureWarning>(scene).Count == 0)
        {
            Transform canvas = GetOrCreateCanvas(scene, report);
            PrefabUtility.InstantiatePrefab(warningPrefab, canvas);
            report.Add("放入暴露警示文字 Prefab（在 " + UiCanvasName + " 底下）");
        }
        if (FindInScene<ResultUI>(scene).Count == 0)
        {
            GameObject resultPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath);
            if (resultPrefab != null)
            {
                PrefabUtility.InstantiatePrefab(resultPrefab, scene);
                report.Add("放入結算畫面 Prefab");
            }
            else
            {
                warnings.Add("還沒有 " + ResultPrefabPath + "，沒有放結算畫面。請先執行「I1 - 從 TestScene_C 建立結算畫面 Prefab」再組裝一次。");
            }
        }
        if (FindInScene<EventSystem>(scene).Count == 0)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<InputSystemUIInputModule>();
            report.Add("放入 EventSystem（新版 Input System）");
        }

        // 6. 死巷出口：和 GarageExit_Trigger 共用同一個觸發區
        SetupAlleyExit(scene, report, warnings);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string message = report.Count > 0
            ? "GameScene 已組裝並存檔：\n・" + string.Join("\n・", report)
            : "GameScene 已經組裝過了，沒有修改。";
        if (warnings.Count > 0)
        {
            message += "\n\n注意：\n・" + string.Join("\n・", warnings);
        }
        EditorUtility.DisplayDialog("完成", message, "好");
    }

    /// <summary>
    /// 選單：I1 #7。在 HUD Prefab 加上互動提示文字（畫面中下方，暫時的位置與樣式，C 之後可調整），
    /// 並接到 HUD 的 _interactPromptText 欄位。字型沿用 HUD 裡現有的文字。可重複執行。
    /// </summary>
    [MenuItem("Tools/Smokee/I1 - HUD Prefab 加上互動提示")]
    public static void AddInteractPromptToHudMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath) == null)
        {
            EditorUtility.DisplayDialog("找不到 Prefab", "找不到 " + HudPrefabPath + "。", "好");
            return;
        }

        string message;
        GameObject contents = PrefabUtility.LoadPrefabContents(HudPrefabPath);
        try
        {
            HUD hud = contents.GetComponentInChildren<HUD>(true);
            Canvas canvas = contents.GetComponentInChildren<Canvas>(true);
            if (hud == null || canvas == null)
            {
                EditorUtility.DisplayDialog("HUD Prefab 結構不同", "HUD Prefab 裡找不到 HUD 元件或 Canvas，沒有修改。", "好");
                return;
            }

            SerializedObject serializedHud = new SerializedObject(hud);
            SerializedProperty promptProperty = serializedHud.FindProperty("_interactPromptText");
            if (promptProperty == null)
            {
                EditorUtility.DisplayDialog("HUD.cs 還沒更新", "HUD 元件沒有 _interactPromptText 欄位，請確認 HUD.cs 已更新並編譯完成。", "好");
                return;
            }

            if (promptProperty.objectReferenceValue != null)
            {
                message = "HUD Prefab 已經有互動提示文字，沒有修改。";
            }
            else
            {
                // 字型沿用 HUD 裡現有的文字（預設字型可能不支援中文）
                TMPro.TMP_Text existingText = contents.GetComponentInChildren<TMPro.TMP_Text>(true);

                GameObject promptObject = new GameObject("InteractPrompt", typeof(RectTransform));
                promptObject.layer = canvas.gameObject.layer;
                promptObject.transform.SetParent(canvas.transform, false);

                RectTransform rect = (RectTransform)promptObject.transform;
                rect.anchorMin = new Vector2(0.5f, 0f);
                rect.anchorMax = new Vector2(0.5f, 0f);
                rect.pivot = new Vector2(0.5f, 0f);
                rect.anchoredPosition = new Vector2(0f, 160f);
                rect.sizeDelta = new Vector2(900f, 60f);

                TMPro.TextMeshProUGUI promptText = promptObject.AddComponent<TMPro.TextMeshProUGUI>();
                if (existingText != null)
                {
                    promptText.font = existingText.font;
                    promptText.fontSharedMaterial = existingText.fontSharedMaterial;
                }
                promptText.text = "按 E 互動";
                promptText.fontSize = 32f;
                promptText.alignment = TMPro.TextAlignmentOptions.Center;
                promptText.color = Color.white;
                promptText.raycastTarget = false;

                promptProperty.objectReferenceValue = promptText;
                serializedHud.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(contents, HudPrefabPath);
                message = "已在 HUD Prefab 加上 InteractPrompt（畫面中下方），並接到 HUD 的互動提示欄位。\n" +
                          "位置與樣式是暫時的，C 之後可以依 UI 設計調整。";
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(HudPrefabPath);
        EditorUtility.DisplayDialog("完成", message, "好");
    }

    /// <summary>
    /// 選單：I1 結算畫面。把 C 在 TestScene_C 做的 ResultCanvas 複製一份，掛上 ResultUI 並接好欄位，
    /// 存成 Prefabs/ResultScreen.prefab。不會存檔 TestScene_C（C 的場景不會被修改）。
    /// </summary>
    [MenuItem("Tools/Smokee/I1 - 從 TestScene_C 建立結算畫面 Prefab")]
    public static void CreateResultPrefabMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestSceneCPath) == null)
        {
            EditorUtility.DisplayDialog("找不到場景", "找不到 " + TestSceneCPath + "。", "好");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath) != null &&
            !EditorUtility.DisplayDialog("Prefab 已存在",
                ResultPrefabPath + " 已經存在。要用 TestScene_C 目前的版面重新建立嗎？\n（之前在 Prefab 上做的修改會被蓋掉）",
                "重新建立", "取消"))
        {
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        // 用「附加」方式開啟 TestScene_C，做完後關掉且不存檔
        Scene testScene = SceneManager.GetSceneByPath(TestSceneCPath);
        bool openedHere = !testScene.isLoaded;
        if (openedHere)
        {
            testScene = EditorSceneManager.OpenScene(TestSceneCPath, OpenSceneMode.Additive);
        }

        string message;
        GameObject clone = null;
        try
        {
            GameObject source = null;
            foreach (GameObject rootObject in testScene.GetRootGameObjects())
            {
                if (rootObject.name == ResultCanvasName)
                {
                    source = rootObject;
                    break;
                }
            }
            if (source == null)
            {
                EditorUtility.DisplayDialog("找不到結算畫面", "TestScene_C 裡找不到最上層的 " + ResultCanvasName + "，沒有建立 Prefab。", "好");
                return;
            }

            clone = Object.Instantiate(source);
            SceneManager.MoveGameObjectToScene(clone, testScene); // 複製品留在 TestScene_C，不弄髒目前開著的場景
            clone.name = "ResultScreen";
            clone.SetActive(true); // C 的 ResultCanvas 預設隱藏；Prefab 改由 ResultUI 控制面板顯示

            Transform panel = FindChildByName(clone.transform, "ResultPanel");
            Transform title = FindChildByName(clone.transform, "ResultTitle");
            Transform stats = FindChildByName(clone.transform, "ResultStats");
            if (panel == null || title == null || stats == null)
            {
                EditorUtility.DisplayDialog("結算畫面結構不同",
                    "ResultCanvas 底下找不到 ResultPanel／ResultTitle／ResultStats，可能 C 改了版面，沒有建立 Prefab。", "好");
                return;
            }

            ResultUI resultUI = clone.GetComponent<ResultUI>();
            if (resultUI == null)
            {
                resultUI = clone.AddComponent<ResultUI>();
            }
            SerializedObject serializedResult = new SerializedObject(resultUI);
            serializedResult.FindProperty("_panel").objectReferenceValue = panel.gameObject;
            serializedResult.FindProperty("_titleText").objectReferenceValue = title.GetComponent<TMPro.TMP_Text>();
            serializedResult.FindProperty("_statsText").objectReferenceValue = stats.GetComponent<TMPro.TMP_Text>();
            serializedResult.ApplyModifiedPropertiesWithoutUndo();

            panel.gameObject.SetActive(false); // 編輯器裡不要擋住畫面，執行時由 ResultUI 打開

            PrefabUtility.SaveAsPrefabAsset(clone, ResultPrefabPath, out bool success);
            message = success
                ? "已建立 " + ResultPrefabPath + "（掛好 ResultUI、欄位已接好）。\nTestScene_C 沒有被修改。\n\n下一步：執行「I1 - 組裝 GameScene」把它放進 GameScene。"
                : "建立 Prefab 失敗，請把 Console 的錯誤訊息貼給 AI。";
        }
        finally
        {
            if (clone != null)
            {
                Object.DestroyImmediate(clone);
            }
            if (openedHere)
            {
                EditorSceneManager.CloseScene(testScene, true);
            }
        }

        Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(ResultPrefabPath);
        EditorUtility.DisplayDialog("完成", message, "好");
    }

    /// <summary>在子物件（包含停用的）裡用名稱找 Transform。</summary>
    private static Transform FindChildByName(Transform root, string childName)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == childName)
            {
                return child;
            }
        }
        return null;
    }

    /// <summary>場景沒有 PlayerController 時，在 B 臨時玩家的位置放 Player.prefab，並刪掉臨時玩家。回傳場景裡的玩家。</summary>
    private static Transform SetupPlayer(Scene scene, GameObject playerPrefab, List<string> report, List<string> warnings)
    {
        List<PlayerController> existing = FindInScene<PlayerController>(scene);
        if (existing.Count > 0)
        {
            return existing[0].transform;
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, scene);
        List<Component> oldPlayers = FindByTypeName(scene, "SimplePlayerMovement");
        if (oldPlayers.Count > 0)
        {
            Transform oldTransform = oldPlayers[0].transform;
            player.transform.SetPositionAndRotation(oldTransform.position, oldTransform.rotation);
            report.Add("在臨時玩家「" + oldTransform.name + "」的位置放入 Player.prefab，並刪除臨時玩家");
            foreach (Component oldPlayer in oldPlayers)
            {
                if (oldPlayer != null)
                {
                    Object.DestroyImmediate(oldPlayer.gameObject);
                }
            }
        }
        else
        {
            report.Add("放入 Player.prefab");
            warnings.Add("找不到 B 的臨時玩家，Player 放在 (0, 0, 0)，請自己拖到開場位置。");
        }
        return player.transform;
    }

    /// <summary>場景沒有 AlleyExitTrigger 時，在 GarageExit_Trigger 物件上加 AlleyExitTrigger（找不到才另外建立一個）。</summary>
    private static void SetupAlleyExit(Scene scene, List<string> report, List<string> warnings)
    {
        if (FindInScene<AlleyExitTrigger>(scene).Count > 0)
        {
            return;
        }

        // 死巷出口和車庫出口共用同一個觸發區：在 B 的 GarageExit_Trigger 物件上加 AlleyExitTrigger
        List<GarageExitTrigger> garageExits = FindInScene<GarageExitTrigger>(scene);
        if (garageExits.Count > 0)
        {
            GameObject garage = garageExits[0].gameObject;
            garage.AddComponent<AlleyExitTrigger>();
            report.Add("在 " + garage.name + " 加上 AlleyExitTrigger（死巷出口和車庫出口共用同一個觸發區）");
            Collider garageCollider = garage.GetComponent<Collider>();
            if (garageCollider == null || !garageCollider.isTrigger)
            {
                warnings.Add(garage.name + " 沒有勾選 Is Trigger 的 Collider，玩家穿過時不會觸發，請檢查。");
            }
            return;
        }

        GameObject exit = new GameObject(AlleyExitName);
        BoxCollider exitCollider = exit.AddComponent<BoxCollider>();
        exitCollider.isTrigger = true;
        exit.AddComponent<AlleyExitTrigger>();
        report.Add("建立死巷出口 " + AlleyExitName);
        warnings.Add("找不到 GarageExit_Trigger，死巷出口放在 (0, 0, 0)，請拖到正確位置。");
    }

    /// <summary>找場景裡名為 GameUI_Canvas 的 Canvas，沒有就建立一個（螢幕疊加、依 1920x1080 縮放）。</summary>
    private static Transform GetOrCreateCanvas(Scene scene, List<string> report)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == UiCanvasName && rootObject.GetComponent<Canvas>() != null)
            {
                return rootObject.transform;
            }
        }

        GameObject canvasObject = new GameObject(UiCanvasName);
        canvasObject.layer = LayerMask.NameToLayer("UI");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        report.Add("建立 UI 畫布 " + UiCanvasName);
        return canvasObject.transform;
    }

    /// <summary>用類別名稱找出場景裡的元件（包含停用的物件）。</summary>
    private static List<Component> FindByTypeName(Scene scene, string typeName)
    {
        List<Component> results = new List<Component>();
        foreach (Component component in FindInScene<Component>(scene))
        {
            if (component != null && component.GetType().Name == typeName)
            {
                results.Add(component);
            }
        }
        return results;
    }

    /// <summary>找出場景裡所有 T 元件（包含停用的物件）。</summary>
    private static List<T> FindInScene<T>(Scene scene) where T : Component
    {
        List<T> results = new List<T>();
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            results.AddRange(rootObject.GetComponentsInChildren<T>(true));
        }
        return results;
    }
}
