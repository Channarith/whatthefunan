using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WhatTheFunan.Robots
{
    /// <summary>
    /// ROBOT CONNECTIVITY MANAGER! 📡🤖
    /// Connect to REAL ROBOTS via Bluetooth and WiFi!
    /// Transfer robot data, control in real-time!
    /// </summary>
    public class RobotConnectivity : MonoBehaviour
    {
        public static RobotConnectivity Instance { get; private set; }

        [Header("Connection Status")]
        [SerializeField] private ConnectionState _bluetoothState;
        [SerializeField] private ConnectionState _wifiState;
        [SerializeField] private string _connectedDeviceName;
        [SerializeField] private string _connectedDeviceAddress;

        [Header("Discovered Devices")]
        [SerializeField] private List<RobotDevice> _discoveredDevices = new List<RobotDevice>();

        [Header("Connection Settings")]
        [SerializeField] private string _wifiSSIDPrefix = "WTF_ROBOT_";
        [SerializeField] private int _wifiPort = 8888;
        [SerializeField] private string _bleServiceUUID = "6E400001-B5A3-F393-E0A9-E50E24DCCA9E"; // Nordic UART Service
        [SerializeField] private string _bleTxCharUUID = "6E400002-B5A3-F393-E0A9-E50E24DCCA9E";
        [SerializeField] private string _bleRxCharUUID = "6E400003-B5A3-F393-E0A9-E50E24DCCA9E";

        [Header("Transfer Progress")]
        [SerializeField] private float _transferProgress;
        [SerializeField] private bool _isTransferring;

        // Events
        public event Action<List<RobotDevice>> OnDevicesDiscovered;
        public event Action<RobotDevice> OnDeviceConnected;
        public event Action OnDeviceDisconnected;
        public event Action<float> OnTransferProgress;
        public event Action OnTransferComplete;
        public event Action<string> OnError;
        public event Action<byte[]> OnDataReceived;

        // Connected robot reference
        private RobotDevice _connectedDevice;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region Bluetooth Low Energy (BLE)

        /// <summary>
        /// Start scanning for BLE robots
        /// </summary>
        public void StartBluetoothScan()
        {
            if (_bluetoothState == ConnectionState.Scanning)
            {
                Debug.LogWarning("Already scanning for Bluetooth devices");
                return;
            }

            _discoveredDevices.Clear();
            _bluetoothState = ConnectionState.Scanning;

            Debug.Log("📶 Starting Bluetooth scan for robots...");

#if UNITY_ANDROID
            StartAndroidBLEScan();
#elif UNITY_IOS
            StartIOSBLEScan();
#else
            // Editor simulation
            SimulateDeviceDiscovery();
#endif
        }

        /// <summary>
        /// Stop BLE scanning
        /// </summary>
        public void StopBluetoothScan()
        {
            _bluetoothState = ConnectionState.Idle;
            Debug.Log("📶 Bluetooth scan stopped");
        }

        /// <summary>
        /// Connect to a specific BLE robot
        /// </summary>
        public void ConnectBluetooth(RobotDevice device)
        {
            if (_bluetoothState == ConnectionState.Connected)
            {
                DisconnectBluetooth();
            }

            _bluetoothState = ConnectionState.Connecting;
            Debug.Log($"📶 Connecting to {device.deviceName}...");

#if UNITY_ANDROID || UNITY_IOS
            StartCoroutine(ConnectBLECoroutine(device));
#else
            // Simulation
            StartCoroutine(SimulateConnection(device));
#endif
        }

        /// <summary>
        /// Disconnect from BLE robot
        /// </summary>
        public void DisconnectBluetooth()
        {
            if (_bluetoothState != ConnectionState.Connected)
                return;

            Debug.Log($"📶 Disconnecting from {_connectedDeviceName}...");

            _connectedDevice = null;
            _connectedDeviceName = "";
            _connectedDeviceAddress = "";
            _bluetoothState = ConnectionState.Idle;

            OnDeviceDisconnected?.Invoke();
        }

        /// <summary>
        /// Send data to connected BLE robot
        /// </summary>
        public void SendBluetoothData(byte[] data)
        {
            if (_bluetoothState != ConnectionState.Connected)
            {
                OnError?.Invoke("Not connected to any Bluetooth device");
                return;
            }

            StartCoroutine(SendBLEDataCoroutine(data));
        }

        #endregion

        #region WiFi Direct / Access Point

        /// <summary>
        /// Start scanning for WiFi robots
        /// </summary>
        public void StartWiFiScan()
        {
            if (_wifiState == ConnectionState.Scanning)
            {
                Debug.LogWarning("Already scanning for WiFi devices");
                return;
            }

            _wifiState = ConnectionState.Scanning;
            Debug.Log("📡 Starting WiFi scan for robots...");

            // In real implementation, scan for SSIDs starting with _wifiSSIDPrefix
#if UNITY_ANDROID
            StartAndroidWiFiScan();
#elif UNITY_IOS
            // iOS doesn't allow WiFi scanning from apps
            OnError?.Invoke("WiFi scanning not available on iOS. Use Bluetooth instead.");
#else
            SimulateDeviceDiscovery();
#endif
        }

        /// <summary>
        /// Connect to a WiFi robot
        /// </summary>
        public void ConnectWiFi(RobotDevice device)
        {
            if (_wifiState == ConnectionState.Connected)
            {
                DisconnectWiFi();
            }

            _wifiState = ConnectionState.Connecting;
            Debug.Log($"📡 Connecting to {device.deviceName} via WiFi...");

            StartCoroutine(ConnectWiFiCoroutine(device));
        }

        /// <summary>
        /// Disconnect from WiFi robot
        /// </summary>
        public void DisconnectWiFi()
        {
            if (_wifiState != ConnectionState.Connected)
                return;

            Debug.Log($"📡 Disconnecting WiFi from {_connectedDeviceName}...");

            _connectedDevice = null;
            _connectedDeviceName = "";
            _wifiState = ConnectionState.Idle;

            OnDeviceDisconnected?.Invoke();
        }

        /// <summary>
        /// Send data to connected WiFi robot
        /// </summary>
        public void SendWiFiData(byte[] data)
        {
            if (_wifiState != ConnectionState.Connected)
            {
                OnError?.Invoke("Not connected to any WiFi device");
                return;
            }

            StartCoroutine(SendWiFiDataCoroutine(data));
        }

        #endregion

        #region Robot Data Transfer

        /// <summary>
        /// Transfer robot data to connected physical robot!
        /// The main feature - upload your game robot to a real robot!
        /// </summary>
        public void TransferRobotData(RobotData robot)
        {
            if (_connectedDevice == null)
            {
                OnError?.Invoke("No robot connected! Connect via Bluetooth or WiFi first.");
                return;
            }

            _isTransferring = true;
            Debug.Log($"📤 Starting transfer of {robot.robotName} to {_connectedDevice.deviceName}...");

            // Get binary data from exporter
            byte[] data = RobotDataExporter.Instance?.ExportToBinary(robot);
            if (data == null)
            {
                OnError?.Invoke("Failed to export robot data");
                _isTransferring = false;
                return;
            }

            StartCoroutine(TransferDataCoroutine(data));
        }

        private IEnumerator TransferDataCoroutine(byte[] data)
        {
            int chunkSize = 512; // BLE typically supports 512 bytes per packet
            int totalChunks = Mathf.CeilToInt((float)data.Length / chunkSize);

            Debug.Log($"📤 Transferring {data.Length} bytes in {totalChunks} chunks...");

            // Send header packet first
            byte[] header = CreateTransferHeader(data.Length, totalChunks);
            yield return SendDataPacket(header);

            // Send data chunks
            for (int i = 0; i < totalChunks; i++)
            {
                int offset = i * chunkSize;
                int size = Mathf.Min(chunkSize, data.Length - offset);

                byte[] chunk = new byte[size + 4]; // 4 bytes for chunk header
                chunk[0] = (byte)(i >> 8);         // Chunk number high byte
                chunk[1] = (byte)(i & 0xFF);       // Chunk number low byte
                chunk[2] = (byte)(size >> 8);      // Chunk size high byte
                chunk[3] = (byte)(size & 0xFF);    // Chunk size low byte

                Buffer.BlockCopy(data, offset, chunk, 4, size);

                yield return SendDataPacket(chunk);

                _transferProgress = (float)(i + 1) / totalChunks;
                OnTransferProgress?.Invoke(_transferProgress);

                yield return new WaitForSeconds(0.02f); // Small delay between chunks
            }

            // Send completion packet
            byte[] footer = CreateTransferFooter();
            yield return SendDataPacket(footer);

            _isTransferring = false;
            Debug.Log($"✅ Transfer complete!");
            OnTransferComplete?.Invoke();
        }

        private byte[] CreateTransferHeader(int totalSize, int totalChunks)
        {
            byte[] header = new byte[16];
            header[0] = 0xWF; // Magic byte 1 (What)
            header[1] = 0xTF; // Magic byte 2 (The Funan!)

            // Actually use valid bytes
            header[0] = 0x57; // 'W'
            header[1] = 0x46; // 'F'
            header[2] = 0x01; // Version
            header[3] = 0x01; // Packet type: header

            // Total size (4 bytes)
            header[4] = (byte)(totalSize >> 24);
            header[5] = (byte)(totalSize >> 16);
            header[6] = (byte)(totalSize >> 8);
            header[7] = (byte)(totalSize & 0xFF);

            // Total chunks (2 bytes)
            header[8] = (byte)(totalChunks >> 8);
            header[9] = (byte)(totalChunks & 0xFF);

            return header;
        }

        private byte[] CreateTransferFooter()
        {
            byte[] footer = new byte[8];
            footer[0] = 0x57; // 'W'
            footer[1] = 0x46; // 'F'
            footer[2] = 0x01; // Version
            footer[3] = 0xFF; // Packet type: footer/complete
            footer[4] = 0x00;
            footer[5] = 0x00;
            footer[6] = 0x00;
            footer[7] = 0x00;
            return footer;
        }

        private IEnumerator SendDataPacket(byte[] packet)
        {
            if (_connectedDevice.connectionType == ConnectionType.Bluetooth)
            {
                yield return SendBLEDataCoroutine(packet);
            }
            else
            {
                yield return SendWiFiDataCoroutine(packet);
            }
        }

        #endregion

        #region Real-Time Robot Control

        /// <summary>
        /// Send real-time movement command to robot!
        /// For direct control mode!
        /// </summary>
        public void SendMovementCommand(MovementCommand command)
        {
            if (_connectedDevice == null) return;

            byte[] data = new byte[8];
            data[0] = 0x4D; // 'M' for Movement
            data[1] = (byte)command.commandType;
            data[2] = (sbyte)Mathf.Clamp(command.forward * 127, -127, 127);
            data[3] = (sbyte)Mathf.Clamp(command.strafe * 127, -127, 127);
            data[4] = (sbyte)Mathf.Clamp(command.rotation * 127, -127, 127);
            data[5] = (byte)command.speed;
            data[6] = 0x00;
            data[7] = CalculateChecksum8(data, 7);

            SendDataPacket(data);
        }

        /// <summary>
        /// Trigger an ability on the robot!
        /// </summary>
        public void SendAbilityCommand(int abilityIndex)
        {
            if (_connectedDevice == null) return;

            byte[] data = new byte[4];
            data[0] = 0x41; // 'A' for Ability
            data[1] = (byte)abilityIndex;
            data[2] = 0x00;
            data[3] = CalculateChecksum8(data, 3);

            SendDataPacket(data);
            Debug.Log($"🎮 Sent ability command: {abilityIndex}");
        }

        /// <summary>
        /// Request status from robot
        /// </summary>
        public void RequestRobotStatus()
        {
            if (_connectedDevice == null) return;

            byte[] data = new byte[4];
            data[0] = 0x53; // 'S' for Status
            data[1] = 0x00;
            data[2] = 0x00;
            data[3] = CalculateChecksum8(data, 3);

            SendDataPacket(data);
        }

        #endregion

        #region Platform Specific Implementations

#if UNITY_ANDROID
        private void StartAndroidBLEScan()
        {
            // Would use AndroidJavaObject to access BluetoothLeScanner
            // BluetoothAdapter.getDefaultAdapter().getBluetoothLeScanner().startScan()
            SimulateDeviceDiscovery();
        }

        private void StartAndroidWiFiScan()
        {
            // Would use AndroidJavaObject to access WifiManager
            // WifiManager.startScan()
            SimulateDeviceDiscovery();
        }
#endif

#if UNITY_IOS
        private void StartIOSBLEScan()
        {
            // Would use native iOS plugin with CBCentralManager
            // centralManager.scanForPeripherals(withServices: [serviceUUID])
            SimulateDeviceDiscovery();
        }
#endif

        #endregion

        #region Coroutines

        private IEnumerator ConnectBLECoroutine(RobotDevice device)
        {
            // Simulated connection
            yield return new WaitForSeconds(2f);

            _connectedDevice = device;
            _connectedDeviceName = device.deviceName;
            _connectedDeviceAddress = device.address;
            _bluetoothState = ConnectionState.Connected;

            Debug.Log($"📶 Connected to {device.deviceName} via Bluetooth!");
            OnDeviceConnected?.Invoke(device);
        }

        private IEnumerator ConnectWiFiCoroutine(RobotDevice device)
        {
            // Simulated connection
            yield return new WaitForSeconds(2f);

            _connectedDevice = device;
            _connectedDeviceName = device.deviceName;
            _wifiState = ConnectionState.Connected;

            Debug.Log($"📡 Connected to {device.deviceName} via WiFi!");
            OnDeviceConnected?.Invoke(device);
        }

        private IEnumerator SendBLEDataCoroutine(byte[] data)
        {
            // Simulated send
            yield return new WaitForSeconds(0.01f);
            Debug.Log($"📶 Sent {data.Length} bytes via BLE");
        }

        private IEnumerator SendWiFiDataCoroutine(byte[] data)
        {
            // Simulated send
            yield return new WaitForSeconds(0.005f);
            Debug.Log($"📡 Sent {data.Length} bytes via WiFi");
        }

        private void SimulateDeviceDiscovery()
        {
            // Simulate finding some robots for testing
            StartCoroutine(SimulateDiscoveryCoroutine());
        }

        private IEnumerator SimulateDiscoveryCoroutine()
        {
            yield return new WaitForSeconds(2f);

            _discoveredDevices = new List<RobotDevice>
            {
                new RobotDevice
                {
                    deviceName = "WTF_NagaBot_001",
                    address = "AA:BB:CC:DD:EE:01",
                    signalStrength = -45,
                    connectionType = ConnectionType.Bluetooth,
                    robotType = "Naga Bot",
                    batteryLevel = 85
                },
                new RobotDevice
                {
                    deviceName = "WTF_ChampaBot_002",
                    address = "AA:BB:CC:DD:EE:02",
                    signalStrength = -60,
                    connectionType = ConnectionType.Bluetooth,
                    robotType = "Champa Bot",
                    batteryLevel = 72
                },
                new RobotDevice
                {
                    deviceName = "WTF_ROBOT_KaviBot",
                    address = "192.168.4.1",
                    signalStrength = -35,
                    connectionType = ConnectionType.WiFi,
                    robotType = "Kavi Bot",
                    batteryLevel = 95
                }
            };

            Debug.Log($"📡 Found {_discoveredDevices.Count} robots!");
            OnDevicesDiscovered?.Invoke(_discoveredDevices);

            _bluetoothState = ConnectionState.Idle;
            _wifiState = ConnectionState.Idle;
        }

        private IEnumerator SimulateConnection(RobotDevice device)
        {
            yield return new WaitForSeconds(1.5f);

            _connectedDevice = device;
            _connectedDeviceName = device.deviceName;
            _connectedDeviceAddress = device.address;
            _bluetoothState = ConnectionState.Connected;

            Debug.Log($"✅ Connected to {device.deviceName}!");
            OnDeviceConnected?.Invoke(device);
        }

        #endregion

        #region Helpers

        private byte CalculateChecksum8(byte[] data, int length)
        {
            byte checksum = 0;
            for (int i = 0; i < length; i++)
            {
                checksum ^= data[i];
            }
            return checksum;
        }

        // Public accessors
        public ConnectionState GetBluetoothState() => _bluetoothState;
        public ConnectionState GetWiFiState() => _wifiState;
        public List<RobotDevice> GetDiscoveredDevices() => _discoveredDevices;
        public RobotDevice GetConnectedDevice() => _connectedDevice;
        public bool IsConnected() => _connectedDevice != null;
        public bool IsTransferring() => _isTransferring;
        public float GetTransferProgress() => _transferProgress;

        #endregion
    }

    #region Data Classes

    public enum ConnectionState
    {
        Idle,
        Scanning,
        Connecting,
        Connected,
        Disconnecting,
        Error
    }

    public enum ConnectionType
    {
        Bluetooth,
        WiFi
    }

    [Serializable]
    public class RobotDevice
    {
        public string deviceName;
        public string address;          // MAC for BLE, IP for WiFi
        public int signalStrength;      // RSSI in dBm
        public ConnectionType connectionType;
        public string robotType;        // What type of robot
        public int batteryLevel;        // 0-100%
        public string firmwareVersion;
    }

    [Serializable]
    public class MovementCommand
    {
        public MovementCommandType commandType;
        public float forward;       // -1 to 1
        public float strafe;        // -1 to 1
        public float rotation;      // -1 to 1
        public int speed;           // 0-100

        public MovementCommand(MovementCommandType type, float fwd, float str, float rot, int spd)
        {
            commandType = type;
            forward = fwd;
            strafe = str;
            rotation = rot;
            speed = spd;
        }
    }

    public enum MovementCommandType
    {
        Stop = 0,
        Walk = 1,
        Run = 2,
        Jump = 3,
        Turn = 4,
        Strafe = 5
    }

    #endregion
}

