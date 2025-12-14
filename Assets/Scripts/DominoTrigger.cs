using UnityEngine;

public class DominoTrigger : MonoBehaviour
{
    // ドミノを倒すための力の強さ（インスペクタで調整可能）
    public float ForceMagnitude = 5000f; // スケールアップに合わせて大きな値に調整

    // このフラグで、ドミノを倒す機能を有効/無効に切り替えます
    [HideInInspector] public bool IsActive = false; 
    
    // ドミノのレイヤーを設定し、意図しないオブジェクトを倒すのを防ぎます
    [Tooltip("ドミノが属するレイヤーを設定してください")]
    public LayerMask DominoLayer; 

    private void OnTriggerEnter(Collider other)
    {
        // 1. 機能が無効であれば、処理をしない
        if (!IsActive) return;

        // 2. 衝突したオブジェクトがドミノレイヤーに属しているか確認
        if (((1 << other.gameObject.layer) & DominoLayer) == 0) return;

        // 3. ドミノのRigidbodyを取得
        // ドミノオブジェクト自体ではなく、その親や子にRigidbodyがある可能性も考慮
        Rigidbody dominoRb = other.GetComponentInParent<Rigidbody>() ?? other.GetComponent<Rigidbody>();

        if (dominoRb != null)
        {
            // ドミノを倒すための力を加える
            // 力を加える方向 (接触した場所からドミノの中心に向かう方向など)
            Vector3 direction = (dominoRb.transform.position - transform.position).normalized;
            
            // 接触した高さから上方向にも少し力を加えて、浮き上がらせてから倒れるようにすると自然
            // スケールアップに合わせてY成分も調整が必要
            direction.y = 5.0f; // 例: 0.5fを10倍

            // Rigidbody.AddForce(方向ベクトル * 力の強さ, ForceMode.Impulse);
            // ForceMode.Impulse は瞬間的な力を加えるため、Massに依存します。
            dominoRb.AddForce(direction * ForceMagnitude, ForceMode.Impulse);
            
            // ドミノを倒した判定やSE再生など、ゲームロジックを追加
            Debug.Log($"ドミノを倒してしまいました！ (力: {ForceMagnitude})");
        }
    }
}