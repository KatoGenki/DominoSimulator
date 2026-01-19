using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

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

    [Header("IK Settings")]
    [SerializeField] private Rig rightHandRig;           // RightHandRigオブジェクトのRigコンポーネント
    [SerializeField] private Transform handIKTarget;     // 右手IKのTarget（追従させる空のGameObject）
    [SerializeField] private float weightLerpSpeed = 5f; // 重み切り替えの滑らかさ
    [SerializeField] private Vector3 handOffset = new Vector3(0.1f, 0.1f, 0f); // 手をドミノのどこに添えるか

    // 設置モードが有効かどうかを保持する変数
    private bool _isPlacementModeActive = false;

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

        // 1. IKの重み（Weight）を制御
        float targetWeight = _isPlacementModeActive ? 1f : 0f;
        if (rightHandRig != null)
        {
            rightHandRig.weight = Mathf.Lerp(rightHandRig.weight, targetWeight, Time.deltaTime * weightLerpSpeed);
        }

        // 2. 四つん這い状態なら、クリックの状態に関わらず手を追従させる
        if (_isPlacementModeActive && handIKTarget != null)
        {
            UpdateHandPosition();
        }

        if (_isHolding && _heldDomino != null)
            {
                // ★修正：ホールド中のドミノ自身のコライダーを無効化する
                // これにより、ドミノが体にめり込んでもプレイヤーが浮き上がらなくなります
                var colliders = _heldDomino.GetComponentsInChildren<Collider>();
                foreach (var col in colliders)
                {
                    col.enabled = false;
                }
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
        // HUDから選択中のドミノデータを取得
        DominoData selectedData = HUDManager.Instance.GetSelectedDominoData();

        // データがない、または残量が0なら置けない
        if (selectedData == null || selectedData.currentCount <= 0) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, groundLayer))
        {
            _isHolding = true;
            // 選択されたデータのプレファブを生成
            _heldDomino = Instantiate(selectedData.prefab, hit.point, _currentRotationY);
            
            // 残量を減らす
            HUDManager.Instance.UseSelectedDomino();

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

            // 地面の位置にオフセットと震えを加えて移動
            _heldDomino.transform.position = hit.point + (hit.normal * placementOffset);
            
            // 地面の傾斜(hit.normal)に合わせつつ、蓄積された回転(_currentRotationY)を適用
            _heldDomino.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * _currentRotationY;
        }
    }

    private void ReleaseDomino()
    {
        if (_heldDomino != null)
        {
            // 1. コライダーをすべて有効に戻す
            var colliders = _heldDomino.GetComponentsInChildren<Collider>();
            foreach (var col in colliders)
            {
                col.enabled = true;
                Debug.Log($"<color=green>Domino Collider Re-enabled: {col.name}</color>");
            }

            // 2. レイヤーを Domino に戻す
            int dominoLayer = LayerMask.NameToLayer("Domino");
            if (dominoLayer != -1)
            {
                SetLayerRecursive(_heldDomino, dominoLayer);
            }

            // 3. 物理を有効にする
            Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.useGravity = true;
            }

            Debug.Log("Domino Released and Physics Activated.");
        }

        // 最後にホールド状態を解除
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

    // ThirdPersonControllerから呼ばれるメソッド
    public void SetPlacementModeActive(bool isActive)
    {
        this.enabled = isActive;
        _isPlacementModeActive = isActive; // モード状態を記録

        if (isActive)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            if (_heldDomino != null) Destroy(_heldDomino);
            _isHolding = false;
            
            // モード終了時にIKの重みを即座に下げる準備
            if(rightHandRig != null) rightHandRig.weight = 0f; 
        }
    }
    private void UpdateHandPosition()
    {
        // 既に生成されているプレビュー用ドミノ、またはホールド中のドミノの位置を取得
        // DominoPlacementの既存ロジックで _heldDomino は常にマウスポインタの位置に更新されているためこれを利用
        if (_heldDomino != null)
        {
            handIKTarget.position = _heldDomino.transform.position + _heldDomino.transform.TransformDirection(handOffset);
            handIKTarget.rotation = _heldDomino.transform.rotation;
        }
    }

    
}