using UnityEngine;
using StarterAssets;

public class DominoPlacement : MonoBehaviour
{
    [Header("参照")]
    public GameObject dominoPrefab; // 本物のドミノ
    public HUDManager hudManager;
    public LayerMask groundLayer;

    [Header("設定")]
    public float placementOffset = 0.5f;
    public float rotationSensitivity = 2.0f;

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
        _mouseDelta = delta;

        // ★修正：インスペクターの変数ではなく、直接 Instance を見に行く
        if (HUDManager.Instance == null) return;

        // 1. HUDから現在選択中のドミノがあるか確認
        int selectedSlot = HUDManager.Instance.GetSelectedSlotIndex();
        if (selectedSlot == -1) return; // 何も選んでなければ何もしない
        // ★ガードを追加：hudManager がセットされていない場合はエラーを防ぐ
        if (hudManager == null) return;

        // 2. 入力に対する処理
        if (isPressed && !_inputPlaceButton) {
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
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

        // ★追加：Sceneビューで視線を赤い線で表示（10メートルの長さ）
        Debug.DrawRay(ray.origin, ray.direction * 10f, Color.red, 2.0f);

        if (Physics.Raycast(ray, out RaycastHit hit, 10f, groundLayer))
        {
            Debug.Log("地面にヒットしました: " + hit.collider.name); // ★追加：ヒット確認ログ

            _heldDomino = Instantiate(dominoPrefab, hit.point + hit.normal * placementOffset, _currentRotationY);
            
            // 物理演算を一時的に無効化（空中に固定するため）
            Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = true;

            _isHolding = true;
        }
        else
        {
            Debug.Log("地面にヒットしませんでした。"); // ★追加：ヒットしなかった場合のログ
        }
    }
    private void UpdateHeldDomino()
    {
        if (_heldDomino == null) return;

        // 1. 円運動による回転
        float angleChange = Vector2.SignedAngle(Vector2.up, _mouseDelta); // 簡易的な円運動検知
        // ※実際には前フレームのdeltaとの比較ロジックを入れるとより精密になります
        _currentRotationY *= Quaternion.Euler(0, _mouseDelta.x * rotationSensitivity, 0);

        // 2. 位置の更新（常にカメラの中央付近の地面に追従）
        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, 10f, groundLayer))
        {
            _heldDomino.transform.position = hit.point + hit.normal * placementOffset;
            _heldDomino.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * _currentRotationY;
        }
    }

    private void ReleaseDomino()
    {
        if (_heldDomino != null)
        {
            // 物理演算を再開
            Rigidbody rb = _heldDomino.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;

            // TODO: ここでプレイヤーの「置く」アニメーションを再生
            Debug.Log("Domino Released! アニメーション再生開始");
        }
        _isHolding = false;
        _heldDomino = null;
    }

    // DominoPlacement.cs に以下のメソッドを上書き、または追加してください

    // ThirdPersonController の UpdateState() から呼ばれる
    public void SetPlacementModeActive(bool isActive)
    {
        // 1. スクリプト自体の動作を切り替え
        this.enabled = isActive;

        // 2. もし非アクティブ（立ち状態）になったら、ホールド中のドミノをクリーンアップ
        if (!isActive)
        {
            if (_heldDomino != null)
            {
                Destroy(_heldDomino); 
            }
            _isHolding = false;
            _heldDomino = null;
        }

        // 3. HUD（手アイコン）の表示切り替え
        // これにより「一人称（isAnyCrawl）の時だけ手が出る」が実現します
        if (hudManager != null)
        {
            hudManager.SetHandIconVisible(isActive);
        }
    }
}