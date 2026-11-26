using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 開發者快捷鍵（只在測試用）。只有 GameManager 勾選 devMode 時才有效。
/// 用來模擬還沒完成的系統（教學、B 的故事事件、寶箱、BOSS、結局），讓 TestScene_A 可以從頭到尾跑一次。
/// 不使用遊戲操作的按鍵（WASD、空白、Shift、E、V、Esc、Tab、滑鼠）。
/// 之後的任務會在這裡繼續加鍵，記得同步更新 HelpText。
/// </summary>
public class DevHotkeys : MonoBehaviour
{
    /// <summary>按鍵說明文字，DevOverlay 會顯示。</summary>
    public const string HelpText =
        "── 開場 ──\n" +
        "F5 接受香菸　F6 拒絕香菸　F7 開場演出結束\n" +
        "── 遊戲中 ──\n" +
        "F1 完成新手教學\n" +
        "F2 擊敗一隻菸怪（+40，第三階段應被擋）\n" +
        "F3 接受目前事件（含拒絕後重新接受）\n" +
        "F4 拒絕目前事件（事件二、三）\n" +
        "F8 完成目前事件\n" +
        "F11 空檔剩餘設為 3 秒\n" +
        "F9 扣 20 HP　F10 回 10 HP\n" +
        "1 淨化設為 60%　2 進入第三階段\n" +
        "3 直接打倒 BOSS（沒有 BOSS 時模擬）　4 模擬結局播完\n" +
        "── 道具（A13）──\n" +
        "5 給予三種道具（ItemRewards.Grant）\n" +
        "6 用貼片　7 用口嚼錠　8 用噴霧（瞄準鏡頭中央）\n" +
        "── BOSS（A14）──\n" +
        "9 開始 BOSS 戰（跳過登場與規則視窗）　0 BOSS 扣 30 血\n" +
        "- 玩家無敵 開／關";

    [Tooltip("F9 每次扣多少 HP")]
    [SerializeField] private float _devDamage = 20f;

    [Tooltip("F10 每次回多少 HP")]
    [SerializeField] private float _devHeal = 10f;

    [Tooltip("F11 把 40 秒空檔的剩餘時間設成幾秒")]
    [SerializeField] private float _gapSkipSeconds = 3f;

    [Tooltip("0 每次讓 BOSS 扣多少血")]
    [SerializeField] private float _devBossDamage = 30f;

    private PlayerHealth _playerHealth;
    private PlayerEffects _playerEffects;

    private void Update()
    {
        GameManager gameManager = GameManager.Instance;
        Keyboard keyboard = Keyboard.current;
        if (gameManager == null || keyboard == null || !gameManager.DevMode)
        {
            return;
        }

        if (gameManager.State == GameState.Opening)
        {
            HandleOpeningKeys(keyboard);
        }
        else if (gameManager.State == GameState.Playing)
        {
            HandlePlayingKeys(gameManager, keyboard);
        }
    }

    private void HandleOpeningKeys(Keyboard keyboard)
    {
        RouteManager routeManager = RouteManager.Instance;
        if (routeManager == null)
        {
            return;
        }

        if (keyboard.f5Key.wasPressedThisFrame)
        {
            routeManager.ChooseRoute(OpeningRoute.AcceptSmoke);
        }
        else if (keyboard.f6Key.wasPressedThisFrame)
        {
            routeManager.ChooseRoute(OpeningRoute.RefuseSmoke);
        }
        else if (keyboard.f7Key.wasPressedThisFrame)
        {
            routeManager.NotifyOpeningFinished();
        }
    }

    private void HandlePlayingKeys(GameManager gameManager, Keyboard keyboard)
    {
        PhaseManager phaseManager = PhaseManager.Instance;

        if (keyboard.f9Key.wasPressedThisFrame)
        {
            PlayerHealth playerHealth = GetPlayerHealth();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(_devDamage, DamageSource.Dev);
            }
            return;
        }
        if (keyboard.f10Key.wasPressedThisFrame)
        {
            PlayerHealth playerHealth = GetPlayerHealth();
            if (playerHealth != null)
            {
                playerHealth.Heal(_devHeal);
            }
            return;
        }
        if (keyboard.digit1Key.wasPressedThisFrame)
        {
            gameManager.DevSetPurification(GameManager.Phase3PurificationUnits);
            return;
        }
        if (keyboard.digit3Key.wasPressedThisFrame)
        {
            BossHealth bossHealth = BossHealth.Instance;
            if (bossHealth != null)
            {
                // 真的把血扣到 0，走正常的打倒流程（BOSS 戰還沒開始時 BossHealth 會擋下並印 Log）
                bossHealth.TakeDamage(bossHealth.CurrentHp, DamageSource.Dev);
            }
            else
            {
                Debug.Log("[DevHotkeys] 場景裡沒有 BOSS，改用模擬（直接呼叫 GameManager.NotifyBossDefeated）。", this);
                gameManager.NotifyBossDefeated();
            }
            return;
        }
        if (keyboard.digit0Key.wasPressedThisFrame)
        {
            if (BossHealth.Instance != null)
            {
                BossHealth.Instance.TakeDamage(_devBossDamage, DamageSource.Dev);
            }
            else
            {
                Debug.LogWarning("[DevHotkeys] 場景裡沒有 BOSS，0 無效。請執行選單 Tools/Smokee/A - A14 放置 BOSS 戰測試區。", this);
            }
            return;
        }
        if (keyboard.minusKey.wasPressedThisFrame)
        {
            PlayerHealth playerHealth = GetPlayerHealth();
            if (playerHealth != null)
            {
                playerHealth.DevInvincible = !playerHealth.DevInvincible;
                Debug.Log("[DevHotkeys] 玩家無敵：" + (playerHealth.DevInvincible ? "開" : "關"), this);
            }
            return;
        }
        if (HandleItemKeys(keyboard))
        {
            return;
        }

