# INTERFACES.md — 共用介面規格（v3.1／第二次檢核）

> 更新日期：2026-11-27（A15）。依據 `docs/GDD_v3.1.md` 與 `docs/DECISIONS.md`。
>
> **A15 更新內容（2026-11-27）**
> - SmokeSource 完成（第 11 節）：🆕 `SetRadiusModifier`／`RemoveRadiusModifier`（key 倍率）、🆕 `OnRadiusChanged`、🆕 `ResetRefusalState`；有 `SmokeMonster` 的物件**自動** `AffectedByRefusal = true`
> - **拒絕幫忙改版（A 決定，2026-11-27）**：煙霧半徑**不擴大**；改成 ① 拒絕期間暴露值**全場景統一每秒 +2** ② 🆕 `RefusalMonsterSpawner` 每 3 秒在玩家 2–10 公尺內補生成菸怪（透明度淡入，接任務後淡出刪除）③ NPC 台詞 → 警告文字。任務提示「煙霧好像越來越濃了……」**刪除**（第 4 節）
> - PhaseManager：🆕 `OnRefusalNpcLine`、🆕 `NotifyRefusalNpcLineFinished()`、🆕 `IsWaitingRefusalLine`、🆕 `DevSetRefusal(bool)`；換場景自動清除拒絕狀態
> - B12 要配合 SmokeMonster 的事項（第 17 節）：B **不用寫計時、不用多呼叫方法**
> - 標 🆕A15 = 這次新增
>
> **A14 更新內容（2026-11-26）**
> - BOSS 地面版完成：`BossController`（狀態機）、`BossHealth`（🆕 `Heal`、實作 `IBossDamageable`）、`BossAttack`（🆕 `ActiveRingCount`、`SmokeRingCooldownRemaining`、`StopAndClearAll`）、🆕 `BossSmokeRing`（第 14 節）
> - **菸炮取消，和菸圈合併成同一種東西**（天上也吐菸圈；天上的菸圈落地留下 3 秒煙霧區，A16）。`OnCannonFired`／`OnCannonLanded` A16 刪除
> - 🆕 BOSS Prefab 結構與擁有者（第 14 節）：`Boss.prefab`／`SmokeRing.prefab`（A）裡面巢狀 `BossVisual.prefab`／`SmokeRingVisual.prefab`（B）
> - PhaseManager：登場 → 規則視窗 → BOSS 戰串接完成；🆕 `DevStartBossFight()`；**沒有人監聽 `OnEndingRequested` 時直接勝利結算**（第 4 節）
> - PlayerHealth 🆕 `DevInvincible`（開發者用，第 9 節）
> - **口嚼錠不能清除菸圈**（只清煙霧區）：`BossSmokeRing` 不實作 `IClearableSmoke`
> - BossController：BOSS 戰中在地面時水平轉向玩家（Inspector `_turnSpeed`，預設每秒 360 度）
> - 刪除：DevTools 的 `FakeBoss`（改用真的 BOSS）
> - 標 🆕A14 = 這次新增
>
> **A13 更新內容（2026-11-25）**
> - 🆕 可選介面 `IInteractCondition`（目標自己的互動條件）、`IQteInteractable`（QTE 期間 E 鍵轉交）；PlayerInteractor 🆕 `CurrentQteTarget`、`OnQteModeChanged`（第 2 節）
> - 🆕 `IBossDamageable`：噴霧只認這個介面（第 1 節）；🆕 `IClearableSmoke` ＋ `ClearableSmoke` 登記表：口嚼錠清除菸圈（第 14 節）
> - PlayerEffects 完成；🆕 `OnItemUseRejected`、`OnGumUsed`、`GumClearRadius`、`IsPhase3ItemAvailable`（第 12 節）
> - 刪除：`PlayerEffects.ApplyItem`／`OnEffectStarted`／`OnEffectEnded`、`ExposureSystem.RiseMultiplier`／`IsVisualSuppressed`、`PlayerAttack.HitCount`、`ItemType` 的 3 個舊道具、`BossAttack.OnSmokeCleared`（改用 `ClearableSmoke.OnCleared`）
> - 標 🆕A13 = 這次新增
>
> **A11 更新內容（2026-11-25）**
> - GameManager：倒數完全移除（時間到不會失敗）；用時；**第三階段淨化進度固定 60%、不跟 BOSS 血量連動**，BOSS 被打倒（🆕 `NotifyBossDefeated()`）才變 100%；結算改用用時
> - PhaseManager：流程完成；🆕 `OnPurificationMilestone`、`NotifyTutorialFinished()`、`BeginTutorialPhase()`、`IsEventInProgress`
> - ScoreCalculator：用時分改成「依秒數比例扣、四捨五入到整數」
> - 🆕 第 22 節「第三階段起點」：`Phase3Checkpoint`、`Phase3Snapshot`（快照）；需要 B、C 配合的介面標「待確認」
> - 刪除：`RoundDuration`、`OnFinalMinute`、`StartRound()`、`DevSetRemainingTime()`、舊版 `ScoreCalculator.Calculate`、`ScoreResult.RemainingSeconds`
> - 標 🆕A11 = 這次新增
> 三人（以及三種 AI 工具）都必須照這份的名稱與參數寫程式。
> **要改這份文件，先在群組講，由 A 更新並合併進 develop。**
> C 使用 ChatGPT 時，每次請把這份文件（或相關段落）上傳／貼給 ChatGPT。
>
> 所有類別都**不使用 namespace**。事件一律是 C# `event Action<...>`。
> 「擁有者」= 唯一可以修改這支檔案的人。
> 標 🆕 = 這次新增；標 ⚠️Obsolete = 已標記即將移除（還能用，但會出現黃色警告，見第 20 節）。
> 「A 已建空殼」= A10 已經建立檔案、名稱都對，但內容由後面的任務（A11–A17）才完成。
> A10 已建空殼的檔案：`PhaseManager`、`TutorialManager`、`TaskHint`、`ItemRewards`、`TreasureChest`、`BossController`、`BossHealth`、`BossAttack`、`SmokerNPC`、`PlayerEffects`（A13 已完成）。
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

// 道具：A13 刪除舊道具，編號寫死不變（避免已存的資料對錯道具；2、4、5 是已刪除的舊道具）
public enum ItemType { NicotineGum = 0, NicotinePatch = 1, NicotineSpray = 3 }
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
    public float  RemainingHp;        // 結算時 HP（勝利：打倒 BOSS 那一刻的 HP）
    public float  ClearTimeSeconds;   // 用時（秒）。意思從「花費秒數」變成「用時」，名稱不變
    // （A11 已刪除 RemainingSeconds）
    public float  SmokeSeconds;       // 待在煙霧裡的秒數（保留，C 可以決定要不要顯示）
    public float  HpScore;            // 0–100
    public float  TimeScore;          // 0–100（用時分）
    public float  TotalScore;         // 0–100
    public string Grade;              // "S" "A" "B" "C" "D"
    public string Title;              // 🆕 頭銜（A18 才填，之前是空字串）
    public bool   CanRetryFromPhase3; // 🆕 失敗而且是在第三階段失敗 → ResultUI 顯示「從第三階段重新挑戰」（A19 之前固定 false）
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
| ~~`int HitCount`~~ | ✅A13 已刪除（每次命中固定 `TakeHit(1)`） |

- 判定範圍：前方 **90 度扇形**（Inspector `_angle`，已經是 90）。`PlayerController.IsActionAllowed(PlayerAction.Attack)` 為 false 時不能攻擊（新手教學前 3 步）。
- 對 BOSS：打到 BossHealth 時，每下傷害 = `BossHealth` 的 Inspector「近戰傷害」（預設 3）。BOSS 在天上時打不到。

### `TargetMarker`（擁有者：A）— 不變

### 🆕A13 介面 `IBossDamageable` `Scripts/Combat/IBossDamageable.cs`（擁有者：A）
```csharp
public interface IBossDamageable {
    void TakeDamage(float amount, DamageSource source);   // 噴霧：(7.5, DamageSource.Spray)
}
```
- **噴霧只認這個介面**（PlayerEffects 不直接依賴 BossHealth）。實作者：A14 的 `BossHealth`（DevTools 的 `FakeBoss` A14 已移除）。
- **小菸怪不要實作**（噴霧對小菸怪無效）。實作的物件（或子物件）要有 Collider（可以是 Trigger）。

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

### 🆕A13 可選介面 `IInteractCondition` `Scripts/Interaction/IInteractCondition.cs`（擁有者：A）
```csharp
public interface IInteractCondition {
    bool CanInteractFrom(Transform player);   // player = 玩家根物件（position 腳底、forward 角色面向）
}
```
- `IInteractable` **另外**實作這個時，PlayerInteractor 會多問一次；沒實作的物件照原本規則（**B 現有的 NPC 不用改**）。
- 用途：A16 的 SmokerNPC「面向它、前方 90 度、2 公尺內」。PlayerInteractor 本身的搜尋範圍是 2.5 公尺，條件只能**更嚴格**，不能更寬。

