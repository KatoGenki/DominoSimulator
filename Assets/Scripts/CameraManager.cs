using UnityEngine;
using Cinemachine;
using StarterAssets;
using Unity.VisualScripting;

public class CameraManager : MonoBehaviour
{

    [Header("プレイ中のカメラ")]
    [SerializeField] private CinemachineVirtualCamera _FPSCamera;
    [SerializeField] private CinemachineVirtualCamera _TPSCamera;
    [Header("結果演出用カメラとUI")]
    [SerializeField] private CinemachineTargetGroup _targetGroup;
    [SerializeField] private CinemachineVirtualCamera _resultCamera;        
    [SerializeField] private GameObject _wipeUI;
    [SerializeField] private Camera _camera2; // 視界判定用
    ThirdPersonController ThirdPersonController;

    private void Start()
    {
        // 追加：シーン内からプレイヤーを探して変数に格納する
        ThirdPersonController = Object.FindFirstObjectByType<ThirdPersonController>();
        // 初期状態のカメラ優先度設定
        if (_FPSCamera != null) _FPSCamera.Priority = 10;
        if (_TPSCamera != null) _TPSCamera.Priority = 20;
        if (_resultCamera != null) _resultCamera.Priority = 5;
        if (_wipeUI != null) _wipeUI.SetActive(false);
    }
    private void Update()
    {
        // プレイヤーがいなければ何もしない（エラーを防ぐ）
        if (ThirdPersonController == null || GameManager.Instance == null) return;
        
        if(ThirdPersonController.PlayerState.CrawlingIdle == ThirdPersonController.CurrentState|| ThirdPersonController.PlayerState.CrawlingMove == ThirdPersonController.CurrentState)
        {
            SwitchToFPSCamera();
        }

        // 視界判定用カメラが必要ならここで処理を追加
        if(GameManager.GameState.Ready == GameManager.Instance.currentState)
        {
        }
    }

    public void SwitchToFPSCamera()
    {
        if (_FPSCamera != null) _FPSCamera.Priority = 20;
        if (_TPSCamera != null) _TPSCamera.Priority = 10;
    }
    public void UpdateCameraTarget(Transform newTarget)
    {
        if (_targetGroup == null) return;

        // 以前のターゲットをクリアし、最新の倒れたドミノにフォーカス
        var targets = _targetGroup.m_Targets;
        for (int i = 0; i < targets.Length; i++)
        {
            _targetGroup.RemoveMember(targets[i].target);
        }
        _targetGroup.AddMember(newTarget, 1f, 0f);
    }

    public void SwitchToResultCamera()
    {
        if (_resultCamera != null) _resultCamera.Priority = 30;
        if (_wipeUI != null) _wipeUI.SetActive(true);
    }
}
