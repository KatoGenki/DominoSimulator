using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;

public class DominoPlacement : MonoBehaviour
{
    [Header("参照")]
    public GameObject dominoPrefab; // 本物のドミノ
    public HUDManager hudManager;
    public LayerMask groundLayer;

    [Header("設定")]
    public float placementOffset = 0.5f;
    public float rotationSensitivity = 2.0f;
    public float minMouseSpeed = 1.0f;       // 誤作動防止用の最小速度

    [Header("緊張感演出")]
    public float shakeIntensity = 0.05f;    // ドミノの震えの強さ

    private GameObject _heldDomino;         // 現在ホールド中のドミノ実体
    private bool _isHolding = false;
    private Quaternion _currentRotationY = Quaternion.identity;
    
    private bool _inputPlaceButton;
    private Vector2 _mouseDelta;
    private Vector2 _previousMouseDelta;     // 1フレーム前の移動量

    // プロパティ：コントローラー側で参照可能にする（移動制限などのため）
    public bool IsManipulating => _isHolding;

    void Start()
    {
        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>();
        }

        if (hudManager == null)
        {
            Debug.LogError("DominoPlacement: HUDManagerが見つかりません！");
        }
    }

    void Update()
    {
        if (_isHolding)
        {
            // 円運動による回転検知
            DetectCircularMotion();
            
            // ドミノの物理的な位置と回転を反映
            UpdateHeldDomino();
        }

        // 1フレーム前の移動量を保存
        _previousMouseDelta = _mouseDelta;
    }

    /// <summary>
    /// ThirdPersonControllerから呼ばれる入力更新メソッド
    /// </summary>
    public void UpdatePlacementInput(bool isLeftPressed, Vector2 delta)
    {
        _mouseDelta = delta;

        if (HUDManager.Instance == null) return;

        // 1. 左クリックの押し下げ・離し検知
        if (isLeftPressed && !_inputPlaceButton) 
        {
            StartHolding();
        } 
        else if (!isLeftPressed && _inputPlaceButton) 
        {
            ReleaseDomino();
        }

        _inputPlaceButton = isLeftPressed;

        // 2. HUDの震え演出更新
        HUDManager.Instance.UpdateHandShake(_isHolding);

        // 3. 左クリックホールド中の追加回転（マウスのX移動量を回転に加算）
        if (_isHolding)
        {
            // マウスの横移動量を角度に変換して蓄積
            float rotationAmount = delta.x * rotationSensitivity;
            _currentRotationY *= Quaternion.Euler(0, rotationAmount, 0);
        }
    }

    private void StartHolding()
    {
        // HUDでスロットが選択されているかチェック
        if (HUDManager.Instance.GetSelectedSlotIndex() == -1) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, groundLayer))
        {
            _isHolding = true;
            _heldDomino = Instantiate(dominoPrefab, hit.point, _currentRotationY);
            
            // レイキャストが自分に当たらないようにレイヤー変更
            SetLayerRecursive(_heldDomino, LayerMask.NameToLayer("Ignore Raycast"));

            Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }
    }

    private void UpdateHeldDomino()
    {
        if (_heldDomino == null) return;

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, 50f, groundLayer))
        {
            // 緊張感による震え
            Vector3 shake = Random.insideUnitSphere * shakeIntensity;
            
            // 地面の位置にオフセットと震えを加えて移動
            _heldDomino.transform.position = hit.point + (hit.normal * placementOffset) + shake;
            
            // 地面の傾斜(hit.normal)に合わせつつ、蓄積された回転(_currentRotationY)を適用
            _heldDomino.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * _currentRotationY;
        }
    }

    private void ReleaseDomino()
    {
        if (_heldDomino != null)
        {
            // レイヤーを戻して物理演算を有効化
            SetLayerRecursive(_heldDomino, LayerMask.NameToLayer("Default"));
            
            Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            Debug.Log("Domino Released!");
        }
        _isHolding = false;
        _heldDomino = null;
    }

    private void DetectCircularMotion()
    {
        if (_mouseDelta.magnitude < minMouseSpeed || _previousMouseDelta.magnitude < minMouseSpeed) return;

        // 移動ベクトルの角度差を求めて回転に変換
        float angleChange = Vector2.SignedAngle(_previousMouseDelta, _mouseDelta);
        float rotationAmount = angleChange * rotationSensitivity;

        _currentRotationY *= Quaternion.Euler(0, rotationAmount, 0);
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    public void SetPlacementModeActive(bool isActive)
    {
        this.enabled = isActive;

        if (isActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // モード解除時にホールド中なら破棄
            if (_heldDomino != null) Destroy(_heldDomino);
            _isHolding = false;
            _heldDomino = null;
        }

        if (hudManager != null)
        {
            hudManager.SetHandIconVisible(isActive);
        }
    }
}