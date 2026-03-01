using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class ResultUIManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static ResultUIManager Instance { get; private set; }

    [Header("リザルトUI")]
    public Image BackgroundImage;
    public TextMeshProUGUI ResultText;
    public TextMeshProUGUI ChainText;
    public List<TextMeshProUGUI> DominoKindsTexts; // ドミノ種類テキスト
    public List<TextMeshProUGUI> DominopiecesTexts; // ドミノの個数表示用テキストリスト
    public TextMeshProUGUI TotalChainText;
    public TextMeshProUGUI MultiplierText;
    public List<TextMeshProUGUI> EachDominoMultiplierTexts; // ドミノごとのスコア表示用テキストリスト
    public TextMeshProUGUI TotalMultiplierText;
    public TextMeshProUGUI SymbolText1;
    public TextMeshProUGUI SymbolText2;
    public TextMeshProUGUI TotalScoreText;
    private readonly List<int> _dominoPieceTargetCounts = new List<int>();
    private int _fallenDominoTypeCount = 0; // 倒れたドミノの種類数
    private int _activeMultiplierCount = 0; // 表示する倍率の種類数

    void Awake()
    {
        //ResultUIManagerのインスタンスをセット
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    } 
    void Start()
    {
        // 初期状態では非表示にする
        gameObject.SetActive(false);
    }

    public void ActiveResultUI()
    {
        StopAllCoroutines();
        this.gameObject.SetActive(true);
        ResetResultElementVisibility();
        // ResultTextを表示
        if (ResultText != null) 
        {
            // ① 背景 + RESULT
            ShowUIWithDelay(0.0f, BackgroundImage.gameObject);
            ShowUIWithDelay(0.0f, ResultText.gameObject); 
        }
    }
    public void ActivechainUI()
    {
        if (ChainText != null) 
        {
            // ② ChainText
            ShowUIWithDelay(0.5f, ChainText.gameObject);
        }
        for (int i = 0; i < _fallenDominoTypeCount && i < DominoKindsTexts.Count; i++)
        {
            // ③ DominoKindsTexts（倒れた種類分のみ）
            ShowUIWithDelay(1.0f, DominoKindsTexts[i].gameObject); 
        }
        for (int i = 0; i < _fallenDominoTypeCount && i < DominopiecesTexts.Count; i++)
        {
            // ④ DominopiecesTexts（倒れた種類分のみ、1からカウントアップ）
            int target = i < _dominoPieceTargetCounts.Count ? _dominoPieceTargetCounts[i] : 0;
            ShowDominoPieceWithCountUpDelay(1.5f, DominopiecesTexts[i], target);
        }
        if (TotalChainText != null) 
        {
            // ⑤ TotalChainText
            ShowUIWithDelay(2.0f, TotalChainText.gameObject); 
        }
    }

    public void ActiveMultiplierUI()
    {
        // ⑦ MultiplierText
        if (MultiplierText != null) 
        {
            ShowUIWithDelay(3.0f, MultiplierText.gameObject);
        }
        // ⑧ EachDominoMultiplierTexts（倍率種類分を表示）
        for (int i = 0; i < _activeMultiplierCount && i < EachDominoMultiplierTexts.Count; i++)
        {
            if (EachDominoMultiplierTexts[i] != null)
                ShowUIWithDelay(3.4f, EachDominoMultiplierTexts[i].gameObject); 
        }
        // ⑨ TotalMultiplierText
        if (TotalMultiplierText != null) 
        {
            ShowUIWithDelay(3.9f, TotalMultiplierText.gameObject); 
        }
    }

    public void ActiveTotalScoreUI()
    {
        // ⑥ SymbolText1
        if (SymbolText1 != null) 
        {
            ShowUIWithDelay(2.5f, SymbolText1.gameObject);
        }
        // ⑩ SymbolText2
        if (SymbolText2 != null) 
        {
            ShowUIWithDelay(4.4f, SymbolText2.gameObject);
        }
        // ⑪ TotalScoreText
        if (TotalScoreText != null) 
        {
            ShowUIWithDelay(4.8f, TotalScoreText.gameObject);
        }
    }

    public void SetEachDominoCountTexts()
    {
        var scoreManager = ScoreManager.Instance;
        if (scoreManager == null) return;

        // 1. IDの昇順でデータを抽出
        var sortedData = scoreManager.fallenDominoCounts
            .OrderBy(x => x.Key) // ここで_dominoTypeID順になる
            .ToList();

        _fallenDominoTypeCount = sortedData.Count;

        // 2. テキストの初期化（全要素を非表示に）
        foreach (var text in DominoKindsTexts)
        {
            if (text != null) { text.text = ""; text.gameObject.SetActive(false); }
        }
        foreach (var text in DominopiecesTexts)
        {
            if (text != null) { text.text = ""; text.gameObject.SetActive(false); }
        }
        foreach (var text in EachDominoMultiplierTexts)
        {
            if (text != null) text.gameObject.SetActive(false);
        }

        _dominoPieceTargetCounts.Clear();

        // 3. 倒れた種類分だけUIに流し込む
        for (int i = 0; i < sortedData.Count; i++)
        {
            if (i < DominoKindsTexts.Count && i < DominopiecesTexts.Count)
            {
                int id = sortedData[i].Key;
                int count = sortedData[i].Value;
                string dName = scoreManager.dominoNames.ContainsKey(id) 
                               ? scoreManager.dominoNames[id] : "Unknown";

                DominoKindsTexts[i].text = $"{dName}";
                DominopiecesTexts[i].text = "x 0";
                _dominoPieceTargetCounts.Add(count);
            }
        }

        // EachDominoMultiplierTexts: 倍率管理から取得し "ChainBonus ×1.15" 形式で表示
        var multipliers = scoreManager.GetActiveMultipliersForDisplay();
        _activeMultiplierCount = multipliers.Count;
        for (int i = 0; i < multipliers.Count && i < EachDominoMultiplierTexts.Count; i++)
        {
            var text = EachDominoMultiplierTexts[i];
            if (text != null)
                text.text = $"{multipliers[i].displayName} ×{multipliers[i].value:F2}";
        }

        // TotalChainText: ドミノの基礎ポイントの合計
        if (TotalChainText != null)
            TotalChainText.text = scoreManager.GetTotalBasePoints().ToString();

        // TotalMultiplierText: 倍率の合計
        if (TotalMultiplierText != null)
            TotalMultiplierText.text = $"×{scoreManager.GetTotalMultiplier():F2}";

        // TotalScoreText: 基礎ポイント × 倍率 = スコア
        if (TotalScoreText != null)
        {
            int score = Mathf.RoundToInt(scoreManager.GetTotalBasePoints() * scoreManager.GetTotalMultiplier());
            TotalScoreText.text = score.ToString();
        }
    }
    //表示するUIを時間差で出すためのコルーチン
    void ShowUIWithDelay(float delay, GameObject uiObject)
    {
        //アクティブにしつつ、表示するUIを時間差で出すためのコルーチンを開始
        StartCoroutine(ShowUIAfterDelay(delay, uiObject));
    }

    System.Collections.IEnumerator ShowUIAfterDelay(float delay, GameObject uiObject)
    {
        yield return new WaitForSeconds(delay);
        // ここでUIを表示する処理
        if (uiObject != null) uiObject.SetActive(true);
    }

    void ShowDominoPieceWithCountUpDelay(float delay, TextMeshProUGUI targetText, int targetCount)
    {
        StartCoroutine(ShowDominoPieceCountUpAfterDelay(delay, targetText, targetCount));
    }

    System.Collections.IEnumerator ShowDominoPieceCountUpAfterDelay(float delay, TextMeshProUGUI targetText, int targetCount)
    {
        yield return new WaitForSeconds(delay);
        if (targetText == null) yield break;

        targetText.gameObject.SetActive(true);
        if (targetCount <= 0)
        {
            targetText.text = "x 0";
            yield break;
        }

        const float countUpDuration = 0.7f;
        int startValue = 1;
        float elapsed = 0f;
        targetText.text = $"x {startValue}";

        while (elapsed < countUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / countUpDuration);
            int value = Mathf.Clamp(Mathf.RoundToInt(Mathf.Lerp(startValue, targetCount, t)), startValue, targetCount);
            targetText.text = $"x {value}";
            yield return null;
        }

        targetText.text = $"x {targetCount}";
    }

    private void ResetResultElementVisibility()
    {
        if (BackgroundImage != null) BackgroundImage.gameObject.SetActive(false);
        if (ResultText != null) ResultText.gameObject.SetActive(false);
        if (ChainText != null) ChainText.gameObject.SetActive(false);
        if (TotalChainText != null) TotalChainText.gameObject.SetActive(false);
        if (MultiplierText != null) MultiplierText.gameObject.SetActive(false);
        if (TotalMultiplierText != null) TotalMultiplierText.gameObject.SetActive(false);
        if (SymbolText1 != null) SymbolText1.gameObject.SetActive(false);
        if (SymbolText2 != null) SymbolText2.gameObject.SetActive(false);
        if (TotalScoreText != null) TotalScoreText.gameObject.SetActive(false);

        foreach (var text in DominoKindsTexts)
        {
            if (text != null) text.gameObject.SetActive(false);
        }
        foreach (var text in DominopiecesTexts)
        {
            if (text != null) text.gameObject.SetActive(false);
        }
        foreach (var text in EachDominoMultiplierTexts)
        {
            if (text != null) text.gameObject.SetActive(false);
        }
    }
}
