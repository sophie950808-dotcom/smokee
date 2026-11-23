# INTERFACES.md — 共用介面規格（v3.1／第二次檢核）

> 更新日期：2026-11-23（A10）。依據 `docs/GDD_v3.1.md` 與 `docs/DECISIONS.md`。
> 三人（以及三種 AI 工具）都必須照這份的名稱與參數寫程式。
> **要改這份文件，先在群組講，由 A 更新並合併進 develop。**
> C 使用 ChatGPT 時，每次請把這份文件（或相關段落）上傳／貼給 ChatGPT。
>
> 所有類別都**不使用 namespace**。事件一律是 C# `event Action<...>`。
> 「擁有者」= 唯一可以修改這支檔案的人。
> 標 🆕 = 這次新增；標 ⚠️Obsolete = 已標記即將移除（還能用，但會出現黃色警告，見第 20 節）。
> 「A 已建空殼」= A10 已經建立檔案、名稱都對，但內容由後面的任務（A11–A17）才完成。
> A10 已建空殼的檔案：`PhaseManager`、`TutorialManager`、`TaskHint`、`ItemRewards`、`TreasureChest`、`BossController`、`BossHealth`、`BossAttack`、`SmokerNPC`、`PlayerEffects`。
> 空殼的方法現在只會印出 Log；**B、C 可以先照名稱寫程式、可以編譯**，實際效果等 A 的對應任務完成。
> B、C 負責的新類別（StoryEventDirector、DialogueUI 等，第 17 節）A 不建空殼，由各自的任務建立。

---

## 0. 共用型別 `Scripts/Core/GameTypes.cs`（擁有者：A）

```csharp
public enum GameState    { Opening, Playing, Paused, Ended }      // 不變
public enum OpeningRoute { None, AcceptSmoke, RefuseSmoke }        // 不變
public enum MonsterSize  { Small, [Obsolete] Large }               // 一般遊戲不再有大型菸怪

// 🆕 遊戲階段（PhaseManager.CurrentPhase）
public enum GamePhase    { Opening, Tutorial, Phase1, Phase2, Phase3, Ending }

// 🆕 新手教學步驟（TutorialManager.CurrentStep）
public enum TutorialStep { None, SwitchView, Move, ApproachMonster, Attack, Done }

// 🆕 BOSS 狀態（BossController.State）
//   Inactive 還沒登場／Intro 登場動畫＋規則視窗／Sky 天上／Ground 地面
//   Final 菸全部捏熄後永久在地面／Defeated 被打倒（結局動畫）
public enum BossState    { Inactive, Intro, Sky, Ground, Final, Defeated }

// 🆕 傷害來源（玩家受傷、BOSS 受傷都用；給特效、音效、UI 判斷用）
public enum DamageSource { Exposure, SmokeRing, SmokeCannon, BossMelee, PlayerMelee, Spray, Dev }

// 🆕 菸怪群組（決定淨化值；B 的生成器設定給菸怪）
public enum MonsterGroup { Tutorial, Phase1, EventGap, StoryEvent }

// 🆕 可以被個別鎖住的玩家操作（新手教學用）
public enum PlayerAction { Move, Look, Jump, Run, ViewToggle, Attack, Interact, UseItem }

// 道具：順序不變（避免已存的資料跑掉），舊道具標記 Obsolete，C7 刪除後由 A 移除
public enum ItemType { NicotineGum, NicotinePatch, [Obsolete] NicotineInhaler,
                       NicotineSpray, [Obsolete] Champix, [Obsolete] Bupropion }
//   NicotinePatch 尼古丁貼片｜NicotineGum 尼古丁口嚼錠｜NicotineSpray 尼古丁戒菸噴霧

// 🆕 使用道具的結果（PlayerEffects.TryUseItem 回傳）
public enum ItemUseResult {
    Consumed,      // 用掉了 → 背包要減 1（貼片、口嚼錠）
    UsedNotConsumed, // 有效果但不消耗（噴霧）
    NotYet,        // 第三階段前 → 已自動顯示「第三階段才能使用」，不消耗
    OnCooldown,    // 噴霧冷卻中，不消耗
    Locked,        // 操作被鎖（新手教學、對話中），不消耗
    Failed         // 其他原因（例如沒有 PlayerEffects），不消耗
}

// 🆕 一句對話（B 的故事資料、C 的 DialogueUI 共用）
[Serializable] public struct DialogueLine {
    public string Speaker;   // 說話的人，例如「小男孩」；空字串 = 旁白
    [TextArea] public string Text;
}

/// 結算結果，由 ScoreCalculator 產生，ResultUI（C）讀取
public struct ScoreResult {
    public bool   IsWin;
    public float  RemainingHp;        // 結算時 HP
    public float  ClearTimeSeconds;   // 用時（秒）。意思從「花費秒數」變成「用時」，名稱不變
    [Obsolete] public float RemainingSeconds;   // 倒數已取消，固定 0
    public float  SmokeSeconds;       // 待在煙霧裡的秒數（保留，C 可以決定要不要顯示）
    public float  HpScore;            // 0–100
    public float  TimeScore;          // 0–100（用時分）
    public float  TotalScore;         // 0–100
    public string Grade;              // "S" "A" "B" "C" "D"
    public string Title;              // 🆕 頭銜（A18 才填，之前是空字串）
    public bool   CanRetryFromPhase3; // 🆕 失敗而且是在第三階段失敗 → ResultUI 顯示「從第三階段重新挑戰」
    public GamePhase EndPhase;        // 🆕 結束時在哪個階段
}
```

---

## 1. 介面 `IHittable` `Scripts/Combat/IHittable.cs`（擁有者：A）

```csharp
public interface IHittable {
    /// 被近戰命中。hitCount 固定是 1（吸入劑已刪除）。
    void TakeHit(int hitCount);
}
```
- **B 的 SmokeMonster、A 的 BossHealth 實作這個介面。** PlayerAttack 只認這個介面。
- 實作的物件（或子物件）一定要有 Collider（可以是 Trigger）。

### `PlayerAttack` `Scripts/Combat/PlayerAttack.cs`（擁有者：A，掛在 Player 上）
| 成員 | 說明 |
|---|---|
| `IHittable CurrentTarget { get; }` | 不變 |
| `event Action<IHittable> OnTargetChanged` | 不變 |
| `event Action OnAttack` | 不變（B 接揮擊音效、動畫） |
| `event Action<IHittable, Vector3> OnHit` | 不變（B 接命中特效） |
| ⚠️Obsolete `int HitCount` | 吸入劑已刪除，固定 1 |

- 判定範圍：前方 **90 度扇形**（Inspector `_angle`，已經是 90）。`PlayerController.IsActionAllowed(PlayerAction.Attack)` 為 false 時不能攻擊（新手教學前 3 步）。
- 對 BOSS：打到 BossHealth 時，每下傷害 = `BossHealth` 的 Inspector「近戰傷害」（預設 3）。BOSS 在天上時打不到。

### `TargetMarker`（擁有者：A）— 不變

## 2. 介面 `IInteractable` `Scripts/Interaction/IInteractable.cs`（擁有者：A）

```csharp
public interface IInteractable {
    string    InteractPrompt { get; }   // HUD 顯示的提示，例如「按 E 開啟寶箱」
    bool      CanInteract    { get; }   // 現在能不能互動
    Transform InteractPoint  { get; }   // 用來計算距離
    void      Interact();               // 玩家按 E 時呼叫
}
```
- 介面本身不變。**實作者改成：A 的 TreasureChest、A 的 SmokerNPC、B 的故事 NPC（求救 NPC、吸菸者）。**
- E 鍵統一用在：NPC 對話與接任務、故事互動（例如捏熄爸爸的菸蒂）、寶箱、第三階段捏熄菸蒂（QTE）。**只有 PlayerInteractor 讀 E。不使用 Q。**

