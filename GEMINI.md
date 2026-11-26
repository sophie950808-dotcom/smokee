# GEMINI.md — 組員 B 專用（Antigravity）

> 這份只給 B 的 Antigravity 使用。請同時遵守 `AGENTS.md` 的全部規則。
> 本專案的 `CLAUDE.md` 是組員 A 的身分檔，**與你無關，請忽略**。

## 你的使用者
- **組員 B**，負責場景、菸怪、開場演出、NPC 故事事件、BOSS 外觀與特效、過場動畫、暴露畫面效果
- 遊戲名稱《菸菸一熄》；企劃書是 `docs/GDD_v3.1.md`，規格以 `docs/DECISIONS.md` 為準（2026-11-22 新版：沒有倒數、三階段、BOSS 戰；**護送 NPC 已刪除**）
- B 是程式、Git 與 vibecoding 的初學者，請用繁體中文、一步一步說明
- **任何改動前都要先說明會改什麼、會有什麼結果，等 B 同意才執行**（見 AGENTS.md 第一節）

## B 負責的範圍
- `Assets/_Project/Scripts/Monsters/`（但 `SmokeSource.cs` 是 A 的，只能使用、不能改）
- `Assets/_Project/Scripts/Opening/`、`Scripts/NPC/`（故事事件）、`Scripts/VFX/`（含 BOSS 外觀特效）
- BOSS 的**戰鬥邏輯**（血量、回血、攻擊判定）在 `Scripts/Boss/`，是 A 的；B 只負責外觀、動畫、特效，依 A 提供的事件播放
- **`GameScene.unity` 只有 B 可以修改**；A、C 會把做好的 Prefab 交給 B 放進場景
- 測試場景：`TestScene_B.unity`

## B 必須照著 docs/INTERFACES.md 實作的介面（v3.1，A11 更新：2026-11-25）
**B 要看 INTERFACES.md 的：第 0 節（共用型別）、第 4 節（PhaseManager 給 B 的事件與方法）、第 5 節（教學發光）、第 6 節（TaskHint）、第 8 節（PlayerController 鎖操作、TeleportTo、SetCameraOverride）、第 11 節（SmokeSource 拒絕懲罰）、第 12 節（ItemRewards）、第 14–15 節（BOSS 事件）、第 17 節（B 擁有的類別）、第 18 節（三條流程）、第 22 節（第三階段起點）。**

**A11 的變動（B 要注意）**
- `PhaseManager` 已完成：GameScene 的 **GameManager 物件上要掛 `PhaseManager` 和 `TutorialManager`**（B 在 GameScene 加）
- 第三階段淨化進度**固定 60%**，打倒 BOSS 才 100%；淨化里程碑事件 `PhaseManager.OnPurificationMilestone(20/60/100)`
- 事件回報要照順序：先 `ReportEventAccepted(n)` 才能 `ReportEventCompleted(n)`；拒絕後重新接受也是呼叫 `ReportEventAccepted(n)`；事件一不能拒絕；編號、順序不對會被忽略並印黃色警告
- A11 已經（經 A 同意）代改 `Opening/OpeningSequence.cs`：拒絕路線**車庫門開好就呼叫 `NotifyOpeningFinished()`**、移除 OnGUI 的「新手引導」文字。B 請先拉 develop 再改這支檔案
- 第三階段起點（第 22 節）需要 B 配合的部分【待確認】：在 GameScene 放第三階段玩家起點（A 會做標記元件 `Phase3StartPoint`）；重新挑戰時（`Phase3Checkpoint.IsRetryPending`）開場不播放

