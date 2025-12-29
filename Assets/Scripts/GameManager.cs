using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState { Build, Ready, Result }
    public GameState currentState = GameState.Build;

    [Header("制限時間設定")]
    public float timeLimit = 60f; // 60秒
    private float _remainingTime;

    [Header("ノルマスコア")]
    public int targetScore = 1000;

    [Header("リザルト判定設定")]
    public float finishCheckDelay = 3.0f; // 最後のドミノが倒れてからチェックまでの待ち時間
    private float _lastToppledTime;
    private bool _isActionPhase = false;

    private bool _isTimerStopped = false;

    

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        _remainingTime = timeLimit;
    }

    void Update()
    {
        if (currentState == GameState.Build && !_isTimerStopped)
        {
            UpdateTimer();
        }

        if (_isActionPhase && Time.time - _lastToppledTime > finishCheckDelay)
        {
            CheckFinalResult();
        }
    }

    private void UpdateTimer()
    {
        _remainingTime -= Time.deltaTime;

        // HUDに残り時間を伝える
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateTimerDisplay(_remainingTime);
        }

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

        // 設置モードを強制終了させる
        var player = Object.FindFirstObjectByType<StarterAssets.ThirdPersonController>();
        if (player != null)
        {
            // 状態をStandingに戻す
            player.CurrentState = StarterAssets.ThirdPersonController.PlayerState.Standing;
            // 内部のカメラ切り替えなどを実行させる
            // UpdateStateがprivateなら、publicの切替用メソッドを作って呼ぶのが安全です
        }

        Debug.Log("タイムアップ！設置終了。最初のドミノを倒してください。");
    }

    // 最終判定（ドミノが止まった後に呼ぶ想定）
    public void CheckResult()
    {
        currentState = GameState.Result;
        bool isClear = ScoreManager.Instance.totalScore >= targetScore;
        
        if (isClear) Debug.Log("ステージクリア！");
        else Debug.Log("スコア不足でゲームオーバー");
        
        // HUDManagerにリザルト表示を依頼する処理をここに追加
    }

    public void NotifyDominoToppled()
    {
        _lastToppledTime = Time.time;
        _isActionPhase = true;
    }

    private void CheckFinalResult()
    {
        _isActionPhase = false;
        
        // ここで「周囲1m以内に立っているドミノがないか」などの最終確認を行い
        // CheckResult() を呼び出す
        CheckResult();
    }
}