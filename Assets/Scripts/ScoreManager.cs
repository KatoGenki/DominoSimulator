using UnityEngine;
using StarterAssets;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance { get; private set; }

    [Header("現在のスコア状況")]
    public int totalScore = 0;
    public int chainCount = 0;

    [Header("ボーナス倍率設定")]
    [Tooltip("高さ1mあたりに加算される倍率（例：0.5の場合、2mの高さで+1倍）")]
    public float heightMultiplier = 0.5f; 
    
    [Tooltip("1連鎖ごとに加算される倍率（例：0.01の場合、100連鎖で+1倍）")]
    public float chainBonusStep = 0.01f;

    void Awake()
    {
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
        // GameManagerの状態がReady（またはリザルト中）の時のみ加点する
        if (GameManager.Instance == null || GameManager.Instance.currentState == GameManager.GameState.Build)
        {
            return; 
        }

        chainCount++;

        // 【計算式】 1.0(基本) + (連鎖数ボーナス) + (高さボーナス)
        float currentChainBonus = chainCount * chainBonusStep;
        float currentHeightBonus = Mathf.Max(0, height) * heightMultiplier;
        
        float finalMultiplier = 1.0f + currentChainBonus + currentHeightBonus;

        // 最終スコア加算
        int addValue = Mathf.RoundToInt(basePoint * finalMultiplier);
        totalScore += addValue;

        // HUDの表示を更新
        if (HUDManager.Instance != null)
        {
            HUDManager.Instance.UpdateScoreDisplay(totalScore, chainCount);
        }

        Debug.Log($"[Score] {name} 倒れた! +{addValue}pt (連鎖倍率:+{currentChainBonus} / 高さ倍率:+{currentHeightBonus})");

        if (GameManager.Instance != null)
        {
            GameManager.Instance.UpdateCameraTarget(fallenDominoTransform);
        }

    }

    public int GetCurrentScore()
    {
        return totalScore;
    }
    public void ResetChain()
    {
        chainCount = 0;
    }
}