# GEMINI.md — 組員 B 專用（Antigravity）

> 這份只給 B 的 Antigravity 使用。請同時遵守 `AGENTS.md` 的全部規則。
> 本專案的 `CLAUDE.md` 是組員 A 的身分檔，**與你無關，請忽略**。

## 你的使用者
- **組員 B**，負責場景、菸怪、開場演出、暴露畫面效果、護送 NPC
- B 是程式、Git 與 vibecoding 的初學者，請用繁體中文、一步一步說明
- **任何改動前都要先說明會改什麼、會有什麼結果，等 B 同意才執行**（見 AGENTS.md 第一節）

## B 負責的範圍
- `Assets/_Project/Scripts/Monsters/`（但 `SmokeSource.cs` 是 A 的，只能使用、不能改）
- `Assets/_Project/Scripts/Opening/`、`Scripts/NPC/`、`Scripts/VFX/`
- **`GameScene.unity` 只有 B 可以修改**；A、C 會把做好的 Prefab 交給 B 放進場景
- 測試場景：`TestScene_B.unity`

## B 必須照著 docs/INTERFACES.md 實作的介面
- `SmokeMonster` 實作 `IHittable`；被擊敗時呼叫 `GameManager.Instance.AddPurificationProgress(ProgressValue)`
- 菸怪與吸菸 NPC 都掛上 A 的 `SmokeSource` 元件
- `OpeningSequence` 監聽 `RouteManager.Instance.OnRouteChosen`，演出結束呼叫 `RouteManager.Instance.NotifyOpeningFinished()`
- 死巷出口 Trigger 呼叫 `RouteManager.Instance.NotifyPlayerLeftAlley()`
- 畫面效果讀 `ExposureSystem.VisualExposure`
- `EscortNPC` 實作 `IInteractable`，護送成功呼叫 `PlayerHealth.Heal(10)`

## B 的任務清單（分工表）
| 編號 | 內容 | 前置 |
|---|---|---|
| B1 | GameScene 灰盒：死巷與一段街道 | A1 |
| B2 | 菸怪 Prefab：大小兩種參數、受擊次數、消散 | A2 |
| B3 | 簡化開場：路線 A 轉黑加字幕、路線 B 進入街道 | B1、C2 |
| B4 | 在 GameScene 放置菸怪 | B1、B2 |
| B5 | 暴露畫面效果（先用假的暴露值測試） | A2 |

## 注意
- 需要安裝套件（例如後製效果）或修改 Project Settings 時，**不要自己做，請 B 去找 A**
- 介面不夠用時，**不要自己改 INTERFACES.md 或 A 的腳本**，請 B 去找 A
