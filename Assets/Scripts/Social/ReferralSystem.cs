using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Social
{
    /// <summary>
    /// Referral/invite system for friend recruitment.
    /// Rewards both referrer and referee.
    /// </summary>
    public class ReferralSystem : MonoBehaviour
    {
        #region Singleton
        private static ReferralSystem _instance;
        public static ReferralSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<string> OnReferralCodeGenerated;
        public static event Action<ReferralReward> OnReferralRewardClaimed;
        public static event Action<int> OnReferralCountUpdated;
        #endregion

        #region Settings
        [Header("Rewards")]
        [SerializeField] private int _referrerGems = 50;
        [SerializeField] private int _refereeGems = 25;
        [SerializeField] private int _referrerCoins = 500;
        [SerializeField] private int _refereeCoins = 250;
        
        [Header("Milestones")]
        [SerializeField] private List<ReferralMilestone> _milestones = new List<ReferralMilestone>();
        #endregion

        #region State
        private string _myReferralCode;
        private string _usedReferralCode;
        private int _referralCount;
        private HashSet<int> _claimedMilestones = new HashSet<int>();
        
        public string MyReferralCode => _myReferralCode;
        public int ReferralCount => _referralCount;
        public bool HasUsedReferralCode => !string.IsNullOrEmpty(_usedReferralCode);
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
            DontDestroyOnLoad(gameObject);
            
            LoadData();
            GenerateReferralCode();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Referral Code
        private void GenerateReferralCode()
        {
            if (!string.IsNullOrEmpty(_myReferralCode)) return;
            
            // Generate unique code based on player ID
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
            char[] code = new char[8];
            
            for (int i = 0; i < 8; i++)
            {
                code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
            }
            
            _myReferralCode = new string(code);
            SaveData();
            
            OnReferralCodeGenerated?.Invoke(_myReferralCode);
            Debug.Log($"[ReferralSystem] Referral code: {_myReferralCode}");
        }

        /// <summary>
        /// Get formatted referral code for display.
        /// </summary>
        public string GetFormattedCode()
        {
            if (_myReferralCode.Length >= 8)
            {
                return $"{_myReferralCode.Substring(0, 4)}-{_myReferralCode.Substring(4, 4)}";
            }
            return _myReferralCode;
        }

        /// <summary>
        /// Get shareable referral link.
        /// </summary>
        public string GetShareableLink()
        {
            // Would use deep linking in production
            return $"https://whatthefunan.com/invite/{_myReferralCode}";
        }
        #endregion

        #region Using Referral Code
        /// <summary>
        /// Use a referral code from another player.
        /// </summary>
        public bool UseReferralCode(string code)
        {
            if (HasUsedReferralCode)
            {
                Debug.Log("[ReferralSystem] Already used a referral code");
                return false;
            }
            
            // Normalize code
            code = code.ToUpper().Replace("-", "").Replace(" ", "");
            
            if (code == _myReferralCode)
            {
                Debug.Log("[ReferralSystem] Cannot use your own code");
                return false;
            }
            
            if (code.Length != 8)
            {
                Debug.Log("[ReferralSystem] Invalid code format");
                return false;
            }
            
            // TODO: Validate code exists in Firebase
            
            _usedReferralCode = code;
            
            // Grant referee rewards
            Economy.CurrencyManager.Instance?.AddGems(_refereeGems);
            Economy.CurrencyManager.Instance?.AddCoins(_refereeCoins);
            
            var reward = new ReferralReward
            {
                type = ReferralRewardType.Referee,
                gems = _refereeGems,
                coins = _refereeCoins
            };
            
            OnReferralRewardClaimed?.Invoke(reward);
            
            // TODO: Notify referrer via Firebase (they get rewards next login)
            
            SaveData();
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log($"[ReferralSystem] Used referral code: {code}");
            return true;
        }
        #endregion

        #region Referral Tracking
        /// <summary>
        /// Called when someone uses your referral code (from Firebase).
        /// </summary>
        public void OnReferralReceived()
        {
            _referralCount++;
            
            // Grant referrer rewards
            Economy.CurrencyManager.Instance?.AddGems(_referrerGems);
            Economy.CurrencyManager.Instance?.AddCoins(_referrerCoins);
            
            var reward = new ReferralReward
            {
                type = ReferralRewardType.Referrer,
                gems = _referrerGems,
                coins = _referrerCoins
            };
            
            OnReferralRewardClaimed?.Invoke(reward);
            OnReferralCountUpdated?.Invoke(_referralCount);
            
            // Check milestones
            CheckMilestones();
            
            SaveData();
            
            Debug.Log($"[ReferralSystem] Referral received! Total: {_referralCount}");
        }

        private void CheckMilestones()
        {
            foreach (var milestone in _milestones)
            {
                if (_referralCount >= milestone.requiredReferrals && 
                    !_claimedMilestones.Contains(milestone.requiredReferrals))
                {
                    ClaimMilestone(milestone);
                }
            }
        }

        private void ClaimMilestone(ReferralMilestone milestone)
        {
            _claimedMilestones.Add(milestone.requiredReferrals);
            
            if (milestone.bonusGems > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(milestone.bonusGems);
            }
            
            if (!string.IsNullOrEmpty(milestone.unlockId))
            {
                PlayerPrefs.SetInt($"Referral_Unlock_{milestone.unlockId}", 1);
            }
            
            SaveData();
            
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            Debug.Log($"[ReferralSystem] Milestone claimed: {milestone.requiredReferrals} referrals");
        }
        #endregion

        #region Sharing
        /// <summary>
        /// Share referral code via native share sheet.
        /// </summary>
        public void ShareReferralCode()
        {
            string message = $"Join me in What the Funan! Use my code {GetFormattedCode()} for free gems! {GetShareableLink()}";
            
            // TODO: Native share
            // NativeShare share = new NativeShare();
            // share.SetText(message);
            // share.Share();
            
            Debug.Log($"[ReferralSystem] Sharing: {message}");
        }
        #endregion

        #region Query
        /// <summary>
        /// Get next unclaimed milestone.
        /// </summary>
        public ReferralMilestone GetNextMilestone()
        {
            foreach (var milestone in _milestones)
            {
                if (!_claimedMilestones.Contains(milestone.requiredReferrals))
                {
                    return milestone;
                }
            }
            return null;
        }

        /// <summary>
        /// Get progress to next milestone.
        /// </summary>
        public float GetMilestoneProgress()
        {
            var next = GetNextMilestone();
            if (next == null) return 1f;
            
            return (float)_referralCount / next.requiredReferrals;
        }
        #endregion

        #region Save/Load
        private void SaveData()
        {
            PlayerPrefs.SetString("Referral_MyCode", _myReferralCode);
            PlayerPrefs.SetString("Referral_UsedCode", _usedReferralCode ?? "");
            PlayerPrefs.SetInt("Referral_Count", _referralCount);
            
            string claimed = string.Join(",", _claimedMilestones);
            PlayerPrefs.SetString("Referral_Claimed", claimed);
            
            PlayerPrefs.Save();
        }

        private void LoadData()
        {
            _myReferralCode = PlayerPrefs.GetString("Referral_MyCode", "");
            _usedReferralCode = PlayerPrefs.GetString("Referral_UsedCode", "");
            _referralCount = PlayerPrefs.GetInt("Referral_Count", 0);
            
            string claimed = PlayerPrefs.GetString("Referral_Claimed", "");
            if (!string.IsNullOrEmpty(claimed))
            {
                foreach (var s in claimed.Split(','))
                {
                    if (int.TryParse(s, out int milestone))
                    {
                        _claimedMilestones.Add(milestone);
                    }
                }
            }
        }
        #endregion
    }

    #region Referral Data Classes
    public enum ReferralRewardType
    {
        Referrer,
        Referee,
        Milestone
    }

    public class ReferralReward
    {
        public ReferralRewardType type;
        public int gems;
        public int coins;
        public string unlockId;
    }

    [Serializable]
    public class ReferralMilestone
    {
        public int requiredReferrals;
        public int bonusGems;
        public string unlockId;
        public string description;
    }
    #endregion
}

