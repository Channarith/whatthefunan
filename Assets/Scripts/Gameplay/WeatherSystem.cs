using UnityEngine;
using System;
using System.Collections;

namespace WhatTheFunan.Gameplay
{
    /// <summary>
    /// Day/Night cycle and weather system for immersive environments.
    /// Affects gameplay, visibility, and spawns.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        #region Singleton
        private static WeatherSystem _instance;
        public static WeatherSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<TimeOfDay> OnTimeOfDayChanged;
        public static event Action<WeatherType> OnWeatherChanged;
        public static event Action OnSunrise;
        public static event Action OnSunset;
        #endregion

        #region Time Settings
        [Header("Day/Night Cycle")]
        [SerializeField] private float _dayDurationMinutes = 20f;
        [SerializeField] private bool _enableCycle = true;
        [SerializeField] private float _startTime = 8f; // 8 AM
        
        [Header("Time Thresholds")]
        [SerializeField] private float _sunriseHour = 6f;
        [SerializeField] private float _morningHour = 9f;
        [SerializeField] private float _noonHour = 12f;
        [SerializeField] private float _afternoonHour = 15f;
        [SerializeField] private float _eveningHour = 18f;
        [SerializeField] private float _sunsetHour = 19f;
        [SerializeField] private float _nightHour = 21f;
        #endregion

        #region Lighting
        [Header("Lighting")]
        [SerializeField] private Light _sunLight;
        [SerializeField] private Gradient _sunColor;
        [SerializeField] private AnimationCurve _sunIntensity;
        [SerializeField] private AnimationCurve _sunAngle;
        [SerializeField] private Material _skyboxMaterial;
        #endregion

        #region Weather Settings
        [Header("Weather")]
        [SerializeField] private WeatherType _currentWeather = WeatherType.Clear;
        [SerializeField] private float _minWeatherDuration = 180f; // 3 minutes
        [SerializeField] private float _maxWeatherDuration = 600f; // 10 minutes
        [SerializeField] private bool _randomizeWeather = true;
        
        [Header("Weather Effects")]
        [SerializeField] private ParticleSystem _rainEffect;
        [SerializeField] private ParticleSystem _heavyRainEffect;
        [SerializeField] private ParticleSystem _fogEffect;
        [SerializeField] private ParticleSystem _stormEffect;
        #endregion

        #region State
        private float _currentHour;
        private TimeOfDay _currentTimeOfDay;
        private float _weatherTimer;
        private bool _wasDay = true;
        
        public float CurrentHour => _currentHour;
        public TimeOfDay CurrentTimeOfDay => _currentTimeOfDay;
        public WeatherType CurrentWeather => _currentWeather;
        public bool IsDay => _currentHour >= _sunriseHour && _currentHour < _sunsetHour;
        public bool IsNight => !IsDay;
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
            
            _currentHour = _startTime;
            UpdateTimeOfDay();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            if (_enableCycle)
            {
                UpdateTime();
                UpdateLighting();
            }
            
