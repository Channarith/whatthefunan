using UnityEngine;
using System;
using System.Collections;
using System.IO;

namespace WhatTheFunan.Features
{
    /// <summary>
    /// Photo mode for capturing in-game screenshots.
    /// Includes filters, stickers, frames, and sharing.
    /// </summary>
    public class PhotoMode : MonoBehaviour
    {
        #region Singleton
        private static PhotoMode _instance;
        public static PhotoMode Instance => _instance;
        #endregion

        #region Events
        public static event Action OnPhotoModeEntered;
        public static event Action OnPhotoModeExited;
        public static event Action<Texture2D> OnPhotoTaken;
        public static event Action<string> OnPhotoSaved;
        #endregion

        #region State
        private bool _isActive;
        private Camera _photoCamera;
        private RenderTexture _captureTexture;
        
        public bool IsActive => _isActive;
        #endregion

        #region Settings
        [Header("Capture Settings")]
        [SerializeField] private int _captureWidth = 1920;
        [SerializeField] private int _captureHeight = 1080;
        [SerializeField] private int _superSampleMultiplier = 2;
        
        [Header("Camera Controls")]
        [SerializeField] private float _zoomSpeed = 2f;
        [SerializeField] private float _minZoom = 30f;
        [SerializeField] private float _maxZoom = 90f;
        [SerializeField] private float _rotateSpeed = 100f;
        [SerializeField] private float _panSpeed = 5f;
        
        [Header("Filters")]
        [SerializeField] private Material[] _filterMaterials;
        private int _currentFilterIndex = -1;
        
        [Header("Stickers")]
        [SerializeField] private Sprite[] _stickers;
        
        [Header("Frames")]
        [SerializeField] private Sprite[] _frames;
        private int _currentFrameIndex = -1;
        #endregion

        #region Camera State
        private Vector3 _originalCameraPos;
        private Quaternion _originalCameraRot;
        private float _originalFOV;
        
        private float _currentZoom;
        private Vector3 _currentRotation;
        private Vector3 _currentPan;
        #endregion

        #region UI
        [Header("UI")]
        [SerializeField] private GameObject _photoModeUI;
        [SerializeField] private GameObject _previewPanel;
        [SerializeField] private UnityEngine.UI.RawImage _previewImage;
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
        }

        private void Update()
        {
            if (!_isActive) return;
            
            HandleCameraInput();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
            
            if (_captureTexture != null)
            {
                _captureTexture.Release();
            }
        }
        #endregion

        #region Photo Mode Control
        /// <summary>
        /// Enter photo mode.
        /// </summary>
        public void EnterPhotoMode()
        {
            if (_isActive) return;
            
            _isActive = true;
            
            // Store original camera state
            _photoCamera = Camera.main;
            if (_photoCamera != null)
            {
                _originalCameraPos = _photoCamera.transform.position;
                _originalCameraRot = _photoCamera.transform.rotation;
                _originalFOV = _photoCamera.fieldOfView;
                _currentZoom = _originalFOV;
            }
            
            // Pause game
            Core.GameManager.Instance?.PauseGame();
            
            // Show UI
            if (_photoModeUI != null)
            {
                _photoModeUI.SetActive(true);
            }
            
            // Hide HUD
            UI.UIManager.Instance?.HideHUD();
            
            // Create capture texture
            int captureW = _captureWidth * _superSampleMultiplier;
            int captureH = _captureHeight * _superSampleMultiplier;
            _captureTexture = new RenderTexture(captureW, captureH, 24);
            
            OnPhotoModeEntered?.Invoke();
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            Debug.Log("[PhotoMode] Entered photo mode");
        }

        /// <summary>
        /// Exit photo mode.
        /// </summary>
        public void ExitPhotoMode()
        {
            if (!_isActive) return;
            
            _isActive = false;
            
            // Restore camera
            if (_photoCamera != null)
            {
                _photoCamera.transform.position = _originalCameraPos;
                _photoCamera.transform.rotation = _originalCameraRot;
                _photoCamera.fieldOfView = _originalFOV;
            }
            
            // Resume game
            Core.GameManager.Instance?.ResumeGame();
            
            // Hide UI
            if (_photoModeUI != null)
            {
                _photoModeUI.SetActive(false);
            }
            
            if (_previewPanel != null)
            {
                _previewPanel.SetActive(false);
            }
            
            // Show HUD
            UI.UIManager.Instance?.ShowHUD();
            
            // Cleanup
            if (_captureTexture != null)
            {
                _captureTexture.Release();
                _captureTexture = null;
            }
            
            OnPhotoModeExited?.Invoke();
            
            Debug.Log("[PhotoMode] Exited photo mode");
        }
        #endregion

        #region Camera Control
        private void HandleCameraInput()
        {
            if (_photoCamera == null) return;
            
            // Touch/Mouse input for rotation and pan would go here
            // For now, just keyboard controls
            
            // Zoom (scroll wheel or pinch)
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                Zoom(-scroll * _zoomSpeed * 10f);
            }
            
