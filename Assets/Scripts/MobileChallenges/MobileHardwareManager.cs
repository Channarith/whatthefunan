using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.MobileChallenges
{
    /// <summary>
    /// MOBILE HARDWARE MANAGER! 📱
    /// Interfaces with real phone hardware for interactive challenges!
    /// Haptics, Accelerometer, Gyroscope, Camera, GPS, Volume, Microphone!
    /// </summary>
    public class MobileHardwareManager : MonoBehaviour
    {
        public static MobileHardwareManager Instance { get; private set; }

        [Header("Hardware Availability")]
        [SerializeField] private bool _hasAccelerometer;
        [SerializeField] private bool _hasGyroscope;
        [SerializeField] private bool _hasHaptics;
        [SerializeField] private bool _hasCamera;
        [SerializeField] private bool _hasGPS;
        [SerializeField] private bool _hasMicrophone;

        [Header("Sensor Data")]
        [SerializeField] private Vector3 _acceleration;
        [SerializeField] private Vector3 _gyroscope;
        [SerializeField] private float _shakeIntensity;
        [SerializeField] private Vector3 _tilt;

        [Header("Motion Detection Settings")]
        [SerializeField] private float _shakeThreshold = 2.5f;
        [SerializeField] private float _sliceThreshold = 15f;
        [SerializeField] private float _tiltSensitivity = 1f;

        // Events
        public event Action<float> OnShakeDetected;
        public event Action<SliceDirection> OnSliceDetected;
        public event Action<Vector3> OnTiltChanged;
        public event Action OnVolumeUpPressed;
        public event Action OnVolumeDownPressed;
        public event Action<float> OnLoudSound;
        public event Action<Vector2> OnDeviceLocationChanged;

        // Internal state
        private Vector3 _lastAcceleration;
        private float _lastShakeTime;
        private Queue<Vector3> _accelerationHistory = new Queue<Vector3>();
        private const int HISTORY_SIZE = 10;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeHardware();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeHardware()
        {
            // Check hardware availability
            _hasAccelerometer = SystemInfo.supportsAccelerometer;
            _hasGyroscope = SystemInfo.supportsGyroscope;
            _hasHaptics = SupportsHaptics();
            _hasCamera = WebCamTexture.devices.Length > 0;
            _hasGPS = Input.location.isEnabledByUser;
            _hasMicrophone = Microphone.devices.Length > 0;

            // Enable gyroscope if available
            if (_hasGyroscope)
            {
                Input.gyro.enabled = true;
            }

            Debug.Log($"📱 Hardware Check:");
            Debug.Log($"  Accelerometer: {_hasAccelerometer}");
            Debug.Log($"  Gyroscope: {_hasGyroscope}");
            Debug.Log($"  Haptics: {_hasHaptics}");
            Debug.Log($"  Camera: {_hasCamera}");
            Debug.Log($"  GPS: {_hasGPS}");
            Debug.Log($"  Microphone: {_hasMicrophone}");
        }

        private bool SupportsHaptics()
        {
#if UNITY_IOS
            return true; // iOS has Taptic Engine
#elif UNITY_ANDROID
            return true; // Android has vibration
#else
            return false;
#endif
        }

        private void Update()
        {
            if (_hasAccelerometer)
            {
                UpdateAccelerometer();
                DetectShake();
                DetectSlice();
                UpdateTilt();
            }

            if (_hasGyroscope)
            {
                UpdateGyroscope();
            }
        }

        #region Accelerometer & Motion

        private void UpdateAccelerometer()
        {
            _acceleration = Input.acceleration;

            // Store history for motion analysis
            _accelerationHistory.Enqueue(_acceleration);
            if (_accelerationHistory.Count > HISTORY_SIZE)
            {
                _accelerationHistory.Dequeue();
            }
        }

        private void DetectShake()
        {
            Vector3 deltaAcceleration = _acceleration - _lastAcceleration;
            _shakeIntensity = deltaAcceleration.magnitude;

            if (_shakeIntensity > _shakeThreshold && Time.time - _lastShakeTime > 0.3f)
            {
                _lastShakeTime = Time.time;
                OnShakeDetected?.Invoke(_shakeIntensity);
                Debug.Log($"📳 SHAKE detected! Intensity: {_shakeIntensity:F2}");
            }

            _lastAcceleration = _acceleration;
        }

        private void DetectSlice()
        {
            if (_accelerationHistory.Count < HISTORY_SIZE) return;

            Vector3[] history = _accelerationHistory.ToArray();
            Vector3 startVelocity = history[0];
            Vector3 endVelocity = history[HISTORY_SIZE - 1];
            Vector3 sliceVector = endVelocity - startVelocity;
            float sliceSpeed = sliceVector.magnitude;

            if (sliceSpeed > _sliceThreshold)
            {
                SliceDirection direction = DetermineSliceDirection(sliceVector);
                OnSliceDetected?.Invoke(direction);
                Debug.Log($"⚔️ SLICE detected! Direction: {direction}, Speed: {sliceSpeed:F2}");
            }
        }

        private SliceDirection DetermineSliceDirection(Vector3 sliceVector)
        {
            // Determine primary direction
            float absX = Mathf.Abs(sliceVector.x);
            float absY = Mathf.Abs(sliceVector.y);
            float absZ = Mathf.Abs(sliceVector.z);

            if (absX > absY && absX > absZ)
            {
                return sliceVector.x > 0 ? SliceDirection.Right : SliceDirection.Left;
            }
            else if (absY > absX && absY > absZ)
            {
                return sliceVector.y > 0 ? SliceDirection.Up : SliceDirection.Down;
            }
            else
            {
                return sliceVector.z > 0 ? SliceDirection.Forward : SliceDirection.Back;
            }
        }

        private void UpdateTilt()
        {
            // Convert acceleration to tilt angles
            _tilt = new Vector3(
                Mathf.Atan2(_acceleration.y, _acceleration.z) * Mathf.Rad2Deg,
                Mathf.Atan2(_acceleration.x, _acceleration.z) * Mathf.Rad2Deg,
                Mathf.Atan2(_acceleration.x, _acceleration.y) * Mathf.Rad2Deg
            ) * _tiltSensitivity;

            OnTiltChanged?.Invoke(_tilt);
        }

        #endregion

        #region Gyroscope

        private void UpdateGyroscope()
        {
            if (!_hasGyroscope) return;
            _gyroscope = Input.gyro.rotationRate;
        }

        public Quaternion GetGyroRotation()
        {
            if (!_hasGyroscope) return Quaternion.identity;
            return Input.gyro.attitude;
        }

        #endregion

        #region Haptic Feedback

        public void TriggerHaptic(HapticType type)
        {
            if (!_hasHaptics) return;

#if UNITY_IOS
            TriggerIOSHaptic(type);
#elif UNITY_ANDROID
            TriggerAndroidHaptic(type);
#endif
        }

        public void TriggerHapticPattern(float[] pattern, float[] amplitudes)
        {
            if (!_hasHaptics) return;

#if UNITY_ANDROID
            // Android supports vibration patterns
            long[] androidPattern = new long[pattern.Length];
            for (int i = 0; i < pattern.Length; i++)
            {
                androidPattern[i] = (long)(pattern[i] * 1000);
            }
            // Would use AndroidJavaObject to call Vibrator.vibrate(pattern, -1)
#endif

            Debug.Log($"📳 Haptic pattern triggered: {pattern.Length} segments");
        }

        public void TriggerRhythmHaptic(float bpm, int beats)
        {
            float beatDuration = 60f / bpm;
            StartCoroutine(RhythmHapticCoroutine(beatDuration, beats));
        }

        private System.Collections.IEnumerator RhythmHapticCoroutine(float beatDuration, int beats)
        {
            for (int i = 0; i < beats; i++)
            {
                TriggerHaptic(HapticType.Medium);
                yield return new WaitForSeconds(beatDuration);
            }
        }

#if UNITY_IOS
        private void TriggerIOSHaptic(HapticType type)
        {
            // Use Unity's iOS haptics or native plugin
            switch (type)
            {
                case HapticType.Light:
                    // UIImpactFeedbackGenerator with .light style
                    Handheld.Vibrate(); // Simplified
                    break;
                case HapticType.Medium:
                    Handheld.Vibrate();
                    break;
                case HapticType.Heavy:
                    Handheld.Vibrate();
                    break;
                case HapticType.Success:
                    // UINotificationFeedbackGenerator with .success
                    Handheld.Vibrate();
                    break;
                case HapticType.Warning:
                    // UINotificationFeedbackGenerator with .warning
                    Handheld.Vibrate();
                    break;
                case HapticType.Error:
                    // UINotificationFeedbackGenerator with .error
                    Handheld.Vibrate();
                    break;
            }
        }
#endif

#if UNITY_ANDROID
        private void TriggerAndroidHaptic(HapticType type)
        {
            int duration = type switch
            {
                HapticType.Light => 10,
                HapticType.Medium => 25,
                HapticType.Heavy => 50,
                HapticType.Success => 30,
                HapticType.Warning => 40,
                HapticType.Error => 100,
                _ => 25
            };

            Handheld.Vibrate();
        }
#endif

        #endregion

        #region Volume Buttons

        // Note: Volume button detection requires native plugins
        // This is a placeholder for the interface
        public void SimulateVolumeUp()
        {
            OnVolumeUpPressed?.Invoke();
            Debug.Log("🔊 Volume UP pressed!");
        }

        public void SimulateVolumeDown()
        {
            OnVolumeDownPressed?.Invoke();
            Debug.Log("🔉 Volume DOWN pressed!");
        }

        #endregion

        #region Microphone

        private AudioClip _microphoneClip;
        private bool _isListening;

        public void StartListening()
        {
            if (!_hasMicrophone || _isListening) return;

            string device = Microphone.devices[0];
            _microphoneClip = Microphone.Start(device, true, 1, 44100);
            _isListening = true;
            Debug.Log("🎤 Microphone listening started");
        }

        public void StopListening()
        {
            if (!_isListening) return;

            Microphone.End(Microphone.devices[0]);
            _isListening = false;
            Debug.Log("🎤 Microphone listening stopped");
        }

        public float GetMicrophoneLevel()
        {
            if (!_isListening || _microphoneClip == null) return 0f;

            float[] samples = new float[128];
            int position = Microphone.GetPosition(Microphone.devices[0]) - 128;
            if (position < 0) return 0f;

            _microphoneClip.GetData(samples, position);

            float sum = 0f;
            foreach (float sample in samples)
            {
                sum += Mathf.Abs(sample);
            }

            float level = sum / samples.Length;

            if (level > 0.1f)
            {
                OnLoudSound?.Invoke(level);
            }

            return level;
        }

        #endregion

        #region GPS/Location

        public void StartLocationService()
        {
            if (!_hasGPS) return;

            Input.location.Start(10f, 10f); // accuracy, update distance
            StartCoroutine(WaitForLocationCoroutine());
        }

        private System.Collections.IEnumerator WaitForLocationCoroutine()
        {
            int maxWait = 20;
            while (Input.location.status == LocationServiceStatus.Initializing && maxWait > 0)
            {
                yield return new WaitForSeconds(1);
                maxWait--;
            }

            if (Input.location.status == LocationServiceStatus.Running)
            {
                Debug.Log($"📍 Location: {Input.location.lastData.latitude}, {Input.location.lastData.longitude}");
                OnDeviceLocationChanged?.Invoke(new Vector2(
                    Input.location.lastData.latitude,
                    Input.location.lastData.longitude
                ));
            }
        }

        public Vector2 GetCurrentLocation()
        {
            if (Input.location.status != LocationServiceStatus.Running)
                return Vector2.zero;

            return new Vector2(
                Input.location.lastData.latitude,
                Input.location.lastData.longitude
            );
        }

        #endregion

        #region Camera

        private WebCamTexture _cameraTexture;

        public void StartCamera(bool frontFacing = true)
        {
            if (!_hasCamera) return;

            WebCamDevice[] devices = WebCamTexture.devices;
            string deviceName = "";

            foreach (var device in devices)
            {
                if (device.isFrontFacing == frontFacing)
                {
                    deviceName = device.name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(deviceName) && devices.Length > 0)
            {
                deviceName = devices[0].name;
            }

            _cameraTexture = new WebCamTexture(deviceName, 640, 480, 30);
            _cameraTexture.Play();

            Debug.Log($"📷 Camera started: {deviceName}");
        }

        public void StopCamera()
        {
            if (_cameraTexture != null)
            {
                _cameraTexture.Stop();
                Destroy(_cameraTexture);
                _cameraTexture = null;
            }
        }

        public WebCamTexture GetCameraTexture() => _cameraTexture;

        #endregion

        // Public accessors
        public Vector3 GetAcceleration() => _acceleration;
        public Vector3 GetGyroscopeData() => _gyroscope;
        public Vector3 GetTilt() => _tilt;
        public float GetShakeIntensity() => _shakeIntensity;
        public bool HasAccelerometer() => _hasAccelerometer;
        public bool HasGyroscope() => _hasGyroscope;
        public bool HasHaptics() => _hasHaptics;
        public bool HasCamera() => _hasCamera;
        public bool HasGPS() => _hasGPS;
        public bool HasMicrophone() => _hasMicrophone;
    }

    #region Enums

    public enum SliceDirection
    {
        Up,
        Down,
        Left,
        Right,
        Forward,
        Back
    }

    public enum HapticType
    {
        Light,
        Medium,
        Heavy,
        Success,
        Warning,
        Error
    }

    #endregion
}

