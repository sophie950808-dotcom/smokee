using TMPro;
using UnityEngine;

public sealed class C2OpeningDialogPreview : MonoBehaviour
{
    [Header("C2 測試場景")]
    [Tooltip("要測試的開場對話框。")]
    [SerializeField] private C2OpeningDialog _dialog;

    [Tooltip("顯示收到的選擇。")]
    [SerializeField] private TMP_Text _result;

    private void OnEnable()
    {
        _result.gameObject.SetActive(false);
        _dialog.OnChoiceSelected += ShowChoice;
    }

    private void OnDisable()
    {
        _dialog.OnChoiceSelected -= ShowChoice;
    }

    private void ShowChoice(bool acceptSmoke)
    {
        _result.text = acceptSmoke ? "已選擇：接受香菸" : "已選擇：拒絕香菸";
        _result.gameObject.SetActive(true);
    }
}
