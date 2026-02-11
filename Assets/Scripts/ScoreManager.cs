using UnityEngine;
using StarterAssets;

public class ScoreManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static ScoreManager Instance { get; private set; }

    [Header("現在のスコア状況")]
    //現在の合計スコア
    public int totalScore = 0;
    //現在の連鎖数
    public int chainCount = 0;

    [Header("ボーナス倍率設定")]
    [Tooltip("1連鎖ごとに加算される倍率（例：0.01の場合、100連鎖で+1倍）")]
    public float chainBonusStep = 0.01f;
    CameraManager CameraManager;

    //起動時に呼ばれる関数
    void Awake()
    {   //インスタンスがnullであれば、自分をセット、そうでなければ自分を破棄
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 各ドミノから呼ばれるスコア加算メソッド
    /// </summary>
    /// <param name="basePoint">ドミノ固有のポイント</param>
    /// <param name="height">倒れた瞬間の高さ</param>
    public void AddScore(int basePoint, float height, Transform fallenDominoTransform)
    {
        // GameManagerが見つからない、もしくは制限時間内の場合はスコア加算を行わない
        if (GameManager.Instance == null || GameManager.Instance.currentState == GameManager.GameState.Build)
        {
            return; 
        }
        //連鎖数を加算
        chainCount++;

        // 【計算式】 1.0(基本) + (連鎖数ボーナス)
        float currentChainBonus = chainCount * chainBonusStep;
        
        float finalMultiplier = 1.0f + currentChainBonus;

        // 最終スコア加算
        int addValue = Mathf.RoundToInt(basePoint * finalMultiplier);
        totalScore += addValue;

        // HUDの表示を更新
        if (HUDManager.Instance != null)
        {   // スコア表示更新メソッドを呼び出し
            HUDManager.Instance.UpdateScoreDisplay(totalScore, chainCount);
        }

        Debug.Log($"[Score] {name} 倒れた! +{addValue}pt (連鎖倍率:+{currentChainBonus} )");

        if (CameraManager != null)
        {
            CameraManager.UpdateCameraTarget(fallenDominoTransform);
        }

    }
    //GameManagerから呼ばれて現在のスコアを伝えるメソッド
    public int GetCurrentScore()
    {
        return totalScore;
    }
    //連鎖数をリセットするメソッド
    //現状どこからも呼ばれていない
    public void ResetChain()
    {
        chainCount = 0;
    }
}