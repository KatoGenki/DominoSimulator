using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using StarterAssets;
using System.Linq; // 並び替えに使用

public class ResultUIManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static ResultUIManager Instance { get; private set; }

    [Header("リザルトUI")]
    public Image BackgroundImage;
    public TextMeshProUGUI ResultText;
    public TextMeshProUGUI ChainText;
    public List<TextMeshProUGUI> EachDominoCountTexts; // ドミノごとの個数表示用テキストリスト
    public List<TextMeshProUGUI> DominopiecesTexts; //　倒したドミノの個数テキスト
    public TextMeshProUGUI TotalChainText;
    public TextMeshProUGUI MultiplierText;
    public List<TextMeshProUGUI> DominoMultiplierTexts; // ドミノごとのスコア表示用テキストリスト
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
        //gameObject.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.currentState == GameManager.GameState.Result)
        {
        
        }
    }

    //public void SetEachDominoCountTexts(List<int> dominoCounts)

    // 【追加】ドミノの集計結果をUIにセットする
    public void SetEachDominoCountTexts()
    {
        if (ScoreManager.Instance == null) return;

        // 1. IDの昇順でデータを抽出
        var sortedData = ScoreManager.Instance.fallenDominoCounts
            .OrderBy(x => x.Key) // ここで_dominoTypeID順になる
            .ToList();

        // 2. テキストの初期化
        foreach (var text in EachDominoCountTexts)
        {
            text.text = "";
            text.gameObject.SetActive(false);
        }

        // 3. UIに流し込む
        for (int i = 0; i < sortedData.Count; i++)
        {
            if (i < EachDominoCountTexts.Count)
            {
                int id = sortedData[i].Key;
                int count = sortedData[i].Value;
                string dName = ScoreManager.Instance.dominoNames.ContainsKey(id) 
                               ? ScoreManager.Instance.dominoNames[id] : "Unknown";

                // 表示例: "100: NormalDomino x 15" 
                // IDを表示したくない場合は {dName} x {count} だけにする
                EachDominoCountTexts[i].text = $"{dName} ";
                DominopiecesTexts[i].text = $"x {count}";
            }
        }
    }
    public void ShowResultUI()
    {
        //Debug.Log("ShowResultUI呼ばれた！");
        if (BackgroundImage != null) BackgroundImage.gameObject.SetActive(true);
        if (ResultText != null) ResultText.gameObject.SetActive(true);
    }

    public void ActivechainUI()
    {
        //Debug.Log("ActivechainUI呼ばれた！");
        SetEachDominoCountTexts(); // ドミノの集計結果をテキストにセット
        if (ChainText != null) 
        {
            Debug.Log("ChainText表示するよ！");
            ShowUIWithDelay(0.5f, ChainText.gameObject); // 0.5秒の遅延で表示
        }
        foreach (var text in EachDominoCountTexts)
        {
            ShowUIWithDelay(1.0f, text.gameObject); // 0.5秒の遅延で表示
        }

        foreach (var text in DominopiecesTexts)
        {
            ShowUIWithDelay(1.0f, text.gameObject); // 0.5秒の遅延で表示
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
        foreach (var text in DominoMultiplierTexts)
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
