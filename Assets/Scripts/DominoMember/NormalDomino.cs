using UnityEngine;

public class NormalDomino : MonoBehaviour
{
    [Header("スコア設定")]
    [SerializeField] private int _scorePoint = 10; // NormalDominoの基本点
    
    private bool _isToppled = false; 
    private float _toppleThreshold = 30f; // 30度傾いたら倒れたと判定

    void Update()
    {
        // まだ判定されておらず、かつドミノが一定以上傾いた場合
        if (!_isToppled && Vector3.Angle(Vector3.up, transform.up) > _toppleThreshold)
        {
            _isToppled = true;
            OnToppled();
        }
    }

    private void OnToppled()
    {
        // 現在の高さ（Y座標）を取得
        float height = transform.position.y;
        
        // ScoreManager（後述）に通知して計算してもらう
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.AddScore(_scorePoint, height);
        }
    }
}