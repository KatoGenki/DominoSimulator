using UnityEngine;
using StarterAssets;

/// <summary>
/// プレイヤーが最初に触れたドミノに動的に追加され、連鎖の開始（動き出し）を検知する
/// </summary>
public class StartDominoWatcher : MonoBehaviour
{
    private Quaternion _initialRotation;
    private bool _isTriggered = false;
    CameraManager CameraManager;
    
    [Header("検知設定")]
    [SerializeField] private float _startAngleThreshold = 2.0f; // 2度傾いたら開始とみなす

    void Awake()
    {
        // シーン内のCameraManagerを探して参照を取得
        CameraManager = Object.FindFirstObjectByType<CameraManager>();
    }
    void Start()
    {
        // アタッチされた瞬間の角度を記録
        _initialRotation = transform.rotation;
    }

    void Update()
    {
        if (_isTriggered) return;

        // 初期角度からの変化をチェック
        float angleDiff = Quaternion.Angle(_initialRotation, transform.rotation);

        if (angleDiff > _startAngleThreshold)
        {
            _isTriggered = true;
            OnChainDetected();
        }
    }

    private void OnChainDetected()
    {
        Debug.Log($"<color=cyan>【演出開始】{gameObject.name} の倒れ始めを検知しました！</color>");

        // 1. HUDの演出を切り替え
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.OnChainStartedUI();
        }

        // 2. カメラの切り替え（CameraManager側にカメラ優先度変更メソッドがある想定）
        if (CameraManager != null)
        {
            //Debug.Log("StartDominoWatcher: カメラを結果演出用に切り替えます。");
            CameraManager.SwitchToResultCamera();
        }
    }
}