using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 遊戲內右上角小齒輪設置與暫停選單
/// 包含：暫停/繼續、按鍵操作、衛教道具資訊、二手菸宣導、返回主選單
/// 企劃書規格：6.3 節衛教道具、4.2 節操作規格
/// </summary>
public class InGameMenu : MonoBehaviour
{
    private bool isMenuOpen = false;
    private int currentTab = 0; // 0: 暫停選單, 1: 按鍵操作, 2: 衛教道具, 3: 衛教宣導

    private void Update()
    {
        // 按 Esc 鍵也可開關選單
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleMenu();
        }
    }

    private void OnGUI()
    {
        // 1. 右上角小齒輪按鈕 (隨時可點擊)
        GUIStyle gearStyle = new GUIStyle(GUI.skin.button);
        gearStyle.fontSize = 22;
        gearStyle.alignment = TextAnchor.MiddleCenter;

        float gearX = Screen.width - 65;
        float gearY = 15;

        if (GUI.Button(new Rect(gearX, gearY, 50, 50), "⚙", gearStyle))
        {
            ToggleMenu();
        }

        // 2. 設置選單主體面板
        if (isMenuOpen)
        {
            int panelW = 680;
            int panelH = 500;
            float cx = (Screen.width - panelW) / 2f;
            float cy = (Screen.height - panelH) / 2f;

            // 外框背景
            GUIStyle winBox = new GUIStyle(GUI.skin.box);
            winBox.fontSize = 20;
            GUI.Box(new Rect(cx, cy, panelW, panelH), "\n【 遊戲設定與衛教資訊 】", winBox);

            // 分頁按鈕列
            string[] tabs = new string[] { "⏸ 遊戲狀態", "🎮 按鍵操作", "💊 衛教道具", "🚭 衛教宣導" };
            for (int i = 0; i < tabs.Length; i++)
            {
                GUIStyle tabStyle = new GUIStyle(GUI.skin.button);
                tabStyle.fontSize = 15;
                if (currentTab == i)
                {
                    tabStyle.normal.textColor = Color.yellow;
                    tabStyle.fontStyle = FontStyle.Bold;
                }

                if (GUI.Button(new Rect(cx + 30 + i * 155, cy + 50, 145, 38), tabs[i], tabStyle))
                {
                    currentTab = i;
                }
            }

            // 分頁內容區域
            GUIStyle contentStyle = new GUIStyle(GUI.skin.label);
            contentStyle.fontSize = 16;
            contentStyle.richText = true;
            contentStyle.normal.textColor = Color.white;

            Rect contentRect = new Rect(cx + 40, cy + 105, panelW - 80, 310);

            switch (currentTab)
            {
                case 0: // 遊戲狀態與基本控制
                    string statusInfo =
                        "<b>【當前遊戲狀態】</b>\n" +
                        "• 遊戲已暫停。遠離菸怪可避免暴露值持續攀升。\n" +
                        "• 擊敗全部大小菸怪即可達成 100% 淨化勝利！\n\n" +
                        "<b>【快速功能】</b>\n" +
                        "• 點擊「繼續遊戲」即可恢復戰鬥。\n" +
                        "• 如需重新開始本局，可點選「重新開始」。";
                    GUI.Label(contentRect, statusInfo, contentStyle);
                    break;

                case 1: // 按鍵操作指南
                    string controlInfo =
                        "<b>【核心操作說明】</b>\n" +
                        "• <b>W / A / S / D</b>：角色移動\n" +
                        "• <b>滑鼠移動</b>：旋轉視角\n" +
                        "• <b>滑鼠左鍵</b>：揮拳普通攻擊（小菸怪3下、大菸怪10下消滅）\n" +
                        "• <b>E 鍵</b>：互動（開啟寶箱答題、引導護送 NPC）\n" +
                        "• <b>滑鼠滾輪</b>：切換背包衛教道具\n" +
                        "• <b>滑鼠右鍵</b>：使用選取的衛教道具（一次性）\n" +
                        "• <b>Esc 鍵 或 ⚙</b>：開啟／關閉本設定選單";
                    GUI.Label(contentRect, controlInfo, contentStyle);
                    break;

                case 2: // 衛教道具
                    string itemInfo =
                        "<b>【6 大衛教道具效果與用途】</b>\n" +
                        "• <b>尼古丁口嚼錠</b>：20秒內移動速度 +50%（作用快速緩解渴求）\n" +
                        "• <b>尼古丁貼片</b>：60秒內暴露值上升減緩 25%（作用溫和持久）\n" +
                        "• <b>尼古丁吸入劑</b>：攻擊力暴增3倍，小菸怪一擊必殺！\n" +
                        "• <b>尼古丁戒菸噴霧</b>：20秒內清除暴露值造成的畫面黑邊煙霧\n" +
                        "• <b>戒必適 / Bupropion</b>：處方戒菸藥物，標記周遭菸怪位置\n" +
                        "<i>※ 註：現實用藥應經醫療專業人員評估，切勿擅自服用。</i>";
                    GUI.Label(contentRect, itemInfo, contentStyle);
                    break;

                case 3: // 衛教宣導
                    string eduInfo =
                        "<b>【二手菸危害宣導】</b>\n" +
                        "• 二手菸沒有安全暴露量，即使短時間接觸也會侵害心血管！\n" +
                        "• 窗戶通風或電風扇無法完全消除室內二手菸與三手菸毒素。\n" +
                        "• 護送長者與孩童至安全區「無菸公園」，可回復玩家生命值！\n\n" +
                        "<b>【戒菸諮詢資源】</b>\n" +
                        "• 免費戒菸專線：<b>0800-636363</b>\n" +
                        "• 全國各醫療院所皆設有戒菸門診，提供專業一對一諮詢服務。";
                    GUI.Label(contentRect, eduInfo, contentStyle);
                    break;
            }

            // 底部操作按鈕列
            GUIStyle btnStyle = new GUIStyle(GUI.skin.button);
            btnStyle.fontSize = 16;

            if (GUI.Button(new Rect(cx + 40, cy + 430, 180, 45), "▶ 繼續遊戲", btnStyle))
            {
                ToggleMenu();
            }

            if (GUI.Button(new Rect(cx + 250, cy + 430, 180, 45), "🔄 重新開始", btnStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }

            if (GUI.Button(new Rect(cx + 460, cy + 430, 180, 45), "🏠 返回主選單", btnStyle))
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene("MainMenu");
            }
        }
    }

    public void ToggleMenu()
    {
        isMenuOpen = !isMenuOpen;

        // 呼叫 GameManager 暫停遊戲時間
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetPaused(isMenuOpen);
        }
        else
        {
            Time.timeScale = isMenuOpen ? 0f : 1f;
        }

        // 解鎖/鎖定滑鼠指針
        Cursor.lockState = isMenuOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isMenuOpen;
    }
}