### 🆕A13 可選介面 `IQteInteractable` `Scripts/Interaction/IQteInteractable.cs`（擁有者：A）
```csharp
public interface IQteInteractable : IInteractable {
    bool IsQteActive { get; }    // QTE 進行中
    void RegisterQtePress();     // 【只給 PlayerInteractor】QTE 期間每按一次 E（不含開始的第一下）
}
```
- 流程：按 E → `Interact()`（目標自己開始 QTE、**第一下自己算**）→ `IsQteActive` 為 true → PlayerInteractor 進入 QTE 模式。
- QTE 模式：鎖定這個目標（不切換到別的目標）、用 key `"Qte"` 鎖住 `Move`、`Jump`（視角可以轉）、之後每次按 E 呼叫 `RegisterQtePress()`。
- `IsQteActive` 變回 false（成功、失敗、離開範圍，**由目標自己判定**）、目標被停用或刪除 → 結束 QTE 模式、解鎖移動、恢復正常找目標。
- 實作者：A16 的 SmokerNPC、DevTools 的 `FakeSmokerNPC`。

### `PlayerInteractor` `Scripts/Interaction/PlayerInteractor.cs`（擁有者：A，A13 完成）
| 成員 | 說明 |
|---|---|
| `IInteractable CurrentTarget { get; }` | 不變（QTE 期間固定是 QTE 目標） |
| `event Action<IInteractable> OnFocusChanged` | 不變（C 的「按 E」提示用） |
| `event Action<IInteractable> OnInteracted` | 玩家按 E 成功互動時（B 可接音效）。QTE 期間的連打不會觸發 |
| `bool IsInQte { get; }` | 正在 QTE 連打中。這段期間 E 鍵全部交給 QTE 目標，不會找其他目標 |
| 🆕A13 `IQteInteractable CurrentQteTarget { get; }` | 正在 QTE 的目標；沒有時 null |
| 🆕A13 `event Action<bool> OnQteModeChanged` | 進入（true）／離開（false）QTE 模式。C 可以用來把「按 E」提示換成「連打 E！」 |

- 目標選擇：範圍內（2.5 公尺）最近、`CanInteract` 為 true、通過 `IInteractCondition`（有實作的話）、沒被牆擋住。
- **沒有目標（提示消失、按 E 沒反應）的時機**：`PlayerController.IsActionAllowed(PlayerAction.Interact)` 為 false（控制鎖、新手教學、暫停）、GameManager「暫停計時」中（對話、過場、視窗）、遊戲結束（`Ended`）。QTE 期間遇到這些情況時，E 不會轉交，但 QTE 模式不會中斷（計時由目標自己決定）。

---

## 3. `GameManager` `Scripts/Core/GameManager.cs`（擁有者：A，A11 完成）

### 狀態與用時
| 成員 | 說明 |
|---|---|
| `static GameManager Instance` | |
| `GameState State { get; }` | 不變 |
| `float ElapsedTime { get; }` | **用時（秒）**。HUD 每幀直接讀，不另設事件 |
| `bool IsTimerPaused { get; }` | 目前有沒有任何系統申請「暫停計時」 |
| `void SetTimerPaused(string key, bool paused)` | **任何系統都可以申請暫停計時**，用自己的 key（例如 `"Dialogue"`、`"Teleport"`、`"BossIntro"`）。所有 key 都放開才繼續計時，不會互相覆蓋 |
| 🆕A11 `IReadOnlyCollection<string> TimerPauseKeys { get; }` | 【開發者工具用】目前申請暫停計時的 key |
| `void SetPaused(bool paused)` | 不變：Esc 暫停選單（`Time.timeScale = 0`） |
| `event Action<bool> OnPauseChanged` | 不變 |
| `event Action<bool> OnTimerPausedChanged` | 「暫停計時」狀態改變時 |
| `event Action OnRoundStarted` | **新手教學開始**（遊戲進入 Playing，HP、用時開始跑） |
| `event Action OnRunReset` | **新手教學結束、第一階段開始時**：用時歸零、HP 補滿、暴露值歸零、淨化歸零 |

**「暫停計時」期間不只停計時**：暴露值、HP（不扣也不回）、40 秒事件空檔、BOSS 行動都會一起停。用在：對話、「要幫忙嗎？」視窗、傳送淡入淡出、過場動畫（BOSS 登場、結局）、第三階段規則介紹視窗。

- **沒有倒數。時間到不會失敗；唯一的失敗條件是 HP 歸零。**
- 新手教學期間 HP 和用時**照算**；教學結束時 `OnRunReset`，全部重新計算（結算的用時 = 從第一階段開始）。
- 用時從 `OnRunReset` 開始到 **BOSS 被打倒為止**；結局動畫不算（GameManager 用 key `"BossDefeated"` 申請暫停計時，所以結局期間 HP、暴露值也不會變）。
- HP 歸零：教學以外的任何階段 → 失敗結算。**教學中 HP 歸零不結算**（A12 TutorialManager 補滿重來）；BOSS 被打倒之後也不再判定。

### 淨化進度（內部整數，1000 = 100%）
| 成員 | 說明 |
|---|---|
| `const int PurificationMax = 1000` | |
| 🆕A11 `const int Phase3PurificationUnits = 600` | 第三階段固定的淨化進度（60%） |
| `int PurificationUnits { get; }` | 0–1000 整數。**寶箱等判斷一律用這個** |
| `float PurificationProgress { get; }` | 0–100，給 UI 顯示（= Units ÷ 10） |
| 🆕A11 `bool IsPurificationLocked { get; }` | 第三階段、結局時 true：`AddPurification` 沒有作用 |
| `void AddPurification(int units)` | 菸怪被擊敗時呼叫（B 的 SmokeMonster）。只在 Playing、而且沒鎖住時有效；結果限制在 0–1000 |
| ⚠️Obsolete `void AddPurificationProgress(float amount)` | 改用 `AddPurification(int)`；暫時會把 amount × 10 四捨五入後轉呼叫（B 的 SmokeMonster 還在用，B10 改完後 I3 刪除） |
| `event Action<float> OnProgressChanged` | 不變，參數 0–100 |
| 🆕A11 `void LockPurificationForPhase3()` | 【只給 PhaseManager】進入第三階段：淨化設為 600 並鎖住 |
| 🆕A11 `void NotifyBossDefeated()` | **【給 A14 的 BOSS 呼叫】BOSS 被打倒**：淨化設為 1000、停止用時、記下這時的 HP（勝利的血量分）。之後 PhaseManager 進入結局 |
| 🆕A11 `bool IsBossDefeated { get; }` | |

- **第三階段淨化進度不跟 BOSS 血量連動（A11 新決定，取代 GDD 4.6、8.1 的公式）**：進入第三階段時是 60%，整場 BOSS 戰都停在 60%（BOSS 回血也不會下降，菸怪等任何來源也不能改變）；BOSS 被打倒的那一刻才變 100%。戰鬥中的回饋看 BOSS 血條（C12）。GameManager **不監聽** `BossHealth.OnBossHpChanged`。
- 淨化到 1000 不會直接勝利；勝利流程見第 18 節 c。

### 結束、重新開始、重新挑戰
| 成員 | 說明 |
|---|---|
| `ScoreResult LastResult { get; }` | 不變 |
| `event Action<ScoreResult> OnGameEnded` | 勝利（結局動畫播完）或失敗（HP 歸零） |
| `void RestartToOpening()` | **C 的暫停選單／結算畫面「重新開始」呼叫** → 清除第三階段快照、重新載入 GameScene，回到開場選擇 |
| `void ReturnToMainMenu()` | **C 的暫停選單「返回主選單」呼叫** → 清除第三階段快照、載入 MainMenu |
| `bool CanRetryFromPhase3 { get; }` | 結算時是否可以從第三階段重新挑戰（**A19 之前固定 false**） |
| `void RetryFromPhase3()` | **C 的結算畫面按鈕呼叫**（A19 完成）。流程見第 22 節 |
| `event Action OnPhase3Retry` | （A19）從第三階段起點開始時。意思見第 22 節 |
| `void FinishWithVictory()` | 【只給 PhaseManager 用】結局動畫結束 → 勝利結算（BOSS 沒被打倒時無效） |
| `void BeginRun()` | 【只給 PhaseManager 用】新手教學開始 → 進入 Playing |
| `void ResetRunForPhase1()` | 【只給 PhaseManager 用】教學結束 → 用時、HP、暴露值、淨化重設 ⇒ `OnRunReset` |

- Esc 仍由 GameManager 讀。**B、C 不要自己 `SceneManager.LoadScene`**，一律呼叫上面兩個方法。
- 開發者用：`DevMode`、`DevForceEnd(bool)`、🆕A11 `DevSetPurification(int)`；Inspector 🆕A11 `devAutoStart`（Off／Tutorial／Phase1，取代舊的 autoStartRound；場景有 RouteManager 時忽略）。
- ⚠️Obsolete `RemainingTime`：**暫時回傳用時**（讓 C 的舊 HUD 顯示往上數的時間），C10 改讀 `ElapsedTime` 後刪除。
- A11 已刪除：`RoundDuration`、`OnFinalMinute`、`StartRound()`、`DevSetRemainingTime()`。

---

## 4. `PhaseManager` `Scripts/Core/PhaseManager.cs`（擁有者：A，A11 完成；A14 BOSS 串接完成；A15 拒絕幫忙完成）

整局流程的導演：教學 → 第一階段 → 三個事件 → 寶箱 → BOSS → 結局。和 GameManager 放在同一個物件上。
**GameScene 的 GameManager 物件上要掛 PhaseManager 和 TutorialManager（B 在 GameScene 加）。**