            // Rotation (arrow keys or drag)
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                Rotate(Vector2.left * _rotateSpeed * Time.unscaledDeltaTime);
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                Rotate(Vector2.right * _rotateSpeed * Time.unscaledDeltaTime);
            }
            if (Input.GetKey(KeyCode.UpArrow))
            {
                Rotate(Vector2.up * _rotateSpeed * Time.unscaledDeltaTime);
            }
            if (Input.GetKey(KeyCode.DownArrow))
            {
                Rotate(Vector2.down * _rotateSpeed * Time.unscaledDeltaTime);
            }
        }

        /// <summary>
        /// Zoom the camera.
        /// </summary>
        public void Zoom(float amount)
        {
            if (_photoCamera == null) return;
            
            _currentZoom = Mathf.Clamp(_currentZoom + amount, _minZoom, _maxZoom);
            _photoCamera.fieldOfView = _currentZoom;
        }

        /// <summary>
        /// Rotate the camera.
        /// </summary>
        public void Rotate(Vector2 delta)
        {
            if (_photoCamera == null) return;
            
            _currentRotation.x -= delta.y;
            _currentRotation.y += delta.x;
            _currentRotation.x = Mathf.Clamp(_currentRotation.x, -80f, 80f);
            
            _photoCamera.transform.rotation = _originalCameraRot * 
                Quaternion.Euler(_currentRotation.x, _currentRotation.y, 0);
        }

        /// <summary>
        /// Pan the camera.
        /// </summary>
        public void Pan(Vector2 delta)
        {
            if (_photoCamera == null) return;
            
            Vector3 movement = _photoCamera.transform.right * delta.x + 
                              _photoCamera.transform.up * delta.y;
            _currentPan += movement * _panSpeed * Time.unscaledDeltaTime;
            
            _photoCamera.transform.position = _originalCameraPos + _currentPan;
        }

        /// <summary>
        /// Reset camera to original position.
        /// </summary>
        public void ResetCamera()
        {
            if (_photoCamera == null) return;
            
            _currentZoom = _originalFOV;
            _currentRotation = Vector3.zero;
            _currentPan = Vector3.zero;
            
            _photoCamera.transform.position = _originalCameraPos;
            _photoCamera.transform.rotation = _originalCameraRot;
            _photoCamera.fieldOfView = _originalFOV;
        }
        #endregion

        #region Filters
        /// <summary>
        /// Apply a filter.
        /// </summary>
        public void ApplyFilter(int filterIndex)
        {
            _currentFilterIndex = filterIndex;
            
            // Apply post-processing filter
            // This would use a post-processing volume or camera effect
        }

        /// <summary>
        /// Cycle to next filter.
        /// </summary>
        public void NextFilter()
        {
            _currentFilterIndex = (_currentFilterIndex + 1) % (_filterMaterials.Length + 1);
            if (_currentFilterIndex == _filterMaterials.Length)
            {
                _currentFilterIndex = -1; // No filter
            }
            ApplyFilter(_currentFilterIndex);
        }
        #endregion

        #region Frames
        /// <summary>
        /// Apply a frame.
        /// </summary>
        public void ApplyFrame(int frameIndex)
        {
            _currentFrameIndex = frameIndex;
            // Apply frame overlay
        }

        /// <summary>
        /// Cycle to next frame.
        /// </summary>
        public void NextFrame()
        {
            _currentFrameIndex = (_currentFrameIndex + 1) % (_frames.Length + 1);
            if (_currentFrameIndex == _frames.Length)
            {
                _currentFrameIndex = -1; // No frame
            }
            ApplyFrame(_currentFrameIndex);
        }
        #endregion

        #region Capture
        /// <summary>
        /// Take a photo.
        /// </summary>
        public void TakePhoto()
        {
            if (!_isActive || _photoCamera == null) return;
            
            StartCoroutine(CapturePhoto());
        }

        private IEnumerator CapturePhoto()
        {
            // Hide UI temporarily
            if (_photoModeUI != null)
            {
                _photoModeUI.SetActive(false);
            }
            
            yield return new WaitForEndOfFrame();
            
            // Capture
            RenderTexture oldTarget = _photoCamera.targetTexture;
            _photoCamera.targetTexture = _captureTexture;
            _photoCamera.Render();
            _photoCamera.targetTexture = oldTarget;
            
            // Convert to Texture2D
            RenderTexture.active = _captureTexture;
            Texture2D photo = new Texture2D(_captureWidth, _captureHeight, TextureFormat.RGB24, false);
            photo.ReadPixels(new Rect(0, 0, _captureWidth, _captureHeight), 0, 0);
            photo.Apply();
            RenderTexture.active = null;
            
            // Show preview
            ShowPreview(photo);
            
            OnPhotoTaken?.Invoke(photo);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log("[PhotoMode] Photo taken!");
        }

        private void ShowPreview(Texture2D photo)
        {
            if (_previewPanel != null)
            {
                _previewPanel.SetActive(true);
            }
            
            if (_previewImage != null)
            {
                _previewImage.texture = photo;
            }
        }
        #endregion

        #region Save & Share
        /// <summary>
        /// Save the current photo to gallery.
        /// </summary>
        public void SavePhoto(Texture2D photo)
        {
            if (photo == null) return;
            
            byte[] bytes = photo.EncodeToPNG();
            string filename = $"WhatTheFunan_{DateTime.Now:yyyyMMdd_HHmmss}.png";
            
            #if UNITY_EDITOR
            // Save to project folder in editor
            string path = Path.Combine(Application.dataPath, "..", "Screenshots", filename);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllBytes(path, bytes);
            Debug.Log($"[PhotoMode] Saved to: {path}");
            #else
            // Save to device gallery
            // NativeGallery.SaveImageToGallery(bytes, "What the Funan", filename, callback);
            #endif
            
            OnPhotoSaved?.Invoke(filename);
            UI.UIManager.Instance?.ShowToast("Photo saved!");
        }

        /// <summary>
        /// Share the current photo.
        /// </summary>
        public void SharePhoto(Texture2D photo)
        {
            if (photo == null) return;
            
            // TODO: Use NativeShare plugin
            // new NativeShare()
            //     .AddFile(photo, "photo.png")
            //     .SetSubject("Check out my photo from What the Funan!")
            //     .SetText("Playing What the Funan!")
            //     .Share();
            
            Debug.Log("[PhotoMode] Sharing photo...");
        }
        #endregion
    }
}

