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
        if (GameManager.Instance == null || ThirdPersonController == null) return;

        // 現在が「設置モード」として振る舞うべき状況かを判定
        bool isBuildingMode = GameManager.Instance.currentState == GameManager.GameState.Build && 
                            (ThirdPersonController.CurrentState == ThirdPersonController.PlayerState.CrawlingIdle || 
                            ThirdPersonController.CurrentState == ThirdPersonController.PlayerState.CrawlingMove);

        if (isBuildingMode)
        {   
            SwitchToFPSCamera();
        }
        else
        {
            SwitchToTPSCamera(); 
        }
    }

    public void SwitchToFPSCamera()
    {
        if (_FPSCamera != null) _FPSCamera.Priority = 20;
        if (_TPSCamera != null) _TPSCamera.Priority = 10;
    }

    private void SwitchToTPSCamera()
    {
        if (_TPSCamera != null) _TPSCamera.Priority = 20;
        if (_FPSCamera != null) _FPSCamera.Priority = 10;
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
