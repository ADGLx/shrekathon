using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Versioning;
using UnityEngine;
using UnityEngine.SceneManagement;

public class RoundManager : MonoBehaviour
{
    // Singleton (from GameOrchestrator)
    public static RoundManager Instance { get; private set; }

    [Header("Game Configuration")]
    [SerializeField] private int totalRounds = 6;

    public int TotalRounds => totalRounds;

    public int CurrentRound { get; private set; }   // 0-indexed
    public bool GameIsOver { get; private set; }

    // Components required for round management
    [Header("Round")]
    [SerializeField] protected CharacterController characterController;
    [SerializeField] protected ContractController contractController;
    [SerializeField] private PitchData[] pitchData;
    [SerializeField] private int waitBeforeStartRoundSeconds = 2;
    [SerializeField] private bool playAudioClips = true;
    [SerializeField] private AudioClip[] betweenRoundClips;
    private int _lastClipIndex = -1;

    // GameAPI calls
    private GameAPI gameAPI;
    private GameData gameData;
    private bool isRequestInFlight;
    private string currentGameId;
    private Coroutine getGamePollingCoroutine;

    [Header("Round")]
    // Tracking of player portraits and points in game
    [SerializeField] GameObject playerControllerPrefab;  // assign in inspector
    [SerializeField] List<PlayerIconData> playerIconDatas;  // assign in inspector player portraits in hierarchy order (e.g. Player1, Player2, etc.)
    [SerializeField] GameObject[] playerControllerContainer; // assign in inspector - parent object to hold instantiated PlayerControllers
    protected Dictionary<string, PlayerController> playerControllers = new Dictionary<string, PlayerController>();
    protected Dictionary<string, int> playerPoints = new Dictionary<string, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // kill duplicate (e.g. if scene reloads)
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        gameAPI = GameAPI.Instance;
        gameData = GameAPI.Instance?.CurrentGameData;

        if (gameData != null)
            currentGameId = gameData.game_id;
        else
            Debug.LogError("[RoundManager] Start — gameData is null", this);
        
        Debug.Log($"[RoundManager] Start — gameId={currentGameId ?? "NONE"}, playerCount={gameData.connected_players.Count()}", this);
        
        pitchData = Resources.LoadAll<PitchData>("PitchData");
        ShufflePitchData();

