using UnityEngine;

public sealed class C2OpeningDialog : MonoBehaviour
{
    /// <summary>將接受香菸的選擇傳給 RouteManager。</summary>
    public void AcceptSmoke()
    {
        SubmitChoice(OpeningRoute.AcceptSmoke);
    }

    /// <summary>將拒絕香菸的選擇傳給 RouteManager。</summary>
    public void RefuseSmoke()
    {
        SubmitChoice(OpeningRoute.RefuseSmoke);
    }

    private void SubmitChoice(OpeningRoute route)
    {
        if (!isActiveAndEnabled)
            return;

        RouteManager routeManager = RouteManager.Instance;
        if (!routeManager)
        {
            Debug.LogWarning("C2 開場對話找不到場景中的 RouteManager。", this);
            return;
        }

        gameObject.SetActive(false);
        routeManager.ChooseRoute(route);
    }
}
