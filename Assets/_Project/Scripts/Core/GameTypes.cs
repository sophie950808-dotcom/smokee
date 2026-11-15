/// <summary>
/// 遊戲目前的狀態。
/// </summary>
public enum GameState { Opening, Playing, Paused, Ended }

/// <summary>
/// 開場選擇的路線。路線 A = AcceptSmoke，路線 B = RefuseSmoke。
/// </summary>
public enum OpeningRoute { None, AcceptSmoke, RefuseSmoke }

/// <summary>
/// 菸怪的體型。
/// </summary>
public enum MonsterSize { Small, Large }

/// <summary>
/// 衛教道具種類。
/// </summary>
public enum ItemType { NicotineGum, NicotinePatch, NicotineInhaler, NicotineSpray, Champix, Bupropion }

/// <summary>
/// 結算結果，由 ScoreCalculator 產生，ResultUI（C）讀取。
/// </summary>
public struct ScoreResult
{
    /// <summary>是否勝利。</summary>
    public bool IsWin;
    /// <summary>結算時 HP。</summary>
    public float RemainingHp;
    /// <summary>實際花費秒數。</summary>
    public float ClearTimeSeconds;
    /// <summary>結算時剩餘秒數。</summary>
    public float RemainingSeconds;
    /// <summary>待在煙霧範圍內的總秒數（「吸入二手菸量」）。</summary>
    public float SmokeSeconds;
    /// <summary>血量分 0–100。</summary>
    public float HpScore;
    /// <summary>時間分 0–100。</summary>
    public float TimeScore;
    /// <summary>總分 0–100。</summary>
    public float TotalScore;
    /// <summary>評級："S" "A" "B" "C" "D"。</summary>
    public string Grade;
}
