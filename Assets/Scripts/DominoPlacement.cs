using UnityEngine;
using UnityEngine.InputSystem;

public class DominoPlacement : MonoBehaviour
{
    [Header("設定")]
    public GameObject DominoPrefab;
    public GameObject PreviewDomino;
    public LayerMask GroundLayer;
    public float PlacementOffset = 0.5f;

    [Header("操作感度")]
    [Tooltip("マウスの円運動に対する回転感度")]
    public float RotationSensitivity = 2.0f;
    [Tooltip("回転検知に必要な最小のマウス移動速度（静止時の誤作動防止）")]
    public float MinMouseSpeed = 1.0f;

    // 内部変数
    private GameObject _currentPreview;
    private bool _isHolding = false;
    private Vector2 _mouseDelta;
    private Vector2 _previousMouseDelta; // 直前のフレームのマウス移動ベクトル
    private Quaternion _currentRotationY = Quaternion.identity;

    private PlayerControls _controls;

    void Start()
    {
        // プレビュー初期化
        if (PreviewDomino != null)
        {
            _currentPreview = Instantiate(PreviewDomino, transform.position, Quaternion.identity);
            _currentPreview.SetActive(false);
        }

        // Input System初期化
        _controls = new PlayerControls();
        
        // 1. 左クリック押下/解放
        _controls.Building.PlaceDomino.started += ctx => _isHolding = true;
        _controls.Building.PlaceDomino.canceled += ctx => ConfirmPlacement();

        // 2. マウス移動量 (Delta) の取得
        _controls.Building.Look.performed += ctx => _mouseDelta = ctx.ReadValue<Vector2>();
        _controls.Building.Look.canceled += ctx => _mouseDelta = Vector2.zero;
    }

    void Update()
    {
        // 常に位置合わせを行う（移動と回転の同時操作のため）
        FindPlacementPosition();

        if (_isHolding)
        {
            // ホールド中は円運動を検知して回転させる
            DetectCircularMotion();
        }
        
        // 直前のベクトルを更新
        _previousMouseDelta = _mouseDelta;
    }

    // --- 円運動（ひねり）の検出ロジック ---
    private void DetectCircularMotion()
    {
        // マウスの動きが小さすぎる場合は回転処理をしない（ノイズ対策）
        if (_mouseDelta.magnitude < MinMouseSpeed) return;
        if (_previousMouseDelta.magnitude < MinMouseSpeed) return;

        // 1. ベクトルの角度変化を計算 (SignedAngle)
        // 直前のフレームの移動方向と、現在の移動方向の差分角度を取得
        // 時計回りならマイナス、反時計回りならプラスの値が返る
        float angleChange = Vector2.SignedAngle(_previousMouseDelta, _mouseDelta);

        // 2. ドミノの回転に適用
        // 感度を掛けて回転量を決定
        float rotationAmount = angleChange * RotationSensitivity;

        // Y軸回転を加算
        Quaternion rotationToAdd = Quaternion.Euler(0, rotationAmount, 0);
        _currentRotationY *= rotationToAdd;
    }

    private void FindPlacementPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, 100f, GroundLayer))
        {
            Vector3 placementPosition = hit.point + hit.normal * PlacementOffset;
            
            // 地面の傾斜に合わせる
            Quaternion groundTilt = Quaternion.FromToRotation(Vector3.up, hit.normal);
            
            // 最終回転 = 地面の傾斜 * プレイヤーのひねり回転
            Quaternion finalRotation = groundTilt * _currentRotationY;

            if (_currentPreview != null)
            {
                _currentPreview.SetActive(true);
                _currentPreview.transform.position = placementPosition;
                _currentPreview.transform.rotation = finalRotation;
            }
        }
        else
        {
            if (_currentPreview != null) _currentPreview.SetActive(false);
        }
    }

    private void ConfirmPlacement()
    {
        if (!_isHolding) return;
        
        if (DominoPrefab != null && _currentPreview != null && _currentPreview.activeSelf)
        {
            Instantiate(DominoPrefab, _currentPreview.transform.position, _currentPreview.transform.rotation);
        }
        _isHolding = false;
        // 回転は維持するか、リセットするか？（ここでは維持する仕様）
    }

    // ... (SetPlacementModeActiveなどは前と同じ) ...
    public void SetPlacementModeActive(bool isActive)
    {
        if (isActive) { _controls.Building.Enable(); this.enabled = true; }
        else { _controls.Building.Disable(); this.enabled = false; _isHolding = false; if(_currentPreview) _currentPreview.SetActive(false); }
    }
    private void OnDisable() { _controls?.Building.Disable(); }
    private void OnDestroy() { _controls?.Dispose(); }
}