### 狀態
| 成員 | 說明 |
|---|---|
| `static PhaseManager Instance` | |
| `GamePhase CurrentPhase { get; }` | |
| `event Action<GamePhase> OnPhaseChanged` | HUD、小地圖、B 的菸怪生成、BOSS 都可以監聽 |
| 🆕A11 `event Action<int> OnPurificationMilestone` | 淨化第一次達到 20、60、100（%）時各觸發一次。參數：20／60／100 |
| 🆕A11 `bool HasReachedMilestone(int percent)` | 是否已經達到過（20／60／100） |
| `int CurrentEventIndex { get; }` | 目前（或剛完成的）故事事件 0、1、2；還沒到第二階段時 -1 |
| 🆕A11 `bool IsEventInProgress { get; }` | 目前的事件已經接下、正在進行 |
| `bool IsInEventGap { get; }` / `float EventGapRemaining { get; }` | 是否在 40 秒空檔、剩幾秒 |
| `bool IsHelpRefused { get; }` | 玩家目前是否拒絕幫忙中 |
| `float RefusalSmokeMultiplier { get; }` | **A15 決定煙霧不擴大，固定 1**（保留名稱） |
| 🆕A15 `bool IsWaitingRefusalLine { get; }` | 【開發者工具用】正在等 NPC 講完拒絕台詞 |
| `bool HasTreasureMap { get; }` | 拿到藏寶圖了沒 |
| `int GetMonsterUnits(MonsterGroup group, int indexInGroup)` | **B 的生成器呼叫**：這隻菸怪要給多少淨化值（見第 21 節） |

### 給 B 的事件（A → B）
| 事件 | B 要做什麼 |
|---|---|
| `event Action<int> OnStoryEventRequested` | 參數：事件編號 0/1/2。**StoryEventDirector 讓那位 NPC 跑來求救** |
| `event Action<int> OnEventGapStarted` | 參數：空檔編號 0/1（事件一後、事件二後）。**B 在街上生成 2 隻空檔菸怪** |
| `event Action<int> OnEventGapEnded` | 40 秒到了（緊接著會發 `OnStoryEventRequested(下一個)`） |
| `event Action<bool> OnHelpRefusedChanged` | true = 開始拒絕（NPC 開始跟著玩家、菸怪外觀改「打不掉」）；false = 接下任務，恢復 |
| 🆕A15 `event Action<int, string> OnRefusalNpcLine` | 參數：事件編號、台詞。拒絕當下（**含再拒絕一次**）→ **B 讓跟著玩家的 NPC 說這句**，講完呼叫 `NotifyRefusalNpcLineFinished()`。⚠️ **一監聽就一定要回報**，不然警告文字不會出現；沒有人監聽時 PhaseManager 自己用 Toast 顯示台詞，3 秒後接著顯示警告 |
| `event Action OnTreasureMapObtained` | 小地圖顯示寶箱（C）、寶箱發光（B 可選） |
| `event Action OnBossIntroRequested` | **B 播 BOSS 登場動畫**（大煙怪衝上天、吸菸者散開）；進入第三階段時發出，重新挑戰時也會再發一次 |
| `event Action OnRulesIntroRequested` | **C 顯示第三階段規則介紹視窗**（B 呼叫 `NotifyBossIntroFinished()` 之後） |
| `event Action OnBossFightStarted` | 規則視窗關掉，BOSS 戰正式開始（緊接著 `BossController.StartFight()`） |
| `event Action OnEndingRequested` | BOSS 被打倒 → **B 播結局動畫**（遺言對話、綠色無煙城市）。🆕A14 **只要有人監聽，PhaseManager 就會等 `NotifyEndingFinished()` 才結算；沒有人監聽時直接勝利結算**（B17 接上之前） |

### 呼叫 PhaseManager 的方法
| 方法 | 誰呼叫 | 時機 |
|---|---|---|
| 🆕A11 `void BeginTutorialPhase()` | PhaseManager 自己（聽 `RouteManager.OnOpeningFinished(RefuseSmoke)`）；開發者自動開始 | 開場結束 → 新手教學。B **不用**呼叫 |
| 🆕A11 `void NotifyTutorialFinished()` | A12 的 TutorialManager（現在用 F1 模擬） | 教學菸怪被打倒 → 第一階段 |
| `void ReportEventAccepted(int index)` | B 的 Director | 事件一對話結束（強制）；事件二、三按「幫忙」；**拒絕後重新接受也是這個** |
| `void ReportHelpRefused(int index)` | B 的 Director | 事件二、三按「拒絕」（可以重複拒絕；事件一呼叫會被忽略） |
| 🆕A15 `void NotifyRefusalNpcLineFinished()` | B 的跟隨 NPC | 拒絕台詞講完 → PhaseManager 顯示警告文字（Toast）。只在拒絕中、等台詞時有效 |
| `void ReportEventCompleted(int index)` | B 的 Director | 給完道具、傳送回街道之後（要先接受過） |
| `void NotifyBossIntroFinished()` | B 的 BOSS 登場演出 | 登場動畫播完 → `OnRulesIntroRequested`。只在第三階段登場中有效 |
| `void NotifyRulesIntroClosed()` | C 的 RulesIntroWindow | 玩家點擊關掉視窗 → `OnBossFightStarted` → `BossController.StartFight()`。只在規則視窗顯示中有效 |
| `void NotifyEndingFinished()` | B 的結局演出 | 結局動畫播完 → 勝利結算（Ending 階段才有效） |
| `void EnterPhase3()` | 【只給 A 的 TreasureChest】 | 寶箱打開（第二階段、拿到藏寶圖後才有效，只有第一次有效） |
| `void DevJumpToPhase3()` / `void DevSetEventGapRemaining(float)` / 🆕A14 `void DevStartBossFight()` / 🆕A15 `void DevSetRefusal(bool)` | 【只給 A 的 DevTools】 | 開發者快捷鍵（`DevStartBossFight`：跳過登場與規則視窗，直接開始 BOSS 戰；`DevSetRefusal`：不檢查事件順序，直接開始／結束拒絕狀態，效果和真的拒絕相同，第三階段以前有效） |

- 編號不對、順序不對（例如還沒接受就完成、空檔中回報）的呼叫會被忽略並在 Console 印黃色警告。

### 規則
- 第一階段：淨化 `PurificationUnits >= 200`（整數判斷）→ 進入 Phase2 → 任務提示「城市中還有很多需要幫助的人，快去看看吧！」→ `OnStoryEventRequested(0)`。
- 40 秒空檔（Inspector 可調）在暫停選單、「暫停計時」期間都會停。
- 拒絕：`IsHelpRefused`、`OnHelpRefusedChanged`。🆕A15 **沒有任務提示**（原本的「煙霧好像越來越濃了……」刪除，source `Refusal` 不再使用）。
- 🆕A15 拒絕幫忙（**全部由 A 處理，B12 不用寫計時**）：
  - `ReportHelpRefused(n)` → `SmokeSource.SetRefusalState(true, 1)`：整張地圖的菸怪立刻無法消除。**煙霧半徑不擴大**（倍率固定 1）
  - **暴露值**（ExposureSystem）：拒絕期間不管在不在煙霧裡，**整個場景統一每秒 +2**（Inspector `_refusalRiseRate`）。約 40 秒到 80（警示）、50 秒到 100（開始扣血）
  - **補生成菸怪**（🆕 `RefusalMonsterSpawner`，掛在 GameManager 物件上）：每 3 秒在玩家周圍 2–10 公尺生成 1 隻，最多 15 隻；用**透明度淡入**（0.8 秒）。數值都是 Inspector 參數
  - **台詞**：拒絕當下 ⇒ `OnRefusalNpcLine`（NPC 說「再這樣下去，整個城市都要被二手菸淹沒了啦……」）→ B 呼叫 `NotifyRefusalNpcLineFinished()` → `TaskHint.ShowToast("因為無情地拒絕幫忙，城市煙霧瀰漫……再這樣下去會有生命危險！", 4)`。文字是 PhaseManager 的 Inspector 欄位
  - 暫停選單、任何「暫停計時」（對話、「要幫忙嗎？」視窗、傳送…）期間：暴露值、補生成、台詞的備用計時都停
  - **再拒絕一次（對跟隨的 NPC 按 E 又按拒絕）**：暴露值、補生成繼續（不重新開始），**台詞與警告再出現一次**
  - `ReportEventAccepted(n)`、進入第三階段 → `SetRefusalState(false, 1)`：所有菸怪可以消除；補生成的菸怪**停止冒煙、不能被打，淡出後刪除**；暴露值恢復一般規則
  - PhaseManager 的 `Awake`、`OnDestroy` 呼叫 `SmokeSource.ResetRefusalState()`：重新開始、重新挑戰、回主選單後不會殘留
