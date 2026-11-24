using System;
using UnityEngine;

/// <summary>
/// 遊戲目前的狀態。
/// </summary>
public enum GameState { Opening, Playing, Paused, Ended }

/// <summary>
/// 開場選擇的路線。路線 A = AcceptSmoke，路線 B = RefuseSmoke。
/// </summary>
public enum OpeningRoute { None, AcceptSmoke, RefuseSmoke }

/// <summary>
/// 菸怪的體型。v3.1 一般遊戲只有小型菸怪。
/// </summary>
public enum MonsterSize
{
    Small,
    [Obsolete("v3.1 一般遊戲不再有大型菸怪。B10 停用大型菸怪 Prefab 後，A 在 I3 刪除。")]
    Large
}

/// <summary>
/// 遊戲階段（PhaseManager.CurrentPhase）。
/// </summary>
public enum GamePhase { Opening, Tutorial, Phase1, Phase2, Phase3, Ending }

/// <summary>
/// 新手教學步驟（TutorialManager.CurrentStep）。
/// </summary>
public enum TutorialStep { None, SwitchView, Move, ApproachMonster, Attack, Done }

/// <summary>
/// BOSS 狀態（BossController.State）。
/// Inactive 還沒登場／Intro 登場動畫與規則視窗／Sky 天上／Ground 地面／
/// Final 菸全部捏熄後永久在地面／Defeated 被打倒。
/// </summary>
public enum BossState { Inactive, Intro, Sky, Ground, Final, Defeated }

/// <summary>
/// 傷害來源（玩家受傷、BOSS 受傷都用），給特效、音效、UI 判斷。
/// </summary>
public enum DamageSource { Exposure, SmokeRing, SmokeCannon, BossMelee, PlayerMelee, Spray, Dev }

/// <summary>
/// 菸怪群組，決定淨化值（PhaseManager.GetMonsterUnits）。
/// </summary>
public enum MonsterGroup { Tutorial, Phase1, EventGap, StoryEvent }

/// <summary>
/// 可以被個別鎖住的玩家操作（PlayerController.SetActionLocked）。
/// </summary>
public enum PlayerAction { Move, Look, Jump, Run, ViewToggle, Attack, Interact, UseItem }

/// <summary>
/// 衛教道具種類。順序不可改（避免已存的資料跑掉）。
/// v3.1 只用 NicotinePatch（貼片）、NicotineGum（口嚼錠）、NicotineSpray（戒菸噴霧）。
/// </summary>
public enum ItemType
{
    NicotineGum,
    NicotinePatch,
    [Obsolete("v3.1 已刪除尼古丁吸入劑。C7 刪除舊道具資料後，A 在 I3 刪除。")]
    NicotineInhaler,
    NicotineSpray,
    [Obsolete("v3.1 已刪除戒必適。C7 刪除舊道具資料後，A 在 I3 刪除。")]
    Champix,
    [Obsolete("v3.1 已刪除 Bupropion。C7 刪除舊道具資料後，A 在 I3 刪除。")]
    Bupropion
}

/// <summary>
/// 使用道具的結果（PlayerEffects.TryUseItem 回傳）。只有 Consumed 時背包要減 1。
/// </summary>
public enum ItemUseResult
{
    /// <summary>用掉了，背包要減 1（貼片、口嚼錠）。</summary>
    Consumed,
    /// <summary>有效果但不消耗（噴霧）。</summary>
    UsedNotConsumed,
    /// <summary>第三階段前不能用；已自動顯示「第三階段才能使用」，不消耗。</summary>
    NotYet,
    /// <summary>噴霧冷卻中，不消耗。</summary>
    OnCooldown,
    /// <summary>操作被鎖（新手教學、對話中），不消耗。</summary>
    Locked,
    /// <summary>其他原因失敗，不消耗。</summary>
    Failed
}

/// <summary>
/// 一句對話（B 的故事資料、C 的 DialogueUI 共用）。
/// </summary>
[Serializable]
public struct DialogueLine
{
    /// <summary>說話的人，例如「小男孩」；空字串 = 旁白。</summary>
    public string Speaker;
    /// <summary>台詞內容。</summary>
    [TextArea] public string Text;
}

/// <summary>
/// 結算結果，由 ScoreCalculator 產生，ResultUI（C）讀取。
/// </summary>
public struct ScoreResult
{
    /// <summary>是否勝利。</summary>
    public bool IsWin;
    /// <summary>結算時 HP。</summary>
    public float RemainingHp;
    /// <summary>用時（秒）。v3.1 起代表「用時」，名稱不變。</summary>
    public float ClearTimeSeconds;
    /// <summary>【即將移除】倒數已取消，固定為 0。</summary>
    [Obsolete("v3.1 取消倒數，固定為 0。A11 刪除。")]
    public float RemainingSeconds;
    /// <summary>待在煙霧範圍內的總秒數（「吸入二手菸量」）。</summary>
    public float SmokeSeconds;
    /// <summary>血量分 0–100。</summary>
    public float HpScore;
    /// <summary>用時分 0–100。</summary>
    public float TimeScore;
    /// <summary>總分 0–100。</summary>
    public float TotalScore;
    /// <summary>評級："S" "A" "B" "C" "D"。</summary>
    public string Grade;
    /// <summary>頭銜（A18 才填，之前是空字串）。</summary>
    public string Title;
    /// <summary>是否可以「從第三階段重新挑戰」（第三階段失敗時為 true）。</summary>
    public bool CanRetryFromPhase3;
    /// <summary>結束時所在的階段。</summary>
    public GamePhase EndPhase;
}