        if (phaseManager == null)
        {
            if (keyboard.f1Key.wasPressedThisFrame || keyboard.f2Key.wasPressedThisFrame
                || keyboard.f3Key.wasPressedThisFrame || keyboard.f4Key.wasPressedThisFrame
                || keyboard.f8Key.wasPressedThisFrame || keyboard.f11Key.wasPressedThisFrame
                || keyboard.digit2Key.wasPressedThisFrame || keyboard.digit4Key.wasPressedThisFrame
                || keyboard.digit9Key.wasPressedThisFrame)
            {
                Debug.LogWarning("[DevHotkeys] 場景裡沒有 PhaseManager，這個按鍵無效。請執行選單 Tools/Smokee/A - A11 設定 TestScene_A。", this);
            }
            return;
        }

        if (keyboard.f1Key.wasPressedThisFrame)
        {
            phaseManager.NotifyTutorialFinished();
        }
        else if (keyboard.f2Key.wasPressedThisFrame)
        {
            // 模擬 B 的菸怪被擊敗：直接加第一階段菸怪的淨化值（第三階段時 GameManager 會擋下）
            gameManager.AddPurification(phaseManager.GetMonsterUnits(MonsterGroup.Phase1, 0));
        }
        else if (keyboard.f3Key.wasPressedThisFrame)
        {
            phaseManager.ReportEventAccepted(phaseManager.CurrentEventIndex);
        }
        else if (keyboard.f4Key.wasPressedThisFrame)
        {
            phaseManager.ReportHelpRefused(phaseManager.CurrentEventIndex);
        }
        else if (keyboard.f8Key.wasPressedThisFrame)
        {
            phaseManager.ReportEventCompleted(phaseManager.CurrentEventIndex);
        }
        else if (keyboard.f11Key.wasPressedThisFrame)
        {
            phaseManager.DevSetEventGapRemaining(_gapSkipSeconds);
        }
        else if (keyboard.digit2Key.wasPressedThisFrame)
        {
            phaseManager.DevJumpToPhase3();
        }
        else if (keyboard.digit4Key.wasPressedThisFrame)
        {
            phaseManager.NotifyEndingFinished();
        }
        else if (keyboard.digit9Key.wasPressedThisFrame)
        {
            phaseManager.DevStartBossFight();
        }
    }

    /// <summary>
    /// A13 道具按鍵：5 給予三種道具；6／7／8 直接使用（模擬 C 的背包按右鍵，背包完成前用來測試）。
    /// 有按下其中一個鍵時回傳 true。
    /// </summary>
    private bool HandleItemKeys(Keyboard keyboard)
    {
        if (keyboard.digit5Key.wasPressedThisFrame)
        {
            ItemRewards.Grant(ItemType.NicotinePatch);
            ItemRewards.Grant(ItemType.NicotineGum);
            ItemRewards.Grant(ItemType.NicotineSpray);
            Debug.Log("[DevHotkeys] 給予三種道具（貼片、口嚼錠、噴霧）。C 的背包還沒完成時，上面會出現「沒有背包在監聽」的黃色警告，這是正常的。", this);
            return true;
        }

        ItemType? type = null;
        if (keyboard.digit6Key.wasPressedThisFrame)
        {
            type = ItemType.NicotinePatch;
        }
        else if (keyboard.digit7Key.wasPressedThisFrame)
        {
            type = ItemType.NicotineGum;
        }
        else if (keyboard.digit8Key.wasPressedThisFrame)
        {
            type = ItemType.NicotineSpray;
        }

        if (type == null)
        {
            return false;
        }

        PlayerEffects playerEffects = GetPlayerEffects();
        if (playerEffects != null)
        {
            ItemUseResult result = playerEffects.TryUseItem(type.Value);
            Debug.Log("[DevHotkeys] 使用 " + type.Value + " → " + result
                      + (result == ItemUseResult.Consumed ? "（背包應該減 1）" : "（背包不扣）"), this);
        }
        return true;
    }

    /// <summary>第一次用到時才在場景裡找 PlayerEffects；找不到時印警告。</summary>
    private PlayerEffects GetPlayerEffects()
    {
        if (_playerEffects == null)
        {
            _playerEffects = FindFirstObjectByType<PlayerEffects>();
            if (_playerEffects == null)
            {
                Debug.LogWarning("[DevHotkeys] 場景裡找不到 PlayerEffects，6／7／8 無效。請執行選單 Tools/Smokee/A - A13 放置道具與互動測試物件。", this);
            }
        }
        return _playerEffects;
    }

    /// <summary>第一次用到時才在場景裡找 PlayerHealth；找不到時印警告。</summary>
    private PlayerHealth GetPlayerHealth()
    {
        if (_playerHealth == null)
        {
            _playerHealth = FindFirstObjectByType<PlayerHealth>();
            if (_playerHealth == null)
            {
                Debug.LogWarning("[DevHotkeys] 場景裡找不到 PlayerHealth，F9／F10 無效。", this);
            }
        }
        return _playerHealth;
    }
}