- 事件三完成 → `HasTreasureMap = true`、`OnTreasureMapObtained`、任務提示「到吸菸室旁的寶箱看看」。
- 進入第三階段（`EnterPhase3`）：教學與三個事件視為完成 → `GameManager.LockPurificationForPhase3()`（60%）→ **第一次進入時存下快照**（第 22 節）→ 清掉 `Phase` 任務提示 → `OnPhaseChanged(Phase3)` → `BossController.BeginIntro()` → `OnBossIntroRequested`。
- 🆕A14 BOSS 戰開場順序固定：登場（等 B 的 `NotifyBossIntroFinished`）→ 規則視窗（等 C 的 `NotifyRulesIntroClosed`）→ 戰鬥。**沒有人接登場或規則視窗時不會自動跳過**（印黃色警告，停在那一步；開發者按 9）。
- BOSS 被打倒（`GameManager.NotifyBossDefeated()` → 淨化 100%）→ `OnPurificationMilestone(100)` → `OnPhaseChanged(Ending)` → `OnEndingRequested` → B 呼叫 `NotifyEndingFinished()` → 勝利結算。🆕A14 沒有人監聽 `OnEndingRequested` 時，直接勝利結算（之後 B17 加上結局動畫，**GameManager 與 PhaseManager 都不用改**）。


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
| Done | 消滅所有的煙怪吧！ | E、道具 | → 呼叫 `PhaseManager.NotifyTutorialFinished()` → 進入 Phase1、`GameManager.OnRunReset` |

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
| ⚠️Obsolete `void NotifyPlayerLeftAlley()` | **A11 起不做任何事**（新流程不再用「走出死巷」開始）。I3 刪除 |

流程（改變處）：
- 路線 A：不變。B7 改成隨機一位路人演出，結束呼叫 `NotifyOpeningFinished()` → RouteManager 重新載入場景。
- 路線 B：**車庫門開好時就呼叫 `NotifyOpeningFinished()`**（A11 已在 B 的 OpeningSequence 改好，經 A、B 同意）→ RouteManager 恢復玩家控制 → PhaseManager 收到 `OnOpeningFinished(RefuseSmoke)` 開始新手教學。場景沒有 PhaseManager 時會印警告。
- ⚠️Obsolete `AlleyExitTrigger`：沒有作用了（GameScene 還掛著，B 在 GameScene 移除元件後，A 在 I3 刪檔）。A 的 Editor 選單已不再自動加這個元件。

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
| 🆕A14 `bool DevInvincible { get; set; }` | 【只給 A 的 DevTools】玩家無敵：所有傷害都不扣（含暴露值 100 的扣血）。只有開發者模式下能設成 true |

- 「暫停計時」期間不扣也不回。

## 10. `ExposureSystem` `Scripts/Player/ExposureSystem.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `Exposure` / `VisualExposure` / `IsWarning` / `IsMaxed` / `SmokeSeconds` / 三個事件 | 不變（`VisualExposure` 現在永遠等於 `Exposure`） |
| 🆕 `void ResetExposure()` | 【只給 GameManager】教學結束、重新挑戰 |
| ~~`RiseMultiplier`、`IsVisualSuppressed`~~ | ✅A13 已刪除 |
| 🆕A15 `float RefusalRiseRate { get; }` / `bool IsRefusalRising { get; }` | 拒絕幫忙期間全場統一的每秒上升量（預設 2）／現在是否正在用它（開發者工具用） |

- 「暫停計時」期間暴露值不變。
- 🆕A15 拒絕幫忙期間（`PhaseManager.IsHelpRefused`）：不管在不在煙霧裡，**統一每秒 +2**（取代範圍內上升與範圍外下降）。`SmokeSeconds` 仍然只算真的在煙霧範圍內的秒數。

## 11. `SmokeSource` `Scripts/Monsters/SmokeSource.cs`（擁有者：A，A15 完成）

| 成員 | 說明 |
|---|---|
| `static IReadOnlyList<SmokeSource> All` | 不變 |
| `float Radius { get; set; }` | **讀取 = 實際半徑** = `BaseRadius` × `RadiusMultiplier` × 所有 key 倍率相乘 ×（`AffectedByRefusal` 而且拒絕中時）`RefusalMultiplier`。**ExposureSystem、Gizmos 都用這個**；設定 = 設基本半徑（相容舊程式） |
| `float BaseRadius { get; set; }` | Inspector 的基本半徑。小菸怪、吸菸 NPC 4；BOSS 7 |
| `float RadiusMultiplier { get; set; }` | Inspector 上這個來源自己的倍率，預設 1。**程式要改倍率請用下面的 key 版**，避免互相覆蓋 |
| 🆕A15 `void SetRadiusModifier(string key, float multiplier)` / `void RemoveRadiusModifier(string key)` | 用自己的 key 加倍率（和 `PlayerController.SetSpeedModifier` 一樣），多個 key **相乘**、不會互相覆蓋。會影響暴露值，**加新的 key 前先問 A** |
| 🆕A15 `event Action<float> OnRadiusChanged` | 這個來源的**實際半徑**改變時（參數：新的實際半徑）。**B 用「實際半徑 ÷ `BaseRadius`」縮放煙霧粒子**。剛出生時不觸發（請在 Start 讀一次 `Radius`）；停用期間變了的話，重新啟用時補發 |
| `bool AffectedByRefusal { get; set; }` | **菸怪是 true**。🆕A15 同一個物件上有 `SmokeMonster` 時，SmokeSource 的 Awake **自動設成 true**（B 自己設也可以）；開場朋友、吸菸 NPC、BOSS、煙霧區是 false（預設） |
| `bool IsIndestructible { get; }` | 拒絕幫忙中而且 `AffectedByRefusal` → true。**B 的 SmokeMonster.TakeHit 看到 true 就不算這一下** |
| `static float RefusalMultiplier { get; }` / `static bool RefusalActive { get; }` | 拒絕倍率（沒有拒絕時 1）／是否拒絕中 |
| `static event Action<bool> OnRefusalStateChanged` | 開始（true）／結束（false）拒絕時。B 換「打不掉」的外觀用。**static 事件：OnDisable 一定要取消監聽** |
| `static void SetRefusalState(bool active, float multiplier)` | 【只給 PhaseManager】受影響的來源各自觸發 `OnRadiusChanged`，再觸發 `OnRefusalStateChanged`（狀態有變時） |
| 🆕A15 `static void ResetRefusalState()` | 【只給 PhaseManager】換場景時清回初始值，**不觸發事件** |
| `bool IsEmitting { get; set; }` | 不變 |

- 掛在：開場朋友、每一隻菸怪、吸菸 NPC、BOSS、天上菸圈落地的煙霧區。
- 🆕A15 **「無法消除」只影響菸怪**（`AffectedByRefusal`）。拒絕中才生成的菸怪，一出生就無法消除（每次讀取時才算，不用另外處理）。拒絕時煙霧半徑不擴大（`RefusalMultiplier` 固定 1）；key 倍率與 `OnRadiusChanged` 保留給之後的機制。

### 🆕A15 `RefusalMonsterSpawner` `Scripts/Core/RefusalMonsterSpawner.cs`（擁有者：A）
| 成員 | 說明 |
|---|---|
| `static RefusalMonsterSpawner Instance` | |
| `int ActiveCount { get; }` / `int MaxMonsters { get; }` / `float NextSpawnRemaining { get; }` | 開發者工具用 |

- **B 在 GameScene 的 GameManager 物件上加這個元件**，把 `SmokeMonster_Small.prefab` 拖進「菸怪 Prefab」。
- 生成位置：玩家周圍隨機（依面積平均），往下打射線找地面；高度差 > 1.5 公尺（屋頂）、周圍 0.6 公尺有東西、和玩家之間有牆 → 換位置，試 10 次都不行就這次不生成。
- 淡入淡出：遊戲執行中把**這隻菸怪的材質複製一份、暫時切成透明**，調 `_BaseColor`（或 `_Color`、`_TintColor`）的透明度；淡入完成**換回 B 原本的材質**。B 的 Prefab、材質檔不會被修改。⚠️ B 要換菸怪的初始外觀（例如「打不掉」的材質）請在 **Awake／OnEnable** 換，不要在 Start 換（淡入開始時會記下當時的材質，淡入結束換回去）。
- 補生成的菸怪就是一般的 B 菸怪（`SmokeMonster.All` 也看得到）；接下任務後淡出刪除，**不會被打倒，不加淨化**（B12 的 `IsIndestructible` 檢查完成後才成立）。
- Scene 視窗 Gizmos：灰色 = 實際半徑（無法消除時偏紅）；選取時橘色 = 實際半徑、淡藍色 = 基本半徑（兩者不同時才畫）。

## 12. `PlayerEffects` `Scripts/Player/PlayerEffects.cs`（擁有者：A，A13 完成；掛在 Player 上）

| 成員 | 說明 |
|---|---|
| `ItemUseResult TryUseItem(ItemType type)` | **C 的背包按右鍵時呼叫**。回傳 `Consumed` 時背包減 1；其他結果不要扣（噴霧回傳 `UsedNotConsumed`） |
| `float SprayCooldown { get; }` / `float SprayCooldownRemaining { get; }` | C 的背包顯示噴霧冷卻（剩餘 ÷ 總秒數） |
| 🆕A13 `bool IsPhase3ItemAvailable { get; }` | 貼片、口嚼錠現在能不能用（= 第三階段）。C 可以把圖示變灰 |
| 🆕A13 `float GumClearRadius { get; }` | 口嚼錠清除半徑 |
| `event Action<ItemType> OnItemUsed` | 道具成功使用（B 特效、音效） |
| 🆕A13 `event Action<ItemType, ItemUseResult, string> OnItemUseRejected` | 沒用成功（不消耗）。參數：道具、結果、原因文字。給音效、背包抖動用；**畫面文字不用 C 顯示**（見下表） |
| `event Action<Vector3, Vector3, bool> OnSprayFired` | 參數：起點（玩家胸口）、終點（打中的點或最遠距離）、有沒有打到 BOSS → **B 播噴霧特效** |
| 🆕A13 `event Action<Vector3, float, int> OnGumUsed` | 參數：中心（玩家腳底）、半徑、清掉幾個 |
| ~~`ApplyItem`、`OnEffectStarted`、`OnEffectEnded`~~ | ✅A13 已刪除 |

