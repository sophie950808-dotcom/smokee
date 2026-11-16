using UnityEngine;

/// <summary>
/// 暴露畫面效果控制器 (任務 B.5)
/// 負責根據暴露值 (0~100) 呈現暗角、色調變灰霧、邊緣窒息感
/// 企劃書規格：4.5 節、10.3 節、18.3 節
/// </summary>
public class ExposureScreenEffect : MonoBehaviour
{
    public static ExposureScreenEffect Instance { get; private set; }

    [Header("測試專用 (B5 階段用假暴露值測試)")]
    [Tooltip("勾選後可在 Inspector 拉動下方滑桿測試效果")]
    public bool testMode = true;

    [Range(0f, 100f)]
    [Tooltip("手動測試用的暴露值 (0 ~ 100)")]
    public float fakeExposureValue = 0f;

    [Header("當前暴露值 (0~100)")]
    [Range(0f, 100f)]
    [SerializeField] private float currentExposure = 0f;

    [Header("效果上限保護 (企劃書 18.3 節)")]
    [Range(0.3f, 0.95f)]
    [Tooltip("暗角與煙霧的最大遮擋透明度，保留中央視野清晰")]
    public float maxAlphaLimit = 0.82f;

    private Texture2D vignetteTexture;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        GenerateVignetteTexture();
    }

    private void Update()
    {
        // 測試模式下讀取 Inspector 的滑桿
        if (testMode)
        {
            SetExposure(fakeExposureValue);

            // 支援快捷鍵方便測試：按小鍵盤 + 增加，- 減少
            if (Input.GetKey(KeyCode.Equals) || Input.GetKey(KeyCode.KeypadPlus))
            {
                fakeExposureValue = Mathf.Min(100f, fakeExposureValue + 30f * Time.deltaTime);
            }
            if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
            {
                fakeExposureValue = Mathf.Max(0f, fakeExposureValue - 30f * Time.deltaTime);
            }
        }
    }

    /// <summary>
    /// 公開方法：設定真實暴露值 (供未來 A 組員的 ExposureSystem 呼叫)
    /// </summary>
    public void SetExposure(float value)
    {
        currentExposure = Mathf.Clamp(value, 0f, 100f);
    }

    /// <summary>
    /// 動態生成圓形漸層暗角貼圖 (免外部貼圖資源，保證任何電腦都能跑)
    /// </summary>
    private void GenerateVignetteTexture()
    {
        int size = 256;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        vignetteTexture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2(size / 2f, size / 2f);
        float maxDist = size / 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center);
                float normDist = Mathf.Clamp01(dist / maxDist);

                // 核心透明、邊緣漸層變濃 (暗灰煙霧色)
                float alpha = Mathf.SmoothStep(0f, 1f, normDist);
                vignetteTexture.SetPixel(x, y, new Color(0.12f, 0.1f, 0.14f, alpha));
            }
        }
        vignetteTexture.Apply();
    }

    private void OnGUI()
    {
        if (currentExposure <= 0.01f || vignetteTexture == null) return;

        // 計算暴露強度比率 (0.0 ~ 1.0)
        float ratio = currentExposure / 100f;

        // 依比例計算透明度與縮放，並受上限保護 (企劃 18.3 節)
        float finalAlpha = Mathf.Min(ratio * 1.05f, maxAlphaLimit);

        Color oldColor = GUI.color;

        // 1. 煙霧暗角覆蓋
        GUI.color = new Color(1f, 1f, 1f, finalAlpha);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), vignetteTexture, ScaleMode.StretchToFill);

        // 2. 暴露值極高 (>= 75) 時的煙霧泛灰窒息感
        if (currentExposure >= 75f)
        {
            float greyRatio = (currentExposure - 75f) / 25f; // 0 ~ 1
            GUI.color = new Color(0.2f, 0.2f, 0.2f, greyRatio * 0.35f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
        }

        GUI.color = oldColor;

        // 3. 測試模式時在左上角顯示數值方便除錯
        if (testMode)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 16;
            labelStyle.normal.textColor = Color.yellow;
            GUI.Label(new Rect(20, 20, 400, 30),
                $"[B5 畫面測試] 假暴露值: {currentExposure:F1} / 100 (可用鍵盤 + / - 鍵調試)", labelStyle);
        }
    }
}