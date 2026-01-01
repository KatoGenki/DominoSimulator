using System.Collections.Generic;
using UnityEngine;
using TMPro; // UI用に追加

namespace StarterAssets
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState
        {
            Build,   // ドミノ設置中
            Ready,   // 最初のドミノを倒す待ち・連鎖中
            Result   // 終了・スコア表示
        }

        [Header("Game State")]
        public GameState currentState = GameState.Build;
        public int targetScore = 1000; 

        [Header("UI References")]
        [SerializeField] private TextMeshProUGUI _timerText;
        [SerializeField] private TextMeshProUGUI _stateText;
        [SerializeField] private GameObject _resultPanel; // リザルト画面の親オブジェクト

        [Header("Timer Settings")]
        [SerializeField] private float _buildTimeLimit = 60f;
        private float _remainingTime;
        private bool _isTimerStopped = false;

        [Header("Domino Monitoring")]
        [SerializeField] private float _velocityThreshold = 0.05f;
        [SerializeField] private float _stopDurationThreshold = 0.5f;
        private List<NormalDomino> _activeDominoes = new List<NormalDomino>();
        private Dictionary<NormalDomino, float> _stopTimers = new Dictionary<NormalDomino, float>();

        [Header("Finish Detection (Camera 2)")]
        [SerializeField] private Camera _camera2;
        [SerializeField] private float _finishWaitTime = 2.0f;
        private float _finishTimer = 0f;
        private Plane[] _camera2Planes;

        [Header("Cinemachine Settings")]
        [SerializeField] private Cinemachine.CinemachineTargetGroup _targetGroup;

        public List<NormalDomino> ActiveDominoes => _activeDominoes;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            _remainingTime = _buildTimeLimit;
            currentState = GameState.Build;
            if (_resultPanel != null) _resultPanel.SetActive(false);
            UpdateUI();
        }

        private void Update()
        {
            switch (currentState)
            {
                case GameState.Build:
                    HandleBuildTimer();
                    break;

                case GameState.Ready:
                    UpdateActiveDominoes();
                    CheckGameFinishCondition();
                    break;
            }
            
            // 毎フレームUI（タイマーなど）を更新
            UpdateUI();
        }

        private void UpdateUI()
        {
            // タイマー表示の更新
            if (_timerText != null)
            {
                if (currentState == GameState.Build)
                    _timerText.text = $"Time: {_remainingTime:F1}s";
                else
                    _timerText.text = ""; // 設置フェーズ以外は非表示
            }

            // ステート表示の更新
            if (_stateText != null)
            {
                switch (currentState)
                {
                    case GameState.Build: _stateText.text = "Phase: Build"; break;
                    case GameState.Ready: _stateText.text = "Phase: Domino Run!"; break;
                    case GameState.Result: _stateText.text = "Finished!"; break;
                }
            }
        }

        private void HandleBuildTimer()
        {
            if (_isTimerStopped) return;

            _remainingTime -= Time.deltaTime;
            if (_remainingTime <= 0)
            {
                OnTimeUp();
            }
        }

        private void OnTimeUp()
        {
            _remainingTime = 0;
            _isTimerStopped = true;
            currentState = GameState.Ready;

            var player = Object.FindFirstObjectByType<ThirdPersonController>();
            if (player != null)
            {
                player.CurrentState = ThirdPersonController.PlayerState.Standing;
                player.ForceUpdateState();
            }
            Debug.Log("Time Up! Start Domino.");
        }

        public void RegisterActiveDomino(NormalDomino domino)
        {
            if (!_activeDominoes.Contains(domino))
            {
                _activeDominoes.Add(domino);
                _stopTimers[domino] = 0f;
                if (_targetGroup != null) _targetGroup.AddMember(domino.transform, 1f, 0.5f);
            }
        }

        private void UpdateActiveDominoes()
        {
            for (int i = _activeDominoes.Count - 1; i >= 0; i--)
            {
                NormalDomino domino = _activeDominoes[i];
                if (domino == null) continue;

                Rigidbody rb = domino.GetComponent<Rigidbody>();
                if (rb == null) continue;

                if (rb.linearVelocity.magnitude < _velocityThreshold && 
                    rb.angularVelocity.magnitude < _velocityThreshold)
                {
                    _stopTimers[domino] += Time.deltaTime;
                }
                else
                {
                    _stopTimers[domino] = 0f;
                }

                if (_stopTimers[domino] >= _stopDurationThreshold)
                {
                    if (_targetGroup != null) _targetGroup.RemoveMember(domino.transform);
                    _activeDominoes.RemoveAt(i);
                    _stopTimers.Remove(domino);
                }
            }
        }

        private void CheckGameFinishCondition()
        {
            if (ScoreManager.Instance != null && ScoreManager.Instance.GetCurrentScore() == 0) return;

            if (_activeDominoes.Count == 0)
            {
                ProcessFinishTimer();
                return;
            }

            _camera2Planes = GeometryUtility.CalculateFrustumPlanes(_camera2);
            bool anyDominoInView = false;

            foreach (var domino in _activeDominoes)
            {
                Collider col = domino.GetComponent<Collider>();
                if (col == null) continue;

                if (GeometryUtility.TestPlanesAABB(_camera2Planes, col.bounds))
                {
                    anyDominoInView = true;
                    break;
                }
            }

            if (!anyDominoInView) ProcessFinishTimer();
            else _finishTimer = 0f;
        }

        private void ProcessFinishTimer()
        {
            _finishTimer += Time.deltaTime;
            if (_finishTimer >= _finishWaitTime)
            {
                OnChainFinished();
            }
        }

        private void OnChainFinished()
        {
            currentState = GameState.Result;
            _finishTimer = 0f;
            
            // リザルトパネルを表示
            if (_resultPanel != null)
            {
                _resultPanel.SetActive(true);
            }
            
            Debug.Log("Chain Finished. Result Displayed.");
        }

        public float GetRemainingTime() => _remainingTime;
    }
}