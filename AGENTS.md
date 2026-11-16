# AGENTS.md — 全組共用的 AI 協作規則

> 本專案三位組員都用 AI 寫程式：A 用 Claude Code、B 用 Antigravity、C 用 ChatGPT。
> **所有 AI 工具都必須遵守這份規則。** 每位組員另有一份身分檔，說明「使用者是誰、負責什麼」：
> - A → `CLAUDE.md`　B → `GEMINI.md`　C → `docs/CHATGPT_PROMPT.md`
> - **只讀屬於你使用者的那份身分檔，其他人的身分檔請忽略。**
>
> 使用者都是程式、Git 與 vibecoding 的初學者。請用**繁體中文**，用初學者聽得懂的話說明。

## 專案
- 二手菸危害宣導遊戲，Unity 3D，Windows PC，鍵盤＋滑鼠
- Unity 版本 **6000.0.43f1**（三人相同，不得升級或降級）
- 規格以 `docs/DECISIONS.md` 為準（優先於企劃書）
- 跨模組的類別、方法、事件名稱以 `docs/INTERFACES.md` 為準

---

## 一、最高原則（任何情況都不能違反）

### 1. 任何改動之前，都要先問使用者
在新增、修改、刪除任何檔案、改任何設定、安裝套件、執行任何會改變東西的指令**之前**，必須先告訴使用者：
- **要改什麼**（哪些檔案、哪些設定）
- **為什麼要改**
- **改了之後會有什麼結果**，包括可能的副作用（例如「會影響 B 的菸怪腳本」「Unity 需要重新啟動」）

**等使用者明確回覆同意（例如「可以」「同意」「好」）才可以執行。**
執行到一半發現計畫需要改變，要**停下來重新說明並再問一次**，不能自己決定。

### 2. 遇到問題就問，不准猜、不准編
- 執行時遇到錯誤、資訊不足、不確定使用者的意思 → **停下來問使用者**
- **不可以編造**：不存在的 Unity API、套件名稱、檔案內容、錯誤原因、設定位置
- 不確定時要直接說「我不確定」，並說明可以怎麼查證（例如「請把 Console 的完整錯誤訊息貼給我」）
- 企劃書、規格沒寫清楚的地方，不要自己補規則，要問使用者

### 3. 不能說「已經測試過」
AI 沒辦法在 Unity 裡按 Play。完成後只能說「程式寫好了，請在 Unity 測試」，並附上測試步驟。
**不可以說「這樣一定可以」「已經測試通過」。**

---

## 二、工作方式
1. **一次只做一個任務。** 不要「順便」重構、改名、整理格式、刪除看起來沒用的程式碼，或加使用者沒要求的功能。
2. **先讀再寫。** 修改前先讀相關的現有檔案與 `docs/INTERFACES.md`，不要憑空假設檔案內容。
3. **修錯誤時先說明原因再修。** 同一個錯誤修了兩次還沒好，就停下來整理目前狀況，和使用者討論，不要一直換方法亂試。
4. **專有名詞第一次出現要解釋**（例如「Prefab 是可以重複使用的物件範本」）。
5. 對話很長、開始忘記前面內容時，主動建議使用者開新對話，並先給交接摘要。

## 三、每次任務完成後必須回報
1. **修改摘要**：新增、修改了哪些檔案
2. **Unity 內需要手動做的事**：建哪些 GameObject、掛哪些元件、Inspector 要拖哪些欄位、Layer／Tag 設定
3. **測試步驟**：按 Play 後要做什麼、應該看到什麼
4. **已知限制或風險**
5. **交接摘要**：做完了什麼、還剩什麼、下次從哪裡開始（讓使用者開新對話時可以貼回來）

---

## 四、檔案所有權（誰可以改什麼）
所有路徑都在 `Assets/_Project/` 底下。

