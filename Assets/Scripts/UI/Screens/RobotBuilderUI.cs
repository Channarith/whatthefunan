using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using WhatTheFunan.Robots;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// ROBOT BUILDER UI! 🔧🤖
    /// Beautiful interface for building and customizing robots!
    /// </summary>
    public class RobotBuilderUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject _mainPanel;
        [SerializeField] private GameObject _templatePanel;
        [SerializeField] private GameObject _customizePanel;
        [SerializeField] private GameObject _aiConfigPanel;
        [SerializeField] private GameObject _abilitiesPanel;
        [SerializeField] private GameObject _visualsPanel;
        [SerializeField] private GameObject _exportPanel;
        [SerializeField] private GameObject _connectPanel;

        [Header("Template Selection")]
        [SerializeField] private Transform _templateContainer;
        [SerializeField] private GameObject _templateCardPrefab;

        [Header("Stats Customization")]
        [SerializeField] private Slider _powerSlider;
        [SerializeField] private Slider _speedSlider;
        [SerializeField] private Slider _defenseSlider;
        [SerializeField] private Slider _intelligenceSlider;
        [SerializeField] private Slider _energySlider;
        [SerializeField] private Slider _precisionSlider;
        [SerializeField] private Text _totalPointsText;
        [SerializeField] private Text _remainingPointsText;

        [Header("AI Configuration")]
        [SerializeField] private Dropdown _primaryStyleDropdown;
        [SerializeField] private Dropdown _secondaryStyleDropdown;
        [SerializeField] private Slider _aggressionSlider;
        [SerializeField] private Slider _cautionSlider;
        [SerializeField] private Slider _adaptabilitySlider;

        [Header("Visual Customization")]
        [SerializeField] private Image _primaryColorPreview;
        [SerializeField] private Image _secondaryColorPreview;
        [SerializeField] private Image _accentColorPreview;
        [SerializeField] private Toggle _nagaCrestToggle;
        [SerializeField] private Toggle _ankorPatternsToggle;
        [SerializeField] private Toggle _celestialGlowToggle;

        [Header("Robot Preview")]
        [SerializeField] private RawImage _robotPreview;
        [SerializeField] private Text _robotNameText;
        [SerializeField] private Text _robotStatsText;

        [Header("Connection Panel")]
        [SerializeField] private Transform _deviceListContainer;
        [SerializeField] private GameObject _deviceCardPrefab;
        [SerializeField] private Text _connectionStatusText;
        [SerializeField] private Slider _transferProgressBar;
        [SerializeField] private Text _transferProgressText;
        [SerializeField] private Button _scanButton;
        [SerializeField] private Button _transferButton;

        [Header("Export Options")]
        [SerializeField] private Button _exportJSONButton;
        [SerializeField] private Button _exportBinaryButton;
        [SerializeField] private Button _exportArduinoButton;
        [SerializeField] private Button _exportPythonButton;
        [SerializeField] private Text _exportStatusText;

        private RobotData _currentRobot;
        private int _maxStatPoints = 300;

        private void Start()
        {
            SetupEventListeners();
            PopulateDropdowns();
            ShowTemplateSelection();
        }

        private void SetupEventListeners()
        {
            // Stats sliders
            if (_powerSlider != null) _powerSlider.onValueChanged.AddListener(OnStatChanged);
            if (_speedSlider != null) _speedSlider.onValueChanged.AddListener(OnStatChanged);
            if (_defenseSlider != null) _defenseSlider.onValueChanged.AddListener(OnStatChanged);
            if (_intelligenceSlider != null) _intelligenceSlider.onValueChanged.AddListener(OnStatChanged);
            if (_energySlider != null) _energySlider.onValueChanged.AddListener(OnStatChanged);
            if (_precisionSlider != null) _precisionSlider.onValueChanged.AddListener(OnStatChanged);

            // AI sliders
            if (_aggressionSlider != null) _aggressionSlider.onValueChanged.AddListener(OnAIConfigChanged);
            if (_cautionSlider != null) _cautionSlider.onValueChanged.AddListener(OnAIConfigChanged);
            if (_adaptabilitySlider != null) _adaptabilitySlider.onValueChanged.AddListener(OnAIConfigChanged);

            // Connectivity events
            if (RobotConnectivity.Instance != null)
            {
                RobotConnectivity.Instance.OnDevicesDiscovered += OnDevicesDiscovered;
                RobotConnectivity.Instance.OnDeviceConnected += OnDeviceConnected;
                RobotConnectivity.Instance.OnDeviceDisconnected += OnDeviceDisconnected;
                RobotConnectivity.Instance.OnTransferProgress += OnTransferProgress;
                RobotConnectivity.Instance.OnTransferComplete += OnTransferComplete;
            }
        }

        private void PopulateDropdowns()
        {
            if (_primaryStyleDropdown != null)
            {
                _primaryStyleDropdown.ClearOptions();
                var styles = new List<string>
                {
                    "⚔️ Aggressive - Rush down offense!",
                    "🛡️ Defensive - Block and counter!",
                    "⚖️ Balanced - Mix of both!",
                    "🎯 Technical - Combo master!",
                    "💢 Berserker - High risk, high reward!",
                    "🧠 Tactical - Exploit weaknesses!",
                    "💨 Evasive - Dodge everything!",
                    "🏰 Tank - Unstoppable force!",
                    "🗡️ Assassin - Quick kills!",
                    "💚 Support - Buff and heal!"
                };
                _primaryStyleDropdown.AddOptions(styles);
            }
        }

        #region Panel Navigation

        public void ShowTemplateSelection()
        {
            HideAllPanels();
            if (_templatePanel != null) _templatePanel.SetActive(true);
            PopulateTemplates();
        }

        public void ShowCustomization()
        {
            HideAllPanels();
            if (_customizePanel != null) _customizePanel.SetActive(true);
            UpdateStatsUI();
        }

        public void ShowAIConfig()
        {
            HideAllPanels();
            if (_aiConfigPanel != null) _aiConfigPanel.SetActive(true);
        }

        public void ShowAbilities()
        {
            HideAllPanels();
            if (_abilitiesPanel != null) _abilitiesPanel.SetActive(true);
        }

        public void ShowVisuals()
        {
            HideAllPanels();
            if (_visualsPanel != null) _visualsPanel.SetActive(true);
        }

        public void ShowExport()
        {
            HideAllPanels();
            if (_exportPanel != null) _exportPanel.SetActive(true);
        }

        public void ShowConnect()
        {
            HideAllPanels();
            if (_connectPanel != null) _connectPanel.SetActive(true);
        }

        private void HideAllPanels()
        {
            if (_templatePanel != null) _templatePanel.SetActive(false);
            if (_customizePanel != null) _customizePanel.SetActive(false);
            if (_aiConfigPanel != null) _aiConfigPanel.SetActive(false);
            if (_abilitiesPanel != null) _abilitiesPanel.SetActive(false);
            if (_visualsPanel != null) _visualsPanel.SetActive(false);
            if (_exportPanel != null) _exportPanel.SetActive(false);
            if (_connectPanel != null) _connectPanel.SetActive(false);
        }

        #endregion

        #region Template Selection

        private void PopulateTemplates()
        {
            if (_templateContainer == null || _templateCardPrefab == null) return;

            // Clear existing
            foreach (Transform child in _templateContainer)
            {
                Destroy(child.gameObject);
            }

            var templates = RobotBuilder.Instance?.GetTemplates();
            if (templates == null) return;

            foreach (var template in templates)
            {
                // Create template card
                // In actual implementation, instantiate prefab and set data
                Debug.Log($"🤖 Template: {template.templateName}");
            }
        }

        public void OnTemplateSelected(string templateId)
        {
            _currentRobot = RobotBuilder.Instance?.CreateFromTemplate(
                templateId,
                "My Robot",
                "player_001"
            );

            if (_currentRobot != null)
            {
                UpdateRobotPreview();
                ShowCustomization();
            }
        }

        public void OnCreateCustomRobot()
        {
            _currentRobot = RobotBuilder.Instance?.CreateNewRobot("Custom Robot", "player_001");
            if (_currentRobot != null)
            {
                UpdateRobotPreview();
                ShowCustomization();
            }
        }

        #endregion

        #region Stats Customization

        private void OnStatChanged(float value)
        {
            if (_currentRobot == null) return;

            int power = Mathf.RoundToInt(_powerSlider?.value ?? 50);
            int speed = Mathf.RoundToInt(_speedSlider?.value ?? 50);
            int defense = Mathf.RoundToInt(_defenseSlider?.value ?? 50);
            int intelligence = Mathf.RoundToInt(_intelligenceSlider?.value ?? 50);
            int energy = Mathf.RoundToInt(_energySlider?.value ?? 50);
            int precision = Mathf.RoundToInt(_precisionSlider?.value ?? 50);

            int total = power + speed + defense + intelligence + energy + precision;

            if (total <= _maxStatPoints)
            {
                RobotBuilder.Instance?.SetStats(power, speed, defense, intelligence, energy, precision);
            }

            UpdateStatsUI();
            UpdateRobotPreview();
        }

        private void UpdateStatsUI()
        {
            if (_currentRobot == null) return;

            int total = _currentRobot.coreStats.GetTotalStatPoints();
            int remaining = _maxStatPoints - total;

            if (_totalPointsText != null)
                _totalPointsText.text = $"Used: {total}/{_maxStatPoints}";

            if (_remainingPointsText != null)
            {
                _remainingPointsText.text = $"Remaining: {remaining}";
                _remainingPointsText.color = remaining < 0 ? Color.red : Color.white;
            }
        }

        #endregion

        #region AI Configuration

        private void OnAIConfigChanged(float value)
        {
            if (_currentRobot == null) return;

            int aggression = Mathf.RoundToInt(_aggressionSlider?.value ?? 50);
            int caution = Mathf.RoundToInt(_cautionSlider?.value ?? 50);
            int adaptability = Mathf.RoundToInt(_adaptabilitySlider?.value ?? 50);

            RobotBuilder.Instance?.SetAIBehavior(aggression, caution, adaptability);
        }

        public void OnPrimaryStyleChanged()
        {
            if (_currentRobot == null || _primaryStyleDropdown == null) return;

            int styleIndex = _primaryStyleDropdown.value;
            FightingStyle primary = (FightingStyle)styleIndex;
            FightingStyle secondary = _currentRobot.aiConfig.secondaryStyle;

            RobotBuilder.Instance?.SetFightingStyle(primary, secondary);
        }

        #endregion

        #region Connectivity

        public void OnScanButtonClicked()
        {
            if (_connectionStatusText != null)
                _connectionStatusText.text = "🔍 Scanning for robots...";

            RobotConnectivity.Instance?.StartBluetoothScan();
            RobotConnectivity.Instance?.StartWiFiScan();
        }

        private void OnDevicesDiscovered(List<RobotDevice> devices)
        {
            if (_connectionStatusText != null)
                _connectionStatusText.text = $"📡 Found {devices.Count} robots!";

            PopulateDeviceList(devices);
        }

        private void PopulateDeviceList(List<RobotDevice> devices)
        {
            if (_deviceListContainer == null) return;

            foreach (Transform child in _deviceListContainer)
            {
                Destroy(child.gameObject);
            }

            foreach (var device in devices)
            {
                // Create device card
                // In actual implementation, instantiate prefab
                Debug.Log($"📱 Device: {device.deviceName} ({device.robotType}) - {device.batteryLevel}%");
            }
        }

        public void OnConnectToDevice(RobotDevice device)
        {
            if (_connectionStatusText != null)
                _connectionStatusText.text = $"🔗 Connecting to {device.deviceName}...";

            if (device.connectionType == ConnectionType.Bluetooth)
            {
                RobotConnectivity.Instance?.ConnectBluetooth(device);
            }
            else
            {
                RobotConnectivity.Instance?.ConnectWiFi(device);
            }
        }

        private void OnDeviceConnected(RobotDevice device)
        {
            if (_connectionStatusText != null)
                _connectionStatusText.text = $"✅ Connected to {device.deviceName}!";

            if (_transferButton != null)
                _transferButton.interactable = true;
        }

        private void OnDeviceDisconnected()
        {
            if (_connectionStatusText != null)
                _connectionStatusText.text = "❌ Disconnected";

            if (_transferButton != null)
                _transferButton.interactable = false;
        }

        public void OnTransferButtonClicked()
        {
            if (_currentRobot == null)
            {
                if (_connectionStatusText != null)
                    _connectionStatusText.text = "⚠️ No robot to transfer!";
                return;
            }

            if (_connectionStatusText != null)
                _connectionStatusText.text = "📤 Transferring robot data...";

            if (_transferProgressBar != null)
            {
                _transferProgressBar.gameObject.SetActive(true);
                _transferProgressBar.value = 0;
            }

            RobotConnectivity.Instance?.TransferRobotData(_currentRobot);
        }

        private void OnTransferProgress(float progress)
        {
            if (_transferProgressBar != null)
                _transferProgressBar.value = progress;

            if (_transferProgressText != null)
                _transferProgressText.text = $"{Mathf.RoundToInt(progress * 100)}%";
        }

        private void OnTransferComplete()
        {
            if (_connectionStatusText != null)
                _connectionStatusText.text = "✅ Transfer complete! Robot is ready!";

            if (_transferProgressBar != null)
            {
                _transferProgressBar.value = 1f;
            }
        }

        #endregion

        #region Export

        public void OnExportJSONClicked()
        {
            if (_currentRobot == null) return;

            string path = RobotDataExporter.Instance?.SaveJSONToFile(_currentRobot);
            if (_exportStatusText != null)
                _exportStatusText.text = $"✅ Exported to: {path}";
        }

        public void OnExportBinaryClicked()
        {
            if (_currentRobot == null) return;

            string path = RobotDataExporter.Instance?.SaveBinaryToFile(_currentRobot);
            if (_exportStatusText != null)
                _exportStatusText.text = $"✅ Exported binary to: {path}";
        }

        public void OnExportArduinoClicked()
        {
            if (_currentRobot == null) return;

            string header = RobotDataExporter.Instance?.ExportToArduinoHeader(_currentRobot);
            if (_exportStatusText != null)
                _exportStatusText.text = "✅ Arduino header generated!";

            Debug.Log(header);
        }

        public void OnExportPythonClicked()
        {
            if (_currentRobot == null) return;

            string python = RobotDataExporter.Instance?.ExportToMicroPython(_currentRobot);
            if (_exportStatusText != null)
                _exportStatusText.text = "✅ MicroPython code generated!";

            Debug.Log(python);
        }

        #endregion

        private void UpdateRobotPreview()
        {
            if (_currentRobot == null) return;

            if (_robotNameText != null)
                _robotNameText.text = _currentRobot.robotName;

            if (_robotStatsText != null)
            {
                var stats = _currentRobot.coreStats;
                _robotStatsText.text = $"⚡ Power: {stats.power}\n" +
                                      $"💨 Speed: {stats.speed}\n" +
                                      $"🛡️ Defense: {stats.defense}\n" +
                                      $"🧠 Intelligence: {stats.intelligence}\n" +
                                      $"🔋 Energy: {stats.energy}\n" +
                                      $"🎯 Precision: {stats.precision}\n" +
                                      $"\n❤️ HP: {stats.healthPoints}\n" +
                                      $"⚡ Energy Cap: {stats.energyCapacity}";
            }
        }
    }
}

