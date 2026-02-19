using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro;
using StarterAssets;
using Unity.VisualScripting;

public class ResultUIManager : MonoBehaviour
{
    // シングルトンインスタンス
    public static ResultUIManager Instance { get; private set; }

    [Header("リザルトUI")]
    public Image BackgroundImage;
    public TextMeshProUGUI ResultText;
    public TextMeshProUGUI ChainText;
    public List<TextMeshProUGUI> EachDominoCountTexts; // ドミノごとの個数表示用テキストリスト
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

    // Update is called once per frame
    void Update()
    {
        if (GameManager.Instance.currentState == GameManager.GameState.Result)
        {
        
        }
    }
    public void ActivechainUI()
    {
        if (ChainText != null) 
        {
            ShowUIWithDelay(0.5f, ChainText.gameObject); // 0.5秒の遅延で表示
        }
        foreach (var text in EachDominoCountTexts)
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
