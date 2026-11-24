# TEAM_GUIDE.md — 組員入門指南

> 給 A、B、C 三人看的。我們都是第一次做 vibecoding 和 Git 協作，遇到不懂的就在群組問，**不要自己猜著做**。
> 有問題找 A（整合與共用文件負責人）。

---

## 1. repo 裡這些 md 檔是什麼

| 檔案 | 給誰 | 用途 | 誰能改 |
|---|---|---|---|
| `AGENTS.md` | **全部 AI** | 全組共用的 AI 規則 | A |
| `CLAUDE.md` | A 的 Claude Code | A 的身分檔 | A |
| `GEMINI.md` | B 的 Antigravity | B 的身分檔 | A |
| `docs/CHATGPT_PROMPT.md` | C 的 ChatGPT | C 貼給 ChatGPT 的指示 | A |
| `docs/INTERFACES.md` | 全部人 | **共用介面**：類別、方法、事件名稱 | A |
| `docs/DECISIONS.md` | 全部人 | **已拍板的規則與數值**（優先於企劃書） | A |
| `docs/GDD_v3.1.md` | 全部人 | **企劃書 v3.1**（Markdown 版，AI 讀得到；C 要上傳到 ChatGPT） | A |
| `docs/菸菸一熄_企畫書_v3.1.docx` | 全部人 | 企劃書原稿（Word 版，內容和 GDD_v3.1.md 相同） | A |
| `docs/learning/` | A | A 的 Claude Code 盤點紀錄（參考用） | A |
| `docs/TEAM_GUIDE.md` | 全部人 | 這份指南 | A |

**為什麼不會打架？**
- 每個 AI 只會自動讀自己的檔案：Claude Code 讀 `CLAUDE.md`，Antigravity 讀 `AGENTS.md` 和 `GEMINI.md`，ChatGPT 什麼都讀不到（要 C 自己上傳）。
- 共用規則只有 `AGENTS.md` 一份，三人的 AI 看到的是同一套規則。
- **這些檔案只有 A 能改**，所以不會兩個人同時改同一份而衝突。想改什麼，在群組跟 A 說。

---

## 2. 第一次設定（每人做一次）

### 2-1. 拿到最新的 develop
1. 打開 GitHub Desktop → 上方 **Current Branch** → 選 `develop`
2. 按 **Fetch origin**，再按 **Pull origin**

### 2-2. 建立自己的分支
1. 確認目前在 `develop`
2. **Current Branch** → **New Branch**
3. 名字：B 打 `feature/B-world`，C 打 `feature/C-ui-quiz`
4. 確認是從 `develop` 建立 → **Create branch** → 按 **Publish branch**

### 2-3. 設定自己的 AI 工具
- **B（Antigravity）**：用 Antigravity 打開 repo 資料夾（有 `Assets`、`AGENTS.md` 的那層）。第一次先問它：「你讀到了哪些規則檔？我是誰？我負責什麼？」確認它回答「組員 B」才開始工作。
- **C（ChatGPT）**：照 `docs/CHATGPT_PROMPT.md` 的說明建一個 ChatGPT 專案，上傳四份檔案（`AGENTS.md`、`docs/INTERFACES.md`、`docs/DECISIONS.md`、`docs/GDD_v3.1.md`）、貼上指示。第一次先問它：「我是誰？我可以改哪些資料夾？」確認它回答正確。

---

## 3. 每天的 Git 流程

### 觀念
- **分支（branch）**：從主線複製一份副本，自己在副本上改，不會影響別人
- **合併（merge）**：把副本的修改併回主線
- **Pull Request（PR）**：在 GitHub 網頁上「請求合併」，讓組員先看過

```
main                ← 穩定版，平常不碰
 └ develop          ← 整合版，大家從這裡拿最新進度（已設為預設分支）
    ├ feature/A-core
    ├ feature/B-world
    └ feature/C-ui-quiz
```

### 開工時：拿組員的新進度
1. **Unity 先存檔**（Ctrl+S）
2. GitHub Desktop 切到 `develop` → **Fetch origin** → **Pull origin**
3. 切回自己的分支 → 上方選單 **Branch** → **Merge into current branch…** → 選 `develop` → Merge
4. 回 Unity 等它重新整理，確認 **Console 沒有紅字**

