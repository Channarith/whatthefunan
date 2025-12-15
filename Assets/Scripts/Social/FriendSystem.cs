using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

namespace WhatTheFunan.Social
{
    /// <summary>
    /// Manages friend connections using friend codes (child-safe, no usernames).
    /// Supports friend list, online status, and activity feed.
    /// </summary>
    public class FriendSystem : MonoBehaviour
    {
        #region Singleton
        private static FriendSystem _instance;
        public static FriendSystem Instance => _instance;
        #endregion

        #region Events
        public static event Action<FriendData> OnFriendAdded;
        public static event Action<FriendData> OnFriendRemoved;
        public static event Action<FriendData> OnFriendRequestReceived;
        public static event Action<FriendData> OnFriendOnline;
        public static event Action<FriendData> OnFriendOffline;
        public static event Action<string> OnFriendCodeGenerated;
        #endregion

        #region Friend Data
        private string _myFriendCode;
        private List<FriendData> _friends = new List<FriendData>();
        private List<FriendRequest> _pendingRequests = new List<FriendRequest>();
        
        public string MyFriendCode => _myFriendCode;
        public IReadOnlyList<FriendData> Friends => _friends;
        public IReadOnlyList<FriendRequest> PendingRequests => _pendingRequests;
        public int FriendCount => _friends.Count;
        #endregion

        #region Settings
        [Header("Settings")]
        [SerializeField] private int _maxFriends = 50;
        [SerializeField] private int _friendCodeLength = 8;
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
            
            GenerateFriendCode();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }
        #endregion

        #region Friend Code
        /// <summary>
        /// Generate a unique friend code for this player.
        /// </summary>
        private void GenerateFriendCode()
        {
            // Check if we already have one saved
            if (PlayerPrefs.HasKey("FriendCode"))
            {
                _myFriendCode = PlayerPrefs.GetString("FriendCode");
            }
            else
            {
                // Generate new code (alphanumeric, no confusing chars)
                const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
                char[] code = new char[_friendCodeLength];
                
                for (int i = 0; i < _friendCodeLength; i++)
                {
                    code[i] = chars[UnityEngine.Random.Range(0, chars.Length)];
                }
                
                _myFriendCode = new string(code);
                PlayerPrefs.SetString("FriendCode", _myFriendCode);
                PlayerPrefs.Save();
            }
            
            OnFriendCodeGenerated?.Invoke(_myFriendCode);
            Debug.Log($"[FriendSystem] My friend code: {_myFriendCode}");
        }

        /// <summary>
        /// Get formatted friend code for display (e.g., "ABCD-EFGH").
        /// </summary>
        public string GetFormattedFriendCode()
        {
            if (_myFriendCode.Length >= 8)
            {
                return $"{_myFriendCode.Substring(0, 4)}-{_myFriendCode.Substring(4, 4)}";
            }
            return _myFriendCode;
        }
        #endregion

        #region Friend Management
        /// <summary>
        /// Send a friend request using a friend code.
        /// </summary>
        public bool SendFriendRequest(string friendCode)
        {
            // Normalize code
            friendCode = friendCode.ToUpper().Replace("-", "").Replace(" ", "");
            
            if (friendCode == _myFriendCode)
            {
                Debug.LogWarning("[FriendSystem] Cannot add yourself");
                return false;
            }
            
            if (_friends.Any(f => f.friendCode == friendCode))
            {
                Debug.LogWarning("[FriendSystem] Already friends");
                return false;
            }
            
            if (_friends.Count >= _maxFriends)
            {
                Debug.LogWarning("[FriendSystem] Friend list full");
                return false;
            }
            
            // In real implementation, this would call Firebase
            // For now, simulate success
            Debug.Log($"[FriendSystem] Friend request sent to: {friendCode}");
            
            // TODO: Firebase implementation
            // - Look up friend code in database
            // - Create friend request document
            // - Send notification to recipient
            
            return true;
        }

