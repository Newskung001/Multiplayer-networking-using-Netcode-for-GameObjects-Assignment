using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MatchResultUI : MonoBehaviour
{
    [SerializeField] private TMP_Text resultText;

    [Header("Game Over UI")]
    [Tooltip("The panel GameObject that should be shown/hidden.")]
    [SerializeField] private GameObject gameOverPanel;
    
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button exitButton;
    [SerializeField] private TMP_Text rematchButtonText;
    [SerializeField] private TMP_Text gameOverStatusText;

    private bool isInitialized = false;

    private void Awake()
    {
        // 1. Verification: Is the script on the panel?
        if (this.gameObject == gameOverPanel)
        {
            Debug.LogError("[MatchResultUI] CRITICAL: This script is attached to the GameOverPanel! " +
                           "Move this script to the 'Canvas' or an always-active 'UI Manager' object. " +
                           "If the script is on the panel, it cannot wake up to show the panel when the match ends.");
        }

        // 2. Force hide immediately so you don't have to remember to hide it in Edit Mode
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }
    }

    private void Start()
    {
        if (resultText == null)
        {
            resultText = GetComponent<TMP_Text>();
        }

        if (rematchButton != null)
        {
            rematchButton.onClick.AddListener(OnRematchClicked);
        }

        if (exitButton != null)
        {
            exitButton.onClick.AddListener(OnExitClicked);
        }

        // Wait for MatchManager to be ready
        StartCoroutine(InitializeWhenReady());
    }

    private IEnumerator InitializeWhenReady()
    {
        // Wait until MatchManager is spawned and networked
        while (MatchManager.Instance == null || !MatchManager.Instance.IsSpawned)
        {
            yield return null;
        }

        // Sync initial state
        UpdateResultText("", MatchManager.Instance.MatchStatusMessage.Value);
        
        // Ensure we check the current state immediately
        bool currentlyOver = MatchManager.Instance.IsMatchOver.Value;
        OnMatchOverChanged(false, currentlyOver);
        UpdateRematchStatusText();

        // Subscribe to future changes
        MatchManager.Instance.MatchStatusMessage.OnValueChanged += UpdateResultText;
        MatchManager.Instance.IsMatchOver.OnValueChanged += OnMatchOverChanged;
        MatchManager.Instance.RematchVoteCount.OnValueChanged += UpdateRematchStatus;
        MatchManager.Instance.TotalPlayers.OnValueChanged += UpdateRematchStatus;

        isInitialized = true;
        Debug.Log($"[MatchResultUI] Initialized. Current Match Over state: {currentlyOver}");
    }

    private void Update()
    {
        if (isInitialized && MatchManager.Instance != null && MatchManager.Instance.IsMatchOver.Value)
        {
            UpdateTimerUI();
        }
    }

    private void OnDestroy()
    {
        if (isInitialized && MatchManager.Instance != null)
        {
            MatchManager.Instance.MatchStatusMessage.OnValueChanged -= UpdateResultText;
            MatchManager.Instance.IsMatchOver.OnValueChanged -= OnMatchOverChanged;
            MatchManager.Instance.RematchVoteCount.OnValueChanged -= UpdateRematchStatus;
            MatchManager.Instance.TotalPlayers.OnValueChanged -= UpdateRematchStatus;
        }
    }

    private void UpdateResultText(Unity.Collections.FixedString64Bytes previousValue, Unity.Collections.FixedString64Bytes newValue)
    {
        if (resultText != null)
        {
            resultText.text = newValue.ToString();
        }
    }

    private void UpdateRematchStatus(int previousValue, int newValue)
    {
        UpdateRematchStatusText();
    }

    private void UpdateRematchStatusText()
    {
        if (gameOverStatusText != null && MatchManager.Instance != null)
        {
            int votes = MatchManager.Instance.RematchVoteCount.Value;
            int total = MatchManager.Instance.TotalPlayers.Value;
            gameOverStatusText.text = $"Rematching {votes}/{total} voted";
        }
    }

    private void UpdateTimerUI()
    {
        if (gameOverStatusText != null && MatchManager.Instance != null)
        {
            float timeRemaining = Mathf.Max(0, MatchManager.Instance.RematchTimer.Value);
            int votes = MatchManager.Instance.RematchVoteCount.Value;
            int total = MatchManager.Instance.TotalPlayers.Value;
            gameOverStatusText.text = $"Rematching {votes}/{total} voted\nTime Remaining: {timeRemaining:F1}s";
        }
    }

    private void OnMatchOverChanged(bool previousValue, bool newValue)
    {
        Debug.Log($"[MatchResultUI] Match Over state changed to: {newValue}");
        
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(newValue);

            // Move the panel to the front of the UI hierarchy so it's not overlapped by player names
            if (newValue)
            {
                gameOverPanel.transform.SetAsLastSibling();
            }
        }

        if (newValue == false)
        {
            if (rematchButton != null) rematchButton.interactable = true;
            if (rematchButtonText != null) rematchButtonText.text = "Rematch";
            if (gameOverStatusText != null) gameOverStatusText.text = "";
        }
    }

    private void OnRematchClicked()
    {
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.VoteRematchServerRpc();
            if (rematchButton != null) rematchButton.interactable = false;
            if (rematchButtonText != null) rematchButtonText.text = "Waiting...";
        }
    }

    private void OnExitClicked()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
        }
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
