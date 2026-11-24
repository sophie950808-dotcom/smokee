using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 死巷開場雙路線控制器 (任務 B.3 & B7)
/// 企劃書規格：3.2 ~ 3.4 節，接受路線改為隨機一位路人（咳嗽 → 台詞 → 動手 → 暈倒）
/// </summary>
public class OpeningSequence : MonoBehaviour
{
    [System.Serializable]
    public class PasserbyProfile
    {
        public string roleName;       // 角色稱呼
        public string attackMethod;   // 動手方式
        public string line;           // 台詞
    }

    [Header("隨機路人名單 (企劃書規格 3.2)")]
    [SerializeField]
    private PasserbyProfile[] _passerbyProfiles = new PasserbyProfile[]
    {
        new PasserbyProfile { roleName = "老奶奶", attackMethod = "拿拐杖打", line = "「肖年欸！年紀輕輕就抽菸是想看我提早死翹翹嗎！」" },
        new PasserbyProfile { roleName = "小朋友", attackMethod = "拿棒棒糖丟", line = "「你好臭！你好臭！」" },
        new PasserbyProfile { roleName = "爸爸", attackMethod = "拿衣架打", line = "「兒子阿！你怎麼在抽菸！看我不打死你！」" }
    };

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

    [Header("接受吸菸演出設定（GameScene）")]
    [Tooltip("勾選後接受路線播放教訓、倒地與文字說明，再回到開場選擇；其他場景仍保留原演出")]
    [SerializeField] private bool _acceptSmokeShowsResult = true; // 預設開啟新版演出
    [Tooltip("門口 NPC 的拳頭或教訓工具物件；由場景設定工具接好")]
    [SerializeField] private Transform _punchHand;
    [Tooltip("NPC 與玩家的水平攻擊距離")]
    [SerializeField] private float _attackDistance = 1.2f;
    [Tooltip("出拳/動手後到命中的秒數")]
    [SerializeField] private float _punchDuration = 0.3f;

    private bool isDoorOpen = false;
    private bool hasTriggeredExit = false;

    // 畫面轉黑遮罩與文字
    private float blackFadeAlpha = 0f;
    private string narrativeSubtitle = "";
    private bool showSubtitle = false;

