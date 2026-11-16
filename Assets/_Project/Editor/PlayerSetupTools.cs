using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A 的 Editor 選單工具（Unity 上方選單 Tools/Smokee）。
/// 1. 建立 Player Prefab（膠囊外型，元件與引用都設定好）
/// 2. 在 TestScene_A 建立測試環境（地板、牆、窄走廊、方塊）並放入 Player
/// 3. A4：在 Player Prefab 補上 PlayerAttack／PlayerInteractor，並在 TestScene_A 放測試用假目標與互動物件
/// </summary>
public static class PlayerSetupTools
{
    private const string PrefabFolder = "Assets/_Project/Prefabs";
    private const string PrefabPath = PrefabFolder + "/Player.prefab";
    private const string MaterialFolder = "Assets/_Project/Materials/TestA";
    private const string ScenePath = "Assets/_Project/Scenes/TestScene_A.unity";
    private const string EnvironmentRootName = "TestEnvironment";
    private const string A4RootName = "A4_TestObjects";

    /// <summary>選單：A4 測試物件。在 Player Prefab 補上攻擊／互動元件，並在 TestScene_A 放測試物件。</summary>
    [MenuItem("Tools/Smokee/A - 放置 A4 攻擊／互動測試物件")]
    public static void BuildA4TestObjectsMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("找不到場景", "找不到 " + ScenePath + "，請確認場景存在。", "好");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        // 1. Player Prefab：沒有就新建（新建的已包含兩個元件），有就只補上缺少的元件
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
        {
            CreatePlayerPrefab();
        }
        else
        {
            AddA4ComponentsToExistingPrefab();
        }

