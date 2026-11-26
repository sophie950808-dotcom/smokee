using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A14 的 Editor 選單工具（Unity 上方選單 Tools/Smokee）。
/// 1. 建立 BOSS 與菸圈的 Prefab（巢狀結構，A 和 B 分開改不同檔案）：
///    - Boss.prefab（A）：BossController、BossHealth、BossAttack、SmokeSource（半徑 7）、膠囊碰撞體、RingMuzzle（菸圈發射點）
///      └ BossVisual.prefab（B）：外觀（暫時是約玩家 2 倍大的灰色膠囊，沒有碰撞體）
///    - SmokeRing.prefab（A）：BossSmokeRing（沒有碰撞體）
///      └ SmokeRingVisual.prefab（B）：外觀（暫時是扁扁的淺灰色球）
///    B 的 Visual Prefab 已經存在時不會覆蓋（保護 B 改好的外觀）；A 的 Prefab 已經存在時會先詢問。
/// 2. 在 TestScene_A 放一塊 BOSS 戰測試區（空地、圍牆、兩面掩護牆）並放入 Boss.prefab；同時移除 A13 的假 BOSS。
/// </summary>
public static class BossSetupTools
{
    private const string BossPrefabFolder = "Assets/_Project/Prefabs/Boss";
    private const string BossPrefabPath = BossPrefabFolder + "/Boss.prefab";
    private const string BossVisualPrefabPath = BossPrefabFolder + "/BossVisual.prefab";
    private const string RingPrefabPath = BossPrefabFolder + "/SmokeRing.prefab";
    private const string RingVisualPrefabPath = BossPrefabFolder + "/SmokeRingVisual.prefab";
    private const string MaterialFolder = "Assets/_Project/Materials/Boss";
    private const string TestMaterialFolder = "Assets/_Project/Materials/TestA";
    private const string ScenePath = "Assets/_Project/Scenes/TestScene_A.unity";
    private const string ArenaRootName = "A14_BossArena";
    private const string A13RootName = "A13_ItemTestObjects";
    private const string FakeBossName = "FakeBoss";

    /// <summary>BOSS 測試區中心（玩家出生在 (0, 0, -10)；避開 A6 的兩個假煙霧來源）。</summary>
    private static readonly Vector3 ArenaCenter = new Vector3(0f, 0f, 45f);

    /// <summary>選單：建立（或更新）BOSS 與菸圈的 Prefab。</summary>
    [MenuItem("Tools/Smokee/A - A14 建立 BOSS Prefab")]
    public static void CreateBossPrefabsMenu()
    {
        string report = CreateBossPrefabs(true);
        EditorUtility.DisplayDialog("A14 BOSS Prefab", report +
            "\n\nB 之後換外觀：只打開 BossVisual.prefab／SmokeRingVisual.prefab，刪掉暫時的膠囊或球，放入模型、動畫、粒子。" +
            "不要改 Boss.prefab、SmokeRing.prefab，Visual 裡不要加碰撞體或腳本邏輯。", "好");
    }

    /// <summary>選單：在 TestScene_A 放 BOSS 戰測試區並放入 Boss.prefab（重複執行會先刪掉舊的再重放）。</summary>
    [MenuItem("Tools/Smokee/A - A14 放置 BOSS 戰測試區")]
    public static void BuildBossArenaMenu()
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

