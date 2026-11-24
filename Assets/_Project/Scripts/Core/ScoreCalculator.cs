using System;
using UnityEngine;

/// <summary>
/// 結算評分，規則依 docs/DECISIONS.md（v3.1，A11 更新）：
/// 總分 = 血量分 × 0.6 + 用時分 × 0.4；
/// 血量分 = HP ÷ 最大 HP × 100；
/// 用時分：用時 ≤ 8 分鐘（480 秒）得 100；超過的部分依秒數比例扣分（每 30 秒 −10），
///         結果四捨五入到整數（剛好 .5 一律進位），最低 0；失敗局 0。
///         例：8:15 → 95、8:16.5 → 95（94.5 進位）、8:29 → 90、12:59 → 0；
/// 評級 S ≥ 90、A ≥ 80、B ≥ 70、C ≥ 60、D &lt; 60，失敗局一律 D。頭銜在 A18 加入。
/// </summary>
public static class ScoreCalculator
{
    private const float HpWeight = 0.6f;
    private const float TimeWeight = 0.4f;

    private const double FullScoreElapsedSeconds = 480.0;
    private const double PenaltyStepSeconds = 30.0;
    private const double PenaltyPerStep = 10.0;

    /// <summary>
    /// v3.1 評分：依結算資料（用時）計算分數與評級。頭銜（Title）在 A18 才填。
    /// elapsedSeconds 是用時（秒）；失敗局用時分固定 0。
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
    /// 用時分：≤ 480 秒得 100；之後依秒數比例扣（每 30 秒 −10），四捨五入到整數（.5 進位），最低 0。
    /// 用 double 計算並以「加 0.5 後無條件捨去」取整數，避免 Mathf.RoundToInt 把 .5 捨到偶數。
    /// </summary>
    private static float GetTimeScore(float elapsedSeconds)
    {
        if (elapsedSeconds <= FullScoreElapsedSeconds)
        {
            return 100f;
        }

        double overSeconds = elapsedSeconds - FullScoreElapsedSeconds;
        double rawScore = 100.0 - overSeconds * PenaltyPerStep / PenaltyStepSeconds;
        double rounded = Math.Floor(rawScore + 0.5 + 1e-9);
        return (float)Math.Max(0.0, Math.Min(100.0, rounded));
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
