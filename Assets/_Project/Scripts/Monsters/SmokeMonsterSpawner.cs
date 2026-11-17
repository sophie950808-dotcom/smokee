using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 菸怪隨機生成器：從設定的點位清單中隨機抽取位置生成怪物
/// 支援地圖任意擴展，免重寫代碼
/// </summary>
public class SmokeMonsterSpawner : MonoBehaviour
{
    [Header("菸怪 Prefabs")]
    public GameObject smallMonsterPrefab;
    public GameObject largeMonsterPrefab;

    [Header("生成數量設定 (企劃書 B4 階段: 4 小 1 大)")]
    public int smallCount = 4;
    public int largeCount = 1;

    [Header("可用生成點位 (空物件)")]
    [Tooltip("將地圖上的 SpawnPoint 空物件拖進來")]
    public List<Transform> spawnPoints = new List<Transform>();

    private void Start()
    {
        SpawnMonsters();
    }

    /// <summary>
    /// 執行隨機生成
    /// </summary>
    public void SpawnMonsters()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            Debug.LogWarning("[菸怪生成] 未設置任何生成點！");
            return;
        }

        // 複製一份可用點位列表，避免同一點重複生成兩隻怪
        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        // 1. 隨機生成大型菸怪
        for (int i = 0; i < largeCount; i++)
        {
            if (availablePoints.Count == 0) break;
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform spawnPt = availablePoints[randomIndex];
            availablePoints.RemoveAt(randomIndex); // 移除已使用的點

            if (largeMonsterPrefab != null)
            {
                Instantiate(largeMonsterPrefab, spawnPt.position, spawnPt.rotation);
                Debug.Log($"[生成] 大型菸怪生成於: {spawnPt.name}");
            }
        }

        // 2. 隨機生成小型菸怪
        for (int i = 0; i < smallCount; i++)
        {
            if (availablePoints.Count == 0) break;
            int randomIndex = Random.Range(0, availablePoints.Count);
            Transform spawnPt = availablePoints[randomIndex];
            availablePoints.RemoveAt(randomIndex); // 移除已使用的點

            if (smallMonsterPrefab != null)
            {
                Instantiate(smallMonsterPrefab, spawnPt.position, spawnPt.rotation);
                Debug.Log($"[生成] 小型菸怪生成於: {spawnPt.name}");
            }
        }
    }
}