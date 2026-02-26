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
            ShowUIWithDelay(0.35f, ChainText.gameObject);
        }
        foreach (var text in DominoKindsTexts)
        {
            // ③ DominoKindsTexts
            ShowUIWithDelay(0.7f, text.gameObject); 
        }
        for (int i = 0; i < DominopiecesTexts.Count; i++)
        {
            // ④ DominopiecesTexts (1 から高速カウントアップ)
            int target = i < _dominoPieceTargetCounts.Count ? _dominoPieceTargetCounts[i] : 0;
            ShowDominoPieceWithCountUpDelay(1.05f, DominopiecesTexts[i], target);
        }
        if (TotalChainText != null) 
        {
            // ⑤ TotalChainText
            ShowUIWithDelay(1.45f, TotalChainText.gameObject); 
        }
    }

    public void ActiveMultiplierUI()
    {
        // ⑦ MultiplierText
        if (MultiplierText != null) 
        {
            ShowUIWithDelay(2.05f, MultiplierText.gameObject);
        }
        // ⑧ EachDominoMultiplierTexts
        foreach (var text in EachDominoMultiplierTexts)
        {
            ShowUIWithDelay(2.35f, text.gameObject); 
        }
        // ⑨ TotalMultiplierText
        if (TotalMultiplierText != null) 
        {
            ShowUIWithDelay(2.75f, TotalMultiplierText.gameObject); 
        }
    }

    public void ActiveTotalScoreUI()
    {
        // ⑥ SymbolText1
        if (SymbolText1 != null) 
        {
            ShowUIWithDelay(1.75f, SymbolText1.gameObject);
        }
        // ⑩ SymbolText2
        if (SymbolText2 != null) 
        {
            ShowUIWithDelay(3.05f, SymbolText2.gameObject);
        }
        // ⑪ TotalScoreText
        if (TotalScoreText != null) 
        {
            ShowUIWithDelay(3.35f, TotalScoreText.gameObject);
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

        // 2. テキストの初期化
        foreach (var text in DominoKindsTexts)
        {
            text.text = "";
            text.gameObject.SetActive(false);
        }
        foreach (var text in DominopiecesTexts)
        {
            text.text = "";
            text.gameObject.SetActive(false);
        }
        _dominoPieceTargetCounts.Clear();
        for (int i = 0; i < DominopiecesTexts.Count; i++)
        {
            _dominoPieceTargetCounts.Add(0);
        }

        // 3. UIに流し込む
        for (int i = 0; i < sortedData.Count; i++)
        {
            if (i < DominoKindsTexts.Count && i < DominopiecesTexts.Count)
            {
                int id = sortedData[i].Key;
                int count = sortedData[i].Value;
                string dName = scoreManager.dominoNames.ContainsKey(id) 
                               ? scoreManager.dominoNames[id] : "Unknown";

                // 表示例: "100: NormalDomino x 15" 
                // IDを表示したくない場合は {dName} x {count} だけにする
                DominoKindsTexts[i].text = $"{dName}";
                DominopiecesTexts[i].text = "x 0";
                _dominoPieceTargetCounts[i] = count;
            }
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

        const float countUpDuration = 0.45f;
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
