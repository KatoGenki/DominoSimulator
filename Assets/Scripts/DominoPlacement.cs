using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class DominoPlacement : MonoBehaviour
{
    [Header("参照")]
    public LayerMask Ground;

    [Header("設定")]
    public float placementOffset = 0.5f;
    public float rotationSensitivity = 2.0f;
    public float minMouseSpeed = 0.1f;
    public Transform fpsCameraTransform; // FPSカメラのTransform

    [Header("IK Settings")]
    [SerializeField] private Rig rightHandRig;
    [SerializeField] private Transform handIKTarget;
    [SerializeField] private float weightLerpSpeed = 20f;
        [SerializeField] private Vector3 handRestingOffset = new Vector3(0.3f, -0.4f, 0.5f); // カメラからの相対位置

    [Header("Hand Offset Settings")]
    [SerializeField]private Vector3 _handIKTargetRotation = new Vector3(150f, 150f, 90f);  // handIKTarget専用の回転
    [SerializeField] private float xAxisAngle = 0.5f;
    [SerializeField] private float zAxisAngle = 0.5f;

    
    [Header("Domino Spawn Offset")]
    [SerializeField] private Vector3 dominoSpawnOffset = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 dominoSpawnRotationOffset = new Vector3(90f, 0f, 0f);


    private bool _isPlacementModeActive = false;
    //ドミノを持っている間
    private GameObject _heldDomino;

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
            SpawnDominoFromInventory();
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
                // handIKTarget の角度にオフセットを足す（簡単に調整可能）
                _heldDomino.transform.rotation = Quaternion.Euler(handIKTarget.eulerAngles + dominoSpawnRotationOffset);
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

    private void SpawnDominoFromInventory()
    {
        if (HUDManager.Instance == null) return;

        // 現在HUDで選択されているドミノデータを取得
        // 直接インベントリを参照する
        int index = HUDManager.Instance.GetCurrentSelectedIndex(); 
        GameObject selectedPrefab = HUDManager.Instance.dominoInventory[index].prefab;
        DominoData data = HUDManager.Instance.dominoInventory[index];
        // ★追加：在庫が0なら生成しない
        if (data.currentCount <= 0 || data.prefab == null)
        {
            Debug.LogWarning($"{data.prefab} の在庫がありません！");
            return;
        }
        if (selectedPrefab == null) return;

        _heldDomino = Instantiate(selectedPrefab);

        Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
        if (rb) rb.isKinematic = true;

        // handIKTargetの位置と回転に合わせてドミノを生成
        if (handIKTarget != null && fpsCameraTransform != null)
        {
            // handIKTarget をカメラ基準の休止位置で初期化
            handIKTarget.position = fpsCameraTransform.TransformPoint(handRestingOffset);
            handIKTarget.rotation = fpsCameraTransform.rotation * Quaternion.Euler(_handIKTargetRotation);

            // ドミノの初期相対位置を handIKTarget に対するオフセットで設定
            _dominoInitialRelativePosition = dominoSpawnOffset;

            // ドミノを handIKTarget の位置 + 回転適用した相対位置に配置
            _heldDomino.transform.position = handIKTarget.position + (handIKTarget.rotation * _dominoInitialRelativePosition);
            _heldDomino.transform.rotation = handIKTarget.rotation * Quaternion.Euler(dominoSpawnRotationOffset);
        }
        else
        {
            Debug.LogWarning("handIKTarget または fpsCameraTransform が設定されていません。");
        }

        Debug.Log("Domino Grabbed (Left Click)");
    }

    private void PlaceDomino()
    {
        // インベントリの在庫を1つ減らす
        if (HUDManager.Instance != null)
        {
            int index = HUDManager.Instance.GetCurrentSelectedIndex();
            var data = HUDManager.Instance.dominoInventory[index];

            if (data.currentCount > 0)
            {
                data.currentCount--; // 残数をマイナス
                HUDManager.Instance.UpdateInventoryUI(); // UI表示を更新（後述のメソッド）
            }
        }

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
        handIKTarget.rotation *= Quaternion.Euler(angleChange * xAxisAngle, 0f, angleChange * zAxisAngle);
    }

    //IKハンドルの重みと位置更新
    public void UpdateIKWeight()
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
        handIKTarget.rotation = fpsCameraTransform.rotation * Quaternion.Euler(_handIKTargetRotation);
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