| 道具 | 效果 | 限制 |
|---|---|---|
| 尼古丁貼片 | `PlayerHealth.Heal(30)`（不超過上限） | 只能在第三階段；一次；**滿血時不能用**（不消耗） |
| 尼古丁口嚼錠 | `ClearableSmoke.ClearInRadius(玩家位置, 15)`：清除 15 公尺內（3D 距離）所有 BOSS 煙霧區（A14 起**不清菸圈**） | 只能在第三階段；一次 |
| 尼古丁戒菸噴霧 | 從**鏡頭中央**射線瞬間判定（最遠 30 公尺【暫定】）；最先碰到 `IBossDamageable` → `TakeDamage(7.5, Spray)` | 無次數限制；冷卻 1.5 秒；不限階段（拿到時已經是第三階段） |

- 噴霧射線：**穿過**玩家自己、不是 BOSS 的 Trigger、小菸怪（`IHittable`）；碰到其他實心物件（牆）就停下。
- 第三階段 = `PhaseManager.CurrentPhase == Phase3`。
- 使用結果與提示：

| 情況 | 回傳 | 畫面提示 | 背包 |
|---|---|---|---|
| 操作被鎖（教學、對話、暫停）、「暫停計時」中、遊戲結束 | `Locked` | 無 | 不扣 |
| 第三階段前用貼片／口嚼錠 | `NotYet` | **PlayerEffects 自己** `TaskHint.ShowToast("第三階段才能使用")`（C 不用另外顯示） | 不扣 |
| 噴霧冷卻中 | `OnCooldown` | 無（背包 UI 顯示冷卻） | 不扣 |
| 滿血時用貼片 | `Failed` | **PlayerEffects 自己** `TaskHint.ShowToast("HP 已經是滿的")` | 不扣 |
| 其他失敗（例如找不到攝影機） | `Failed` | 無 | 不扣 |
| 貼片、口嚼錠成功 | `Consumed` | — | **減 1** |
| 噴霧成功（有沒有打中都算） | `UsedNotConsumed` | — | 不扣 |

- 所有數值都是 Inspector 參數：回血量、口嚼錠半徑、噴霧傷害、冷卻、射程、提示文字。

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

## 14. 🆕 BOSS `Scripts/Boss/`（擁有者：A，A14 地面版完成、A16 天上與最終局面；外觀、特效由 B 依事件播放）

### 🆕A14 Prefab 結構與擁有者（`Assets/_Project/Prefabs/Boss/`）
```
Boss.prefab（A）              BossController、BossHealth、BossAttack、SmokeSource（半徑 7）、CapsuleCollider（高 4、半徑 1，實心）
 ├ RingMuzzle（A）            菸圈發射點（離地 2 公尺）
 └ BossVisual.prefab（B）     巢狀 Prefab：模型、動畫、粒子（暫時是灰色膠囊，沒有碰撞體）
SmokeRing.prefab（A）         BossSmokeRing（沒有碰撞體，用程式判定命中）
 └ SmokeRingVisual.prefab（B）巢狀 Prefab：菸圈外觀（暫時是扁球，直徑 1.6 = 命中半徑 0.8；+Z 是飛行方向）
```
- 建立方式：A 的選單 `Tools/Smokee/A - A14 建立 BOSS Prefab`。**B 的 Visual Prefab 已經存在時不會被覆蓋**。
- **B 換外觀：只打開 `BossVisual.prefab`、`SmokeRingVisual.prefab`**，刪掉暫時的膠囊／球，放模型、Animator、粒子。不要改 `Boss.prefab`、`SmokeRing.prefab`；Visual 裡不要放碰撞體以外的邏輯，也不要放會擋住玩家的碰撞體（BOSS 的碰撞體在根物件）。
- **BOSS 的移動（天上高度、降落）與縮放由 A 的 BossController 控制根物件**；B 只做模型、動畫、粒子，依事件播放。
- BOSS 在 GameScene 的位置由 B13 決定（B 把 `Boss.prefab` 拖進 GameScene，並把地面位置的空物件拖進 BossController 的 `_groundPoint`；留空 = 原地）。**場景裡只能有一個 BOSS。**
- BOSS 戰開始前 BOSS 一直在場景裡、看得到（A14 不隱藏）；要不要在登場前藏起來由 B 的登場演出決定。

### `BossController`
| 成員 | 說明 |
|---|---|
| `static BossController Instance` | |
| `BossState State { get; }` | Inactive → Intro → Ground／Sky → Final → Defeated |
| `event Action<BossState> OnStateChanged` | **B 播起飛、降落、最終局面、倒下的動畫** |
| `int SizeLevel { get; }` | 0–3，血量每降 25% 加一級（飛得更低、體型更小）（A16） |
| `event Action<int> OnSizeLevelChanged` | B 換縮小特效（A16） |
| `float StateTimeRemaining { get; }` | 這個狀態還剩幾秒切換（B 可做預告特效）。A14 地面不切換，固定 0 |
| `void BeginIntro()` | 【只給 PhaseManager】進入第三階段時：Inactive → Intro |
| `void StartFight()` | 【只給 PhaseManager】規則視窗關掉後：移到 `_groundPoint`（有設定的話）、開始冒煙、任務提示「捏熄吸菸者的菸，打倒大煙怪！」→ Ground。只在 Intro 有效 |

- **Inactive、Intro：不攻擊、不受傷、不冒煙**（SmokeSource.IsEmitting = false）。
- A14：開打直接進入 Ground，**一直停在地面**。【A16】開打先在天上；地面 8 秒、天上 12 秒輪流。
- 在地面時根物件**水平轉向玩家**（Inspector `_turnSpeed`，預設每秒 360 度）；開打前、被打倒後、暫停中不轉。B 的模型跟著根物件轉，模型的正面請朝 +Z。
- 最後一位吸菸 NPC 被捏熄：BOSS 立刻降到地面 → `Final`（永久在地面、不回血、不吐菸圈、改物理攻擊；自己的 7 公尺煙霧保留）（A16）。
- **打倒時（BossHealth 歸零）的順序**：`BossAttack.StopAndClearAll()`（停止攻擊、移除場上菸圈，不觸發口嚼錠特效）→ 停止冒煙 → 清掉 `Boss` 任務提示 → `OnStateChanged(Defeated)` → `GameManager.NotifyBossDefeated()`。
- 遊戲暫停（Esc）、「暫停計時」中：BOSS 的狀態計時、吐菸圈、菸圈飛行全部停止（菸圈停在空中）。
- 收到 `GameManager.OnPhase3Retry` 自己重設成 `Inactive`、滿血（A19）。

### `BossHealth`（實作 `IHittable`、🆕A14 `IBossDamageable`）
| 成員 | 說明 |
|---|---|
| `static BossHealth Instance` | |
| `float CurrentHp { get; }` / `float MaxHp { get; }` | 預設 150 |
| `bool IsDefeated { get; }` | |
| `event Action<float, float> OnBossHpChanged` | 參數：目前、最大 → **C 的 BOSS 血條**（受傷、回血都會觸發；淨化進度不跟血量連動） |
| `event Action<float, DamageSource> OnBossDamaged` | 參數：實際扣掉的血量、來源（`PlayerMelee` 近戰／`Spray` 噴霧／`Dev`）→ B 依來源播不同的受擊特效 |
| `event Action OnBossDefeated` | 血量歸零時一次（C 血條隱藏；後續由 BossController 處理） |
| `void TakeDamage(float amount, DamageSource source)` | 噴霧 `(7.5, Spray)`（數值在 PlayerEffects）、開發者工具 |
| `TakeHit(int)` | 近戰：每下 Inspector `_meleeDamage`（預設 3），來源 `PlayerMelee`；BOSS 在天上時無效 |
| 🆕A14 `void Heal(float amount)` | 回血（不超過上限）。A16 的自動回血使用 |

- **不受傷的時機**：BOSS 狀態是 Inactive／Intro／Defeated、遊戲不是 Playing、「暫停計時」中。
- 回血：地面 = 抽菸人數 × 1／秒；天上 = 1 + 抽菸人數 × 1／秒；Final 不回血（A16）。

### `BossAttack`
| 成員 | 說明 |
|---|---|
| `event Action<Vector3, Vector3> OnSmokeRingFired` | 參數：起點、方向（已正規化）。**每吐一個菸圈觸發**（地面；A16 起天上也是）→ B 特效、音效 |
| 🆕A14 `int ActiveRingCount { get; }` | 場上還在飛的菸圈數量（開發者工具用） |
| 🆕A14 `float SmokeRingCooldownRemaining { get; }` | 距離下一個菸圈幾秒（開發者工具用） |
| 🆕A14 `void StopAndClearAll()` | 【只給 BossController】打倒時移除場上所有菸圈 |
| ⚠️A16 刪除 `event Action<Vector3, float> OnCannonFired` / `event Action<Vector3> OnCannonLanded` | **A14 決定菸炮取消**（改成天上也吐菸圈）。A16 改成煙霧區事件，**B 不要接這兩個** |
| `event Action OnMeleeWindup` / `event Action<bool> OnMeleeSwing` | 物理攻擊：預備動作開始、揮下（參數：有沒有打中）（A16） |
| `int ClearSmokeInRadius(Vector3 center, float radius)` | A13 起等於 `ClearableSmoke.ClearInRadius`（保留相容） |

