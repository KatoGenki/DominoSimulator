using UnityEngine;
using StarterAssets;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging;

public class DominoPlacement : MonoBehaviour
{
    [Header("参照")]
    public GameObject dominoPrefab;
    public LayerMask groundLayer;

    [Header("設定")]
    public float placementOffset = 0.5f;
    public float rotationSensitivity = 2.0f;
    public float minMouseSpeed = 1.0f;

    [Header("IK Settings")]
    [SerializeField] private Rig rightHandRig;
    [SerializeField] private Transform handIKTarget;
    [SerializeField] private float weightLerpSpeed = 5f;
    [SerializeField] private Vector3 handOffset = new Vector3(0.1f, 0.1f, 0f);
    [SerializeField] private Vector3 handRotationOffset = new Vector3(0f, -90f, 0f);

    private bool _isPlacementModeActive = false;
    private GameObject _heldDomino;
    private Quaternion _currentRotationY = Quaternion.identity;
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
        }

        if (_heldDomino != null)
        {
            // 2. 【移動】右クリックホールド中のみ、地面に沿って移動
            if (mouse.rightButton.isPressed)
            {
                MoveDominoWithMouse();
            }

            // 3. 【回転】左クリックホールド中は回転を検知
            if (mouse.leftButton.isPressed)
            {
                DetectCircularMotion();
                _heldDomino.transform.rotation = _currentRotationY;
            }

            // 4. 【終了】左クリックを離した瞬間に設置（物理有効化）
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                PlaceDomino();
            }
        }

        // 共通更新
        UpdateIKWeight();
        UpdateHandPosition();
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

        _currentRotationY = _heldDomino.transform.rotation;
        
        // 最初はマウスポインタの地点に移動
        MoveDominoWithMouse(); 
        Debug.Log("Domino Grabbed (Left Click)");
    }

    private void MoveDominoWithMouse()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, groundLayer))
        {
            Vector3 targetPos = hit.point + Vector3.up * placementOffset;
            _heldDomino.transform.position = targetPos;
        }
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
        if (_mouseDelta.magnitude < minMouseSpeed || _previousMouseDelta.magnitude < minMouseSpeed) return;
        float angleChange = Vector2.SignedAngle(_previousMouseDelta, _mouseDelta);
        _currentRotationY *= Quaternion.Euler(0, angleChange * rotationSensitivity, 0);
    }

    private void UpdateIKWeight()
    {
        if (rightHandRig == null) return;
        float targetWeight = (_heldDomino != null) ? 1f : 0f;
        rightHandRig.weight = Mathf.Lerp(rightHandRig.weight, targetWeight, Time.deltaTime * weightLerpSpeed);
    }

    private void UpdateHandPosition()
    {
        // 既に生成されているプレビュー用ドミノ、またはホールド中のドミノの位置を取得
        // DominoPlacementの既存ロジックで _heldDomino は常にマウスポインタの位置に更新されているためこれを利用
        if (_heldDomino != null)
        {
            Vector3 offset = handOffset;
            offset.y -= 1.0f; // Y座標をさらに下げる
            handIKTarget.position = _heldDomino.transform.position + _heldDomino.transform.TransformDirection(offset);
            handIKTarget.rotation = _heldDomino.transform.rotation * Quaternion.Euler(handRotationOffset); // 90度は調整
        }
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