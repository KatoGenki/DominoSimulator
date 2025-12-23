using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HUDManager : MonoBehaviour
{
    // ★追加：どこからでも HUDManager.Instance でアクセスできるようにする
    public static HUDManager Instance { get; private set; }

    [Header("ホットバー設定")]
    public RectTransform selector;
    public List<RectTransform> slots;
    public Image handIcon;

    private int _currentSelectedIndex = 0;
    private bool _hasSelectedOnce = false;

    private Vector3 _originalHandPos;

    private Vector3 _targetHandPos;

    void Awake() 
    {
        if (handIcon != null) _originalHandPos = handIcon.rectTransform.localPosition;
        // シングルトンの初期化
        if (Instance == null) {
            Instance = this;
        } else {
            Destroy(gameObject); // 二重に存在しないようにする
        }
    }

    void Start()
    {
        if (selector != null) selector.gameObject.SetActive(false);
        if (handIcon != null) handIcon.gameObject.SetActive(false);
    }

    void Update()
    {
        // 1. マウスホイールによる選択切り替え
        if (Mouse.current != null)
        {
            Vector2 scrollVector = Mouse.current.scroll.ReadValue();
            if (scrollVector.y != 0f)
            {
                if (!_hasSelectedOnce)
                {
                    // ★初回操作時は必ず左端(0)からスタート
                    _currentSelectedIndex = 0;
                    ShowSelector();
                }
                else
                {
                    // 2回目以降は通常通り切り替え
                    if (scrollVector.y > 0f) ChangeSlot(-1);
                    else if (scrollVector.y < 0f) ChangeSlot(1);
                }
            }
        }

        
        // 2. 数字キー（1〜9）による直接選択
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

    void ShowSelector()
    {
        _hasSelectedOnce = true;
        if (selector != null) selector.gameObject.SetActive(true);
        UpdateSelectorPosition();
    }

    void ChangeSlot(int direction)
    {
        _currentSelectedIndex += direction;
        if (_currentSelectedIndex < 0) _currentSelectedIndex = slots.Count - 1;
        if (_currentSelectedIndex >= slots.Count) _currentSelectedIndex = 0;
        
        UpdateSelectorPosition();
    }

    void UpdateSelectorPosition()
    {
        if (_hasSelectedOnce && slots.Count > _currentSelectedIndex && selector != null)
        {
            selector.position = slots[_currentSelectedIndex].position;
        }
    }

    public void SetHandIconVisible(bool isVisible)
    {
        if (handIcon != null) handIcon.gameObject.SetActive(isVisible);
    }

    public int GetSelectedSlotIndex() => _hasSelectedOnce ? _currentSelectedIndex : -1;

    // DominoPlacementから毎フレーム呼ばれる
    public void UpdateHandShake(bool isHolding) 
    {
        if (handIcon == null || !handIcon.gameObject.activeSelf) return;

        if (isHolding) 
        {
            // ★演出：ホールド中はアイコンを少し中央（左上方向）へ寄せる
            Vector3 offsetToCenter = new Vector3(-150f, 100f, 0f); 
            _targetHandPos = _originalHandPos + offsetToCenter;

            float uiShake = 10.0f; 
            handIcon.rectTransform.localPosition = _targetHandPos + new Vector3(
                Random.Range(-uiShake, uiShake),
                Random.Range(-uiShake, uiShake),
                0
            );
        } 
        else 
        {
            // 離したら元の位置（右端）へ戻す
            handIcon.rectTransform.localPosition = _originalHandPos;
        }
    }
}