### 完成一個任務時：交出去
1. 確認 Unity 編譯完成、Console **沒有紅字**
2. GitHub Desktop 左邊看變更清單：
   - 新增的 `.cs` 有沒有對應的 `.cs.meta`？（**一定要有**）
   - 有沒有出現你沒動過的檔案？（例如 `GameScene.unity`，不是 B 就不該出現 → 右鍵 **Discard changes**）
3. 左下 Summary 寫說明，例如 `feat: B1 greybox alley and street` → **Commit to 你的分支**
4. **Push origin**
5. 按 **Create Pull Request** → 網頁上確認是 **`develop` ← 你的分支** → 寫改了什麼、怎麼測 → 建立
6. 在群組請一位組員看；看過就按 **Merge pull request**
7. 網頁出現 **Delete branch** 按鈕時**不要按**（分支還要繼續用）
8. 群組說一聲「○○ 合併了，大家 pull 一下」

### 常見狀況
- **切換分支時跳出提示**，說有沒 commit 的修改 → 先回去 commit，再切換
- **合併時出現 conflict（衝突）**：
  - `.cs` 檔 → 請你的 AI 幫忙解，或找 A
  - `.unity` / `.prefab` 檔 → **不要自己合**，在群組討論要留哪一邊
- **不確定該不該按某個按鈕** → 先在群組問，不要賭

---

## 4. Unity 的坑（一定要看）

### 坑一：`.meta` 檔
Unity 幫每個檔案配一個 `.meta`，裡面是這個檔案的「身分證字號」。Unity 記錄引用時看的是身分證字號，不是檔名。
- **新增腳本後，一定要先回 Unity 等編譯完，再 commit**，讓 `.meta` 產生並一起上傳
- **不要在檔案總管搬移、改名、刪除** Assets 裡的東西，要在 Unity 的 Project 視窗做
- C 用 ChatGPT：**先在 Unity 建腳本，再貼上程式碼**（見 `docs/CHATGPT_PROMPT.md` 最後一段）

### 坑二：類別名稱 = 檔名
`SmokeMonster.cs` 裡面必須是 `public class SmokeMonster`，否則掛不上物件。

### 坑三：場景和 Prefab 不能兩人同時改
場景檔是幾萬行的文字，兩人都改就合不起來。
- `GameScene` **只有 B 能改**；`MainMenu` **只有 C 能改**
- 其他人在自己的 `TestScene_A／B／C` 測試，做好的東西存成 **Prefab** 交給場景負責人

### 坑四：Inspector 欄位要手動拖
AI 沒辦法幫你拖。忘了拖，按 Play 就會出現 `NullReferenceException`。AI 每次完成都會列出「要手動做的事」，照著做。

### 坑五：一個紅字，全部不能玩
任何一支腳本有紅字錯誤，整個專案都不能按 Play。**有紅字的東西絕對不能合併進 develop。**

### 坑六：Play 模式中改的數值會消失
按停止後全部還原。調好的數字先記下來，停止後再改一次。

### 坑七：不要自己改專案設定或裝套件
`Project Settings`、`Package Manager` 改了會影響全組，**需要的話找 A**。

---

## 5. 跟 AI 合作的規則（AGENTS.md 摘要）
1. **AI 改任何東西前都要先說明、等你同意。** 它沒問就改，請它停下來。
2. **AI 不可以猜或編答案。** 它說的 Unity 功能你找不到，很可能是它編的，直接問它「這個真的存在嗎？」
3. **AI 說寫好了 ≠ 完成。** 一定要自己按 Play 測過。
4. **一次只叫 AI 做一件事。**
5. **錯誤訊息要整段複製貼給 AI**，不要只說「它壞了」。
6. 同一個錯誤 AI 修兩次還沒好，就停下來，在群組問。
7. 介面（`docs/INTERFACES.md`）不夠用 → 找 A，不要讓 AI 自己發明新名稱。
8. AI 寫的程式至少要看得懂「這支在做什麼」，看不懂就問它。
