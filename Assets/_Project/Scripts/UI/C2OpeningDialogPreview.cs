using TMPro;
using UnityEngine;

public sealed class C2OpeningDialogPreview : MonoBehaviour
{
    [Header("C2 測試場景")]
    [Tooltip("測試場景中實際接收路線選擇的 RouteManager。")]
    [SerializeField] private RouteManager _routeManager;

    [Tooltip("顯示 RouteManager 通知的選擇。")]
    [SerializeField] private TMP_Text _result;

    private void OnEnable()
    {
        _result.gameObject.SetActive(false);
        if (_routeManager)
            _routeManager.OnRouteChosen += ShowChoice;
    }

    private void OnDisable()
    {
        if (_routeManager)
            _routeManager.OnRouteChosen -= ShowChoice;
    }

    private void ShowChoice(OpeningRoute route)
    {
        _result.text = route == OpeningRoute.AcceptSmoke ? "已選擇：接受香菸" : "已選擇：拒絕香菸";
        _result.gameObject.SetActive(true);
    }
}
