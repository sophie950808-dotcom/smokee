using UnityEngine;

/// <summary>
/// 測試用假吸菸 NPC（只在 TestScene_A 用）：測試 PlayerInteractor 的兩個新功能。
/// 1. IInteractCondition：玩家面向它（前方 _angle 度內）、_distance 公尺內（水平距離）才會出現「按 E」提示。
/// 2. IQteInteractable：第一次按 E 開始 QTE（算第 1 下），之後每次按 E 由 PlayerInteractor 轉交過來，
///    Console 印出「第幾次連打」；_duration 秒內打滿 _requiredPresses 下成功（變灰、不能再互動），
///    超過時間或離開範圍就失敗、進度歸零，可以馬上重試。
/// 這是簡化版，只為了測 E 鍵轉交；真正的吸菸 NPC 是 A16 的 SmokerNPC。
/// 物件上需要有 Collider，PlayerInteractor 才找得到它。
/// </summary>
public class FakeSmokerNPC : MonoBehaviour, IQteInteractable, IInteractCondition
{
    [Header("互動")]
    [Tooltip("畫面上顯示的提示文字")]
    [SerializeField] private string _prompt = "按 E 捏熄菸蒂（測試）";

    [Tooltip("用來計算距離的位置。留空就用物件自己的位置")]
    [SerializeField] private Transform _interactPoint;

    [Header("捏熄判定")]
    [Tooltip("玩家要在幾公尺內（水平距離）")]
    [SerializeField] private float _distance = 2f;

    [Tooltip("玩家正前方幾度內（90 = 左右各 45 度）")]
    [SerializeField] private float _angle = 90f;

    [Header("QTE（簡化版）")]
    [Tooltip("要連打 E 幾下（第一下就算）")]
    [SerializeField] private int _requiredPresses = 5;

    [Tooltip("幾秒內要完成")]
    [SerializeField] private float _duration = 2f;

    [Header("顏色")]
    [Tooltip("捏熄後變成的顏色")]
    [SerializeField] private Color _snuffedColor = Color.gray;

    private bool _isSnuffed;
    private bool _isQteActive;
    private int _presses;
    private float _timeRemaining;
    private Transform _player;

    /// <summary>HUD 顯示的提示文字。</summary>
    public string InteractPrompt => _prompt;

    /// <summary>還沒被捏熄才能互動。</summary>
    public bool CanInteract => !_isSnuffed;

    /// <summary>用來計算距離的位置。</summary>
    public Transform InteractPoint => _interactPoint != null ? _interactPoint : transform;

    /// <summary>QTE 是否正在進行中。</summary>
    public bool IsQteActive => _isQteActive;

    /// <summary>QTE 已按幾下（DevOverlay 顯示用）。</summary>
    public int Presses => _presses;

    /// <summary>QTE 需要幾下。</summary>
    public int RequiredPresses => _requiredPresses;

    /// <summary>QTE 還剩幾秒。</summary>
    public float TimeRemaining => _timeRemaining;

    /// <summary>玩家要面向它（前方 _angle 度）、_distance 公尺內（水平距離）。</summary>
    public bool CanInteractFrom(Transform player)
    {
        Vector3 toNpc = InteractPoint.position - player.position;
        toNpc.y = 0f;
        if (toNpc.magnitude > _distance)
        {
            return false;
        }

        Vector3 forward = player.forward;
        forward.y = 0f;
        if (toNpc.sqrMagnitude < 0.0001f || forward.sqrMagnitude < 0.0001f)
        {
            return true;
        }

        return Vector3.Angle(forward, toNpc) <= _angle * 0.5f;
    }

    /// <summary>第一次按 E：開始 QTE，這一下算第 1 下。</summary>
    public void Interact()
    {
        if (_isSnuffed || _isQteActive)
        {
            return;
        }

        _isQteActive = true;
        _presses = 0;
        _timeRemaining = _duration;
        if (_player == null)
        {
            _player = FindPlayer();
        }
        Debug.Log($"[FakeSmokerNPC] {name}：QTE 開始！{_duration} 秒內連打 E {_requiredPresses} 下", this);
        CountPress();
    }

    /// <summary>QTE 期間 PlayerInteractor 轉交過來的每一下 E。</summary>
    public void RegisterQtePress()
    {
        if (!_isQteActive)
        {
            Debug.LogWarning($"[FakeSmokerNPC] {name}：不在 QTE 中卻收到連打，PlayerInteractor 轉交有問題。", this);
            return;
        }
        CountPress();
    }

    private void CountPress()
    {
        _presses++;
        Debug.Log($"[FakeSmokerNPC] {name}：第 {_presses} 次連打（{_presses}/{_requiredPresses}，剩 {_timeRemaining:F2} 秒）", this);

        if (_presses >= _requiredPresses)
        {
            _isQteActive = false;
            _isSnuffed = true;
            Debug.Log($"[FakeSmokerNPC] {name}：QTE 成功，菸被捏熄了！", this);
            SetColor(_snuffedColor);
        }
    }

    private void Update()
    {
        if (!_isQteActive)
        {
            return;
        }

        _timeRemaining -= Time.deltaTime;
        if (_timeRemaining <= 0f)
        {
            EndFailed("超過時間");
            return;
        }

        if (_player != null && !CanInteractFrom(_player))
        {
            EndFailed("離開範圍或沒有面向 NPC");
        }
    }

    private void OnDisable()
    {
        _isQteActive = false;
    }

    private void EndFailed(string reason)
    {
        _isQteActive = false;
        _presses = 0;
        _timeRemaining = 0f;
        Debug.Log($"[FakeSmokerNPC] {name}：QTE 失敗（{reason}），進度歸零，可以馬上重試。", this);
    }

    /// <summary>找玩家（場景裡的 PlayerController）。</summary>
    private static Transform FindPlayer()
    {
        PlayerController controller = FindFirstObjectByType<PlayerController>();
        return controller != null ? controller.transform : null;
    }

    private void SetColor(Color color)
    {
        Renderer targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer == null)
        {
            return;
        }

        Material material = targetRenderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }
    }

    private void OnValidate()
    {
        _distance = Mathf.Max(0.1f, _distance);
        _angle = Mathf.Clamp(_angle, 1f, 360f);
        _requiredPresses = Mathf.Max(1, _requiredPresses);
        _duration = Mathf.Max(0.1f, _duration);
    }
}
