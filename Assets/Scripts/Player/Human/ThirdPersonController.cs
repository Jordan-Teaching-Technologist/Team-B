﻿using UnityEngine;
using Photon.Pun;
using UnityEngine.UI;
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
    public class ThirdPersonController : MonoBehaviourPun, IPunObservable
    {
        [Header("Player")] [Tooltip("Move speed of the character in m/s")]
        public float MoveSpeed = 2.0f;

        [Tooltip("Sprint speed of the character in m/s")]
        public float SprintSpeed = 5.335f;

        [Tooltip("How fast the character turns to face movement direction")] [Range(0.0f, 0.3f)]
        public float RotationSmoothTime = 0.12f;

        [Tooltip("Acceleration and deceleration")]
        public float SpeedChangeRate = 10.0f;

        public AudioClip LandingAudioClip;
        public AudioClip[] FootstepAudioClips;
        [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

        [Space(10)] [Tooltip("The height the player can jump")]
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

        [Tooltip("Useful for rough ground")] public float GroundedOffset = -0.14f;

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

        public float Sensitivity = 1f;

        [Tooltip("For locking the camera position on all axis")]
        public bool LockCameraPosition = false;
        
        [Header("Stamina Bar")]
        public float Stamina = 1.0f;
        public float StaminaDecreaseRate = 0.2f; 
        public float StaminaRecoveryRate = 0.1f; 



        // cinemachine
        private float _cinemachineTargetYaw;
        private float _cinemachineTargetPitch;

        // player
        private float _speed;
        private float _animationBlend;
        private float _targetRotation = 0.0f;
        private float _rotationVelocity;
        private float _verticalVelocity;
        private float _terminalVelocity = 53.0f;

        // timeout deltatime
        private float _jumpTimeoutDelta;
        private float _fallTimeoutDelta;

        // animation IDs
        private int _animIDSpeed;
        private int _animIDGrounded;
        private int _animIDJump;
        private int _animIDFreeFall;
        private int _animIDMotionSpeed;

#if ENABLE_INPUT_SYSTEM
        private PlayerInput _playerInput;
#endif
        private Animator _animator;
        private CharacterController _controller;
        private StarterAssetsInputs _input;
        private GameObject _mainCamera;

        

        private const float _threshold = 0.01f;
        private bool _rotateOnMove = true;

        private bool _hasAnimator;

        private bool canSprint = true; 

        // current pos and rot
        public Vector3 currentPos;
        public Quaternion currentRot;

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
            _input = GetComponent<StarterAssetsInputs>();
            // get a reference to our main camera
            if (_mainCamera == null)
            {
                _mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
            }

            _hasAnimator = TryGetComponent(out _animator);

            if (_hasAnimator)
            {
                AssignAnimationIDs();
            }
        }

        private void Start()
        {
            _cinemachineTargetYaw = CinemachineCameraTarget.transform.rotation.eulerAngles.y;

            _controller = GetComponent<CharacterController>();

#if ENABLE_INPUT_SYSTEM
            _playerInput = GetComponent<PlayerInput>();
#else
			Debug.LogError( "Starter Assets package is missing dependencies. Please use Tools/Starter Assets/Reinstall Dependencies to fix it");
#endif

            // reset our timeouts on start
            _jumpTimeoutDelta = JumpTimeout;
            _fallTimeoutDelta = FallTimeout;

            currentPos = transform.position;
            currentRot = transform.rotation;
        }

        private void Update()
        {
            _hasAnimator = TryGetComponent(out _animator);

            if(photonView.IsMine){
                JumpAndGravity();
                GroundedCheck();
                Move();
                pickup();
            }
            else
            {
                UpdateOther();
            }
        }

        private void pickup()
        {
            if (photonView.IsMine && Keyboard.current.rKey.wasPressedThisFrame)
            {
                _animator.SetTrigger("pickup");
                photonView.RPC("TriggerPickupAnimation", RpcTarget.All);
            }
        }

        private void OnControllerColliderHit(ControllerColliderHit hit)
        {
            if (hit.gameObject.CompareTag("Target"))
            {

                PhotonView targetPhotonView = hit.gameObject.GetComponent<PhotonView>();


                if (targetPhotonView != null && Input.GetKeyDown(KeyCode.R))
                {
                    // 调用目标上的RPC方法来显示DeiUI
                    targetPhotonView.RPC("showDeiUI", targetPhotonView.Owner, null);
                }
            }
        }

        [PunRPC]
        void TriggerPickupAnimation()
        {
            _animator.SetTrigger("pickup");
        }

        private void LateUpdate()
        {
            CameraRotation();
        }

        void UpdateOther()
        {
            transform.position = Vector3.Lerp(transform.position, currentPos, Time.deltaTime * 10);
            transform.rotation = Quaternion.Slerp(transform.rotation, currentRot, Time.deltaTime * 500);
        }

        private void AssignAnimationIDs()
        {
            _animIDSpeed = Animator.StringToHash("Speed");
            _animIDGrounded = Animator.StringToHash("Grounded");
            _animIDJump = Animator.StringToHash("Jump");
            _animIDFreeFall = Animator.StringToHash("FreeFall");
            _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");
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

                _cinemachineTargetYaw += _input.look.x * deltaTimeMultiplier * Sensitivity;
                _cinemachineTargetPitch += _input.look.y * deltaTimeMultiplier * Sensitivity;
            }

            // clamp our rotations so our values are limited 360 degrees
            _cinemachineTargetYaw = ClampAngle(_cinemachineTargetYaw, float.MinValue, float.MaxValue);
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Cinemachine will follow this target
            CinemachineCameraTarget.transform.rotation = Quaternion.Euler(_cinemachineTargetPitch + CameraAngleOverride,
                _cinemachineTargetYaw, 0.0f);
        }

        private void Move()
        {
            float targetSpeed = (_input.move != Vector2.zero && canSprint && Stamina > 0 && _input.sprint) ? SprintSpeed : MoveSpeed;
            if (_input.sprint && canSprint && Stamina > 0 && _input.move != Vector2.zero)
            {
                Stamina -= StaminaDecreaseRate * Time.deltaTime;
                if (Stamina <= 0)
                {
                    Stamina = 0;
                    canSprint = false;
                }
            }

            UpdateStamina();

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

                if (_rotateOnMove)
                {
                    transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
                }
            }


            Vector3 targetDirection = Quaternion.Euler(0.0f, _targetRotation, 0.0f) * Vector3.forward;

            // move the player
            _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) +
                             new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);

            // update animator if using character
            if (_hasAnimator)
            {
                _animator.SetFloat(_animIDSpeed, _animationBlend);
                _animator.SetFloat(_animIDMotionSpeed, inputMagnitude);
            }
        }


            private void UpdateStamina()
            {
                if (!_input.sprint || !canSprint || _input.move != Vector2.zero)
                {
                    Stamina += StaminaRecoveryRate * Time.deltaTime;
                    if (Stamina >= 1)
                    {
                        Stamina = 1;
                        canSprint = true; // 体力完全恢复，现在可以再次奔跑
                    }
                }

                if (FightUI.Instance != null)
                {
                    FightUI.Instance.UpdateStaminaBar(Stamina); 
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

                // Jump - DISABLED
                // if (_input.jump && _jumpTimeoutDelta <= 0.0f)
                // {
                //     _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);
                //
                //     if (_hasAnimator)
                //     {
                //         _animator.SetBool(_animIDJump, true);
                //     }
                // }

                // jump timeout - still count down to handle any existing logic dependencies, but jumping is disabled
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
                // _input.jump = false; - This line is now unnecessary as jumping is disabled
            }

            // apply gravity over time if under terminal (multiply by delta time twice to linearly speed up over time)
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

        public void SetSensitivity(float newSensitivity)
        {
            Sensitivity = newSensitivity;
        }

        public void SetRotateOnMove(bool newRotateOnMove)
        {
            _rotateOnMove = newRotateOnMove;
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

        

        public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(_input.move.x);
                stream.SendNext(_input.move.y);
                stream.SendNext(transform.position);
                stream.SendNext(transform.rotation);
                stream.SendNext(_input.shoot);
                stream.SendNext(_input.look.x);
                stream.SendNext(_input.look.y);

                stream.SendNext(_animator.GetFloat(_animIDSpeed));
                stream.SendNext(_animator.GetBool(_animIDGrounded));
                stream.SendNext(_animator.GetBool(_animIDJump));
                stream.SendNext(_animator.GetBool(_animIDFreeFall));
                stream.SendNext(_animator.GetFloat(_animIDMotionSpeed));

            }
            else
            {
                _input.move.x = (float)stream.ReceiveNext();
                _input.move.y = (float)stream.ReceiveNext();
                currentPos = (Vector3)stream.ReceiveNext();
                currentRot = (Quaternion)stream.ReceiveNext();
                _input.shoot = (bool)stream.ReceiveNext();
                _input.look.x = (float)stream.ReceiveNext();
                _input.look.y = (float)stream.ReceiveNext();

                _animator.SetFloat(_animIDSpeed, (float)stream.ReceiveNext());
                _animator.SetBool(_animIDGrounded, (bool)stream.ReceiveNext());
                _animator.SetBool(_animIDJump, (bool)stream.ReceiveNext());
                _animator.SetBool(_animIDFreeFall, (bool)stream.ReceiveNext());
                _animator.SetFloat(_animIDMotionSpeed, (float)stream.ReceiveNext());
            }
        }
    }
}