### `PlayerInteractor` `Scripts/Interaction/PlayerInteractor.cs`（擁有者：A）
| 成員 | 說明 |
|---|---|
| `IInteractable CurrentTarget { get; }` | 不變 |
| `event Action<IInteractable> OnFocusChanged` | 不變（C 的「按 E」提示用） |
| 🆕 `event Action<IInteractable> OnInteracted` | 玩家按 E 成功互動時（B 可接音效） |
| 🆕 `bool IsInQte { get; }` | 正在 QTE 連打中。這段期間 E 鍵全部交給 QTE，不會找其他目標 |

- `PlayerController.IsActionAllowed(PlayerAction.Interact)` 為 false 時（新手教學、對話中）目標為 null。

---

## 3. `GameManager` `Scripts/Core/GameManager.cs`（擁有者：A）

### 狀態與用時
| 成員 | 說明 |
|---|---|
| `static GameManager Instance` | |
| `GameState State { get; }` | 不變 |
| 🆕 `float ElapsedTime { get; }` | **用時（秒）**。HUD 每幀直接讀，不另設事件 |
| 🆕 `bool IsTimerPaused { get; }` | 目前有沒有任何系統申請「暫停計時」 |
| 🆕 `void SetTimerPaused(string key, bool paused)` | **任何系統都可以申請暫停計時**，用自己的 key（例如 `"Dialogue"`、`"Teleport"`、`"BossIntro"`）。所有 key 都放開才繼續計時，不會互相覆蓋 |
| `void SetPaused(bool paused)` | 不變：Esc 暫停選單（`Time.timeScale = 0`） |
| `event Action<bool> OnPauseChanged` | 不變 |
| 🆕 `event Action<bool> OnTimerPausedChanged` | 「暫停計時」狀態改變時 |
| `event Action OnRoundStarted` | 意思改成：**新手教學開始**（遊戲進入 Playing，HP、用時開始跑） |
| 🆕 `event Action OnRunReset` | **新手教學結束、第一階段開始時**：用時歸零、HP 補滿、暴露值歸零、淨化歸零 |

**「暫停計時」期間不只停計時**（A 決定）：暴露值、HP（不扣也不回）、40 秒事件空檔、BOSS 行動都會一起停。用在：對話、「要幫忙嗎？」視窗、傳送淡入淡出、過場動畫（BOSS 登場、結局）、第三階段規則介紹視窗。

- 新手教學期間 HP 和用時**照算**（給玩家看遊戲開始時的樣子）；教學結束時 `OnRunReset`，全部重新計算。
- 用時從 `OnRunReset` 開始到 BOSS 被打倒為止；結局動畫不算。

### 淨化進度（內部整數，1000 = 100%）
| 成員 | 說明 |
|---|---|
| 🆕 `const int PurificationMax = 1000` | |
| 🆕 `int PurificationUnits { get; }` | 0–1000 整數。**寶箱等判斷一律用這個** |
| `float PurificationProgress { get; }` | 0–100，給 UI 顯示（= Units ÷ 10） |
| 🆕 `void AddPurification(int units)` | 菸怪被擊敗時呼叫（B 的 SmokeMonster）。只在 Playing 有效 |
| ⚠️Obsolete `void AddPurificationProgress(float amount)` | 改用 `AddPurification(int)`；暫時會把 amount × 10 四捨五入後轉呼叫 |
| `event Action<float> OnProgressChanged` | 不變，參數 0–100 |

- 第三階段：GameManager 自己監聽 `BossHealth.OnBossHpChanged`，淨化 = 600 + 400 ×（1 − 目前血量 ÷ 最大血量）（無條件捨去成整數）。BOSS 回血時淨化會往下掉。打倒 BOSS 時設為 1000。
- **淨化到 1000 不再直接勝利**；勝利流程見第 18 節 c。

### 結束、重新開始、重新挑戰
| 成員 | 說明 |
|---|---|
| `ScoreResult LastResult { get; }` | 不變 |
| `event Action<ScoreResult> OnGameEnded` | 不變。勝利（結局動畫播完）或失敗（HP 歸零；**新手教學期間例外**，見第 5 節） |
| 🆕 `void RestartToOpening()` | **C 的暫停選單／結算畫面「重新開始」呼叫** → 重新載入 GameScene，回到開場選擇 |
| 🆕 `void ReturnToMainMenu()` | **C 的暫停選單「返回主選單」呼叫** → 載入 MainMenu |
| 🆕 `bool CanRetryFromPhase3 { get; }` | 結算時是否可以從第三階段重新挑戰 |
| 🆕 `void RetryFromPhase3()` | **C 的結算畫面按鈕呼叫**：HP 恢復成第一次進入第三階段時的數值、用時繼續累計、重播 BOSS 登場 |
| 🆕 `event Action OnPhase3Retry` | 重新挑戰開始時。**收到要自己重設**：BOSS、吸菸 NPC、菸圈（A）；BOSS 特效（B）；背包恢復快照、結算畫面隱藏、BOSS 血條（C） |
| 🆕 `void FinishWithVictory()` | 【只給 PhaseManager 用】結局動畫結束 → 勝利結算 |
| 🆕 `void BeginRun()` | 【只給 PhaseManager 用】新手教學開始 → 進入 Playing（取代舊的 `StartRound`） |
| 🆕 `void ResetRunForPhase1()` | 【只給 PhaseManager 用】教學結束 → 用時、HP、暴露值、淨化重設 ⇒ `OnRunReset` |

- Esc 仍由 GameManager 讀。**B、C 不要自己 `SceneManager.LoadScene`**，一律呼叫上面兩個方法。
- 開發者用：`DevMode`、`DevForceEnd` 不變；⚠️Obsolete `DevSetRemainingTime`；⚠️Obsolete `RoundDuration`、`RemainingTime`、`OnFinalMinute`、`StartRound()`（改由 PhaseManager 開始教學）。

---

## 4. 🆕 `PhaseManager` `Scripts/Core/PhaseManager.cs`（擁有者：A，A 已建空殼，A11 完成）

整局流程的導演：教學 → 第一階段 → 三個事件 → 寶箱 → BOSS → 結局。和 GameManager 放在同一個物件上。

### 狀態
| 成員 | 說明 |
|---|---|
| `static PhaseManager Instance` | |
| `GamePhase CurrentPhase { get; }` | |
| `event Action<GamePhase> OnPhaseChanged` | HUD、小地圖、B 的菸怪生成、BOSS 都可以監聽 |
| `int CurrentEventIndex { get; }` | 目前（或下一個）故事事件 0、1、2；還沒到第二階段時 -1 |
| `bool IsInEventGap { get; }` / `float EventGapRemaining { get; }` | 是否在 40 秒空檔、剩幾秒 |
| `bool IsHelpRefused { get; }` | 玩家目前是否拒絕幫忙中 |
| `float RefusalSmokeMultiplier { get; }` | 拒絕期間菸怪煙霧倍率（1 → 最多 2） |
| `bool HasTreasureMap { get; }` | 拿到藏寶圖了沒 |
| `int GetMonsterUnits(MonsterGroup group, int indexInGroup)` | **B 的生成器呼叫**：這隻菸怪要給多少淨化值（見第 21 節） |

