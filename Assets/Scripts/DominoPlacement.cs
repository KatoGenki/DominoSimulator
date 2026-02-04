using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class DominoPlacement : MonoBehaviour
{
    [Header("参照")]
    public GameObject dominoPrefab;
    public LayerMask Ground;

    [Header("設定")]
    public float placementOffset = 0.5f;
    public float rotationSensitivity = 2.0f;
    public float minMouseSpeed = 0.1f;
    public Transform fpsCameraTransform; // FPSカメラのTransform

    [Header("IK Settings")]
    [SerializeField] private Rig rightHandRig;
    [SerializeField] private Transform handIKTarget;
    [SerializeField] private float weightLerpSpeed = 5f;
        [SerializeField] private Vector3 handRestingOffset = new Vector3(0.3f, -0.4f, 0.5f); // カメラからの相対位置

    [Header("Hand Offset Settings")]
    [SerializeField] private Vector3 handOffset = new Vector3(0.1f, 0.1f, 0f);
    [SerializeField] private Vector3 handRotationOffset = new Vector3(30f, 150f, 270f);
    
    [Header("Domino Spawn Offset")]
    [SerializeField] private Vector3 dominoSpawnOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 dominoSpawnRotationOffset = new Vector3(90f, 0f, 0f);


    private bool _isPlacementModeActive = false;
    private GameObject _heldDomino;
    private Quaternion _handIKTargetRotation = Quaternion.identity;  // handIKTarget専用の回転
    private Vector3 _dominoInitialRelativePosition = Vector3.zero;   // ドミノの初期相対位置
    private bool _isRightClickHeld = false;                           // 右クリック状態
    private bool _isLeftClickHeld = false;                          // 左クリック状態
    private Vector2 _mouseDelta;
    private Vector2 _previousMouseDelta;

    void Update()
    {
        if (!_isPlacementModeActive) return;

        var mouse = Mouse.current;
        if (mouse == null) return;

        _mouseDelta = mouse.delta.ReadValue();

        // 1. 【開始】左クリックを押した瞬間にドミノを生成
        if (mouse.leftButton.wasPressedThisFrame && _heldDomino == null)
        {
            SpawnDominoPreview();
            _isLeftClickHeld = true;
        }

        if (_heldDomino != null)
        {
            // 2. 【移動】右クリックホールド中のみhandIKTarget位置を更新
            if (mouse.rightButton.wasPressedThisFrame)
            {
                // 右クリック開始時：マウスデルタをリセット（移動の基準点を現在位置に設定）
                _isRightClickHeld = true;
            }
            // 右クリックホールド中：マウスデルタをワールド座標に変換して移動
            if (mouse.rightButton.isPressed && _isRightClickHeld)
            {
                Vector2 screenMouseDelta = mouse.delta.ReadValue();
                //カメラの向きを基準に移動量を計算
                Vector3 cameraRight = Camera.main.transform.right;
                Vector3 cameraup = Camera.main.transform.up;
                Vector3 worldMovement = (cameraRight * screenMouseDelta.x + cameraup * screenMouseDelta.y) * 0.01f;
                handIKTarget.position += worldMovement;
            }
            else if (_isRightClickHeld && !mouse.rightButton.isPressed)
            {
                // 右クリック終了時にhandIKTarget位置を固定
                _isRightClickHeld = false;
            }

            // 3. 【回転】左クリックホールド中は回転を検知
            if (mouse.leftButton.isPressed)
            {
                DetectCircularMotion();
                // ドミノ自体の回転
                _heldDomino.transform.rotation = Quaternion.Euler(dominoSpawnRotationOffset);
                // ドミノ位置：handIKTarget + (回転適用した初期相対位置)
                _heldDomino.transform.position = handIKTarget.position + (handIKTarget.rotation * _dominoInitialRelativePosition);
            }

            // 4. 【終了】左クリックを離した瞬間に設置（物理有効化）
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                PlaceDomino();
                _isLeftClickHeld = false;
            }
        }

        // 共通更新
         UpdateIKWeight();
        // // 右クリック中以外はhandIKTarget位置を固定
        if (!_isRightClickHeld && !_isLeftClickHeld)
        {
            UpdateHandPosition();
        }
        _previousMouseDelta = _mouseDelta;
    }

    private void SpawnDominoPreview()
    {
        _heldDomino = Instantiate(dominoPrefab);
        // 生成時はコライダーをオフ（自分との衝突回避）
        var colliders = _heldDomino.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = false;

        Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // handIKTargetの位置と回転に合わせてドミノを生成
        if (handIKTarget != null)
        {
            // 初期相対位置をdominoSpawnOffsetで設定
            _dominoInitialRelativePosition = dominoSpawnOffset;
            // handIKTargetの回転をdominoSpawnRotationOffsetで初期化
            _handIKTargetRotation = Quaternion.Euler(dominoSpawnRotationOffset);
            
            _heldDomino.transform.position = handIKTarget.position + _dominoInitialRelativePosition;
            _heldDomino.transform.rotation = Quaternion.Euler(dominoSpawnRotationOffset);
        }
        else
        {
            // フォールバック: マウス位置を基準にドミノを配置
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out RaycastHit hit, 100f, Ground))
            {
                Vector3 targetPos = hit.point + Vector3.up * placementOffset;
                _heldDomino.transform.position = targetPos;
                handIKTarget.position = targetPos;
            }
        }

        Debug.Log("Domino Grabbed (Left Click)");
    }

    private void PlaceDomino()
    {
        // コライダーを戻す
        var colliders = _heldDomino.GetComponentsInChildren<Collider>();
        foreach (var col in colliders) col.enabled = true;

        // レイヤーを戻す（エラー回避用チェック付き）
        int dominoLayer = LayerMask.NameToLayer("Domino");
        if (dominoLayer != -1) SetLayerRecursive(_heldDomino, dominoLayer);

        // 物理挙動をON
        Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
        }

        _heldDomino = null;
        Debug.Log("Domino Placed (Left Click Released)");
    }

    private void DetectCircularMotion()
    {
        Debug.Log($"{handIKTarget.rotation}");
        if (_mouseDelta.magnitude < minMouseSpeed || _previousMouseDelta.magnitude < minMouseSpeed) 
        {
            Debug.Log("Mouse movement too small to detect rotation.");
            return;
        }
        float angleChange = Vector2.SignedAngle(_previousMouseDelta, _mouseDelta);
        //2/4追加　XとZ軸の回転量を調整して疑似的に水平回転を作りたい
        handIKTarget.rotation *= Quaternion.Euler(angleChange * 0.5f, 0f, angleChange * 0.3f);
    }

    //IKハンドルの重みと位置更新
    private void UpdateIKWeight()
    {
        if (rightHandRig == null) return;
        float targetWeight = (_heldDomino != null) ? 1f : 0f;
        rightHandRig.weight = Mathf.Lerp(rightHandRig.weight, targetWeight, Time.deltaTime * weightLerpSpeed);
    }

    private void UpdateHandPosition()
    {   // カメラからの相対位置に手を移動（固定）
        Vector3 targetPos = fpsCameraTransform.TransformPoint(handRestingOffset);
        handIKTarget.position = Vector3.Lerp(handIKTarget.position, targetPos, Time.deltaTime * 5f);
        //handITTarget.rotationを直接いじる方針を試すためコメントアウト
        //handIKTarget.rotation = fpsCameraTransform.rotation * Quaternion.Euler(150f,90f,90f);
    }

    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform) SetLayerRecursive(child.gameObject, newLayer);
    }

    public void SetPlacementModeActive(bool isActive)
    {
        _isPlacementModeActive = isActive;
        this.enabled = isActive;
        if (!isActive && _heldDomino != null)
        {
            Destroy(_heldDomino);
            if (rightHandRig != null) rightHandRig.weight = 0f;
        }
    }
}