        AssignPlayerPortraits();
        InitializeGame();
    }

    private void ShufflePitchData()
    {
        for (int i = pitchData.Length - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (pitchData[i], pitchData[j]) = (pitchData[j], pitchData[i]);
        }
    }

    public void InitializeGame()
    {
        CurrentRound = 0;
        GameIsOver = false;
        Debug.Log("[RoundManager] Game initialised.");
        StartRound();
    }

    public void NextRound()
    {
        CurrentRound++;
        StartRound();
    }

    private void PlayRandomBetweenRoundClip()
    {
        if (!playAudioClips) return;
        if (betweenRoundClips == null || betweenRoundClips.Length == 0) return;

        int index = _lastClipIndex;
        if (betweenRoundClips.Length > 1)
            while (index == _lastClipIndex)
                index = UnityEngine.Random.Range(0, betweenRoundClips.Length);
        else
            index = 0;

        _lastClipIndex = index;
        AudioManager.Instance.playVoice(betweenRoundClips[index]);
    }

    public void StartRound()
    {
        PlayRandomBetweenRoundClip();
        PitchData currentPitch = pitchData[CurrentRound];
        Debug.Log($"[RoundManager] StartRound — round {CurrentRound + 1}/{totalRounds} | pitch='{currentPitch.characterName}', gameType={currentPitch.gameType}, gameDurationMs={currentPitch.gameDurationMs}", this);

        DealManager dealManager;
        if (currentPitch.gameType == "MAX_TAP")
            dealManager = gameObject.AddComponent<MaxTapDeal>();
        else if (currentPitch.gameType == "MIN_TAP")
            dealManager = gameObject.AddComponent<MinTapDeal>();
        else if (currentPitch.gameType == "RANGE_TAP")
            dealManager = gameObject.AddComponent<RangeTapDeal>();
        else
            throw new Exception($"Unsupported game type: {currentPitch.gameType}");

        dealManager.Init(characterController, contractController);
        dealManager.Load(currentPitch);
        dealManager.OnDestroyed += EndRound();
        StartCoroutine(WaitForLoadThenDisplay(dealManager));
        StartCoroutine(CallStartRoundAPI(currentPitch.gameDurationMs));
    }

    private IEnumerator WaitForLoadThenDisplay(DealManager dealManager)
    {
        Debug.Log("[RoundManager] WaitForLoadThenDisplay — waiting for CurrentData...", this);
        yield return new WaitUntil(() => dealManager.CurrentData != null);
        Debug.Log("[RoundManager] WaitForLoadThenDisplay — data ready, calling displayDeal.", this);
        dealManager.displayDeal();
    }

    private System.Action EndRound()
    {
        return () =>
        {
            Debug.Log($"[RoundManager] EndRound fired — round {CurrentRound + 1}/{totalRounds}", this);
            if (CurrentRound + 1 >= totalRounds)
            {
                Debug.Log("[RoundManager] All rounds complete — triggering EndGame.", this);
                GameIsOver = true;
                EndGame();
            }
            else
            {
                Debug.Log($"[RoundManager] Waiting {waitBeforeStartRoundSeconds}s before next round.", this);
                StartCoroutine(WaitThenNextRound());
            }
        };
    }

    // THIS IS THE PAUSE STATE!
    private IEnumerator WaitThenNextRound()
    {
        //characterController.Populate(betweenPitchScenes[CurrentRound]); // show next character silhouette in between rounds
        yield return new WaitForSeconds(waitBeforeStartRoundSeconds);
        Debug.Log("[RoundManager] WaitThenNextRound — proceeding to NextRound.", this);
        NextRound();
    }

    // ---------------------------------------------------- //
    // ----- Client - GameAPI Interaction Logic Below ----- //

    private IEnumerator CallStartRoundAPI(int gameDurationMs)
    {
        Debug.Log("[RoundManager] CallStartRoundAPI — attempting to call StartRound API.", this);
        if (isRequestInFlight) yield break;

        if (gameAPI == null)
        {
            Debug.LogError("[RoundManager] GameAPI reference is missing.", this);
            yield break;
        }

        if (string.IsNullOrWhiteSpace(currentGameId))
        {
            Debug.LogError("[RoundManager] No game_id available.", this);
            yield break;
        }

        isRequestInFlight = true;
        bool isDone = false;

        StartRoundRequest request = new StartRoundRequest
        {
            game_id = currentGameId,
            time_limit_ms = gameDurationMs
        };

        gameAPI.StartRound(
            request,
            response =>
            {
                Debug.Log($"StartRound completed. round_id={response.round_id}, status={response.status}", this);
                isRequestInFlight = false;
                isDone = true;
            },
            error =>
            {
                Debug.LogError($"[RoundManager] StartRound API failed: {error}", this);
                isRequestInFlight = false;
                isDone = true;
            });

        yield return new WaitUntil(() => isDone);
    }

    public void EndGame()
    {
        if (isRequestInFlight) return;

        if (gameAPI == null)
        {
            Debug.LogError("[RoundManager] GameAPI reference is missing.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(currentGameId))
        {
            Debug.LogError("[RoundManager] No game_id available.", this);
            return;
        }

        isRequestInFlight = true;

        EndGameRequest request = new EndGameRequest
        {
            game_id = currentGameId
        };

        gameAPI.EndGame(
            request,
            response =>
            {
                isRequestInFlight = false;
                GameIsOver = true;

                // Find the player with the highest points
                string winnerId = null;
                int highestPoints = int.MinValue;
                foreach (var kvp in playerPoints)
                {
                    if (kvp.Value > highestPoints)
                    {
                        highestPoints = kvp.Value;
                        winnerId = kvp.Key;
                    }
                }

                EndGameData endGameData = new EndGameData();
                if (winnerId != null)
                {
                    int winnerIndex = Array.IndexOf(gameData.connected_players, winnerId);
                    if (winnerIndex >= 0 && winnerIndex < playerIconDatas.Count)
                    {
                        endGameData.winnerName   = winnerId;
                        endGameData.winnerSprite = playerIconDatas[winnerIndex].characterSprite;
                    }
                    else
                    {
                        endGameData.winnerName = winnerId;
                    }
                    endGameData.winnerPoints = highestPoints;
                }

                gameAPI.StoreEndGameData(endGameData);
                SceneManager.LoadScene("Results");
            },
            error =>
            {
                isRequestInFlight = false;
                Debug.LogError($"[RoundManager] EndGame API failed: {error}", this);
            });
    }

    private void StopGetGamePolling()
    {
        if (getGamePollingCoroutine == null) return;
        StopCoroutine(getGamePollingCoroutine);
        getGamePollingCoroutine = null;
    }

    private void OnDisable()
    {
        StopGetGamePolling();
    }

    // Tracking Player Portraits and Points (for future use in score viewer, etc.)
    void AssignPlayerPortraits()
    {
        if (gameData?.connected_players == null)
        {
            Debug.LogError("[RoundManager] AssignPlayerPortraits — no connected players in gameData.", this);
            return;
        }

        for (int i = 0; i < gameData.connected_players.Length; i++)
        {
            string playerId = gameData.connected_players[i];

            GameObject controllerObj = Instantiate(playerControllerPrefab, playerControllerContainer[i].transform);
            PlayerController controller = controllerObj.GetComponent<PlayerController>();

            if (i < playerIconDatas.Count)
                controller.Populate(playerIconDatas[i]);
            else
                Debug.LogWarning($"[RoundManager] No PlayerIconData for player index {i} ({playerId}).", this);

            playerControllers[playerId] = controller;
        }

        Debug.Log($"[RoundManager] AssignPlayerPortraits — assigned {playerControllers.Count} player portraits.", this);
    }

    public void UpdatePlayerPoints(string playerId, int points)
    {
        if (!playerControllers.ContainsKey(playerId))
        {
            Debug.LogError($"[RoundManager] UpdatePlayerPoints — no PlayerController found for playerId '{playerId}'.", this);
            return;
        }
        if (!playerPoints.ContainsKey(playerId))
            playerPoints[playerId] = 0;

        playerPoints[playerId] += points;
        playerControllers[playerId].UpdatePlayerPoints(playerPoints[playerId]);
    }

    public void SetPlayerStatus(string playerId, bool isPressed, bool isLocked)
    {
        if (!playerControllers.ContainsKey(playerId))
        {
            Debug.LogError($"[RoundManager] SetPlayerStatus — no PlayerController found for playerId '{playerId}'.", this);
            return;
        }

        playerControllers[playerId].SetPlayerStatus(isPressed, isLocked);
    }

    public void ResetPlayerStatuses()
    {
        foreach (var playerId in playerControllers.Keys)
        {
            playerControllers[playerId].SetPlayerStatus(false, false);
        }
    }

    public string[] GetConnectedPlayers()
    {
        if (gameAPI == null || gameAPI.CurrentGameData == null) return new string[0];
        return gameAPI.CurrentGameData.connected_players ?? new string[0];
    }

    void Update()
    {
    }
}
