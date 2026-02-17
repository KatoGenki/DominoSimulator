using UnityEngine;
using StarterAssets;

public class NormalDominoS : DominoBase
{
    // //ドミノ固有のスコア
    // [Header("スコア設定")]
    // //倒れたときに加算されるスコア
    // [SerializeField] private int _scorePoint = 10; 
    // //ドミノが倒れたか判定するフラグ
    // private bool _isToppled = false; 
    // // ドミノが倒れたと判定する角度
    // private float _toppleThreshold = 30f; 
    // //毎フレーム実行される関数
    // void Update()
    // {
    //     // 転倒フラグがfalseかつ、設定した角度よりもドミノが倒れていたら
    //     if (!_isToppled && Vector3.Angle(Vector3.up, transform.up) > _toppleThreshold)
    //     {
    //         _isToppled = true;
    //         OnToppled();
    //     }
    // }
    // //ドミノが倒れたときにスコアを加算し、カメラのターゲットを変更する関数
    // private void OnToppled()
    // {
    //     // 現在の高さ（Y座標）を取得
    //     float height = transform.position.y;
        
    //     // ScoreManagerが存在していたら
    //     if (ScoreManager.Instance != null)
    //     {   // スコア加算メソッドを呼び出し、このドミノのスコアを加える
    //         ScoreManager.Instance.AddScore(_scorePoint, height, this.transform);
    //     }

    //     // 倒れた自分をカメラのターゲットにする
    //     // ScoreManagerのAddScore内でもUpdateCameraTargetを呼んでいるためいらない可能性あり
    //     if (GameManager.Instance != null)
    //     {   // カメラターゲット更新メソッドを呼び出し、自分をターゲットにする
    //         GameManager.Instance.UpdateCameraTarget(this.transform);
    //     }
    // }
}