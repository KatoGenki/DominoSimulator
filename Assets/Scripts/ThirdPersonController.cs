using UnityEngine;
using Cinemachine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

namespace StarterAssets
{
    [RequireComponent(typeof(CharacterController))]
#if ENABLE_INPUT_SYSTEM 
    [RequireComponent(typeof(PlayerInput))]
#endif
    public class ThirdPersonController : MonoBehaviour
    {
        public enum PlayerState
        {
            Standing,
            CrawlingIdle, // 四つん這い・静止（設置モード）
            CrawlingMove  // 四つん這い・移動（ハイハイ）
        }
        [Header("State")]
        public PlayerState CurrentState = PlayerState.Standing;

        [Header("Player")]
        public float MoveSpeed = 2.0f;
        public float SprintSpeed = 5.335f;

        [Header("Crawling")]
        public float CrawlSpeed = 1.2f;

        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        public float JumpHeight = 1.2f;
        public float Gravity = -15.0f;
        public float JumpTimeout = 0.50f;
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        public bool Grounded = true;
        public float GroundedOffset = -0.14f;
        public float GroundedRadius = 0.28f;
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        public GameObject CinemachineCameraTarget;
        public float TopClamp = 70.0f;
        public float BottomClamp = -30.0f;
        public float CameraAngleOverride = 0.0f;
        public bool LockCameraPosition = false;

        [Header("Cameras")]
        public CinemachineVirtualCamera TPSCamera; 
        public CinemachineVirtualCamera FPSCamera;

        [Header("Domino System")]
        public DominoPlacement dominoPlacementManager;

        [Header("IK Settings")]
        public Transform ikTarget; // 手が追いかける空のオブジェクト
        public float ikWeightSpeed = 5f; // IKのオンオフの切り替え速度
        private float _ikWeight = 0f; // 現在のIKの重み

        [Header("Hand Movement Settings")]
        public float handReachDistance = 2.0f; // 手が届く距離
        public Vector3 handOffset = new Vector3(0.2f, -0.2f, 0.5f); // 画面中央からの手のズレ調整

        // 内部変数
        private bool _placeDomino = false; 
        private Vector2 _mouseDelta = Vector2.zero;
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        private float _speed;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 159.0f;
        

        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDIsBuilding;
        private int _animIDIdCrawling;

        private DominoTrigger[] handFootTriggers; 

#if ENABLE_INPUT_SYSTEM 
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        private const float _threshold = 0.01f;
        private bool _hasAnimator;