- **菸怪**（B10、B12）：`SmokeMonster` 實作 `IHittable`；新增 `int PurificationUnits`（整數，1000 = 100%）、`MonsterGroup Group`、`OnDefeated`／`OnAnyDefeated` 事件；被擊敗呼叫 `GameManager.Instance.AddPurification(PurificationUnits)`（舊的 `AddPurificationProgress` 會出現黃色警告，B10 改掉）
  - 生成時用 `PhaseManager.Instance.GetMonsterUnits(group, index)` 設定淨化值（教學 0、第一階段 40、空檔 25、事件 33／33／34）
  - Awake 時設 `SmokeSource.AffectedByRefusal = true`；`TakeHit` 時若 `SmokeSource.IsIndestructible` 為 true → 播「打不掉」回饋、不算次數
  - 生成時機：監聽 `PhaseManager.OnPhaseChanged(Phase1)`（5 隻）、`OnEventGapStarted`（每次 2 隻）；故事地點 3 隻由故事事件生成
- **開場**（B7）：`OpeningSequence` 監聽 `RouteManager.Instance.OnRouteChosen`
  - 接受路線：隨機一位路人演出，結束呼叫 `RouteManager.Instance.NotifyOpeningFinished()`（重新載入由 RouteManager 處理，**不要自己 `LoadScene`**）
  - 拒絕路線：**車庫門開好時就呼叫 `NotifyOpeningFinished()`**（新手教學從車庫裡的第 1 步開始），並移除 OnGUI 的「新手引導」文字（改由 TutorialManager 的任務提示）——**A11 已代改完成**
  - `AlleyExitTrigger` 已標記 Obsolete、A11 起不做任何事：B 從 GameScene 的物件上移除這個元件（目前還掛著）
- **新手教學發光**：監聽 `TutorialManager.OnStepChanged`，讀 `MoveTarget`、`TutorialMonster`；`GameScene` 的 TutorialManager 欄位由 B 拖入
- **故事事件**（B9、B11、B12）：B 建立 `StoryEventDirector`、`StoryEventData`（ScriptableObject）、`StoryTeleporter`（名稱、成員見 INTERFACES 第 17 節）
  - 監聽 `PhaseManager.OnStoryEventRequested(n)` → NPC 跑來求救；回報用 `PhaseManager.ReportEventAccepted／ReportHelpRefused／ReportEventCompleted(n)`
  - 拒絕後 NPC 跟著玩家、實作 `IInteractable`，按 E **再跳一次「要幫忙嗎？」**
  - 給道具用 `ItemRewards.Grant(ItemType)`（事件一貼片、事件二口嚼錠；藏寶圖由 PhaseManager 處理）
  - 任務提示用 `TaskHint.SetTask("StoryEvent", 文字)`；對話用 C 的 `DialogueUI.Show(...)`；「要幫忙嗎？」用 C 的 `HelpPromptUI.Show(...)`
  - 傳送用 `PlayerController.TeleportTo(...)`，期間 `GameManager.SetTimerPaused("Teleport", true)`、`PlayerController.SetControlLock("Teleport", true)`
- **BOSS 外觀與演出**（B13、B15–B17）：BOSS 的移動、縮放、傷害判定是 A 的；B 只做模型（子物件）、動畫、特效
  - （A14）Prefab 在 `Assets/_Project/Prefabs/Boss/`：**B 只改 `BossVisual.prefab`、`SmokeRingVisual.prefab`**（巢狀放在 A 的 `Boss.prefab`、`SmokeRing.prefab` 裡）；不要改 A 的 Prefab，Visual 裡不要加碰撞體或邏輯腳本。B13 把 `Boss.prefab` 拖進 GameScene 並設定 BossController 的 `_groundPoint`（場景裡只能有一個 BOSS）
  - （A14）**菸炮取消**：天上也吐同一種菸圈（天上的菸圈落地留下 3 秒煙霧區，A16）。菸圈特效接 `BossAttack.OnSmokeRingFired(起點, 方向)`；**不要接** `OnCannonFired`／`OnCannonLanded`。口嚼錠只清煙霧區、不清菸圈（`ClearableSmoke.OnCleared` 照樣觸發）
  - （A14）BOSS 在地面時根物件會水平轉向玩家：`BossVisual.prefab` 的模型正面請朝 +Z
  - 監聽 `PhaseManager.OnBossIntroRequested` 播登場 → `PhaseManager.NotifyBossIntroFinished()`（重新挑戰會再播一次）
  - 監聽 `BossController.OnStateChanged`、`OnSizeLevelChanged`、`BossHealth.OnBossDamaged`、`BossAttack` 的事件、`PlayerEffects.OnSprayFired(起點, 終點, 有沒有打中)`、口嚼錠清除特效聽 `ClearableSmoke.OnCleared(中心, 半徑)`（A13 起取代 `BossAttack.OnSmokeCleared`）
  - 監聽 `PhaseManager.OnEndingRequested` 播結局 → `PhaseManager.NotifyEndingFinished()`（A14：**一監聽就一定要在播完時呼叫**，否則遊戲不會結算；沒有人監聽時 A 會直接結算）
  - 受擊特效：`BossHealth.OnBossDamaged(傷害, 來源)`，來源 `DamageSource.PlayerMelee`（近戰）／`Spray`（噴霧）
  - 過場鏡頭用 `PlayerController.SetCameraOverride(點)`，不用再把 PlayerController 停用