| 組員 | 負責 |
|---|---|
| A | `Scripts/Core/`、`Scripts/Player/`、`Scripts/Combat/`、`Scripts/Interaction/`、`Scripts/DevTools/`、`Scripts/Monsters/SmokeSource.cs`、`TestScene_A` |
| B | `Scripts/Monsters/`（SmokeSource 除外）、`Scripts/Opening/`、`Scripts/NPC/`、`Scripts/VFX/`、**`GameScene.unity`**、`TestScene_B` |
| C | `Scripts/UI/`、`Scripts/Items/`、`Scripts/Quiz/`、**`MainMenu.unity`**、`TestScene_C` |
| **只有 A 能改** | `AGENTS.md`、`CLAUDE.md`、`GEMINI.md`、`docs/` 全部、`ProjectSettings/`、`Packages/manifest.json` |

- **只改使用者負責的檔案。** 任務需要改到別人的檔案時，停下來告訴使用者「這需要找 ○○ 討論」。
- 發現 `docs/INTERFACES.md` 的介面不夠用或需要修改時，**不要自己改介面**，請使用者去找 A。
- 需要安裝套件或改專案設定時，請使用者去找 A。

## 五、Unity 規則
1. **不要用文字方式修改 `.unity`、`.prefab`、`.asset`、`.meta` 檔。** 場景與 Prefab 請使用者在 Unity 編輯器操作，或寫 Editor 選單腳本（放在 `Assets/_Project/Editor/`）讓使用者一鍵執行。
2. **不要在 Unity 外面搬移、改名、刪除資產檔案**，會讓 `.meta` 對不上。請使用者在 Unity 的 Project 視窗操作。
3. 新增腳本後，提醒使用者**先回 Unity 等編譯完成、確認 Console 沒有紅字，再 commit**，並確認 `.meta` 檔一起提交。
4. **MonoBehaviour 的類別名稱必須和檔名完全相同。**
5. 不要建立重複的管理器（例如第二個 GameManager），也不要另外保存淨化進度、HP、暴露值等狀態的副本。一律讀取 `docs/INTERFACES.md` 定義的屬性與事件。
6. 輸入：使用新版 Input System（`using UnityEngine.InputSystem;`，`Keyboard.current`、`Mouse.current`）。**不要用舊版 `Input.GetKey`、`Input.GetAxis`。**
7. 只有 A 的 PlayerInteractor 讀 E 鍵、只有 GameManager 讀 Esc 鍵。其他腳本不要自己讀這兩個鍵。
8. Unity 6 API：用 `Rigidbody.linearVelocity`（不是 `velocity`）、`FindFirstObjectByType`（不是 `FindObjectOfType`）。
9. 暫停時 `Time.timeScale = 0`，UI 動畫請用 `Time.unscaledDeltaTime`。

## 六、Git 規則
1. **不要自己 commit、push、merge、切換分支**，除非使用者明確要求。使用者用 GitHub Desktop 自己檢查與提交。
2. **絕對不要執行**：`git reset --hard`、`git push --force`、`git clean`、刪除資料夾等無法復原的指令。
3. 不要把密碼、API Key、個人資料寫進任何檔案。

## 七、程式風格
- 類別與公開成員用 PascalCase，私有欄位用 `_camelCase`
- Inspector 可調數值用 `[SerializeField] private`，加 `[Header]` 與 `[Tooltip]`（中文）
- 跨模組溝通用 C# `event Action<...>`，事件命名 `OnXxx`
- 不使用 namespace
- 每個公開方法、事件都要有 `/// <summary>` 中文註解

## 八、資料夾結構
```
Assets/_Project/
  Scenes/      MainMenu, GameScene, TestScene_A, TestScene_B, TestScene_C
  Scripts/
    Core/  Player/  Combat/  Interaction/      ← A
    DevTools/                                  ← A（測試用假物件，只在測試場景用）
    Monsters/  Opening/  NPC/  VFX/            ← B（SmokeSource.cs 是 A 的）
    Items/  Quiz/  UI/                         ← C
  Editor/  Prefabs/  Materials/  Models/  Audio/  VFX/  UI/  ScriptableObjects/
```