### 給 B 的事件（A → B）
| 事件 | B 要做什麼 |
|---|---|
| `event Action<int> OnStoryEventRequested` | 參數：事件編號 0/1/2。**StoryEventDirector 讓那位 NPC 跑來求救** |
| `event Action<int> OnEventGapStarted` | 參數：空檔編號 0/1（事件一後、事件二後）。**B 在街上生成 2 隻空檔菸怪** |
| `event Action<int> OnEventGapEnded` | 40 秒到了（緊接著會發 `OnStoryEventRequested(下一個)`） |
| `event Action<bool> OnHelpRefusedChanged` | true = 開始拒絕（NPC 開始跟著玩家、菸怪外觀改「打不掉」）；false = 接下任務，恢復 |
| `event Action OnTreasureMapObtained` | 小地圖顯示寶箱（C）、寶箱發光（B 可選） |
| `event Action OnBossIntroRequested` | **B 播 BOSS 登場動畫**（大煙怪衝上天、吸菸者散開）；重新挑戰時也會再發一次 |
| `event Action OnRulesIntroRequested` | **C 顯示第三階段規則介紹視窗** |
| `event Action OnBossFightStarted` | 規則視窗關掉，BOSS 戰正式開始 |
| `event Action OnEndingRequested` | BOSS 被打倒 → **B 播結局動畫**（遺言對話、綠色無煙城市） |

### B、C 呼叫的方法（B/C → A）
| 方法 | 誰呼叫 | 時機 |
|---|---|---|
| `void ReportEventAccepted(int index)` | B 的 Director | 事件一對話結束（強制）；事件二、三按「幫忙」 |
| `void ReportHelpRefused(int index)` | B 的 Director | 事件二、三按「拒絕」（可以重複拒絕） |
| `void ReportEventCompleted(int index)` | B 的 Director | 給完道具、傳送回街道之後 |
| `void NotifyBossIntroFinished()` | B 的 BOSS 登場演出 | 登場動畫播完 |
| `void NotifyRulesIntroClosed()` | C 的 RulesIntroWindow | 玩家點擊關掉視窗 |
| `void NotifyEndingFinished()` | B 的結局演出 | 結局動畫播完 → 勝利結算 |
| `void EnterPhase3()` | 【只給 A 的 TreasureChest】 | 寶箱打開 |
| `void DevJumpToPhase3()` | 【只給 A 的 DevTools】 | 開發者快捷鍵（F4 任務） |

### 規則
- 第一階段：淨化達 200（20%）→ 任務提示「城市中還有很多需要幫助的人，快去看看吧！」→ 進入 Phase2 → `OnStoryEventRequested(0)`。
- 40 秒空檔在「暫停計時」期間也會停。
- 拒絕：每 10 秒煙霧倍率 +0.1（線性，100 秒到 2 倍），只影響菸怪；整張地圖菸怪無法消除（透過 SmokeSource，見第 11 節）。接下任務後倍率回 1。
- 事件三完成 → `HasTreasureMap = true`、`OnTreasureMapObtained`、任務提示「到吸菸室旁的寶箱看看」。

## 5. 🆕 `TutorialManager` `Scripts/Core/TutorialManager.cs`（擁有者：A，A 已建空殼，A12 完成）

| 成員 | 說明 |
|---|---|
| `static TutorialManager Instance` | |
| `TutorialStep CurrentStep { get; }` | |
| `bool IsActive { get; }` | |
| `event Action<TutorialStep> OnStepChanged` | **B 依步驟切換發光提示**（Move → 車庫外目標發光；ApproachMonster → 菸怪附近範圍發光；Attack → 菸怪發光） |
| `event Action OnTutorialFinished` | |
| `Transform MoveTarget { get; }` / `SmokeMonster TutorialMonster { get; }` | Inspector 指定（B 在 GameScene 拖進來）；B 的發光效果可以讀 |
| `void BeginTutorial()` | 【只給 PhaseManager】 |

| 步驟 | 任務提示 | 這一步解鎖 | 完成條件 |
|---|---|---|---|
| SwitchView | 按 V 鍵切換視角 | V（滑鼠視角、跳躍、奔跑全程可用） | 切換一次視角 |
| Move | 試著按 WASD 走走看看吧！ | WASD | 走到 `MoveTarget` 半徑 1.5 公尺內【暫定】 |
| ApproachMonster | 試著走到小煙怪旁邊！ | — | 走到教學菸怪半徑 3 公尺內【暫定】 |
| Attack | 按下左鍵消滅菸怪！ | 左鍵 | 教學菸怪被擊敗 |
| Done | 消滅所有的煙怪吧！ | E、道具 | → `PhaseManager` 進入 Phase1、`GameManager.OnRunReset` |

- 鎖定方式：`PlayerController.SetActionLocked(action, "Tutorial", true)`。教學沒解鎖的 E、道具、攻擊，B、C 不用自己判斷。
- **教學期間 HP 歸零不會進結算**：TutorialManager 讓 HP 補滿、暴露值歸零，玩家留在原地，**從當下那一步**繼續，並發出 `event Action OnTutorialRestarted`（B 可以重新播發光提示）。教學以外的階段 HP 歸零才進失敗結算。

## 6. 🆕 `TaskHint` 目前任務提示 `Scripts/Core/TaskHint.cs`（擁有者：A，static class）

HUD（C）顯示的「任務提示」唯一來源。A、B 的系統都用這裡更新，**C 只負責顯示**。

```csharp
public static class TaskHint {
    // 持續顯示的「目前任務」（疊起來：最後設定的顯示在上面，清掉後露出前一個）
    public static string CurrentTask { get; }
    public static event Action<string> OnTaskChanged;        // 參數：目前要顯示的文字；空字串 = 不顯示
    public static void SetTask(string source, string text);  // source：誰設的，例如 "Tutorial"、"Phase"、"StoryEvent"、"Boss"
    public static void ClearTask(string source);

    // 短暫提示（幾秒後自動消失），例如「第三階段才能使用」
    public static event Action<string, float> OnToast;       // 參數：文字、顯示秒數
    public static void ShowToast(string text, float seconds = 2.5f);
}
```
- 重新載入場景時自動清空。
- 誰會設哪些文字見第 19 節。

---

## 7. `RouteManager` `Scripts/Core/RouteManager.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `OpeningRoute ChosenRoute` / `ChooseRoute(route)` / `NotifyOpeningFinished()` / `OnRouteChosen` / `OnOpeningFinished` | 不變 |
| ⚠️Obsolete `void NotifyPlayerLeftAlley()` | 新流程不再用「走出死巷」開始；改成教學開始 |

流程（改變處）：
- 路線 A：不變。B7 改成隨機一位路人演出，結束呼叫 `NotifyOpeningFinished()` → RouteManager 重新載入場景。
- 路線 B：**B 在車庫門開好時就呼叫 `NotifyOpeningFinished()`**（不再等玩家走到車庫出口）→ PhaseManager 開始新手教學。
- ⚠️Obsolete `AlleyExitTrigger`：沒有作用了，B 可以在 GameScene 移除元件（B7）。

## 8. `PlayerController` `Scripts/Player/PlayerController.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `bool IsFirstPerson` / `OnViewModeChanged` | 不變 |
| `bool IsControlEnabled { get; }` | 沒有任何鎖、而且不在暫停中 |
| 🆕 `void SetControlLock(string key, bool locked)` | **用自己的 key 鎖住全部操作**並解鎖游標，例如 `"Dialogue"`、`"HelpPrompt"`、`"RulesIntro"`、`"Cutscene"`。所有 key 都放開才恢復，不會互相覆蓋 |
| `void SetControlEnabled(bool enabled)` | 保留（B、C 現有程式在用），等於 `SetControlLock("Default", !enabled)`。**新程式請用 key 版** |
| 🆕 `void SetActionLocked(PlayerAction action, string key, bool locked)` | 只鎖一種操作（新手教學、QTE 期間鎖移動） |
| 🆕 `bool IsActionAllowed(PlayerAction action)` | C 的背包切換／使用道具前先檢查 `UseItem` |
| 🆕 `void TeleportTo(Vector3 position, Quaternion rotation)` | **B 的 StoryTeleporter 用**：瞬間移動玩家（CharacterController 不能直接改 position） |
| 🆕 `void SetCameraOverride(Transform cameraPoint)` / `void ClearCameraOverride()` | **B 的過場動畫用**：Main Camera 改跟著指定的點，不用再把 PlayerController 停用 |
| `SetSpeedModifier` / `RemoveSpeedModifier` | 不變 |

