using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using WhatTheFunan.Challenges;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// DAILY CHALLENGE UI! 🌅
    /// Shows today's challenges, progress, streaks, and rewards!
    /// </summary>
    public class DailyChallengeUI : MonoBehaviour
    {
        [Header("Main Panel")]
        [SerializeField] private GameObject _dailyPanel;
        [SerializeField] private CanvasGroup _canvasGroup;

        [Header("Header")]
        [SerializeField] private Text _headerTitle;
        [SerializeField] private Text _resetTimerText;
        [SerializeField] private Text _streakText;
        [SerializeField] private Image _streakFireIcon;

        [Header("Challenge List")]
        [SerializeField] private Transform _challengeListContainer;
        [SerializeField] private GameObject _challengeItemPrefab;

        [Header("Progress Bar")]
        [SerializeField] private Slider _overallProgressBar;
        [SerializeField] private Text _progressText;

        [Header("Mystery Challenge")]
        [SerializeField] private GameObject _mysteryPanel;
        [SerializeField] private Text _mysteryTitle;
        [SerializeField] private Text _mysteryDescription;
        [SerializeField] private Image _mysteryLockIcon;

        [Header("Streak Rewards")]
        [SerializeField] private Transform _streakRewardsContainer;
        [SerializeField] private GameObject _streakMilestonePrefab;

        [Header("Day Theme")]
        [SerializeField] private Text _dayThemeText;
        [SerializeField] private Image _dayThemeIcon;

        [Header("Completion Celebration")]
        [SerializeField] private GameObject _celebrationPanel;
        [SerializeField] private ParticleSystem _confettiParticles;
        [SerializeField] private Animator _celebrationAnimator;

        [Header("Bonus Rewards Display")]
        [SerializeField] private GameObject _bonusRewardPopup;
        [SerializeField] private Text _bonusRewardText;

        private List<GameObject> _challengeItems = new List<GameObject>();

        private void Start()
        {
            SubscribeToEvents();
            RefreshDisplay();
            UpdateResetTimer();
            InvokeRepeating(nameof(UpdateResetTimer), 1f, 1f);
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
            CancelInvoke(nameof(UpdateResetTimer));
        }

        private void SubscribeToEvents()
        {
            if (DailyChallengeSystem.Instance != null)
            {
                DailyChallengeSystem.Instance.OnDailyChallengesRefreshed += OnChallengesRefreshed;
                DailyChallengeSystem.Instance.OnChallengeCompleted += OnChallengeCompleted;
                DailyChallengeSystem.Instance.OnStreakUpdated += OnStreakUpdated;
                DailyChallengeSystem.Instance.OnStreakRewardEarned += OnStreakRewardEarned;
                DailyChallengeSystem.Instance.OnAllDailiesCompleted += OnAllDailiesCompleted;
                DailyChallengeSystem.Instance.OnBonusChallengeUnlocked += OnBonusChallengeUnlocked;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (DailyChallengeSystem.Instance != null)
            {
                DailyChallengeSystem.Instance.OnDailyChallengesRefreshed -= OnChallengesRefreshed;
                DailyChallengeSystem.Instance.OnChallengeCompleted -= OnChallengeCompleted;
                DailyChallengeSystem.Instance.OnStreakUpdated -= OnStreakUpdated;
                DailyChallengeSystem.Instance.OnStreakRewardEarned -= OnStreakRewardEarned;
                DailyChallengeSystem.Instance.OnAllDailiesCompleted -= OnAllDailiesCompleted;
                DailyChallengeSystem.Instance.OnBonusChallengeUnlocked -= OnBonusChallengeUnlocked;
            }
        }

        public void RefreshDisplay()
        {
            if (DailyChallengeSystem.Instance == null) return;

            UpdateHeader();
            UpdateDayTheme();
            UpdateChallengeList();
            UpdateMysteryChallenge();
            UpdateProgress();
            UpdateStreakDisplay();
        }

        private void UpdateHeader()
        {
            if (_headerTitle != null)
            {
                DayOfWeek today = DateTime.UtcNow.DayOfWeek;
                string dayEmoji = today switch
                {
                    DayOfWeek.Monday => "💪",
                    DayOfWeek.Tuesday => "🔥",
                    DayOfWeek.Wednesday => "🤪",
                    DayOfWeek.Thursday => "📚",
                    DayOfWeek.Friday => "💕",
                    DayOfWeek.Saturday => "🎉",
                    DayOfWeek.Sunday => "☀️",
                    _ => "🌅"
                };
                _headerTitle.text = $"{dayEmoji} Today's Challenges!";
            }
        }

        private void UpdateDayTheme()
        {
            if (_dayThemeText == null) return;

            DayOfWeek today = DateTime.UtcNow.DayOfWeek;
            string theme = today switch
            {
                DayOfWeek.Monday => "💪 MOTIVATION MONDAY!\nEasy challenges to start your week!",
                DayOfWeek.Tuesday => "🔥 TRY-HARD TUESDAY!\nBonus hard challenge available!",
                DayOfWeek.Wednesday => "🤪 WACKY WEDNESDAY!\nSilly challenges for maximum fun!",
                DayOfWeek.Thursday => "📚 THOUGHTFUL THURSDAY!\nLearn something new today!",
                DayOfWeek.Friday => "💕 FRIEND FRIDAY!\nConnect with your friends!",
                DayOfWeek.Saturday => "🎉 SUPER SATURDAY!\nDouble challenges, double fun!",
                DayOfWeek.Sunday => "☀️ SUNNY SUNDAY!\nRelax and enjoy extra challenges!",
                _ => "🌅 Daily Challenges!"
            };
            _dayThemeText.text = theme;
        }

        private void UpdateChallengeList()
        {
            // Clear existing items
            foreach (var item in _challengeItems)
            {
                if (item != null) Destroy(item);
            }
            _challengeItems.Clear();

            if (_challengeListContainer == null || _challengeItemPrefab == null) return;

            var challenges = DailyChallengeSystem.Instance?.GetTodaysChallenges();
            if (challenges == null) return;

            foreach (var challenge in challenges)
            {
                var item = Instantiate(_challengeItemPrefab, _challengeListContainer);
                SetupChallengeItem(item, challenge);
                _challengeItems.Add(item);
            }
        }

        private void SetupChallengeItem(GameObject item, DailyChallenge challenge)
        {
            var nameText = item.transform.Find("Name")?.GetComponent<Text>();
            var descText = item.transform.Find("Description")?.GetComponent<Text>();
            var progressText = item.transform.Find("Progress")?.GetComponent<Text>();
            var progressBar = item.transform.Find("ProgressBar")?.GetComponent<Slider>();
            var rewardText = item.transform.Find("Reward")?.GetComponent<Text>();
            var checkmark = item.transform.Find("Checkmark")?.gameObject;
            var categoryIcon = item.transform.Find("CategoryIcon")?.GetComponent<Image>();

            if (nameText != null)
            {
                string categoryEmoji = GetCategoryEmoji(challenge.definition.category);
                nameText.text = $"{categoryEmoji} {challenge.definition.displayName}";
                
                if (challenge.isCompleted)
                    nameText.color = new Color(0.5f, 0.8f, 0.5f); // Green for complete
                else if (challenge.definition.isHard)
                    nameText.color = new Color(1f, 0.6f, 0.2f); // Orange for hard
                else if (challenge.definition.isSilly)
                    nameText.color = new Color(1f, 0.8f, 0.2f); // Yellow for silly
            }

            if (descText != null)
                descText.text = challenge.definition.description;

            if (progressText != null)
                progressText.text = $"{challenge.currentProgress}/{challenge.definition.targetCount}";

            if (progressBar != null)
            {
                progressBar.maxValue = challenge.definition.targetCount;
                progressBar.value = challenge.currentProgress;
            }

            if (rewardText != null)
            {
                var r = challenge.definition.rewards;
                rewardText.text = $"🪙{r.coins} ⭐{r.xp}";
                if (r.gems > 0) rewardText.text += $" 💎{r.gems}";
            }

            if (checkmark != null)
                checkmark.SetActive(challenge.isCompleted);

            // Time estimate
            var timeText = item.transform.Find("TimeEstimate")?.GetComponent<Text>();
            if (timeText != null)
                timeText.text = $"~{challenge.definition.estimatedMinutes} min";
        }

        private string GetCategoryEmoji(DailyChallengeCategory category)
        {
            return category switch
            {
                DailyChallengeCategory.Exploration => "🗺️",
                DailyChallengeCategory.Collection => "📦",
                DailyChallengeCategory.MiniGame => "🎮",
                DailyChallengeCategory.Building => "🏗️",
                DailyChallengeCategory.Social => "💕",
                DailyChallengeCategory.Combat => "⚔️",
                DailyChallengeCategory.Quest => "📜",
                DailyChallengeCategory.Education => "📚",
                DailyChallengeCategory.Creative => "🎨",
                DailyChallengeCategory.Silly => "🤪",
                DailyChallengeCategory.Mystery => "❓",
                _ => "🎯"
            };
        }

        private void UpdateMysteryChallenge()
        {
            if (_mysteryPanel == null) return;

            var mystery = DailyChallengeSystem.Instance?.GetMysteryChallenge();
            if (mystery == null)
            {
                _mysteryPanel.SetActive(false);
                return;
            }

            _mysteryPanel.SetActive(true);

            if (_mysteryTitle != null)
            {
                _mysteryTitle.text = mystery.isLocked 
                    ? "❓ MYSTERY CHALLENGE ❓" 
                    : $"✨ {mystery.definition.revealedName}";
            }

            if (_mysteryDescription != null)
            {
                _mysteryDescription.text = mystery.isLocked
                    ? "Complete ALL daily challenges to unlock!"
                    : mystery.definition.description;
            }

            if (_mysteryLockIcon != null)
                _mysteryLockIcon.gameObject.SetActive(mystery.isLocked);
        }

        private void UpdateProgress()
        {
            var challenges = DailyChallengeSystem.Instance?.GetTodaysChallenges();
            if (challenges == null) return;

            int completed = 0;
            foreach (var c in challenges)
            {
                if (c.isCompleted) completed++;
            }

            if (_overallProgressBar != null)
            {
                _overallProgressBar.maxValue = challenges.Count;
                _overallProgressBar.value = completed;
            }

            if (_progressText != null)
                _progressText.text = $"{completed}/{challenges.Count} Complete!";
        }

        private void UpdateStreakDisplay()
        {
            int streak = DailyChallengeSystem.Instance?.GetCurrentStreak() ?? 0;

            if (_streakText != null)
            {
                _streakText.text = $"🔥 {streak} Day Streak!";
                
                // Color based on streak
                if (streak >= 30) _streakText.color = new Color(1f, 0.8f, 0f); // Gold
                else if (streak >= 14) _streakText.color = new Color(1f, 0.5f, 0f); // Orange
                else if (streak >= 7) _streakText.color = new Color(1f, 0.3f, 0.3f); // Red
                else _streakText.color = Color.white;
            }

            if (_streakFireIcon != null)
            {
                // Animate fire based on streak
                float scale = 1f + (streak * 0.02f); // Grows slightly with streak
                _streakFireIcon.transform.localScale = Vector3.one * Mathf.Min(scale, 1.5f);
            }

            UpdateStreakMilestones(streak);
        }

        private void UpdateStreakMilestones(int currentStreak)
        {
            if (_streakRewardsContainer == null) return;

            // Show upcoming milestones
            int[] milestones = { 3, 7, 14, 30 };
            string[] milestoneNames = { "3 Days", "Week!", "2 Weeks!", "Month!" };
            string[] milestoneEmojis = { "🌟", "🗓️", "💪", "👑" };

            // Clear existing
            foreach (Transform child in _streakRewardsContainer)
                Destroy(child.gameObject);

            for (int i = 0; i < milestones.Length; i++)
            {
                if (_streakMilestonePrefab != null)
                {
                    var milestone = Instantiate(_streakMilestonePrefab, _streakRewardsContainer);
                    var text = milestone.GetComponentInChildren<Text>();
                    var image = milestone.GetComponent<Image>();

                    if (text != null)
                        text.text = $"{milestoneEmojis[i]} {milestoneNames[i]}";

                    if (image != null)
                    {
                        if (currentStreak >= milestones[i])
                            image.color = new Color(0.3f, 0.8f, 0.3f, 1f); // Green - achieved!
                        else if (currentStreak >= milestones[i] - 2)
                            image.color = new Color(1f, 0.8f, 0.3f, 1f); // Yellow - almost!
                        else
                            image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f); // Gray - not yet
                    }
                }
            }
        }

        private void UpdateResetTimer()
        {
            if (_resetTimerText == null) return;

            var timeRemaining = DailyChallengeSystem.Instance?.GetTimeUntilReset() ?? TimeSpan.Zero;

            if (timeRemaining.TotalHours >= 1)
            {
                _resetTimerText.text = $"⏰ Resets in {(int)timeRemaining.TotalHours}h {timeRemaining.Minutes}m";
            }
            else
            {
                _resetTimerText.text = $"⏰ Resets in {timeRemaining.Minutes}m {timeRemaining.Seconds}s";
                _resetTimerText.color = Color.yellow; // Urgency!
            }
        }

        // Event handlers
        private void OnChallengesRefreshed(List<DailyChallenge> challenges)
        {
            RefreshDisplay();
            ShowNewDayAnimation();
        }

        private void OnChallengeCompleted(DailyChallenge challenge)
        {
            RefreshDisplay();
            ShowChallengeCompleteAnimation(challenge);
        }

        private void OnStreakUpdated(int newStreak)
        {
            UpdateStreakDisplay();
            ShowStreakAnimation(newStreak);
        }

        private void OnStreakRewardEarned(DailyReward reward)
        {
            ShowStreakRewardPopup(reward);
        }

        private void OnAllDailiesCompleted()
        {
            ShowAllCompleteAnimation();
        }

        private void OnBonusChallengeUnlocked(DailyChallenge mystery)
        {
            UpdateMysteryChallenge();
            ShowMysteryUnlockAnimation();
        }

        // Animations
        private void ShowNewDayAnimation()
        {
            Debug.Log("🌅 NEW DAY! Fresh challenges await!");
            // Trigger sunrise animation
        }

        private void ShowChallengeCompleteAnimation(DailyChallenge challenge)
        {
            Debug.Log($"✅ {challenge.definition.displayName} COMPLETE!");
            // Trigger checkmark animation, coin burst
        }

        private void ShowStreakAnimation(int streak)
        {
            Debug.Log($"🔥 STREAK: {streak} days!");
            // Fire grows, number pops
        }

        private void ShowStreakRewardPopup(DailyReward reward)
        {
            if (_bonusRewardPopup != null)
            {
                _bonusRewardPopup.SetActive(true);
                if (_bonusRewardText != null)
                {
                    _bonusRewardText.text = $"🔥 STREAK REWARD!\n🪙 {reward.coins}  💎 {reward.gems}";
                    if (!string.IsNullOrEmpty(reward.specialItem))
                        _bonusRewardText.text += $"\n🎁 {reward.specialItem}";
                }
            }
        }

        private void ShowAllCompleteAnimation()
        {
            Debug.Log("🎉 ALL DAILIES COMPLETE!");
            if (_celebrationPanel != null)
                _celebrationPanel.SetActive(true);
            if (_confettiParticles != null)
                _confettiParticles.Play();
            if (_celebrationAnimator != null)
                _celebrationAnimator.SetTrigger("Celebrate");
        }

        private void ShowMysteryUnlockAnimation()
        {
            Debug.Log("🔓 MYSTERY CHALLENGE UNLOCKED!");
            // Dramatic reveal animation
        }

        // Button callbacks
        public void OnOpenDailyPanel()
        {
            if (_dailyPanel != null)
                _dailyPanel.SetActive(true);
            RefreshDisplay();
        }

        public void OnCloseDailyPanel()
        {
            if (_dailyPanel != null)
                _dailyPanel.SetActive(false);
        }

        public void OnClaimStreakReward()
        {
            if (_bonusRewardPopup != null)
                _bonusRewardPopup.SetActive(false);
        }

        public void OnCelebrationDismissed()
        {
            if (_celebrationPanel != null)
                _celebrationPanel.SetActive(false);
        }
    }
}

