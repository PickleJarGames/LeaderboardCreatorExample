using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Dan.Main;
using UnityEngine.UI;
using System.Collections;

namespace LeaderboardCreatorDemo
{
    public class LeaderboardManager : MonoBehaviour
    {
        [Header("Leaderboard Settings")]
        public string publicLeaderboardKey; // Set this in the Inspector

        [Header("UI References")]
        public TMP_InputField playerNameInput;
        public Transform leaderboardContent; // Assign Content object of ScrollView
        public GameObject leaderboardEntryPrefab; // Assign your prefab
        public TextMeshProUGUI fetchingDataText; // Assign in Inspector
        public TextMeshProUGUI rText; // Assign in Inspector
        public ScrollRect leaderboardScrollRect; // Assign in Inspector
        private string lastSubmittedPlayerName = null; // Store the last submitted player name

        [Header("Audio")]
        public AudioClip submitScoreSFX; // Assign in Inspector

        private const int MAX_TIME = 999999; // or higher if needed

        // Call this to submit a score
        public void SubmitScore(int score)
        {
            string playerName = playerNameInput != null ? playerNameInput.text : "Player";
            lastSubmittedPlayerName = playerName; // Store the last submitted player name
            if (playerNameInput != null)
                playerNameInput.interactable = false;
            LeaderboardCreator.UploadNewEntry(publicLeaderboardKey, playerName, score, OnUploadSuccessCallback, OnUploadFailed);
        }

        // Overloaded method to submit a score with specified player name and time in seconds
        public void SubmitScore(string playerName, int timeInSeconds)
        {
            lastSubmittedPlayerName = playerName;
            int invertedScore = MAX_TIME - timeInSeconds;
            Debug.Log($"Attempting to upload: key={publicLeaderboardKey}, name={playerName}, score={invertedScore} (actual time: {timeInSeconds})");
            LeaderboardCreator.UploadNewEntry(publicLeaderboardKey, playerName, invertedScore, OnUploadSuccessCallback, OnUploadFailed);
        }

        private void OnUploadSuccessCallback(bool success)
        {
            if (success)
            {
                Debug.Log("Score uploaded successfully!");
                DownloadScores();
            }
            else
            {
                Debug.LogError("Score upload failed unexpectedly.");
            }
        }

        private void OnUploadFailed(string error)
        {
            Debug.LogError("Failed to upload score: " + error);
        }

        // Call this to download and display the leaderboard
        public void DownloadScores()
        {
            Debug.Log("Downloading leaderboard for key: " + publicLeaderboardKey);
            if (fetchingDataText != null)
            {
                fetchingDataText.text = "Fetching Data...";
                fetchingDataText.gameObject.SetActive(true);
            }

            LeaderboardCreator.GetLeaderboard(publicLeaderboardKey, OnLeaderboardDownloaded, OnDownloadFailed);
        }

        private void OnLeaderboardDownloaded(Dan.Models.Entry[] entries)
        {
            if (fetchingDataText != null)
                fetchingDataText.gameObject.SetActive(false);

            foreach (Transform child in leaderboardContent)
                Destroy(child.gameObject);

            int rank = 1;
            int playerIndex = -1;
            for (int i = 0; i < entries.Length; i++)
            {
                var entry = entries[i];
                GameObject entryObj = Instantiate(leaderboardEntryPrefab, leaderboardContent);
                TMP_Text[] texts = entryObj.GetComponentsInChildren<TMP_Text>();
                if (texts.Length >= 3)
                {
                    texts[0].text = $"{rank}.";
                    texts[1].text = entry.Username;
                    // Convert back to actual time
                    int actualTime = MAX_TIME - entry.Score;
                    texts[2].text = FormatTime(actualTime);
                }
                if (!string.IsNullOrEmpty(lastSubmittedPlayerName) && entry.Username == lastSubmittedPlayerName)
                    playerIndex = i;
                rank++;
            }

            if (playerIndex >= 0 && leaderboardScrollRect != null)
                StartCoroutine(ScrollToPlayerEntry(playerIndex));
        }

        private IEnumerator ScrollToPlayerEntry(int playerIndex)
        {
            // Wait for end of frame so layout is updated
            yield return new WaitForEndOfFrame();

            int totalEntries = leaderboardContent.childCount;
            if (totalEntries <= 1)
                leaderboardScrollRect.verticalNormalizedPosition = 1f;
            else
            {
                // 1 = top, 0 = bottom
                float normalizedPos = 1f - (playerIndex / (float)(totalEntries - 1));
                leaderboardScrollRect.verticalNormalizedPosition = Mathf.Clamp01(normalizedPos);
            }
        }

        // Helper to format score as time (e.g., seconds to mm:ss)
        private string FormatTime(int score)
        {
            int minutes = score / 60;
            int seconds = score % 60;
            return $"{minutes:00}:{seconds:00}";
        }

        private void OnDownloadFailed(string error)
        {
            if (fetchingDataText != null)
            {
                fetchingDataText.text = "Download Failed";
                fetchingDataText.gameObject.SetActive(true);
            }

            Debug.LogError("Failed to download leaderboard: " + error);
        }

        public void UpdateRTextVisibility()
        {
            if (rText != null)
            {
                rText.gameObject.SetActive(SceneManager.GetActiveScene().name == "Pizzeria");
            }
        }

        public void PlaySubmitScoreSFX()
        {
            if (submitScoreSFX != null && AudioManager.Instance != null)
                AudioManager.Instance.PlaySFX(submitScoreSFX, 1f);
        }
    }
}
