using UnityEditor;
using UnityEngine;

/// <summary>
/// 檢查 ScoreCalculator 的計算結果（Unity 上方選單 Tools/Dev/Run Score Checks）。
/// 不需要按 Play，也不會修改任何場景或資產；結果印在 Console。
/// A11：改用「用時」。用時分 = 100 − 超過 480 秒的秒數 ÷ 30 × 10，四捨五入到整數（.5 進位），最低 0；失敗局 0。
/// 每個案例固定使用：最大 HP 100、SmokeSeconds 0。
/// </summary>
public static class ScoreCheckTools
{
    private const float MaxHp = 100f;
    private const float Tolerance = 0.01f;

    /// <summary>一個測試案例：輸入與預期結果。</summary>
    private struct ScoreCase
    {
        public string Name;
        public bool IsWin;
        public float Hp;
        public float ElapsedSeconds;
        public float ExpectedTimeScore;
        public float ExpectedTotal;
        public string ExpectedGrade;

        public ScoreCase(string name, bool isWin, float hp, float elapsedSeconds,
                         float expectedTimeScore, float expectedTotal, string expectedGrade)
        {
            Name = name;
            IsWin = isWin;
            Hp = hp;
            ElapsedSeconds = elapsedSeconds;
            ExpectedTimeScore = expectedTimeScore;
            ExpectedTotal = expectedTotal;
            ExpectedGrade = expectedGrade;
        }
    }

    private static readonly ScoreCase[] Cases =
    {
        new ScoreCase("勝利、HP 100、用時 7:59", true, 100f, 479f, 100f, 100f, "S"),
        new ScoreCase("【邊界】勝利、HP 100、用時 8:00", true, 100f, 480f, 100f, 100f, "S"),
        new ScoreCase("勝利、HP 100、用時 8:01（99.67 → 100）", true, 100f, 481f, 100f, 100f, "S"),
        new ScoreCase("勝利、HP 100、用時 8:05（98.33 → 98）", true, 100f, 485f, 98f, 99.2f, "S"),
        new ScoreCase("勝利、HP 80、用時 8:15（95）", true, 80f, 495f, 95f, 86f, "A"),
        new ScoreCase("【邊界 .5】勝利、HP 100、用時 8:16.5（94.5 → 95）", true, 100f, 496.5f, 95f, 98f, "S"),
        new ScoreCase("勝利、HP 100、用時 8:29（90.33 → 90）", true, 100f, 509f, 90f, 96f, "S"),
        new ScoreCase("【邊界】勝利、HP 100、用時 9:15（用時分 75，總分剛好 90）", true, 100f, 555f, 75f, 90f, "S"),
        new ScoreCase("勝利、HP 75、用時 10:00（60）", true, 75f, 600f, 60f, 69f, "C"),
        new ScoreCase("勝利、HP 50、用時 12:58（0.67 → 1）", true, 50f, 778f, 1f, 30.4f, "D"),
        new ScoreCase("勝利、HP 100、用時 12:59（0.33 → 0）", true, 100f, 779f, 0f, 60f, "C"),
        new ScoreCase("勝利、HP 100、用時 15:00（最低 0）", true, 100f, 900f, 0f, 60f, "C"),
        new ScoreCase("失敗、HP 80、用時 5:00（失敗局用時分 0、評級 D）", false, 80f, 300f, 0f, 48f, "D"),
        new ScoreCase("失敗（HP 歸零）、HP 0、用時 10:00", false, 0f, 600f, 0f, 0f, "D"),
    };

    /// <summary>選單：依序跑所有案例，在 Console 印出 PASS／FAIL 與實際計算結果。</summary>
    [MenuItem("Tools/Dev/Run Score Checks")]
    public static void RunScoreChecks()
    {
        int passed = 0;

        for (int i = 0; i < Cases.Length; i++)
        {
            ScoreCase c = Cases[i];
            ScoreResult result = ScoreCalculator.Calculate(c.IsWin, c.Hp, MaxHp, c.ElapsedSeconds, 0f);

            bool timeScoreOk = Mathf.Abs(result.TimeScore - c.ExpectedTimeScore) <= Tolerance;
            bool totalOk = Mathf.Abs(result.TotalScore - c.ExpectedTotal) <= Tolerance;
            bool gradeOk = result.Grade == c.ExpectedGrade;
            bool clearTimeOk = Mathf.Abs(result.ClearTimeSeconds - c.ElapsedSeconds) <= Tolerance;
            bool ok = timeScoreOk && totalOk && gradeOk && clearTimeOk;

            string detail = "血量分 " + result.HpScore.ToString("F2")
                            + "、用時分 " + result.TimeScore.ToString("F2") + "（預期 " + c.ExpectedTimeScore + "）"
                            + "、總分 " + result.TotalScore.ToString("F4") + "（預期 " + c.ExpectedTotal + "）"
                            + "、評級 " + result.Grade + "（預期 " + c.ExpectedGrade + "）"
                            + "、用時 " + result.ClearTimeSeconds.ToString("F1") + "（預期 " + c.ElapsedSeconds + "）";

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
