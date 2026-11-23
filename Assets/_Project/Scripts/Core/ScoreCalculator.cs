using System;
using UnityEngine;

/// <summary>
/// 結算評分，規則依 docs/DECISIONS.md（v3.1）：
/// 總分 = 血量分 × 0.6 + 用時分 × 0.4；
/// 血量分 = HP ÷ 最大 HP × 100；
/// 用時分：用時 ≤ 8 分鐘得 100，之後每「滿」30 秒 −10，最低 0；失敗局 0；
/// 評級 S ≥ 90、A ≥ 80、B ≥ 70、C ≥ 60、D &lt; 60，失敗局一律 D。頭銜在 A18 加入。
/// </summary>
public static class ScoreCalculator
{
    private const float HpWeight = 0.6f;
    private const float TimeWeight = 0.4f;
    private const float FullTimeScoreSeconds = 180f;

    private const float FullScoreElapsedSeconds = 480f;
    private const float PenaltyStepSeconds = 30f;
    private const float PenaltyPerStep = 10f;

    /// <summary>
    /// v3.1 評分：依結算資料（用時）計算分數與評級。頭銜（Title）在 A18 才填。
    /// </summary>
    public static ScoreResult Calculate(bool isWin, float hp, float maxHp,
                                        float elapsedSeconds, float smokeSeconds)
    {
        float clampedHp = Mathf.Max(hp, 0f);
        float clampedElapsed = Mathf.Max(elapsedSeconds, 0f);

        float hpScore = maxHp > 0f ? Mathf.Clamp(clampedHp / maxHp * 100f, 0f, 100f) : 0f;
        float timeScore = isWin ? GetTimeScore(clampedElapsed) : 0f;
        float totalScore = hpScore * HpWeight + timeScore * TimeWeight;

        return new ScoreResult
        {
            IsWin = isWin,
            RemainingHp = clampedHp,
            ClearTimeSeconds = clampedElapsed,
            SmokeSeconds = smokeSeconds,
            HpScore = hpScore,
            TimeScore = timeScore,
            TotalScore = totalScore,
            Grade = GetGrade(isWin, totalScore),
            Title = "",
        };
    }

    /// <summary>
    /// 【即將移除】舊版評分（倒數用）：時間分 = min(剩餘秒數 ÷ 180 × 100, 100)。
    /// </summary>
    [Obsolete("v3.1 取消倒數，請改用 Calculate(isWin, hp, maxHp, elapsedSeconds, smokeSeconds)。A11 刪除。")]
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
            Title = "",
        };
    }

    /// <summary>用時分：≤ 480 秒得 100，之後每「滿」30 秒 −10，最低 0。</summary>
    private static float GetTimeScore(float elapsedSeconds)
    {
        if (elapsedSeconds <= FullScoreElapsedSeconds)
        {
            return 100f;
        }

        int steps = Mathf.FloorToInt((elapsedSeconds - FullScoreElapsedSeconds) / PenaltyStepSeconds);
        return Mathf.Max(100f - steps * PenaltyPerStep, 0f);
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
