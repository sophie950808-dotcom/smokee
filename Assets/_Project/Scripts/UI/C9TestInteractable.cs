using UnityEngine;

/// <summary>僅供 TestScene_C 使用的可重複互動目標；不模擬正式 NPC 故事。</summary>
public sealed class C9TestInteractable : MonoBehaviour, IInteractable
{
    [Header("測試內容")]
    [Tooltip("交給既有 HUD 顯示的互動提示。")]
    [SerializeField] private string _prompt = "按 E 詢問測試目標甲";
    [Tooltip("交給 HelpPromptUI 顯示的問題。")]
    [SerializeField] private string _question = "這是測試問題，願意幫忙嗎？";
    private bool _available = true;
    /// <summary>測試目標提供的提示文字。</summary>
    public string InteractPrompt => _prompt;
    /// <summary>是否能再次互動。</summary>
    public bool CanInteract => isActiveAndEnabled && _available && HelpPromptUI.Instance
        && !HelpPromptUI.Instance.IsShowing && (!DialogueUI.Instance || !DialogueUI.Instance.IsShowing);
    /// <summary>互動距離的計算位置。</summary>
    public Transform InteractPoint => transform;
    /// <summary>成功開啟視窗的累計次數。</summary>
    public int InteractionCount { get; private set; }
    /// <summary>接受的累計次數。</summary>
    public int AcceptCount { get; private set; }
    /// <summary>拒絕的累計次數。</summary>
    public int RefuseCount { get; private set; }
    /// <summary>由 PlayerInteractor 呼叫；此腳本不讀 E 鍵。</summary>
    public void Interact()
    {
        if (!CanInteract) return;
        InteractionCount++;
        HelpPromptUI.Instance.Show(_question, () => AcceptCount++, () => RefuseCount++);
    }
    /// <summary>測試目標失效時，讓既有互動偵測自行清除提示。</summary>
    public void SetAvailable(bool value) => _available = value;
}
