using UnityEngine;

/// <summary>背包道具的顯示資料；效果由 PlayerEffects 處理。</summary>
[CreateAssetMenu(fileName = "HealthItemData", menuName = "菸菸一熄/道具資料")]
public sealed class HealthItemData : ScriptableObject
{
    [Tooltip("共用道具種類。")]
    public ItemType Type;
    [Tooltip("背包顯示名稱。")]
    public string DisplayName;
    [Tooltip("正式美術可於之後替換；留空使用原型佔位圖形。")]
    public Sprite Icon;
    [TextArea, Tooltip("道具的遊戲功能說明；正式衛教文案另行核對。")]
    public string InfoText;
}
