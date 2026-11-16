# INTERFACES.md — 共用介面規格

> 三人（以及三種 AI 工具）都必須照這份的名稱與參數寫程式。
> **要改這份文件，先在群組講，由 A 更新並合併進 develop。**
> C 使用 ChatGPT 時，每次請把這份文件（或相關段落）貼給 ChatGPT。
>
> 所有類別都**不使用 namespace**。事件一律是 C# `event Action<...>`。
> 「擁有者」= 唯一可以修改這支檔案的人。

---

## 0. 共用型別 `Scripts/Core/GameTypes.cs`（擁有者：A）

```csharp
public enum GameState    { Opening, Playing, Paused, Ended }
public enum OpeningRoute { None, AcceptSmoke, RefuseSmoke }   // 路線 A = AcceptSmoke，路線 B = RefuseSmoke
public enum MonsterSize  { Small, Large }
public enum ItemType     { NicotineGum, NicotinePatch, NicotineInhaler, NicotineSpray, Champix, Bupropion }

/// 結算結果，由 ScoreCalculator 產生，ResultUI（C）讀取
public struct ScoreResult {
    public bool   IsWin;
    public float  RemainingHp;        // 結算時 HP
    public float  ClearTimeSeconds;   // 實際花費秒數
    public float  RemainingSeconds;
    public float  SmokeSeconds;       // 待在煙霧範圍內的總秒數（「吸入二手菸量」）
    public float  HpScore;            // 0–100
    public float  TimeScore;          // 0–100
    public float  TotalScore;         // 0–100
    public string Grade;              // "S" "A" "B" "C" "D"
}
```

## 1. 介面 `IHittable` `Scripts/Combat/IHittable.cs`（擁有者：A）

```csharp
public interface IHittable {
    /// 被攻擊命中。hitCount 通常是 1；吸入劑效果時是 3。
    void TakeHit(int hitCount);
}
```
- **B 的 SmokeMonster 必須實作這個介面**，A 的 PlayerAttack 只認這個介面。
- **實作這個介面的物件（或它的子物件）一定要有 Collider**（可以是 Trigger），否則 PlayerAttack 找不到它。

### `PlayerAttack` `Scripts/Combat/PlayerAttack.cs`（擁有者：A，掛在 Player 上）

| 成員 | 說明 |
|---|---|
| `IHittable CurrentTarget { get; }` | 現在按左鍵會打到的目標（每幀更新），沒有時為 null |
| `event Action<IHittable> OnTargetChanged` | 「現在會打到的目標」改變時，null = 沒有目標 |
| `int HitCount { get; set; }` | 每次命中傳給 `TakeHit` 的次數，預設 1、最小 1。**吸入劑效果期間改成 3，結束改回 1** |
| `event Action OnAttack` | 每次揮擊（不論有沒有打中）→ B 可接揮擊音效、動畫 |
| `event Action<IHittable, Vector3> OnHit` | 命中時。參數：被打中的目標、命中位置 → B 可接命中音效、特效 |

- 滑鼠左鍵攻擊，只打前方扇形內最近、沒被牆擋住的一個目標。`PlayerController.IsControlEnabled` 為 false 時不能攻擊。

### `TargetMarker` `Scripts/Combat/TargetMarker.cs`（擁有者：A，掛在 Player 上）
- 在攻擊目標（橘紅色）與互動目標（綠色）頭上顯示旋轉、浮動的倒金字塔。
- 外觀可替換：Inspector 的 `Attack Marker Prefab`／`Interact Marker Prefab` 指定 B 做的 Prefab 即可。**這些 Prefab 不可以有 Collider**，否則會擋住攻擊與互動判定。

## 2. 介面 `IInteractable` `Scripts/Interaction/IInteractable.cs`（擁有者：A）

