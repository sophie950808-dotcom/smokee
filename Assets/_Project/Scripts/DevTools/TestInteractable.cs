using UnityEngine;

/// <summary>
/// 測試用互動物件（只在 TestScene_A 用）：實作 IInteractable。
/// 按 E 後在 Console 印出訊息，之後就不能再互動，並變成灰色。
/// 物件上需要有 Collider，PlayerInteractor 才找得到它。
/// </summary>
public class TestInteractable : MonoBehaviour, IInteractable
{
    [Header("互動")]
    [Tooltip("畫面上顯示的提示文字")]
    [SerializeField] private string _prompt = "按 E 測試互動";

    [Tooltip("用來計算距離的位置。留空就用物件自己的位置")]
    [SerializeField] private Transform _interactPoint;

    [Tooltip("互動過後變成的顏色")]
    [SerializeField] private Color _usedColor = Color.gray;

    private bool _used;

    /// <summary>HUD 顯示的提示文字。</summary>
    public string InteractPrompt => _prompt;

    /// <summary>還沒互動過才是 true。</summary>
    public bool CanInteract => !_used;

    /// <summary>用來計算距離的位置。</summary>
    public Transform InteractPoint => _interactPoint != null ? _interactPoint : transform;

    /// <summary>玩家按 E 時呼叫：印出訊息，之後不能再互動。</summary>
    public void Interact()
    {
        if (_used)
        {
            return;
        }

        _used = true;
        Debug.Log($"{name}：互動成功！之後不能再互動。", this);

        Renderer targetRenderer = GetComponentInChildren<Renderer>();
        if (targetRenderer == null)
        {
            return;
        }

        Material material = targetRenderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", _usedColor);
        }
        else
        {
            material.color = _usedColor;
        }
    }
}
