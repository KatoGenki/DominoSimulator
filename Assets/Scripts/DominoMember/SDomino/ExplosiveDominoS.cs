using UnityEngine;

public class ExplosiveDomino : DominoBase
{
    [Header("Explosion Settings")]
    [SerializeField] private float _explosionRadius = 5.0f;   // 衝撃波の届く範囲
    [SerializeField] private float _explosionForce = 800.0f;    // 衝撃の強さ
    [SerializeField] private float _upwardsModifier = 1.5f;   // オブジェクトをどれだけ上に跳ね飛ばすか
    [SerializeField] private LayerMask _affectedLayers;       // 衝撃を与えるレイヤー（Dominoレイヤーを指定）

    [Header("Visual Effects")]
    [SerializeField] private GameObject _explosionEffectPrefab; // 爆発エフェクト（任意）

    protected override void OnToppled()
    {
        // 親クラスのスコア加算やカメラターゲット更新処理を先に実行
        base.OnToppled();

        // 爆発処理を実行
        ExecuteExplosion();
    }

    private void ExecuteExplosion()
    {
        Vector3 explosionPos = transform.position;

        // 1. 指定した範囲内のコライダーを取得
        Collider[] colliders = Physics.OverlapSphere(explosionPos, _explosionRadius, _affectedLayers);

        foreach (Collider hit in colliders)
        {
            // 自分自身には衝撃を与えないようにチェック（あるいはRigidbodyがあれば飛ばす）
            if (hit.gameObject == this.gameObject) continue;

            Rigidbody rb = hit.attachedRigidbody;
            if (rb != null)
            {
                // 2. AddExplosionForce で放射状の力を加える
                // (力, 中心点, 半径, 上方向への補正)
                rb.AddExplosionForce(_explosionForce, explosionPos, _explosionRadius, _upwardsModifier);
            }
        }

        // 2. 自分自身を吹き飛ばす処理を追加
        if (_rb != null)
        {
            // 爆発地点を少しだけ「自分の真下」にずらすと、綺麗に上に吹き飛びます
            Vector3 selfExplosionPos = explosionPos + Vector3.down * 0.5f;
            _rb.AddExplosionForce(_explosionForce, selfExplosionPos, _explosionRadius, _upwardsModifier);
            
            // オプション：爆発の衝撃でランダムに回転を加えるとより「吹っ飛んだ感」が出ます
            _rb.AddTorque(Random.insideUnitSphere * _explosionForce, ForceMode.Impulse);
        }
        
        // 3. 視覚演出の生成
        if (_explosionEffectPrefab != null)
        {
            Instantiate(_explosionEffectPrefab, explosionPos, Quaternion.identity);
        }

        Debug.Log($"<color=red>【BOOM!】</color> {gameObject.name} が爆発し、{colliders.Length} 個のオブジェクトに影響を与えました。");
    }

    // Unityエディタ上で爆発範囲を確認するためのデバッグ表示
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _explosionRadius);
    }
}