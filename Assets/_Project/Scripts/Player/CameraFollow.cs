using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("跟隨目標")]
    public Transform target; // 玩家 Capsule

    [Header("跟隨參數")]
    public Vector3 offset = new Vector3(0f, 2.5f, -4f); // 攝影機與玩家的相對距離與高度
    public float smoothSpeed = 8f;                     // 跟隨平滑速度
    public bool lookAtTarget = true;

    private void LateUpdate()
    {
        if (target == null) return;

        // 計算期望位置
        Vector3 desiredPosition = target.position + target.TransformDirection(offset);

        // 平滑移動
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // 視角朝向玩家
        if (lookAtTarget)
        {
            Vector3 lookTarget = target.position + Vector3.up * 1.5f;
            transform.LookAt(lookTarget);
        }
    }
}