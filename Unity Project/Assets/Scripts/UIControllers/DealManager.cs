using System;
using System.Collections.Generic;
using TMPro;    
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

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

        Debug.Log($"[DealManager] Loaded pitch: {data.characterName} — {data.contractTitle} | gameType={gameType}, gameDurationMs={gameDurationMs}");
        //@todo: Perform this only after API request made for next round to begin
        Debug.Log($"[DealManager] Starting EndGame timer: {gameDurationMs}ms", this);
        Invoke(nameof(EndGame), gameDurationMs / 1000f);
    }

    public void EndGame()
    {
        Debug.Log($"[DealManager] Game ended for pitch: {CurrentData.characterName} — {CurrentData.contractTitle}");

        string[] players = RoundManager.Instance.GetConnectedPlayers();
        RoundManager.Instance.ResetPlayerStatuses();
        foreach (string player in players)
        {
            int score = CalculateScore(player);
            RoundManager.Instance.UpdatePlayerPoints(player, score);
            Debug.Log($"[DealManager] Player '{player}' scored {score} points this round.", this);
        }

        Debug.Log($"[DealManager] Firing OnDestroyed event.", this);
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
