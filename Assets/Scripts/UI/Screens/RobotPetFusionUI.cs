using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using WhatTheFunan.Robots;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// ROBOT-PET FUSION UI! 🤖🐾
    /// Mix and match interface for creating powerful combinations!
    /// </summary>
    public class RobotPetFusionUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject _fusionPanel;
        [SerializeField] private GameObject _selectionPanel;
        [SerializeField] private GameObject _abilitySelectionPanel;
        [SerializeField] private GameObject _resultPanel;

        [Header("Robot Selection")]
        [SerializeField] private Transform _robotListContainer;
        [SerializeField] private GameObject _robotCardPrefab;
        [SerializeField] private Image _selectedRobotPreview;
        [SerializeField] private Text _selectedRobotName;
        [SerializeField] private Text _selectedRobotStats;

        [Header("Pet Selection")]
        [SerializeField] private Transform _petListContainer;
        [SerializeField] private GameObject _petCardPrefab;
        [SerializeField] private Image _selectedPetPreview;
        [SerializeField] private Text _selectedPetName;
        [SerializeField] private Text _selectedPetStats;

        [Header("Synergy Preview")]
        [SerializeField] private Transform _synergyListContainer;
        [SerializeField] private GameObject _synergyCardPrefab;
        [SerializeField] private Text _synergyLevelText;
        [SerializeField] private Image _synergyMeter;

        [Header("Ability Selection")]
        [SerializeField] private Transform _robotAbilityContainer;
        [SerializeField] private Transform _petAbilityContainer;
        [SerializeField] private Transform _selectedAbilityContainer;
        [SerializeField] private GameObject _abilityTogglePrefab;
        [SerializeField] private Text _abilityCountText;

        [Header("Fusion Result")]
        [SerializeField] private Text _fusionNameText;
        [SerializeField] private Text _fusedStatsText;
        [SerializeField] private Transform _fusionAbilityContainer;
        [SerializeField] private GameObject _fusionAbilityPrefab;
        [SerializeField] private Image _fusionPreviewImage;

        [Header("Buttons")]
        [SerializeField] private Button _previewSynergyButton;
        [SerializeField] private Button _selectAbilitiesButton;
        [SerializeField] private Button _createFusionButton;
        [SerializeField] private Button _clearFusionButton;
        [SerializeField] private Button _useFusionButton;

        [Header("Colors")]
        [SerializeField] private Color _synergyGoodColor = Color.green;
        [SerializeField] private Color _synergyGreatColor = Color.cyan;
        [SerializeField] private Color _synergyPerfectColor = Color.magenta;
        [SerializeField] private Color _synergyLegendaryColor = Color.yellow;

        // State
        private RobotData _selectedRobot;
        private PetData _selectedPet;
        private List<string> _selectedRobotAbilities = new List<string>();
        private List<string> _selectedPetAbilities = new List<string>();
        private FusedUnit _currentFusion;

        private void Start()
        {
            SetupEventListeners();
            ShowSelectionPanel();
        }

        private void SetupEventListeners()
        {
            if (_previewSynergyButton != null)
                _previewSynergyButton.onClick.AddListener(PreviewSynergies);

            if (_selectAbilitiesButton != null)
                _selectAbilitiesButton.onClick.AddListener(ShowAbilitySelection);

            if (_createFusionButton != null)
                _createFusionButton.onClick.AddListener(CreateFusion);

            if (_clearFusionButton != null)
                _clearFusionButton.onClick.AddListener(ClearSelection);

            if (_useFusionButton != null)
                _useFusionButton.onClick.AddListener(UseFusion);

            // Subscribe to fusion events
            if (RobotPetFusion.Instance != null)
            {
                RobotPetFusion.Instance.OnFusionCreated += OnFusionCreated;
                RobotPetFusion.Instance.OnSynergyActivated += OnSynergyActivated;
                RobotPetFusion.Instance.OnFusionAbilityUnlocked += OnFusionAbilityUnlocked;
            }
        }

        #region Panel Navigation

        public void ShowSelectionPanel()
        {
            HideAllPanels();
            if (_selectionPanel != null) _selectionPanel.SetActive(true);
            PopulateRobotList();
            PopulatePetList();
        }

        public void ShowAbilitySelection()
        {
            if (_selectedRobot == null || _selectedPet == null)
            {
                Debug.LogWarning("Select both robot and pet first!");
                return;
            }

            HideAllPanels();
            if (_abilitySelectionPanel != null) _abilitySelectionPanel.SetActive(true);
            PopulateAbilitySelection();
        }

        public void ShowResultPanel()
        {
            HideAllPanels();
            if (_resultPanel != null) _resultPanel.SetActive(true);
        }

        private void HideAllPanels()
        {
            if (_selectionPanel != null) _selectionPanel.SetActive(false);
            if (_abilitySelectionPanel != null) _abilitySelectionPanel.SetActive(false);
            if (_resultPanel != null) _resultPanel.SetActive(false);
        }

        #endregion

        #region Robot Selection

        private void PopulateRobotList()
        {
            if (_robotListContainer == null) return;

            // Clear existing
            foreach (Transform child in _robotListContainer)
            {
                Destroy(child.gameObject);
            }

            var templates = RobotBuilder.Instance?.GetTemplates();
            if (templates == null) return;

            foreach (var template in templates)
            {
                CreateRobotCard(template);
            }
        }

        private void CreateRobotCard(RobotTemplate template)
        {
            // In actual implementation, instantiate prefab
            Debug.Log($"🤖 Robot card: {template.templateName}");
        }

        public void OnRobotSelected(RobotData robot)
        {
            _selectedRobot = robot;

            if (_selectedRobotName != null)
                _selectedRobotName.text = robot.robotName;

            if (_selectedRobotStats != null)
            {
                var stats = robot.coreStats;
                _selectedRobotStats.text = $"⚡{stats.power} 💨{stats.speed} 🛡️{stats.defense}\n" +
                                          $"🧠{stats.intelligence} 🔋{stats.energy} 🎯{stats.precision}";
            }

            // Update synergy preview if pet is also selected
            if (_selectedPet != null)
            {
                PreviewSynergies();
            }

            // Highlight recommended pets
            HighlightRecommendedPets();

            Debug.Log($"🤖 Selected robot: {robot.robotName}");
        }

        #endregion

        #region Pet Selection

        private void PopulatePetList()
        {
            if (_petListContainer == null) return;

            // Clear existing
            foreach (Transform child in _petListContainer)
            {
                Destroy(child.gameObject);
            }

            var pets = PetDatabase.Instance?.GetAllPets();
            if (pets == null) return;

            foreach (var pet in pets)
            {
                CreatePetCard(pet);
            }
        }

        private void CreatePetCard(PetData pet)
        {
            // In actual implementation, instantiate prefab
            Debug.Log($"🐾 Pet card: {pet.petName} ({pet.element})");
        }

        public void OnPetSelected(PetData pet)
        {
            _selectedPet = pet;

            if (_selectedPetName != null)
                _selectedPetName.text = pet.petName;

            if (_selectedPetStats != null)
            {
                var stats = pet.stats;
                _selectedPetStats.text = $"⚡{stats.power} 💨{stats.speed} 🛡️{stats.defense}\n" +
                                        $"🧠{stats.intelligence} 🔋{stats.energy} 🎯{stats.precision}\n" +
                                        $"Element: {pet.element} | Role: {pet.role}";
            }

            // Update synergy preview if robot is also selected
            if (_selectedRobot != null)
            {
                PreviewSynergies();
            }

            Debug.Log($"🐾 Selected pet: {pet.petName}");
        }

        private void HighlightRecommendedPets()
        {
            if (_selectedRobot == null) return;

            var recommended = PetDatabase.Instance?.GetRecommendedPetsForRobot(_selectedRobot);
            // In actual implementation, highlight these pets in the list
            Debug.Log($"✨ {recommended?.Count ?? 0} recommended pets for {_selectedRobot.robotName}");
        }

        #endregion

        #region Synergy Preview

        private void PreviewSynergies()
        {
            if (_selectedRobot == null || _selectedPet == null) return;

            // Clear existing synergy cards
            if (_synergyListContainer != null)
            {
                foreach (Transform child in _synergyListContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            var synergies = RobotPetFusion.Instance?.GetPossibleSynergies(_selectedRobot, _selectedPet);
            if (synergies == null || synergies.Count == 0)
            {
                if (_synergyLevelText != null)
                    _synergyLevelText.text = "❌ No synergy detected\nTry a different combination!";
                if (_synergyMeter != null)
                    _synergyMeter.fillAmount = 0;
                return;
            }

            // Find highest synergy level
            SynergyLevel highestLevel = SynergyLevel.None;
            foreach (var synergy in synergies)
            {
                if (synergy.synergyLevel > highestLevel)
                    highestLevel = synergy.synergyLevel;

                CreateSynergyCard(synergy);
            }

            UpdateSynergyDisplay(highestLevel, synergies.Count);
        }

        private void CreateSynergyCard(SynergyBonus synergy)
        {
            // In actual implementation, instantiate prefab
            Debug.Log($"✨ Synergy: {synergy.synergyName} ({synergy.synergyLevel})");
            Debug.Log($"   Unlocks: {synergy.unlocksFusionAbility}");
        }

        private void UpdateSynergyDisplay(SynergyLevel level, int count)
        {
            if (_synergyLevelText != null)
            {
                string stars = level switch
                {
                    SynergyLevel.Good => "⭐",
                    SynergyLevel.Great => "⭐⭐",
                    SynergyLevel.Perfect => "⭐⭐⭐",
                    SynergyLevel.Legendary => "🌟🌟🌟🌟",
                    _ => "❌"
                };

                string levelName = level switch
                {
                    SynergyLevel.Good => "Good Synergy",
                    SynergyLevel.Great => "Great Synergy!",
                    SynergyLevel.Perfect => "PERFECT SYNERGY!",
                    SynergyLevel.Legendary => "✨ LEGENDARY SYNERGY! ✨",
                    _ => "No Synergy"
                };

                _synergyLevelText.text = $"{stars}\n{levelName}\n({count} bonuses active)";
            }

            if (_synergyMeter != null)
            {
                _synergyMeter.fillAmount = (int)level / 4f;
                _synergyMeter.color = level switch
                {
                    SynergyLevel.Good => _synergyGoodColor,
                    SynergyLevel.Great => _synergyGreatColor,
                    SynergyLevel.Perfect => _synergyPerfectColor,
                    SynergyLevel.Legendary => _synergyLegendaryColor,
                    _ => Color.gray
                };
            }
        }

        #endregion

        #region Ability Selection

        private void PopulateAbilitySelection()
        {
            _selectedRobotAbilities.Clear();
            _selectedPetAbilities.Clear();

            // Populate robot abilities
            if (_robotAbilityContainer != null)
            {
                foreach (Transform child in _robotAbilityContainer)
                {
                    Destroy(child.gameObject);
                }

                foreach (var ability in _selectedRobot.abilities)
                {
                    CreateAbilityToggle(ability.abilityId, ability.abilityName, true);
                }
            }

            // Populate pet abilities
            if (_petAbilityContainer != null)
            {
                foreach (Transform child in _petAbilityContainer)
                {
                    Destroy(child.gameObject);
                }

                foreach (var ability in _selectedPet.abilities)
                {
                    CreateAbilityToggle(ability.abilityId, ability.abilityName, false);
                }
            }

            UpdateAbilityCount();
        }

        private void CreateAbilityToggle(string abilityId, string abilityName, bool isRobot)
        {
            // In actual implementation, instantiate toggle prefab
            Debug.Log($"📜 Ability: {abilityName} (from {(isRobot ? "Robot" : "Pet")})");

            // Auto-select first few abilities
            if (isRobot && _selectedRobotAbilities.Count < 3)
            {
                _selectedRobotAbilities.Add(abilityId);
            }
            else if (!isRobot && _selectedPetAbilities.Count < 3)
            {
                _selectedPetAbilities.Add(abilityId);
            }
        }

        public void OnAbilityToggled(string abilityId, bool isRobot, bool isSelected)
        {
            var list = isRobot ? _selectedRobotAbilities : _selectedPetAbilities;

            if (isSelected)
            {
                if (!list.Contains(abilityId))
                    list.Add(abilityId);
            }
            else
            {
                list.Remove(abilityId);
            }

            UpdateAbilityCount();
        }

        private void UpdateAbilityCount()
        {
            int total = _selectedRobotAbilities.Count + _selectedPetAbilities.Count;
            int max = 6;

            if (_abilityCountText != null)
            {
                _abilityCountText.text = $"Selected: {total}/{max}";
                _abilityCountText.color = total > max ? Color.red : Color.white;
            }

            if (_createFusionButton != null)
            {
                _createFusionButton.interactable = total > 0 && total <= max;
            }
        }

        #endregion

        #region Fusion Creation

        private void CreateFusion()
        {
            if (_selectedRobot == null || _selectedPet == null)
            {
                Debug.LogWarning("Select both robot and pet first!");
                return;
            }

            _currentFusion = RobotPetFusion.Instance?.CreateFusion(
                _selectedRobot,
                _selectedPet,
                _selectedRobotAbilities,
                _selectedPetAbilities
            );
        }

        private void OnFusionCreated(FusedUnit fusion)
        {
            _currentFusion = fusion;
            ShowResultPanel();
            DisplayFusionResult(fusion);
        }

        private void DisplayFusionResult(FusedUnit fusion)
        {
            if (_fusionNameText != null)
                _fusionNameText.text = $"🤖🐾 {fusion.fusionName}";

            if (_fusedStatsText != null)
            {
                var stats = fusion.fusedStats;
                _fusedStatsText.text = $"⚡ Power: {stats.power}\n" +
                                       $"💨 Speed: {stats.speed}\n" +
                                       $"🛡️ Defense: {stats.defense}\n" +
                                       $"🧠 Intelligence: {stats.intelligence}\n" +
                                       $"🔋 Energy: {stats.energy}\n" +
                                       $"🎯 Precision: {stats.precision}\n" +
                                       $"\n💥 Crit Chance: {stats.criticalChance}%\n" +
                                       $"💥 Crit Damage: {stats.criticalDamage}%\n" +
                                       $"💨 Evasion: {stats.evasion}%";
            }

            // Display abilities
            if (_fusionAbilityContainer != null)
            {
                foreach (Transform child in _fusionAbilityContainer)
                {
                    Destroy(child.gameObject);
                }

                // Regular abilities
                foreach (var ability in fusion.selectedAbilities)
                {
                    CreateFusionAbilityDisplay(ability);
                }

                // Fusion abilities (special!)
                foreach (var fusionAbility in fusion.fusionAbilities)
                {
                    CreateFusionAbilityDisplay(fusionAbility);
                }
            }

            // Display synergies
            if (fusion.activeSynergies.Count > 0)
            {
                Debug.Log($"🌟 Active synergies:");
                foreach (var synergy in fusion.activeSynergies)
                {
                    Debug.Log($"   - {synergy.synergyName}");
                }
            }
        }

        private void CreateFusionAbilityDisplay(FusedAbility ability)
        {
            string sourceIcon = ability.source switch
            {
                AbilitySource.Robot => "🤖",
                AbilitySource.Pet => "🐾",
                _ => "⚡"
            };

            Debug.Log($"   {sourceIcon} {ability.abilityName} - DMG: {ability.damage}, Cost: {ability.energyCost}");
        }

        private void CreateFusionAbilityDisplay(FusionAbility ability)
        {
            Debug.Log($"   🌟 FUSION: {ability.abilityName} - DMG: {ability.damage}!");
            Debug.Log($"      {ability.description}");
        }

        private void OnSynergyActivated(SynergyBonus synergy)
        {
            Debug.Log($"✨ Synergy activated: {synergy.synergyName}");
        }

        private void OnFusionAbilityUnlocked(FusionAbility ability)
        {
            Debug.Log($"🌟 FUSION ABILITY UNLOCKED: {ability.abilityName}!");
        }

        #endregion

        #region Actions

        private void ClearSelection()
        {
            _selectedRobot = null;
            _selectedPet = null;
            _selectedRobotAbilities.Clear();
            _selectedPetAbilities.Clear();
            _currentFusion = null;

            if (_selectedRobotName != null) _selectedRobotName.text = "Select a Robot";
            if (_selectedPetName != null) _selectedPetName.text = "Select a Pet";
            if (_selectedRobotStats != null) _selectedRobotStats.text = "";
            if (_selectedPetStats != null) _selectedPetStats.text = "";
            if (_synergyLevelText != null) _synergyLevelText.text = "";
            if (_synergyMeter != null) _synergyMeter.fillAmount = 0;

            ShowSelectionPanel();
            Debug.Log("🔓 Selection cleared");
        }

        private void UseFusion()
        {
            if (_currentFusion == null)
            {
                Debug.LogWarning("No fusion created!");
                return;
            }

            // In actual implementation, apply fusion to battle/gameplay
            Debug.Log($"🎮 Using fusion: {_currentFusion.fusionName}");
            Debug.Log($"   Total abilities: {_currentFusion.selectedAbilities.Count + _currentFusion.fusionAbilities.Count}");
        }

        #endregion
    }
}

