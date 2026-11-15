using System;
using System.Collections;
using UnityEngine;

public enum MonsterSize
{
    Small, // 小型菸怪 (3 下擊敗)
    Large  // 大型菸怪 (10 下擊敗)
}

/// <summary>
/// 菸怪行為：受擊計數、消散、通知擊敗加分
/// </summary>
[RequireComponent(typeof(SmokeSource))]
public class SmokeMonster : MonoBehaviour
{
    // 對外擊敗廣播事件，供 A 組員的 GameManager 接收以增加淨化進度
    public static event Action<MonsterSize> OnMonsterDefeated;

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

    /// <summary>
    /// 被玩家攻擊時呼叫（供 A 組員的 PlayerAttack 調用）
    /// </summary>
    public void TakeHit()
    {
        if (isDead) return;

        currentHits++;
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
            smokeSource.isActive = false;
            smokeSource.enabled = false;
        }

        // 通知 GameManager 增加淨化進度
        OnMonsterDefeated?.Invoke(size);

        // 延遲 1 秒後移除物件
        Destroy(gameObject, 1.0f);
    }
}