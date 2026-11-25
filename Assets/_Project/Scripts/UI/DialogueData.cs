using System.Collections.Generic;
using UnityEngine;

/// <summary>共用對話資料容器；正式台詞由 C11 製作。</summary>
[CreateAssetMenu(menuName = "菸菸一熄/對話資料", fileName = "DialogueData")]
public sealed class DialogueData : ScriptableObject
{
    [Tooltip("依序播放的角色名稱與台詞。空角色名稱表示旁白。")]
    public List<DialogueLine> Lines = new List<DialogueLine>();
}
