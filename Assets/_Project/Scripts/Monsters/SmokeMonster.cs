using System;
using System.Collections;
using System.Collections.Generic; // [I1 整合] All 清單用
using UnityEngine;



/// <summary>
/// 菸怪行為：受擊計數、消散、通知擊敗加分
/// </summary>
[RequireComponent(typeof(SmokeSource))]
public class SmokeMonster : MonoBehaviour, IHittable // [I1 整合] 實作 IHittable，讓 PlayerAttack 打得到
{
    /// <summary>[I1 整合] 被擊敗時增加的淨化進度（4小1大配置：小 15% / 大 40%，全滅剛好 100%）</summary>
    public float ProgressValue => size == MonsterSize.Small ? 15f : 40f;

    // [I1 整合] 規格（INTERFACES.md）名稱的唯讀屬性。不改原本欄位名稱，避免 Inspector 已存的設定遺失
    private static readonly List<SmokeMonster> allMonsters = new List<SmokeMonster>();

    /// <summary>[I1 整合] 目前場景中所有菸怪（C 的小地圖用來顯示剩餘菸怪）</summary>
    public static IReadOnlyList<SmokeMonster> All => allMonsters;
    /// <summary>[I1 整合] 菸怪大小（規格名稱）</summary>
    public MonsterSize Size => size;
    /// <summary>[I1 整合] 擊敗所需次數（規格名稱）</summary>
    public int HitsRequired => hitsRequired;
    /// <summary>[I1 整合] 是否已被擊敗</summary>
    public bool IsDefeated => isDead;

    [Header("怪獸規格")]
    public MonsterSize size = MonsterSize.Small;
    [Tooltip("擊敗所需次數")]
    public int hitsRequired = 3;

    [Header("煙霧粒子元件")]
    [Tooltip("煙霧粒子系統 (擊敗時會停止播放)")]
    public ParticleSystem smokeParticles;

    private int currentHits = 0;
    private bool isDead = false;
    private SmokeSource smokeSource;

    private void Awake()
    {
        smokeSource = GetComponent<SmokeSource>();
        // 依企劃書設定預設次數
        if (size == MonsterSize.Small)
        {
            hitsRequired = 3;
        }
        else
        {
            hitsRequired = 10;
        }
    }

    // [I1 整合] 出現時加入 All，消失時移除
    private void OnEnable()
    {
        if (!allMonsters.Contains(this)) allMonsters.Add(this);
    }

    private void OnDisable()
    {
        allMonsters.Remove(this);
    }

    /// <summary>
    /// 被玩家攻擊時呼叫（供 A 組員的 PlayerAttack 調用）
    /// </summary>
    public void TakeHit(int hitCount) // [I1 整合] 依 IHittable 加上 hitCount 參數
    {
        if (isDead) return;

        currentHits += Mathf.Max(1, hitCount); // [I1 整合] 一般攻擊 +1；吸入劑效果時一次 +3
        Debug.Log($"[菸怪受擊] {gameObject.name} 承受打擊 ({currentHits}/{hitsRequired})");

        StartCoroutine(HitFlashEffect());

        if (currentHits >= hitsRequired)
        {
            Die();
        }
    }

    private IEnumerator HitFlashEffect()
    {
        Vector3 baseScale = transform.localScale;
        transform.localScale = baseScale * 1.15f;
        yield return new WaitForSeconds(0.08f);
        transform.localScale = baseScale;
    }

    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"[菸怪消散] {gameObject.name} 已被擊敗！");

        // 停止散發煙霧
        if (smokeParticles != null)
        {
            smokeParticles.Stop();
        }
        if (smokeSource != null)
        {
            smokeSource.IsEmitting = false; // [I1 整合] 改用規格名稱（原本是 isActive 別名）
            smokeSource.enabled = false;
        }

        // [I1 整合] 關掉碰撞體，消散的 1 秒內不會再被攻擊鎖定
        foreach (Collider col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }

        // 通知 GameManager 增加淨化進度
        // [I1 整合] 改成直接呼叫 GameManager（原本的 static 事件沒有人接收）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddPurificationProgress(ProgressValue);
        }

        // 延遲 1 秒後移除物件
        Destroy(gameObject, 1.0f);
    }
}