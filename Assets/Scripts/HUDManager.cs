using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using TMPro; // TextMeshProを使う場合はこれが必要
using StarterAssets;

[System.Serializable]
public class DominoData
{
    public string name;            // ドミノの名前
    public GameObject prefab;      // 設置するプレファブ
    public Sprite icon;            // HUDに表示するアイコン
    public int currentCount;       // 残量
}

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance { get; private set; }

    [Header("ホットバー設定")]
    public RectTransform selector;
    public List<RectTransform> slots; // 既存の枠リスト
    public Image handIcon;

    [Header("ドミノデータ管理")]
    public List<DominoData> dominoInventory; // ここにインスペクターでドミノを登録

    [Header("スコア表示用UI")]
    public TMPro.TextMeshProUGUI scoreText;
    public TMPro.TextMeshProUGUI chainText;


    [Header("タイマー・ノルマ表示")]
    public TMPro.TextMeshProUGUI timerText;
    public TMPro.TextMeshProUGUI targetScoreText;
    private int _currentSelectedIndex = 0;
    private bool _hasSelectedOnce = false;
    private Vector3 _originalHandPos;
    private Vector3 _targetHandPos;

    void Awake() 
    {
        if (handIcon != null) _originalHandPos = handIcon.rectTransform.localPosition;
        if (Instance == null) { Instance = this; } else { Destroy(gameObject); }
    }

    void Start()
    {
        if (selector != null) selector.gameObject.SetActive(false);
        if (handIcon != null) handIcon.gameObject.SetActive(false);
        
        // 起動時にHUDの表示を最新にする
        RefreshAllSlots();
        SetTargetScoreUI(GameManager.Instance.targetScore);
    }

    void Update()
    {
        // マウスホイールによる選択切り替え
        if (Mouse.current != null)
        {
            Vector2 scrollVector = Mouse.current.scroll.ReadValue();
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

    // ★追加：スロットの表示（画像と数値）を更新するメソッド
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
                    iconTr.gameObject.SetActive(true);
                    iconTr.GetComponent<Image>().sprite = dominoInventory[i].icon;
                }
                if (textTr) {
                    textTr.gameObject.SetActive(true);
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
        {
            return dominoInventory[_currentSelectedIndex];
        }
        return null;
    }

    // 残量を減らすメソッド
    public void UseSelectedDomino()
    {
        var data = GetSelectedDominoData();
        if (data != null && data.currentCount > 0)
        {
            data.currentCount--;
            RefreshAllSlots(); // 表示を更新
        }
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
            // 残り10秒で赤くする演出
            if (time <= 10f) timerText.color = Color.red;
        }
    }

    public void SetTargetScoreUI(int target)
    {
        if (targetScoreText != null) targetScoreText.text = $"TARGET: {target:N0}";
    }

    // --- 以下、既存のメソッドを維持 ---
    void ShowSelector() { _hasSelectedOnce = true; if (selector != null) selector.gameObject.SetActive(true); UpdateSelectorPosition(); }
    void ChangeSlot(int direction) { _currentSelectedIndex = (int)Mathf.Repeat(_currentSelectedIndex + direction, slots.Count); UpdateSelectorPosition(); }
    void UpdateSelectorPosition() { if (_hasSelectedOnce && slots.Count > _currentSelectedIndex && selector != null) selector.position = slots[_currentSelectedIndex].position; }
    public void SetHandIconVisible(bool isVisible) { if (handIcon != null) handIcon.gameObject.SetActive(isVisible); }
    public int GetSelectedSlotIndex() => _hasSelectedOnce ? _currentSelectedIndex : -1;
    public void UpdateHandShake(bool isHolding) { /* 既存の揺れ処理 */ }
}