            if (_randomizeWeather)
            {
                UpdateWeatherTimer();
            }
        }
        #endregion

        #region Time Management
        private void UpdateTime()
        {
            // Calculate hours per real second
            float hoursPerSecond = 24f / (_dayDurationMinutes * 60f);
            _currentHour += hoursPerSecond * Time.deltaTime;
            
            if (_currentHour >= 24f)
            {
                _currentHour -= 24f;
            }
            
            UpdateTimeOfDay();
            CheckDayNightTransition();
        }

        private void UpdateTimeOfDay()
        {
            TimeOfDay newTimeOfDay;
            
            if (_currentHour < _sunriseHour)
            {
                newTimeOfDay = TimeOfDay.Night;
            }
            else if (_currentHour < _morningHour)
            {
                newTimeOfDay = TimeOfDay.Dawn;
            }
            else if (_currentHour < _noonHour)
            {
                newTimeOfDay = TimeOfDay.Morning;
            }
            else if (_currentHour < _afternoonHour)
            {
                newTimeOfDay = TimeOfDay.Noon;
            }
            else if (_currentHour < _eveningHour)
            {
                newTimeOfDay = TimeOfDay.Afternoon;
            }
            else if (_currentHour < _sunsetHour)
            {
                newTimeOfDay = TimeOfDay.Evening;
            }
            else if (_currentHour < _nightHour)
            {
                newTimeOfDay = TimeOfDay.Dusk;
            }
            else
            {
                newTimeOfDay = TimeOfDay.Night;
            }
            
            if (newTimeOfDay != _currentTimeOfDay)
            {
                _currentTimeOfDay = newTimeOfDay;
                OnTimeOfDayChanged?.Invoke(_currentTimeOfDay);
            }
        }

        private void CheckDayNightTransition()
        {
            bool isCurrentlyDay = IsDay;
            
            if (!_wasDay && isCurrentlyDay)
            {
                OnSunrise?.Invoke();
                Debug.Log("[WeatherSystem] Sunrise");
            }
            else if (_wasDay && !isCurrentlyDay)
            {
                OnSunset?.Invoke();
                Debug.Log("[WeatherSystem] Sunset");
            }
            
            _wasDay = isCurrentlyDay;
        }

        /// <summary>
        /// Set the time of day directly.
        /// </summary>
        public void SetTime(float hour)
        {
            _currentHour = Mathf.Clamp(hour, 0f, 24f);
            UpdateTimeOfDay();
            UpdateLighting();
        }

        /// <summary>
        /// Set time to a specific time of day.
        /// </summary>
        public void SetTimeOfDay(TimeOfDay time)
        {
            switch (time)
            {
                case TimeOfDay.Dawn:
                    SetTime(_sunriseHour);
                    break;
                case TimeOfDay.Morning:
                    SetTime(_morningHour);
                    break;
                case TimeOfDay.Noon:
                    SetTime(_noonHour);
                    break;
                case TimeOfDay.Afternoon:
                    SetTime(_afternoonHour);
                    break;
                case TimeOfDay.Evening:
                    SetTime(_eveningHour);
                    break;
                case TimeOfDay.Dusk:
                    SetTime(_sunsetHour);
                    break;
                case TimeOfDay.Night:
                    SetTime(_nightHour);
                    break;
            }
        }
        #endregion

        #region Lighting
        private void UpdateLighting()
        {
            if (_sunLight == null) return;
            
            float normalizedTime = _currentHour / 24f;
            
            // Update sun color
            if (_sunColor != null)
            {
                _sunLight.color = _sunColor.Evaluate(normalizedTime);
            }
            
            // Update sun intensity
            if (_sunIntensity != null)
            {
                _sunLight.intensity = _sunIntensity.Evaluate(normalizedTime);
            }
            
            // Update sun angle
            if (_sunAngle != null)
            {
                float angle = _sunAngle.Evaluate(normalizedTime);
                _sunLight.transform.rotation = Quaternion.Euler(angle, 170f, 0f);
            }
            
            // Update skybox
            if (_skyboxMaterial != null)
            {
                _skyboxMaterial.SetFloat("_AtmosphereThickness", IsDay ? 1f : 0.5f);
            }
        }
        #endregion

        #region Weather
        private void UpdateWeatherTimer()
        {
            _weatherTimer -= Time.deltaTime;
            
            if (_weatherTimer <= 0)
            {
                ChangeToRandomWeather();
                _weatherTimer = UnityEngine.Random.Range(_minWeatherDuration, _maxWeatherDuration);
            }
        }

        private void ChangeToRandomWeather()
        {
            // Weight towards clear weather
            float roll = UnityEngine.Random.value;
            
            WeatherType newWeather;
            if (roll < 0.5f)
            {
                newWeather = WeatherType.Clear;
            }
            else if (roll < 0.7f)
            {
                newWeather = WeatherType.Cloudy;
            }
            else if (roll < 0.85f)
            {
                newWeather = WeatherType.Rain;
            }
            else if (roll < 0.95f)
            {
                newWeather = WeatherType.HeavyRain;
            }
            else
            {
                newWeather = WeatherType.Storm;
            }
            
            SetWeather(newWeather);
        }

        /// <summary>
        /// Set the weather type.
        /// </summary>
        public void SetWeather(WeatherType weather)
        {
            if (weather == _currentWeather) return;
            
            _currentWeather = weather;
            
            // Disable all effects first
            if (_rainEffect != null) _rainEffect.Stop();
            if (_heavyRainEffect != null) _heavyRainEffect.Stop();
            if (_fogEffect != null) _fogEffect.Stop();
            if (_stormEffect != null) _stormEffect.Stop();
            
            // Enable appropriate effect
            switch (weather)
            {
                case WeatherType.Clear:
                    // No effects
                    break;
                case WeatherType.Cloudy:
                    // Just reduce sun intensity
                    break;
                case WeatherType.Fog:
                    if (_fogEffect != null) _fogEffect.Play();
                    break;
                case WeatherType.Rain:
                    if (_rainEffect != null) _rainEffect.Play();
                    break;
                case WeatherType.HeavyRain:
                    if (_heavyRainEffect != null) _heavyRainEffect.Play();
                    break;
                case WeatherType.Storm:
                    if (_stormEffect != null) _stormEffect.Play();
                    if (_heavyRainEffect != null) _heavyRainEffect.Play();
                    break;
            }
            
            OnWeatherChanged?.Invoke(weather);
            Debug.Log($"[WeatherSystem] Weather changed to: {weather}");
        }
        #endregion

        #region Query
        /// <summary>
        /// Get formatted time string (e.g., "14:30").
        /// </summary>
        public string GetTimeString()
        {
            int hours = Mathf.FloorToInt(_currentHour);
            int minutes = Mathf.FloorToInt((_currentHour - hours) * 60);
            return $"{hours:D2}:{minutes:D2}";
        }

        /// <summary>
        /// Check if current weather is rainy.
        /// </summary>
        public bool IsRaining()
        {
            return _currentWeather == WeatherType.Rain || 
                   _currentWeather == WeatherType.HeavyRain ||
                   _currentWeather == WeatherType.Storm;
        }
        #endregion
    }

    #region Enums
    public enum TimeOfDay
    {
        Dawn,
        Morning,
        Noon,
        Afternoon,
        Evening,
        Dusk,
        Night
    }

    public enum WeatherType
    {
        Clear,
        Cloudy,
        Fog,
        Rain,
        HeavyRain,
        Storm
    }
    #endregion
}