```csharp
public interface IInteractable {
    string    InteractPrompt { get; }   // HUD 顯示的提示，例如「按 E 開啟寶箱」
    bool      CanInteract    { get; }   // 已開過的寶箱回傳 false
    Transform InteractPoint  { get; }   // 用來計算距離
    void      Interact();               // 玩家按 E 時呼叫
}
```
- **C 的 ChestController、B 的 EscortNPC 實作這個介面。**
- 只有 A 的 `PlayerInteractor` 會讀 E 鍵。B、C **不要自己讀 E 鍵**。
- `PlayerInteractor` 事件：`event Action<IInteractable> OnFocusChanged`（null 代表附近沒有可互動物件）→ C 的 HUD 用來顯示／隱藏提示。
- `PlayerInteractor` 屬性：`IInteractable CurrentTarget { get; }`（目前的目標，沒有時為 null）→ HUD 啟動時可以先讀一次。
- **實作這個介面的物件（或它的子物件）一定要有 Collider**（可以是 Trigger），否則 PlayerInteractor 找不到它。
- 中間被牆擋住（玩家胸口到 `InteractPoint` 之間有其他碰撞體）時不能互動；`PlayerController.IsControlEnabled` 為 false 時目標為 null。

---

## 3. `GameManager` `Scripts/Core/GameManager.cs`（擁有者：A）

場景裡只有一個，放在 GameScene。用 `GameManager.Instance` 取得。

| 成員 | 說明 |
|---|---|
| `static GameManager Instance` | |
| `GameState State { get; }` | |
| `float RoundDuration { get; }` | Inspector 可調，預設 420 |
| `float RemainingTime { get; }` | 剩餘秒數 |
| `float PurificationProgress { get; }` | 0–100 |
| `ScoreResult LastResult { get; }` | 遊戲結束後才有值 |
| `void StartRound()` | 開始 420 秒倒數（RouteManager 呼叫） |
| `void AddPurificationProgress(float amount)` | 菸怪被擊敗時呼叫（B 的 SmokeMonster） |
| `void SetPaused(bool paused)` | 暫停／繼續（C 的暫停選單按鈕也可呼叫） |
| `event Action<float> OnProgressChanged` | 參數：目前進度 0–100 |
| `event Action OnRoundStarted` | 倒數開始 |
| `event Action OnFinalMinute` | 剩 60 秒時觸發一次（小地圖顯示菸怪） |
| `event Action<bool> OnPauseChanged` | 參數：是否暫停 |
| `event Action<ScoreResult> OnGameEnded` | 勝利或失敗結算 |

- Esc 鍵由 GameManager 讀取並切換暫停。C 的暫停選單只要監聽 `OnPauseChanged`。
- 暫停使用 `Time.timeScale = 0`。**C 的 UI 動畫請用 `Time.unscaledDeltaTime`。**
- 剩餘時間每幀變動，HUD 直接每幀讀 `RemainingTime` 即可，不另設事件。
- 暫停可在 Opening、Playing 時進入，繼續後回到原本的狀態；`Ended` 時 Esc 無效。`AddPurificationProgress` 只在 `Playing` 時有效。
- **`DevMode`、`DevSetRemainingTime`、`DevForceEnd` 只給 A 的開發者工具（`Scripts/DevTools/`）使用，B、C 不要呼叫。** GameManager 沒勾選 devMode 時它們沒有作用。

## 4. `RouteManager` `Scripts/Core/RouteManager.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `static RouteManager Instance` | |
| `OpeningRoute ChosenRoute { get; }` | |
| `void ChooseRoute(OpeningRoute route)` | **C 的開場對話框按鈕呼叫**。只接受第一次選擇，之後的呼叫忽略 |
| `void NotifyOpeningFinished()` | **B 的 OpeningSequence 演出結束時呼叫**。選過路線後的第一次呼叫才有效 |
| `void NotifyPlayerLeftAlley()` | 玩家走出死巷 → 開始倒數。只在「選了 `RefuseSmoke` 且開場已結束」時有效，只觸發一次。**B 不用自己呼叫，掛 `AlleyExitTrigger` 即可** |
| `event Action<OpeningRoute> OnRouteChosen` | **B 的 OpeningSequence 監聽，播放對應演出** |
| `event Action<OpeningRoute> OnOpeningFinished` | |

