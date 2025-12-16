using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using System.Collections.Generic;

namespace WhatTheFunan.Building
{
    /// <summary>
    /// AI controller for kingdom villagers.
    /// Villagers wander, work, and interact with the kingdom.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class VillagerAI : MonoBehaviour
    {
        #region Enums
        public enum VillagerState
        {
            Idle,
            Walking,
            Working,
            Eating,
            Sleeping,
            Dancing,
            Talking
        }
        #endregion

        #region Settings
        [Header("Identity")]
        [SerializeField] private string _villagerId;
        [SerializeField] private string _villagerName;
        [SerializeField] private string _species = "Elephant";
        
        [Header("Movement")]
        [SerializeField] private float _walkSpeed = 2f;
        [SerializeField] private float _runSpeed = 4f;
        [SerializeField] private float _wanderRadius = 15f;
        [SerializeField] private float _minWaitTime = 3f;
        [SerializeField] private float _maxWaitTime = 10f;
        
        [Header("Work")]
        [SerializeField] private VillagerJob _job = VillagerJob.Idle;
        [SerializeField] private Transform _workStation;
        [SerializeField] private float _workDuration = 30f;
        
        [Header("Schedule")]
        [SerializeField] private int _wakeHour = 6;
        [SerializeField] private int _sleepHour = 20;
        [SerializeField] private int _workStartHour = 8;
        [SerializeField] private int _workEndHour = 17;
        
        [Header("Visuals")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _headBone;
        [SerializeField] private GameObject _sleepZzzEffect;
        [SerializeField] private GameObject _happyEffect;
        [SerializeField] private GameObject _workEffect;
        
        [Header("Speech Bubbles")]
        [SerializeField] private Transform _speechBubbleAnchor;
        [SerializeField] private float _speechBubbleDuration = 3f;
        #endregion

        #region Components
        private NavMeshAgent _agent;
        private VillagerState _currentState = VillagerState.Idle;
        private Coroutine _stateCoroutine;
        private Transform _home;
        private List<Transform> _interestPoints = new List<Transform>();
        #endregion

        #region State
        private float _happiness = 50f;
        private bool _isBeingPetted;
        private float _lastInteractionTime;
        private int _currentGameHour;
        
        public string VillagerId => _villagerId;
        public string VillagerName => _villagerName;
        public VillagerState CurrentState => _currentState;
        public float Happiness => _happiness;
        public VillagerJob Job => _job;
        #endregion

        #region Unity Lifecycle
        private void Awake()
        {
            _agent = GetComponent<NavMeshAgent>();
            _agent.speed = _walkSpeed;
            
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
        }

        private void Start()
        {
            // Find interest points
            FindInterestPoints();
            
            // Start behavior
            StartCoroutine(BehaviorLoop());
        }

        private void Update()
        {
            UpdateAnimations();
            UpdateHappiness();
            
            // Get current game hour
            if (Gameplay.WeatherSystem.Instance != null)
            {
                _currentGameHour = Mathf.FloorToInt(Gameplay.WeatherSystem.Instance.CurrentTimeNormalized * 24);
            }
        }

        private void OnDestroy()
        {
            if (_stateCoroutine != null)
                StopCoroutine(_stateCoroutine);
        }
        #endregion

        #region Setup
        /// <summary>
        /// Initialize villager with data.
        /// </summary>
        public void Initialize(VillagerData data, Transform home)
        {
            _villagerId = data.id;
            _villagerName = data.name;
            _species = data.species;
            _job = data.job;
            _happiness = data.happiness;
            _home = home;
        }

        private void FindInterestPoints()
        {
            // Find places villagers like to visit
            var decorations = FindObjectsOfType<BuildableObject>();
            foreach (var obj in decorations)
            {
                if (obj.Category == BuildableObject.BuildCategory.Decorations ||
                    obj.Category == BuildableObject.BuildCategory.Functional)
                {
                    _interestPoints.Add(obj.transform);
                }
            }
        }
        #endregion

        #region Behavior Loop
        private IEnumerator BehaviorLoop()
        {
            while (true)
            {
                // Determine what to do based on time and job
                VillagerState nextState = DetermineNextState();
                
                if (nextState != _currentState)
                {
                    if (_stateCoroutine != null)
                        StopCoroutine(_stateCoroutine);
                    
                    _stateCoroutine = StartCoroutine(ExecuteState(nextState));
                }
                
                yield return new WaitForSeconds(1f);
            }
        }

        private VillagerState DetermineNextState()
        {
            // Night time = sleep
            if (_currentGameHour >= _sleepHour || _currentGameHour < _wakeHour)
            {
                return VillagerState.Sleeping;
            }
            
            // Work hours
            if (_job != VillagerJob.Idle && 
                _currentGameHour >= _workStartHour && 
                _currentGameHour < _workEndHour)
            {
                return VillagerState.Working;
            }
            
            // Random activities during free time
            float rand = Random.value;
            
            if (rand < 0.3f && _interestPoints.Count > 0)
            {
                return VillagerState.Walking;
            }
            else if (rand < 0.4f && _happiness > 60)
            {
                return VillagerState.Dancing;
            }
            else if (rand < 0.5f)
            {
                return VillagerState.Talking;
            }
            
            return VillagerState.Idle;
        }

        private IEnumerator ExecuteState(VillagerState state)
        {
            _currentState = state;
            
            switch (state)
            {
                case VillagerState.Idle:
                    yield return IdleState();
                    break;
                case VillagerState.Walking:
                    yield return WalkingState();
                    break;
                case VillagerState.Working:
                    yield return WorkingState();
                    break;
                case VillagerState.Sleeping:
                    yield return SleepingState();
                    break;
                case VillagerState.Dancing:
                    yield return DancingState();
                    break;
                case VillagerState.Talking:
                    yield return TalkingState();
                    break;
            }
            
            _currentState = VillagerState.Idle;
        }
        #endregion

        #region States
        private IEnumerator IdleState()
        {
            _agent.isStopped = true;
            
            // Random idle animations
            if (_animator != null)
            {
                _animator.SetTrigger("Idle");
            }
            
            float waitTime = Random.Range(_minWaitTime, _maxWaitTime);
            yield return new WaitForSeconds(waitTime);
        }

        private IEnumerator WalkingState()
        {
            _agent.isStopped = false;
            _agent.speed = _walkSpeed;
            
            // Pick destination
            Vector3 destination;
            
            if (_interestPoints.Count > 0 && Random.value < 0.7f)
            {
                // Go to an interest point
                Transform target = _interestPoints[Random.Range(0, _interestPoints.Count)];
                destination = target.position;
            }
            else
            {
                // Random wander
                destination = GetRandomNavMeshPoint(_wanderRadius);
            }
            
            _agent.SetDestination(destination);
            
            // Wait until arrived
            while (!_agent.pathPending && _agent.remainingDistance > 0.5f)
            {
                yield return null;
            }
            
            // Pause at destination
            yield return new WaitForSeconds(Random.Range(2f, 5f));
        }

        private IEnumerator WorkingState()
        {
            // Go to work station
            if (_workStation != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_workStation.position);
                
                while (_agent.remainingDistance > 1f)
                {
                    yield return null;
                }
            }
            
            _agent.isStopped = true;
            
            // Work animation
            if (_animator != null)
            {
                _animator.SetTrigger("Work");
            }
            
            // Show work effect
            if (_workEffect != null)
            {
                _workEffect.SetActive(true);
            }
            
            // Generate resources based on job
            float workTimer = 0;
            while (workTimer < _workDuration)
            {
                workTimer += Time.deltaTime;
                
                // Periodic resource generation
                if (workTimer % 10 < Time.deltaTime)
                {
                    GenerateWorkOutput();
                }
                
                yield return null;
            }
            
            if (_workEffect != null)
            {
                _workEffect.SetActive(false);
            }
        }

        private IEnumerator SleepingState()
        {
            // Go home
            if (_home != null)
            {
                _agent.isStopped = false;
                _agent.SetDestination(_home.position);
                
                while (_agent.remainingDistance > 1f)
                {
                    yield return null;
                }
            }
            
            _agent.isStopped = true;
            
            // Sleep animation
            if (_animator != null)
            {
                _animator.SetTrigger("Sleep");
            }
            
            // Show zzz
            if (_sleepZzzEffect != null)
            {
                _sleepZzzEffect.SetActive(true);
            }
            
            // Wait until morning
            while (_currentGameHour >= _sleepHour || _currentGameHour < _wakeHour)
            {
                yield return new WaitForSeconds(1f);
            }
            
            if (_sleepZzzEffect != null)
            {
                _sleepZzzEffect.SetActive(false);
            }
        }

        private IEnumerator DancingState()
        {
            _agent.isStopped = true;
            
            if (_animator != null)
            {
                _animator.SetTrigger("Dance");
            }
            
            // Show happy effect
            if (_happyEffect != null)
            {
                _happyEffect.SetActive(true);
            }
            
            yield return new WaitForSeconds(Random.Range(5f, 15f));
            
            if (_happyEffect != null)
            {
                _happyEffect.SetActive(false);
            }
        }

        private IEnumerator TalkingState()
        {
            // Find nearby villager
            var nearby = FindNearbyVillager();
            
            if (nearby == null)
            {
                yield break;
            }
            
            // Walk towards them
            _agent.isStopped = false;
            _agent.SetDestination(nearby.transform.position);
            
            while (_agent.remainingDistance > 2f)
            {
                yield return null;
            }
            
            _agent.isStopped = true;
            
            // Face each other
            transform.LookAt(nearby.transform);
            
            // Chat animation
            if (_animator != null)
            {
                _animator.SetTrigger("Talk");
            }
            
            // Show speech bubble
            ShowSpeechBubble();
            
            yield return new WaitForSeconds(Random.Range(3f, 8f));
        }
        #endregion

        #region Work Output
        private void GenerateWorkOutput()
        {
            switch (_job)
            {
                case VillagerJob.Farmer:
                    ResourceManager.Instance?.AddResource("food", 1);
                    break;
                case VillagerJob.Builder:
                    // Speeds up construction
                    break;
                case VillagerJob.Crafter:
                    // Chance to craft items
                    break;
                case VillagerJob.Guard:
                    // Increases kingdom defense
                    break;
                case VillagerJob.Merchant:
                    // Generates coins
                    Economy.CurrencyManager.Instance?.AddCoins(1);
                    break;
                case VillagerJob.Entertainer:
                    // Increases kingdom happiness
                    break;
            }
        }
        #endregion

        #region Navigation
        private Vector3 GetRandomNavMeshPoint(float radius)
        {
            Vector3 randomDirection = Random.insideUnitSphere * radius;
            randomDirection += transform.position;
            
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDirection, out hit, radius, NavMesh.AllAreas))
            {
                return hit.position;
            }
            