- 小菸圈（A14）：BOSS 在地面時，進入地面 2 秒後吐第一個，之後每 2 秒一個。發射點 `RingMuzzle`，**朝「發射當下」玩家胸口（腳底往上 1 公尺）直線飛，不追蹤**。
- 🆕A14 **菸圈和菸炮是同一種東西**：A16 起 BOSS 在天上也吐同一種菸圈（同樣 8 HP、每 2 秒，從天上的高度斜斜往下飛）；**天上吐的菸圈落到地面時留下 3 秒煙霧區**（`BossSmokeZone`，是煙霧來源、可被口嚼錠清除）。
- 菸圈、煙霧區是 A 的小腳本（`BossSmokeRing`、A16 的 `BossSmokeZone`，同資料夾）＋ Prefab。**B 只換 Visual Prefab**。
- **只有煙霧區實作 `IClearableSmoke`**（A16）；A14 決定菸圈不實作（口嚼錠不能清除菸圈）。

### 🆕A14 `BossSmokeRing` `Scripts/Boss/BossSmokeRing.cs`（擁有者：A；**不**實作 `IClearableSmoke`）
| 成員 | 說明 |
|---|---|
| `void Launch(Vector3 direction, float speed, float damage, float lifetime, float hitRadius, Transform owner, LayerMask obstacleMask, bool leaveZoneOnGround, Action<BossSmokeRing> onEnded)` | 【只給 BossAttack】發射 |
| `void Despawn()` | 【只給 BossAttack】直接移除（BOSS 被打倒） |

- 命中玩家（命中半徑 0.8 內碰到玩家的 CharacterController）→ `PlayerHealth.TakeDamage(8, DamageSource.SmokeRing)` → 消失。
- 撞到牆、地面等實心物件（Trigger、BOSS 自己、玩家不算）→ 消失；超過存在時間（4 秒）→ 消失。吸菸 NPC 等實心物件也會擋住菸圈。
- **菸圈本身不是煙霧來源**（不增加暴露值），**口嚼錠也不能清除菸圈**。
- 不使用物件池（場上最多約 2 個，直接生成、刪除）。

### 🆕A13 口嚼錠清除：`IClearableSmoke` ＋ `ClearableSmoke`（`Scripts/Boss/`，擁有者：A）
```csharp
public interface IClearableSmoke {
    Vector3 SmokePosition { get; }   // 判斷在不在範圍內
    void    ClearSmoke();            // 被清除：自己消失（Destroy 或停用）
}
public static class ClearableSmoke {
    public static IReadOnlyList<IClearableSmoke> All { get; }
    public static event Action<Vector3, float> OnCleared;   // 參數：中心、半徑 → B 的清除特效（範圍內沒東西也會觸發）
    public static void Register(IClearableSmoke smoke);     // 出生時（OnEnable）
    public static void Unregister(IClearableSmoke smoke);   // 消失時（OnDisable）
    public static int  ClearInRadius(Vector3 center, float radius); // 【只給 PlayerEffects、BossAttack】回傳清掉幾個
}
```
- 實作者：A16 的 `BossSmokeZone`、DevTools 的 `FakeSmokeRing`（模擬煙霧區，測試口嚼錠半徑用，保留）。**A14 決定 `BossSmokeRing` 不實作**（口嚼錠不能清除菸圈）。

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
| `CanInteract` | 正在抽菸、BOSS 戰中 |
| 🆕A13 `CanInteractFrom(Transform player)` | **實作 `IInteractCondition`**（A16）：玩家面向它（前方 90 度）且 2 公尺內 |
| 🆕A13 `IsQteActive` | **實作 `IQteInteractable`**（A16）：`ActiveQte == this` |
| `Interact()` | 開始 QTE（這一下算第 1 下） |
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
- E 鍵由 PlayerInteractor 讀，透過 `IQteInteractable.RegisterQtePress()` 交給正在 QTE 的 NPC（第 2 節）。**C 只負責顯示，不讀 E。**
- QTE 期間鎖移動由 PlayerInteractor 負責（key `"Qte"`），SmokerNPC 不用自己鎖。

## 16. `ScoreCalculator` `Scripts/Core/ScoreCalculator.cs`（擁有者：A，A11 用時分、A18 頭銜）

```csharp
public static class ScoreCalculator {
    // 🆕
    public static ScoreResult Calculate(bool isWin, float hp, float maxHp,
                                        float elapsedSeconds, float smokeSeconds);
    // （A11 已刪除舊版 6 個參數的 Calculate）
}
```
- 總分 = 血量分 × 0.6 + 用時分 × 0.4。
- 用時分（A11 決定）：≤ 480 秒 100；超過的部分**依秒數比例扣**：100 − 超過秒數 ÷ 30 × 10，**四捨五入到整數（剛好 .5 一律進位）**，最低 0；失敗局 0。
  例：8:01 → 100、8:05 → 98、8:15 → 95、8:16.5 → 95、8:29 → 90、10:00 → 60、12:58 → 1、12:59 → 0。
- 測試：Unity 選單 `Tools/Dev/Run Score Checks`（14 個案例）。

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
- 🆕A15 B12 拒絕幫忙要配合的事（**暴露值、補生成、計時、恢復都在 A，B 不用寫計時**）：
  1. `TakeHit` 一開始若 `SmokeSource.IsIndestructible` 為 true → 播「打不動」回饋（例如彈開、灰色閃一下），**不算次數**、直接 return
  2. 監聽 `SmokeSource.OnRefusalStateChanged` 換「打不掉」外觀；出生時（**Awake／OnEnable**，不要在 Start）讀 `SmokeSource.RefusalActive` 決定初始外觀；OnDisable 取消監聽
  3. `AffectedByRefusal` 由 SmokeSource 自動設定（B 在 Awake 設 true 也可以，不衝突）
  4. 跟隨 NPC：監聽 `PhaseManager.OnRefusalNpcLine` 說台詞，講完呼叫 `NotifyRefusalNpcLineFinished()`
  5. GameScene：GameManager 物件加 `RefusalMonsterSpawner`、拖入 `SmokeMonster_Small.prefab`
  6. 煙霧半徑不擴大，**不用**做粒子縮放（`OnRadiusChanged` 留著，之後有需要再接）
- 行為：出生點半徑 3 公尺內遊走、每秒 1 公尺，不追擊、不攻擊。

### `SmokeMonsterSpawner`（擁有者：B，B10）
- 監聽 `PhaseManager.OnPhaseChanged`（Phase1 → 生成 5 隻）、`OnEventGapStarted`（每次 2 隻）；故事地點的 3 隻由 Director 生成。
- 生成時設定 `Group` 與 `PurificationUnits = PhaseManager.Instance.GetMonsterUnits(group, index)`。

### `OpeningSequence`（擁有者：B，B7）
- 接受路線：隨機一位路人（老奶奶／小朋友／爸爸）→ 咳嗽 → 台詞（`DialogueUI`）→ 動手 → 暈倒 → `NotifyOpeningFinished()`。
- 拒絕路線：朋友點菸 → 車庫門開好 → `NotifyOpeningFinished()`。OnGUI 的「新手引導」文字移除，改由 TutorialManager 設任務提示。（**A11 已代改 OpeningSequence.cs 這兩處**，經 A 同意，B 請確認）

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
- 🆕A14 **B 只改 `Prefabs/Boss/BossVisual.prefab`、`SmokeRingVisual.prefab`**（第 14 節 Prefab 結構）；B13 把 `Boss.prefab` 放進 GameScene、設定 `_groundPoint`。
- 監聽 `PhaseManager.OnBossIntroRequested` → 播登場（BOSS 衝上天、吸菸者從吸菸室散開）→ `PhaseManager.NotifyBossIntroFinished()`。重新挑戰會再播一次。
- 監聽 `BossController.OnStateChanged`、`OnSizeLevelChanged`、`BossHealth.OnBossDamaged`（依 `DamageSource` 分近戰／噴霧）、`BossAttack.OnSmokeRingFired` → 播動畫、特效。**菸炮已取消**，不要接 `OnCannonFired`／`OnCannonLanded`。
- 監聽 `PhaseManager.OnEndingRequested` → BOSS 遺言（`DialogueUI`）→ 綠色城市 → `PhaseManager.NotifyEndingFinished()`。⚠️A14 **一監聽就一定要在播完時呼叫 `NotifyEndingFinished()`**，不然遊戲會停在結局、不會結算（沒有人監聽時 PhaseManager 會直接結算）。
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
- 滾輪切換、右鍵使用（C 讀；**兩個都要**先檢查 `PlayerController.IsActionAllowed(PlayerAction.UseItem)`，新手教學期間會被鎖）→ `PlayerEffects.TryUseItem(type)` → 回傳 `Consumed` 才減 1（結果表見第 12 節）。
- 噴霧冷卻：`SprayCooldownRemaining ÷ SprayCooldown`；「第三階段才能使用」A 已自動顯示 Toast，背包不用再顯示（要做音效可聽 `OnItemUseRejected`）。
- 背包只放 3 種道具（`ItemType` A13 起只剩這 3 個）；藏寶圖不放進背包。
- 重新挑戰（**A11 改**）：背包快照改由 A 的 `Phase3Checkpoint` 保存，**C 不用自己記**。C 需要提供 `GetCount(ItemType)` 與 🆕【待 C 確認】`void SetCount(ItemType type, int count)`，見第 22 節。
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
    教學菸怪被打倒 ⇒ OnTutorialFinished → PhaseManager.NotifyTutorialFinished()
