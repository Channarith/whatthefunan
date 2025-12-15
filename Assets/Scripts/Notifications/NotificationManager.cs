using UnityEngine;
using System;
using System.Collections.Generic;

namespace WhatTheFunan.Notifications
{
    /// <summary>
    /// Manages local and push notifications.
    /// Handles engagement reminders, daily rewards, and event notifications.
    /// Child-friendly notifications that comply with COPPA.
    /// </summary>
    public class NotificationManager : MonoBehaviour
    {
        #region Singleton
        private static NotificationManager _instance;
        public static NotificationManager Instance => _instance;
        #endregion

        #region Events
        public static event Action OnPermissionGranted;
        public static event Action OnPermissionDenied;
        public static event Action<ScheduledNotification> OnNotificationScheduled;
        #endregion

        #region Settings
        [Header("Notification Settings")]
        [SerializeField] private bool _enabled = true;
        [SerializeField] private bool _dailyRewardReminder = true;
        [SerializeField] private bool _eventReminders = true;
        [SerializeField] private bool _engagementReminders = true;
        
        [Header("Timing")]
        [SerializeField] private int _dailyRewardHour = 18; // 6 PM
        [SerializeField] private int _engagementHoursAfterLastPlay = 24;
        
        [Header("Messages")]
        [SerializeField] private List<NotificationMessage> _engagementMessages = new List<NotificationMessage>();
        #endregion

        #region State
        private bool _permissionGranted;
        private List<ScheduledNotification> _scheduledNotifications = new List<ScheduledNotification>();
        
        public bool IsEnabled => _enabled && _permissionGranted;
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
            
            LoadSettings();
        }

        private void Start()
        {
            InitializeNotifications();
        }