## 9. `PlayerHealth` `Scripts/Player/PlayerHealth.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `CurrentHp` / `MaxHp` / `OnHpChanged` / `OnDied` | 不變 |
| `void TakeDamage(float amount)` | 不變（來源視為 `Exposure`） |
| 🆕 `void TakeDamage(float amount, DamageSource source)` | BOSS 攻擊用 |
| 🆕 `event Action<float, DamageSource> OnDamaged` | 參數：扣多少、來源 → B 受擊特效、C 受傷紅框 |
| `void Heal(float amount)` | 不變（尼古丁貼片 +30 用；EscortNPC 已刪除） |
| 🆕 `void ResetHp(float hp)` | 【只給 GameManager、TutorialManager】教學結束／教學中歸零補滿、重新挑戰恢復快照（也會清掉「已死亡」狀態） |

- 「暫停計時」期間不扣也不回。

## 10. `ExposureSystem` `Scripts/Player/ExposureSystem.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `Exposure` / `VisualExposure` / `IsWarning` / `IsMaxed` / `SmokeSeconds` / 三個事件 | 不變（`VisualExposure` 現在永遠等於 `Exposure`） |
| 🆕 `void ResetExposure()` | 【只給 GameManager】教學結束、重新挑戰 |
| ⚠️Obsolete `RiseMultiplier`、`IsVisualSuppressed` | 舊道具效果，不再使用 |

- 「暫停計時」期間暴露值不變。

## 11. `SmokeSource` `Scripts/Monsters/SmokeSource.cs`（擁有者：A，A15 完成）

| 成員 | 說明 |
|---|---|
| `static IReadOnlyList<SmokeSource> All` | 不變 |
| `float Radius { get; set; }` | **讀取 = 實際半徑**（基本半徑 × 倍率 × 拒絕倍率）；設定 = 設基本半徑（相容舊程式） |
| 🆕 `float BaseRadius { get; set; }` | Inspector 的基本半徑。預設改成 4（BOSS 7） |
| 🆕 `float RadiusMultiplier { get; set; }` | 這個來源自己的倍率，預設 1 |
| 🆕 `bool AffectedByRefusal { get; set; }` | **菸怪要設成 true**（B 的 SmokeMonster 在 Awake 設定）；開場朋友、吸菸 NPC、BOSS 是 false |
| 🆕 `bool IsIndestructible { get; }` | 拒絕幫忙中而且 `AffectedByRefusal` → true。**B 的 SmokeMonster.TakeHit 看到 true 就不算這一下** |
| 🆕 `static float RefusalMultiplier { get; }` / `static bool RefusalActive { get; }` | |
| 🆕 `static event Action<bool> OnRefusalStateChanged` | B 換「打不掉」的外觀用 |
| 🆕 `static void SetRefusalState(bool active, float multiplier)` | 【只給 PhaseManager】 |
| `bool IsEmitting { get; set; }` | 不變 |

- 掛在：開場朋友、每一隻菸怪、吸菸 NPC、BOSS、菸炮落地的煙霧區。

## 12. `PlayerEffects` `Scripts/Player/PlayerEffects.cs`（擁有者：A，A13 完成）

| 成員 | 說明 |
|---|---|
| 🆕 `ItemUseResult TryUseItem(ItemType type)` | **C 的背包按右鍵時呼叫**。回傳 `Consumed` 時背包減 1；其他結果不要扣 |
| 🆕 `float SprayCooldown { get; }` / `float SprayCooldownRemaining { get; }` | C 的背包顯示噴霧冷卻 |
| 🆕 `event Action<ItemType> OnItemUsed` | 道具成功使用（B 特效、音效） |
| 🆕 `event Action<Vector3, Vector3, bool> OnSprayFired` | 參數：起點、終點、有沒有打到 BOSS → **B 播噴霧特效** |
| ⚠️Obsolete `void ApplyItem(ItemType type)` | 改用 `TryUseItem` |
| ⚠️Obsolete `OnEffectStarted` / `OnEffectEnded` | 新道具沒有持續時間 |

| 道具 | 效果 | 限制 |
|---|---|---|
| 尼古丁貼片 | HP +30（不超過上限） | 只能在第三階段；一次 |
| 尼古丁口嚼錠 | 清除玩家半徑 15 公尺內所有 BOSS 菸圈與菸炮煙霧區 | 只能在第三階段；一次 |
| 尼古丁戒菸噴霧 | 朝**鏡頭中央**方向瞬間判定，只對 BOSS 造成 7.5 傷害；對小菸怪無效；最遠 30 公尺【暫定】 | 無次數限制；冷卻 1.5 秒 |

- 第三階段 = `PhaseManager.CurrentPhase == Phase3`。提早使用 → PlayerEffects 自己呼叫 `TaskHint.ShowToast("第三階段才能使用")`，回傳 `NotYet`，C 不用另外顯示。

### 🆕 `ItemRewards` `Scripts/Core/ItemRewards.cs`（擁有者：A，static class）
給道具的統一入口，避免 A、B 的程式直接依賴 C 的背包。
```csharp
public static class ItemRewards {
    public static event Action<ItemType> OnItemGranted;  // C 的 Inventory 監聽，收到就加進背包
    public static void Grant(ItemType type);             // B 的故事事件（貼片、口嚼錠）、A 的寶箱（噴霧）呼叫
}
```

## 13. 🆕 `TreasureChest` `Scripts/Interaction/TreasureChest.cs`（擁有者：A，A17 完成）

實作 `IInteractable`。放在吸菸室旁（A 做 Prefab，B 放進 GameScene）。**不可以沿用 C 的 ChestController**。

| 成員 | 說明 |
|---|---|
| `static TreasureChest Instance` | |
| `bool IsRevealed { get; }` | 拿到藏寶圖了沒（= `PhaseManager.HasTreasureMap`）→ **C 的小地圖顯示寶箱** |
| `bool IsOpened { get; }` | |
| `event Action OnChestRevealed` | 寶箱出現（B 可以播出現特效） |
| `event Action OnChestOpened` | B 開箱動畫、音效 |
| `event Action OnChestLocked` | 淨化不足、按 E 被擋時 |
| `CanInteract` | 拿到藏寶圖、而且還沒開過 |

- **拿到藏寶圖之前寶箱不存在**：Inspector 的 `_visualRoot`（寶箱模型與碰撞體，放在子物件）一開始隱藏，看不到、碰不到、也不能按 E；`OnTreasureMapObtained` 時才顯示。腳本本身掛在根物件、一直啟用（這樣 `Instance` 一直找得到）。
| `Interact()` | 淨化 < 600 → `TaskHint.ShowToast("路上好像還有還沒消除的煙怪")`；≥ 600 → 打開、`ItemRewards.Grant(NicotineSpray)`、`PhaseManager.EnterPhase3()` |

---

## 14. 🆕 BOSS `Scripts/Boss/`（擁有者：A，A14、A16 完成；外觀、特效由 B 依事件播放）

BOSS Prefab 結構：根物件掛 `BossController`、`BossHealth`、`BossAttack`、`SmokeSource`（半徑 7）＋碰撞體。**B 的模型與特效放在子物件**，不要在子物件放 Collider 以外的邏輯。
**BOSS 的移動（天上高度、降落）與縮放由 A 的 BossController 控制根物件**；B 只做模型、動畫、粒子。

