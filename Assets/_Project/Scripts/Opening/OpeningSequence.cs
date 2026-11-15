using System.Collections;
using UnityEngine;

public class OpeningSequence : MonoBehaviour
{
    [Header("角色與物件引用")]
    [Tooltip("吸菸 NPC (Capsule)")]
    public Transform smokingNPC;
    [Tooltip("揍人路人 (Capsule)")]
    public Transform passerby;
    [Tooltip("車庫門物件 (要往上升起的門)")]
    public Transform garageDoor;
    [Tooltip("玩家物件")]
    public Transform player;

    [Header("車庫門開門設定")]
    [Tooltip("車庫門開門時向上移動的高度 (公尺)")]
    public float doorOpenHeight = 3.5f;
    [Tooltip("開門所需時間 (秒)")]
    public float doorOpenSpeed = 2.0f;

    [Header("路人設定")]
    [Tooltip("路人衝向玩家速度")]
    public float passerbySpeed = 5.0f;

    public enum RouteChoice
    {
        None,
        Accept, // 路線 A：抽菸
        Refuse  // 路線 B：拒絕
    }
    public RouteChoice currentChoice = RouteChoice.None;

    private bool isDoorOpen = false;
    private bool hasTriggeredExitEvent = false;

    // 畫面轉黑與文字提示
    private float blackFadeAlpha = 0f;
    private string narrativeMessage = "";
    private bool showNarrative = false;

    private void Start()
    {
        // 初始時路人先隱藏或靜止在車庫外不遠處
        if (passerby != null)
        {
            passerby.gameObject.SetActive(false);
        }
    }

    private void OnGUI()
    {
        // 1. 開場選擇 UI (還沒做選擇時顯示)
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
                currentChoice = RouteChoice.Accept;
                StartCoroutine(OpenDoorRoutine());
            }

            if (GUI.Button(new Rect(centerX + 260, centerY + 120, 160, 50), "拒絕 (不抽)", buttonStyle))
            {
                currentChoice = RouteChoice.Refuse;
                StartCoroutine(OpenDoorRoutine());
            }
        }

        // 2. 路線 A：被揍後的黑屏懲罰演出
        if (blackFadeAlpha > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, blackFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            if (showNarrative)
            {
                GUIStyle msgStyle = new GUIStyle(GUI.skin.label);
                msgStyle.fontSize = 24;
                msgStyle.alignment = TextAnchor.MiddleCenter;
                msgStyle.normal.textColor = Color.red;

                GUI.Label(new Rect(0, Screen.height / 2f - 60, Screen.width, 120), narrativeMessage, msgStyle);
            }
        }
    }

    /// <summary>
    /// 車庫門打開協程
    /// </summary>
    private IEnumerator OpenDoorRoutine()
    {
        Debug.Log("[開場] 選擇完畢，車庫門正在升起...");

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
        Debug.Log("[開場] 車庫門已打開！請操控玩家走出車庫。");
    }

    /// <summary>
    /// 當玩家走出車庫踩到門口觸發器時被呼叫
    /// </summary>
    public void OnPlayerExitedGarage()
    {
        if (!isDoorOpen || hasTriggeredExitEvent) return;
        hasTriggeredExitEvent = true;

        if (currentChoice == RouteChoice.Accept)
        {
            StartCoroutine(RouteA_EncounterAngryPasserby());
        }
        else if (currentChoice == RouteChoice.Refuse)
        {
            StartCoroutine(RouteB_EncounterNormalPasserby());
        }
    }

    // 路線 A：走出車庫後遇到路人被揍
    private IEnumerator RouteA_EncounterAngryPasserby()
    {
        Debug.Log("[路線 A] 玩家走出車庫，身上散發二手菸！");
        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);
            yield return new WaitForSeconds(0.5f);

            // 路人衝向玩家
            float timer = 0f;
            Vector3 startPos = passerby.position;
            Vector3 targetPos = player != null ? player.position : startPos;

            while (timer < 1f)
            {
                timer += Time.deltaTime * (passerbySpeed / Mathf.Max(0.1f, Vector3.Distance(startPos, targetPos)));
                passerby.position = Vector3.Lerp(startPos, targetPos, timer);
                yield return null;
            }
        }

        // 黑屏懲罰
        narrativeMessage = "門外的路人吸到你剛抽的二手菸，引發強烈不滿上前痛扁了你！\n\n【二手菸在室內室外都會對他人造成嚴重危害】";
        showNarrative = true;

        float fade = 0f;
        while (fade < 1f)
        {
            fade += Time.deltaTime * 2.5f;
            blackFadeAlpha = Mathf.Clamp01(fade);
            yield return null;
        }

        yield return new WaitForSeconds(3.5f);
        // 重新開始
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex
        );
    }

    // 路線 B：走出車庫後遇到一般路人，正式踏入街道
    private IEnumerator RouteB_EncounterNormalPasserby()
    {
        Debug.Log("[路線 B] 玩家走出車庫，空氣清新！路人正常經過。");
        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);
        }
        yield return null;
    }
}