        // 2. 開啟 TestScene_A，移除上次放的 A4 測試物件，重新放
        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool hasPlayer = false;
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == A4RootName)
            {
                Object.DestroyImmediate(rootObject);
            }
            else if (rootObject.GetComponent<PlayerController>() != null)
            {
                hasPlayer = true;
            }
        }

        BuildA4TestObjects();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string message = "A4 測試物件已放好並存檔。\n按 Play 就可以測試攻擊與互動。";
        if (!hasPlayer)
        {
            message += "\n\n注意：場景裡沒有 Player。請先執行「A - 建立 TestScene_A 測試環境」。";
        }

        EditorUtility.DisplayDialog("完成", message, "好");
    }

    /// <summary>打開現有的 Player Prefab，補上缺少的 PlayerAttack／PlayerInteractor，其他設定保留。</summary>
    private static void AddA4ComponentsToExistingPrefab()
    {
        GameObject contents = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            if (contents.GetComponent<PlayerController>() == null)
            {
                Debug.LogWarning("PlayerSetupTools：Player Prefab 上沒有 PlayerController，無法加攻擊／互動元件。");
                return;
            }

            if (contents.GetComponent<PlayerAttack>() == null)
            {
                contents.AddComponent<PlayerAttack>();
            }

            if (contents.GetComponent<PlayerInteractor>() == null)
            {
                contents.AddComponent<PlayerInteractor>();
            }

            if (contents.GetComponent<TargetMarker>() == null)
            {
                contents.AddComponent<TargetMarker>();
            }

            PrefabUtility.SaveAsPrefabAsset(contents, PrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(contents);
        }
    }

    /// <summary>
    /// 放 A4 測試物件（Player 出生在 (0, 0, -10)，面向 +Z）：
    /// 前方 2 個 TestDummy、東邊短牆後面 1 個 TestDummy 與 1 個 TestInteractable（測試隔牆），
    /// 西邊 1 個 TestInteractable，以及暫時的互動提示 OnGUI。
    /// </summary>
    private static void BuildA4TestObjects()
    {
        EnsureFolder(MaterialFolder);
        Material wallMaterial = GetOrCreateMaterial("TestA_Wall", new Color(0.85f, 0.8f, 0.7f));
        Material dummyMaterial = GetOrCreateMaterial("TestA_Dummy", Color.white);
        Material interactMaterial = GetOrCreateMaterial("TestA_Interactable", new Color(0.2f, 0.8f, 0.3f));

        Transform root = new GameObject(A4RootName).transform;

        // 前方的兩個假目標（高 2 公尺）
        CreateDummy("Dummy_Front", root, new Vector3(0f, 1f, -7.5f), dummyMaterial);
        CreateDummy("Dummy_Left", root, new Vector3(-2.5f, 1f, -7.5f), dummyMaterial);

        // 東邊一面短牆，牆後面放假目標和互動物件：站在牆西側應該打不到、也不能互動
        CreateBlock("A4_Wall", root, new Vector3(7f, 1.5f, -10f), new Vector3(0.3f, 3f, 3f), wallMaterial);
        CreateDummy("Dummy_BehindWall", root, new Vector3(8f, 1f, -9.5f), dummyMaterial);
        CreateInteractable("Interactable_BehindWall", root, new Vector3(8f, 0.5f, -11f), interactMaterial);

        // 西邊的互動物件（沒被擋住）
        CreateInteractable("Interactable_Test", root, new Vector3(-4f, 0.5f, -10f), interactMaterial);

        // 暫時的互動提示（正式 HUD 由 C 負責）
        GameObject prompt = new GameObject("DebugInteractPrompt");
        prompt.transform.SetParent(root, false);
        prompt.AddComponent<DebugInteractPrompt>();
    }

    private static void CreateDummy(string name, Transform parent, Vector3 position, Material material)
    {
        GameObject dummy = CreateBlock(name, parent, position, new Vector3(0.8f, 2f, 0.8f), material);
        dummy.AddComponent<TestDummy>();
    }

    private static void CreateInteractable(string name, Transform parent, Vector3 position, Material material)
    {
        GameObject interactable = CreateBlock(name, parent, position, Vector3.one, material);
        interactable.AddComponent<TestInteractable>();
    }

    /// <summary>選單：建立（或覆蓋）Player Prefab。</summary>
    [MenuItem("Tools/Smokee/A - 建立 Player Prefab")]
    public static void CreatePlayerPrefabMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
        {
            bool overwrite = EditorUtility.DisplayDialog(
                "Player Prefab 已存在",
                PrefabPath + " 已經存在。\n\n要用預設設定覆蓋嗎？你在 Prefab 上手動改過的設定會不見。",
                "覆蓋", "取消");
            if (!overwrite)
            {
                return;
            }
        }

        GameObject prefab = CreatePlayerPrefab();
        EditorGUIUtility.PingObject(prefab);
        EditorUtility.DisplayDialog("完成", "已建立 " + PrefabPath, "好");
    }

    /// <summary>選單：在 TestScene_A 建立測試環境並放入 Player。</summary>
    [MenuItem("Tools/Smokee/A - 建立 TestScene_A 測試環境")]
    public static void BuildTestSceneMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("找不到場景", "找不到 " + ScenePath + "，請確認場景存在。", "好");
            return;
        }

        // 如果目前開著的場景有沒存的修改，先問要不要存
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        // 移除這個工具上次建立的東西（測試環境、帶有 PlayerController 的物件），其他物件保留
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == EnvironmentRootName || rootObject.GetComponent<PlayerController>() != null)
            {
                Object.DestroyImmediate(rootObject);
            }
        }

        BuildEnvironment();

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            prefab = CreatePlayerPrefab();
        }

        GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        player.name = "Player";
        player.transform.position = new Vector3(0f, 0.1f, -10f);

        Camera mainCamera = FindOrCreateMainCamera(scene);
        SerializedObject serializedController = new SerializedObject(player.GetComponent<PlayerController>());
        serializedController.FindProperty("_camera").objectReferenceValue = mainCamera;
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = player;

        EditorUtility.DisplayDialog("完成", "TestScene_A 測試環境已建立並存檔。\n按 Play 就可以測試。", "好");
    }

    private static GameObject CreatePlayerPrefab()
    {
        EnsureFolder(PrefabFolder);
        EnsureFolder(MaterialFolder);

        Material bodyMaterial = GetOrCreateMaterial("TestA_PlayerBody", new Color(0.2f, 0.55f, 0.95f));
        Material noseMaterial = GetOrCreateMaterial("TestA_PlayerNose", new Color(1f, 0.8f, 0.1f));

        GameObject root = new GameObject("Player");

        // CharacterController：高 2 公尺、半徑 0.4，中心在腰部，腳底剛好在物件原點
        CharacterController characterController = root.AddComponent<CharacterController>();
        characterController.height = 2f;
        characterController.radius = 0.4f;
        characterController.center = new Vector3(0f, 1f, 0f);
        characterController.stepOffset = 0.3f;

        PlayerController playerController = root.AddComponent<PlayerController>();
        root.AddComponent<PlayerAttack>();
        root.AddComponent<PlayerInteractor>();
        root.AddComponent<TargetMarker>();

        // 身體：膠囊外型（拿掉膠囊自帶的碰撞體，碰撞交給 CharacterController）
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localPosition = new Vector3(0f, 1f, 0f);
        body.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
        Object.DestroyImmediate(body.GetComponent<Collider>());
        body.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

        // 鼻子：前方的小方塊，用來看出角色面向哪裡
        GameObject nose = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nose.name = "Nose";
        nose.transform.SetParent(root.transform, false);
        nose.transform.localPosition = new Vector3(0f, 1.5f, 0.4f);
        nose.transform.localScale = new Vector3(0.2f, 0.15f, 0.2f);
        Object.DestroyImmediate(nose.GetComponent<Collider>());
        nose.GetComponent<Renderer>().sharedMaterial = noseMaterial;

        // 鏡頭轉軸：眼睛高度
        GameObject pivot = new GameObject("CameraPivot");
        pivot.transform.SetParent(root.transform, false);
        pivot.transform.localPosition = new Vector3(0f, 1.6f, 0f);

        // 把引用拖進 PlayerController 的 Inspector 欄位
        SerializedObject serializedController = new SerializedObject(playerController);
        serializedController.FindProperty("_cameraPivot").objectReferenceValue = pivot.transform;
        SerializedProperty renderers = serializedController.FindProperty("_bodyRenderers");
        renderers.arraySize = 2;
        renderers.GetArrayElementAtIndex(0).objectReferenceValue = body.GetComponent<Renderer>();
        renderers.GetArrayElementAtIndex(1).objectReferenceValue = nose.GetComponent<Renderer>();
        serializedController.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static void BuildEnvironment()
    {
        EnsureFolder(MaterialFolder);
        Material floorMaterial = GetOrCreateMaterial("TestA_Floor", new Color(0.45f, 0.45f, 0.45f));
        Material wallMaterial = GetOrCreateMaterial("TestA_Wall", new Color(0.85f, 0.8f, 0.7f));
        Material boxMaterial = GetOrCreateMaterial("TestA_Box", new Color(0.85f, 0.35f, 0.3f));

        Transform root = new GameObject(EnvironmentRootName).transform;

        // 地板 40 x 40，上表面在 y = 0
        CreateBlock("Floor", root, new Vector3(0f, -0.5f, 0f), new Vector3(40f, 1f, 40f), floorMaterial);

        // 四面外牆，高 4
        CreateBlock("Wall_North", root, new Vector3(0f, 2f, 20.5f), new Vector3(42f, 4f, 1f), wallMaterial);
        CreateBlock("Wall_South", root, new Vector3(0f, 2f, -20.5f), new Vector3(42f, 4f, 1f), wallMaterial);
        CreateBlock("Wall_East", root, new Vector3(20.5f, 2f, 0f), new Vector3(1f, 4f, 40f), wallMaterial);
        CreateBlock("Wall_West", root, new Vector3(-20.5f, 2f, 0f), new Vector3(1f, 4f, 40f), wallMaterial);

        // 窄走廊（內寬約 2 公尺），測試第三人稱鏡頭會不會穿牆
        CreateBlock("Corridor_Left", root, new Vector3(5f, 1.5f, 5f), new Vector3(0.5f, 3f, 12f), wallMaterial);
        CreateBlock("Corridor_Right", root, new Vector3(7.5f, 1.5f, 5f), new Vector3(0.5f, 3f, 12f), wallMaterial);

        // 一面獨立的牆和一根柱子，貼著它們轉鏡頭測試
        CreateBlock("Wall_Single", root, new Vector3(-6f, 1.5f, 4f), new Vector3(8f, 3f, 0.5f), wallMaterial);
        CreateBlock("Pillar", root, new Vector3(-2f, 2f, -4f), new Vector3(1f, 4f, 1f), wallMaterial);

        // 階梯方塊：0.5 / 1.0 / 1.5 公尺高，測試跳躍
        CreateBlock("Box_0.5m", root, new Vector3(-10f, 0.25f, -6f), new Vector3(2f, 0.5f, 2f), boxMaterial);
        CreateBlock("Box_1.0m", root, new Vector3(-12f, 0.5f, -6f), new Vector3(2f, 1f, 2f), boxMaterial);
        CreateBlock("Box_1.5m", root, new Vector3(-14f, 0.75f, -6f), new Vector3(2f, 1.5f, 2f), boxMaterial);

        // 散落的方塊
        CreateBlock("Box_A", root, new Vector3(3f, 0.5f, -6f), new Vector3(1f, 1f, 1f), boxMaterial);
        CreateBlock("Box_B", root, new Vector3(12f, 1f, -8f), new Vector3(2f, 2f, 2f), boxMaterial);
        CreateBlock("Box_C", root, new Vector3(-8f, 0.75f, 12f), new Vector3(1.5f, 1.5f, 1.5f), boxMaterial);
    }

    private static GameObject CreateBlock(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
        block.name = name;
        block.transform.SetParent(parent, false);
        block.transform.localPosition = position;
        block.transform.localScale = scale;
        block.GetComponent<Renderer>().sharedMaterial = material;
        return block;
    }

    private static Camera FindOrCreateMainCamera(Scene scene)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Camera camera = rootObject.GetComponentInChildren<Camera>(true);
            if (camera != null && camera.CompareTag("MainCamera"))
            {
                return camera;
            }
        }

        // 場景裡沒有 Main Camera 才新建一台
        GameObject cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        Camera newCamera = cameraObject.AddComponent<Camera>();
        cameraObject.AddComponent<AudioListener>();
        return newCamera;
    }

    private static Material GetOrCreateMaterial(string materialName, Color color)
    {
        string path = MaterialFolder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("PlayerSetupTools：找不到 URP Lit Shader，改用 Standard。");
            shader = Shader.Find("Standard");
        }

        material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string folderPath)
    {
        if (AssetDatabase.IsValidFolder(folderPath))
        {
            return;
        }

        string parent = Path.GetDirectoryName(folderPath).Replace('\\', '/');
        string folderName = Path.GetFileName(folderPath);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folderName);
    }
}