        private bool IsCurrentDeviceMouse
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return _playerInput.currentControlScheme == "KeyboardMouse";
#else
                return false;
#endif
            }
        }

        private void Awake()
        {
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;
            _hasAnimator = TryGetComponent(out _animator);
            _controller = GetComponent<CharacterController>();
            _input = GetComponent<StarterAssetsInputs>();
#if ENABLE_INPUT_SYSTEM 
            _playerInput = GetComponent<PlayerInput>();
#endif
            AssignAnimationIDs();
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            handFootTriggers = GetComponentsInChildren<DominoTrigger>(true);

            // 最初から全てのトリガーを起動しておく
            foreach (var trigger in handFootTriggers) trigger.IsActive = true;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            HandleInputState();
            JumpAndGravity();
            GroundedCheck();
            Move();
            UpdateHandIKTarget();

            // マウスの移動量を更新
            if (_input != null) _mouseDelta = _input.look;

            // 建築モード（Crawling）の時のみドミノ操作を実行
            bool isBuildingMode = (CurrentState != PlayerState.Standing);
            // ドミノ操作中（IsManipulating）はIKの重みを最大にする
            float targetWeight = (isBuildingMode && dominoPlacementManager.IsManipulating) ? 1f : 0f;
            _ikWeight = Mathf.Lerp(_ikWeight, targetWeight, Time.deltaTime * ikWeightSpeed);

            if (isBuildingMode && dominoPlacementManager != null)
            {
                dominoPlacementManager.UpdatePlacementInput(_placeDomino, _mouseDelta);
                
                // ★追加：マウスの動きに合わせてIK Targetを動かす（簡易版）
                if (ikTarget != null) {
                    // カメラの向きに合わせてターゲットを移動させるロジックなどをここに
                }
            }
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
            _animIDIsBuilding = Animator.StringToHash("IsBuilding"); 
            _animIDIdCrawling = Animator.StringToHash("IsCrawling"); 
        }

        private void GroundedCheck()
        {
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers, QueryTriggerInteraction.Ignore);

            if (_hasAnimator) _animator.SetBool(_animIDGrounded, Grounded);
        }

        private void CameraRotation()
        {
            // 3人称視点（Standing）の時だけ、マウスでカメラ角度を更新
            if (CurrentState == PlayerState.Standing)
            {
                if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
                {
                    float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;
                    _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                    _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
                }
            }

            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(
                _cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 
                0.0f
            );
        }

        public void OnPlaceDomino(InputValue value)
        {
            _placeDomino = value.isPressed;
        }

        private void Move()
        {
            bool hasMoveInput = _input.move != Vector2.zero;
            bool isBuildingMode = (CurrentState != PlayerState.Standing);
            bool isSprinting = _input.sprint && !isBuildingMode;

            // 1. 目標速度の決定
            float targetSpeed = 0.0f;
            if (hasMoveInput)
            {
                targetSpeed = isBuildingMode ? CrawlSpeed : (isSprinting ? SprintSpeed : MoveSpeed);
            }

            // 2. 速度の補間
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;
            float speedOffset = 0.1f;
            if (Mathf.Abs(currentHorizontalSpeed - targetSpeed) > speedOffset)
            {
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed, Time.deltaTime * SpeedChangeRate);
            }
            else
            {
                _speed = targetSpeed;
            }

            // 3. アニメーターへの反映
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDIsBuilding, isBuildingMode);
                _animator.SetBool(_animIDIdCrawling, isBuildingMode && hasMoveInput);
                _animator.SetFloat(_animIDSpeed, _speed);
                _animator.SetFloat(_animIDMotionSpeed, hasMoveInput ? 1f : 0f);
            }

            // 4. 回転と移動方向
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            if (!isBuildingMode)
            {
                // 3人称：移動方向に回転
                if (hasMoveInput)
                {
                    _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
                    float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }
            else
            {
                // 四つん這い：カメラ正面固定
                _targetRotation = _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, RotationSmoothTime);
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }

            // 5. 移動の実行
            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * (!isBuildingMode ? Vector3.forward : inputDirection);
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        }

        private void HandleInputState()
        {
            if (_input.buildMode)
            {
                _input.buildMode = false;
                if (CurrentState == PlayerState.Standing)
                {
                    if (Grounded) CurrentState = PlayerState.CrawlingIdle;
                }
                else
                {
                    CurrentState = PlayerState.Standing;
                }
                UpdateState();
            }
        }

        private void JumpAndGravity()
        {
            // 建築モード中はジャンプ不可
            if (CurrentState != PlayerState.Standing)
            {
                _input.jump = false;
                _jumpTimeoutDelta = JumpTimeout;
                if (Grounded && _verticalVelocity < 0.0f) _verticalVelocity = -2f;
            }

            if (Grounded)
            {
                _fallTimeoutDelta = FallTimeout;
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                if (_verticalVelocity < 0.0f) _verticalVelocity = -2f;

                // ジャンプ実行（Standingのみ）
                if (CurrentState == PlayerState.Standing && _input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                    if (_hasAnimator) _animator.SetBool(_animIDJump, true);
                }

                if (_jumpTimeoutDelta >= 0.0f) _jumpTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _jumpTimeoutDelta = JumpTimeout;
                if (_fallTimeoutDelta >= 0.0f) _fallTimeoutDelta -= Time.deltaTime;
                else if (_hasAnimator) _animator.SetBool(_animIDFreeFall, true);
                _input.jump = false;
            }

            if (_verticalVelocity < _terminalVelocity) _verticalVelocity += Gravity * Time.deltaTime;
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (!Grounded) return;
            if (hit.normal.y < 0.1f) 
            {
                if (_verticalVelocity > 0f)
                {
                    _verticalVelocity = -2f; 
                    _input.jump = false;
                }
            }
        }

        private void UpdateState()
        {
            bool isAnyCrawl = (CurrentState != PlayerState.Standing);
            
            // カメラの切り替え
            FPSCamera.Priority = isAnyCrawl ? 20 : 10;
            TPSCamera.Priority = isAnyCrawl ? 10 : 20;

            // 設置モードの有効化（これはハイハイ時のみで良いはずです）
            if (dominoPlacementManager != null) 
                dominoPlacementManager.SetPlacementModeActive(isAnyCrawl);

            // ★修正箇所：状態に関わらず常にトリガーを有効にする
            if (handFootTriggers != null)
            {
                foreach (var trigger in handFootTriggers) 
                {
                    trigger.IsActive = true; // 常時 true に設定
                }
            }
        }

        // Animator Events
        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f && FootstepAudioClips.Length > 0)
            {
                var index = Random.Range(0, FootstepAudioClips.Length);
                AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }
        
        public void ForceUpdateState()
        {
            UpdateState();
        }

        private void OnAnimatorIK(int layerIndex)
        {
            if (!_hasAnimator || ikTarget == null) return;

            // 右手の位置と回転をIKターゲットに合わせる
            _animator.SetIKPositionWeight(AvatarIKGoal.RightHand, _ikWeight);
            _animator.SetIKRotationWeight(AvatarIKGoal.RightHand, _ikWeight);
            
            _animator.SetIKPosition(AvatarIKGoal.RightHand, ikTarget.position);
            _animator.SetIKRotation(AvatarIKGoal.RightHand, ikTarget.rotation);
        }

        private void UpdateHandIKTarget()
        {
            if (CurrentState == PlayerState.Standing || ikTarget == null) return;

            // 1. カメラの中央からレイ（光線）を飛ばす
            Ray ray = new Ray(_mainCamera.transform.position, _mainCamera.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, handReachDistance, GroundLayers))
            {
                // 地面に当たった場合：その場所に手を近づける
                ikTarget.position = Vector3.Lerp(ikTarget.position, hit.point + Vector3.up * 0.1f, Time.deltaTime * 10f);
            }
            else
            {
                // 何も当たらない場合：カメラの前方の空中に配置
                Vector3 defaultPos = _mainCamera.transform.TransformPoint(handOffset + Vector3.forward * 1.5f);
                ikTarget.position = Vector3.Lerp(ikTarget.position, defaultPos, Time.deltaTime * 5f);
            }

            // 手の向きをカメラの方向に合わせる（必要に応じて）
            ikTarget.rotation = Quaternion.LookRotation(_mainCamera.transform.forward);
        }

    }

    
}