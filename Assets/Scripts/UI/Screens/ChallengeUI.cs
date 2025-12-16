using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using WhatTheFunan.Challenges;

namespace WhatTheFunan.UI
{
    /// <summary>
    /// CHALLENGE UI! 🎯
    /// Shows all active challenges, leaderboards, and lets players participate!
    /// </summary>
    public class ChallengeUI : MonoBehaviour
    {
        [Header("Main Panels")]
        [SerializeField] private GameObject _challengeHubPanel;
        [SerializeField] private GameObject _weeklyPanel;
        [SerializeField] private GameObject _monthlyPanel;
        [SerializeField] private GameObject _dailyPanel;
        [SerializeField] private GameObject _leaderboardPanel;
        [SerializeField] private GameObject _submissionPanel;

        [Header("Weekly Challenge Display")]
        [SerializeField] private Text _weeklyTitle;
        [SerializeField] private Text _weeklyDescription;
        [SerializeField] private Text _weeklyTimeRemaining;
        [SerializeField] private Image _weeklyIcon;
        [SerializeField] private Text _weeklyRewards;
        [SerializeField] private Button _weeklyParticipateButton;

        [Header("Monthly Challenge Display")]
        [SerializeField] private Text _monthlyTitle;
        [SerializeField] private Text _monthlyDescription;
        [SerializeField] private Text _monthlyTimeRemaining;
        [SerializeField] private Image _monthlyIcon;
        [SerializeField] private Text _monthlyRewards;
        [SerializeField] private Button _monthlyParticipateButton;

        [Header("Daily Challenges")]
        [SerializeField] private Transform _dailyChallengeContainer;
        [SerializeField] private GameObject _dailyChallengeItemPrefab;

        [Header("Leaderboard")]
        [SerializeField] private Transform _leaderboardContainer;
        [SerializeField] private GameObject _leaderboardEntryPrefab;
        [SerializeField] private Text _yourRankText;

        [Header("Entry Viewer")]
        [SerializeField] private Transform _entryGalleryContainer;
        [SerializeField] private GameObject _entryThumbnailPrefab;

        [Header("Animation")]
        [SerializeField] private Animator _panelAnimator;
        [SerializeField] private ParticleSystem _celebrationParticles;

        private Challenge _selectedChallenge;

