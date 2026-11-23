using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 「目前任務提示」的唯一來源。A、B 的系統用這裡更新文字，C 的 HUD 只負責顯示。
/// 規格見 docs/INTERFACES.md 第 6 節。
/// - 任務（Task）：持續顯示。多個系統都設定時，最後設定的顯示在上面；清掉後露出前一個。
/// - 短暫提示（Toast）：幾秒後自動消失，例如「第三階段才能使用」。
/// 重新載入場景時自動清空。
/// </summary>
public static class TaskHint
{
    // 依設定順序排列；最後一個是目前顯示的任務
    private static readonly List<KeyValuePair<string, string>> _tasks = new List<KeyValuePair<string, string>>();
    private static bool _sceneHookAdded;

    /// <summary>目前要顯示的任務文字；空字串 = 不顯示。</summary>
    public static string CurrentTask => _tasks.Count > 0 ? _tasks[_tasks.Count - 1].Value : "";

    /// <summary>目前任務改變時觸發。參數：要顯示的文字（空字串 = 不顯示）。C 的 HUD 監聽。</summary>
    public static event Action<string> OnTaskChanged;

    /// <summary>要顯示短暫提示時觸發。參數：文字、顯示秒數。C 的 HUD 監聽。</summary>
    public static event Action<string, float> OnToast;

    /// <summary>
    /// 設定任務提示。source 是設定者的名字，例如 "Tutorial"、"Phase"、"StoryEvent"、"Refusal"、"Boss"。
    /// 同一個 source 再設定會換掉舊文字，並移到最上面。text 為空時等於 ClearTask(source)。
    /// </summary>
    public static void SetTask(string source, string text)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }
        if (string.IsNullOrEmpty(text))
        {
            ClearTask(source);
            return;
        }

        string before = CurrentTask;
        RemoveSource(source);
        _tasks.Add(new KeyValuePair<string, string>(source, text));
        NotifyIfChanged(before);
    }

    /// <summary>清掉某個 source 設定的任務提示；底下如果還有別的任務，會改顯示那一個。</summary>
    public static void ClearTask(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return;
        }

        string before = CurrentTask;
        RemoveSource(source);
        NotifyIfChanged(before);
    }

    /// <summary>顯示短暫提示（預設 2.5 秒後消失）。</summary>
    public static void ShowToast(string text, float seconds = 2.5f)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        OnToast?.Invoke(text, Mathf.Max(0.1f, seconds));
    }

    private static void RemoveSource(string source)
    {
        _tasks.RemoveAll(pair => pair.Key == source);
    }

    private static void NotifyIfChanged(string before)
    {
        if (before != CurrentTask)
        {
            OnTaskChanged?.Invoke(CurrentTask);
        }
    }

    /// <summary>每次進入 Play 時清空，並在每次載入場景時清空。</summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _tasks.Clear();
        OnTaskChanged = null;
        OnToast = null;
        if (!_sceneHookAdded)
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
            _sceneHookAdded = true;
        }
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
        {
            return;
        }

        string before = CurrentTask;
        _tasks.Clear();
        NotifyIfChanged(before);
    }
}
