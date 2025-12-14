 using UnityEngine;
 using Cinemachine;
#if ENABLE_INPUT_SYSTEM 
using UnityEngine.InputSystem;
#endif

/* Note: animations are called via the controller for both the character and capsule using animator null checks
 */

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
            Standing, // 立ち（通常移動、TPS）
            Building  // 設置モード（四つん這い、FPS、移動不可）
        }
        [Header("State")]
        public PlayerState CurrentState = PlayerState.Standing;

        [Header("Player")]
        [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")]
        [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)]
        [Tooltip("The height the player can jump")]
        public float JumpHeight = 1.2f;

        [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
        public float Gravity = -15.0f;

        [Space(10)]
        [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
        public float JumpTimeout = 0.50f;

        [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
        public float FallTimeout = 0.15f;

        [Header("Player Grounded")]
        [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
        public bool Grounded = true;

        [Tooltip("Useful for rough ground")]
        public float GroundedOffset = -0.14f;

        [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
        public float GroundedRadius = 0.28f;

        [Tooltip("What layers the character uses as ground")]
        public LayerMask GroundLayers;

        [Header("Cinemachine")]
        [Tooltip("The follow target set in the Cinemachine Virtual Camera that the camera will follow")]
        public GameObject CinemachineCameraTarget;

        [Tooltip("How far in degrees can you move the camera up")]
        public float TopClamp = 70.0f;

        [Tooltip("How far in degrees can you move the camera down")]
        public float BottomClamp = -30.0f;

        [Tooltip("Additional degress to override the camera. Useful for fine tuning camera position when locked")]
        public float CameraAngleOverride = 0.0f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;

        [Header("Cameras")]
        [Tooltip("三人称視点 (TPS) 用の Cinemachine Virtual Camera")]
        public CinemachineVirtualCamera TPSCamera; 

        [Tooltip("一人称視点 (FPS) 用の Cinemachine Virtual Camera")]
        public CinemachineVirtualCamera FPSCamera;

        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 159.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;
        private int _animIDIsBuilding;

        private DominoTrigger[] handFootTriggers;  // 手足のDominoTriggerコンポーネントを保持する配列


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
            // get a reference to our main camera
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
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            AssignAnimationIDs();

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;
            handFootTriggers = GetComponentsInChildren<DominoTrigger>(true);
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            HandleInputState(); // 状態変化処理
            JumpAndGravity();
            GroundedCheck();
            Move();
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
            _animIDIsBuilding = Animator.StringToHash("IsBuilding"); // 設置/四つん這い兼用
        }

        private void GroundedCheck()
        {
            // set sphere position, with offset
            Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - GroundedOffset,
                transform.position.z);
            Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, GroundLayers,
                QueryTriggerInteraction.Ignore);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetBool(_animIDGrounded, Grounded);
            }
        }

        private void CameraRotation()
        {
            // if there is an input and camera position is not fixed
            if (_input.look.sqrMagnitude >= _threshold && !LockCameraPosition)
            {
                //Don't multiply mouse input by Time.deltaTime;
                float deltaTimeMultiplier = IsCurrentDeviceMouse ? 1.0f : Time.deltaTime;

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        // ThirdPersonController.cs の Move関数全体を以下に置き換えてください

        // private void Move()
        // {
        //     // --- 1. 設置モード (Building) チェックと移動制限 ---
        //     if (CurrentState == PlayerState.Building)
        //     {
        //         // 移動停止のための処理
        //         _speed = 0f;
        //         _animationBlend = 0f;
        //         if (_hasAnimator)
        //         {
        //             _animator.SetFloat(_animIDSpeed, 0f);
        //             _animator.SetFloat(_animIDMotionSpeed, 0f);
        //         }
        //         // キャラクターコントローラーを動かさない (重力はJumpAndGravityで処理される)
        //         return; 
        //     }
        //     // --- ------------------------------------------ ---
            
        //     // --- 2. Standingモード (TPS) の移動ロジック (既存のロジック) ---
            
        //     // set target speed based on move speed, sprint speed and if sprint is pressed
        //     float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed; // 1回目の定義は削除されている
            
        //     // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

        //     // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //     // if there is no input, set the target speed to 0
        //     if (_input.move == Vector2.zero) targetSpeed = 0.0f;

        //     // a reference to the players current horizontal velocity
        //     float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        //     float speedOffset = 0.1f;
        //     float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

        //     // accelerate or decelerate to target speed
        //     if (currentHorizontalSpeed < targetSpeed - speedOffset ||
        //         currentHorizontalSpeed > targetSpeed + speedOffset)
        //     {
        //         // creates curved result rather than a linear one giving a more organic speed change
        //         // note T in Lerp is clamped, so we don't need to clamp our speed
        //         _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
        //             Time.deltaTime * SpeedChangeRate);

        //         // round speed to 3 decimal places
        //         _speed = Mathf.Round(_speed * 1000f) / 1000f;
        //     }
        //     else
        //     {
        //         _speed = targetSpeed;
        //     }

        //     _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        //     if (_animationBlend < 0.01f) _animationBlend = 0f;
            
        //     // normalise input direction
        //     Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

        //     // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
        //     // if there is a move input rotate player when the player is moving
        //     if (_input.move != Vector2.zero)
        //     {
        //         _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
        //                                 _mainCamera.transform.eulerAngles.y;
        //         float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
        //             RotationSmoothTime);

        //         // rotate to face input direction relative to camera position
        //         transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        //     }


        //     Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

        //     // move the player
        //     _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
        //                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

        //     // update animator if using character
        //     if (_hasAnimator)
        //     {
        //         _animator.SetFloat(_animIDSpeed, _animationBlend);
        //         _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
        //     }
        // }
        private void Move()
        {
            // --- 1. 設置モード (Building) チェックと移動制限 ---
            if (CurrentState == PlayerState.Building)
            {
                // 移動停止のための処理
                _speed = 0f;
                _animationBlend = 0f;
                if (_hasAnimator)
                {
                    _animator.SetFloat(_animIDSpeed, 0f);
                    _animator.SetFloat(_animIDMotionSpeed, 0f);
                }
                // キャラクターコントローラーを動かさない (重力はJumpAndGravityで処理される)
                return; 
            }
            // --- ------------------------------------------ ---
            
            // --- 2. Standingモード (TPS) の移動ロジック (既存のロジック) ---
            
            // set target speed based on move speed, sprint speed and if sprint is pressed
            float targetSpeed = _input.sprint ? SprintSpeed : MoveSpeed; 
            
            // a simplistic acceleration and deceleration designed to be easy to remove, replace, or iterate upon

            // note: Vector2's == operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is no input, set the target speed to 0
            if (_input.move == Vector2.zero) targetSpeed = 0.0f;

            // a reference to the players current horizontal velocity
            float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

            float speedOffset = 0.1f;
            float inputMagnitude = _input.analogMovement ? _input.move.magnitude : 1f;

            // accelerate or decelerate to target speed
            if (currentHorizontalSpeed < targetSpeed - speedOffset ||
                currentHorizontalSpeed > targetSpeed + speedOffset)
            {
                // creates curved result rather than a linear one giving a more organic speed change
                // note T in Lerp is clamped, so we don't need to clamp our speed
                _speed = Mathf.Lerp(currentHorizontalSpeed, targetSpeed * inputMagnitude,
                    Time.deltaTime * SpeedChangeRate);

                // round speed to 3 decimal places
                _speed = Mathf.Round(_speed * 1000f) / 1000f;
            }
            else
            {
                _speed = targetSpeed;
            }

            _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
            if (_animationBlend < 0.01f) _animationBlend = 0f;
            
            // normalise input direction
            Vector3 inputDirection = new Vector3(_input.move.x, 0.0f, _input.move.y).normalized;

            // note: Vector2's != operator uses approximation so is not floating point error prone, and is cheaper than magnitude
            // if there is a move input rotate player when the player is moving
            if (_input.move != Vector2.zero)
            {
                _targetRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg +
                                        _mainCamera.transform.eulerAngles.y;
                float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity,
                    RotationSmoothTime);

                // rotate to face input direction relative to camera position
                transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // ***** ここから修正箇所 *****
            Vector3 horizontalMovement = targetDirection.normalized * (_speed * Time.deltaTime);

            // 壁登りバグ（高く飛ぶ問題）の対策：
            // 地面にいる（Grounded）状態でジャンプ入力がある（_input.jump）場合、
            // 水平方向の移動（horizontalMovement）を強制的にゼロにし、
            // 壁との摩擦・衝突による意図しない垂直速度のブーストを防ぐ
            if (Grounded && _input.jump) 
            {
                horizontalMovement = Vector3.zero;
            }

            // move the player
            _controller.Move(horizontalMovement +
                                    new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
            // ***** ここまで修正箇所 *****

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }
        private void HandleInputState()
        {
            // Eキー (BuildMode): 設置モード ⇔ 移動モード のトグル切り替え
            if (_input.buildMode)
            {
                _input.buildMode = false; // 入力フラグを消費（リセット）
                
                // 状態をトグル切り替え
                if (CurrentState == PlayerState.Building)
                {
                    CurrentState = PlayerState.Standing;
                    UpdateState(); // ★ 状態更新関数を呼び出し ★
                }
                else if (Grounded) 
                {
                    CurrentState = PlayerState.Building;
                    UpdateState(); // ★ 状態更新関数を呼び出し ★
                }
            }
        }
        private void JumpAndGravity()
        {
            if (Grounded)
            {
                // reset the fall timeout timer
                _fallTimeoutDelta = FallTimeout;

                // update animator if using character
                if (_hasAnimator)
                {
                    _animator.SetBool(_animIDJump, false);
                    _animator.SetBool(_animIDFreeFall, false);
                }

                // stop our velocity dropping infinitely when grounded
                if (_verticalVelocity < 0.0f)
                {
                    _verticalVelocity = -2f;
                }

                // Jump
                if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                {
                    // the square root of H * -2 * G = how much velocity needed to reach desired height
                    _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDJump, true);
                    }
                }

                // jump timeout
                if (_jumpTimeoutDelta >= 0.0f)
                {
                    _jumpTimeoutDelta -= Time.deltaTime;
                }
            }
            else
            {
                // reset the jump timeout timer
                _jumpTimeoutDelta = JumpTimeout;

                // fall timeout
                if (_fallTimeoutDelta >= 0.0f)
                {
                    _fallTimeoutDelta -= Time.deltaTime;
                }
                else
                {
                    // update animator if using character
                    if (_hasAnimator)
                    {
                        _animator.SetBool(_animIDFreeFall, true);
                    }
                }

                // if we are not grounded, do not jump
                _input.jump = false;
            }

            
            if (_verticalVelocity < _terminalVelocity)
            {
                _verticalVelocity += Gravity * Time.deltaTime;
            }
        }

        private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
        {
            if (lfAngle < -360f) lfAngle += 360f;
            if (lfAngle > 360f) lfAngle -= 360f;
            return Mathf.Clamp(lfAngle, lfMin, lfMax);
        }

        private void OnDrawGizmosSelected()
        {
            Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
            Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

            if (Grounded) Gizmos.color = transparentGreen;
            else Gizmos.color = transparentRed;

            // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
            Gizmos.DrawSphere(
                new Vector3(transform.position.x, transform.position.y - GroundedOffset, transform.position.z),
                GroundedRadius);
        }

        private void OnFootstep(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                if (FootstepAudioClips.Length > 0)
                {
                    var index = Random.Range(0, FootstepAudioClips.Length);
                    AudioSource.PlayClipAtPoint(FootstepAudioClips[index], transform.TransformPoint(_controller.center), FootstepAudioVolume);
                }
            }
        }

        private void OnLand(AnimationEvent animationEvent)
        {
            if (animationEvent.animatorClipInfo.weight > 0.5f)
            {
                AudioSource.PlayClipAtPoint(LandingAudioClip, transform.TransformPoint(_controller.center), FootstepAudioVolume);
            }
        }

        // CharacterControllerが他のコライダーに衝突したときに呼ばれる
        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            // 1. 地面にいることを確認（空中での衝突は重力で対処）
            if (!Grounded) return;

            // 2. 衝突した面が水平に近い（壁、または登れない急斜面）かチェック
            // hit.normal.y は法線ベクトルのY成分。0.1f 未満ならほぼ垂直と見なせる。
            if (hit.normal.y < 0.1f) 
            {
                // 3. 垂直速度を強制的にリセット（負の値にして地面に張り付かせる）
                // これにより、壁からの意図しない上向きの反力成分を無効化する。
                if (_verticalVelocity > 0f)
                {
                    // 0fにするとバグる可能性があるため、小さな負の値(-2f)を維持
                    _verticalVelocity = -2f; 

                    // おまけ：ジャンプ入力フラグもリセットし、ジャンプそのものも防止する
                    _input.jump = false;
                }
            }
        }

        // --- 状態更新時の処理（カメラとアニメ） ---
        private void UpdateState()
        {
            bool isBuilding = CurrentState == PlayerState.Building;
            // カメラの優先度切り替え（方針A）
            if (CurrentState == PlayerState.Building)
            {
                // FPSカメラを有効化
                FPSCamera.Priority = 20;
                TPSCamera.Priority = 10;
                
                // 設置モードに入ったときに、CharacterControllerのColliderを調整しても良い
                // _controller.height = 1.0f; // 例: 高さを低くする
            }
            else
            {
                // TPSカメラを有効化
                FPSCamera.Priority = 10;
                TPSCamera.Priority = 20;

                // _controller.height = 2.0f; // 例: 高さを戻す
            }
            // ***** 追加箇所：ドミノトリガーの有効/無効化 *****
            if (handFootTriggers != null)
            {
                foreach (var trigger in handFootTriggers)
                {
                    // Building モードのときだけ、ドミノを倒す機能を有効にする（isBuildingの値をそのまま渡す）
                    trigger.IsActive = isBuilding;

                    // コライダー自体を有効/無効にしたい場合は以下も実行
                    // trigger.gameObject.SetActive(isBuilding); 
                }
            }

            // アニメーターへの通知（四つん這い=Buildingとして扱う）
            if (_hasAnimator)
            {
                // IsBuildingがtrueの時、Animatorで四つん這いのアニメーションに遷移させる
                _animator.SetBool(_animIDIsBuilding, CurrentState == PlayerState.Building);
            }
        }
    }
}