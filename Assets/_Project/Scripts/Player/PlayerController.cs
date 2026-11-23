using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 玩家控制：移動、奔跑、跳躍、滑鼠視角、第一／第三人稱切換、第三人稱鏡頭防穿牆。
/// 規格見 docs/INTERFACES.md 第 5 節。
/// 角色左右轉向 = 整個 Player 轉；上下看 = CameraPivot 轉。
/// 場景只用一台 Main Camera，本腳本每幀把它移到正確位置（攝影機不用放在 Player 底下）。
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("要控制的攝影機。留空會自動抓場景裡 Tag 為 MainCamera 的攝影機")]
    [SerializeField] private Camera _camera;

    [Tooltip("鏡頭轉軸，放在眼睛高度（Player 的子物件）。第一人稱時鏡頭就在這個位置")]
    [SerializeField] private Transform _cameraPivot;

    [Tooltip("第一人稱時要隱藏的身體模型（Renderer）")]
    [SerializeField] private Renderer[] _bodyRenderers;

    [Header("移動")]
    [Tooltip("走路速度（公尺／秒）")]
    [SerializeField] private float _moveSpeed = 4f;

    [Tooltip("按住左 Shift 奔跑時的速度倍率，例如 1.6 = 走路速度的 1.6 倍")]
    [SerializeField] private float _runMultiplier = 1.6f;

    [Tooltip("跳躍高度（公尺）")]
    [SerializeField] private float _jumpHeight = 1.2f;

    [Tooltip("重力強度（正數，越大掉越快）。現實約 9.8，遊戲常用 15～25 比較俐落")]
    [SerializeField] private float _gravity = 20f;

    [Header("視角")]
    [Tooltip("滑鼠靈敏度（每移動 1 像素轉幾度）")]
    [SerializeField] private float _mouseSensitivity = 0.1f;

    [Tooltip("往上看的最大角度（度）")]
    [SerializeField] private float _maxLookUp = 70f;

    [Tooltip("往下看的最大角度（度）")]
    [SerializeField] private float _maxLookDown = 60f;

    [Tooltip("勾選 = 遊戲開始時是第一人稱")]
    [SerializeField] private bool _startInFirstPerson = false;

    [Header("第三人稱鏡頭")]
    [Tooltip("鏡頭在角色後方的距離（公尺）")]
    [SerializeField] private float _thirdPersonDistance = 4f;

    [Tooltip("第三人稱時鏡頭比眼睛再高多少（公尺）")]
    [SerializeField] private float _thirdPersonHeightOffset = 0.3f;

    [Tooltip("防穿牆偵測球的半徑。越大越早拉近，越不容易看到牆裡面")]
    [SerializeField] private float _cameraCollisionRadius = 0.25f;

    [Tooltip("鏡頭被牆擠近時，最近只會到這個距離")]
    [SerializeField] private float _minCameraDistance = 0.3f;

    [Tooltip("哪些 Layer 會擋住鏡頭。預設全部（玩家自己的碰撞體會自動忽略）")]
    [SerializeField] private LayerMask _cameraCollisionMask = ~0;

    /// <summary>目前是否為第一人稱。</summary>
    public bool IsFirstPerson => _isFirstPerson;

    /// <summary>
    /// 玩家現在能不能操作：沒有任何系統用 SetControlLock／SetControlEnabled(false) 鎖住，而且不在暫停中。
    /// PlayerAttack、PlayerInteractor 用這個判斷能不能攻擊／互動。
    /// </summary>
    public bool IsControlEnabled => CanControl;

    /// <summary>切換第一／第三人稱時觸發。參數：是否第一人稱。</summary>
    public event Action<bool> OnViewModeChanged;

    // SetControlEnabled（舊方法）使用的 key
    private const string DefaultControlKey = "Default";

    private CharacterController _controller;
    private bool _isFirstPerson;
    private bool _isPaused;
    private readonly HashSet<string> _controlLocks = new HashSet<string>();
    private readonly Dictionary<PlayerAction, HashSet<string>> _actionLocks = new Dictionary<PlayerAction, HashSet<string>>();
    private Transform _cameraOverride;
    private GameManager _gameManager;
    private float _yaw;
    private float _pitch;
    private float _verticalVelocity;
    private readonly Dictionary<string, float> _speedModifiers = new Dictionary<string, float>();
    private readonly RaycastHit[] _cameraHits = new RaycastHit[16];

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();

        if (_camera == null)
        {
            _camera = Camera.main;
        }

        if (_camera == null)
        {
            Debug.LogWarning("PlayerController：找不到攝影機。請在 Inspector 指定，或確認場景裡有 Tag 為 MainCamera 的攝影機。", this);
        }

        if (_cameraPivot == null)
        {
            Debug.LogWarning("PlayerController：沒有指定 CameraPivot，視角功能無法運作。", this);
        }

        _yaw = transform.eulerAngles.y;
    }

    private void Start()
    {
        SetFirstPerson(_startInFirstPerson);

        // 監聽 GameManager 的暫停事件（測試場景沒有 GameManager 時就略過）
        _gameManager = GameManager.Instance;
        if (_gameManager != null)
        {
            _gameManager.OnPauseChanged += HandlePauseChanged;
            _isPaused = _gameManager.State == GameState.Paused;
        }

        ApplyCursorState();
    }

    private void OnDestroy()
    {
        if (_gameManager != null)
        {
            _gameManager.OnPauseChanged -= HandlePauseChanged;
        }
    }

    private void Update()
    {
        if (CanControl)
        {
            if (IsActionAllowed(PlayerAction.Look))
            {
                HandleLook();
            }
            if (IsActionAllowed(PlayerAction.ViewToggle))
            {
                HandleViewToggle();
            }
        }

        HandleMovement();
    }

    /// <summary>玩家現在能不能操作：沒有任何控制鎖，而且不在暫停中。</summary>
    private bool CanControl => _controlLocks.Count == 0 && !_isPaused;

    private void LateUpdate()
    {
        UpdateCameraPosition();
    }

    /// <summary>
    /// 開啟或關閉玩家操作。false = 停止移動與視角，並解鎖滑鼠游標（例如開問答 UI、開場演出）；
    /// true = 恢復操作並鎖定游標。關閉時仍有重力，角色不會浮在空中。
    /// </summary>
    public void SetControlEnabled(bool enabled)
    {
        // 舊方法保留給 B、C 現有程式：等於用 "Default" 這個 key 上鎖／解鎖
        SetControlLock(DefaultControlKey, !enabled);
    }

    /// <summary>
    /// 用自己的 key 鎖住或放開全部操作，例如 ("Dialogue", true)。
    /// 鎖住時停止移動與視角並解鎖游標；所有 key 都放開才恢復，不會互相覆蓋。
    /// </summary>
    public void SetControlLock(string key, bool locked)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (locked)
        {
            _controlLocks.Add(key);
        }
        else
        {
            _controlLocks.Remove(key);
        }
        ApplyCursorState();
    }

    /// <summary>
    /// 只鎖住或放開一種操作（新手教學、QTE 用），例如 (PlayerAction.Move, "Tutorial", true)。
    /// 同一種操作可以被多個 key 鎖住，全部放開才可以用。
    /// </summary>
    public void SetActionLocked(PlayerAction action, string key, bool locked)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        if (!_actionLocks.TryGetValue(action, out HashSet<string> keys))
        {
            keys = new HashSet<string>();
            _actionLocks[action] = keys;
        }

        if (locked)
        {
            keys.Add(key);
        }
        else
        {
            keys.Remove(key);
        }
    }

    /// <summary>
    /// 這種操作現在能不能用：整體可以操作（IsControlEnabled），而且這種操作沒被個別鎖住。
    /// C 的背包切換／使用道具前先檢查 PlayerAction.UseItem。
    /// </summary>
    public bool IsActionAllowed(PlayerAction action)
    {
        if (!CanControl)
        {
            return false;
        }

        return !_actionLocks.TryGetValue(action, out HashSet<string> keys) || keys.Count == 0;
    }

    /// <summary>
    /// 把玩家瞬間移動到指定位置與方向（B 的 StoryTeleporter 用）。
    /// CharacterController 啟用時不能直接改 position，這裡會先停用再啟用。
    /// </summary>
    public void TeleportTo(Vector3 position, Quaternion rotation)
    {
        bool wasEnabled = _controller.enabled;
        _controller.enabled = false;

        _yaw = rotation.eulerAngles.y;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0f, _yaw, 0f));
        _verticalVelocity = 0f;

        _controller.enabled = wasEnabled;
    }

    /// <summary>
    /// 過場動畫用：Main Camera 改成每幀跟著指定的點（位置與旋轉），不再跟著玩家。
    /// 傳 null 等於 ClearCameraOverride()。
    /// </summary>
    public void SetCameraOverride(Transform cameraPoint)
    {
        _cameraOverride = cameraPoint;
    }

    /// <summary>取消過場鏡頭，Main Camera 回到跟著玩家。</summary>
    public void ClearCameraOverride()
    {
        _cameraOverride = null;
    }

    /// <summary>
    /// 設定一個速度倍率，例如 ("Gum", 1.5f)、("MaxExposure", 0.7f)。
    /// 同一個 key 再設定會覆蓋舊值；所有倍率相乘。
    /// </summary>
    public void SetSpeedModifier(string key, float multiplier)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        _speedModifiers[key] = multiplier;
    }

    /// <summary>移除一個速度倍率。key 不存在時不做任何事。</summary>
    public void RemoveSpeedModifier(string key)
    {
        if (string.IsNullOrEmpty(key))
        {
            return;
        }

        _speedModifiers.Remove(key);
    }

    private void HandleLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || _cameraPivot == null)
        {
            return;
        }

        // 滑鼠位移是「這一幀移動了幾像素」，本身就和幀率無關，不用乘 deltaTime
        Vector2 delta = mouse.delta.ReadValue() * _mouseSensitivity;
        _yaw += delta.x;
        _pitch = Mathf.Clamp(_pitch - delta.y, -_maxLookUp, _maxLookDown);

        // 角色永遠面向鏡頭的水平方向
        transform.rotation = Quaternion.Euler(0f, _yaw, 0f);
        _cameraPivot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
    }

    private void HandleViewToggle()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
        {
            SetFirstPerson(!_isFirstPerson);
        }
    }

    private void HandleMovement()
    {
        Vector2 input = Vector2.zero;
        bool isRunning = false;
        bool jumpPressed = false;

        Keyboard keyboard = Keyboard.current;
        if (CanControl && keyboard != null)
        {
            if (IsActionAllowed(PlayerAction.Move))
            {
                if (keyboard.wKey.isPressed) input.y += 1f;
                if (keyboard.sKey.isPressed) input.y -= 1f;
                if (keyboard.dKey.isPressed) input.x += 1f;
                if (keyboard.aKey.isPressed) input.x -= 1f;
            }
            isRunning = IsActionAllowed(PlayerAction.Run) && keyboard.leftShiftKey.isPressed;
            jumpPressed = IsActionAllowed(PlayerAction.Jump) && keyboard.spaceKey.wasPressedThisFrame;
        }

        // 斜走不會比直走快
        input = Vector2.ClampMagnitude(input, 1f);

        // 角色已經面向鏡頭水平方向，所以用角色自己的前方／右方就是「相對鏡頭」
        Vector3 moveDirection = transform.forward * input.y + transform.right * input.x;
        float speed = _moveSpeed * (isRunning ? _runMultiplier : 1f) * GetSpeedMultiplier();

        bool isGrounded = _controller.isGrounded;
        if (isGrounded && _verticalVelocity < 0f)
        {
            // 給一點向下的力，讓角色穩穩貼地（下坡時不會一跳一跳）
            _verticalVelocity = -2f;
        }

        if (isGrounded && jumpPressed)
        {
            // 物理公式：要跳到高度 h，起跳速度 = √(2gh)
            _verticalVelocity = Mathf.Sqrt(2f * _gravity * _jumpHeight);
        }

        _verticalVelocity -= _gravity * Time.deltaTime;

        Vector3 velocity = moveDirection * speed;
        velocity.y = _verticalVelocity;
        _controller.Move(velocity * Time.deltaTime);

        // 撞到天花板時停止往上
        if ((_controller.collisionFlags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
        {
            _verticalVelocity = 0f;
        }
    }

    private void UpdateCameraPosition()
    {
        if (_camera == null)
        {
            return;
        }

        if (_cameraOverride != null)
        {
            _camera.transform.SetPositionAndRotation(_cameraOverride.position, _cameraOverride.rotation);
            return;
        }

        if (_cameraPivot == null)
        {
            return;
        }

        Vector3 pivotPosition = _cameraPivot.position;
        Quaternion lookRotation = _cameraPivot.rotation;

        if (_isFirstPerson)
        {
            _camera.transform.SetPositionAndRotation(pivotPosition, lookRotation);
            return;
        }

        Vector3 origin = pivotPosition + Vector3.up * _thirdPersonHeightOffset;
        Vector3 backward = lookRotation * Vector3.back;
        float distance = _thirdPersonDistance;

        // 從頭部往鏡頭方向射一顆小球，碰到牆就把鏡頭拉到牆前面
        int hitCount = Physics.SphereCastNonAlloc(
            origin, _cameraCollisionRadius, backward, _cameraHits,
            _thirdPersonDistance, _cameraCollisionMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _cameraHits[i];

            // 忽略玩家自己的碰撞體
            if (hit.collider.transform.IsChildOf(transform))
            {
                continue;
            }

            // 起點就已經重疊的碰撞體（distance 為 0）不算
            if (hit.distance <= 0f)
            {
                continue;
            }

            distance = Mathf.Min(distance, hit.distance);
        }

        distance = Mathf.Max(distance, _minCameraDistance);
        _camera.transform.SetPositionAndRotation(origin + backward * distance, lookRotation);
    }

    private void SetFirstPerson(bool firstPerson)
    {
        _isFirstPerson = firstPerson;

        if (_bodyRenderers != null)
        {
            foreach (Renderer bodyRenderer in _bodyRenderers)
            {
                if (bodyRenderer != null)
                {
                    bodyRenderer.enabled = !firstPerson;
                }
            }
        }

        OnViewModeChanged?.Invoke(firstPerson);
    }

    private void HandlePauseChanged(bool paused)
    {
        // 暫停狀態和 SetControlEnabled 分開記錄：
        // 例如問答 UI 開著時按暫停再繼續，玩家仍然維持「不能操作、游標解鎖」
        _isPaused = paused;
        ApplyCursorState();
    }

    private void ApplyCursorState()
    {
        bool canControl = CanControl;
        Cursor.lockState = canControl ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !canControl;
    }

    private float GetSpeedMultiplier()
    {
        float multiplier = 1f;
        foreach (float value in _speedModifiers.Values)
        {
            multiplier *= value;
        }

        return multiplier;
    }
}
