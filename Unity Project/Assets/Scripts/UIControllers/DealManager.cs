using System;
using System.Collections.Generic;
using TMPro;    
using UnityEngine;

public abstract class DealManager : MonoBehaviour
{
    // ------------------------------------------------------------------ //
    //  Component References (auto-fetched or wired in Inspector)
    // ------------------------------------------------------------------ //
    [Header("Sub-components (auto-resolved if left empty)")]
    [SerializeField] protected CharacterController characterController;
    [SerializeField] protected ContractController contractController;
    [SerializeField] protected int gameDurationMs;
    [SerializeField] protected string gameType;

    [SerializeField] protected int gamePoints;
    [Header("Round UI")]
    [SerializeField] private TMP_Text roundTimerText;
    [SerializeField] private Color roundTimerStartColor = Color.white;
    [SerializeField] private Color roundTimerEndColor = Color.red;

    private Coroutine roundTimerCoroutine;
    private float roundEndTime;
    private float roundDurationSeconds = 0.001f;
    private bool isRoundTimerRunning;

    // The data asset currently loaded into this pitch
    public PitchData CurrentData { get; private set; }

    // ------------------------------------------------------------------ //
    //  Unity Lifecycle
    // ------------------------------------------------------------------ //
    
    public event Action OnDestroyed;

    public void Init(CharacterController characterControllerRef, ContractController contractControllerRef)
    {
        this.characterController = characterControllerRef;
        this.contractController = contractControllerRef;
        Debug.Log($"[DealManager] Init — characterController={(characterControllerRef != null ? "set" : "NULL")}, contractController={(contractControllerRef != null ? "set" : "NULL")}", this);
    }

    public void SetRoundTimerText(TMP_Text timerText)
    {
        roundTimerText = timerText;
    }

    public void SetRoundTimerColors(Color startColor, Color endColor)
    {
        roundTimerStartColor = startColor;
        roundTimerEndColor = endColor;
    }

    private void Awake()
    {
        // Fall back to GetComponent if Inspector references are not set
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (contractController == null)
            contractController  = GetComponent<ContractController>();

        Debug.Log($"[DealManager] Awake — characterController={(characterController != null ? "found" : "MISSING")}, contractController={(contractController != null ? "found" : "MISSING")}", this);
    }

    protected virtual void Update() {
        // Debug.Log($"[DealManager] Update", this); this works
        RoundLogic();
    }

    // ------------------------------------------------------------------ //
    //  Public Lifecycle API  (called by RoundController)
    // ------------------------------------------------------------------ //

    /// <summary>
    /// Load a PitchData asset into both sub-components without showing anything yet.
    /// Call this before transitioning into the pitch scene so data is ready instantly.
    /// </summary>
    /// 

    public void Load(PitchData data)
    {
        // Populates the contract with data
        if (data == null)
        {
            Debug.LogError("[DealManager] Load called with null PitchData.");
            return;
        }

        CurrentData = data;
        characterController.Populate(data);
        contractController.Populate(data);
        gameDurationMs = data.gameDurationMs;
        gameType = data.gameType;
        gamePoints = data.points;

        displayDeal();
        Debug.Log($"[DealManager] Loaded pitch: {data.characterName} — {data.contractTitle} | gameType={gameType}, gameDurationMs={gameDurationMs}");
        Debug.Log($"[DealManager] Starting EndGame timer: {gameDurationMs}ms", this);
        if (roundTimerText == null)
            Debug.LogWarning("[DealManager] roundTimerText is not assigned. Assign a TMP_Text in the inspector to show the timer.", this);
        StartRoundTimer(gameDurationMs / 1000f);
        Invoke(nameof(EndGame), gameDurationMs / 1000f);
    }

    public void EndGame()
    {
        StopRoundTimer();
        StartCoroutine(EndGameRoutine());
    }

