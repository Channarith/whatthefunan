using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using WhatTheFunan.Characters;
using WhatTheFunan.Core;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// Virtual joystick for mobile touch controls.
    /// Drag to move the player character.
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        #region Settings
        [Header("Joystick Settings")]
        [SerializeField] private float _joystickRange = 50f;
        [SerializeField] private float _deadzone = 0.1f;
        [SerializeField] private bool _dynamicPosition = true;
        
        [Header("Visual")]
        [SerializeField] private RectTransform _background;
        [SerializeField] private RectTransform _handle;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private float _inactiveAlpha = 0.5f;
        [SerializeField] private float _activeAlpha = 1f;
        
        [Header("Haptics")]
        [SerializeField] private bool _enableHaptics = true;
        #endregion

        #region State
        private Vector2 _inputVector;
        private Vector2 _startPosition;
        private bool _isDragging;
        private Canvas _parentCanvas;
        private Camera _uiCamera;
        #endregion

        #region Properties
        public Vector2 InputVector => _inputVector;
        public bool IsDragging => _isDragging;
        public float Horizontal => _inputVector.x;
        public float Vertical => _inputVector.y;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _parentCanvas = GetComponentInParent<Canvas>();
            if (_parentCanvas.renderMode == RenderMode.ScreenSpaceCamera)
            {
                _uiCamera = _parentCanvas.worldCamera;
            }

            _startPosition = _background.anchoredPosition;
            
            SetAlpha(_inactiveAlpha);
        }

        private void Update()
        {
            // Send input to player controller
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.SetJoystickInput(_inputVector);
            }
        }
        #endregion

        #region Pointer Events
        public void OnPointerDown(PointerEventData eventData)
        {
            _isDragging = true;
            SetAlpha(_activeAlpha);
            
            // Haptic feedback
            if (_enableHaptics)
            {
                HapticManager.Instance?.TriggerHaptic(HapticManager.HapticType.Light);
            }

            // Dynamic positioning - move joystick to touch position
            if (_dynamicPosition)
            {
                Vector2 localPoint;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _background.parent as RectTransform,
                    eventData.position,
                    _uiCamera,
                    out localPoint
                );
                _background.anchoredPosition = localPoint;
            }

            OnDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _background,
                eventData.position,
                _uiCamera,
                out localPoint
            );

            // Calculate input vector
            _inputVector = localPoint / _joystickRange;
            
            // Clamp to unit circle
            if (_inputVector.magnitude > 1f)
            {
                _inputVector = _inputVector.normalized;
            }

            // Apply deadzone
            if (_inputVector.magnitude < _deadzone)
            {
                _inputVector = Vector2.zero;
            }

            // Move handle visual
            _handle.anchoredPosition = _inputVector * _joystickRange;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            _isDragging = false;
            _inputVector = Vector2.zero;
            SetAlpha(_inactiveAlpha);
            
            // Reset handle position
            _handle.anchoredPosition = Vector2.zero;
            
            // Reset background position if dynamic
            if (_dynamicPosition)
            {
                _background.anchoredPosition = _startPosition;
            }
        }
        #endregion

        #region Visual
        private void SetAlpha(float alpha)
        {
            if (_canvasGroup != null)
            {
                _canvasGroup.alpha = alpha;
            }
        }

        /// <summary>
        /// Show the joystick.
        /// </summary>
        public void Show()
        {
            gameObject.SetActive(true);
        }

        /// <summary>
        /// Hide the joystick.
        /// </summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            _inputVector = Vector2.zero;
            _isDragging = false;
        }
        #endregion
    }
}

