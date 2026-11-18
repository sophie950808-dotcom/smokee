using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 死巷開場雙路線控制器 (任務 B.3)
/// 企劃書規格：3.2 ~ 3.4 節
/// </summary>
public class OpeningSequence : MonoBehaviour
{
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

    // [I1 整合] 路線選擇改由 RouteManager 記錄（RouteManager.Instance.ChosenRoute），這裡不再另存一份

    private bool isDoorOpen = false;
    private bool hasTriggeredExit = false;

    // 畫面轉黑遮罩與文字
    private float blackFadeAlpha = 0f;
    private string narrativeSubtitle = "";
    private bool showSubtitle = false;

    private void Start()
    {
        // 路人一直存在於場景中，不隱藏

        // 朋友初始先不冒煙
        if (npcSmokeParticles != null)
        {
            npcSmokeParticles.Stop();
        }

        // [I1 整合] 監聽 RouteManager：玩家選好路線時播放演出
        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.OnRouteChosen += HandleRouteChosen;
        }
        else
        {
            Debug.LogWarning("[開場演出] 場景裡找不到 RouteManager，開場無法運作。", this);
        }
    }

    private void OnDestroy()
    {
        // [I1 整合] 物件被刪除時取消監聽
        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.OnRouteChosen -= HandleRouteChosen;
        }
    }

    /// <summary>
    /// [I1 整合] RouteManager 通知玩家選好路線時呼叫：開門；拒絕路線時朋友點菸冒煙。
    /// （原本寫在選擇框按鈕裡的內容搬到這裡）
    /// </summary>
    private void HandleRouteChosen(OpeningRoute route)
    {
        if (route == OpeningRoute.RefuseSmoke)
        {
            // 朋友點起菸成為死巷第一個煙霧來源
            if (npcSmokeParticles != null) npcSmokeParticles.Play();
            if (smokingNPC != null)
            {
                SmokeSource src = smokingNPC.GetComponentInChildren<SmokeSource>(); // [I1 整合] SmokeSource 掛在子物件 Capsule 上
                if (src != null) src.IsEmitting = true;
            }
        }
        StartCoroutine(OpenDoorSequence());
    }

    private void OnGUI()
    {
        // 1. 車庫新手教學提示文字 (往下移到 Y=115，完全避開頂部 HUD 資訊列)
        if (!hasTriggeredExit && RouteManager.Instance != null && RouteManager.Instance.ChosenRoute != OpeningRoute.None)
        {
            GUIStyle guideStyle = new GUIStyle(GUI.skin.box);
            guideStyle.fontSize = 18;
            guideStyle.alignment = TextAnchor.MiddleCenter;
            guideStyle.normal.textColor = Color.yellow;

            int gWidth = 620;
            int gHeight = 58;
            float gx = (Screen.width - gWidth) / 2f;
            GUI.Box(new Rect(gx, 115, gWidth, gHeight), "【新手引導】靠近吸菸者會吸入二手菸，視野會變暗！\n請操控角色走出車庫展開街道任務！", guideStyle);
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

        // [I1 整合] 門開了，恢復玩家控制讓玩家走出車庫（RouteManager 在場景開始時把控制關掉了）
        PlayerController controller = player != null
            ? player.GetComponent<PlayerController>()
            : FindFirstObjectByType<PlayerController>();
        if (controller != null) controller.SetControlEnabled(true);

        Debug.Log("[開場演出] 車庫門開啟，請操控角色走出車庫！");
    }

    /// <summary>
    /// 玩家撞到 GarageExitTrigger 時被調用
    /// </summary>
    public void OnPlayerReachedExit()
    {
        if (!isDoorOpen || hasTriggeredExit) return;
        hasTriggeredExit = true;

        // [I1 整合] 改讀 RouteManager 記錄的路線
        OpeningRoute route = RouteManager.Instance != null ? RouteManager.Instance.ChosenRoute : OpeningRoute.None;

        if (route == OpeningRoute.AcceptSmoke)
        {
            StartCoroutine(RouteA_PunishAndDieRoutine());
        }
        else if (route == OpeningRoute.RefuseSmoke)
        {
            StartCoroutine(RouteB_EnterStreetRoutine());
        }
    }

    // --- 路線 A 演出：鏡頭強制對準路人 ➔ 路人衝撞玩家 ➔ 倒地黑屏 ---
    private IEnumerator RouteA_PunishAndDieRoutine()
    {
        // 1. 鎖定玩家操作
        PlayerController controller = player != null
            ? player.GetComponent<PlayerController>()
            : FindFirstObjectByType<PlayerController>();
        if (controller != null) controller.SetControlEnabled(false);

        // 2. 原本的 Passerby 直接衝過來撞玩家
        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);

            // 【核心修正】強制鏡頭和玩家轉身正對路人！
            Vector3 lookAtPasserby = (passerby.position - player.position);
            lookAtPasserby.y = 0;
            if (lookAtPasserby != Vector3.zero && player != null)
            {
                player.rotation = Quaternion.LookRotation(lookAtPasserby);
            }
            // 【雙人全景側拍攝影機】同時拍到玩家與路人全身！
            if (Camera.main != null && player != null)
            {
                Camera.main.fieldOfView = 85f;
                // 1. 計算兩人的中點
                Vector3 midPoint = (player.position + passerby.position) * 0.5f;
                // 2. 計算兩人的連線方向與垂直側面方向
                Vector3 betweenDir = (passerby.position - player.position).normalized;
                Vector3 sideDir = Vector3.Cross(betweenDir, Vector3.up).normalized; // 垂直側面

                // 3. 攝影機擺在側面 4.5 公尺處、抬高 1.8 公尺
                Vector3 camPos = midPoint + sideDir * 4.5f + Vector3.up * 1.8f;
                Camera.main.transform.position = camPos;
                Camera.main.transform.LookAt(midPoint + Vector3.up * 0.9f);
            }

            // 停頓 0.35 秒，讓玩家看清楚路人全貌與廣角街景
            yield return new WaitForSeconds(0.35f);

            Vector3 targetPos = player != null ? player.position : passerby.position;

            // 朝向玩家衝過來 (速度 3.0 m/s，全程都在鏡頭正中央)
            while (Vector3.Distance(passerby.position, targetPos) > 0.6f)
            {
                passerby.position = Vector3.MoveTowards(passerby.position, targetPos, 3.0f * Time.deltaTime);
                Vector3 dir = (targetPos - passerby.position);
                dir.y = 0;
                if (dir != Vector3.zero) passerby.rotation = Quaternion.LookRotation(dir);
                yield return null;
            }
        }

        // 3. 玩家被撞倒地
        if (player != null)
        {
            float fallElapsed = 0f;
            Quaternion startRot = player.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(85f, 0f, 0f); // 倒地
            Vector3 pStart = player.position;

            while (fallElapsed < 0.25f)
            {
                fallElapsed += Time.deltaTime;
                float ft = fallElapsed / 0.25f;
                player.rotation = Quaternion.Slerp(startRot, targetRot, ft);
                player.position = Vector3.Lerp(pStart, pStart + Vector3.down * 0.5f, ft);
                yield return null;
            }
        }

        // 4. 畫面轉黑
        narrativeSubtitle = "門外路人吸到你飄出的二手菸，憤怒地上前痛扁了你！\n\n【二手菸害人害己，遊戲即將重新開始】";
        showSubtitle = true;

        float fade = 0f;
        while (fade < 1f)
        {
            fade += Time.deltaTime * 4f;
            blackFadeAlpha = Mathf.Clamp01(fade);
            yield return null;
        }

        yield return new WaitForSeconds(1.5f);

        // 重新載入場景
        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.NotifyOpeningFinished();
        }
    }

    // --- 路線 B 演出：直接進入街道，通知 RouteManager ---
    private IEnumerator RouteB_EnterStreetRoutine()
    {
        Debug.Log("[路線 B] 成功走出倉庫！玩家進入主要街道！");

        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);
        }

        // 關鍵：通知 A 組員的 RouteManager 開場結束
        // [I1 整合] 改成直接呼叫 RouteManager（原本的 static 事件與 SendMessage 沒有人接收）
        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.NotifyOpeningFinished();
        }

        yield return null;
    }
}