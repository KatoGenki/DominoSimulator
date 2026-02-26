using System.Collections.Generic;
using UnityEngine;
using TMPro;

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
        [SerializeField] private GameObject _resultPanel;

        [Header("Timer Settings")]
        [SerializeField] private float _buildTimeLimit = 60f;
        private float _remainingTime;
        private bool _isTimerStopped = false;

        [Header("ID Management")]
        // インスタンスIDをキーに、配置された全ドミノを保持
        private Dictionary<int, DominoBase> _dominoRegistry = new Dictionary<int, DominoBase>();
        private int _instanceIdCounter = 0;

        [Header("Finish Detection")]
        [SerializeField] private float _finishWaitTime = 2.0f;
        private float _finishTimer = 0f;

        [Header("Camera & Visuals")]
        [SerializeField] private GameObject _wipeUI;

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
            if (_wipeUI != null) _wipeUI.SetActive(false);
            UpdateUI();
        }

        private void Update()
        {
            UpdateUI();

            switch (currentState)
            {
                case GameState.Build:
                    HandleBuildTimer();
                    break;

                case GameState.Ready:
                    CheckChainStatus();
                    break;
            }
        }

        // --- ID登録・管理システム ---

        /// <summary>
        /// ドミノが生成されたときに自分を登録し、個体識別用のInstanceIDを発行する
        /// </summary>
        public int RegisterDomino(DominoBase domino)
        {
            _instanceIdCounter++;
            _dominoRegistry.Add(_instanceIdCounter, domino);
            return _instanceIdCounter;
        }

        /// <summary>
        /// 特定の種類(TypeID)のドミノが現在シーンにいくつあるか集計する
        /// </summary>
        public int GetCountByType(int typeID)
        {
            int count = 0;
            foreach (var d in _dominoRegistry.Values)
            {
                if (d.DominoTypeID == typeID) count++;
            }
            return count;
        }

        // --- 連鎖監視・終了判定 ---

        private void CheckChainStatus()
        {
            bool anyMoving = false;
            var scoreManager = ScoreManager.Instance;

            // 全てのドミノをループして動きをチェック
            foreach (var domino in _dominoRegistry.Values)
            {
                if (domino == null) continue;
                if (domino.IsMoving)
                {
                    anyMoving = true;
                    break;
                }
            }


            // 動いているドミノがない場合、終了タイマーを進める
            // ※スコアが0（一度も倒れていない）場合は開始待ちなので除外
            if (!anyMoving && scoreManager != null && scoreManager.totalScore > 0)
            {
                _finishTimer += Time.deltaTime;
                if (_finishTimer >= _finishWaitTime)
                {
                    Debug.Log("連鎖終了！");
                    OnChainFinished();
                }
            }
            else
            {
                Debug.Log("anyMoving: " + anyMoving);
                Debug.Log("scoreManager.totalScore: " + scoreManager.totalScore);
                _finishTimer = 0f;
            }
        }

        // --- モード切り替えロジック ---

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
            }
            Debug.Log("Time Up! Ready to start chain.");
        }

        private void OnChainFinished()
        {
            if (currentState == GameState.Result) return;

            currentState = GameState.Result;
            _finishTimer = 0f;
            var scoreManager = ScoreManager.Instance;
            var resultUIManager = ResultUIManager.Instance;

            if (resultUIManager != null && scoreManager != null)
            {
                Debug.Log("ふへへ、お兄ちゃん♡　スコアは" + scoreManager.totalScore + "点だよ♡");
                resultUIManager.SetEachDominoCountTexts(); // ドミノの種類と個数をテキストにセット
                resultUIManager.ActiveResultUI();
                if (_resultPanel != null) _resultPanel.SetActive(true);
                resultUIManager.ActivechainUI();
                resultUIManager.ActiveMultiplierUI();
                resultUIManager.ActiveTotalScoreUI();
            }
            Debug.Log("Chain Finished! Displaying Results.");
        }

        //UI操作
        private void UpdateUI()
        {
            if (_timerText != null)
            {
                _timerText.text = (currentState == GameState.Build) ? $"Time: {_remainingTime:F1}s" : "";
            }

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

        public float GetRemainingTime() => _remainingTime;
    }
}