        // Prefab 還沒建立就先建（已經存在就不詢問、不覆蓋）
        string prefabReport = CreateBossPrefabs(false);
        GameObject bossPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPrefabPath);
        if (bossPrefab == null)
        {
            EditorUtility.DisplayDialog("失敗", "找不到 " + BossPrefabPath + "，請先執行「A - A14 建立 BOSS Prefab」。", "好");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        bool hasPlayer = false;
        bool hasPhaseManager = false;
        bool removedFakeBoss = false;
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == ArenaRootName)
            {
                Object.DestroyImmediate(rootObject);
                continue;
            }
            if (rootObject.name == A13RootName)
            {
                Transform fakeBoss = rootObject.transform.Find(FakeBossName);
                if (fakeBoss != null)
                {
                    Object.DestroyImmediate(fakeBoss.gameObject);
                    removedFakeBoss = true;
                }
            }
            if (rootObject.GetComponentInChildren<PlayerController>(true) != null)
            {
                hasPlayer = true;
            }
            if (rootObject.GetComponentInChildren<PhaseManager>(true) != null)
            {
                hasPhaseManager = true;
            }
        }

        // 場景裡其他地方還有 BOSS（不在測試區底下）時提醒，避免有兩個 BOSS
        bool hasOtherBoss = Object.FindFirstObjectByType<BossController>(FindObjectsInactive.Include) != null;

        BuildArena(bossPrefab);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        string message = "BOSS 戰測試區已放好並存檔：玩家出生點正前方（北邊）約 40 公尺，南邊牆中間有入口。\n" +
                         (string.IsNullOrEmpty(prefabReport) ? "" : prefabReport + "\n") +
                         (removedFakeBoss ? "已從場景移除 A13 的假 BOSS（FakeBoss）。請在 Project 視窗刪除 Scripts/DevTools/FakeBoss.cs。\n" : "") +
                         "\n測試：按 2 進入第三階段 → 按 9 開始 BOSS 戰。";
        if (!hasPlayer)
        {
            message += "\n\n注意：場景裡沒有 Player。請先執行「A - 建立 TestScene_A 測試環境」。";
        }
        if (!hasPhaseManager)
        {
            message += "\n\n注意：場景裡沒有 PhaseManager，按 2 無法進入第三階段。請先執行「A - A11 設定 TestScene_A」。";
        }
        if (hasOtherBoss)
        {
            message += "\n\n注意：場景裡原本就有另一個 BOSS（不在 " + ArenaRootName + " 底下），請刪掉多的那一個。";
        }
        EditorUtility.DisplayDialog("完成", message, "好");
    }

    /// <summary>
    /// 建立 4 個 Prefab。askOverwrite = true 時，A 的 Prefab 已經存在會詢問要不要覆蓋；false 時已經存在就跳過。
    /// 回傳做了什麼（給對話框顯示）。
    /// </summary>
    private static string CreateBossPrefabs(bool askOverwrite)
    {
        EnsureFolder(BossPrefabFolder);
        EnsureFolder(MaterialFolder);
        string report = "";

        // 1. B 的外觀 Prefab：已經存在就不動
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BossVisualPrefabPath) == null)
        {
            CreateBossVisualPrefab();
            report += "已建立 BossVisual.prefab（B 負責，暫時外觀）。\n";
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RingVisualPrefabPath) == null)
        {
            CreateRingVisualPrefab();
            report += "已建立 SmokeRingVisual.prefab（B 負責，暫時外觀）。\n";
        }

        // 2. A 的 Prefab：菸圈要先建，BOSS 才能引用它
        if (ShouldBuild(RingPrefabPath, "SmokeRing.prefab", askOverwrite))
        {
            CreateRingPrefab();
            report += "已建立 SmokeRing.prefab。\n";
        }
        if (ShouldBuild(BossPrefabPath, "Boss.prefab", askOverwrite))
        {
            CreateBossPrefab();
            report += "已建立 Boss.prefab。\n";
        }

        AssetDatabase.SaveAssets();
        return report.TrimEnd('\n');
    }

    private static bool ShouldBuild(string path, string displayName, bool askOverwrite)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
        {
            return true;
        }
        if (!askOverwrite)
        {
            return false;
        }
        return EditorUtility.DisplayDialog("覆蓋 " + displayName + "？",
            displayName + " 已經存在。覆蓋會把 Inspector 數值恢復成預設（場景裡的 BOSS 引用不會斷）。\n" +
            "B 的 Visual Prefab 不會被覆蓋。",
            "覆蓋", "保留原本的");
    }

    /// <summary>BossVisual.prefab：空的根物件＋暫時的膠囊（高 4 公尺、半徑 1，約玩家 2 倍大；沒有碰撞體）。</summary>
    private static void CreateBossVisualPrefab()
    {
        Material material = GetOrCreateMaterial(MaterialFolder, "Boss_Placeholder", new Color(0.3f, 0.3f, 0.32f));
        GameObject root = new GameObject("BossVisual");
        GameObject capsule = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        capsule.name = "Placeholder_Capsule";
        capsule.transform.SetParent(root.transform, false);
        capsule.transform.localPosition = new Vector3(0f, 2f, 0f);
        capsule.transform.localScale = new Vector3(2f, 2f, 2f);
        Object.DestroyImmediate(capsule.GetComponent<Collider>());
        capsule.GetComponent<Renderer>().sharedMaterial = material;

        PrefabUtility.SaveAsPrefabAsset(root, BossVisualPrefabPath);
        Object.DestroyImmediate(root);
    }

    /// <summary>SmokeRingVisual.prefab：空的根物件＋暫時的扁球（直徑 1.6 = 命中半徑 0.8；面向飛行方向；沒有碰撞體）。</summary>
    private static void CreateRingVisualPrefab()
    {
        Material material = GetOrCreateMaterial(MaterialFolder, "SmokeRing_Placeholder", new Color(0.8f, 0.8f, 0.8f));
        GameObject root = new GameObject("SmokeRingVisual");
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Placeholder_Ring";
        sphere.transform.SetParent(root.transform, false);
        sphere.transform.localScale = new Vector3(1.6f, 1.6f, 0.5f);
        Object.DestroyImmediate(sphere.GetComponent<Collider>());
        sphere.GetComponent<Renderer>().sharedMaterial = material;

        PrefabUtility.SaveAsPrefabAsset(root, RingVisualPrefabPath);
        Object.DestroyImmediate(root);
    }

    /// <summary>SmokeRing.prefab：BossSmokeRing（沒有碰撞體，用程式判定命中）＋巢狀 SmokeRingVisual.prefab。</summary>
    private static void CreateRingPrefab()
    {
        GameObject root = new GameObject("SmokeRing");
        root.AddComponent<BossSmokeRing>();
        AddNestedPrefab(RingVisualPrefabPath, root.transform);

        PrefabUtility.SaveAsPrefabAsset(root, RingPrefabPath);
        Object.DestroyImmediate(root);
    }

    /// <summary>
    /// Boss.prefab：根物件放 4 個腳本與膠囊碰撞體（實心，玩家穿不過去；近戰、噴霧打得到），
    /// 子物件 RingMuzzle（菸圈發射點，離地 2 公尺）＋巢狀 BossVisual.prefab。
    /// </summary>
    private static void CreateBossPrefab()
    {
        GameObject root = new GameObject("Boss");

        CapsuleCollider collider = root.AddComponent<CapsuleCollider>();
        collider.center = new Vector3(0f, 2f, 0f);
        collider.height = 4f;
        collider.radius = 1f;

        root.AddComponent<BossController>();
        root.AddComponent<BossHealth>();
        BossAttack attack = root.AddComponent<BossAttack>();
        SmokeSource smokeSource = root.AddComponent<SmokeSource>();

        GameObject muzzle = new GameObject("RingMuzzle");
        muzzle.transform.SetParent(root.transform, false);
        muzzle.transform.localPosition = new Vector3(0f, 2f, 0f);

        AddNestedPrefab(BossVisualPrefabPath, root.transform);

        // SmokeSource：BOSS 半徑 7、不受拒絕影響（BossController 會在 BOSS 戰開始時才讓它冒煙）
        SerializedObject serializedSmoke = new SerializedObject(smokeSource);
        serializedSmoke.FindProperty("_radius").floatValue = 7f;
        serializedSmoke.FindProperty("_affectedByRefusal").boolValue = false;
        serializedSmoke.ApplyModifiedPropertiesWithoutUndo();

        // BossAttack：菸圈 Prefab 與發射點
        SerializedObject serializedAttack = new SerializedObject(attack);
        serializedAttack.FindProperty("_smokeRingPrefab").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<BossSmokeRing>(RingPrefabPath);
        serializedAttack.FindProperty("_ringMuzzle").objectReferenceValue = muzzle.transform;
        serializedAttack.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, BossPrefabPath);
        Object.DestroyImmediate(root);
    }

    private static void AddNestedPrefab(string prefabPath, Transform parent)
    {
        GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (asset == null)
        {
            Debug.LogWarning("BossSetupTools：找不到 " + prefabPath + "，外觀沒有放進去。");
            return;
        }
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(asset);
        instance.transform.SetParent(parent, false);
    }

    /// <summary>
    /// 測試區（中心 (0, 0, 45)，30 x 30）：北、東、西三面牆，南牆中間留 6 公尺入口；
    /// 中間偏南有兩面掩護牆（躲在後面菸圈會撞牆消失）；BOSS 在中心往北 7 公尺。
    /// </summary>
    private static void BuildArena(GameObject bossPrefab)
    {
        Material wallMaterial = GetOrCreateMaterial(TestMaterialFolder, "TestA_ArenaWall", new Color(0.55f, 0.6f, 0.7f));
        Transform root = new GameObject(ArenaRootName).transform;
        root.position = ArenaCenter;

        CreateBlock("Wall_North", root, new Vector3(0f, 2f, 15f), new Vector3(30f, 4f, 0.5f), wallMaterial);
        CreateBlock("Wall_East", root, new Vector3(15f, 2f, 0f), new Vector3(0.5f, 4f, 30f), wallMaterial);
        CreateBlock("Wall_West", root, new Vector3(-15f, 2f, 0f), new Vector3(0.5f, 4f, 30f), wallMaterial);
        CreateBlock("Wall_South_Left", root, new Vector3(-9f, 2f, -15f), new Vector3(12f, 4f, 0.5f), wallMaterial);
        CreateBlock("Wall_South_Right", root, new Vector3(9f, 2f, -15f), new Vector3(12f, 4f, 0.5f), wallMaterial);
        CreateBlock("Cover_Left", root, new Vector3(-5f, 1.5f, -3f), new Vector3(4f, 3f, 0.5f), wallMaterial);
        CreateBlock("Cover_Right", root, new Vector3(5f, 1.5f, -3f), new Vector3(4f, 3f, 0.5f), wallMaterial);

        GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(bossPrefab);
        boss.name = "Boss";
        boss.transform.SetParent(root, false);
        boss.transform.localPosition = new Vector3(0f, 0f, 7f);
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

    private static Material GetOrCreateMaterial(string folder, string materialName, Color color)
    {
        EnsureFolder(folder);
        string path = folder + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            Debug.LogWarning("BossSetupTools：找不到 URP Lit Shader，改用 Standard。");
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
