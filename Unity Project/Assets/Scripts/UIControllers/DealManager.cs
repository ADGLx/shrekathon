using System;
using System.Collections.Generic;
using TMPro;    
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.Experimental.GraphView.GraphView;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(ContractController))]
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

    // The data asset currently loaded into this pitch
    public PitchData CurrentData { get; private set; }

    // ------------------------------------------------------------------ //
    //  Unity Lifecycle
    // ------------------------------------------------------------------ //
    
    public event Action OnDestroyed;

    private void Awake()
    {
        // Fall back to GetComponent if Inspector references are not set
        if (characterController == null)
            characterController = GetComponent<CharacterController>();
        if (contractController == null)
            contractController  = GetComponent<ContractController>();
    }

    protected virtual void Update() {
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

        Debug.Log($"[DealManager] Loaded pitch: {data.characterName} — {data.contractTitle}");
        //@todo: Perform this only after API request made for next round to begin
        Invoke(nameof(EndGame), gameDurationMs);
    }

    public void EndGame()
    {
        Debug.Log($"[DealManager] Game ended for pitch: {CurrentData.characterName} — {CurrentData.contractTitle}");
        //@todo: Sends player scores to game manager, reset any GUI effects applied
        /*
        Dictionary<string, int> playerScores = new Dictionary<string, int>();
        foreach (KeyValuePair<string, PlayerData> player in players)
        {
            playerScores[player.Key] = CalculateScore(player.Key);
            GameManager.Instance.GetPlayerGUI(player.Key).ResetState();
        }

        GameManager.Instance.UpdateScore(playerScores);
        GameManager.Instance.EndGame();
        */

        OnDestroyed?.Invoke();
        Destroy(this);
    }

    public void displayDeal()
    {
        characterController.Show();
        contractController.Show();
    }
    public void hideDeal()
    {
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
