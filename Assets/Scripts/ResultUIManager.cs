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
        this.gameObject.SetActive(true);
        // ResultTextを表示
        if (ResultText != null) 
        {
            ShowUIWithDelay(0.5f, BackgroundImage.gameObject); // 0.5秒の遅延で表示
            ShowUIWithDelay(0.5f, ResultText.gameObject); // 0.5秒の遅延で表示
        }
    }
    public void ActivechainUI()
    {
        if (ChainText != null) 
        {
            ShowUIWithDelay(0.5f, ChainText.gameObject); // 0.5秒の遅延で表示
        }
        foreach (var text in DominoKindsTexts)
        {
            ShowUIWithDelay(1.0f, text.gameObject); // 0.5秒の遅延で表示
        }
        foreach (var text in DominopiecesTexts)
        {
            ShowUIWithDelay(1.5f, text.gameObject); // 0.5秒の遅延で表示
        }
        if (TotalChainText != null) 
        {
            ShowUIWithDelay(1.5f, TotalChainText.gameObject); // 0.5秒の遅延で表示
        }
    }

    public void ActiveMultiplierUI()
    {
        if (MultiplierText != null) 
        {
            ShowUIWithDelay(0.5f, MultiplierText.gameObject); // 0.5秒の遅延で表示
        }
        foreach (var text in EachDominoMultiplierTexts)
        {
            ShowUIWithDelay(1.0f, text.gameObject); // 0.5秒の遅延で表示
        }
        if (TotalMultiplierText != null) 
        {
            ShowUIWithDelay(1.5f, TotalMultiplierText.gameObject); // 0.5秒の遅延で表示
        }
    }

    public void ActiveTotalScoreUI()
    {
        if (SymbolText1 != null) 
        {
            ShowUIWithDelay(0.5f, SymbolText1.gameObject); // 0.5秒の遅延で表示
        }
        if (SymbolText2 != null) 
        {
            ShowUIWithDelay(1.0f, SymbolText2.gameObject); // 0.5秒の遅延で表示
        }
        if (TotalScoreText != null) 
        {
            ShowUIWithDelay(1.5f, TotalScoreText.gameObject); // 0.5秒の遅延で表示
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

        // 3. UIに流し込む
        for (int i = 0; i < sortedData.Count; i++)
        {
            if (i < DominoKindsTexts.Count)
            {
                int id = sortedData[i].Key;
                int count = sortedData[i].Value;
                string dName = scoreManager.dominoNames.ContainsKey(id) 
                               ? scoreManager.dominoNames[id] : "Unknown";

                // 表示例: "100: NormalDomino x 15" 
                // IDを表示したくない場合は {dName} x {count} だけにする
                DominoKindsTexts[i].text = $"{dName}";
                DominopiecesTexts[i].text = $"x {count}";
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
}
