using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// CinemachineFreeLook 用のズーム制御（マウスホイールでFOVを変更）
/// </summary>
public class FreeLookZoomController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineFreeLook _freeLook;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 5f;
    [SerializeField] private float _minFOV = 25f;
    [SerializeField] private float _maxFOV = 60f;

    private void Reset()
    {
        if (_freeLook == null)
        {
            _freeLook = GetComponent<CinemachineFreeLook>();
        }
    }

    private void Awake()
    {
        if (_freeLook == null)
        {
            _freeLook = GetComponent<CinemachineFreeLook>();
        }
    }

    private void Update()
    {
        if (_freeLook == null) return;

        // Ready かつ ドミノが倒れている（スコアが一度でも加算されている）ときだけズームを有効にする
        var gameManager = GameManager.Instance;
        var scoreManager = ScoreManager.Instance;
        bool canZoom = gameManager != null
                       && scoreManager != null
                       && gameManager.currentState == GameManager.GameState.Ready
                       && scoreManager.totalScore > 0;

        if (!canZoom) return;

        float scrollDelta = 0f;

        // 新InputSystemのマウスが使える場合はそちらを優先
        var mouse = Mouse.current;
        if (mouse != null)
        {
            scrollDelta = mouse.scroll.ReadValue().y;
        }
        else
        {
            // フォールバックとして旧InputSystemの軸を使用
            scrollDelta = UnityEngine.Input.GetAxis("Mouse ScrollWheel") * 120f;
        }

        if (Mathf.Approximately(scrollDelta, 0f)) return;

        var lens = _freeLook.m_Lens;

        // 上方向スクロールを「ズームイン」にしたいのでマイナス方向に適用
        float fov = lens.FieldOfView - scrollDelta * _zoomSpeed * Time.deltaTime;
        lens.FieldOfView = Mathf.Clamp(fov, _minFOV, _maxFOV);

        _freeLook.m_Lens = lens;
    }
}