### `BossController`
| 成員 | 說明 |
|---|---|
| `static BossController Instance` | |
| `BossState State { get; }` | |
| `event Action<BossState> OnStateChanged` | **B 播起飛、降落、最終局面、倒下的動畫** |
| `int SizeLevel { get; }` | 0–3，血量每降 25% 加一級（飛得更低、體型更小） |
| `event Action<int> OnSizeLevelChanged` | B 換縮小特效 |
| `float StateTimeRemaining { get; }` | 這個狀態還剩幾秒切換（B 可做預告特效） |
| `void BeginIntro()` / `void StartFight()` | 【只給 PhaseManager】 |

- 開打時先在**天上**；地面 8 秒、天上 12 秒輪流。
- 最後一位吸菸 NPC 被捏熄：BOSS 立刻降到地面 → `Final`（永久在地面、不回血、不吐菸圈菸炮、改物理攻擊；自己的 7 公尺煙霧保留）。
- 收到 `GameManager.OnPhase3Retry` 自己重設成 `Inactive`、滿血。

### `BossHealth`（實作 `IHittable`）
| 成員 | 說明 |
|---|---|
| `static BossHealth Instance` | |
| `float CurrentHp { get; }` / `float MaxHp { get; }` | 預設 150 |
| `bool IsDefeated { get; }` | |
| `event Action<float, float> OnBossHpChanged` | 參數：目前、最大 → **C 的 BOSS 血條**、GameManager 淨化換算 |
| `event Action<float, DamageSource> OnBossDamaged` | 參數：傷害、來源 → B 受擊特效 |
| `event Action OnBossDefeated` | |
| `void TakeDamage(float amount, DamageSource source)` | 噴霧、開發者工具 |
| `TakeHit(int)` | 近戰：每下 3；BOSS 在天上時無效 |

- 回血：地面 = 抽菸人數 × 1／秒；天上 = 1 + 抽菸人數 × 1／秒；Final 不回血。

### `BossAttack`
| 成員 | 說明 |
|---|---|
| `event Action<Vector3, Vector3> OnSmokeRingFired` | 參數：起點、方向（地面時每 2 秒） |
| `event Action<Vector3, float> OnCannonFired` | 參數：落點、幾秒後落地 → **B 在落點顯示預警** |
| `event Action<Vector3> OnCannonLanded` | 落地（之後留下 3 秒煙霧區） |
| `event Action OnMeleeWindup` / `event Action<bool> OnMeleeSwing` | 物理攻擊：預備動作開始、揮下（參數：有沒有打中） |
| `event Action<Vector3, float> OnSmokeCleared` | 口嚼錠清除：中心、半徑 → B 特效 |
| `int ClearSmokeInRadius(Vector3 center, float radius)` | 【只給 PlayerEffects】清掉範圍內的菸圈與煙霧區，回傳清掉幾個 |

- 菸圈、菸炮、煙霧區是 A 的小腳本（`BossSmokeRing`、`BossCannonShell`、`BossSmokeZone`，同資料夾）＋ Prefab。**B 可以換 Prefab 裡的外觀**（不要改腳本、不要刪碰撞體）。

## 15. 🆕 `SmokerNPC` `Scripts/Boss/SmokerNPC.cs`（擁有者：A，A16 完成）

第三階段街上的 4 位吸菸者（爸爸、北友會兩位朋友、阿公）。實作 `IInteractable`，身上有 `SmokeSource`（半徑 4）。**和第二階段故事裡的 NPC 是不同物件**。

| 成員 | 說明 |
|---|---|
| `static IReadOnlyList<SmokerNPC> All` | **C 的小地圖**顯示吸菸 NPC |
| `static int SmokingCount { get; }` | 還在抽菸的人數（BOSS 回血用） |
| `static event Action<int> OnSmokingCountChanged` | |
| `bool IsSmoking { get; }` | 小地圖圖示：false = 熄滅 |
| `event Action<SmokerNPC> OnSnuffed` / `static event Action<SmokerNPC> OnAnySnuffed` | 被捏熄 → B 熄菸特效、C 小地圖 |
| `string DisplayName { get; }` | 例如「阿公」 |
| `CanInteract` | 正在抽菸、BOSS 戰中、玩家面向它（前方 90 度）且 2 公尺內 |
| `Interact()` | 開始 QTE |
| `void BeginSmoking()` | 【只給 A】BOSS 戰開始、重新挑戰時開始抽菸 |
| `void RegisterQtePress()` | 【只給 PlayerInteractor】QTE 期間每按一次 E |

### QTE（連打 E）
| 成員 | 說明 |
|---|---|
| `static SmokerNPC ActiveQte { get; }` | 正在 QTE 的那位；沒有時 null |
| `int QtePresses { get; }` / `int QteRequired { get; }` | 已按幾下／要幾下（5） |
| `float QteTimeRemaining { get; }` / `float QteDuration { get; }` | 剩幾秒／總共幾秒（2） |
| `static event Action<SmokerNPC> OnQteStarted` | **C 的 QteBarUI 顯示** |
| `static event Action<SmokerNPC, bool> OnQteEnded` | 參數：是否成功 → QteBarUI 隱藏 |

- 第一下 E 開始 QTE 並算 1 下；QTE 期間不能移動、可以轉視角；被 BOSS 打中不中斷；超過時間失敗、進度歸零、可以馬上重試。
- E 鍵由 PlayerInteractor 讀，再交給 `ActiveQte`。**C 只負責顯示，不讀 E。**

## 16. `ScoreCalculator` `Scripts/Core/ScoreCalculator.cs`（擁有者：A，A11 用時分、A18 頭銜）

```csharp
public static class ScoreCalculator {
    // 🆕
    public static ScoreResult Calculate(bool isWin, float hp, float maxHp,
                                        float elapsedSeconds, float smokeSeconds);
    // ⚠️Obsolete 舊版（倒數用）
    public static ScoreResult Calculate(bool isWin, float hp, float maxHp,
                                        float remainingSeconds, float roundDuration, float smokeSeconds);
}
```
- 總分 = 血量分 × 0.6 + 用時分 × 0.4；用時分：≤ 8 分鐘 100，之後每多 30 秒 −10，最低 0；失敗局 0。

---

## 17. B、C 擁有、但 A 需要知道的介面

### `SmokeMonster`（擁有者：B，B10、B12）
| 成員 | 說明 |
|---|---|
| 實作 `IHittable` | 不變 |
| `static IReadOnlyList<SmokeMonster> All`、`bool IsDefeated` | 不變（C 的小地圖） |
| 🆕 `int PurificationUnits { get; set; }` | 淨化值（整數，1000 = 100%）。**教學菸怪 0**；由生成器用 `PhaseManager.GetMonsterUnits(...)` 設定 |
| 🆕 `MonsterGroup Group { get; set; }` | |
| 🆕 `event Action<SmokeMonster> OnDefeated` / `static event Action<SmokeMonster> OnAnyDefeated` | 被擊敗 |
| ⚠️ `float ProgressValue` | 改用 `PurificationUnits`（B 的檔案，A 不會動，B10 自己移除） |
- 被擊敗：`GameManager.Instance.AddPurification(PurificationUnits)`，並停止 SmokeSource。
- `TakeHit` 時若 `SmokeSource.IsIndestructible` 為 true → 播「打不掉」回饋，不算次數。
- Awake 時設 `SmokeSource.AffectedByRefusal = true`。
- 行為：出生點半徑 3 公尺內遊走、每秒 1 公尺，不追擊、不攻擊。

### `SmokeMonsterSpawner`（擁有者：B，B10）
- 監聽 `PhaseManager.OnPhaseChanged`（Phase1 → 生成 5 隻）、`OnEventGapStarted`（每次 2 隻）；故事地點的 3 隻由 Director 生成。
- 生成時設定 `Group` 與 `PurificationUnits = PhaseManager.Instance.GetMonsterUnits(group, index)`。

