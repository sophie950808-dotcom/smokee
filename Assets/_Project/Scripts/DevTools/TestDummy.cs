using UnityEngine;

/// <summary>
/// 測試用假目標（只在 TestScene_A 用）：實作 IHittable，要打幾下才會消失。
/// 每被打一下就往紅色變，並在 Console 印出剩幾下。
/// 物件上需要有 Collider，PlayerAttack 才找得到它。
/// </summary>
public class TestDummy : MonoBehaviour, IHittable
{
    [Header("耐久")]
    [Tooltip("要打幾下才會消失")]
    [Min(1)]
    [SerializeField] private int _hitsToDestroy = 3;

    [Header("顏色")]
    [Tooltip("還沒被打時的顏色")]
    [SerializeField] private Color _fullColor = Color.white;

    [Tooltip("快被打掉時的顏色")]
    [SerializeField] private Color _lowColor = Color.red;

    private int _hitsTaken;
    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponentInChildren<Renderer>();
        ApplyColor();
    }

    /// <summary>被攻擊命中。hitCount 是這次算幾下（吸入劑時是 3）。</summary>
    public void TakeHit(int hitCount)
    {
        _hitsTaken += Mathf.Max(1, hitCount);
        int remaining = Mathf.Max(0, _hitsToDestroy - _hitsTaken);
        Debug.Log($"{name} 被打 {hitCount} 下，剩 {remaining} 下", this);

        if (remaining <= 0)
        {
            Debug.Log($"{name} 被打倒了，消失", this);
            Destroy(gameObject);
            return;
        }

        ApplyColor();
    }

    private void ApplyColor()
    {
        if (_renderer == null)
        {
            return;
        }

        float t = _hitsToDestroy <= 1 ? 1f : (float)_hitsTaken / (_hitsToDestroy - 1);
        Color color = Color.Lerp(_fullColor, _lowColor, t);

        // 執行時用 material（會自動複製一份），不會改到共用的材質檔
        Material material = _renderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }
    }
}
