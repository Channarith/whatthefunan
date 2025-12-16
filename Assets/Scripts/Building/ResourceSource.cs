using UnityEngine;
using System.Collections;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// A gatherable resource source in the world (trees, rocks, etc.)
    /// </summary>
    public class ResourceSource : MonoBehaviour
    {
        #region Enums
        public enum SourceType
        {
            Tree,
            Rock,
            BambooGrove,
            ClayDeposit,
            PalmTree,
            MineralVein,
            FishingSpot,
            Bush
        }
        #endregion

        #region Settings
        [Header("Resource")]
        [SerializeField] private string _resourceId = "wood";
        [SerializeField] private SourceType _sourceType = SourceType.Tree;
        [SerializeField] private int _resourceAmount = 10;
        [SerializeField] private int _gatherPerHit = 2;
        [SerializeField] private float _gatherCooldown = 0.5f;
        
        [Header("Respawn")]
        [SerializeField] private bool _canRespawn = true;
        [SerializeField] private float _respawnTime = 300f; // 5 minutes
        
        [Header("Visuals")]
        [SerializeField] private GameObject _fullModel;
        [SerializeField] private GameObject _depletedModel;
        [SerializeField] private ParticleSystem _gatherEffect;
        [SerializeField] private float _shakeIntensity = 0.1f;
        [SerializeField] private float _shakeDuration = 0.2f;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _gatherSound;
        [SerializeField] private AudioClip _depletedSound;
        
        [Header("Tool Requirement")]
        [SerializeField] private string _requiredTool = ""; // Empty = bare hands
        [SerializeField] private float _toolSpeedBonus = 1.5f;
        #endregion

        #region State
        private int _currentAmount;
        private float _lastGatherTime;
        private bool _isDepleted;
        private Coroutine _respawnCoroutine;
        private Vector3 _originalPosition;
        private Quaternion _originalRotation;
        
        public string ResourceId => _resourceId;
        public bool IsEmpty => _currentAmount <= 0;
        public int RemainingAmount => _currentAmount;
        public float RespawnProgress => _respawnCoroutine != null ? 
            (Time.time - _lastGatherTime) / _respawnTime : 1f;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _currentAmount = _resourceAmount;
            _originalPosition = transform.position;
            _originalRotation = transform.rotation;
            
            UpdateVisuals();
        }

        private void OnEnable()
        {
            if (_isDepleted && _canRespawn)
            {
                _respawnCoroutine = StartCoroutine(RespawnRoutine());
            }
        }
        #endregion

        #region Gathering
        /// <summary>
        /// Attempt to gather from this source.
        /// </summary>
        public int Gather(string toolId = "")
        {
            if (_isDepleted) return 0;
            
            // Check cooldown
            if (Time.time - _lastGatherTime < _gatherCooldown)
            {
                return 0;
            }
            
            _lastGatherTime = Time.time;
            
            // Calculate gather amount
            int baseGather = _gatherPerHit;
            
            // Tool bonus
            if (!string.IsNullOrEmpty(_requiredTool) && toolId == _requiredTool)
            {
                baseGather = Mathf.CeilToInt(baseGather * _toolSpeedBonus);
            }
            
            // Don't exceed remaining
            int gathered = Mathf.Min(baseGather, _currentAmount);
            _currentAmount -= gathered;
            
            // Effects
            PlayGatherEffect();
            
            // Check depletion
            if (_currentAmount <= 0)
            {
                OnDepleted();
            }
            
            return gathered;
        }

        /// <summary>
        /// Gather all remaining at once.
        /// </summary>
        public int GatherAll()
        {
            if (_isDepleted) return 0;
            
            int gathered = _currentAmount;
            _currentAmount = 0;
            
            PlayGatherEffect();
            OnDepleted();
            
            return gathered;
        }

        private void OnDepleted()
        {
            _isDepleted = true;
            
            // Play depleted sound
            if (_depletedSound != null)
            {
                Core.AudioManager.Instance?.PlaySFX(_depletedSound);
            }
            
            UpdateVisuals();
            
            // Start respawn
            if (_canRespawn)
            {
                _respawnCoroutine = StartCoroutine(RespawnRoutine());
            }
            
            Debug.Log($"[ResourceSource] {gameObject.name} depleted");
        }
        #endregion

        #region Effects
        private void PlayGatherEffect()
        {
            // Particle effect
            if (_gatherEffect != null)
            {
                _gatherEffect.Play();
            }
            
            // Sound
            if (_gatherSound != null)
            {
                Core.AudioManager.Instance?.PlaySFX(_gatherSound);
            }
            
            // Haptic
            Core.HapticManager.Instance?.TriggerLight();
            
            // Shake
            StartCoroutine(ShakeRoutine());
        }

        private IEnumerator ShakeRoutine()
        {
            float elapsed = 0f;
            
            while (elapsed < _shakeDuration)
            {
                elapsed += Time.deltaTime;
                
                float intensity = _shakeIntensity * (1f - elapsed / _shakeDuration);
                Vector3 offset = new Vector3(
                    Random.Range(-intensity, intensity),
                    0,
                    Random.Range(-intensity, intensity)
                );
                
                transform.position = _originalPosition + offset;
                
                yield return null;
            }
            
            transform.position = _originalPosition;
        }
        #endregion

        #region Respawn
        private IEnumerator RespawnRoutine()
        {
            yield return new WaitForSeconds(_respawnTime);
            
            Respawn();
        }

        private void Respawn()
        {
            _currentAmount = _resourceAmount;
            _isDepleted = false;
            _respawnCoroutine = null;
            
            // Play respawn effect
            if (_gatherEffect != null)
            {
                _gatherEffect.Play();
            }
            
            UpdateVisuals();
            
            Debug.Log($"[ResourceSource] {gameObject.name} respawned");
        }

        /// <summary>
        /// Force respawn immediately.
        /// </summary>
        public void ForceRespawn()
        {
            if (_respawnCoroutine != null)
            {
                StopCoroutine(_respawnCoroutine);
                _respawnCoroutine = null;
            }
            
            Respawn();
        }
        #endregion

        #region Visuals
        private void UpdateVisuals()
        {
            if (_fullModel != null)
            {
                _fullModel.SetActive(!_isDepleted);
            }
            
            if (_depletedModel != null)
            {
                _depletedModel.SetActive(_isDepleted);
            }
        }
        #endregion

        #region Interaction
        /// <summary>
        /// Get interaction prompt text.
        /// </summary>
        public string GetInteractionPrompt()
        {
            if (_isDepleted)
            {
                return $"(Respawning in {Mathf.CeilToInt(_respawnTime - (Time.time - _lastGatherTime))}s)";
            }
            
            string resourceName = ResourceManager.Instance?.GetDefinition(_resourceId)?.displayName ?? _resourceId;
            return $"Gather {resourceName} ({_currentAmount} left)";
        }

        /// <summary>
        /// Check if player can interact.
        /// </summary>
        public bool CanInteract()
        {
            return !_isDepleted && Time.time - _lastGatherTime >= _gatherCooldown;
        }
        #endregion

        #region Editor
        private void OnDrawGizmosSelected()
        {
            // Show interaction radius
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, 2f);
        }
        #endregion
    }
}