### `OpeningSequence`（擁有者：B，B7）
- 接受路線：隨機一位路人（老奶奶／小朋友／爸爸）→ 咳嗽 → 台詞（`DialogueUI`）→ 動手 → 暈倒 → `NotifyOpeningFinished()`。
- 拒絕路線：朋友點菸 → 車庫門開好 → `NotifyOpeningFinished()`。OnGUI 的「新手引導」文字移除，改由 TutorialManager 設任務提示。

### 🆕 `StoryEventDirector` `Scripts/NPC/StoryEventDirector.cs`（擁有者：B，B9、B11、B12）
三個事件共用一套流程，差異放在 `StoryEventData`。
| 成員 | 說明 |
|---|---|
| `static StoryEventDirector Instance` | |
| `int ActiveEventIndex { get; }` | -1 = 沒有 |
| `event Action<int> OnEventStarted` | 傳送到故事地點時 |
| `event Action<int> OnEventCompleted` | |
| `event Action<int> OnHelpRefused` | |
- **監聽 `PhaseManager.OnStoryEventRequested(n)`** → NPC 跑來 → 求救對話 → （事件二、三）`HelpPromptUI` → 呼叫 `PhaseManager.ReportEventAccepted/ReportHelpRefused`。
- 拒絕後：求救 NPC 跟著玩家，實作 `IInteractable`（「按 E 和○○說話」），按 E 再跳一次「要幫忙嗎？」。
- 接下後：傳送 → 生成 3 隻事件菸怪（`MonsterGroup.StoryEvent`）→ 清完 → 故事對話 → 吸菸者 `IInteractable` 按 E → 結局 → `ItemRewards.Grant(道具)`＋交付對話 → 傳送回街道 → `PhaseManager.ReportEventCompleted(n)`。
- 每一步用 `TaskHint.SetTask("StoryEvent", data 裡的文字)` 更新任務提示；事件結束 `ClearTask("StoryEvent")`。

### 🆕 `StoryEventData` `Scripts/NPC/StoryEventData.cs`（ScriptableObject，擁有者：B；台詞內容由 C 的 DialogueData 提供）
| 欄位 | 說明 |
|---|---|
| `int EventIndex` | 0/1/2 |
| `bool CanRefuse` | 事件一 false |
| `string HelpQuestion` | 「要幫忙嗎？」視窗文字 |
| `DialogueData RequestDialogue, AfterClearDialogue, StoryDialogue, EndingDialogue, RewardDialogue` | C 做的台詞資料 |
| `bool GivesItem` / `ItemType RewardItem` | 事件一貼片、事件二口嚼錠；事件三給藏寶圖（不是道具，由 PhaseManager 處理） |
| `string FollowHint, ClearHint, InteractHint` | 任務提示文字 |

### 🆕 `StoryTeleporter` `Scripts/NPC/StoryTeleporter.cs`（擁有者：B）
- 淡出 → `PlayerController.TeleportTo(...)` → 淡入；期間 `GameManager.SetTimerPaused("Teleport", true)`、`SetControlLock("Teleport", true)`。

### BOSS 外觀、登場與結局（擁有者：B，B13、B15–B17）
- 監聽 `PhaseManager.OnBossIntroRequested` → 播登場（BOSS 衝上天、吸菸者從吸菸室散開）→ `PhaseManager.NotifyBossIntroFinished()`。重新挑戰會再播一次。
- 監聽 `BossController.OnStateChanged`、`OnSizeLevelChanged`、`BossHealth.OnBossDamaged`、`BossAttack` 的事件 → 播動畫、特效。
- 監聽 `PhaseManager.OnEndingRequested` → BOSS 遺言（`DialogueUI`）→ 綠色城市 → `PhaseManager.NotifyEndingFinished()`。
- 過場鏡頭用 `PlayerController.SetCameraOverride`；期間 `SetTimerPaused("Cutscene", true)`、`SetControlLock("Cutscene", true)`。

### 🆕 `DialogueUI` `Scripts/UI/DialogueUI.cs`（擁有者：C，C8）
| 成員 | 說明 |
|---|---|
| `static DialogueUI Instance` | |
| `bool IsShowing { get; }` | |
| `void Show(IReadOnlyList<DialogueLine> lines, Action onFinished)` | B 呼叫；全部講完後呼叫 onFinished |
| `void Show(DialogueData data, Action onFinished)` | 同上，用資料檔 |
| `event Action OnDialogueStarted` / `event Action OnDialogueFinished` | |
- 顯示期間：`SetControlLock("Dialogue", true)`、`SetTimerPaused("Dialogue", true)`，結束放開。
- 翻下一句：**滑鼠左鍵或空白鍵**（C 自己讀；不要讀 E）。暫停中（`Time.timeScale = 0`）不能翻頁。

### 🆕 `DialogueData` `Scripts/UI/DialogueData.cs`（ScriptableObject，擁有者：C，C11）
- `List<DialogueLine> Lines`。開場路人、三個事件、道具交付（含真實用途與「實際用藥應經醫療專業人員評估」）、BOSS 遺言。

### 🆕 `HelpPromptUI`（擁有者：C，C9）
- `static HelpPromptUI Instance`；`void Show(string question, Action onAccept, Action onRefuse)`；`bool IsShowing`。
- 顯示期間 `SetControlLock("HelpPrompt", true)`、`SetTimerPaused("HelpPrompt", true)`。
- 「按 E」提示：監聽 `PlayerInteractor.OnFocusChanged`，顯示 `InteractPrompt`（現在 HUD 已有，C9 可以搬過來或沿用）。

### 🆕 `RulesIntroWindow`（擁有者：C，C12）
- 監聽 `PhaseManager.OnRulesIntroRequested` → 顯示（道具用途、E 鍵捏熄與 QTE、如何攻擊 BOSS；不寫真實用途）→ 點擊 → `PhaseManager.NotifyRulesIntroClosed()`。
- 顯示期間 `SetControlLock("RulesIntro", true)`、`SetTimerPaused("RulesIntro", true)`。

### 🆕 `QteBarUI`（擁有者：C，C12）
- 監聽 `SmokerNPC.OnQteStarted` / `OnQteEnded`；顯示期間每幀讀 `SmokerNPC.ActiveQte` 的 `QtePresses / QteRequired`、`QteTimeRemaining / QteDuration`。

### 🆕 `BossHealthBar`（擁有者：C，C12）
- 監聽 `PhaseManager.OnBossFightStarted` 顯示、`BossHealth.OnBossDefeated` 隱藏；`BossHealth.OnBossHpChanged(目前, 最大)` 更新。
- 收到 `GameManager.OnPhase3Retry` 隱藏（下一次 OnBossFightStarted 再顯示）。

### `HUD`（擁有者：C，C10）
- 用時：每幀讀 `GameManager.Instance.ElapsedTime`（不要再讀 `RemainingTime`）。
- 淨化：`OnProgressChanged`／`PurificationProgress`（不變）。
- 任務提示：`TaskHint.OnTaskChanged`、`TaskHint.CurrentTask`；短暫提示：`TaskHint.OnToast`。

### `Inventory` / `InventoryUI` / `HealthItemData`（擁有者：C，C7）
- `Inventory`：`static Instance`、`int GetCount(ItemType)`、`ItemType? SelectedItem`、`event Action OnInventoryChanged`。
- 監聽 `ItemRewards.OnItemGranted` 加道具。
- 滾輪切換、右鍵使用（C 讀；先檢查 `PlayerController.IsActionAllowed(PlayerAction.UseItem)`）→ `PlayerEffects.TryUseItem(type)` → 回傳 `Consumed` 才減 1。
- 重新挑戰：第一次收到 `PhaseManager.OnPhaseChanged(Phase3)` 時記下背包內容；收到 `GameManager.OnPhase3Retry` 恢復。
- `HealthItemData` 欄位至少：`ItemType Type`、`string DisplayName`、`Sprite Icon`、`string InfoText`。

