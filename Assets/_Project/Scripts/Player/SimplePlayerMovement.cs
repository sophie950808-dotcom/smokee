using UnityEngine;

/// <summary>
/// 臨時玩家移動腳本：供 B 組員在 A 組員尚未提交正式 PlayerController 前測試場景與觸發
/// 支援 WASD 移動、滑鼠水平轉向
/// </summary>
public class SimplePlayerMovement : MonoBehaviour
{
    [Header("移動速度")]
    public float moveSpeed = 5f;
    public float rotateSpeed = 120f;

    private CharacterController controller;

    private void Start()
    {
        // 自動檢查或添加 CharacterController 以防穿牆
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // 1. 滑鼠水平轉向 (也可以用 A/D 轉向)
        float horizontalInput = Input.GetAxis("Horizontal"); // A/D 或 左右方向鍵
        float verticalInput = Input.GetAxis("Vertical");     // W/S 或 上下方向鍵

        // 旋轉角色
        transform.Rotate(0, horizontalInput * rotateSpeed * Time.deltaTime, 0);

        // 2. 前後移動
        Vector3 moveDirection = transform.forward * verticalInput * moveSpeed;

        if (controller != null)
        {
            // 如果身上有 CharacterController，用它移動有物理碰撞
            moveDirection.y = -9.8f * Time.deltaTime; // 簡易重力
            controller.Move(moveDirection * Time.deltaTime);
        }
        else
        {
            // 如果只有 Rigidbody 或 Transform
            transform.position += transform.forward * verticalInput * moveSpeed * Time.deltaTime;
        }
    }
}