using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// ドミノ追従中の FreeLook カメラ用入力プロバイダ。
/// - マウス視点操作の感度を上げる
/// - X軸入力を反転させる
/// - （任意で）ドミノ追従中のみ適用
/// </summary>
public class DominoFreeLookInputProvider : CinemachineInputProvider
{
    [Header("Sensitivity")]
    [SerializeField] private float _xSensitivity = 1.5f;
    [SerializeField] private float _ySensitivity = 1.2f;

    [Header("Invert Settings")]
    [SerializeField] private bool _invertX = true;

    [Header("適用条件")]
    [Tooltip("ドミノ追従中（Ready かつスコア>0）のときだけ反映するか")]
    [SerializeField] private bool _onlyWhenFollowingDomino = true;
    private GameManager _gameManager;
    private ScoreManager _scoreManager;

    private void Awake()
    {
        if (GameManager.Instance == null)
        {
            _gameManager = Object.FindFirstObjectByType<GameManager>();
        }
        if (ScoreManager.Instance == null)
        {
            _scoreManager = Object.FindFirstObjectByType<ScoreManager>();
        }
    }

    public override float GetAxisValue(int axis)
    {
        // CinemachineInputProvider の標準実装から値を取得
        float value = base.GetAxisValue(axis);

        if (!enabled)
            return value;

        // ドミノ追従中のみ有効にするオプション
        if (_onlyWhenFollowingDomino)
        {
            var gm = GameManager.Instance;
            var sm = ScoreManager.Instance;
            if (gm == null || sm == null)
                return value;

            // Ready かつ 一度でもスコアが加算された（連鎖中）ときのみ
            if (!(gm.currentState == GameManager.GameState.Ready && sm.totalScore > 0))
                return value;
        }

        switch (axis)
        {
            case 0: // X軸（左右回転）
                if (_invertX)
                    value = -value;
                value *= _xSensitivity;
                break;

            case 1: // Y軸（上下回転）
                value *= _ySensitivity;
                break;

            // Z軸はそのまま
        }

        return value;
    }
}

