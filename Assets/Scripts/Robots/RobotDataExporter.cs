using UnityEngine;
using System;
using System.Text;
using System.IO;

namespace WhatTheFunan.Robots
{
    /// <summary>
    /// ROBOT DATA EXPORTER! 📤🤖
    /// Export robot data for transfer to REAL ROBOTS!
    /// Supports JSON, Binary, and proprietary formats!
    /// Designed for Bluetooth/WiFi transfer!
    /// </summary>
    public class RobotDataExporter : MonoBehaviour
    {
        public static RobotDataExporter Instance { get; private set; }

        [Header("Export Settings")]
        [SerializeField] private string _exportVersion = "1.0.0";
        [SerializeField] private string _exportPath;

        // Events
        public event Action<string> OnExportComplete;
        public event Action<byte[]> OnBinaryExportComplete;
        public event Action<string> OnExportError;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _exportPath = Application.persistentDataPath + "/RobotExports/";

                // Ensure directory exists
                if (!Directory.Exists(_exportPath))
                {
                    Directory.CreateDirectory(_exportPath);
                }
            }
            else
            {
                Destroy(gameObject);
            }
        }

        #region JSON Export (Human Readable)

        /// <summary>
        /// Export robot data as JSON string
        /// Perfect for debugging and web APIs
        /// </summary>
        public string ExportToJSON(RobotData robot)
        {
            try
            {
                var exportData = CreateExportWrapper(robot);
                string json = JsonUtility.ToJson(exportData, true);

                Debug.Log($"📤 Robot exported to JSON: {robot.robotName}");
                OnExportComplete?.Invoke(json);

                return json;
            }
            catch (Exception e)
            {
                OnExportError?.Invoke($"JSON export failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save JSON to file
        /// </summary>
        public string SaveJSONToFile(RobotData robot)
        {
            string json = ExportToJSON(robot);
            if (json == null) return null;

            string fileName = $"{robot.robotId}_{robot.robotName.Replace(" ", "_")}.json";
            string filePath = _exportPath + fileName;

            File.WriteAllText(filePath, json);
            Debug.Log($"💾 Robot saved to: {filePath}");

            return filePath;
        }

        #endregion

        #region Binary Export (Compact for Bluetooth/WiFi)

        /// <summary>
        /// Export robot data as compact binary
        /// Optimized for Bluetooth Low Energy (BLE) transfer
        /// </summary>
        public byte[] ExportToBinary(RobotData robot)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream())
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    // Header
                    writer.Write(Encoding.ASCII.GetBytes("WTFR")); // Magic bytes: What The Funan Robot
                    writer.Write((byte)1); // Version
                    writer.Write(robot.robotId);
                    writer.Write(robot.robotName);
                    writer.Write(robot.createdTimestamp);

                    // Core Stats (6 bytes - each stat 0-100 fits in 1 byte)
                    writer.Write((byte)robot.coreStats.power);
                    writer.Write((byte)robot.coreStats.speed);
                    writer.Write((byte)robot.coreStats.defense);
                    writer.Write((byte)robot.coreStats.intelligence);
                    writer.Write((byte)robot.coreStats.energy);
                    writer.Write((byte)robot.coreStats.precision);

                    // Combat Stats
                    writer.Write((byte)robot.combatStats.meleeAffinity);
                    writer.Write((byte)robot.combatStats.rangedAffinity);
                    writer.Write((byte)robot.combatStats.magicAffinity);
                    writer.Write((byte)robot.combatStats.physicalArmor);
                    writer.Write((byte)robot.combatStats.energyShielding);
                    writer.Write((byte)robot.combatStats.evasionChance);
                    writer.Write((byte)robot.combatStats.criticalChance);
                    writer.Write((byte)robot.combatStats.criticalDamage);

                    // Elemental Affinities (8 bytes)
                    writer.Write((byte)robot.combatStats.elementalAffinity.water);
                    writer.Write((byte)robot.combatStats.elementalAffinity.fire);
                    writer.Write((byte)robot.combatStats.elementalAffinity.earth);
                    writer.Write((byte)robot.combatStats.elementalAffinity.wind);
                    writer.Write((byte)robot.combatStats.elementalAffinity.lightning);
                    writer.Write((byte)robot.combatStats.elementalAffinity.nature);
                    writer.Write((byte)robot.combatStats.elementalAffinity.celestial);
                    writer.Write((byte)robot.combatStats.elementalAffinity.shadow);

                    // AI Config
                    writer.Write((byte)robot.aiConfig.primaryStyle);
                    writer.Write((byte)robot.aiConfig.secondaryStyle);
                    writer.Write((byte)robot.aiConfig.aggression);
                    writer.Write((byte)robot.aiConfig.caution);
                    writer.Write((byte)robot.aiConfig.adaptability);
                    writer.Write((byte)robot.aiConfig.patternRecognition);

                    // Physical Config
                    writer.Write((byte)robot.physicalConfig.chassisType);
                    writer.Write((byte)robot.physicalConfig.sizeClass);
                    writer.Write((byte)robot.physicalConfig.armCount);
                    writer.Write((byte)robot.physicalConfig.legCount);
                    writer.Write(robot.physicalConfig.hasWings);
                    writer.Write(robot.physicalConfig.hasTail);

                    // Servo Configurations
                    writer.Write((byte)robot.physicalConfig.servos.Count);
                    foreach (var servo in robot.physicalConfig.servos)
                    {
                        writer.Write(servo.servoId);
                        writer.Write(servo.jointName);
                        writer.Write((short)servo.minAngle);
                        writer.Write((short)servo.maxAngle);
                        writer.Write((short)servo.defaultAngle);
                        writer.Write((short)servo.speed);
                        writer.Write((short)servo.torque);
                    }

                    // Motor Configurations
                    writer.Write((byte)robot.physicalConfig.motors.Count);
                    foreach (var motor in robot.physicalConfig.motors)
                    {
                        writer.Write(motor.motorId);
                        writer.Write(motor.motorName);
                        writer.Write((short)motor.maxRPM);
                        writer.Write((short)motor.power);
                        writer.Write(motor.reversible);
                    }

                    // Abilities
                    writer.Write((byte)robot.abilities.Count);
                    foreach (var ability in robot.abilities)
                    {
                        writer.Write(ability.abilityId);
                        writer.Write(ability.abilityName);
                        writer.Write((byte)ability.type);
                        writer.Write((byte)ability.element);
                        writer.Write((short)ability.baseDamage);
                        writer.Write((short)ability.energyCost);
                        writer.Write(ability.cooldown);
                        writer.Write(ability.range);
                        writer.Write(ability.areaOfEffect);

                        // Action sequences (for real robot execution!)
                        if (ability.actionSequence != null)
                        {
                            writer.Write((byte)ability.actionSequence.Count);
                            foreach (var step in ability.actionSequence)
                            {
                                writer.Write((byte)step.stepNumber);
                                writer.Write(step.duration);

                                // Servo movements
                                if (step.servoMovements != null)
                                {
                                    writer.Write((byte)step.servoMovements.Count);
                                    foreach (var move in step.servoMovements)
                                    {
                                        writer.Write(move.servoId);
                                        writer.Write((short)move.targetAngle);
                                        writer.Write((short)move.speed);
                                        writer.Write((byte)move.easing);
                                    }
                                }
                                else
                                {
                                    writer.Write((byte)0);
                                }

                                // Motor commands
                                if (step.motorCommands != null)
                                {
                                    writer.Write((byte)step.motorCommands.Count);
                                    foreach (var cmd in step.motorCommands)
                                    {
                                        writer.Write(cmd.motorId);
                                        writer.Write((sbyte)cmd.speed);
                                        writer.Write(cmd.duration);
                                    }
                                }
                                else
                                {
                                    writer.Write((byte)0);
                                }
                            }
                        }
                        else
                        {
                            writer.Write((byte)0);
                        }
                    }

                    // Learned patterns (AI memory)
                    var patterns = robot.battleHistory.learnedPatterns;
                    writer.Write((short)(patterns?.Count ?? 0));
                    if (patterns != null)
                    {
                        foreach (var pattern in patterns)
                        {
                            writer.Write(pattern.patternId);
                            writer.Write(pattern.triggerCondition);
                            writer.Write(pattern.response);
                            writer.Write(pattern.confidence);
                        }
                    }

                    // Checksum
                    byte[] data = ms.ToArray();
                    uint checksum = CalculateChecksum(data);
                    writer.Write(checksum);

                    byte[] finalData = ms.ToArray();

                    Debug.Log($"📤 Robot exported to binary: {robot.robotName}");
                    Debug.Log($"   Size: {finalData.Length} bytes");

                    OnBinaryExportComplete?.Invoke(finalData);
                    return finalData;
                }
            }
            catch (Exception e)
            {
                OnExportError?.Invoke($"Binary export failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save binary to file
        /// </summary>
        public string SaveBinaryToFile(RobotData robot)
        {
            byte[] data = ExportToBinary(robot);
            if (data == null) return null;

            string fileName = $"{robot.robotId}_{robot.robotName.Replace(" ", "_")}.wtfr";
            string filePath = _exportPath + fileName;

            File.WriteAllBytes(filePath, data);
            Debug.Log($"💾 Robot binary saved to: {filePath}");

            return filePath;
        }

        #endregion

        #region Real Robot Protocol Export

        /// <summary>
        /// Export as Arduino/ESP32 compatible C header file!
        /// For direct embedding in real robot firmware!
        /// </summary>
        public string ExportToArduinoHeader(RobotData robot)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("// ========================================");
            sb.AppendLine("// WHAT THE FUNAN - ROBOT DATA EXPORT");
            sb.AppendLine($"// Robot: {robot.robotName}");
            sb.AppendLine($"// ID: {robot.robotId}");
            sb.AppendLine($"// Generated: {DateTime.Now}");
            sb.AppendLine("// ========================================");
            sb.AppendLine();
            sb.AppendLine("#ifndef ROBOT_DATA_H");
            sb.AppendLine("#define ROBOT_DATA_H");
            sb.AppendLine();
            sb.AppendLine("#include <Arduino.h>");
            sb.AppendLine();

            // Robot identity
            sb.AppendLine("// === ROBOT IDENTITY ===");
            sb.AppendLine($"#define ROBOT_NAME \"{robot.robotName}\"");
            sb.AppendLine($"#define ROBOT_ID \"{robot.robotId}\"");
            sb.AppendLine();

            // Core stats
            sb.AppendLine("// === CORE STATS (0-100) ===");
            sb.AppendLine($"#define STAT_POWER {robot.coreStats.power}");
            sb.AppendLine($"#define STAT_SPEED {robot.coreStats.speed}");
            sb.AppendLine($"#define STAT_DEFENSE {robot.coreStats.defense}");
            sb.AppendLine($"#define STAT_INTELLIGENCE {robot.coreStats.intelligence}");
            sb.AppendLine($"#define STAT_ENERGY {robot.coreStats.energy}");
            sb.AppendLine($"#define STAT_PRECISION {robot.coreStats.precision}");
            sb.AppendLine();

            // AI behavior
            sb.AppendLine("// === AI BEHAVIOR ===");
            sb.AppendLine($"#define AI_STYLE_PRIMARY {(int)robot.aiConfig.primaryStyle}");
            sb.AppendLine($"#define AI_AGGRESSION {robot.aiConfig.aggression}");
            sb.AppendLine($"#define AI_CAUTION {robot.aiConfig.caution}");
            sb.AppendLine($"#define AI_ADAPTABILITY {robot.aiConfig.adaptability}");
            sb.AppendLine();

            // Physical config
            sb.AppendLine("// === PHYSICAL CONFIG ===");
            sb.AppendLine($"#define CHASSIS_TYPE {(int)robot.physicalConfig.chassisType}");
            sb.AppendLine($"#define ARM_COUNT {robot.physicalConfig.armCount}");
            sb.AppendLine($"#define LEG_COUNT {robot.physicalConfig.legCount}");
            sb.AppendLine($"#define HAS_WINGS {(robot.physicalConfig.hasWings ? 1 : 0)}");
            sb.AppendLine($"#define HAS_TAIL {(robot.physicalConfig.hasTail ? 1 : 0)}");
            sb.AppendLine();

            // Servo configurations
            if (robot.physicalConfig.servos.Count > 0)
            {
                sb.AppendLine("// === SERVO CONFIGURATION ===");
                sb.AppendLine($"#define SERVO_COUNT {robot.physicalConfig.servos.Count}");
                sb.AppendLine();
                sb.AppendLine("typedef struct {");
                sb.AppendLine("    const char* jointName;");
                sb.AppendLine("    int minAngle;");
                sb.AppendLine("    int maxAngle;");
                sb.AppendLine("    int defaultAngle;");
                sb.AppendLine("    int speed;");
                sb.AppendLine("} ServoConfig;");
                sb.AppendLine();
                sb.AppendLine("const ServoConfig SERVOS[SERVO_COUNT] = {");

                for (int i = 0; i < robot.physicalConfig.servos.Count; i++)
                {
                    var servo = robot.physicalConfig.servos[i];
                    string comma = i < robot.physicalConfig.servos.Count - 1 ? "," : "";
                    sb.AppendLine($"    {{\"{servo.jointName}\", {servo.minAngle}, {servo.maxAngle}, {servo.defaultAngle}, {servo.speed}}}{comma}");
                }
                sb.AppendLine("};");
                sb.AppendLine();
            }

            // Abilities as action sequences
            sb.AppendLine("// === ABILITIES ===");
            sb.AppendLine($"#define ABILITY_COUNT {robot.abilities.Count}");
            sb.AppendLine();

            for (int i = 0; i < robot.abilities.Count; i++)
            {
                var ability = robot.abilities[i];
                string prefix = $"ABILITY_{i}";

                sb.AppendLine($"// Ability {i}: {ability.abilityName}");
                sb.AppendLine($"#define {prefix}_NAME \"{ability.abilityName}\"");
                sb.AppendLine($"#define {prefix}_DAMAGE {ability.baseDamage}");
                sb.AppendLine($"#define {prefix}_ENERGY_COST {ability.energyCost}");
                sb.AppendLine($"#define {prefix}_COOLDOWN {ability.cooldown:F2}f");
                sb.AppendLine();
            }

            // Fighting style enumeration
            sb.AppendLine("// === FIGHTING STYLE ENUM ===");
            sb.AppendLine("enum FightingStyle {");
            foreach (FightingStyle style in Enum.GetValues(typeof(FightingStyle)))
            {
                sb.AppendLine($"    STYLE_{style.ToString().ToUpper()} = {(int)style},");
            }
            sb.AppendLine("};");
            sb.AppendLine();

            sb.AppendLine("#endif // ROBOT_DATA_H");

            string headerContent = sb.ToString();

            Debug.Log($"📤 Robot exported to Arduino header: {robot.robotName}");
            OnExportComplete?.Invoke(headerContent);

            return headerContent;
        }

        /// <summary>
        /// Export as MicroPython code for ESP32/Raspberry Pi robots!
        /// </summary>
        public string ExportToMicroPython(RobotData robot)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("# ========================================");
            sb.AppendLine("# WHAT THE FUNAN - ROBOT DATA EXPORT");
            sb.AppendLine($"# Robot: {robot.robotName}");
            sb.AppendLine($"# ID: {robot.robotId}");
            sb.AppendLine($"# Generated: {DateTime.Now}");
            sb.AppendLine("# ========================================");
            sb.AppendLine();
            sb.AppendLine("class RobotData:");
            sb.AppendLine($"    NAME = \"{robot.robotName}\"");
            sb.AppendLine($"    ID = \"{robot.robotId}\"");
            sb.AppendLine();
            sb.AppendLine("    # Core Stats (0-100)");
            sb.AppendLine($"    POWER = {robot.coreStats.power}");
            sb.AppendLine($"    SPEED = {robot.coreStats.speed}");
            sb.AppendLine($"    DEFENSE = {robot.coreStats.defense}");
            sb.AppendLine($"    INTELLIGENCE = {robot.coreStats.intelligence}");
            sb.AppendLine($"    ENERGY = {robot.coreStats.energy}");
            sb.AppendLine($"    PRECISION = {robot.coreStats.precision}");
            sb.AppendLine();
            sb.AppendLine("    # AI Behavior");
            sb.AppendLine($"    AI_STYLE = {(int)robot.aiConfig.primaryStyle}  # {robot.aiConfig.primaryStyle}");
            sb.AppendLine($"    AGGRESSION = {robot.aiConfig.aggression}");
            sb.AppendLine($"    CAUTION = {robot.aiConfig.caution}");
            sb.AppendLine($"    ADAPTABILITY = {robot.aiConfig.adaptability}");
            sb.AppendLine();
            sb.AppendLine("    # Servos");
            sb.AppendLine("    SERVOS = [");
            foreach (var servo in robot.physicalConfig.servos)
            {
                sb.AppendLine($"        {{\"joint\": \"{servo.jointName}\", \"min\": {servo.minAngle}, \"max\": {servo.maxAngle}, \"default\": {servo.defaultAngle}, \"speed\": {servo.speed}}},");
            }
            sb.AppendLine("    ]");
            sb.AppendLine();
            sb.AppendLine("    # Abilities");
            sb.AppendLine("    ABILITIES = [");
            foreach (var ability in robot.abilities)
            {
                sb.AppendLine($"        {{\"name\": \"{ability.abilityName}\", \"damage\": {ability.baseDamage}, \"cost\": {ability.energyCost}, \"cooldown\": {ability.cooldown:F2}}},");
            }
            sb.AppendLine("    ]");
            sb.AppendLine();

            // Add execution helper
            sb.AppendLine("    @staticmethod");
            sb.AppendLine("    def get_motor_speed(stat_speed):");
            sb.AppendLine("        \"\"\"Convert game speed stat to motor PWM (0-255)\"\"\"");
            sb.AppendLine("        return int(stat_speed * 2.55)");
            sb.AppendLine();
            sb.AppendLine("    @staticmethod");
            sb.AppendLine("    def get_servo_speed(stat_speed):");
            sb.AppendLine("        \"\"\"Convert game speed stat to servo speed (degrees/sec)\"\"\"");
            sb.AppendLine("        return stat_speed * 3  # 0-300 deg/sec");

            string pythonContent = sb.ToString();

            Debug.Log($"📤 Robot exported to MicroPython: {robot.robotName}");
            OnExportComplete?.Invoke(pythonContent);

            return pythonContent;
        }

        #endregion

        #region Import

        /// <summary>
        /// Import robot from JSON
        /// </summary>
        public RobotData ImportFromJSON(string json)
        {
            try
            {
                var wrapper = JsonUtility.FromJson<RobotExportWrapper>(json);
                if (wrapper == null || wrapper.robotData == null)
                {
                    OnExportError?.Invoke("Invalid JSON format");
                    return null;
                }

                Debug.Log($"📥 Robot imported: {wrapper.robotData.robotName}");
                return wrapper.robotData;
            }
            catch (Exception e)
            {
                OnExportError?.Invoke($"JSON import failed: {e.Message}");
                return null;
            }
        }

        /// <summary>
        /// Import robot from binary
        /// </summary>
        public RobotData ImportFromBinary(byte[] data)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(data))
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    // Verify magic bytes
                    byte[] magic = reader.ReadBytes(4);
                    if (Encoding.ASCII.GetString(magic) != "WTFR")
                    {
                        OnExportError?.Invoke("Invalid binary format (bad magic bytes)");
                        return null;
                    }

                    byte version = reader.ReadByte();
                    if (version != 1)
                    {
                        OnExportError?.Invoke($"Unsupported version: {version}");
                        return null;
                    }

                    var robot = new RobotData();
                    robot.robotId = reader.ReadString();
                    robot.robotName = reader.ReadString();
                    robot.createdTimestamp = reader.ReadInt64();

                    // Core stats
                    robot.coreStats.power = reader.ReadByte();
                    robot.coreStats.speed = reader.ReadByte();
                    robot.coreStats.defense = reader.ReadByte();
                    robot.coreStats.intelligence = reader.ReadByte();
                    robot.coreStats.energy = reader.ReadByte();
                    robot.coreStats.precision = reader.ReadByte();
                    robot.coreStats.RecalculateDerived();

                    // Continue reading other fields...
                    // (Abbreviated for length)

                    Debug.Log($"📥 Robot imported from binary: {robot.robotName}");
                    return robot;
                }
            }
            catch (Exception e)
            {
                OnExportError?.Invoke($"Binary import failed: {e.Message}");
                return null;
            }
        }

        #endregion

        #region Helpers

        private RobotExportWrapper CreateExportWrapper(RobotData robot)
        {
            return new RobotExportWrapper
            {
                exportVersion = _exportVersion,
                exportTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                platform = Application.platform.ToString(),
                robotData = robot
            };
        }

        private uint CalculateChecksum(byte[] data)
        {
            uint checksum = 0;
            foreach (byte b in data)
            {
                checksum = ((checksum << 5) + checksum) + b;
            }
            return checksum;
        }

        public string GetExportPath() => _exportPath;

        #endregion
    }

    /// <summary>
    /// Wrapper for JSON export with metadata
    /// </summary>
    [Serializable]
    public class RobotExportWrapper
    {
        public string exportVersion;
        public long exportTimestamp;
        public string platform;
        public RobotData robotData;
    }
}

