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
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            HandleInputState();
            JumpAndGravity();
            GroundedCheck();
            Move();

            // マウスの移動量を更新
            if (_input != null) _mouseDelta = _input.look;

            // 建築モード（Crawling）の時のみドミノ操作を実行
            bool isBuildingMode = (CurrentState != PlayerState.Standing);
            if (isBuildingMode && dominoPlacementManager != null)
            {
                // 左クリックホールド(_placeDomino)中に移動と回転の両方を渡す
                dominoPlacementManager.UpdatePlacementInput(_placeDomino, _mouseDelta);
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
            FPSCamera.Priority = isAnyCrawl ? 20 : 10;
            TPSCamera.Priority = isAnyCrawl ? 10 : 20;

            if (dominoPlacementManager != null) dominoPlacementManager.SetPlacementModeActive(isAnyCrawl);
            if (handFootTriggers != null)
            {
                foreach (var trigger in handFootTriggers) trigger.IsActive = isAnyCrawl;
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
    }
}