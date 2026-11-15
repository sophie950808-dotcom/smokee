# CLAUDE.md — 組員 A 專用（Claude Code）

> 這份只給 A 的 Claude Code 使用。其他 AI 工具請忽略本檔，改看 `AGENTS.md` 與自己的身分檔。

@AGENTS.md

## 你的使用者
- **組員 A（胡妤安）**，負責核心系統、玩家控制，以及整合 B、C 的程式
- A 也管理 `develop` 分支與所有共用文件（`AGENTS.md`、`docs/`）
- A 是程式與 vibecoding 初學者，用過 Unity 2D 與 C#，Git 只用過 GitHub Desktop

## A 負責的範圍
見 `AGENTS.md` 第四節。A 是唯一可以改共用文件、`ProjectSettings/`、`Packages/` 的人，但**改之前一樣要先問 A、說明結果、等同意**。

## A 的任務清單（分工表）
| 編號 | 內容 | 前置 |
|---|---|---|
| A2 | 共用介面空殼（依 docs/INTERFACES.md） | A1 |
| A3 | PlayerController：移動、奔跑、跳躍、視角、第一／第三人稱切換 | A1 |
| A4 | PlayerAttack 近戰攻擊（先用方塊當假目標）＋ PlayerInteractor | A3 |
| A5 | GameManager：計時、淨化進度、勝敗；可調參數 | A2 |
| A6 | ExposureSystem：依距離計算、80 警示事件 | B2 |
| A7 | PlayerHealth：暴露值 100 扣血、**HP 歸零觸發失敗**（沒有倒地） | A5、A6 |
| A8 | ScoreCalculator 評分 | A5、A7 |
| A9 | RouteManager：路線 A 重新載入場景、路線 B 走出死巷開始計時 | A5 |
| I1 | 合併確認、串接所有介面、修編譯錯誤 | 全部 |

## A 的測試場景
`Assets/_Project/Scenes/TestScene_A.unity`（不要動 GameScene）

## 整合 B、C 的程式時（I1）
- 只修正**介面不一致**與**編譯錯誤**，不重寫 B、C 的邏輯
- 每一處要改 B、C 的檔案，都要先列出來讓 A 確認，A 會再通知該組員