[A] PhaseManager：Phase = Phase1 ⇒ OnPhaseChanged(Phase1)（B 生成 5 隻）
    → GameManager：用時歸零、HP 補滿、暴露值與淨化歸零 ⇒ OnRunReset
    TaskHint「消滅所有的煙怪吧！」
[B] 菸怪被打倒 → GameManager.AddPurification(40) ×5 ⇒ OnProgressChanged
[A] PhaseManager：淨化 ≥ 200（整數判斷）⇒ OnPurificationMilestone(20) → Phase = Phase2 ⇒ OnPhaseChanged(Phase2)
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
        [A] SmokeSource.SetRefusalState(true, 1) ⇒ SmokeSource.OnRefusalStateChanged(true)（B：打不掉的外觀）
            ⇒ OnHelpRefusedChanged(true)（B：女孩跟著玩家）
            ⇒ OnRefusalNpcLine(1, 台詞)（B：女孩說話 → NotifyRefusalNpcLineFinished()）→ Toast 警告
            暴露值全場每秒 +2；RefusalMonsterSpawner 每 3 秒在玩家附近淡入 1 隻菸怪（最多 15）
        [B] 玩家對女孩按 E → 再跳 HelpPromptUI（再拒絕 → ReportHelpRefused(1) → 台詞與警告再一次）
    └ 幫忙 → PhaseManager.ReportEventAccepted(1)
        [A] SmokeSource.SetRefusalState(false, 1) ⇒ OnHelpRefusedChanged(false)；補生成的菸怪淡出刪除；暴露值恢復一般規則
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
[A] PhaseManager：教學與三個事件視為完成 → GameManager.LockPurificationForPhase3()（淨化 600，整場 BOSS 戰固定）
    ⇒ OnPurificationMilestone(60)（若還沒達到）
    → Phase3Checkpoint.SaveFirstEntry(快照)（只有第一次進入時存；第 22 節）
    → Phase = Phase3 ⇒ OnPhaseChanged(Phase3)
    → BossController.BeginIntro() ⇒ OnStateChanged(Intro)（不攻擊、不受傷、不冒煙）
    ⇒ OnBossIntroRequested                                        （B 聽）
[B] 登場演出（SetTimerPaused/SetControlLock "Cutscene"、SetCameraOverride）
    → PhaseManager.NotifyBossIntroFinished()
[A] PhaseManager ⇒ OnRulesIntroRequested                          （C 聽）
[C] RulesIntroWindow 顯示 → 玩家點擊 → PhaseManager.NotifyRulesIntroClosed()
[A] PhaseManager ⇒ OnBossFightStarted（C 血條顯示）
    → BossController.StartFight() ⇒ OnStateChanged(Ground)（A14；A16 改成先 Sky）；開始冒煙；（A16）SmokerNPC 全部開始抽菸
```

### c. BOSS 戰 → 最終局面 → 打倒 → 結局 → 結算（含重新挑戰）
```
[A] BossController：Sky 12 秒 ⇄ Ground 8 秒 ⇒ OnStateChanged（B 起飛、降落）
    Ground、Sky：每 2 秒菸圈 ⇒ OnSmokeRingFired（A14 決定菸炮取消，天上也吐菸圈；天上的菸圈落地留下 3 秒煙霧區，A16）
    命中玩家 → PlayerHealth.TakeDamage(8, SmokeRing) ⇒ OnDamaged
    BossHealth 每秒回血（依 SmokerNPC.SmokingCount）⇒ OnBossHpChanged（C 血條；淨化進度不變，停在 60%）
    血量每降 25% ⇒ OnSizeLevelChanged（變低、變小）
[玩家] 近戰（地面）→ BossHealth.TakeHit ⇒ OnBossDamaged
       噴霧 → PlayerEffects.TryUseItem(Spray) ⇒ OnSprayFired（B 特效）→ BossHealth.TakeDamage(7.5, Spray)
       對 SmokerNPC 按 E → QTE ⇒ SmokerNPC.OnQteStarted（C 進度條）
         成功 ⇒ OnQteEnded(true) ⇒ OnAnySnuffed ⇒ OnSmokingCountChanged
[A] SmokingCount = 0 → BossController 立刻降落 ⇒ OnStateChanged(Final)
    不回血、不吐煙；玩家 3 公尺內每 3 秒物理攻擊 ⇒ OnMeleeWindup ⇒ OnMeleeSwing
    TaskHint「打倒大煙怪！」
[A] BossHealth 歸零 ⇒ OnBossDefeated（C 血條隱藏）
    → BossController：停止攻擊、清掉場上菸圈、停止冒煙 ⇒ OnStateChanged(Defeated)
    → GameManager.NotifyBossDefeated()：淨化 = 1000、停止用時（暫停計時 key "BossDefeated"）、記下 HP
    PhaseManager ⇒ OnPurificationMilestone(100) → Phase = Ending ⇒ OnPhaseChanged(Ending) ⇒ OnEndingRequested（B 聽）
[B] 結局演出（BOSS 遺言、綠色城市）→ PhaseManager.NotifyEndingFinished()
    （沒有人監聽 OnEndingRequested 時，PhaseManager 直接進下一行）
[A] → GameManager.FinishWithVictory() ⇒ OnGameEnded(result)（C 結算畫面）

失敗：教學以外的任何時候 HP 歸零 ⇒ PlayerHealth.OnDied → GameManager 結算 ⇒ OnGameEnded
    （教學中 HP 歸零 → TutorialManager 補滿重來 ⇒ OnTutorialRestarted，不結算）
    第三階段失敗 → result.CanRetryFromPhase3 = true
[C] 按「從第三階段重新挑戰」→ GameManager.RetryFromPhase3()
[A]（A19）記下用時 → 重新載入 GameScene → 直接從第三階段起點開始（第 22 節）
    HP、背包還原成第一次的快照、用時接續 ⇒ OnPhase3Retry ⇒ OnBossIntroRequested（重播登場）⇒ … ⇒ OnRulesIntroRequested（重播規則視窗）