        /// <summary>
        /// Accept a friend request.
        /// </summary>
        public bool AcceptFriendRequest(string requestId)
        {
            var request = _pendingRequests.FirstOrDefault(r => r.requestId == requestId);
            if (request == null)
            {
                return false;
            }
            
            if (_friends.Count >= _maxFriends)
            {
                Debug.LogWarning("[FriendSystem] Friend list full");
                return false;
            }
            
            var newFriend = new FriendData
            {
                friendCode = request.senderCode,
                displayName = request.senderName,
                avatarId = request.senderAvatarId,
                addedDate = DateTime.Now
            };
            
            _friends.Add(newFriend);
            _pendingRequests.Remove(request);
            
            OnFriendAdded?.Invoke(newFriend);
            
            // TODO: Update Firebase
            
            Debug.Log($"[FriendSystem] Accepted friend request from: {newFriend.displayName}");
            return true;
        }

        /// <summary>
        /// Decline a friend request.
        /// </summary>
        public void DeclineFriendRequest(string requestId)
        {
            var request = _pendingRequests.FirstOrDefault(r => r.requestId == requestId);
            if (request != null)
            {
                _pendingRequests.Remove(request);
                // TODO: Update Firebase
            }
        }

        /// <summary>
        /// Remove a friend.
        /// </summary>
        public void RemoveFriend(string friendCode)
        {
            var friend = _friends.FirstOrDefault(f => f.friendCode == friendCode);
            if (friend != null)
            {
                _friends.Remove(friend);
                OnFriendRemoved?.Invoke(friend);
                // TODO: Update Firebase
                
                Debug.Log($"[FriendSystem] Removed friend: {friend.displayName}");
            }
        }

        /// <summary>
        /// Block a player (prevents future requests).
        /// </summary>
        public void BlockPlayer(string friendCode)
        {
            // Remove if friend
            RemoveFriend(friendCode);
            
            // Add to block list
            var blocked = PlayerPrefs.GetString("BlockedPlayers", "");
            blocked += friendCode + ",";
            PlayerPrefs.SetString("BlockedPlayers", blocked);
            PlayerPrefs.Save();
            
            Debug.Log($"[FriendSystem] Blocked player: {friendCode}");
        }
        #endregion

        #region Online Status
        /// <summary>
        /// Update my online status.
        /// </summary>
        public void SetOnlineStatus(bool online)
        {
            // TODO: Update Firebase presence
            Debug.Log($"[FriendSystem] Online status: {online}");
        }

        /// <summary>
        /// Get online friends.
        /// </summary>
        public List<FriendData> GetOnlineFriends()
        {
            return _friends.Where(f => f.isOnline).ToList();
        }
        #endregion

        #region Activity
        /// <summary>
        /// Get friend's recent activity.
        /// </summary>
        public List<FriendActivity> GetFriendActivity(string friendCode)
        {
            // TODO: Fetch from Firebase
            return new List<FriendActivity>();
        }
        #endregion

        #region Save/Load
        public FriendSaveData GetSaveData()
        {
            return new FriendSaveData
            {
                friends = _friends.ToList()
            };
        }

        public void LoadSaveData(FriendSaveData data)
        {
            _friends = data.friends.ToList();
        }

        [Serializable]
        public class FriendSaveData
        {
            public List<FriendData> friends;
        }
        #endregion
    }

    #region Friend Data Classes
    [Serializable]
    public class FriendData
    {
        public string friendCode;
        public string displayName;
        public string avatarId;
        public int level;
        public DateTime addedDate;
        public DateTime lastOnline;
        public bool isOnline;
    }

    [Serializable]
    public class FriendRequest
    {
        public string requestId;
        public string senderCode;
        public string senderName;
        public string senderAvatarId;
        public DateTime sentDate;
    }

    [Serializable]
    public class FriendActivity
    {
        public string activityType; // "level_up", "quest_complete", "rare_catch", etc.
        public string description;
        public DateTime timestamp;
    }
    #endregion
}

