using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// A15 的 Editor 選單（Unity 上方選單 Tools/Smokee）：在 TestScene_A 放「拒絕幫忙」測試區。
/// - 4 隻 B 的 SmokeMonster_Small（受拒絕影響：SmokeSource 看到同物件有 SmokeMonster 會自動勾選）
/// - 1 個「不是菸怪」的假吸菸 NPC（FakeSmokerNPC ＋ SmokeSource 半徑 4，不受拒絕影響）
/// - 2 個站位標記（沒有碰撞體）：離菸怪／假吸菸 NPC 6 公尺（煙霧範圍外：拒絕中暴露值仍然上升、沒有拒絕時下降）
/// - 把 SmokeMonster_Small 拖進 DevHotkeys 的「\ 鍵生成」欄位
/// - GameManager 物件掛上 RefusalMonsterSpawner（拒絕時在玩家附近補生成菸怪），拖入同一個 Prefab
/// 重複執行會先刪掉舊的測試區再重放。
/// </summary>
public static class RefusalTestSetup
{
    private const string ScenePath = "Assets/_Project/Scenes/TestScene_A.unity";
    private const string MonsterPrefabPath = "Assets/_Project/Prefabs/SmokeMonster_Small.prefab";
    private const string RootName = "A15_RefusalTest";
    private const float RowZ = -50f;

    /// <summary>選單：在 TestScene_A 放拒絕幫忙測試區並存檔。</summary>
    [MenuItem("Tools/Smokee/A - A15 放置拒絕幫忙測試區")]
    public static void BuildRefusalTestMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
        {
            EditorUtility.DisplayDialog("找不到場景", "找不到 " + ScenePath + "，請確認場景存在。", "好");
            return;
        }
        GameObject monsterPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(MonsterPrefabPath);
        if (monsterPrefab == null)
        {
            EditorUtility.DisplayDialog("找不到菸怪 Prefab", "找不到 " + MonsterPrefabPath + "，請先從 develop 拉下 B 的菸怪。", "好");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        DevHotkeys hotkeys = null;
        bool hasPhaseManager = false;
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            if (rootObject.name == RootName)
            {
                Object.DestroyImmediate(rootObject);
                continue;
            }
            if (hotkeys == null)
            {
                hotkeys = rootObject.GetComponentInChildren<DevHotkeys>(true);
            }
            if (rootObject.GetComponentInChildren<PhaseManager>(true) != null)
            {
                hasPhaseManager = true;
            }
        }

        GameObject root = new GameObject(RootName);
        root.transform.position = Vector3.zero;

        // 4 隻菸怪，間隔 16 公尺：原本半徑 4 互不重疊，2 倍（8 公尺）時剛好碰在一起
        float[] xs = { -24f, -8f, 8f, 24f };
        for (int i = 0; i < xs.Length; i++)
        {
            GameObject monster = (GameObject)PrefabUtility.InstantiatePrefab(monsterPrefab, scene);
            monster.name = "Monster_" + (i + 1);
            monster.transform.SetParent(root.transform, false);
            monster.transform.position = new Vector3(xs[i], 0f, RowZ);
        }

        // 不是菸怪的煙霧來源：假吸菸 NPC（不受拒絕影響）
        GameObject smoker = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        smoker.name = "FakeSmoker_NotMonster";
        smoker.transform.SetParent(root.transform, false);
        smoker.transform.position = new Vector3(0f, 1f, RowZ - 14f);
        smoker.AddComponent<FakeSmokerNPC>();
        SmokeSource smokerSource = smoker.AddComponent<SmokeSource>();
        SerializedObject serializedSource = new SerializedObject(smokerSource);
        serializedSource.FindProperty("_radius").floatValue = 4f;
        serializedSource.FindProperty("_affectedByRefusal").boolValue = false;
        serializedSource.ApplyModifiedPropertiesWithoutUndo();

        // 站位標記：離 Monster_1、假吸菸 NPC 各 6 公尺（朝玩家出生點的方向）
        CreateMarker("Stand_6m_FromMonster1", root.transform, new Vector3(xs[0], 0f, RowZ + 6f));
        CreateMarker("Stand_6m_FromFakeSmoker", root.transform, new Vector3(0f, 0f, RowZ - 14f + 6f));

        // 拒絕時補生成菸怪：掛在 GameManager 物件上（和 PhaseManager 同一個），拖入菸怪 Prefab
        bool setSpawner = false;
        GameManager gameManager = Object.FindFirstObjectByType<GameManager>(FindObjectsInactive.Include);
        if (gameManager != null)
        {
            RefusalMonsterSpawner spawner = gameManager.GetComponent<RefusalMonsterSpawner>();
            if (spawner == null)
            {
                spawner = gameManager.gameObject.AddComponent<RefusalMonsterSpawner>();
            }
            SerializedObject serializedSpawner = new SerializedObject(spawner);
            serializedSpawner.FindProperty("_monsterPrefab").objectReferenceValue = monsterPrefab;
            serializedSpawner.ApplyModifiedPropertiesWithoutUndo();
            setSpawner = true;
        }

        // DevHotkeys 的 \ 鍵生成菸怪
        bool setHotkeys = false;
        if (hotkeys != null)
        {
            SerializedObject serializedHotkeys = new SerializedObject(hotkeys);
            SerializedProperty prefabProperty = serializedHotkeys.FindProperty("_devMonsterPrefab");
            if (prefabProperty != null)
            {
                prefabProperty.objectReferenceValue = monsterPrefab;
                serializedHotkeys.ApplyModifiedPropertiesWithoutUndo();
                setHotkeys = true;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Selection.activeGameObject = root;

        string message = "拒絕幫忙測試區已放好並存檔：玩家出生點後方（南邊）約 40 公尺。\n" +
                         "・Monster_1～4：B 的 SmokeMonster_Small（受拒絕影響）\n" +
                         "・FakeSmoker_NotMonster：不是菸怪的煙霧來源（不受影響）\n" +
                         "・兩個白色圓盤：離菸怪／假吸菸 NPC 6 公尺的站位\n" +
                         (setHotkeys ? "・DevHotkeys 的 \\ 鍵已設定為生成 SmokeMonster_Small\n" : "") +
                         (setSpawner ? "・GameManager 物件已掛上 RefusalMonsterSpawner（拒絕時補生成菸怪）\n" : "");
        if (!setSpawner)
        {
            message += "\n注意：場景裡找不到 GameManager，沒有掛上 RefusalMonsterSpawner。請先執行「A - 放置 A5 GameManager」。";
        }
        if (!setHotkeys)
        {
            message += "\n注意：場景裡找不到 DevHotkeys，\\ 鍵無法生成菸怪。請先執行「A - A11 設定 TestScene_A」。";
        }
        if (!hasPhaseManager)
        {
            message += "\n注意：場景裡沒有 PhaseManager，[ ] 鍵無效。請先執行「A - A11 設定 TestScene_A」。";
        }
        EditorUtility.DisplayDialog("A15 完成", message, "好");
    }

    /// <summary>建立一個沒有碰撞體的扁圓盤，當作站位標記。</summary>
    private static void CreateMarker(string name, Transform parent, Vector3 position)
    {
        GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        marker.name = name;
        marker.transform.SetParent(parent, false);
        marker.transform.position = position + new Vector3(0f, 0.02f, 0f);
        marker.transform.localScale = new Vector3(1f, 0.02f, 1f);
        Object.DestroyImmediate(marker.GetComponent<Collider>());
    }
}