        private void Start()
        {
            SubscribeToEvents();
            RefreshChallengeDisplay();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (RotatingChallengeManager.Instance != null)
            {
                RotatingChallengeManager.Instance.OnNewWeeklyChallenge += OnNewWeeklyChallenge;
                RotatingChallengeManager.Instance.OnNewMonthlyChallenge += OnNewMonthlyChallenge;
                RotatingChallengeManager.Instance.OnChallengeCompleted += OnChallengeCompleted;
                RotatingChallengeManager.Instance.OnRewardEarned += OnRewardEarned;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (RotatingChallengeManager.Instance != null)
            {
                RotatingChallengeManager.Instance.OnNewWeeklyChallenge -= OnNewWeeklyChallenge;
                RotatingChallengeManager.Instance.OnNewMonthlyChallenge -= OnNewMonthlyChallenge;
                RotatingChallengeManager.Instance.OnChallengeCompleted -= OnChallengeCompleted;
                RotatingChallengeManager.Instance.OnRewardEarned -= OnRewardEarned;
            }
        }

        public void RefreshChallengeDisplay()
        {
            if (RotatingChallengeManager.Instance == null) return;

            // Update Weekly Challenge
            var weekly = RotatingChallengeManager.Instance.GetCurrentWeeklyChallenge();
            if (weekly != null)
            {
                UpdateWeeklyDisplay(weekly);
            }

            // Update Monthly Challenge
            var monthly = RotatingChallengeManager.Instance.GetCurrentMonthlyChallenge();
            if (monthly != null)
            {
                UpdateMonthlyDisplay(monthly);
            }

            // Update Daily Challenges
            var dailies = RotatingChallengeManager.Instance.GetDailyChallenges();
            UpdateDailyDisplay(dailies);
        }

        private void UpdateWeeklyDisplay(Challenge challenge)
        {
            if (_weeklyTitle != null)
                _weeklyTitle.text = challenge.definition.displayName;

            if (_weeklyDescription != null)
                _weeklyDescription.text = challenge.definition.description;

            if (_weeklyTimeRemaining != null)
            {
                var remaining = challenge.endTime - DateTime.UtcNow;
                _weeklyTimeRemaining.text = FormatTimeRemaining(remaining);
            }

            if (_weeklyRewards != null)
            {
                var rewards = challenge.definition.rewards;
                _weeklyRewards.text = $"🪙 {rewards.coins}  💎 {rewards.gems}\n🎁 {rewards.specialItem}";
            }

            if (_weeklyIcon != null && challenge.definition.icon != null)
                _weeklyIcon.sprite = challenge.definition.icon;
        }

        private void UpdateMonthlyDisplay(Challenge challenge)
        {
            if (_monthlyTitle != null)
                _monthlyTitle.text = challenge.definition.displayName;

            if (_monthlyDescription != null)
                _monthlyDescription.text = challenge.definition.description;

            if (_monthlyTimeRemaining != null)
            {
                var remaining = challenge.endTime - DateTime.UtcNow;
                _monthlyTimeRemaining.text = FormatTimeRemaining(remaining);
            }

            if (_monthlyRewards != null)
            {
                var rewards = challenge.definition.rewards;
                _monthlyRewards.text = $"🪙 {rewards.coins}  💎 {rewards.gems}\n🎁 {rewards.specialItem}\n⭐ {rewards.legendaryUnlock}";
            }
        }

        private void UpdateDailyDisplay(List<Challenge> dailies)
        {
            if (_dailyChallengeContainer == null) return;

            // Clear existing
            foreach (Transform child in _dailyChallengeContainer)
            {
                Destroy(child.gameObject);
            }

            // Add new daily challenge items
            foreach (var daily in dailies)
            {
                if (_dailyChallengeItemPrefab != null)
                {
                    var item = Instantiate(_dailyChallengeItemPrefab, _dailyChallengeContainer);
                    SetupDailyItem(item, daily);
                }
            }
        }

        private void SetupDailyItem(GameObject item, Challenge challenge)
        {
            var titleText = item.transform.Find("Title")?.GetComponent<Text>();
            var rewardText = item.transform.Find("Reward")?.GetComponent<Text>();
            var button = item.GetComponent<Button>();

            if (titleText != null)
                titleText.text = challenge.definition.displayName;

            if (rewardText != null)
                rewardText.text = $"🪙 {challenge.definition.rewards.coins}";

            if (button != null)
                button.onClick.AddListener(() => SelectChallenge(challenge));
        }

        public void SelectChallenge(Challenge challenge)
        {
            _selectedChallenge = challenge;
            ShowChallengeDetails(challenge);
        }

        private void ShowChallengeDetails(Challenge challenge)
        {
            Debug.Log($"📋 Showing details for: {challenge.definition.displayName}");
            
            // Show appropriate panel based on challenge type
            switch (challenge.definition.challengeType)
            {
                case ChallengeType.MusicCreation:
                    ShowMusicCreationUI();
                    break;
                case ChallengeType.KingdomCreation:
                    ShowKingdomCreationUI();
                    break;
                case ChallengeType.DanceChallenge:
                    ShowDanceUI();
                    break;
                case ChallengeType.CookingChallenge:
                    ShowCookingUI();
                    break;
                case ChallengeType.PhotoContest:
                    ShowPhotoUI();
                    break;
                default:
                    ShowGenericChallengeUI();
                    break;
            }
        }

        public void ShowLeaderboard(string challengeId)
        {
            if (_leaderboardPanel != null)
                _leaderboardPanel.SetActive(true);

            var leaderboard = RotatingChallengeManager.Instance?.GetLeaderboard(challengeId, 10);
            if (leaderboard == null) return;

            // Clear existing
            if (_leaderboardContainer != null)
            {
                foreach (Transform child in _leaderboardContainer)
                {
                    Destroy(child.gameObject);
                }
            }

            // Populate leaderboard
            for (int i = 0; i < leaderboard.Count; i++)
            {
                var participant = leaderboard[i];
                if (_leaderboardEntryPrefab != null && _leaderboardContainer != null)
                {
                    var entry = Instantiate(_leaderboardEntryPrefab, _leaderboardContainer);
                    SetupLeaderboardEntry(entry, i + 1, participant);
                }
            }
        }

        private void SetupLeaderboardEntry(GameObject entry, int rank, ChallengeParticipant participant)
        {
            var rankText = entry.transform.Find("Rank")?.GetComponent<Text>();
            var nameText = entry.transform.Find("Name")?.GetComponent<Text>();
            var scoreText = entry.transform.Find("Score")?.GetComponent<Text>();

            string rankEmoji = rank switch
            {
                1 => "🥇",
                2 => "🥈",
                3 => "🥉",
                _ => $"#{rank}"
            };

            if (rankText != null) rankText.text = rankEmoji;
            if (nameText != null) nameText.text = participant.playerName;
            if (scoreText != null) scoreText.text = $"👍 {participant.GetTotalVotes()}";
        }

        public void ShowEntryGallery(string challengeId)
        {
            Debug.Log($"🖼️ Showing entry gallery for challenge: {challengeId}");
            // Would display all submitted entries for voting
        }

        public void VoteForCurrentEntry(int score)
        {
            // Player votes 1-10 for current entry
            Debug.Log($"👍 Voted {score}/10");
        }

        // Navigation methods
        private void ShowMusicCreationUI()
        {
            Debug.Log("🎵 Opening Music Creation Studio...");
            // Would transition to music creation scene/panel
        }

        private void ShowKingdomCreationUI()
        {
            Debug.Log("🏰 Opening Kingdom Builder...");
            // Would transition to build mode
        }

        private void ShowDanceUI()
        {
            Debug.Log("💃 Opening Dance Floor...");
            // Would transition to dance game
        }

        private void ShowCookingUI()
        {
            Debug.Log("🍳 Opening Kitchen...");
            // Would transition to cooking game
        }

        private void ShowPhotoUI()
        {
            Debug.Log("📸 Opening Photo Mode...");
            // Would enable photo mode
        }

        private void ShowGenericChallengeUI()
        {
            Debug.Log("📋 Opening Challenge Details...");
        }

        // Event handlers
        private void OnNewWeeklyChallenge(Challenge challenge)
        {
            Debug.Log($"🎉 NEW WEEKLY CHALLENGE: {challenge.definition.displayName}");
            RefreshChallengeDisplay();
            ShowNewChallengeAnnouncement(challenge, "WEEKLY");
        }

        private void OnNewMonthlyChallenge(Challenge challenge)
        {
            Debug.Log($"👑 NEW MONTHLY CHALLENGE: {challenge.definition.displayName}");
            RefreshChallengeDisplay();
            ShowNewChallengeAnnouncement(challenge, "MONTHLY");
        }

        private void OnChallengeCompleted(Challenge challenge)
        {
            Debug.Log($"🏆 Challenge completed: {challenge.definition.displayName}");
            ShowLeaderboard(challenge.definition.challengeId);
        }

        private void OnRewardEarned(ChallengeReward reward)
        {
            _celebrationParticles?.Play();
            ShowRewardPopup(reward);
        }

        private void ShowNewChallengeAnnouncement(Challenge challenge, string type)
        {
            // Big animated announcement for new challenges
            Debug.Log($"🎊 ANNOUNCING NEW {type} CHALLENGE!");
        }

        private void ShowRewardPopup(ChallengeReward reward)
        {
            Debug.Log($"🎁 YOU WON: {reward.coins} coins, {reward.gems} gems!");
            if (!string.IsNullOrEmpty(reward.specialItem))
                Debug.Log($"🎁 SPECIAL: {reward.specialItem}");
            if (!string.IsNullOrEmpty(reward.legendaryUnlock))
                Debug.Log($"⭐ LEGENDARY: {reward.legendaryUnlock}");
        }

        private string FormatTimeRemaining(TimeSpan remaining)
        {
            if (remaining.TotalDays >= 1)
                return $"{(int)remaining.TotalDays}d {remaining.Hours}h left!";
            if (remaining.TotalHours >= 1)
                return $"{(int)remaining.TotalHours}h {remaining.Minutes}m left!";
            return $"{remaining.Minutes}m left! HURRY!";
        }

        // Button callbacks
        public void OnWeeklyParticipateClicked()
        {
            var weekly = RotatingChallengeManager.Instance?.GetCurrentWeeklyChallenge();
            if (weekly != null)
            {
                SelectChallenge(weekly);
            }
        }

        public void OnMonthlyParticipateClicked()
        {
            var monthly = RotatingChallengeManager.Instance?.GetCurrentMonthlyChallenge();
            if (monthly != null)
            {
                SelectChallenge(monthly);
            }
        }

        public void OnViewLeaderboardClicked()
        {
            if (_selectedChallenge != null)
            {
                ShowLeaderboard(_selectedChallenge.definition.challengeId);
            }
        }

        public void OnClosePanel()
        {
            _challengeHubPanel?.SetActive(false);
            _weeklyPanel?.SetActive(false);
            _monthlyPanel?.SetActive(false);
            _dailyPanel?.SetActive(false);
            _leaderboardPanel?.SetActive(false);
            _submissionPanel?.SetActive(false);
        }
    }
}

