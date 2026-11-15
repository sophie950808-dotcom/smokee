using UnityEngine;

public class GarageExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 只要是玩家穿過這個區域
        OpeningSequence opening = FindFirstObjectByType<OpeningSequence>();
        if (opening != null)
        {
            opening.OnPlayerExitedGarage();
        }
    }
}