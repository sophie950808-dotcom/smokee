using UnityEngine;

/// <summary>
/// 暫時的暴露值警示文字（只在 TestScene_A 用）：監聽 ExposureSystem.OnWarningChanged，
/// 用 OnGUI 在畫面上方顯示一行紅字。正式版由 C 的 ExposureWarning 負責，到時刪掉這個物件即可。
/// </summary>
public class DebugExposureWarning : MonoBehaviour
{
    [Tooltip("要監聽的 ExposureSystem。留空會自動在場景裡找")]
    [SerializeField] private ExposureSystem _exposureSystem;

    [Tooltip("警示文字內容")]
    [SerializeField] private string _warningText = "警告：二手菸暴露值過高！請盡快離開煙霧範圍";

    [Tooltip("文字大小")]
    [SerializeField] private int _fontSize = 28;

    private bool _isShowing;
    private GUIStyle _style;

    private void Start()
    {
        if (_exposureSystem == null)
        {
            _exposureSystem = FindFirstObjectByType<ExposureSystem>();
        }

        if (_exposureSystem == null)
        {
            Debug.LogWarning("DebugExposureWarning：場景裡找不到 ExposureSystem。", this);
            return;
        }

        _exposureSystem.OnWarningChanged += HandleWarningChanged;
        HandleWarningChanged(_exposureSystem.IsWarning);
    }

    private void OnDestroy()
    {
        if (_exposureSystem != null)
        {
            _exposureSystem.OnWarningChanged -= HandleWarningChanged;
        }
    }

    private void HandleWarningChanged(bool isWarning)
    {
        _isShowing = isWarning;
    }

    private void OnGUI()
    {
        if (!_isShowing)
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
            _style.normal.textColor = new Color(1f, 0.3f, 0.3f);
        }

        float width = 640f;
        float height = _fontSize * 2f;
        Rect rect = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.12f, width, height);
        GUI.Box(rect, _warningText, _style);
    }
}
