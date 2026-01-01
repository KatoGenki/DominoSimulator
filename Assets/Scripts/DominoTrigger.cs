using UnityEngine;

public class DominoTrigger : MonoBehaviour
{
    public float ForceMagnitude = 1.0f;
    public bool IsActive = true;
    [SerializeField] private float _upwardOffset = 0.2f;
    [SerializeField] private float _torquePower = 0.5f;
    public LayerMask DominoLayer;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        // レイヤーチェック
        if (((1 << other.gameObject.layer) & DominoLayer) == 0) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        // 1. プレイヤーの進行方向を取得
        Vector3 pushDir = transform.root.forward; 
        pushDir.y = 0;
        pushDir.Normalize();

        // 2. 【修正箇所】最新のUnity仕様に合わせてプロパティ名を変更
        // velocity -> linearVelocity
        // angularVelocity -> angularVelocity (Unity 2023.1以降、linearVelocityと対になるよう整理されました)
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 3. 叩く位置を決定
        Vector3 hitPoint = other.transform.position + (Vector3.up * _upwardOffset);

        // 4. 衝撃力を加える
        rb.AddForceAtPosition(pushDir * ForceMagnitude, hitPoint, ForceMode.Impulse);

        // 5. 強制的に前方に倒れる回転力を加える
        Vector3 torqueDir = Vector3.Cross(Vector3.up, pushDir);
        rb.AddTorque(torqueDir * _torquePower, ForceMode.Impulse);
    }
}