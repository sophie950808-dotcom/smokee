using UnityEngine;
using UnityEngine.InputSystem; // [I1 整合] 新版 Input System
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// <summary>
/// 暴露畫面效果控制器 (任務 B.5)
/// 負責根據暴露值 (0~100) 呈現暗角、色調變灰霧、邊緣窒息感
/// 企劃書規格：4.5 節、10.3 節、18.3 節
/// [I1 整合] 畫法由 OnGUI 改為 URP Volume（規格 DECISIONS.md：畫面後製使用 URP Volume）。
/// 原因：OnGUI 一定畫在所有 Canvas UI 上面，會蓋住 HUD。計算方式與上限保護沿用 B 的設計。
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

    // [I1 整合] URP Volume 與效果（在 Awake 用程式建立）
    private Volume exposureVolume;
    private VolumeProfile volumeProfile;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;
    private ExposureSystem exposureSystem; // [I1 整合] 玩家的暴露值來源（自動在場景裡找）

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else // [I1 整合] 只刪掉多出來的元件，不刪整個物件
        {
            Debug.LogWarning("[暴露畫面效果] 場景中有超過一個 ExposureScreenEffect，已移除多出來的元件。", this);
            Destroy(this);
            return; // 不再建立第二個 Volume
        }

        CreateVolume(); // [I1 整合] 原本是 GenerateVignetteTexture()
    }

    private void Start()
    {
        // [I1 整合] Main Camera 沒開後製時 Volume 不會有效果，執行時自動打開（不會存進場景）
        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            UniversalAdditionalCameraData cameraData = mainCamera.GetUniversalAdditionalCameraData();
            if (cameraData != null && !cameraData.renderPostProcessing)
            {
                cameraData.renderPostProcessing = true;
                Debug.Log("[暴露畫面效果] Main Camera 的 Post Processing 沒有開，已在執行時自動打開。想固定的話請在 Main Camera 的 Inspector 勾選 Rendering → Post Processing。");
            }
        }
    }

    private void OnDestroy()
    {
        // [I1 整合] 清掉執行時建立的 Volume 設定檔
        if (volumeProfile != null) Destroy(volumeProfile);
        if (Instance == this) Instance = null; // [I1 整合] 清掉 Instance，避免指向已刪除的物件
    }

    private void Update()
    {
        // 測試模式下讀取 Inspector 的滑桿
        if (testMode)
        {
            SetExposure(fakeExposureValue);

            // 支援快捷鍵方便測試：按小鍵盤 + 增加，- 減少
            Keyboard keyboard = Keyboard.current; // [I1 整合] 改用新版 Input System
            if (keyboard != null && (keyboard.equalsKey.isPressed || keyboard.numpadPlusKey.isPressed))
            {
                fakeExposureValue = Mathf.Min(100f, fakeExposureValue + 30f * Time.deltaTime);
            }
            if (keyboard != null && (keyboard.minusKey.isPressed || keyboard.numpadMinusKey.isPressed))
            {
                fakeExposureValue = Mathf.Max(0f, fakeExposureValue - 30f * Time.deltaTime);
            }
        }
        else // [I1 整合] 非測試模式：讀玩家的真實暴露值（規格：B 的畫面效果讀 VisualExposure）
        {
            if (exposureSystem == null) exposureSystem = FindFirstObjectByType<ExposureSystem>();
            if (exposureSystem != null) SetExposure(exposureSystem.VisualExposure);
        }

        UpdateVolume(); // [I1 整合] 依暴露值更新暗角與變灰
    }

    /// <summary>
    /// 公開方法：設定真實暴露值 (供未來 A 組員的 ExposureSystem 呼叫)
    /// </summary>
    public void SetExposure(float value)
    {
        currentExposure = Mathf.Clamp(value, 0f, 100f);
    }

    /// <summary>
    /// [I1 整合] 建立全域 Volume，加上暗角（Vignette）與調色（ColorAdjustments），取代原本的暗角貼圖
    /// </summary>
    private void CreateVolume()
    {
        GameObject volumeObject = new GameObject("ExposureVolume");
        volumeObject.transform.SetParent(transform, false);

        exposureVolume = volumeObject.AddComponent<Volume>();
        exposureVolume.isGlobal = true;
        exposureVolume.priority = 100f; // 比場景裡其他 Volume 優先

        volumeProfile = ScriptableObject.CreateInstance<VolumeProfile>();
        exposureVolume.profile = volumeProfile;

        vignette = volumeProfile.Add<Vignette>(true);
        vignette.color.Override(new Color(0.12f, 0.1f, 0.14f)); // 沿用 B 原本的暗灰煙霧色
        vignette.smoothness.Override(0.6f);
        vignette.intensity.Override(0f);

        colorAdjustments = volumeProfile.Add<ColorAdjustments>(true);
        colorAdjustments.saturation.Override(0f);
        colorAdjustments.postExposure.Override(0f);
    }

    /// <summary>
    /// [I1 整合] 依暴露值更新效果。計算方式沿用 B 原本的 OnGUI 版本
    /// </summary>
    private void UpdateVolume()
    {
        if (vignette == null || colorAdjustments == null) return;

        // 計算暴露強度比率 (0.0 ~ 1.0)
        float ratio = currentExposure / 100f;

        // 1. 煙霧暗角：依比例計算強度，並受上限保護 (企劃 18.3 節)
        vignette.intensity.value = Mathf.Min(ratio * 1.05f, maxAlphaLimit);

        // 2. 暴露值極高 (>= 75) 時的煙霧泛灰窒息感：降低飽和度、稍微調暗
        float greyRatio = currentExposure >= 75f ? (currentExposure - 75f) / 25f : 0f; // 0 ~ 1
        colorAdjustments.saturation.value = -70f * greyRatio;
        colorAdjustments.postExposure.value = -0.5f * greyRatio;
    }

    private void OnGUI()
    {
        // [I1 整合] 暗角與變灰改由 URP Volume 處理，OnGUI 只保留測試模式的除錯文字
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
