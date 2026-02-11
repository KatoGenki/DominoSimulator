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

    // 物理的に動いているかの判定
    public bool IsMoving => _rb != null && _rb.linearVelocity.magnitude > 0.05f;

    protected virtual void Awake() => _rb = GetComponent<Rigidbody>();

    protected virtual void Start()
    {
        if (GameManager.Instance != null)
        {
            // GameManagerから自分だけのInstanceIDをもらう
            InstanceID = GameManager.Instance.RegisterDomino(this);
        }
    }

    protected virtual void Update()
    {
        if (!_isToppled && Vector3.Angle(Vector3.up, transform.up) > _toppleThreshold)
        {
            _isToppled = true;
            OnToppled();
        }
    }

    protected virtual void OnToppled()
    {
        if (ScoreManager.Instance != null)
            ScoreManager.Instance.AddScore(_scorePoint, transform.position.y, this.transform);

        if (CameraManager != null)
            CameraManager.UpdateCameraTarget(this.transform);
        
        Debug.Log($"Type:{DominoTypeID} / Instance:{InstanceID} が倒れました");
    }
}