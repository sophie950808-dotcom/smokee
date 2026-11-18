using UnityEngine;

public class GarageExitTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 判斷只要進來的是玩家 (或其身上帶有 CharacterController / Tag 為 Player)
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null)
        {
            OpeningSequence opening = FindFirstObjectByType<OpeningSequence>();
            if (opening != null)
            {
                opening.OnPlayerReachedExit();
            }
        }
    }
}