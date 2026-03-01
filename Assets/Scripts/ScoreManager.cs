using UnityEngine;
using System.Collections.Generic;
using StarterAssets;

/// <summary> 倍率の種類と値を保持（リザルト表示用） </summary>
public struct MultiplierInfo
{
    public string displayName;
    public float value;
}

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
    // IDごとの倒れた個数を記録するDictionary
    // Key: DominoTypeID, Value: 倒れた数
    public Dictionary<int, int> fallenDominoCounts = new Dictionary<int, int>();

    // IDに対応する名称を一時的に保持する辞書（表示用）
    public Dictionary<int, string> dominoNames = new Dictionary<int, string>();

    // IDごとの基礎ポイント（初回登録時に保存）
    private Dictionary<int, int> _dominoBasePoints = new Dictionary<int, int>();

    //起動時に呼ばれる関数
    void Awake()
    {   //インスタンスがnullであれば、自分をセット、そうでなければ自分を破棄
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        CameraManager = Object.FindFirstObjectByType<CameraManager>();
    }

    /// <summary>
    /// 各ドミノから呼ばれるスコア加算メソッド
    /// </summary>
    /// <param name="basePoint">ドミノ固有のポイント</param>
    /// <param name="height">倒れた瞬間の高さ</param>
    public void AddScore(int basePoint, float height, Transform fallenDominoTransform)
    {
        _ = height;
        //Debug.Log("AddScore呼ばれた！");
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
        //IDベースの集計
        // 倒れたオブジェクトからDominoBaseを取得
        DominoBase domino = fallenDominoTransform.GetComponent<DominoBase>();
        if (domino != null)
        {
            int id = domino.DominoTypeID;
            
            // 個数をカウント
            if (fallenDominoCounts.TryGetValue(id, out int currentCount))
            {
                fallenDominoCounts[id] = currentCount + 1;
            }
            else
            {
                fallenDominoCounts.Add(id, 1);
            }

            // 表示用に名称も紐づけておく（初回のみ）
            if (!dominoNames.ContainsKey(id))
            {
                dominoNames.Add(id, domino.name.Replace("(Clone)", "").Trim());
            }
            // 基礎ポイントを紐づけておく（初回のみ）
            if (!_dominoBasePoints.ContainsKey(id))
            {
                _dominoBasePoints.Add(id, basePoint);
            }
        }
        // HUDの表示を更新
        if (HUDManager.Instance != null)
        {   // スコア表示更新メソッドを呼び出し
            HUDManager.Instance.UpdateScoreDisplay(totalScore, chainCount);
        }

        //Debug.Log($"[Score] {name} 倒れた! +{addValue}pt (連鎖倍率:+{currentChainBonus} )");

        if (CameraManager != null)
        {
            CameraManager.UpdateCameraTarget(fallenDominoTransform);
        }

    }
    /// <summary>
    /// リザルト表示用に、現在有効な倍率の一覧を取得する。
    /// 新しいボーナス種類を追加する場合は、このメソッドにエントリを追加する。
    /// </summary>
    public List<MultiplierInfo> GetActiveMultipliersForDisplay()
    {
        var list = new List<MultiplierInfo>();

        // ChainBonus: 連鎖数に応じた倍率
        float chainBonus = chainCount * chainBonusStep;
        float chainMultiplier = 1.0f + chainBonus;
        list.Add(new MultiplierInfo { displayName = "ChainBonus", value = chainMultiplier });

        // 将来的なボーナス追加例:
        // list.Add(new MultiplierInfo { displayName = "HeightBonus", value = heightMultiplier });

        return list;
    }

    /// <summary> ドミノの基礎ポイントの合計（種類×個数の合計） </summary>
    public int GetTotalBasePoints()
    {
        int total = 0;
        foreach (var kv in fallenDominoCounts)
        {
            if (_dominoBasePoints.TryGetValue(kv.Key, out int basePoint))
                total += basePoint * kv.Value;
        }
        return total;
    }

    /// <summary> 全倍率を掛け合わせた合計倍率（スコア計算に使用） </summary>
    public float GetTotalMultiplier()
    {
        float total = 1.0f;
        foreach (var m in GetActiveMultipliersForDisplay())
        {
            total *= m.value;
        }
        return total;
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