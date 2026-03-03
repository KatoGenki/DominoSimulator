using UnityEngine;
using Cinemachine;
using StarterAssets;
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

    private ThirdPersonController _thirdPersonController;
    private readonly Queue<Transform> _recentDominoTargets = new Queue<Transform>();

    private void Start()
    {
        // 追加：シーン内からプレイヤーを探して変数に格納する
        _thirdPersonController = Object.FindFirstObjectByType<ThirdPersonController>();
        // 初期状態のカメラ優先度設定
        if (_FPSCamera != null) _FPSCamera.Priority = 10;
        if (_TPSCamera != null) _TPSCamera.Priority = 20;
        if (_resultCamera != null) _resultCamera.Priority = 5;
        if (_groupFreeLookCamera != null) _groupFreeLookCamera.Priority = 0;
        if (_wipeUI != null) _wipeUI.SetActive(false);
    }
    private void Update()
    {
        var gameManager = GameManager.Instance;
        if (gameManager == null || _thirdPersonController == null) return;

        // 現在が「設置モード」として振る舞うべき状況かを判定
        bool isBuildingMode = gameManager.currentState == GameManager.GameState.Build &&
                            (_thirdPersonController.CurrentState == ThirdPersonController.PlayerState.CrawlingIdle ||
                            _thirdPersonController.CurrentState == ThirdPersonController.PlayerState.CrawlingMove);

        if (isBuildingMode)
        {   
            SwitchToFPSCamera();
        }
        else
        {
            SwitchToTPSCamera(); 
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
    }
}