        private void OnDestroy()
        {
            if (_instance == this) _instance = null;
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                // App going to background - schedule notifications
                ScheduleEngagementNotifications();
                RecordLastPlayTime();
            }
            else
            {
                // App returning - cancel engagement notifications
                CancelAllEngagementNotifications();
            }
        }

        private void OnApplicationQuit()
        {
            ScheduleEngagementNotifications();
            RecordLastPlayTime();
        }
        #endregion

        #region Initialization
        private void InitializeNotifications()
        {
            // TODO: Initialize Unity Mobile Notifications
            // #if UNITY_IOS
            // var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge;
            // NotificationCenter.RequestAuthorization(authorizationOption).ContinueWith(task => { });
            // #endif
            
            // #if UNITY_ANDROID
            // var channel = new AndroidNotificationChannel {
            //     Id = "funan_channel",
            //     Name = "What the Funan",
            //     Importance = Importance.Default,
            //     Description = "Game notifications",
            // };
            // AndroidNotificationCenter.RegisterNotificationChannel(channel);
            // #endif
            
            _permissionGranted = PlayerPrefs.GetInt("NotificationPermission", 0) == 1;
            
            Debug.Log("[NotificationManager] Initialized (mock mode)");
        }

        /// <summary>
        /// Request notification permission from user.
        /// </summary>
        public void RequestPermission()
        {
            // TODO: Implement actual permission request
            
            // For mock, simulate permission granted
            _permissionGranted = true;
            PlayerPrefs.SetInt("NotificationPermission", 1);
            PlayerPrefs.Save();
            
            OnPermissionGranted?.Invoke();
            Debug.Log("[NotificationManager] Permission granted (mock)");
        }
        #endregion

        #region Scheduling
        /// <summary>
        /// Schedule a local notification.
        /// </summary>
        public void ScheduleNotification(string title, string message, DateTime fireTime, string notificationId = null)
        {
            if (!IsEnabled) return;
            
            notificationId = notificationId ?? Guid.NewGuid().ToString();
            
            var scheduled = new ScheduledNotification
            {
                id = notificationId,
                title = title,
                message = message,
                fireTime = fireTime
            };
            
            _scheduledNotifications.Add(scheduled);
            
            // TODO: Actually schedule with platform
            // #if UNITY_IOS
            // var notification = new iOSNotification {
            //     Identifier = notificationId,
            //     Title = title,
            //     Body = message,
            //     Trigger = new iOSNotificationTimeIntervalTrigger {
            //         TimeInterval = fireTime - DateTime.Now
            //     }
            // };
            // iOSNotificationCenter.ScheduleNotification(notification);
            // #endif
            
            OnNotificationScheduled?.Invoke(scheduled);
            Debug.Log($"[NotificationManager] Scheduled: {title} at {fireTime}");
        }

        /// <summary>
        /// Cancel a specific notification.
        /// </summary>
        public void CancelNotification(string notificationId)
        {
            _scheduledNotifications.RemoveAll(n => n.id == notificationId);
            
            // TODO: Platform cancel
            // #if UNITY_IOS
            // iOSNotificationCenter.RemoveScheduledNotification(notificationId);
            // #endif
            
            Debug.Log($"[NotificationManager] Cancelled: {notificationId}");
        }

        /// <summary>
        /// Cancel all notifications.
        /// </summary>
        public void CancelAllNotifications()
        {
            _scheduledNotifications.Clear();
            
            // TODO: Platform cancel all
            // #if UNITY_IOS
            // iOSNotificationCenter.RemoveAllScheduledNotifications();
            // #endif
            
            Debug.Log("[NotificationManager] Cancelled all notifications");
        }
        #endregion

        #region Engagement Notifications
        private void ScheduleEngagementNotifications()
        {
            if (!_engagementReminders) return;
            
            // Schedule at different intervals
            int[] hoursLater = { 24, 48, 72, 168 }; // 1 day, 2 days, 3 days, 1 week
            
            for (int i = 0; i < hoursLater.Length && i < _engagementMessages.Count; i++)
            {
                var message = _engagementMessages[i];
                var fireTime = DateTime.Now.AddHours(hoursLater[i]);
                
                ScheduleNotification(
                    message.title,
                    message.body,
                    fireTime,
                    $"engagement_{hoursLater[i]}h"
                );
            }
        }

        private void CancelAllEngagementNotifications()
        {
            int[] hoursLater = { 24, 48, 72, 168 };
            foreach (int hours in hoursLater)
            {
                CancelNotification($"engagement_{hours}h");
            }
        }

        private void RecordLastPlayTime()
        {
            PlayerPrefs.SetString("LastPlayTime", DateTime.Now.ToString("o"));
            PlayerPrefs.Save();
        }
        #endregion

        #region Daily Reward Reminder
        /// <summary>
        /// Schedule daily reward reminder.
        /// </summary>
        public void ScheduleDailyRewardReminder()
        {
            if (!_dailyRewardReminder) return;
            
            // Check if already claimed today
            if (LiveOps.DailyRewards.Instance?.CanClaimToday == false)
            {
                // Schedule for tomorrow at the specified hour
                var tomorrow = DateTime.Today.AddDays(1).AddHours(_dailyRewardHour);
                
                ScheduleNotification(
                    "Your daily treasure awaits!",
                    "Come back to claim your daily reward in What the Funan!",
                    tomorrow,
                    "daily_reward"
                );
            }
        }
        #endregion

        #region Event Notifications
        /// <summary>
        /// Schedule notification for an upcoming event.
        /// </summary>
        public void ScheduleEventNotification(string eventName, DateTime startTime)
        {
            if (!_eventReminders) return;
            
            // Notify 1 hour before
            var notifyTime = startTime.AddHours(-1);
            
            if (notifyTime > DateTime.Now)
            {
                ScheduleNotification(
                    $"{eventName} starts soon!",
                    "A special event is about to begin. Don't miss out!",
                    notifyTime,
                    $"event_{eventName}"
                );
            }
        }
        #endregion

        #region Settings
        /// <summary>
        /// Enable or disable notifications.
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enabled = enabled;
            SaveSettings();
            
            if (!enabled)
            {
                CancelAllNotifications();
            }
        }

        /// <summary>
        /// Enable or disable daily reward reminders.
        /// </summary>
        public void SetDailyRewardReminder(bool enabled)
        {
            _dailyRewardReminder = enabled;
            SaveSettings();
            
            if (!enabled)
            {
                CancelNotification("daily_reward");
            }
        }

        /// <summary>
        /// Enable or disable engagement reminders.
        /// </summary>
        public void SetEngagementReminders(bool enabled)
        {
            _engagementReminders = enabled;
            SaveSettings();
            
            if (!enabled)
            {
                CancelAllEngagementNotifications();
            }
        }

        private void SaveSettings()
        {
            PlayerPrefs.SetInt("Notification_Enabled", _enabled ? 1 : 0);
            PlayerPrefs.SetInt("Notification_DailyReward", _dailyRewardReminder ? 1 : 0);
            PlayerPrefs.SetInt("Notification_Event", _eventReminders ? 1 : 0);
            PlayerPrefs.SetInt("Notification_Engagement", _engagementReminders ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void LoadSettings()
        {
            _enabled = PlayerPrefs.GetInt("Notification_Enabled", 1) == 1;
            _dailyRewardReminder = PlayerPrefs.GetInt("Notification_DailyReward", 1) == 1;
            _eventReminders = PlayerPrefs.GetInt("Notification_Event", 1) == 1;
            _engagementReminders = PlayerPrefs.GetInt("Notification_Engagement", 1) == 1;
        }
        #endregion
    }

    #region Notification Data Classes
    [Serializable]
    public class NotificationMessage
    {
        public string title;
        public string body;
    }

    public class ScheduledNotification
    {
        public string id;
        public string title;
        public string message;
        public DateTime fireTime;
    }
    #endregion
}

