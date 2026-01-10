using UnityEngine;

public class WipeCameraFollow : MonoBehaviour
{
    private Camera _mainCamera;

    void Start()
    {
        // メインカメラ（Cinemachineが制御しているカメラ）を探す
        _mainCamera = Camera.main;
    }

    void LateUpdate()
    {
        if (_mainCamera != null)
        {
            // メインカメラの座標と回転をそのままコピーする
            // これにより、メイン画面がどっちのVirtual Cameraを映していても、
            // ワイプ側も同じように追従します
            transform.position = _mainCamera.transform.position;
            transform.rotation = _mainCamera.transform.rotation;
        }
    }
}