            return transform.position;
        }

        private VillagerAI FindNearbyVillager()
        {
            var villagers = FindObjectsOfType<VillagerAI>();
            
            foreach (var v in villagers)
            {
                if (v != this && Vector3.Distance(transform.position, v.transform.position) < 10f)
                {
                    return v;
                }
            }
            
            return null;
        }
        #endregion

        #region Animation
        private void UpdateAnimations()
        {
            if (_animator == null) return;
            
            // Movement blend
            float speed = _agent.velocity.magnitude / _walkSpeed;
            _animator.SetFloat("Speed", speed);
            
            // State
            _animator.SetInteger("State", (int)_currentState);
        }
        #endregion

        #region Happiness
        private void UpdateHappiness()
        {
            // Happiness slowly trends toward kingdom happiness
            float kingdomHappiness = KingdomManager.Instance?.Happiness ?? 50;
            _happiness = Mathf.Lerp(_happiness, kingdomHappiness, Time.deltaTime * 0.01f);
        }

        /// <summary>
        /// Pet the villager to increase happiness.
        /// </summary>
        public void Pet()
        {
            if (Time.time - _lastInteractionTime < 5f) return;
            
            _lastInteractionTime = Time.time;
            _happiness = Mathf.Min(100, _happiness + 10);
            
            // Happy reaction
            if (_animator != null)
            {
                _animator.SetTrigger("Happy");
            }
            
            if (_happyEffect != null)
            {
                StartCoroutine(ShowEffectBriefly(_happyEffect, 2f));
            }
            
            Core.HapticManager.Instance?.TriggerLight();
            Core.AudioManager.Instance?.PlaySFX("sfx_villager_happy");
        }

        private IEnumerator ShowEffectBriefly(GameObject effect, float duration)
        {
            effect.SetActive(true);
            yield return new WaitForSeconds(duration);
            effect.SetActive(false);
        }
        #endregion

        #region Speech
        private void ShowSpeechBubble()
        {
            // Would show UI speech bubble with random dialogue
            string[] dialogues = {
                "What a lovely day!",
                "I love this kingdom!",
                "Time to work!",
                "Have you seen the temple?",
                "The Naga Prince visited!",
                "*happy noises*"
            };
            
            string dialogue = dialogues[Random.Range(0, dialogues.Length)];
            Debug.Log($"[{_villagerName}]: {dialogue}");
        }
        #endregion

        #region Interaction
        /// <summary>
        /// Player tapped on villager.
        /// </summary>
        public void OnPlayerInteract()
        {
            Pet();
            
            // Show villager info popup
            UI.UIManager.Instance?.ShowVillagerInfo(this);
        }
        #endregion
    }
}