### `MiniMap`（擁有者：C，C13）
- 菸怪 `SmokeMonster.All`；吸菸室：Inspector 拖場景物件；寶箱 `TreasureChest.Instance`（`IsRevealed` 才顯示，或監聽 `PhaseManager.OnTreasureMapObtained`）；第三階段 `SmokerNPC.All`（`IsSmoking` false 熄滅）。不顯示求救 NPC。

### `ResultUI`、暫停選單（擁有者：C，C14）
- `OnGameEnded`：`ScoreResult.CanRetryFromPhase3` 為 true 才顯示「從第三階段重新挑戰」→ `GameManager.RetryFromPhase3()`；收到 `OnPhase3Retry` 隱藏。
- 「重新開始」→ `GameManager.RestartToOpening()`；「返回主選單」→ `GameManager.ReturnToMainMenu()`；「繼續遊戲」→ `GameManager.SetPaused(false)`。

---

## 18. 三條關鍵流程（誰呼叫誰）

`→ 呼叫方法`　`⇒ 發出事件（誰在聽）`

### a. 新手教學 → 第一階段 → 事件一 → 40 秒 → 事件二（含拒絕再接受）→ 事件三
```
[B] OpeningSequence：拒絕 → 朋友點菸 → 車庫門開好
    → RouteManager.NotifyOpeningFinished()
        ⇒ OnOpeningFinished(RefuseSmoke)                         （PhaseManager 聽）
[A] PhaseManager：Phase = Tutorial ⇒ OnPhaseChanged(Tutorial)
    → GameManager 進入 Playing（HP、用時開始跑）⇒ OnRoundStarted
    → TutorialManager.BeginTutorial()
[A] TutorialManager：4 步驟（鎖操作、TaskHint.SetTask("Tutorial", …)）⇒ OnStepChanged（B 發光）
    教學菸怪被打倒 ⇒ OnTutorialFinished
[A] PhaseManager：Phase = Phase1 ⇒ OnPhaseChanged(Phase1)（B 生成 5 隻）
    → GameManager：用時歸零、HP 補滿、暴露值與淨化歸零 ⇒ OnRunReset
    TaskHint「消滅所有的煙怪吧！」
[B] 菸怪被打倒 → GameManager.AddPurification(40) ×5 ⇒ OnProgressChanged
[A] PhaseManager：淨化 ≥ 200 → Phase = Phase2 ⇒ OnPhaseChanged(Phase2)
    TaskHint「城市中還有很多需要幫助的人，快去看看吧！」
    ⇒ OnStoryEventRequested(0)                                   （B Director 聽）
[B] Director：小男孩跑來 → DialogueUI.Show(求救) → 強制 → PhaseManager.ReportEventAccepted(0)
    → StoryTeleporter 淡出傳送 → 生成 3 隻（33/33/34）→ 清完 → 故事對話
    → 爸爸 IInteractable（按 E 捏熄菸蒂）→ 結局 → ItemRewards.Grant(NicotinePatch) ⇒ OnItemGranted（C 背包）
    → 交付對話（真實用途）→ 傳送回街道 → PhaseManager.ReportEventCompleted(0)
[A] PhaseManager ⇒ OnEventGapStarted(0)（B 生成 2 隻）… 40 秒（暫停計時時也停）
    ⇒ OnEventGapEnded(0) ⇒ OnStoryEventRequested(1)
[B] Director：女孩跑來 → 求救對話 → HelpPromptUI.Show
    └ 拒絕 → PhaseManager.ReportHelpRefused(1)
        [A] ⇒ OnHelpRefusedChanged(true)；SmokeSource.SetRefusalState(true, 1.0)
            ⇒ SmokeSource.OnRefusalStateChanged(true)（B：打不掉的外觀）
            每 10 秒倍率 +0.1（最多 2）；TaskHint「煙霧好像越來越濃了……」
        [B] 女孩跟著玩家；玩家對她按 E → 再跳 HelpPromptUI（可以再拒絕）
    └ 幫忙 → PhaseManager.ReportEventAccepted(1)
        [A] ⇒ OnHelpRefusedChanged(false)；SmokeSource.SetRefusalState(false, 1)
        [B] 傳送 → …（同事件一）→ Grant(NicotineGum) → ReportEventCompleted(1)
[A] ⇒ OnEventGapStarted(1) … 40 秒 ⇒ OnStoryEventRequested(2)
[B] 老奶奶事件（流程同事件二）→ ReportEventCompleted(2)
```

### b. 藏寶圖 → 寶箱 → BOSS 登場 → 規則視窗 → BOSS 戰
```
[A] PhaseManager：ReportEventCompleted(2) → HasTreasureMap = true
    ⇒ OnTreasureMapObtained（C 小地圖顯示寶箱）；TaskHint「到吸菸室旁的寶箱看看」
    → TreasureChest 顯示模型與碰撞體 ⇒ OnChestRevealed（B 出現特效）
[A] 玩家對 TreasureChest 按 E
    └ 淨化 < 600 → TaskHint.ShowToast("路上好像還有還沒消除的煙怪") ⇒ OnChestLocked
    └ 淨化 ≥ 600 → ⇒ OnChestOpened（B 開箱特效）→ ItemRewards.Grant(NicotineSpray)
                → PhaseManager.EnterPhase3()
[A] PhaseManager：Phase = Phase3 ⇒ OnPhaseChanged(Phase3)
    （GameManager 第一次進入時記下 HP；C 背包記下快照）
    → BossController.BeginIntro() ⇒ OnStateChanged(Intro)
    ⇒ OnBossIntroRequested                                        （B 聽）
[B] 登場演出（SetTimerPaused/SetControlLock "Cutscene"、SetCameraOverride）
    → PhaseManager.NotifyBossIntroFinished()
[A] PhaseManager ⇒ OnRulesIntroRequested                          （C 聽）
[C] RulesIntroWindow 顯示 → 玩家點擊 → PhaseManager.NotifyRulesIntroClosed()
[A] PhaseManager ⇒ OnBossFightStarted（C 血條顯示）
    → BossController.StartFight() ⇒ OnStateChanged(Sky)；SmokerNPC 全部開始抽菸
```

### c. BOSS 戰 → 最終局面 → 打倒 → 結局 → 結算（含重新挑戰）
```
[A] BossController：Sky 12 秒 ⇄ Ground 8 秒 ⇒ OnStateChanged（B 起飛、降落）
    Sky：BossAttack 每 4 秒菸炮 ⇒ OnCannonFired（B 預警）⇒ OnCannonLanded（煙霧區 3 秒）
    Ground：每 2 秒菸圈 ⇒ OnSmokeRingFired
    命中玩家 → PlayerHealth.TakeDamage(15 或 8, 來源) ⇒ OnDamaged
    BossHealth 每秒回血（依 SmokerNPC.SmokingCount）⇒ OnBossHpChanged（C 血條、GameManager 淨化）
    血量每降 25% ⇒ OnSizeLevelChanged（變低、變小）
[玩家] 近戰（地面）→ BossHealth.TakeHit ⇒ OnBossDamaged
       噴霧 → PlayerEffects.TryUseItem(Spray) ⇒ OnSprayFired（B 特效）→ BossHealth.TakeDamage(7.5, Spray)
       對 SmokerNPC 按 E → QTE ⇒ SmokerNPC.OnQteStarted（C 進度條）
         成功 ⇒ OnQteEnded(true) ⇒ OnAnySnuffed ⇒ OnSmokingCountChanged
[A] SmokingCount = 0 → BossController 立刻降落 ⇒ OnStateChanged(Final)
    不回血、不吐煙；玩家 3 公尺內每 3 秒物理攻擊 ⇒ OnMeleeWindup ⇒ OnMeleeSwing
    TaskHint「打倒大煙怪！」
[A] BossHealth 歸零 ⇒ OnBossDefeated ⇒ OnStateChanged(Defeated)
    GameManager：淨化 = 1000、停止用時
    PhaseManager：Phase = Ending ⇒ OnPhaseChanged(Ending) ⇒ OnEndingRequested（B 聽）
[B] 結局演出（BOSS 遺言、綠色城市）→ PhaseManager.NotifyEndingFinished()
[A] → GameManager.FinishWithVictory() ⇒ OnGameEnded(result)（C 結算畫面）

失敗：教學以外的任何時候 HP 歸零 ⇒ PlayerHealth.OnDied → GameManager 結算 ⇒ OnGameEnded
    （教學中 HP 歸零 → TutorialManager 補滿重來 ⇒ OnTutorialRestarted，不結算）
    第三階段失敗 → result.CanRetryFromPhase3 = true
[C] 按「從第三階段重新挑戰」→ GameManager.RetryFromPhase3()
[A] HP 恢復快照、暴露值歸零、用時繼續 ⇒ OnPhase3Retry（A：BOSS、SmokerNPC、菸圈重設；B：特效重設；C：背包恢復、結算與血條隱藏）
    → PhaseManager 重新 BeginIntro ⇒ OnBossIntroRequested（重播登場）⇒ … ⇒ OnRulesIntroRequested（重播規則視窗）
```