流程：
1. 場景開始：GameManager 為 `Opening` 狀態，RouteManager 呼叫 `SetControlEnabled(false)`（讓玩家點對話框）
2. 開場對話框（C）→ `ChooseRoute(...)` → 觸發 `OnRouteChosen`
3. OpeningSequence（B）播放演出 → `NotifyOpeningFinished()`
4. 若是 `AcceptSmoke`：RouteManager 等待一小段時間（Inspector 可調，預設 1 秒）後**重新載入目前場景**，所有狀態回到初始值，玩家重新選擇。**B 不要自己 `LoadScene`**
5. 若是 `RefuseSmoke`：RouteManager 恢復玩家操作；走出死巷 → `AlleyExitTrigger` → `NotifyPlayerLeftAlley()` → `GameManager.StartRound()`

- GameManager 的開發者選項 `autoStartRound` 在場景裡有 RouteManager 時不會生效。

### `AlleyExitTrigger` `Scripts/Core/AlleyExitTrigger.cs`（擁有者：A，B 掛在物件上）

- 掛在死巷出口的 Collider 上（**勾選 Is Trigger**；加元件時會自動勾選）。
- 玩家（身上有 `PlayerController`）穿過時自動呼叫 `RouteManager.Instance.NotifyPlayerLeftAlley()`，不用寫程式。
- 沒有公開成員；玩家來回穿過也沒關係，「只觸發一次」由 RouteManager 判斷。

## 5. `PlayerController` `Scripts/Player/PlayerController.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `bool IsFirstPerson { get; }` | |
| `bool IsControlEnabled { get; }` | 目前能不能操作：沒被 `SetControlEnabled(false)` 關掉，而且不在暫停中 |
| `void SetControlEnabled(bool enabled)` | false = 停止移動與視角，並**解鎖滑鼠游標**；true = 恢復並鎖定游標 |
| `void SetSpeedModifier(string key, float multiplier)` | 例如 `("Gum", 1.5f)`、`("MaxExposure", 0.7f)`；所有倍率相乘 |
| `void RemoveSpeedModifier(string key)` | |
| `event Action<bool> OnViewModeChanged` | 參數：是否第一人稱 |

- **C 開啟問答 UI 時呼叫 `SetControlEnabled(false)`，關閉時 `SetControlEnabled(true)`**，否則滑鼠點不到按鈕。
- 開場期間的玩家控制（`SetControlEnabled` 只記一個開關，**最後一次呼叫的人說了算**，所以照下面分段負責）：
  1. **場景開始**：RouteManager 呼叫 `SetControlEnabled(false)`，讓玩家點開場對話框。
  2. **選完路線到演出結束**：由 B 的 OpeningSequence 決定（例如鐵捲門打開後 `true` 讓玩家走出車庫、播演出時 `false`）。
  3. **路線 B 演出結束**（`NotifyOpeningFinished()` 之後）：RouteManager 呼叫 `SetControlEnabled(true)`，B 不用再呼叫。
  4. **路線 A 演出結束**：RouteManager 等待後重新載入場景，B 不用恢復控制。

## 6. `PlayerHealth` `Scripts/Player/PlayerHealth.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `float CurrentHp { get; }` / `float MaxHp { get; }` | |
| `void Heal(float amount)` | **B 的 EscortNPC 護送成功時呼叫 `Heal(10)`** |
| `void TakeDamage(float amount)` | |
| `event Action<float, float> OnHpChanged` | 參數：目前 HP、最大 HP |
| `event Action OnDied` | HP 歸零 → GameManager 判定失敗 |

## 7. `ExposureSystem` `Scripts/Player/ExposureSystem.cs`（擁有者：A）

