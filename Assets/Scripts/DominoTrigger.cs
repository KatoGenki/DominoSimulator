using UnityEngine;

public class DominoTrigger : MonoBehaviour
{
    // ドミノを倒すための力の強さ（インスペクタで調整可能）
    public float ForceMagnitude = 5000f; // スケールアップに合わせて大きな値に調整

    // このフラグで、ドミノを倒す機能を有効/無効に切り替えます
    [HideInInspector] public bool IsActive = false; 
    [SerializeField] private float _pushPower = 0.5f; // 弱い力で十分です
    [SerializeField] private float _upwardOffset = 0.1f; // ドミノの上の方を叩くためのオフセット
    // ドミノのレイヤーを設定し、意図しないオブジェクトを倒すのを防ぎます
    [Tooltip("ドミノが属するレイヤーを設定してください")]
    public LayerMask DominoLayer; 

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        // 相手がドミノ（Rigidbody）を持っているか確認
        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;

        // 1. 【重要】力を加える方向を「水平」に限定する
        // 自分の位置からドミノの位置への方向を計算
        Vector3 pushDir = other.transform.position - transform.position;
        pushDir.y = 0; // Y成分を0にすることで、上下の跳ね上がりを防止
        pushDir.Normalize();

        // 2. 【重要】叩く位置（作用点）をドミノの上部に設定する
        // 真ん中より少し上を叩くことで、回転（トルク）が生まれて自然に倒れる
        Vector3 hitPoint = other.transform.position + (Vector3.up * _upwardOffset);

        // 3. 瞬間的な力（Impulse）を加える
        rb.AddForceAtPosition(pushDir * _pushPower, hitPoint, ForceMode.Impulse);
    }
    // ThirdPersonController内、またはDominoTrigger内での処理例
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        Rigidbody body = hit.collider.attachedRigidbody;

        // 相手にRigidbodyがない、またはKinematic（物理無効）なら無視
        if (body == null || body.isKinematic) return;

        // 下方向にぶつかった場合は無視（地面など）
        if (hit.moveDirection.y < -0.3) return;

        // --- 自然に倒すための計算 ---
        // キャラクターの移動方向をベースにするが、上向きの力は抑える
        Vector3 pushDir = new Vector3(hit.moveDirection.x, 0, hit.moveDirection.z);

        // 押し出す強さを大幅に下げる (0.1〜0.5程度で調整)
        float pushPower = 0.5f; 

        // AddForceAtPosition を使うと、当たった場所に応じて「回転」が加わるので自然に倒れる
        body.AddForceAtPosition(pushDir * pushPower, hit.point, ForceMode.Impulse);
    }
}