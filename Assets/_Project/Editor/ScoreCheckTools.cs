using UnityEditor;
using UnityEngine;

/// <summary>
/// A8：檢查 ScoreCalculator 的計算結果（Unity 上方選單 Tools/Dev/Run Score Checks）。
/// 不需要按 Play，也不會修改任何場景或資產；結果印在 Console。
/// 每個案例固定使用：最大 HP 100、局長 420 秒、SmokeSeconds 0。
/// </summary>
public static class ScoreCheckTools
{
    private const float MaxHp = 100f;
    private const float RoundDuration = 420f;
    private const float Tolerance = 0.01f;

    /// <summary>一個測試案例：輸入與預期結果。</summary>
    private struct ScoreCase
    {
        public string Name;
        public bool IsWin;
        public float Hp;
        public float RemainingSeconds;
        public float ExpectedTotal;
        public string ExpectedGrade;

        public ScoreCase(string name, bool isWin, float hp, float remainingSeconds,
                         float expectedTotal, string expectedGrade)
        {
            Name = name;
            IsWin = isWin;
            Hp = hp;
            RemainingSeconds = remainingSeconds;
            ExpectedTotal = expectedTotal;
            ExpectedGrade = expectedGrade;
        }
    }

    private static readonly ScoreCase[] Cases =
    {
        new ScoreCase("勝利、HP 100、剩 200 秒", true, 100f, 200f, 100f, "S"),
        new ScoreCase("勝利、HP 90、剩 180 秒", true, 90f, 180f, 94f, "S"),
        new ScoreCase("勝利、HP 75、剩 135 秒", true, 75f, 135f, 75f, "B"),
        new ScoreCase("勝利、HP 50、剩 90 秒", true, 50f, 90f, 50f, "D"),
        new ScoreCase("失敗、HP 80、剩 0 秒", false, 80f, 0f, 48f, "D"),
        new ScoreCase("失敗（HP 歸零）、HP 0、剩 100 秒", false, 0f, 100f, 0f, "D"),
        new ScoreCase("【邊界】勝利、HP 100、剩 135 秒（剛好 90 分）", true, 100f, 135f, 90f, "S"),
    };

    /// <summary>選單：依序跑所有案例，在 Console 印出 PASS／FAIL 與實際計算結果。</summary>
    [MenuItem("Tools/Dev/Run Score Checks")]
    public static void RunScoreChecks()
    {
        int passed = 0;

        for (int i = 0; i < Cases.Length; i++)
        {
            ScoreCase c = Cases[i];
            ScoreResult result = ScoreCalculator.Calculate(c.IsWin, c.Hp, MaxHp, c.RemainingSeconds, RoundDuration, 0f);

            float expectedClearTime = RoundDuration - c.RemainingSeconds;
            bool totalOk = Mathf.Abs(result.TotalScore - c.ExpectedTotal) <= Tolerance;
            bool gradeOk = result.Grade == c.ExpectedGrade;
            bool clearTimeOk = Mathf.Abs(result.ClearTimeSeconds - expectedClearTime) <= Tolerance;
            bool ok = totalOk && gradeOk && clearTimeOk;

            string detail = "血量分 " + result.HpScore.ToString("F2")
                            + "、時間分 " + result.TimeScore.ToString("F2")
                            + "、總分 " + result.TotalScore.ToString("F4") + "（預期 " + c.ExpectedTotal + "）"
                            + "、評級 " + result.Grade + "（預期 " + c.ExpectedGrade + "）"
                            + "、通關秒數 " + result.ClearTimeSeconds.ToString("F1") + "（預期 " + expectedClearTime + "）";

            if (ok)
            {
                passed++;
                Debug.Log("[ScoreCheck] PASS｜" + c.Name + "｜" + detail);
            }
            else
            {
                Debug.LogError("[ScoreCheck] FAIL｜" + c.Name + "｜" + detail);
            }
        }

        string summary = "[ScoreCheck] 結果：" + passed + " / " + Cases.Length + " 通過";
        if (passed == Cases.Length)
        {
            Debug.Log(summary);
        }
        else
        {
            Debug.LogError(summary);
        }
    }
}
