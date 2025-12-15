using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Social
{
    /// <summary>
    /// Manages daily gift sending and receiving between friends.
    /// No trading (prevents scams) - only sending.
    /// </summary>
    public class GiftSystem : MonoBehaviour
    {
        #region Singleton
        private static GiftSystem _instance;
        public static GiftSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<Gift> OnGiftSent;
        public static event Action<Gift> OnGiftReceived;
        public static event Action<Gift> OnGiftClaimed;
        public static event Action<string> OnDailyGiftReset;
        #endregion

        #region Gift Data
        [Header("Gift Types")]
        [SerializeField] private List<GiftType> _giftTypes = new List<GiftType>();
        
        private List<Gift> _inbox = new List<Gift>();
        private Dictionary<string, DateTime> _lastGiftSent = new Dictionary<string, DateTime>();
        
        public IReadOnlyList<Gift> Inbox => _inbox;
        public int UnclaimedGiftCount => _inbox.Count(g => !g.isClaimed);
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private int _maxInboxSize = 50;
        [SerializeField] private int _giftExpirationDays = 7;
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
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void Start()
        {
            // Clean up expired gifts
            CleanupExpiredGifts();
        }
        #endregion

        #region Sending Gifts
        /// <summary>
        /// Check if we can send a gift to a friend today.
        /// </summary>
        public bool CanSendGift(string friendCode)
        {
            if (!_lastGiftSent.TryGetValue(friendCode, out DateTime lastSent))
            {
                return true;
            }
            
            return lastSent.Date != DateTime.Today;
        }

        /// <summary>
        /// Send a gift to a friend.
        /// </summary>
        public bool SendGift(string friendCode, string giftTypeId = null)
        {
            if (!CanSendGift(friendCode))
            {
                Debug.LogWarning("[GiftSystem] Already sent gift to this friend today");
                return false;
            }
            
            // Select gift type (random if not specified)
            GiftType giftType;
            if (string.IsNullOrEmpty(giftTypeId))
            {
                giftType = SelectRandomGift();
            }
            else
            {
                giftType = _giftTypes.FirstOrDefault(g => g.giftTypeId == giftTypeId);
            }
            
            if (giftType == null)
            {
                Debug.LogError("[GiftSystem] Invalid gift type");
                return false;
            }
            
            var gift = new Gift
            {
                giftId = Guid.NewGuid().ToString(),
                giftTypeId = giftType.giftTypeId,
                senderCode = FriendSystem.Instance?.MyFriendCode ?? "",
                senderName = "You", // Would come from profile
                recipientCode = friendCode,
                sentDate = DateTime.Now,
                expirationDate = DateTime.Now.AddDays(_giftExpirationDays)
            };
            
            _lastGiftSent[friendCode] = DateTime.Now;
            
            // TODO: Send to Firebase
            
            OnGiftSent?.Invoke(gift);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Light);
            
            Debug.Log($"[GiftSystem] Sent {giftType.giftName} to {friendCode}");
            return true;
        }

        /// <summary>
        /// Send gifts to all friends who haven't received one today.
        /// </summary>
        public int SendGiftsToAll()
        {
            var friends = FriendSystem.Instance?.Friends;
            if (friends == null) return 0;
            
            int sentCount = 0;
            foreach (var friend in friends)
            {
                if (CanSendGift(friend.friendCode))
                {
                    if (SendGift(friend.friendCode))
                    {
                        sentCount++;
                    }
                }
            }
            
            Debug.Log($"[GiftSystem] Sent {sentCount} gifts");
            return sentCount;
        }

        private GiftType SelectRandomGift()
        {
            // Weight by rarity (more common = higher chance)
            float totalWeight = _giftTypes.Sum(g => 1f / (g.rarity + 1));
            float roll = UnityEngine.Random.Range(0, totalWeight);
            
            float current = 0;
            foreach (var gift in _giftTypes)
            {
                current += 1f / (gift.rarity + 1);
                if (roll <= current)
                {
                    return gift;
                }
            }
            
            return _giftTypes.FirstOrDefault();
        }
        #endregion

        #region Receiving Gifts
        /// <summary>
        /// Called when a gift is received (from Firebase listener).
        /// </summary>
        public void OnGiftReceivedFromServer(Gift gift)
        {
            if (_inbox.Count >= _maxInboxSize)
            {
                // Remove oldest claimed gift
                var oldestClaimed = _inbox
                    .Where(g => g.isClaimed)
                    .OrderBy(g => g.sentDate)
                    .FirstOrDefault();
                    
                if (oldestClaimed != null)
                {
                    _inbox.Remove(oldestClaimed);
                }
            }
            
            _inbox.Add(gift);
            OnGiftReceived?.Invoke(gift);
            
            Debug.Log($"[GiftSystem] Received gift from {gift.senderName}");
        }

        /// <summary>
        /// Claim a gift from inbox.
        /// </summary>
        public bool ClaimGift(string giftId)
        {
            var gift = _inbox.FirstOrDefault(g => g.giftId == giftId);
            if (gift == null)
            {
                Debug.LogWarning("[GiftSystem] Gift not found");
                return false;
            }
            
            if (gift.isClaimed)
            {
                Debug.LogWarning("[GiftSystem] Gift already claimed");
                return false;
            }
            
            if (DateTime.Now > gift.expirationDate)
            {
                Debug.LogWarning("[GiftSystem] Gift expired");
                _inbox.Remove(gift);
                return false;
            }
            
            // Get gift type and grant rewards
            var giftType = _giftTypes.FirstOrDefault(g => g.giftTypeId == gift.giftTypeId);
            if (giftType != null)
            {
                GrantGiftReward(giftType);
            }
            
            gift.isClaimed = true;
            gift.claimedDate = DateTime.Now;
            
            OnGiftClaimed?.Invoke(gift);
            Core.HapticManager.Instance?.TriggerHaptic(Core.HapticManager.HapticType.Success);
            
            Debug.Log($"[GiftSystem] Claimed gift: {giftType?.giftName}");
            return true;
        }

        /// <summary>
        /// Claim all unclaimed gifts.
        /// </summary>
        public int ClaimAllGifts()
        {
            var unclaimed = _inbox.Where(g => !g.isClaimed && DateTime.Now <= g.expirationDate).ToList();
            int claimed = 0;
            
            foreach (var gift in unclaimed)
            {
                if (ClaimGift(gift.giftId))
                {
                    claimed++;
                }
            }
            
            Debug.Log($"[GiftSystem] Claimed {claimed} gifts");
            return claimed;
        }

        private void GrantGiftReward(GiftType giftType)
        {
            if (giftType.coinReward > 0)
            {
                Economy.CurrencyManager.Instance?.AddCoins(giftType.coinReward);
            }
            
            if (giftType.gemReward > 0)
            {
                Economy.CurrencyManager.Instance?.AddGems(giftType.gemReward);
            }
            
            if (!string.IsNullOrEmpty(giftType.itemReward))
            {
                // InventorySystem.Instance?.AddItem(giftType.itemReward);
            }
        }
        #endregion

        #region Cleanup
        private void CleanupExpiredGifts()
        {
            int removed = _inbox.RemoveAll(g => DateTime.Now > g.expirationDate);
            if (removed > 0)
            {
                Debug.Log($"[GiftSystem] Removed {removed} expired gifts");
            }
        }
        #endregion

        #region Query
        /// <summary>
        /// Get friends we can still send gifts to today.
        /// </summary>
        public List<FriendData> GetFriendsToGift()
        {
            var friends = FriendSystem.Instance?.Friends;
            if (friends == null) return new List<FriendData>();
            
            return friends.Where(f => CanSendGift(f.friendCode)).ToList();
        }

        /// <summary>
        /// Get count of friends we can gift today.
        /// </summary>
        public int GetRemainingGiftCount()
        {
            return GetFriendsToGift().Count;
        }
        #endregion
    }

    #region Gift Data Classes
    [Serializable]
    public class GiftType
    {
        public string giftTypeId;
        public string giftName;
        public Sprite icon;
        public int rarity; // 0 = common, higher = rarer
        
        [Header("Rewards")]
        public int coinReward;
        public int gemReward;
        public string itemReward;
    }

    [Serializable]
    public class Gift
    {
        public string giftId;
        public string giftTypeId;
        public string senderCode;
        public string senderName;
        public string recipientCode;
        public DateTime sentDate;
        public DateTime expirationDate;
        public bool isClaimed;
        public DateTime claimedDate;
    }
    #endregion
}

