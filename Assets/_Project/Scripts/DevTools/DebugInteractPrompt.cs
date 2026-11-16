using UnityEngine;

/// <summary>
/// 暫時的互動提示（只在 TestScene_A 用）：監聽 PlayerInteractor.OnFocusChanged，
/// 用 OnGUI 在畫面下方顯示提示文字。正式版由 C 的 HUD 負責，到時刪掉這個物件即可。
/// </summary>
public class DebugInteractPrompt : MonoBehaviour
{
    [Tooltip("要監聽的 PlayerInteractor。留空會自動在場景裡找")]
    [SerializeField] private PlayerInteractor _interactor;

    [Tooltip("文字大小")]
    [SerializeField] private int _fontSize = 28;

    private string _text;
    private GUIStyle _style;

    private void Start()
    {
        if (_interactor == null)
        {
            _interactor = FindFirstObjectByType<PlayerInteractor>();
        }

        if (_interactor == null)
        {
            Debug.LogWarning("DebugInteractPrompt：場景裡找不到 PlayerInteractor。", this);
            return;
        }

        _interactor.OnFocusChanged += HandleFocusChanged;
        HandleFocusChanged(_interactor.CurrentTarget);
    }

    private void OnDestroy()
    {
        if (_interactor != null)
        {
            _interactor.OnFocusChanged -= HandleFocusChanged;
        }
    }

    private void HandleFocusChanged(IInteractable target)
    {
        _text = target != null ? target.InteractPrompt : null;
    }

    private void OnGUI()
    {
        if (string.IsNullOrEmpty(_text))
        {
            return;
        }

        if (_style == null)
        {
            _style = new GUIStyle(GUI.skin.box)
            {
                fontSize = _fontSize,
                alignment = TextAnchor.MiddleCenter,
            };
        }

        float width = 420f;
        float height = _fontSize * 2f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.75f, width, height);
        GUI.Box(rect, _text, _style);
    }
}