---

## 19. 任務提示文字的來源（`TaskHint`）

| 時機 | 誰設 | source | 文字 |
|---|---|---|---|
| 教學 4 步驟 | A TutorialManager | `Tutorial` | 見第 5 節 |
| 第一階段 | A PhaseManager | `Phase` | 消滅所有的煙怪吧！ |
| 第一 → 第二階段 | A PhaseManager | `Phase` | 城市中還有很多需要幫助的人，快去看看吧！ |
| 事件中各步驟 | B Director | `StoryEvent` | `StoryEventData` 的 FollowHint／ClearHint／InteractHint |
| 拒絕幫忙中 | A PhaseManager | `Refusal` | 煙霧好像越來越濃了…… |
| 拿到藏寶圖 | A PhaseManager | `Phase` | 到吸菸室旁的寶箱看看 |
| BOSS 戰 | A BossController | `Boss` | 捏熄吸菸者的菸，打倒大煙怪！／（Final）打倒大煙怪！ |
| 短暫提示 | A PlayerEffects／TreasureChest | Toast | 第三階段才能使用／路上好像還有還沒消除的煙怪 |

---

## 20. 舊介面處理表

| 舊介面 | 處理 | 誰、哪個任務 |
|---|---|---|
| `GameManager.RoundDuration`、`RemainingTime`、`OnFinalMinute`、`StartRound()`、`DevSetRemainingTime()` | ⚠️Obsolete | A11 刪除（C10 先把 HUD 改讀 `ElapsedTime`） |
| `GameManager.AddPurificationProgress(float)` | ⚠️Obsolete，暫時轉呼叫 | B10 改用 `AddPurification(int)` 後，A 在 I3 刪除 |
| `ScoreCalculator.Calculate`（6 個參數）、`ScoreResult.RemainingSeconds` | ⚠️Obsolete | A11 刪除 |
| `RouteManager.NotifyPlayerLeftAlley()`、`AlleyExitTrigger` | ⚠️Obsolete | B7 從 GameScene 移除元件；A 在 I3 刪檔 |
| `MonsterSize.Large` | ⚠️Obsolete | B10 停用大型菸怪 Prefab 後，A 在 I3 刪除 |
| `ItemType.NicotineInhaler`、`Champix`、`Bupropion` | ⚠️Obsolete | C7 刪除舊道具資料後，A 在 I3 刪除 |
| `PlayerAttack.HitCount` | ⚠️Obsolete | A13 刪除 |
| `ExposureSystem.RiseMultiplier`、`IsVisualSuppressed` | ⚠️Obsolete | A13 刪除 |
| `PlayerEffects.ApplyItem`、`OnEffectStarted`、`OnEffectEnded` | ⚠️Obsolete | A13 刪除 |
| `PlayerController.SetControlEnabled` | 保留（等於 key "Default"） | — |
| `SmokeMonster.ProgressValue` | B 的檔案，A 不標記 | B10 改成 `PurificationUnits` |
| `EscortNPC`、`SafeZone`、`GuideLine` | 刪除 | B7（在 Unity Project 視窗） |
| `Assets/_Project/UI/InGameMenu.cs` | 刪除（暫停選單改由 C 做） | B7（在 Unity Project 視窗） |
| `ChestController`、`QuizManager`、`QuizData` | 刪除 | C7（在 Unity Project 視窗） |
| `OpeningSequence` 的 OnGUI「新手引導」文字 | 移除，改用 TaskHint | B7 |
| `DevOverlay`、`DevHotkeys` 的倒數功能 | 改成用時、加跳到第三階段 | A11、F4 |
| `Editor/ScoreCheckTools.cs` | 會出現 Obsolete 警告 | A11 改用新版 |

## 21. 數值清單（全部做成 Inspector 參數）

| 數值 | 預設 | 放在 |
|---|---|---|
| HP 最大值 | 100 | PlayerHealth |
| 暴露值 100 每秒扣血 | 5 | PlayerHealth |
| 範圍內上升係數／範圍外下降 | 10／3 | ExposureSystem（Player.prefab 已存 10／3） |
| 警示門檻／100 時移速倍率 | 80／0.7 | ExposureSystem |
| 煙霧半徑：小菸怪、吸菸 NPC／BOSS／菸炮煙霧區 | 4／7／3【暫定】 | SmokeSource（各 Prefab） |
| 菸怪淨化值：教學／第一階段／空檔／事件 | 0／40 ×5／25 ×4／33、33、34 ×3 | PhaseManager（`GetMonsterUnits`；開始時檢查總和 = 600） |
| 第一階段目標／寶箱門檻 | 200／600 | PhaseManager、TreasureChest |
| 事件間隔 | 40 秒 | PhaseManager |
| 拒絕煙霧：間隔／每次增加／上限 | 10 秒／0.1／2 | PhaseManager |
| 教學：走到目標判定半徑／靠近菸怪半徑 | 1.5／3【暫定】 | TutorialManager |
| 菸怪：受擊次數／遊走半徑／速度 | 3／3／1 | SmokeMonster（B） |
| 近戰：距離／角度／冷卻 | 2／90／0.5 | PlayerAttack |
| 貼片回血 | 30 | PlayerEffects |
| 口嚼錠半徑 | 15 | PlayerEffects |
| 噴霧傷害／冷卻／最遠距離 | 7.5／1.5／30【暫定】 | PlayerEffects |
| BOSS 最大血量／近戰受傷 | 150／3 | BossHealth |
| 回血：地面每人／天上基本／天上每人 | 1／1／1 | BossHealth |
| 地面停留／天上停留 | 8／12 | BossController |
| 天上高度（4 級）／體型（4 級） | 12、10、8、6 公尺【暫定】／1、0.85、0.7、0.55【暫定】 | BossController |
| 菸圈：間隔／傷害／速度／存在時間／命中半徑 | 2／8／6／4／0.8【後三個暫定】 | BossAttack |
| 菸炮：間隔／傷害／飛行時間／命中半徑／煙霧區時間 | 4／15／1.5／1.5／3【飛行、半徑暫定】 | BossAttack |
| 物理攻擊：距離／間隔／傷害／預備時間 | 3／3／10／0.6【暫定】 | BossAttack |
| QTE：距離／角度／次數／時間 | 2／90／5／2 | SmokerNPC |
| 用時分：滿分時間／每段秒數／每段扣分 | 480／30／10 | ScoreCalculator（static，用常數；A20 再決定要不要搬到 Inspector） |
| 評分權重 | 0.6／0.4 | ScoreCalculator（常數） |
