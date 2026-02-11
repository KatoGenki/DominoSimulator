using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class DominoTrigger : MonoBehaviour
{
    public float ForceMagnitude = 1.0f;
    public bool IsActive = true;
    [SerializeField] private float _upwardOffset = 0.2f;
    public LayerMask DominoLayer;

    private Collider _myCollider; // 追加：自身のコライダー参照用

    private void Start()
    {
        // 起動時にコライダーを取得
        _myCollider = GetComponent<Collider>();
    }

    private void Update()
    {
        if (GameManager.Instance == null || _myCollider == null) return;

        // --- フェーズに応じて IsTrigger を動的に切り替え ---
        //変数内は真偽値であり、Buildフェーズ以外ならtrue
        bool shouldBeTrigger = (GameManager.Instance.currentState != GameManager.GameState.Build);
        //IsTriggerに真偽値をセット
        if (_myCollider.isTrigger != shouldBeTrigger)
        {
            _myCollider.isTrigger = shouldBeTrigger;
        }
    }

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

        // 2. 速度の初期化
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // 3. 叩く位置を決定
        Vector3 hitPoint = other.transform.position + (Vector3.up * _upwardOffset);

        // 4. 衝撃力を加える
        rb.AddForceAtPosition(screenMouseDelta * ForceMagnitude, hitPoint, ForceMode.Impulse);
    }
}