using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 死巷開場雙路線控制器 (任務 B.3)
/// 企劃書規格：3.2 ~ 3.4 節
/// </summary>
public class OpeningSequence : MonoBehaviour
{
    // 對外靜態事件：路線 B 結束時廣播，通知 A 組員的 RouteManager / GameManager
    public static event Action OnOpeningFinished;

    [Header("角色與物件引用")]
    [Tooltip("吸菸的朋友 NPC")]
    public Transform smokingNPC;
    [Tooltip("吸菸 NPC 身上粒子 (拒絕時點菸冒煙)")]
    public ParticleSystem npcSmokeParticles;
    [Tooltip("路人 NPC")]
    public Transform passerby;
    [Tooltip("車庫鐵捲門物件")]
    public Transform garageDoor;
    [Tooltip("玩家物件")]
    public Transform player;

    [Header("車庫門開門設定")]
    public float doorOpenHeight = 3.5f;
    public float doorOpenSpeed = 2.0f;

    [Header("路人速度設定")]
    public float passerbySpeed = 6.0f;

    public enum RouteChoice
    {
        None,
        RouteA_Accept, // 接受抽菸
        RouteB_Refuse  // 拒絕抽菸
    }
    [HideInInspector] public RouteChoice currentChoice = RouteChoice.None;

    private bool isDoorOpen = false;
    private bool hasTriggeredExit = false;

    // 畫面轉黑遮罩與文字
    private float blackFadeAlpha = 0f;
    private string narrativeSubtitle = "";
    private bool showSubtitle = false;

    private void Start()
    {
        // 初始設定：路人先隱藏
        if (passerby != null)
        {
            passerby.gameObject.SetActive(false);
        }

        // 朋友初始先不冒煙
        if (npcSmokeParticles != null)
        {
            npcSmokeParticles.Stop();
        }
    }

    private void OnGUI()
    {
        // 1. 開場選擇 UI (還沒選擇時顯示)
        if (currentChoice == RouteChoice.None)
        {
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.fontSize = 20;
            boxStyle.alignment = TextAnchor.MiddleCenter;

            GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 18;

            int boxWidth = 460;
            int boxHeight = 200;
            float centerX = (Screen.width - boxWidth) / 2f;
            float centerY = (Screen.height - boxHeight) / 2f;

            GUI.Box(new Rect(centerX, centerY, boxWidth, boxHeight),
                "車庫裡的朋友：\n「這裡沒人管，要不要來一根放鬆一下？」", boxStyle);

            if (GUI.Button(new Rect(centerX + 40, centerY + 120, 160, 50), "接受 (抽菸)", buttonStyle))
            {
                currentChoice = RouteChoice.RouteA_Accept;
                StartCoroutine(OpenDoorSequence());
            }

            if (GUI.Button(new Rect(centerX + 260, centerY + 120, 160, 50), "拒絕 (不抽)", buttonStyle))
            {
                currentChoice = RouteChoice.RouteB_Refuse;
                // 朋友點起菸成為死巷第一個煙霧來源
                if (npcSmokeParticles != null) npcSmokeParticles.Play();
                if (smokingNPC != null)
                {
                    SmokeSource src = smokingNPC.GetComponent<SmokeSource>();
                    if (src != null) src.IsEmitting = true;
                }
                StartCoroutine(OpenDoorSequence());
            }
        }

        // 2. 路線 A：畫面轉黑 + 字幕提示
        if (blackFadeAlpha > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, blackFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            if (showSubtitle)
            {
                GUIStyle subStyle = new GUIStyle(GUI.skin.label);
                subStyle.fontSize = 26;
                subStyle.alignment = TextAnchor.MiddleCenter;
                subStyle.fontStyle = FontStyle.Bold;
                subStyle.normal.textColor = new Color(1f, 0.3f, 0.3f); // 紅色警示文字

                GUI.Label(new Rect(0, Screen.height / 2f - 80, Screen.width, 160), narrativeSubtitle, subStyle);
            }
        }
    }

    private IEnumerator OpenDoorSequence()
    {
        Debug.Log("[開場演出] 車庫門正在升起...");
        if (garageDoor != null)
        {
            Vector3 startPos = garageDoor.position;
            Vector3 targetPos = startPos + Vector3.up * doorOpenHeight;
            float elapsed = 0f;

            while (elapsed < doorOpenSpeed)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / doorOpenSpeed);
                garageDoor.position = Vector3.Lerp(startPos, targetPos, t);
                yield return null;
            }
        }
        isDoorOpen = true;
        Debug.Log("[開場演出] 車庫門開啟，請操控角色走出車庫！");
    }

    /// <summary>
    /// 玩家撞到 GarageExitTrigger 時被調用
    /// </summary>
    public void OnPlayerReachedExit()
    {
        if (!isDoorOpen || hasTriggeredExit) return;
        hasTriggeredExit = true;

        if (currentChoice == RouteChoice.RouteA_Accept)
        {
            StartCoroutine(RouteA_PunishAndDieRoutine());
        }
        else if (currentChoice == RouteChoice.RouteB_Refuse)
        {
            StartCoroutine(RouteB_EnterStreetRoutine());
        }
    }

    // --- 路線 A 演出：路人吸到二手菸上前揍死玩家 ➔ 畫面轉黑 ➔ 重新開始 ---
    private IEnumerator RouteA_PunishAndDieRoutine()
    {
        Debug.Log("[路線 A] 玩家走出車庫，身上散發二手菸！");

        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.3f);

            // 路人快速衝撞玩家
            float elapsed = 0f;
            Vector3 startPos = passerby.position;
            Vector3 targetPos = player != null ? player.position : startPos;

            while (elapsed < 1.0f)
            {
                elapsed += Time.deltaTime * (passerbySpeed / Mathf.Max(0.5f, Vector3.Distance(startPos, targetPos)));
                passerby.position = Vector3.Lerp(startPos, targetPos, elapsed);
                yield return null;
            }
        }

        // 畫面轉黑
        narrativeSubtitle = "門外路人吸到你產生的二手菸，憤怒地將你擊倒！\n\n【二手菸害人害己，遊戲失敗】";
        showSubtitle = true;

        float fade = 0f;
        while (fade < 1f)
        {
            fade += Time.deltaTime * 3f;
            blackFadeAlpha = Mathf.Clamp01(fade);
            yield return null;
        }

        yield return new WaitForSeconds(3.5f);

        // 重新載入場景
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    // --- 路線 B 演出：直接進入街道，通知 RouteManager ---
    private IEnumerator RouteB_EnterStreetRoutine()
    {
        Debug.Log("[路線 B] 成功走出倉庫！玩家進入主要街道！");

        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);
        }

        // 關鍵：廣播通知 A 組員的 RouteManager / GameManager
        OnOpeningFinished?.Invoke();

        // 也可以透過 SendMessage 或尋找 RouteManager 防呆調用 (企劃書第 10.4 節)
        GameObject routeMgr = GameObject.Find("RouteManager");
        if (routeMgr != null)
        {
            routeMgr.SendMessage("OnOpeningFinished", SendMessageOptions.DontRequireReceiver);
        }

        yield return null;
    }
}