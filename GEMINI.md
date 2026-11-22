# GEMINI.md — 組員 B 專用（Antigravity）

> 這份只給 B 的 Antigravity 使用。請同時遵守 `AGENTS.md` 的全部規則。
> 本專案的 `CLAUDE.md` 是組員 A 的身分檔，**與你無關，請忽略**。

## 你的使用者
- **組員 B**，負責場景、菸怪、開場演出、NPC 故事事件、BOSS 外觀與特效、過場動畫、暴露畫面效果
- 遊戲名稱《菸菸一熄》；規格以 `docs/DECISIONS.md` 為準（2026-11-22 新版：沒有倒數、三階段、BOSS 戰；**護送 NPC 已刪除**）
- B 是程式、Git 與 vibecoding 的初學者，請用繁體中文、一步一步說明
- **任何改動前都要先說明會改什麼、會有什麼結果，等 B 同意才執行**（見 AGENTS.md 第一節）

## B 負責的範圍
- `Assets/_Project/Scripts/Monsters/`（但 `SmokeSource.cs` 是 A 的，只能使用、不能改）
- `Assets/_Project/Scripts/Opening/`、`Scripts/NPC/`（故事事件）、`Scripts/VFX/`（含 BOSS 外觀特效）
- BOSS 的**戰鬥邏輯**（血量、回血、攻擊判定）在 `Scripts/Boss/`，是 A 的；B 只負責外觀、動畫、特效，依 A 提供的事件播放
- **`GameScene.unity` 只有 B 可以修改**；A、C 會把做好的 Prefab 交給 B 放進場景
- 測試場景：`TestScene_B.unity`

## B 必須照著 docs/INTERFACES.md 實作的介面
- `SmokeMonster` 實作 `IHittable`；被擊敗時呼叫 `GameManager.Instance.AddPurificationProgress(ProgressValue)`
- 菸怪與吸菸 NPC 都掛上 A 的 `SmokeSource` 元件
- `OpeningSequence` 監聽 `RouteManager.Instance.OnRouteChosen`，演出結束呼叫 `RouteManager.Instance.NotifyOpeningFinished()`
  - 路線 A 的重新載入由 RouteManager 處理，OpeningSequence **不要自己 `LoadScene`**
  - 開場期間玩家控制的分工見 `docs/INTERFACES.md` 第 5 節（場景開始由 RouteManager 鎖住；選完路線到演出結束由 B 決定；路線 B 結束由 RouteManager 恢復）
- 畫面效果讀 `ExposureSystem.VisualExposure`
- 死巷出口的 `AlleyExitTrigger`（A 的元件）先留在場景中不要刪；新流程沒有倒數，它之後怎麼用由 A10 決定
- 會被玩家按 E 互動的 NPC 實作 `IInteractable`；**不要自己讀 E 鍵或 Q 鍵**
- 故事事件、階段切換、BOSS、吸菸 NPC 的新介面名稱，等 A 完成 A10（更新 `docs/INTERFACES.md`）後照著用；**INTERFACES 還沒寫的名稱不要自己發明**
- `EscortNPC`、`SafeZone`、`GuideLine` 已不使用，由 B 在 Unity 的 Project 視窗刪除（任務 B7）

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
