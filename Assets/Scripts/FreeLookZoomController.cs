using UnityEngine;
using Cinemachine;
using UnityEngine.InputSystem;
using StarterAssets;

/// <summary>
/// CinemachineFreeLook 用のズーム制御
/// - マウスホイールで「各リグの半径」を倍率スケールで拡大・縮小
/// - 元のリグのプロポーションを維持したままズーム
/// </summary>
public class FreeLookZoomController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineFreeLook _freeLook;

    [Header("Zoom Settings")]
    [SerializeField] private float _zoomSpeed = 1.0f;
    [Tooltip("リグ半径スケールの最小倍率")]
    [SerializeField] private float _minZoomScale = 0.5f;
    [Tooltip("リグ半径スケールの最大倍率")]
    [SerializeField] private float _maxZoomScale = 2.0f;
    [SerializeField] private float _minRadius = 3f;
    [SerializeField] private float _maxRadius = 30f;
    [Tooltip("Height の絶対値の最小値（Top/Mid は正、Bottom は負で適用）")]
    [SerializeField] private float _minHeightAbs = 0.5f;
    [Tooltip("Height の絶対値の最大値（Top/Mid は正、Bottom は負で適用）")]
    [SerializeField] private float _maxHeightAbs = 10f;

    // 元のリグ半径と高さを保持しておき、そこに倍率を掛ける
    private float[] _baseRadiuses;
    private float[] _baseHeights;
    private float _currentZoomScale = 1.0f;

    private void Reset()
    {
        if (_freeLook == null)
        {
            _freeLook = GetComponent<CinemachineFreeLook>();
        }
    }

    private void Awake()
    {
        if (_freeLook == null)
        {
            _freeLook = GetComponent<CinemachineFreeLook>();
        }

        // FreeLook が有効なら、起動時のリグを指定値で初期化し、ベース値としてコピーしておく
        if (_freeLook != null)
        {
            var orbits = _freeLook.m_Orbits;

            // TopRig, MiddleRig, BottomRig の初期値を指定どおりに設定
            if (orbits.Length >= 3)
            {
                // TopRig
                orbits[0].m_Height = 5f;
                orbits[0].m_Radius = 2f;
                // MiddleRig
                orbits[1].m_Height = 2.5f;
                orbits[1].m_Radius = 5f;
                // BottomRig
                orbits[2].m_Height = -3f;
                orbits[2].m_Radius = 2f;
            }

            _freeLook.m_Orbits = orbits;

            int len = _freeLook.m_Orbits.Length;
            _baseRadiuses = new float[len];
            _baseHeights = new float[len];
            for (int i = 0; i < len; i++)
            {
                _baseRadiuses[i] = _freeLook.m_Orbits[i].m_Radius;
                _baseHeights[i] = _freeLook.m_Orbits[i].m_Height;
            }
        }
    }

    private void Update()
    {
        if (_freeLook == null) return;

        // Ready かつ ドミノが倒れている（スコアが一度でも加算されている）ときだけズームを有効にする
        var gameManager = GameManager.Instance;
        var scoreManager = ScoreManager.Instance;
        bool canZoom = gameManager != null
                       && scoreManager != null
                       && gameManager.currentState == GameManager.GameState.Ready
                       && scoreManager.totalScore > 0;

        if (!canZoom) return;

        float scrollDelta = 0f;

        // 新InputSystemのマウスが使える場合はそちらを優先
        var mouse = Mouse.current;
        if (mouse != null)
        {
            scrollDelta = mouse.scroll.ReadValue().y;
        }
        else
        {
            // フォールバックとして旧InputSystemの軸を使用
            scrollDelta = UnityEngine.Input.GetAxis("Mouse ScrollWheel") * 120f;
        }

        if (Mathf.Approximately(scrollDelta, 0f)) return;

        // 保険として、ベース配列が未初期化またはサイズ不一致なら現在値から再構築
        if (_baseRadiuses == null || _baseRadiuses.Length != _freeLook.m_Orbits.Length)
        {
            int len = _freeLook.m_Orbits.Length;
            _baseRadiuses = new float[len];
            for (int i = 0; i < len; i++)
            {
                _baseRadiuses[i] = _freeLook.m_Orbits[i].m_Radius;
            }
        }
        if (_baseHeights == null || _baseHeights.Length != _freeLook.m_Orbits.Length)
        {
            int len = _freeLook.m_Orbits.Length;
            _baseHeights = new float[len];
            for (int i = 0; i < len; i++)
            {
                _baseHeights[i] = _freeLook.m_Orbits[i].m_Height;
            }
        }

        // 上方向スクロールを「ズームイン」にしたいのでスケールを減少方向へ
        float scaleDelta = -scrollDelta * _zoomSpeed * Time.deltaTime;
        _currentZoomScale = Mathf.Clamp(_currentZoomScale + scaleDelta, _minZoomScale, _maxZoomScale);

        // 各リグに「元の半径・高さ × 現在スケール」を適用
        for (int i = 0; i < _freeLook.m_Orbits.Length; i++)
        {
            float baseRadius = _baseRadiuses[i];
            float baseHeight = _baseHeights[i];
            var orbit = _freeLook.m_Orbits[i];

            float scaledRadius = baseRadius * _currentZoomScale;
            float scaledHeight = baseHeight * _currentZoomScale;

            orbit.m_Radius = Mathf.Clamp(scaledRadius, _minRadius, _maxRadius);

            // Height は「符号はベースそのまま、絶対値のみスケール＆クランプ」することで
            // BottomRig(負の高さ) もズームアウト時に「さらに下」に移動するようにする
            float baseSign = Mathf.Sign(baseHeight);
            float heightAbs = Mathf.Abs(scaledHeight);
            heightAbs = Mathf.Clamp(heightAbs, _minHeightAbs, _maxHeightAbs);
            orbit.m_Height = baseSign * heightAbs;

            _freeLook.m_Orbits[i] = orbit;
        }
    }
}