| 成員 | 說明 |
|---|---|
| `float Exposure { get; }` | 0–100，實際暴露值 |
| `float VisualExposure { get; }` | **B 的畫面效果讀這個**；戒菸噴霧生效時為 0，其餘等於 Exposure |
| `bool IsWarning { get; }` | Exposure ≥ 80 |
| `bool IsMaxed { get; }` | Exposure ≥ 100 |
| `float SmokeSeconds { get; }` | 累計待在煙霧範圍內的秒數。**只算倒數中（Playing）**，開場死巷裡的時間不算（暴露值本身照算） |
| `event Action<float> OnExposureChanged` | |
| `event Action<bool> OnWarningChanged` | **C 的警示文字監聽**；true 顯示、false 隱藏 |
| `event Action<bool> OnMaxedChanged` | 進入／離開 100（咳嗽音效、減速） |
| `float RiseMultiplier { get; set; }` | 上升速度倍率，預設 1。**只給 A 的 PlayerEffects（尼古丁貼片 0.75）使用** |
| `bool IsVisualSuppressed { get; set; }` | true 時 `VisualExposure` 回傳 0（暴露值照算）。**只給 A 的 PlayerEffects（戒菸噴霧）使用** |

- 同時在多個煙霧範圍內時，取**上升速度最大**的那一個來源，不加總。距離只算水平距離。
- 只在 GameManager 為 `Opening` 或 `Playing` 時計算（開場吸菸 NPC 也會影響暴露值）。
- 暴露值 100 時由 ExposureSystem 自己呼叫 `SetSpeedModifier("MaxExposure", 0.7)`，B、C 不用處理減速。

## 8. `SmokeSource` `Scripts/Monsters/SmokeSource.cs`（擁有者：A，B 掛在物件上）

| 成員 | 說明 |
|---|---|
| `static IReadOnlyList<SmokeSource> All` | 目前所有啟用中的煙霧來源 |
| `float Radius { get; set; }` | Inspector 可調，預設 50 |
| `bool IsEmitting { get; set; }` | false 時不影響暴露值 |

- 在 `OnEnable` 自動加入 `All`，`OnDisable` 自動移除。
- **掛在：開場吸菸 NPC、每一隻菸怪。** 菸怪被擊敗時把 `IsEmitting` 設 false 或停用物件即可。

## 9. `ScoreCalculator` `Scripts/Core/ScoreCalculator.cs`（擁有者：A）

```csharp
public static class ScoreCalculator {
    public static ScoreResult Calculate(bool isWin, float hp, float maxHp,
                                        float remainingSeconds, float roundDuration,
                                        float smokeSeconds);
}
```
規則見 docs/DECISIONS.md。

## 10. `PlayerEffects` `Scripts/Player/PlayerEffects.cs`（擁有者：A，之後實作）

| 成員 | 說明 |
|---|---|
| `void ApplyItem(ItemType type)` | **C 的背包按右鍵使用道具時呼叫** |
| `event Action<ItemType, float> OnEffectStarted` | 參數：道具、持續秒數（HUD 可顯示倒數） |
| `event Action<ItemType> OnEffectEnded` | |

---

## B、C 擁有、但 A 需要知道的介面

### `SmokeMonster`（擁有者：B）
- 實作 `IHittable`
- 欄位：`MonsterSize Size`、`int HitsRequired`（小 3／大 10）、`float ProgressValue`（小 5／大 15）
- `static IReadOnlyList<SmokeMonster> All`、`bool IsDefeated`：**C 的小地圖用來顯示剩餘菸怪**
- 被擊敗時：呼叫 `GameManager.Instance.AddPurificationProgress(ProgressValue)`，並停止 SmokeSource

### `ChestController`（擁有者：C）／`EscortNPC`（擁有者：B）
- 實作 `IInteractable`

### `HealthItemData`（擁有者：C，ScriptableObject）
- 欄位至少包含 `ItemType Type`、`string DisplayName`、`Sprite Icon`、`string InfoText`
- 使用道具時：`PlayerEffects.ApplyItem(item.Type)`
