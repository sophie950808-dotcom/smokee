using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// 【A15】拒絕幫忙時在玩家附近補生成菸怪（視覺上的壓力）。掛在 GameManager 物件上（和 PhaseManager 同一個）。
/// - 拒絕期間每 _spawnInterval 秒，在玩家周圍 _minDistance–_maxDistance 公尺隨機生成 1 隻（最多 _maxMonsters 隻）
/// - 生成的菸怪一出生就無法消除（SmokeSource 拒絕狀態），用「透明度」淡入
/// - 接下任務（或進入第三階段）時全部淡出後刪除
/// - 暫停選單、「暫停計時」期間不生成
/// 淡入淡出：遊戲執行中複製一份材質、暫時切成透明，淡入完成換回原本的材質。B 的 Prefab 與材質檔不會被修改。
/// 規格見 docs/INTERFACES.md 第 4 節「拒絕幫忙」。
/// </summary>
public class RefusalMonsterSpawner : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int SurfaceId = Shader.PropertyToID("_Surface");

    [Header("生成")]
    [Tooltip("要生成的菸怪 Prefab（B 的 SmokeMonster_Small）")]
    [SerializeField] private GameObject _monsterPrefab;

    [Tooltip("拒絕期間每隔幾秒生成 1 隻")]
    [SerializeField] private float _spawnInterval = 3f;

    [Tooltip("場上最多同時有幾隻補生成的菸怪（滿了就停止生成）")]
    [SerializeField] private int _maxMonsters = 15;

    [Tooltip("離玩家最近幾公尺（水平距離）")]
    [SerializeField] private float _minDistance = 2f;

    [Tooltip("離玩家最遠幾公尺（水平距離）")]
    [SerializeField] private float _maxDistance = 10f;

    [Header("淡入淡出（透明度）")]
    [Tooltip("淡入幾秒")]
    [SerializeField] private float _fadeInSeconds = 0.8f;

    [Tooltip("淡出幾秒（接下任務時）")]
    [SerializeField] private float _fadeOutSeconds = 0.8f;

    [Header("找生成位置")]
    [Tooltip("檢查地面、牆壁用的 Layer（預設全部）")]
    [SerializeField] private LayerMask _obstacleMask = ~0;

    [Tooltip("每次生成最多試幾個隨機位置；都不合適就這次不生成")]
    [SerializeField] private int _maxTries = 10;

    [Tooltip("生成點的地面高度和玩家腳底最多差幾公尺（避免生到屋頂上）")]
    [SerializeField] private float _maxHeightDifference = 1.5f;

    [Tooltip("生成點周圍要空出多大的半徑（避免卡進牆裡）")]
    [SerializeField] private float _clearanceRadius = 0.6f;

    /// <summary>一個可以調透明度的材質複製品：顏色屬性與原本的顏色。</summary>
    private class FadableMaterial
    {
        public Material Material;
        public int ColorId;
        public Color BaseColor;
    }

    /// <summary>
    /// 一隻菸怪淡入淡出時的材質資料：原本的材質、暫時的複製品（全部）、其中可以調透明度的那些。
    /// </summary>
    private class FadeState
    {
        public readonly List<Renderer> Renderers = new List<Renderer>();
        public readonly List<Material[]> Originals = new List<Material[]>();
        public readonly List<Material> Instances = new List<Material>();
        public readonly List<FadableMaterial> Fadables = new List<FadableMaterial>();
        public Coroutine Routine;
    }

    private readonly List<GameObject> _spawned = new List<GameObject>();
    private readonly Dictionary<GameObject, FadeState> _fades = new Dictionary<GameObject, FadeState>();
    private PhaseManager _subscribedPhaseManager;
    private Transform _player;
    private float _spawnTimer;
    private int _spawnCount;

    /// <summary>場景中的補生成器（DevOverlay 讀取用）；沒有時 null。</summary>
    public static RefusalMonsterSpawner Instance { get; private set; }

    /// <summary>場上補生成的菸怪數量（不含正在淡出的）。</summary>
    public int ActiveCount
    {
        get
        {
            RemoveDestroyed();
            return _spawned.Count;
        }
    }

    /// <summary>最多幾隻。</summary>
    public int MaxMonsters => _maxMonsters;

    /// <summary>拒絕中距離下一隻生成還有幾秒；沒有拒絕或已滿時 0。</summary>
    public float NextSpawnRemaining
    {
        get
        {
            PhaseManager phaseManager = PhaseManager.Instance;
            if (phaseManager == null || !phaseManager.IsHelpRefused || ActiveCount >= _maxMonsters)
            {
                return 0f;
            }
            return Mathf.Max(0f, _spawnInterval - _spawnTimer);
        }
    }

    /// <summary>每次進入 Play 時清掉 static 的 Instance。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Instance = null;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[RefusalMonsterSpawner] 場景中有超過一個 RefusalMonsterSpawner，請只保留一個。", this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (Instance != this)
        {
            return;
        }

        // PhaseManager.Instance 在 Awake 就設定好了
        _subscribedPhaseManager = PhaseManager.Instance;
        if (_subscribedPhaseManager != null)
        {
            _subscribedPhaseManager.OnHelpRefusedChanged += HandleHelpRefusedChanged;
        }
        else
        {
            Debug.LogWarning("[RefusalMonsterSpawner] 場景裡找不到 PhaseManager，拒絕時不會補生成菸怪。", this);
        }

        if (_monsterPrefab == null)
        {
            Debug.LogWarning("[RefusalMonsterSpawner] 沒有設定菸怪 Prefab，拒絕時不會補生成菸怪。", this);
        }
    }

    private void OnDestroy()
    {
        if (_subscribedPhaseManager != null)
        {
            _subscribedPhaseManager.OnHelpRefusedChanged -= HandleHelpRefusedChanged;
        }

        // 清掉還沒換回來的透明材質複製品（場景結束時物件會一起被刪掉）
        foreach (FadeState state in _fades.Values)
        {
            DestroyInstances(state);
        }
        _fades.Clear();

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (Instance != this || _monsterPrefab == null)
        {
            return;
        }

        PhaseManager phaseManager = PhaseManager.Instance;
        GameManager gameManager = GameManager.Instance;
        if (phaseManager == null || !phaseManager.IsHelpRefused)
        {
            return;
        }
        // 暫停選單、暫停計時（對話、「要幫忙嗎？」視窗…）期間不生成
        if (gameManager == null || gameManager.State != GameState.Playing || gameManager.IsTimerPaused)
        {
            return;
        }

        if (ActiveCount >= _maxMonsters)
        {
            return;
        }

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _spawnInterval)
        {
            _spawnTimer = 0f;
            TrySpawn();
        }
    }

    private void HandleHelpRefusedChanged(bool refused)
    {
        _spawnTimer = 0f;
        if (!refused)
        {
            FadeOutAll();
        }
    }

    /// <summary>在玩家周圍找一個合適的位置生成 1 隻；找不到就這次不生成。</summary>
    private void TrySpawn()
    {
        Transform player = GetPlayer();
        if (player == null)
        {
            return;
        }

        if (!TryFindSpawnPoint(player.position, out Vector3 point))
        {
            Debug.Log("[RefusalMonsterSpawner] 玩家附近找不到合適的生成位置，這次不生成。", this);
            return;
        }

        Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
        GameObject monster = Instantiate(_monsterPrefab, point, rotation);
        _spawnCount++;
        monster.name = _monsterPrefab.name + "_Refusal" + _spawnCount;
        _spawned.Add(monster);
        StartFade(monster, 0f, 1f, _fadeInSeconds, false);
    }

    /// <summary>
    /// 在玩家周圍隨機找點：往下打射線找地面、高度差不能太大、周圍沒有障礙物、和玩家之間沒有牆擋住。
    /// </summary>
    private bool TryFindSpawnPoint(Vector3 playerPosition, out Vector3 point)
    {
        float minDistance = Mathf.Max(0f, _minDistance);
        float maxDistance = Mathf.Max(minDistance, _maxDistance);
        float rayHeight = _maxHeightDifference + 3f;

        for (int i = 0; i < Mathf.Max(1, _maxTries); i++)
        {
            // 依面積平均分布（不會集中在玩家旁邊）
            float distance = Mathf.Sqrt(Random.Range(minDistance * minDistance, maxDistance * maxDistance));
            float angle = Random.Range(0f, Mathf.PI * 2f);
            Vector3 candidate = playerPosition + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * distance;

            if (!Physics.Raycast(candidate + Vector3.up * rayHeight, Vector3.down, out RaycastHit hit,
                    rayHeight * 2f, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }
            if (Mathf.Abs(hit.point.y - playerPosition.y) > _maxHeightDifference)
            {
                continue;
            }

            Vector3 ground = hit.point;
            Vector3 clearanceCenter = ground + Vector3.up * (_clearanceRadius + 0.3f);
            if (Physics.CheckSphere(clearanceCenter, _clearanceRadius, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }
            if (Physics.Linecast(playerPosition + Vector3.up, ground + Vector3.up, _obstacleMask, QueryTriggerInteraction.Ignore))
            {
                continue;
            }

            point = ground;
            return true;
        }

        point = Vector3.zero;
        return false;
    }

    /// <summary>接下任務：所有補生成的菸怪停止冒煙、不能被打，淡出後刪除。</summary>
    private void FadeOutAll()
    {
        RemoveDestroyed();
        for (int i = 0; i < _spawned.Count; i++)
        {
            GameObject monster = _spawned[i];

            SmokeSource source = monster.GetComponent<SmokeSource>();
            if (source != null)
            {
                source.IsEmitting = false;
            }
            foreach (Collider collider in monster.GetComponentsInChildren<Collider>())
            {
                collider.enabled = false;
            }
            foreach (ParticleSystem particles in monster.GetComponentsInChildren<ParticleSystem>())
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            StartFade(monster, 1f, 0f, _fadeOutSeconds, true);
        }
        // 清單清空：淡出中的菸怪不再算在場上（刪除由淡出完成時處理）
        _spawned.Clear();
    }

    /// <summary>開始淡入或淡出。同一隻已經在淡入中時，先換回原本材質再重新開始。</summary>
    private void StartFade(GameObject monster, float from, float to, float duration, bool destroyAtEnd)
    {
        if (_fades.TryGetValue(monster, out FadeState oldState))
        {
            if (oldState.Routine != null)
            {
                StopCoroutine(oldState.Routine);
            }
            RestoreOriginals(oldState);
            _fades.Remove(monster);
        }

        FadeState state = CreateFadeState(monster);
        _fades[monster] = state;
        SetAlpha(state, from);
        state.Routine = StartCoroutine(FadeRoutine(monster, state, from, to, duration, destroyAtEnd));
    }

    private IEnumerator FadeRoutine(GameObject monster, FadeState state, float from, float to, float duration, bool destroyAtEnd)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            if (monster == null)
            {
                // 被別的系統刪掉了（例如 B 的菸怪被擊敗）
                DestroyInstances(state);
                _fades.Remove(monster);
                yield break;
            }
            elapsed += Time.deltaTime;
            SetAlpha(state, Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / Mathf.Max(0.01f, duration))));
            yield return null;
        }

        _fades.Remove(monster);
        if (monster == null)
        {
            DestroyInstances(state);
            yield break;
        }

        if (destroyAtEnd)
        {
            SetAlpha(state, 0f);
            Destroy(monster);
            DestroyInstances(state);
        }
        else
        {
            // 淡入完成：換回 B 原本的材質（和其他菸怪看起來一樣）
            RestoreOriginals(state);
        }
    }

    /// <summary>
    /// 把這隻菸怪的 Renderer 材質換成複製品，有顏色屬性的切成透明。
    /// 整個 Renderer 都沒有顏色屬性時不處理（保持原本材質，不會淡入淡出）。
    /// </summary>
    private static FadeState CreateFadeState(GameObject monster)
    {
        FadeState state = new FadeState();
        foreach (Renderer renderer in monster.GetComponentsInChildren<Renderer>())
        {
            Material[] originals = renderer.sharedMaterials;
            if (!HasAnyColorProperty(originals))
            {
                continue;
            }

            Material[] instances = new Material[originals.Length];
            for (int i = 0; i < originals.Length; i++)
            {
                Material original = originals[i];
                if (original == null)
                {
                    continue;
                }

                Material instance = new Material(original);
                instance.name = original.name + " (Fade)";
                instances[i] = instance;
                state.Instances.Add(instance);

                int colorId = FindColorProperty(instance);
                if (colorId != 0)
                {
                    MakeTransparent(instance);
                    state.Fadables.Add(new FadableMaterial
                    {
                        Material = instance,
                        ColorId = colorId,
                        BaseColor = instance.GetColor(colorId),
                    });
                }
            }

            state.Renderers.Add(renderer);
            state.Originals.Add(originals);
            renderer.sharedMaterials = instances;
        }
        return state;
    }

    /// <summary>設定透明度（0–1，乘上材質原本的透明度）。</summary>
    private static void SetAlpha(FadeState state, float alpha)
    {
        for (int i = 0; i < state.Fadables.Count; i++)
        {
            FadableMaterial fadable = state.Fadables[i];
            if (fadable.Material == null)
            {
                continue;
            }
            Color color = fadable.BaseColor;
            color.a *= alpha;
            fadable.Material.SetColor(fadable.ColorId, color);
        }
    }

    private static bool HasAnyColorProperty(Material[] materials)
    {
        foreach (Material material in materials)
        {
            if (material != null && FindColorProperty(material) != 0)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>換回原本的材質，刪掉透明的複製品。</summary>
    private static void RestoreOriginals(FadeState state)
    {
        for (int i = 0; i < state.Renderers.Count; i++)
        {
            if (state.Renderers[i] != null)
            {
                state.Renderers[i].sharedMaterials = state.Originals[i];
            }
        }
        DestroyInstances(state);
    }

    private static void DestroyInstances(FadeState state)
    {
        for (int i = 0; i < state.Instances.Count; i++)
        {
            if (state.Instances[i] != null)
            {
                Destroy(state.Instances[i]);
            }
        }
        state.Instances.Clear();
        state.Fadables.Clear();
    }

    /// <summary>找材質的顏色屬性（URP 是 _BaseColor；其他 Shader 可能是 _Color、_TintColor）。找不到回傳 0。</summary>
    private static int FindColorProperty(Material material)
    {
        if (material.HasProperty(BaseColorId))
        {
            return BaseColorId;
        }
        if (material.HasProperty(ColorId))
        {
            return ColorId;
        }
        if (material.HasProperty(TintColorId))
        {
            return TintColorId;
        }
        return 0;
    }

    /// <summary>
    /// 把 URP Lit／Unlit 的不透明材質（複製品）切成透明（Alpha 混合）。已經是透明的材質（例如粒子）不用改。
    /// </summary>
    private static void MakeTransparent(Material material)
    {
        if (!material.HasProperty(SurfaceId) || material.GetFloat(SurfaceId) >= 0.5f)
        {
            return;
        }

        material.SetFloat(SurfaceId, 1f);
        material.SetFloat("_Blend", 0f);
        material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
        material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
        material.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
        material.SetFloat("_ZWrite", 0f);
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = (int)RenderQueue.Transparent;
    }

    private void RemoveDestroyed()
    {
        _spawned.RemoveAll(monster => monster == null);
    }

    private Transform GetPlayer()
    {
        if (_player == null)
        {
            PlayerController controller = FindFirstObjectByType<PlayerController>();
            if (controller != null)
            {
                _player = controller.transform;
            }
            else
            {
                Debug.LogWarning("[RefusalMonsterSpawner] 場景裡找不到 PlayerController，無法補生成菸怪。", this);
            }
        }
        return _player;
    }

    private void OnValidate()
    {
        _spawnInterval = Mathf.Max(0.1f, _spawnInterval);
        _maxMonsters = Mathf.Max(0, _maxMonsters);
        _minDistance = Mathf.Max(0f, _minDistance);
        _maxDistance = Mathf.Max(_minDistance, _maxDistance);
        _fadeInSeconds = Mathf.Max(0f, _fadeInSeconds);
        _fadeOutSeconds = Mathf.Max(0f, _fadeOutSeconds);
        _maxTries = Mathf.Max(1, _maxTries);
        _clearanceRadius = Mathf.Max(0.05f, _clearanceRadius);
    }
}