    private void Start()
    {
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
        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.OnRouteChosen -= HandleRouteChosen;
        }
    }

    /// <summary>
    /// RouteManager 通知玩家選好路線時呼叫
    /// </summary>
    private void HandleRouteChosen(OpeningRoute route)
    {
        if (route == OpeningRoute.RefuseSmoke || _acceptSmokeShowsResult)
        {
            // 朋友點起菸成為死巷第一個煙霧來源
            if (npcSmokeParticles != null) npcSmokeParticles.Play();
            if (smokingNPC != null)
            {
                SmokeSource src = smokingNPC.GetComponentInChildren<SmokeSource>();
                if (src != null) src.IsEmitting = true;
            }
        }
        StartCoroutine(OpenDoorSequence());
    }

    private void OnGUI()
    {
        // 1. 車庫新手教學提示文字
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

        // 2. 路線 A：畫面轉黑 + 字幕提示（隨機路人台詞與教訓說明）
        if (blackFadeAlpha > 0f)
        {
            Color oldColor = GUI.color;
            GUI.color = new Color(0, 0, 0, blackFadeAlpha);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = oldColor;

            if (showSubtitle)
            {
                GUIStyle subStyle = new GUIStyle(GUI.skin.label);
                subStyle.fontSize = 24;
                subStyle.alignment = TextAnchor.MiddleCenter;
                subStyle.fontStyle = FontStyle.Bold;
                subStyle.normal.textColor = new Color(1f, 0.4f, 0.4f); // 亮紅警示色

                GUI.Label(new Rect(0, Screen.height / 2f - 100, Screen.width, 200), narrativeSubtitle, subStyle);
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

        OpeningRoute route = RouteManager.Instance != null ? RouteManager.Instance.ChosenRoute : OpeningRoute.None;

        if (route == OpeningRoute.AcceptSmoke)
        {
            // 隨機抽選一位路人 (老奶奶 / 小朋友 / 爸爸)
            PasserbyProfile selected = _passerbyProfiles[UnityEngine.Random.Range(0, _passerbyProfiles.Length)];

            StartCoroutine(_acceptSmokeShowsResult
                ? RouteA_PunchAndRestartRoutine(selected)
                : RouteA_PunishAndDieRoutine(selected));
        }
        else if (route == OpeningRoute.RefuseSmoke)
        {
            StartCoroutine(RouteB_EnterStreetRoutine());
        }
    }

    // 企畫書新版：隨機路人 咳嗽 → 台詞 → 動手 → 倒地 → 回到選擇
    private IEnumerator RouteA_PunchAndRestartRoutine(PasserbyProfile profile)
    {
        PlayerController controller = player != null ? player.GetComponent<PlayerController>() : null;
        if (player == null || passerby == null || controller == null || _punchHand == null)
        {
            Debug.LogError("[開場演出] 接受路線缺少玩家、路人或攻擊物件引用。", this);
            yield break;
        }

        controller.SetControlEnabled(false);
        controller.enabled = false;
        CharacterController character = player.GetComponent<CharacterController>();
        if (character != null) character.enabled = false;
        foreach (Renderer body in player.GetComponentsInChildren<Renderer>(true)) body.enabled = true;
        passerby.gameObject.SetActive(true);

        // 攝影機對準路人與玩家
        Vector3 towardNpc = passerby.position - player.position;
        towardNpc.y = 0f;
        if (towardNpc.sqrMagnitude > 0.001f) player.rotation = Quaternion.LookRotation(towardNpc);
        Vector3 side = Vector3.Cross(towardNpc.normalized, Vector3.up);
        Camera camera = Camera.main;
        if (camera != null)
        {
            camera.fieldOfView = 65f;
            Vector3 focus = (player.position + passerby.position) * 0.5f;
            focus.y = player.position.y + 0.9f;
            camera.transform.position = focus + side * Mathf.Max(4.5f, towardNpc.magnitude * 0.8f) + Vector3.up * 1.8f;
            camera.transform.LookAt(focus);
        }
        yield return new WaitForSeconds(0.35f);

        Debug.Log($"[接受路線] 路人【{profile.roleName}】聞到二手菸狂咳，衝過來！", this);

        // 路人跑向玩家
        Vector3 npcStart = passerby.position;
        float elapsed = 0f;
        while (true)
        {
            Vector3 direction = player.position - passerby.position;
            direction.y = 0f;
            if (direction.magnitude <= _attackDistance) break;
            passerby.rotation = Quaternion.LookRotation(direction);
            Vector3 target = new Vector3(player.position.x, npcStart.y, player.position.z);
            Vector3 next = Vector3.MoveTowards(passerby.position, target,
                Mathf.Max(0.1f, passerbySpeed) * Time.deltaTime);
            elapsed += Time.deltaTime;
            next.y = npcStart.y + Mathf.Abs(Mathf.Sin(elapsed * 14f)) * 0.08f;
            passerby.position = next;
            yield return null;
        }
        passerby.position = new Vector3(passerby.position.x, npcStart.y, passerby.position.z);
        Vector3 toPlayer = player.position - passerby.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude > 0.001f) passerby.rotation = Quaternion.LookRotation(toPlayer);

        // 動手教訓玩家 (拐杖/棒棒糖/衣架/揮拳)
        _punchHand.gameObject.SetActive(true);
        Vector3 handRest = _punchHand.localPosition;
        Vector3 handHit = new Vector3(0.15f, handRest.y, _attackDistance - 0.15f);
        float punchSeconds = Mathf.Max(0.05f, _punchDuration);
        for (float t = 0f; t < punchSeconds; t += Time.deltaTime)
        {
            _punchHand.localPosition = Vector3.Lerp(handRest, handHit, t / punchSeconds);
            yield return null;
        }
        _punchHand.localPosition = handHit;
        Debug.Log($"[接受路線] {profile.roleName}【{profile.attackMethod}】命中玩家！", this);

        // 玩家倒地
        Quaternion startRotation = player.rotation;
        Vector3 startPosition = player.position;
        for (float t = 0f; t < 0.45f; t += Time.deltaTime)
        {
            float fraction = Mathf.SmoothStep(0f, 1f, t / 0.45f);
            player.rotation = Quaternion.Slerp(startRotation, startRotation * Quaternion.Euler(-85f, 0f, 0f), fraction);
            player.position = startPosition + Vector3.up * (0.4f * fraction);
            _punchHand.localPosition = Vector3.Lerp(handHit, handRest, fraction);
            yield return null;
        }
        player.rotation = startRotation * Quaternion.Euler(-85f, 0f, 0f);
        player.position = startPosition + Vector3.up * 0.4f;
        yield return new WaitForSeconds(0.6f);

        // 畫面漸黑
        while (blackFadeAlpha < 1f)
        {
            blackFadeAlpha = Mathf.Clamp01(blackFadeAlpha + Time.deltaTime * 2f);
            yield return null;
        }

        // 顯示企劃書規範台詞與教訓說明
        narrativeSubtitle = $"{profile.roleName}咳了幾聲大罵：\n{profile.line}\n然後{profile.attackMethod}將你擊倒！\n\n【二手菸會危害周遭的人！請重新選擇】";
        showSubtitle = true;

        yield return new WaitForSeconds(3.5f);

        // 通知 RouteManager 重置回開場選擇（沿用 RouteManager，不重複重載場景）
        if (RouteManager.Instance != null)
            RouteManager.Instance.NotifyOpeningFinished();
    }

    // 保留相容舊演出，同樣帶入隨機路人
    private IEnumerator RouteA_PunishAndDieRoutine(PasserbyProfile profile)
    {
        PlayerController controller = player != null
            ? player.GetComponent<PlayerController>()
            : FindFirstObjectByType<PlayerController>();
        if (controller != null) controller.SetControlEnabled(false);

        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);
            Vector3 lookAtPasserby = (passerby.position - player.position);
            lookAtPasserby.y = 0;
            if (lookAtPasserby != Vector3.zero && player != null)
            {
                player.rotation = Quaternion.LookRotation(lookAtPasserby);
            }
            if (Camera.main != null && player != null)
            {
                Camera.main.fieldOfView = 85f;
                Vector3 midPoint = (player.position + passerby.position) * 0.5f;
                Vector3 betweenDir = (passerby.position - player.position).normalized;
                Vector3 sideDir = Vector3.Cross(betweenDir, Vector3.up).normalized;
                Camera.main.transform.position = midPoint + sideDir * 4.5f + Vector3.up * 1.8f;
                Camera.main.transform.LookAt(midPoint + Vector3.up * 0.9f);
            }

            yield return new WaitForSeconds(0.35f);

            Vector3 targetPos = player != null ? player.position : passerby.position;
            while (Vector3.Distance(passerby.position, targetPos) > 0.6f)
            {
                passerby.position = Vector3.MoveTowards(passerby.position, targetPos, 3.0f * Time.deltaTime);
                Vector3 dir = (targetPos - passerby.position);
                dir.y = 0;
                if (dir != Vector3.zero) passerby.rotation = Quaternion.LookRotation(dir);
                yield return null;
            }
        }

        if (player != null)
        {
            float fallElapsed = 0f;
            Quaternion startRot = player.rotation;
            Quaternion targetRot = startRot * Quaternion.Euler(85f, 0f, 0f);
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

        narrativeSubtitle = $"{profile.roleName}：{profile.line}\n\n門外路人吸到二手菸，{profile.attackMethod}教訓了你！\n【二手菸害人害己，遊戲即將重新開始】";
        showSubtitle = true;

        float fade = 0f;
        while (fade < 1f)
        {
            fade += Time.deltaTime * 4f;
            blackFadeAlpha = Mathf.Clamp01(fade);
            yield return null;
        }

        yield return new WaitForSeconds(3.5f);

        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.NotifyOpeningFinished();
        }
    }

    private IEnumerator RouteB_EnterStreetRoutine()
    {
        Debug.Log("[路線 B] 成功走出倉庫！玩家進入主要街道！");
        if (passerby != null)
        {
            passerby.gameObject.SetActive(true);
        }

        if (RouteManager.Instance != null)
        {
            RouteManager.Instance.NotifyOpeningFinished();
        }

        yield return null;
    }
}