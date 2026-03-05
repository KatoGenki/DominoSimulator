using UnityEngine;
using Cinemachine;
using StarterAssets;
using System;
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{
    [Header("プレイ中のカメラ")]
    [SerializeField] private CinemachineVirtualCamera _FPSCamera;
    [SerializeField] private CinemachineVirtualCamera _TPSCamera;
    [Header("結果演出用カメラとUI")]
    [SerializeField] private CinemachineTargetGroup _targetGroup;
    [SerializeField] private CinemachineVirtualCamera _resultCamera;
    [SerializeField] private CinemachineFreeLook _groupFreeLookCamera;
    [SerializeField] private GameObject _wipeUI;

    [Header("ドミノ追従カメラ設定")]
    [Tooltip("ターゲットとして保持する直近ドミノの最大数")]
    [SerializeField] private int _maxTrackedDominos = 10;
    [Tooltip("一番新しいドミノの重み")]
    [SerializeField] private float _newestDominoWeight = 1.0f;
    [Tooltip("一番古いドミノの重み")]
    [SerializeField] private float _oldestDominoWeight = 0.3f;

    [Header("ドミノ分岐検出パラメータ")]
    [Tooltip("「隣り合っている」とみなすドミノ間の最大水平距離")]
    [SerializeField] private float _adjacencyDistance = 1.2f;
    [Tooltip("同一親から別方向へ広がるとみなす最小角度（度数法）")]
    [SerializeField] private float _branchAngleThreshold = 25f;
    [Tooltip("同一ルートとみなす方向の許容角度（度数法）")]
    [SerializeField] private float _branchMergeAngle = 10f;
    [Tooltip("隣接判定を行う際の高さ方向の最大差分")]
    [SerializeField] private float _maxAdjacencyHeight = 0.3f;
    [Tooltip("この倍率を超える距離のドミノは爆発などによる飛びとして分岐判定から除外する")]
    [SerializeField] private float _explosionOutlierMultiplier = 3f;
    [Tooltip("分岐の親候補を探す際に、_recentDominoTargets の末尾から何個までを見るか")]
    [SerializeField] private int _maxRecentForParentSearch = 10;
    [Tooltip("カメラ・ワイプで同時に扱う最大分岐ルート数（メイン含む）")]
    [SerializeField] private int _maxVisibleBranches = 3;

    [Header("分岐デバッグ設定")]
    [Tooltip("true の場合、ターゲット更新時に直近ドミノの座標をログ出力する")]
    [SerializeField] private bool _enableDebugDominoQueueLog = false;
    [Tooltip("デバッグログに出力する直近ドミノの最大個数")]
    [SerializeField] private int _debugLogCount = 5;

    [Header("分岐カメラ")]
    [Tooltip("通常のゲームプレイ／FreeLook 用のメインカメラ")]
    [SerializeField] private Camera _mainCamera;
    [Tooltip("分岐表示用の親ルートカメラ（3分割時は中央、2分割時は右）")]
    [SerializeField] private Camera _branchCameraParent;
    [Tooltip("分岐表示用の左ルートカメラ")]
    [SerializeField] private Camera _branchCameraLeft;
    [Tooltip("分岐表示用の右ルートカメラ")]
    [SerializeField] private Camera _branchCameraRight;
    [Header("分岐カメラ追従設定")]
    [Tooltip("ターゲットからどれだけ後方にカメラを置くか")]
    [SerializeField] private float _branchCameraDistance = 6f;
    [Tooltip("ターゲットからどれだけ上方向にカメラを置くか")]
    [SerializeField] private float _branchCameraHeight = 3f;
    [Tooltip("カメラが注視する位置の高さオフセット")]
    [SerializeField] private float _branchCameraLookAtHeight = 0.5f;

    private ThirdPersonController _thirdPersonController;
    private readonly Queue<Transform> _recentDominoTargets = new Queue<Transform>();

    // 分岐管理用
    private class BranchInfo
    {
        public Transform Parent;
        public readonly List<Transform> Heads = new List<Transform>();
    }

    // 親ドミノごとの分岐情報
    private readonly Dictionary<Transform, BranchInfo> _branchInfos = new Dictionary<Transform, BranchInfo>();

    /// <summary>
    /// 分岐が検出されたときに通知されるイベント（親ドミノと、優先度順に並んだ各分岐の先頭ドミノ）
    /// </summary>
    public event Action<Transform, List<Transform>> OnDominoBranchDetected;

    // BranchCamera 用の現在ターゲット
    private bool _isBranchViewActive = false;
    private Transform _branchParentTarget;
    private Transform _branchLeftTarget;
    private Transform _branchRightTarget;

    private void Start()
    {
        // 追加：シーン内からプレイヤーを探して変数に格納する
        _thirdPersonController = UnityEngine.Object.FindFirstObjectByType<ThirdPersonController>();
        // 初期状態のカメラ優先度設定
        if (_FPSCamera != null) _FPSCamera.Priority = 10;
        if (_TPSCamera != null) _TPSCamera.Priority = 20;
        if (_resultCamera != null) _resultCamera.Priority = 5;
        if (_groupFreeLookCamera != null) _groupFreeLookCamera.Priority = 0;
        if (_wipeUI != null) _wipeUI.SetActive(false);

        // 分岐カメラは初期状態では無効化しておく
        SetBranchCamerasActive(false);
    }
    private void Update()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || _thirdPersonController == null) return;

        // 現在が「設置モード」として振る舞うべき状況かを判定
        bool isBuildingMode = gameManager.currentState == GameManager.GameState.Build &&
                            (_thirdPersonController.CurrentState == ThirdPersonController.PlayerState.CrawlingIdle ||
                            _thirdPersonController.CurrentState == ThirdPersonController.PlayerState.CrawlingMove);

        if (!_isBranchViewActive)
        {
            // 通常時のみプレイヤーカメラの切り替えを行う
            if (isBuildingMode)
            {   
                SwitchToFPSCamera();
            }
            else
            {
                SwitchToTPSCamera(); 
            }
        }
    }

    public void SwitchToFPSCamera()
    {
        if (_FPSCamera != null) _FPSCamera.Priority = 20;
        if (_TPSCamera != null) _TPSCamera.Priority = 10;
    }

    private void SwitchToTPSCamera()
    {
        if (_TPSCamera != null) _TPSCamera.Priority = 20;
        if (_FPSCamera != null) _FPSCamera.Priority = 10;
    }
    public void UpdateCameraTarget(Transform newTarget)
    {
        if (_targetGroup == null)
        {
            Debug.LogWarning("CameraManager: TargetGroupが設定されていません！");
            return;
        }

        if (newTarget == null) return;

        // すでにキューに存在する場合はいったん除外して末尾に入れ直す
        if (_recentDominoTargets.Contains(newTarget))
        {
            var tempQueue = new Queue<Transform>(_recentDominoTargets.Count);
            foreach (var t in _recentDominoTargets)
            {
                if (t != null && t != newTarget)
                {
                    tempQueue.Enqueue(t);
                }
            }
            _recentDominoTargets.Clear();
            foreach (var t in tempQueue)
            {
                _recentDominoTargets.Enqueue(t);
            }
        }

        _recentDominoTargets.Enqueue(newTarget);

        // キューのサイズやnullを整理しながら、古いものから順に削除
        while (_recentDominoTargets.Count > 0 &&
              (_recentDominoTargets.Count > _maxTrackedDominos || _recentDominoTargets.Peek() == null))
        {
            var removed = _recentDominoTargets.Dequeue();
            if (removed != null)
            {
                _targetGroup.RemoveMember(removed);
            }
        }

        // TargetGroupに存在しないTransformを追加
        foreach (var t in _recentDominoTargets)
        {
            if (t == null) continue;

            bool alreadyMember = false;
            var currentMembers = _targetGroup.m_Targets;
            for (int i = 0; i < currentMembers.Length; i++)
            {
                if (currentMembers[i].target == t)
                {
                    alreadyMember = true;
                    break;
                }
            }

            if (!alreadyMember)
            {
                _targetGroup.AddMember(t, 1f, 0f);
            }
        }

        // 古いものほど軽く、新しいものほど重くなるように重みを再計算
        if (_recentDominoTargets.Count > 0)
        {
            var members = _targetGroup.m_Targets;

            int validCount = 0;
            foreach (var t in _recentDominoTargets)
            {
                if (t != null) validCount++;
            }

            if (validCount == 0) return;

            int index = 0;
            foreach (var t in _recentDominoTargets)
            {
                if (t == null) continue;

                float t01 = (validCount == 1) ? 1f : (float)index / (validCount - 1);
                float weight = Mathf.Lerp(_oldestDominoWeight, _newestDominoWeight, t01);

                for (int i = 0; i < members.Length; i++)
                {
                    if (members[i].target == t)
                    {
                        members[i].weight = weight;
                        break;
                    }
                }

                index++;
            }

            _targetGroup.m_Targets = members;
        }

        // 直近ドミノのデバッグ表示
        if (_enableDebugDominoQueueLog)
        {
            LogRecentDominoPositions(_debugLogCount);
        }

        // 分岐検出ロジック
        TryDetectBranch(newTarget);
    }

    public void SwitchToResultCamera()
    {
        // Debug.Log("カメラを結果演出用に切り替えました");
        // FreeLookカメラが設定されている場合は、ドミノ群を追うFreeLookをメインにする
        if (_groupFreeLookCamera != null)
        {
            // まず既存カメラの優先度を下げる（必要に応じて微調整してください）
            if (_FPSCamera != null) _FPSCamera.Priority = 5;
            if (_TPSCamera != null) _TPSCamera.Priority = 5;
            if (_resultCamera != null) _resultCamera.Priority = 10;

            // FreeLookを最優先にする
            _groupFreeLookCamera.Priority = 40;
        }
        else
        {
            // FreeLookが未設定の場合は従来どおり_resultCameraをメインにする
            if (_resultCamera != null) _resultCamera.Priority = 30;
        }

        if (_wipeUI != null) _wipeUI.SetActive(true);

        // 結果表示開始時点では通常の FreeLook 表示とし、分岐カメラはまだ非アクティブ
        _isBranchViewActive = false;
        SetBranchCamerasActive(false);
        if (_mainCamera != null) _mainCamera.enabled = true;
    }

    /// <summary>
    /// 右下ワイプから呼び出される分岐カメラ切り替えハンドラ。
    /// FreeLook ベースの結果カメラから、BranchCamera の 2～3分割表示に切り替える。
    /// </summary>
    public void OnBranchWipeClicked()
    {
        _isBranchViewActive = true;

        // FreeLook / メインカメラを無効化し、BranchCamera 群を有効化
        if (_mainCamera != null) _mainCamera.enabled = false;
        SetBranchCamerasActive(true);
    }

    /// <summary>
    /// _recentDominoTargets に格納されている Transform のうち、
    /// 最新のものから最大 maxCount 個の XZ 座標をログ出力する。
    /// </summary>
    /// <param name="maxCount">出力する最大個数</param>
    private void LogRecentDominoPositions(int maxCount)
    {
        if (maxCount <= 0 || _recentDominoTargets.Count == 0) return;

        var array = _recentDominoTargets.ToArray();
        if (array.Length == 0) return;

        // Queue.ToArray は先頭が古い要素になるので、末尾（最新）から逆順に見る
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.Append("[CameraManager] RecentDominos (latest first): ");

        int printed = 0;
        for (int i = array.Length - 1; i >= 0 && printed < maxCount; i--)
        {
            var t = array[i];
            if (t == null) continue;

            Vector3 pos = t.position;
            sb.AppendFormat("#{0}:({1:F2},{2:F2}) ", printed, pos.x, pos.z);
            printed++;
        }

        if (printed > 0)
        {
            Debug.Log(sb.ToString());
        }
    }

    private void LateUpdate()
    {
        if (_isBranchViewActive)
        {
            UpdateBranchCameras();
        }
    }

    /// <summary>
    /// 新しく倒れたドミノ newTarget をもとに、直近のドミノ履歴から
    /// 分岐を検出する。
    /// </summary>
    /// <param name="newTarget">新しくターゲットに追加されたドミノ</param>
    private void TryDetectBranch(Transform newTarget)
    {
        if (newTarget == null) return;

        // 爆発由来のドミノは分岐判定の対象外
        var newDomino = newTarget.GetComponent<DominoBase>();
        if (newDomino != null && (newDomino.WasHitByExplosion || newDomino.TouchedExplodedDomino))
        {
            return;
        }

        Transform parent = FindParentCandidate(newTarget);
        if (parent == null) return;

        var parentDomino = parent.GetComponent<DominoBase>();
        if (parentDomino != null && (parentDomino.WasHitByExplosion || parentDomino.TouchedExplodedDomino))
        {
            return;
        }

        Vector3 parentPos = parent.position;
        Vector3 newPos = newTarget.position;

        // 高さ差が大きすぎる場合は除外
        if (Mathf.Abs(newPos.y - parentPos.y) > _maxAdjacencyHeight)
        {
            return;
        }

        float horizontalDist = HorizontalDistance(parentPos, newPos);

        // 明らかに飛びすぎている場合は爆発などによる飛び石とみなして除外
        if (horizontalDist > _adjacencyDistance * _explosionOutlierMultiplier)
        {
            return;
        }

        // 隣接範囲外ならそもそも分岐候補にしない
        if (horizontalDist > _adjacencyDistance)
        {
            return;
        }

        if (!_branchInfos.TryGetValue(parent, out var branchInfo))
        {
            branchInfo = new BranchInfo { Parent = parent };
            _branchInfos[parent] = branchInfo;
        }

        Vector3 vNew = new Vector3(newPos.x - parentPos.x, 0f, newPos.z - parentPos.z);
        if (vNew.sqrMagnitude < 0.0001f) return;

        // 既存の各分岐方向と比較し、角度が小さいものは同一路線とみなす
        for (int i = 0; i < branchInfo.Heads.Count; i++)
        {
            var head = branchInfo.Heads[i];
            if (head == null) continue;

            Vector3 headPos = head.position;
            Vector3 vHead = new Vector3(headPos.x - parentPos.x, 0f, headPos.z - parentPos.z);
            if (vHead.sqrMagnitude < 0.0001f) continue;

            float angle = Vector3.Angle(vHead, vNew);
            if (angle < _branchMergeAngle)
            {
                // 同一路線として扱い、より遠くまで進んでいる方を先頭として保持する
                float existingDist = HorizontalDistance(parentPos, headPos);
                if (horizontalDist > existingDist)
                {
                    branchInfo.Heads[i] = newTarget;
                }
                return;
            }
        }

        // ここまで来たら新しい方向の分岐として扱う
        branchInfo.Heads.Add(newTarget);

        // 2 本以上の分岐が揃ったタイミングでイベント通知
        if (branchInfo.Heads.Count >= 2)
        {
            NotifyBranchDetected(branchInfo);
        }
    }

    /// <summary>
    /// _recentDominoTargets の末尾から最大 _maxRecentForParentSearch 個を見て、
    /// newTarget に最も近い候補を親ドミノとして返す。
    /// </summary>
    private Transform FindParentCandidate(Transform newTarget)
    {
        if (newTarget == null) return null;
        if (_recentDominoTargets.Count == 0) return null;

        var array = _recentDominoTargets.ToArray();
        if (array.Length == 0) return null;

        Vector3 newPos = newTarget.position;
        Transform bestParent = null;
        float bestDist = float.MaxValue;

        int inspected = 0;
        for (int i = array.Length - 1; i >= 0 && inspected < _maxRecentForParentSearch; i--)
        {
            var candidate = array[i];
            if (candidate == null || candidate == newTarget) continue;

            float dist = HorizontalDistance(candidate.position, newPos);
            if (dist < bestDist)
            {
                bestDist = dist;
                bestParent = candidate;
            }

            inspected++;
        }

        return bestParent;
    }

    /// <summary>
    /// 親ドミノとその分岐情報から、スコア上位の分岐ルートのみを選別してイベント通知する。
    /// スコアは「親からの水平距離」が大きいものを高スコアとする簡易版。
    /// </summary>
    private void NotifyBranchDetected(BranchInfo branchInfo)
    {
        if (branchInfo == null) return;
        if (OnDominoBranchDetected == null) return;

        Transform parent = branchInfo.Parent;
        if (parent == null) return;

        // null を除外した有効な先頭ドミノだけを抽出
        List<Transform> validHeads = new List<Transform>();
        foreach (var head in branchInfo.Heads)
        {
            if (head != null)
            {
                validHeads.Add(head);
            }
        }

        if (validHeads.Count < 2) return;

        Vector3 parentPos = parent.position;

        // 親からの水平距離が大きい順にソート（進行度が高いルートを優先）
        validHeads.Sort((a, b) =>
        {
            float da = HorizontalDistance(parentPos, a.position);
            float db = HorizontalDistance(parentPos, b.position);
            return db.CompareTo(da); // 降順
        });

        // カメラで扱う最大本数に制限
        int maxBranches = Mathf.Max(1, _maxVisibleBranches);
        if (validHeads.Count > maxBranches)
        {
            validHeads = validHeads.GetRange(0, maxBranches);
        }

        // デバッグ用ログ（どの親から何本の分岐が検出されたか）
        string log = $"[CameraManager] Branch detected from '{parent.name}'. Heads: ";
        for (int i = 0; i < validHeads.Count; i++)
        {
            var h = validHeads[i];
            if (h == null) continue;
            Vector3 p = h.position;
            log += $"#{i}:{h.name}({p.x:F2},{p.z:F2}) ";
        }
        Debug.Log(log);

        // BranchCamera 用ターゲット更新
        UpdateBranchCameraTargets(parent, validHeads);

        // 外部向けイベント
        OnDominoBranchDetected?.Invoke(parent, validHeads);
    }

    /// <summary>
    /// 水平距離（XZ 平面上の距離）を返すヘルパー。
    /// </summary>
    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    /// <summary>
    /// 分岐検出結果をもとに、BranchCamera が追従すべきターゲットを更新する。
    /// ルートが 3 本の場合: 親ルート=中央、分岐1=左、分岐2=右。
    /// ルートが 2 本の場合: 親ルート=右、分岐1=左。
    /// </summary>
    private void UpdateBranchCameraTargets(Transform parent, List<Transform> heads)
    {
        _branchParentTarget = null;
        _branchLeftTarget = null;
        _branchRightTarget = null;

        if (parent == null || heads == null || heads.Count == 0)
        {
            return;
        }

        if (heads.Count >= 2)
        {
            // 3ルート想定: 親 + 分岐2本
            _branchParentTarget = parent;
            _branchLeftTarget = heads[0];
            _branchRightTarget = heads[1];
        }
        else
        {
            // 2ルート想定: 親 + 分岐1本
            _branchParentTarget = parent;
            _branchLeftTarget = heads[0];
            _branchRightTarget = null;
        }
    }

    /// <summary>
    /// BranchCamera 群の有効／無効をまとめて切り替える。
    /// </summary>
    private void SetBranchCamerasActive(bool isActive)
    {
        if (_branchCameraParent != null) _branchCameraParent.enabled = isActive;
        if (_branchCameraLeft != null) _branchCameraLeft.enabled = isActive;
        if (_branchCameraRight != null) _branchCameraRight.enabled = isActive;
    }

    /// <summary>
    /// 現在のターゲット情報に基づき、BranchCamera のビューポートと追従を更新する。
    /// </summary>
    private void UpdateBranchCameras()
    {
        int routeCount = 0;
        if (_branchParentTarget != null) routeCount++;
        if (_branchLeftTarget != null) routeCount++;
        if (_branchRightTarget != null) routeCount++;

        if (routeCount == 0)
        {
            // まだ分岐情報がない場合は何もしない
            return;
        }

        // ルート数に応じてカメラの rect を設定
        if (routeCount >= 3 && _branchCameraParent != null && _branchCameraLeft != null && _branchCameraRight != null)
        {
            // 3分割: 左=分岐1, 中央=親, 右=分岐2
            _branchCameraLeft.rect = new Rect(0f, 0f, 1f / 3f, 1f);
            _branchCameraParent.rect = new Rect(1f / 3f, 0f, 1f / 3f, 1f);
            _branchCameraRight.rect = new Rect(2f / 3f, 0f, 1f / 3f, 1f);
        }
        else if (routeCount == 2)
        {
            // 2分割: 右に親ルート、左に分岐
            if (_branchCameraLeft != null)
            {
                _branchCameraLeft.rect = new Rect(0f, 0f, 0.5f, 1f);
            }
            if (_branchCameraParent != null)
            {
                _branchCameraParent.rect = new Rect(0.5f, 0f, 0.5f, 1f);
            }
            if (_branchCameraRight != null)
            {
                // 2ルート時は右カメラは未使用
                _branchCameraRight.enabled = false;
            }
        }

        // それぞれのカメラを対応するターゲットに追従させる
        if (_branchCameraParent != null && _branchParentTarget != null)
        {
            FollowTarget(_branchCameraParent, _branchParentTarget);
        }
        if (_branchCameraLeft != null && _branchLeftTarget != null)
        {
            FollowTarget(_branchCameraLeft, _branchLeftTarget);
        }
        if (_branchCameraRight != null && _branchRightTarget != null && _branchCameraRight.enabled)
        {
            FollowTarget(_branchCameraRight, _branchRightTarget);
        }
    }

    /// <summary>
    /// 単純なオフセット＋LookAt でターゲットを追従させるヘルパー。
    /// </summary>
    private void FollowTarget(Camera cam, Transform target)
    {
        if (cam == null || target == null) return;

        Vector3 targetPos = target.position;
        // 後方＆上から見下ろす位置に配置
        Vector3 offset = new Vector3(0f, _branchCameraHeight, -_branchCameraDistance);
        cam.transform.position = targetPos + offset;

        Vector3 lookAtPos = targetPos;
        lookAtPos.y += _branchCameraLookAtHeight;
        cam.transform.LookAt(lookAtPos);
    }
}