```

---

## 19. 任務提示文字的來源（`TaskHint`）

| 時機 | 誰設 | source | 文字 |
|---|---|---|---|
| 教學 4 步驟 | A TutorialManager | `Tutorial` | 見第 5 節 |
| 第一階段 | A PhaseManager | `Phase` | 消滅所有的煙怪吧！ |
| 第一 → 第二階段 | A PhaseManager | `Phase` | 城市中還有很多需要幫助的人，快去看看吧！ |
| 事件中各步驟 | B Director | `StoryEvent` | `StoryEventData` 的 FollowHint／ClearHint／InteractHint |
| 拒絕幫忙中 | — | — | 🆕A15 **刪除**（沒有任務提示） |
| 拒絕的警告（NPC 講完台詞後） | A PhaseManager | Toast | 因為無情地拒絕幫忙，城市煙霧瀰漫……再這樣下去會有生命危險！ |
| 拿到藏寶圖 | A PhaseManager | `Phase` | 到吸菸室旁的寶箱看看 |
| BOSS 戰 | A BossController | `Boss` | 捏熄吸菸者的菸，打倒大煙怪！／（Final）打倒大煙怪！ |
| 短暫提示 | A PlayerEffects／TreasureChest | Toast | 第三階段才能使用／路上好像還有還沒消除的煙怪 |

---

## 20. 舊介面處理表

| 舊介面 | 處理 | 誰、哪個任務 |
|---|---|---|
| `GameManager.RoundDuration`、`OnFinalMinute`、`StartRound()`、`DevSetRemainingTime()` | ✅ A11 已刪除 | — |
| `GameManager.RemainingTime` | ⚠️Obsolete（C 的 `HUD.cs` 還在用；A11 起暫時回傳用時） | C10 改讀 `ElapsedTime` 後，A 在 I3 刪除 |
| `GameManager.AddPurificationProgress(float)` | ⚠️Obsolete，暫時轉呼叫 | B10 改用 `AddPurification(int)` 後，A 在 I3 刪除 |
| `ScoreCalculator.Calculate`（6 個參數）、`ScoreResult.RemainingSeconds` | ✅ A11 已刪除 | — |
| `RouteManager.NotifyPlayerLeftAlley()`、`AlleyExitTrigger` | ⚠️Obsolete（A11 起不做任何事；GameScene 還掛著元件） | B 從 GameScene 移除元件；A 在 I3 刪檔 |
| `GameManager` Inspector `autoStartRound` | ✅ A11 改成 `devAutoStart`（Off／Tutorial／Phase1） | — |
| `MonsterSize.Large` | ⚠️Obsolete | B10 停用大型菸怪 Prefab 後，A 在 I3 刪除 |
| `ItemType.NicotineInhaler`、`Champix`、`Bupropion` | ✅A13 已刪除（沒有人在用；其他道具編號不變） | — |
| `PlayerAttack.HitCount` | ✅A13 已刪除 | — |
| `ExposureSystem.RiseMultiplier`、`IsVisualSuppressed` | ✅A13 已刪除（`VisualExposure` 保留，永遠等於 `Exposure`） | — |
| `PlayerEffects.ApplyItem`、`OnEffectStarted`、`OnEffectEnded` | ✅A13 已刪除 | — |
| `BossAttack.OnSmokeCleared` | ✅A13 已刪除，改用 `ClearableSmoke.OnCleared` | — |
| 菸炮（`BossAttack.OnCannonFired`、`OnCannonLanded`、Inspector 菸炮欄位、`BossCannonShell`） | A14 決定取消（天上也吐菸圈）；事件還在但不會觸發 | A16 刪除，改成煙霧區事件 |
| DevTools `FakeBoss` | A14 改用真的 BOSS；A14 選單已從 TestScene_A 移除物件 | A 在 Unity Project 視窗刪除 `FakeBoss.cs` |
| `PlayerController.SetControlEnabled` | 保留（等於 key "Default"） | — |
| `SmokeMonster.ProgressValue` | B 的檔案，A 不標記 | B10 改成 `PurificationUnits` |
| `EscortNPC`、`SafeZone`、`GuideLine` | 刪除 | B7（在 Unity Project 視窗） |
| `Assets/_Project/UI/InGameMenu.cs` | 刪除（暫停選單改由 C 做） | B7（在 Unity Project 視窗） |
| `ChestController`、`QuizManager`、`QuizData` | 刪除 | C7（在 Unity Project 視窗） |
| `OpeningSequence` 的 OnGUI「新手引導」文字 | 移除，改用 TaskHint | B7 |
| `DevOverlay`、`DevHotkeys` 的倒數功能 | ✅ A11 改成用時與模擬按鍵；一開場跳到第三階段在 F4 | F4 |
| `Editor/ScoreCheckTools.cs` | ✅ A11 改用用時 | — |
| 第三階段淨化 = 60% + 40% ×（1 − BOSS 血量比例） | ✅ A11 取消（不連動，見第 3 節） | — |

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
| 拒絕：暴露值全場每秒上升 | 2 | ExposureSystem（`_refusalRiseRate`） |
| 拒絕：補生成間隔／上限／距離／淡入／淡出 | 3 秒／15 隻／2–10 公尺／0.8／0.8 秒 | RefusalMonsterSpawner |
| 拒絕：NPC 台詞／警告文字／警告秒數／沒人接台詞時台詞顯示秒數 | 見第 4 節／4／3 | PhaseManager |
| 教學：走到目標判定半徑／靠近菸怪半徑 | 1.5／3【暫定】 | TutorialManager |
| 菸怪：受擊次數／遊走半徑／速度 | 3／3／1 | SmokeMonster（B） |
| 近戰：距離／角度／冷卻 | 2／90／0.5 | PlayerAttack |
| 貼片回血 | 30 | PlayerEffects |
| 口嚼錠半徑 | 15 | PlayerEffects |
| 噴霧傷害／冷卻／最遠距離 | 7.5／1.5／30【暫定】 | PlayerEffects |
| BOSS 最大血量／近戰受傷 | 150／3 | BossHealth（噴霧 7.5 在 PlayerEffects） |
| 回血：地面每人／天上基本／天上每人 | 1／1／1 | BossHealth |
| 地面停留／天上停留 | 8／12 | BossController |
| 天上高度（4 級）／體型（4 級） | 12、10、8、6 公尺【暫定】／1、0.85、0.7、0.55【暫定】 | BossController |
| 菸圈（地面、天上共用）：間隔／傷害／速度／存在時間／命中半徑／瞄準高度 | 2／8／6／4／0.8／1【後四個暫定】 | BossAttack |
| 天上菸圈落地的煙霧區：時間／半徑 | 3／3【半徑暫定】 | BossAttack（A16） |
| 物理攻擊：距離／間隔／傷害／預備時間 | 3／3／10／0.6【暫定】 | BossAttack |
| QTE：距離／角度／次數／時間 | 2／90／5／2 | SmokerNPC |
| 用時分：滿分時間／每 30 秒扣 10（依秒數比例、四捨五入到整數） | 480／30／10 | ScoreCalculator（static，用常數；A20 再決定要不要搬到 Inspector） |
| 評分權重 | 0.6／0.4 | ScoreCalculator（常數） |

---

## 22. 🆕A11 第三階段起點 `Scripts/Core/Phase3Checkpoint.cs`（擁有者：A；A11 快照存取，A19 還原與重新載入，F4 開發者一開場就跳）

「從第三階段重新挑戰」（A19）和「開發者跳到第三階段」（F4）共用**同一個**起點流程。

### 規則（GDD 10.3、DECISIONS）
| 項目 | 重新挑戰時 |
|---|---|
| HP、道具 | 還原成「**第一次**」進入第三階段時的快照；之後再重試也一樣用第一次的 |
| 用時 | **持續累計**，接著失敗那一刻繼續算 |
| 結算的血量分 | 以最後一次成功打倒 BOSS 時的 HP 計算 |
| 淨化進度 | 從 60% 開始；新手教學與三個事件都視為已完成 |
| 開發者一開場就跳到第三階段（沒有真正玩過） | 使用**預設快照**：HP 100，尼古丁貼片 1、尼古丁口嚼錠 1、尼古丁戒菸噴霧 1 |
| 開發者在第一、二階段中跳過去（A11 的按鍵 2） | 和正常進入一樣，用當下真實的 HP；在場景內切換，不重新載入 |

### (a) 快照資料
```csharp
[Serializable] public struct ItemStack { public ItemType Type; public int Count; }

[Serializable] public class Phase3Snapshot {
    public float Hp;                 // 第一次進入第三階段時的 HP
    public List<ItemStack> Items;    // 背包內容
    public bool  ItemsCaptured;      // 道具有沒有真的記下來（C 的 Inventory 提供方法前是 false，還原時不要動背包）
    public float ElapsedAtEntry;     // 第一次進入時的用時（參考用）
    public bool  IsDefault;          // 開發者預設快照
}
```

### (b) 放在哪裡、什麼時候清除
```csharp
public static class Phase3Checkpoint {
    public static bool HasSnapshot { get; }
    public static Phase3Snapshot Snapshot { get; }        // 回傳複製品；沒有時 null
    public static bool  IsRetryPending { get; }           // 下一次載入 GameScene 要直接從第三階段起點開始（A19）
    public static float CarriedElapsedTime { get; }       // 重新挑戰要接續的用時（A19）
    public static int   RetryCount { get; }               // 重新挑戰次數（A19）
    public static bool  SaveFirstEntry(Phase3Snapshot s); // 【只給 PhaseManager】已經有快照就不覆蓋，回傳 false
    public static Phase3Snapshot CreateDefault();         // 預設快照
    public static void  RequestRetry(float elapsed);      // 【A19】目前只印 Log
    public static void  ConsumePendingRetry();            // 【A19】目前只印 Log
    public static void  Clear();
}
```
- static 類別，**重新載入場景後資料還在**（不像 TaskHint 會在載入場景時清空）。
- 清除時機：`GameManager.RestartToOpening()`、`ReturnToMainMenu()`、每次按 Play。
- 存快照的時機：`PhaseManager` 進入第三階段時（`EnterPhase3`、開發者按鍵 2）。

### (c) 起點流程：重新載入 GameScene（A19 實作）
1. C 的結算畫面按「從第三階段重新挑戰」→ `GameManager.RetryFromPhase3()`
2. `Phase3Checkpoint.RequestRetry(目前用時)` → 重新載入 GameScene
3. 場景載入後，PhaseManager **等一個 frame**（讓所有物件的 Start 都跑完、監聽都接好）
4. 跳過開場、教學、第一、二階段：GameManager 進入 Playing、用時 = `CarriedElapsedTime`、HP = 快照、暴露值 0、淨化 600 並鎖住
5. ⇒ `GameManager.OnPhase3Retry`（**意思：已經從第三階段起點開始了**。C 還原背包；B 把玩家放到第三階段起點）
6. → `Phase = Phase3` ⇒ `OnBossIntroRequested`（重播登場）→ 規則視窗 → BOSS 戰

選重新載入的原因：場景內要重設的東西太多（BOSS、吸菸 NPC、菸圈、故事 NPC、菸怪、寶箱、特效、UI），容易漏；重新載入後每個物件都是全新的。缺點是開場相關的系統要判斷「這次要跳過」。

### (d) 需要 B、C 配合的介面【待組員確認，確認前不要實作】
| 誰 | 要提供什麼 | 用途 |
|---|---|---|
| C `Inventory` | `int GetCount(ItemType type)`（第 17 節已有）| A 存快照時讀貼片、口嚼錠、噴霧的數量 |
| C `Inventory` | 🆕 `void SetCount(ItemType type, int count)` | 收到 `OnPhase3Retry` 時，A 依快照設定背包（或由 C 自己讀 `Phase3Checkpoint.Snapshot` 設定，二選一） |
| C 開場對話框（C2OpeningDialog） | `Phase3Checkpoint.IsRetryPending` 為 true 時不要顯示 | 重新挑戰不經過開場 |
| B | 第三階段玩家起始位置：A 寫一個標記元件 🆕 `Phase3StartPoint`（只有位置，沒有邏輯），**B 在 GameScene 放一個空物件掛上去** | 重新挑戰、開發者跳關時把玩家放到這裡 |
| B `OpeningSequence` | `IsRetryPending` 為 true 時：車庫門直接是開的、朋友不點菸、不播開場 | 重新挑戰不經過開場 |
| B 故事 NPC、菸怪生成 | 重新挑戰時不會收到 Phase1、Phase2 的事件，應該本來就不會出現，**請 B 確認** | |
