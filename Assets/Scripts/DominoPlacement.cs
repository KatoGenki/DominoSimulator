using UnityEngine;
using UnityEngine.InputSystem;
using StarterAssets; // ★追加: これがないとStarterAssetsInputsが見つかりません

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
    private bool _isHolding = false; // 現在ホールド（回転調整）中かどうか
    private Vector2 _mouseDelta;
    private Vector2 _previousMouseDelta; 
    private Quaternion _currentRotationY = Quaternion.identity;

    // ThirdPersonControllerから送られてくる入力状態
    private bool _inputPlaceButton = false; // ボタンが押されているか
    private bool _prevInputPlaceButton = false; // 前のフレームでボタンが押されていたか

    private StarterAssetsInputs _starterInputs;

    void Start()
    {
        // プレビュー初期化
        if (PreviewDomino != null)
        {
            _currentPreview = Instantiate(PreviewDomino, transform.position, Quaternion.identity);
            _currentPreview.SetActive(false);
        }

        // コンポーネント取得
        _starterInputs = GetComponent<StarterAssetsInputs>();

        if (_starterInputs == null)
        {
            Debug.LogError("DominoPlacement: StarterAssetsInputsコンポーネントが見つかりません。");
        }

        // ★削除: _controls = new PlayerControls(); などの記述は全て削除しました。
        // 入力は ThirdPersonController から UpdatePlacementInput を通じて受け取るため不要です。
    }

    void Update()
    {
        // --- 1. クリック状態の変化を検知 ---
        // 「押された瞬間」
        if (_inputPlaceButton && !_prevInputPlaceButton)
        {
            _isHolding = true;
        }
        // 「離された瞬間」
        if (!_inputPlaceButton && _prevInputPlaceButton)
        {
            ConfirmPlacement();
        }

        // 状態を更新
        _prevInputPlaceButton = _inputPlaceButton;


        // --- 2. メインロジック ---
        // 常に位置合わせを行う
        FindPlacementPosition();

        if (_isHolding)
        {
            // ホールド中は円運動を検知して回転させる
            DetectCircularMotion();
        }
        
        // 直前のベクトルを更新
        _previousMouseDelta = _mouseDelta;
    }

    private void DetectCircularMotion()
    {
        if (_mouseDelta.magnitude < MinMouseSpeed) return;
        if (_previousMouseDelta.magnitude < MinMouseSpeed) return;

        float angleChange = Vector2.SignedAngle(_previousMouseDelta, _mouseDelta);
        float rotationAmount = angleChange * RotationSensitivity;

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
            Quaternion groundTilt = Quaternion.FromToRotation(Vector3.up, hit.normal);
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
    }

    public void SetPlacementModeActive(bool isActive)
    {
        // ★修正: _controls の有効無効化処理を削除
        this.enabled = isActive;
        
        if (!isActive)
        {
             _isHolding = false;
             if(_currentPreview) _currentPreview.SetActive(false);
        }
    }

    // ThirdPersonController から毎フレーム呼ばれる
    public void UpdatePlacementInput(bool isPressed, Vector2 delta)
    {
        if (enabled) 
        {
            _inputPlaceButton = isPressed; // ボタンの状態を更新
            _mouseDelta = delta;           // マウスの動きを更新
        }
    }
}