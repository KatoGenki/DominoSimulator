using UnityEngine;
using Cinemachine;

public class WipeManager : MonoBehaviour
{
    [Header("Virtual Cameras")]
    [SerializeField] private CinemachineVirtualCamera _resultCamera;   // 至近距離
    [SerializeField] private CinemachineVirtualCamera _overviewCamera; // 全体・引き

    [Header("Wipe Setup")]
    [SerializeField] private Camera _wipeCamera; // Brainを外してTarget Textureを入れたカメラ

    private bool _isMainOverview = false;

    public void OnWipeClicked()
    {
        _isMainOverview = !_isMainOverview;
        
        // 1. メイン画面（GameView）の切り替え
        // 優先度が高い方がメインカメラに映ります
        if (_isMainOverview)
        {
            _resultCamera.Priority = 10;
            _overviewCamera.Priority = 35;
        }
        else
        {
            _resultCamera.Priority = 35;
            _overviewCamera.Priority = 10;
        }

        // 2. ワイプ画面（Render Texture）の切り替え
        // ワイプカメラはBrainがないので、手動で映す対象の座標へ動かします
        UpdateWipeCameraPosition();
    }

    private void Update()
    {
        // ワイプの中身も常にターゲットを追いかけさせる
        UpdateWipeCameraPosition();
    }

    private void UpdateWipeCameraPosition()
    {
        if (_wipeCamera == null) return;

        // メインが「全体」なら、ワイプは「至近距離」のカメラの位置をコピー
        // メインが「至近距離」なら、ワイプは「全体」のカメラの位置をコピー
        CinemachineVirtualCamera targetVcam = _isMainOverview ? _resultCamera : _overviewCamera;

        if (targetVcam != null)
        {
            _wipeCamera.transform.position = targetVcam.transform.position;
            _wipeCamera.transform.rotation = targetVcam.transform.rotation;
            
            // 画角（FOV）も合わせる
            _wipeCamera.fieldOfView = targetVcam.m_Lens.FieldOfView;
        }
    }
}