- 畫面效果讀 `ExposureSystem.VisualExposure`（不變）
- 會被玩家按 E 互動的 NPC 實作 `IInteractable`；**不要自己讀 E 鍵或 Q 鍵**。需要額外條件（例如要面向 NPC）時，可以另外實作可選介面 `IInteractCondition`（A13，見 INTERFACES 第 2 節）
- **INTERFACES 還沒寫的名稱不要自己發明**，不夠用請 B 去找 A
- B7 要在 Unity 的 Project 視窗刪除：`EscortNPC`、`SafeZone`、`GuideLine`、`Assets/_Project/UI/InGameMenu.cs`（暫停選單改由 C 做）
- 標記 `[Obsolete]` 的東西（黃色警告）**不要自己刪**，等各自的刪除任務

## B 的任務清單（分工表）
完整分工見 Google 試算表「煙煙一息 分工大表」。第一次檢核的 B1–B6、I2、F2 已完成。

### 第二次檢核（12/03：整局可從頭玩到尾）
| 日期 | 編號 | 內容 | 前置 |
|---|---|---|---|
| 11/23 | B8 | GameScene 外圍灰盒：故事地點 1–3、吸菸室、寶箱、4 位吸菸 NPC 位置 | — |
| 11/24 | B7 | 刪除護送模組；接受路線改為隨機一位路人（咳嗽 → 台詞 → 動手 → 暈倒） | A10 |
| 11/25 | B10 | 菸怪在出生點附近遊走；依階段生成 | A11 |
| 11/26 | B9 | 故事事件流程：NPC 求救、淡出傳送、返回街道 | A11、B8、C8 |
| 11/27 | B11 | 事件一至三演出：對話、E 互動、結局、交付道具 | B9、C8 |
| 11/28 | B12 | 拒絕幫忙：菸怪無法消除、煙霧擴大、NPC 跟隨 | B9、B10、A15 |
| 11/29 | B13 | 吸菸室、BOSS 簡單外觀與位置、吸菸 NPC 擺放（11/30 預留補進度） | B8、A14 |
| 12/01 | I4 | 把所有 Prefab 放進 GameScene | I3 |
| 12/02 | F5 | 檢查場景、碰撞與效能 | T2 |

### 第三次檢核（12/17：可發表版本）
| 日期 | 編號 | 內容 |
|---|---|---|
| 12/04 | B14 | BOSS 正式外觀與模型 |
| 12/06 | B15 | 菸圈、菸炮、煙霧區特效；BOSS 隨血量降低並縮小 |
| 12/08 | B16 | BOSS 登場動畫 |
| 12/10 | B17 | 結局動畫（BOSS 遺言、綠色無煙城市） |
| 12/12 | I6 | 新的 Prefab 與動畫放進 GameScene |
| 12/15 | F8 | 檢查場景、碰撞與效能 |

## 注意
- 需要安裝套件（例如後製效果）或修改 Project Settings 時，**不要自己做，請 B 去找 A**
- 介面不夠用時，**不要自己改 INTERFACES.md 或 A 的腳本**，請 B 去找 A
