using UnityEngine;
using System;
using WhatTheFunan.Core;

namespace WhatTheFunan.Characters
{
    /// <summary>
    /// Main player character controller.
    /// Handles movement, input, and player state.
    /// Supports mobile touch controls.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        #region Singleton
        private static PlayerController _instance;
        public static PlayerController Instance => _instance;
        #endregion

        #region Events
        public static event Action OnPlayerSpawned;
        public static event Action OnPlayerDied;
        public static event Action<Vector3> OnPlayerMoved;
        public static event Action OnPlayerJumped;
        public static event Action OnPlayerLanded;
        #endregion

        #region Components
        private CharacterController _characterController;
        private Animator _animator;
        private Transform _cameraTransform;
        #endregion

        #region Movement Settings
        [Header("Movement")]
        [SerializeField] private float _moveSpeed = 5f;
        [SerializeField] private float _runSpeed = 8f;
        [SerializeField] private float _rotationSpeed = 10f;
        [SerializeField] private float _acceleration = 10f;
        [SerializeField] private float _deceleration = 10f;
        
        [Header("Jump")]
        [SerializeField] private float _jumpHeight = 1.5f;
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private LayerMask _groundLayer;
        
        [Header("Animation")]
        [SerializeField] private float _animationSmoothTime = 0.1f;
        #endregion

        #region Mobile Input
        [Header("Mobile Controls")]
        [SerializeField] private bool _useMobileControls = true;
        [SerializeField] private float _joystickDeadzone = 0.1f;
        
        private Vector2 _joystickInput;
        private bool _jumpRequested;
        private bool _runToggled;
        #endregion

        #region State
        public enum PlayerState
        {
            Idle,
            Walking,
            Running,
            Jumping,
            Falling,
            Combat,
            Interacting,
            Mounted,
            Disabled
        }

        [SerializeField] private PlayerState _currentState = PlayerState.Idle;
        public PlayerState CurrentState
        {
            get => _currentState;
            private set
            {
                if (_currentState != value)
                {
                    _currentState = value;
                    OnStateChanged(_currentState);
                }
            }
        }

        private Vector3 _velocity;
        private Vector3 _moveDirection;
        private float _currentSpeed;
        private bool _isGrounded;
        private bool _wasGrounded;
        #endregion

        #region Animation Hashes
        private static readonly int AnimSpeed = Animator.StringToHash("Speed");
        private static readonly int AnimIsGrounded = Animator.StringToHash("IsGrounded");
        private static readonly int AnimIsJumping = Animator.StringToHash("IsJumping");
        private static readonly int AnimIsFalling = Animator.StringToHash("IsFalling");
        private static readonly int AnimVerticalVelocity = Animator.StringToHash("VerticalVelocity");
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;

