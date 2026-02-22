using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro; // TextMeshProを使う場合はこれが必要
using StarterAssets;
using Unity.VisualScripting;

[System.Serializable]
public class DominoData
{
    public GameObject prefab;      // 設置するプレファブ
    public Sprite icon;            // HUDに表示するアイコン
    public int currentCount;       // 残量
}

public class HUDManager : MonoBehaviour
{   // シングルトンインスタンス
    public static HUDManager Instance { get; private set; }

    [Header("ホットバー設定")]
    public Image hotbarBackground;
    public RectTransform selector;
    public List<RectTransform> slots; // 既存の枠リスト

    [Header("ドミノデータ管理")]
    public List<DominoData> dominoInventory; // ここにインスペクターでドミノを登録

    [Header("スコア表示用UI")]
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI chainText;


    [Header("タイマー・ノルマ表示")]
    public TMPro.TextMeshProUGUI timerText;
    public TMPro.TextMeshProUGUI targetScoreText;
    //選択中のスロット
    private int _currentSelectedIndex = 0;
    //一度でも選択したかどうかのフラグ
    private bool _hasSelectedOnce = false;

    void Awake() 
    {   
        //インスタンスをセット
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    void Start()
    {   // 起動時にセレクターを非表示にする
        if (selector != null) selector.gameObject.SetActive(false);
        
        // 起動時にHUDの表示を最新にする
        RefreshAllSlots();
        SetTargetScoreUI(GameManager.Instance.targetScore);
    }

    void Update()
    {
        // マウスホイールによる選択切り替え
        if (Mouse.current != null)
        {   //スクロールの位置を取得
            Vector2 scrollVector = Mouse.current.scroll.ReadValue();
            //スクロールがあったら
            if (scrollVector.y != 0f)
            {
                if (!_hasSelectedOnce)
                {
                    _currentSelectedIndex = 0;
                    ShowSelector();
                }
                else
                {
                    if (scrollVector.y > 0f) ChangeSlot(-1);
                    else if (scrollVector.y < 0f) ChangeSlot(1);
                }
            }
        }

        // 数字キー選択
        if (Keyboard.current != null)
        {
            for (int i = 0; i < slots.Count; i++)
            {   
                if (Keyboard.current[Key.Digit1 + i].wasPressedThisFrame)
                {
                    if (!_hasSelectedOnce) ShowSelector();
                    _currentSelectedIndex = i;
                    UpdateSelectorPosition();
                }
            }
        }
    }

    // スロットの表示（画像と数値）を更新するメソッド
    public void RefreshAllSlots()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            // スロットの中に "Icon" と "CountText" という名前のオブジェクトがある前提
            Transform iconTr = slots[i].Find("Icon");
            Transform textTr = slots[i].Find("CountText");

            if (i < dominoInventory.Count)
            {
                // データがある場合
                if (iconTr) {
                    //アイコンを表示
                    iconTr.gameObject.SetActive(true);
                    //アイコンを設定
                    iconTr.GetComponent<Image>().sprite = dominoInventory[i].icon;
                }
                if (textTr) {
                    //テキストを表示
                    textTr.gameObject.SetActive(true);
                    //該当ドミノの残量を表示
                    textTr.GetComponent<TextMeshProUGUI>().text = dominoInventory[i].currentCount.ToString();
                }
            }
            else
            {
                // データがない空のスロットは非表示にする
                if (iconTr) iconTr.gameObject.SetActive(false);
                if (textTr) textTr.gameObject.SetActive(false);
            }
        }
    }

    // 選択中のドミノデータを取得するメソッド（DominoPlacementから使う）
    public DominoData GetSelectedDominoData()
    {  
        if (_hasSelectedOnce && _currentSelectedIndex < dominoInventory.Count)
        {   //選択されているドミノデータを返す
            return dominoInventory[_currentSelectedIndex];
        }
        return null;
    }

    // 残量を減らすメソッド
    public void UseSelectedDomino()
    {
        var data = GetSelectedDominoData();
        //ドミノデータがあり、かつ残量がある場合
        if (data != null && data.currentCount > 0)
        {   //残量を1減らす
            data.currentCount--;
            RefreshAllSlots(); // 表示を更新
        }
    }
    public void UpdateInventoryUI()
    {
        // dominoInventory[i].currentCount の値を、各スロットにあるText等に反映させる処理
        // 例：各スロットに個数表示用のTextMeshProUGUIがついている場合
        
        for (int i = 0; i < dominoInventory.Count; i++)
        {
            // slots[i] の子要素にあるTextコンポーネントを更新するようなイメージ
            var countText = slots[i].GetComponentInChildren<TextMeshProUGUI>();
            if (countText != null)
            {
                countText.text = dominoInventory[i].currentCount.ToString();
            }
        }
        
        
        // デバッグログで確認
        int index = _currentSelectedIndex;
        Debug.Log($"{dominoInventory[index].prefab.name} 残り: {dominoInventory[index].currentCount}");
    }

    public void UpdateScoreDisplay(int totalScore, int chain)
    {
        if (scoreText != null) 
            scoreText.text = $"SCORE: {totalScore:N0}"; // N0は桁区切りカンマを入れる指定
            
        if (chainText != null) 
            chainText.text = $"{chain} CHAIN";
    }

    public void UpdateTimerDisplay(float time)
    {
        if (timerText != null)
        {
            // 00:00 の形式で表示
            int minutes = Mathf.FloorToInt(time / 60F);
            int seconds = Mathf.FloorToInt(time % 60F);
            timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            timerText.alignment = TextAlignmentOptions.Right;
        }
    }
    public int GetCurrentSelectedIndex()
    {
        return _currentSelectedIndex;
    }

    public void SetTargetScoreUI(int target)
    {
        if (targetScoreText != null) targetScoreText.text = $"TARGET: {target:N0}";
    }

    public void OnChainStartedUI()
    {
        // 1. 選択枠（セレクター）を非表示にする
        if (selector != null) selector.gameObject.SetActive(false);

        // 2. ホットバーを非表示にする
        if (slots != null)
        {
            foreach (var slot in slots)
            {
                if (slot != null) slot.gameObject.SetActive(false);
            }
        }
        
        if(hotbarBackground != null) hotbarBackground.gameObject.SetActive(false);

        // 3. チェーンテキストに開始の合図を出す（演出用）
        if (chainText != null)
        {
            chainText.text = "START CHAIN!";
            chainText.color = Color.yellow; // 目立つ色に変更
        }

        Debug.Log("HUDManager: 連鎖開始UIへ切り替えました。");
    }

    // --- 以下、既存のメソッドを維持 ---
    void ShowSelector() { _hasSelectedOnce = true; if (selector != null) selector.gameObject.SetActive(true); UpdateSelectorPosition(); }
    void ChangeSlot(int direction) { _currentSelectedIndex = (int)Mathf.Repeat(_currentSelectedIndex + direction, slots.Count); UpdateSelectorPosition(); }
    void UpdateSelectorPosition() { if (_hasSelectedOnce && slots.Count > _currentSelectedIndex && selector != null) selector.position = slots[_currentSelectedIndex].position; }
    public int GetSelectedSlotIndex() => _hasSelectedOnce ? _currentSelectedIndex : -1;
    public void UpdateHandShake(bool isHolding) { /* 既存の揺れ処理 */ }
}