    private void StartRoundTimer(float durationSeconds)
    {
        StopRoundTimer();

        isRoundTimerRunning = true;
        roundDurationSeconds = Mathf.Max(0.001f, durationSeconds);
        roundEndTime = Time.time + roundDurationSeconds;
        UpdateRoundTimerText(durationSeconds);
        roundTimerCoroutine = StartCoroutine(UpdateRoundTimerRoutine());
    }

    private void StopRoundTimer()
    {
        isRoundTimerRunning = false;
        if (roundTimerCoroutine != null)
        {
            StopCoroutine(roundTimerCoroutine);
            roundTimerCoroutine = null;
        }
        UpdateRoundTimerText(0f);
    }

    private System.Collections.IEnumerator UpdateRoundTimerRoutine()
    {
        while (isRoundTimerRunning)
        {
            float remaining = Mathf.Max(0f, roundEndTime - Time.time);
            UpdateRoundTimerText(remaining);

            if (remaining <= 0f)
                yield break;

            yield return null;
        }
    }

    private void UpdateRoundTimerText(float secondsRemaining)
    {
        if (roundTimerText == null)
            return;

        float safeSeconds = Mathf.Max(0f, secondsRemaining);
        float progress = 1f - Mathf.Clamp01(safeSeconds / roundDurationSeconds);
        roundTimerText.text = $"{safeSeconds:0.000}s";
        roundTimerText.color = Color.Lerp(roundTimerStartColor, roundTimerEndColor, progress);
    }

    private System.Collections.IEnumerator EndGameRoutine()
    {
        Debug.Log($"[DealManager] Game ended for pitch: {CurrentData.characterName} — {CurrentData.contractTitle}");

        string currentGameId = GameAPI.Instance?.CurrentGameData?.game_id;
        if (PlayerInputHandler.Instance != null && !string.IsNullOrWhiteSpace(currentGameId))
        {
            Debug.Log("[DealManager] Timer finished. Requesting final round input from API.", this);
            yield return PlayerInputHandler.Instance.FetchRoundInputOnce(currentGameId);
        }
        else
        {
            Debug.LogWarning("[DealManager] Skipping final round input fetch because PlayerInputHandler or game_id is missing.", this);
        }

        string[] players = RoundManager.Instance.GetConnectedPlayers();
        RoundManager.Instance.ResetPlayerStatuses();
        Dictionary<string, List<PlayerPress>> playerPress = PlayerInputHandler.Instance != null
            ? PlayerInputHandler.Instance.GetPlayerPress()
            : new Dictionary<string, List<PlayerPress>>();

        Debug.Log($"[DealManager] Starting score calculation. gameType={gameType}, players={players.Length}, tapPayloadPlayers={playerPress.Count}", this);
        Dictionary<string, int> roundScores = new Dictionary<string, int>();
        foreach (string player in players)
        {
            int score = CalculateScore(player);
            roundScores[player] = score;
            RoundManager.Instance.UpdatePlayerPoints(player, score);
            Debug.Log($"[DealManager] Player '{player}' scored {score} points this round.", this);
        }
        Debug.Log($"[DealManager] Round {(RoundManager.Instance.CurrentRound + 1)} gained points: {string.Join(", ", roundScores)}", this);

        Debug.Log($"[DealManager] Firing OnDestroyed event.", this);
        hideDeal();
        OnDestroyed?.Invoke();
        Destroy(this);
    }

    public void displayDeal()
    {
        Debug.Log("[DealManager] displayDeal — showing character and contract.", this);
        characterController.Show();
        contractController.Show();
    }

    public void hideDeal()
    {
        Debug.Log("[DealManager] hideDeal — hiding character and contract.", this);
        characterController.Hide();
        contractController.Hide();
    }

    // ------------------------------------------------------------------ //
    //  Abstract Methods (game logic must be implimented by concrete subclasses)
    // ------------------------------------------------------------------ //

    // Abstract class
    protected abstract void RoundLogic();
    protected abstract int CalculateScore(string playerKey);
}
