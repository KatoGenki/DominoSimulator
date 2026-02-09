using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
public class DominoTrigger : MonoBehaviour
{
    public float ForceMagnitude = 1.0f;
    public bool IsActive = true;
    [SerializeField] private float _upwardOffset = 0.2f;
    public LayerMask DominoLayer;

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;


        // GameManagerがReady状態のときのみ、最初の接触を検知する
        if (GameManager.Instance != null && GameManager.Instance.currentState == GameManager.GameState.Ready)
        {
            // 触れたのがドミノレイヤーであり、まだWatcherが付いていない場合
            if (((1 << other.gameObject.layer) & DominoLayer) != 0)
            {
                if (other.GetComponent<StartDominoWatcher>() == null)
                {
                    // Watcherを動的に追加（これが「最初の一個」の判定になる）
                    other.gameObject.AddComponent<StartDominoWatcher>();
                    Debug.Log($"Trigger: {other.name} を連鎖開始ドミノとしてロックしました。");
                }
            }
        }
        
        // レイヤーチェック
        if (((1 << other.gameObject.layer) & DominoLayer) == 0) return;

        Rigidbody rb = other.attachedRigidbody;
        if (rb == null || rb.isKinematic) return;
        // 1. マウスの動きを取得
        var mouse = Mouse.current;
        Vector3 screenMouseDelta = mouse.delta.ReadValue();

        // 2. 【修正箇所】最新のUnity仕様に合わせてプロパティ名を変更
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 3. 叩く位置を決定
        Vector3 hitPoint = other.transform.position + (Vector3.up * _upwardOffset);

        // 4. 衝撃力を加える
        rb.AddForceAtPosition(screenMouseDelta * ForceMagnitude, hitPoint, ForceMode.Impulse);
    }
}