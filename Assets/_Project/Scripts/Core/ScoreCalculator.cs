using UnityEngine;

/// <summary>
/// 結算評分，規則依 docs/DECISIONS.md：
/// 總分 = 血量分 × 0.6 + 時間分 × 0.4；
/// 血量分 = HP ÷ 最大 HP × 100；
/// 時間分：勝利 min(剩餘秒數 ÷ 180 × 100, 100)，失敗 0；
/// 評級 S ≥ 90、A ≥ 80、B ≥ 70、C ≥ 60、D &lt; 60，失敗局一律 D。
/// </summary>
public static class ScoreCalculator
{
    private const float HpWeight = 0.6f;
    private const float TimeWeight = 0.4f;
    private const float FullTimeScoreSeconds = 180f;

    /// <summary>
    /// 依結算資料計算分數與評級。
    /// </summary>
    public static ScoreResult Calculate(bool isWin, float hp, float maxHp,
                                        float remainingSeconds, float roundDuration,
                                        float smokeSeconds)
    {
        float clampedHp = Mathf.Max(hp, 0f);
        float clampedRemaining = Mathf.Max(remainingSeconds, 0f);

        float hpScore = maxHp > 0f ? Mathf.Clamp(clampedHp / maxHp * 100f, 0f, 100f) : 0f;
        float timeScore = isWin
            ? Mathf.Min(clampedRemaining / FullTimeScoreSeconds * 100f, 100f)
            : 0f;
        float totalScore = hpScore * HpWeight + timeScore * TimeWeight;

        return new ScoreResult
        {
            IsWin = isWin,
            RemainingHp = clampedHp,
            ClearTimeSeconds = Mathf.Max(roundDuration - clampedRemaining, 0f),
            RemainingSeconds = clampedRemaining,
            SmokeSeconds = smokeSeconds,
            HpScore = hpScore,
            TimeScore = timeScore,
            TotalScore = totalScore,
            Grade = GetGrade(isWin, totalScore),
        };
    }

    private static string GetGrade(bool isWin, float totalScore)
    {
        if (!isWin) return "D";
        if (totalScore >= 90f) return "S";
        if (totalScore >= 80f) return "A";
        if (totalScore >= 70f) return "B";
        if (totalScore >= 60f) return "C";
        return "D";
    }
}