            _characterController = GetComponent<CharacterController>();
            _animator = GetComponentInChildren<Animator>();
            _cameraTransform = Camera.main?.transform;
        }

        private void Start()
        {
            OnPlayerSpawned?.Invoke();
            Debug.Log("[PlayerController] Player spawned");
        }

        private void Update()
        {
            if (CurrentState == PlayerState.Disabled) return;

            GatherInput();
            GroundCheck();
            Move();
            ApplyGravity();
            UpdateAnimator();
        }

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }
        #endregion

        #region Input
        private void GatherInput()
        {
            // Get input from virtual joystick or keyboard
            if (_useMobileControls)
            {
                // Mobile input is set externally by VirtualJoystick component
                // _joystickInput is already set
            }
            else
            {
                // Keyboard input for testing
                _joystickInput = new Vector2(
                    Input.GetAxisRaw("Horizontal"),
                    Input.GetAxisRaw("Vertical")
                );
                
                if (Input.GetButtonDown("Jump"))
                {
                    _jumpRequested = true;
                }
                
                _runToggled = Input.GetKey(KeyCode.LeftShift);
            }

            // Apply deadzone
            if (_joystickInput.magnitude < _joystickDeadzone)
            {
                _joystickInput = Vector2.zero;
            }
        }

        /// <summary>
        /// Set joystick input from virtual joystick (mobile).
        /// </summary>
        public void SetJoystickInput(Vector2 input)
        {
            _joystickInput = input;
        }

        /// <summary>
        /// Request a jump (from UI button).
        /// </summary>
        public void RequestJump()
        {
            _jumpRequested = true;
        }

        /// <summary>
        /// Toggle run mode (from UI button).
        /// </summary>
        public void ToggleRun(bool running)
        {
            _runToggled = running;
        }
        #endregion

        #region Movement
        private void Move()
        {
            if (CurrentState == PlayerState.Combat || 
                CurrentState == PlayerState.Interacting ||
                CurrentState == PlayerState.Mounted)
            {
                return;
            }

            // Calculate move direction relative to camera
            Vector3 inputDirection = new Vector3(_joystickInput.x, 0, _joystickInput.y).normalized;
            
            if (_cameraTransform != null && inputDirection.magnitude > 0.1f)
            {
                // Get camera forward/right on the horizontal plane
                Vector3 cameraForward = _cameraTransform.forward;
                Vector3 cameraRight = _cameraTransform.right;
                cameraForward.y = 0;
                cameraRight.y = 0;
                cameraForward.Normalize();
                cameraRight.Normalize();
                
                _moveDirection = (cameraForward * inputDirection.z + cameraRight * inputDirection.x).normalized;
            }
            else
            {
                _moveDirection = inputDirection;
            }

            // Calculate target speed
            float targetSpeed = 0f;
            if (_moveDirection.magnitude > 0.1f)
            {
                targetSpeed = _runToggled ? _runSpeed : _moveSpeed;
            }

            // Smoothly adjust current speed
            if (targetSpeed > _currentSpeed)
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _acceleration * Time.deltaTime);
            }
            else
            {
                _currentSpeed = Mathf.MoveTowards(_currentSpeed, targetSpeed, _deceleration * Time.deltaTime);
            }

            // Apply movement
            Vector3 movement = _moveDirection * _currentSpeed;
            _characterController.Move(movement * Time.deltaTime);

            // Rotate towards movement direction
            if (_moveDirection.magnitude > 0.1f)
            {
                Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, _rotationSpeed * Time.deltaTime);
            }

            // Update state
            if (_isGrounded)
            {
                if (_currentSpeed > 0.1f)
                {
                    CurrentState = _runToggled ? PlayerState.Running : PlayerState.Walking;
                }
                else
                {
                    CurrentState = PlayerState.Idle;
                }
            }

            OnPlayerMoved?.Invoke(transform.position);
        }

        private void GroundCheck()
        {
            _wasGrounded = _isGrounded;
            
            // Check if grounded using spherecast
            _isGrounded = Physics.SphereCast(
                transform.position + Vector3.up * 0.5f,
                0.3f,
                Vector3.down,
                out RaycastHit hit,
                _groundCheckDistance + 0.5f,
                _groundLayer
            );

            // Just landed
            if (_isGrounded && !_wasGrounded)
            {
                OnPlayerLanded?.Invoke();
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
            }
        }

        private void ApplyGravity()
        {
            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f; // Small downward force to keep grounded
            }

            // Handle jump
            if (_jumpRequested && _isGrounded)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                CurrentState = PlayerState.Jumping;
                OnPlayerJumped?.Invoke();
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
                _jumpRequested = false;
            }
            else
            {
                _jumpRequested = false;
            }

            // Apply gravity
            _velocity.y += _gravity * Time.deltaTime;
            _characterController.Move(_velocity * Time.deltaTime);

            // Update falling state
            if (!_isGrounded && _velocity.y < 0)
            {
                CurrentState = PlayerState.Falling;
            }
        }
        #endregion

        #region Animation
        private void UpdateAnimator()
        {
            if (_animator == null) return;

            float normalizedSpeed = _currentSpeed / _runSpeed;
            
            _animator.SetFloat(AnimSpeed, normalizedSpeed, _animationSmoothTime, Time.deltaTime);
            _animator.SetBool(AnimIsGrounded, _isGrounded);
            _animator.SetBool(AnimIsJumping, CurrentState == PlayerState.Jumping);
            _animator.SetBool(AnimIsFalling, CurrentState == PlayerState.Falling);
            _animator.SetFloat(AnimVerticalVelocity, _velocity.y);
        }
        #endregion

        #region State Management
        private void OnStateChanged(PlayerState newState)
        {
            Debug.Log($"[PlayerController] State changed to: {newState}");
            
            // Handle state-specific logic
            switch (newState)
            {
                case PlayerState.Disabled:
                    _currentSpeed = 0;
                    _velocity = Vector3.zero;
                    break;
            }
        }

        /// <summary>
        /// Enable player controls.
        /// </summary>
        public void EnableControls()
        {
            if (CurrentState == PlayerState.Disabled)
            {
                CurrentState = PlayerState.Idle;
            }
        }

        /// <summary>
        /// Disable player controls.
        /// </summary>
        public void DisableControls()
        {
            CurrentState = PlayerState.Disabled;
        }

        /// <summary>
        /// Enter combat mode.
        /// </summary>
        public void EnterCombat()
        {
            CurrentState = PlayerState.Combat;
        }

        /// <summary>
        /// Exit combat mode.
        /// </summary>
        public void ExitCombat()
        {
            CurrentState = PlayerState.Idle;
        }

        /// <summary>
        /// Start interacting (dialogue, objects).
        /// </summary>
        public void StartInteraction()
        {
            CurrentState = PlayerState.Interacting;
        }

        /// <summary>
        /// End interaction.
        /// </summary>
        public void EndInteraction()
        {
            CurrentState = PlayerState.Idle;
        }

        /// <summary>
        /// Mount a creature.
        /// </summary>
        public void Mount()
        {
            CurrentState = PlayerState.Mounted;
            // TODO: Handle mount logic
        }

        /// <summary>
        /// Dismount from creature.
        /// </summary>
        public void Dismount()
        {
            CurrentState = PlayerState.Idle;
            // TODO: Handle dismount logic
        }
        #endregion

        #region Teleport/Spawn
        /// <summary>
        /// Teleport the player to a position.
        /// </summary>
        public void Teleport(Vector3 position)
        {
            _characterController.enabled = false;
            transform.position = position;
            _characterController.enabled = true;
            _velocity = Vector3.zero;
        }

        /// <summary>
        /// Teleport the player and set rotation.
        /// </summary>
        public void Teleport(Vector3 position, Quaternion rotation)
        {
            Teleport(position);
            transform.rotation = rotation;
        }
        #endregion

        #region Death
        /// <summary>
        /// Handle player death.
        /// </summary>
        public void Die()
        {
            DisableControls();
            OnPlayerDied?.Invoke();
            
            // TODO: Play death animation
            // TODO: Show game over screen
        }
        #endregion

        #region Debug
        private void OnDrawGizmosSelected()
        {
            // Draw ground check
            Gizmos.color = _isGrounded ? Color.green : Color.red;
            Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.5f - Vector3.up * _groundCheckDistance, 0.3f);
        }
        #endregion
    }
}

