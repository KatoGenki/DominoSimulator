using UnityEngine;
using StarterAssets;

public abstract class DominoBase : MonoBehaviour
{
    [Header("ID Settings")]
    [Tooltip("種類ごとの固定ID (例: S=100, M=200, L=300)")]
    [SerializeField] private int _dominoTypeID; 
    public int DominoTypeID => _dominoTypeID;

    // プレイ中に割り振られる一意の番号
    public int InstanceID { get; private set; }

    [Header("Base Parameters")]
    [SerializeField] protected int _scorePoint = 10;
    [SerializeField] protected float _toppleThreshold = 30f;
    
    protected bool _isToppled = false;
    protected Rigidbody _rb;
    CameraManager CameraManager;
    //設置時の角度を記録するための変数
    private Quaternion _initialRotation;
    // 物理的に動いているかの判定
    public bool IsMoving => _rb != null && _rb.linearVelocity.magnitude > 0.05f;

    protected virtual void Awake() => _rb = GetComponent<Rigidbody>();

    protected virtual void Start()
    {
        _initialRotation = transform.rotation;
        if (GameManager.Instance != null)
        {
            // GameManagerから自分だけのInstanceIDをもらう
            InstanceID = GameManager.Instance.RegisterDomino(this);
        }
    }

    protected virtual void Update()
    {
        //「初期の回転」と「現在の回転」の間の角度差（degree）を計算
        float angleDiff = Quaternion.Angle(_initialRotation, transform.rotation);

        //差分がしきい値を超えたら「倒れた」とみなす
        if (!_isToppled && angleDiff > _toppleThreshold)
        {
            //Debug.Log($"[DominoBase] {name} が倒れ始めました！変化した角度: {angleDiff}");
            _isToppled = true;
            OnToppled();
        }
    }

    protected virtual void OnToppled()
    {
        //Debug.Log($"[DominoBase] {name} が倒れました！");
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(_scorePoint, transform.position.y, this.transform);

        if (CameraManager != null)
            CameraManager.UpdateCameraTarget(this.transform);
    }
}