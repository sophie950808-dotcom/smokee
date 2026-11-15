using System;
using UnityEngine;

public sealed class C2OpeningDialog : MonoBehaviour
{
    /// <summary>玩家選擇後發出通知。true 為接受香菸，false 為拒絕香菸。</summary>
    public event Action<bool> OnChoiceSelected;

    /// <summary>送出接受香菸的選擇。</summary>
    public void AcceptSmoke()
    {
        SubmitChoice(true);
    }

    /// <summary>送出拒絕香菸的選擇。</summary>
    public void RefuseSmoke()
    {
        SubmitChoice(false);
    }

    private void SubmitChoice(bool acceptSmoke)
    {
        if (!isActiveAndEnabled)
            return;

        Action<bool> receiver = OnChoiceSelected;
        if (receiver == null)
        {
            Debug.LogWarning("C2 開場對話尚未連接路線接收端。", this);
            return;
        }

        gameObject.SetActive(false);
        receiver.Invoke(acceptSmoke);
    }
}
