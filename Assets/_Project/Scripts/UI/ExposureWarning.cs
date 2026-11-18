using TMPro;
using UnityEngine;

/// <summary>依玩家的暴露警示狀態顯示或隱藏文字。</summary>
public class ExposureWarning : MonoBehaviour
{
    [Header("場景資料")]
    [Tooltip("拖入玩家身上的 ExposureSystem。單獨預覽警示時可以留空。")]
    [SerializeField] private ExposureSystem _exposureSystem;

    [Header("警示文字")]
    [Tooltip("拖入顯示警示訊息的 TextMeshPro 元件。")]
    [SerializeField] private TMP_Text _warningText;

    private void OnEnable()
    {
        if (_exposureSystem == null)                                 // [I1 整合] 欄位空著時自動找玩家
            _exposureSystem = FindFirstObjectByType<ExposureSystem>();

        if (_exposureSystem != null)
            _exposureSystem.OnWarningChanged += SetWarningVisible;

        SetWarningVisible(_exposureSystem != null && _exposureSystem.IsWarning);
    }

    private void OnDisable()
    {
        if (_exposureSystem != null)
            _exposureSystem.OnWarningChanged -= SetWarningVisible;

        SetWarningVisible(false);
    }

    private void SetWarningVisible(bool isVisible)
    {
        if (_warningText != null)
            _warningText.enabled = isVisible;
    }

#if UNITY_EDITOR
    [ContextMenu("C4/模擬顯示警示")]
    private void PreviewShowWarning()
    {
        if (Application.isPlaying)
            SetWarningVisible(true);
    }

    [ContextMenu("C4/模擬隱藏警示")]
    private void PreviewHideWarning()
    {
        if (Application.isPlaying)
            SetWarningVisible(false);
    }
#endif
}