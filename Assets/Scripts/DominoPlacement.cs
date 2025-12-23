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

    [Header("緊張感演出")]
    public float shakeIntensity = 0.05f; // ドミノの震えの強さ

    private GameObject _heldDomino; // 現在ホールド中のドミノ実体
    private bool _isHolding = false;
    private Quaternion _currentRotationY = Quaternion.identity;
    
    // 入力用変数
    private bool _inputPlaceButton;
    private Vector2 _mouseDelta;


    void Start()
    {
        // もしインスペクターで割り当てを忘れていたら、シーン内から探す
        if (hudManager == null)
        {
            hudManager = FindFirstObjectByType<HUDManager>();
        }

        if (hudManager == null)
        {
            Debug.LogError("DominoPlacement: HUDManagerが見つかりません！Canvasにスクリプトがついているか確認してください。");
        }
    }
    void Update()
    {
        if (!_isHolding)
        {
            // ホールドしていない時は、設置予定場所の計算のみ（必要なら）
            // レイキャストで地面の傾きなどを事前に取得しておくとスムーズです
        }
        else
        {
            // ホールド中の移動と回転
            UpdateHeldDomino();
        }
    }

    // DominoPlacement.cs の UpdatePlacementInput を修正

    public void UpdatePlacementInput(bool isPressed, Vector2 delta)
    {

        Debug.Log($"UpdatePlacementInput 呼び出し中: 押下={isPressed}, 選択スロット={HUDManager.Instance.GetSelectedSlotIndex()}");
        _mouseDelta = delta;

        // ★修正：インスペクターの変数ではなく、直接 Instance を見に行く
        if (HUDManager.Instance == null) return;

        // 1. HUDから現在選択中のドミノがあるか確認
        int selectedSlot = HUDManager.Instance.GetSelectedSlotIndex();
        if (selectedSlot == -1) return; // 何も選んでなければ何もしない
        // ★ガードを追加：hudManager がセットされていない場合はエラーを防ぐ
        if (hudManager == null) return;
        //Debug.Log("地面に");
        // 2. 入力に対する処理
        if (isPressed && !_inputPlaceButton) {
            Debug.Log("StartHoldingを実行します"); // ★追加
            StartHolding();
        } else if (!isPressed && _inputPlaceButton) {
            ReleaseDomino();
        }

        _inputPlaceButton = isPressed;

        // ★ここも Instance 経由で呼び出す
        HUDManager.Instance.UpdateHandShake(_isHolding);

        if (_isHolding) {
            UpdateHeldDomino();
        }
    }

    private void StartHolding()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        // ★追加：Sceneビューで視線を赤い線で表示（10メートルの長さ）
        Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 2.0f);
        //Debug.Log("地面" );
    if (Physics.Raycast(ray, out RaycastHit hit, 10f, groundLayer))
        {
            _isHolding = true;
            _heldDomino = Instantiate(dominoPrefab, hit.point, _currentRotationY);
            
            // ★修正：子オブジェクトも含めて全てのレイヤーを Ignore Raycast に変更
            SetLayerRecursive(_heldDomino, LayerMask.NameToLayer("Ignore Raycast"));

            Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;
        }

    }

    private void UpdateHeldDomino()
    {
        if (_heldDomino == null) return;

        // ★修正：画面中央(0.5, 0.5)ではなく、現在のマウス位置からRayを飛ばす
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);

        // ★追加：ホールド中もRayをSceneビューに表示（デバッグ用）
        Debug.DrawRay(ray.origin, ray.direction * 50f, Color.green);

        if (Physics.Raycast(ray, out RaycastHit hit, 50f, groundLayer))
        {
            // 震えを足す
            Vector3 shake = Random.insideUnitSphere * shakeIntensity;
            
            // ヒットした地面の場所にドミノを移動
            _heldDomino.transform.position = hit.point + (hit.normal * placementOffset) + shake;
            
            // 地面の傾きに合わせつつ、現在の回転(Y)を適用
            _heldDomino.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * _currentRotationY;
        }
    }

    private void ReleaseDomino()
    {
        if (_heldDomino != null)
        {
            // ★修正：設置時は全てのパーツを Default（または元のレイヤー）に戻す
            SetLayerRecursive(_heldDomino, LayerMask.NameToLayer("Default"));
            // 物理演算を再開
            Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            // TODO: ここでプレイヤーの「置く」アニメーションを再生
            Debug.Log("Domino Released! アニメーション再生開始");
        }
        _isHolding = false;
        _heldDomino = null;
    }

    // ★追加：親子構造をすべて辿ってレイヤーを変えるヘルパーメソッド
    private void SetLayerRecursive(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            SetLayerRecursive(child.gameObject, newLayer);
        }
    }

    // ThirdPersonController の UpdateState() から呼ばれる
    // DominoPlacement.cs 内

    public void SetPlacementModeActive(bool isActive)
    {
        this.enabled = isActive;

        // ★追加：マウスカーソルのロック・表示設定
        if (isActive)
        {
            Cursor.lockState = CursorLockMode.None; // ロック解除
            Cursor.visible = true;                // カーソルを表示
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 再ロック（通常時）
            Cursor.visible = false;                   // 非表示
        }

        if (!isActive)
        {
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