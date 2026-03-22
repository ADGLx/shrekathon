using System;
using System.Collections;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    // Singleton (from GameOrchestrator)
    public static RoundManager Instance { get; private set; }

    [Header("Game Configuration")]
    [SerializeField] private int totalRounds = 6;
    [SerializeField] private int playerCount = 4;

    public int TotalRounds => totalRounds;
    public int PlayerCount => playerCount;

    public int CurrentRound { get; private set; }   // 0-indexed
    public bool GameIsOver { get; private set; }

    // Components required for round management
    [Header("Round")]
    [SerializeField] protected CharacterController characterController;
    [SerializeField] protected ContractController contractController;
    [SerializeField] private PitchData[] pitchData;
    [SerializeField] private int waitBeforeStartRoundSeconds = 2;

    // GameAPI calls
    private GameAPI gameAPI;
    private GameData gameData;
    private bool isRequestInFlight;
    private string currentGameId;
    private Coroutine getGamePollingCoroutine;

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
        playerCount = gameData != null ? gameData.amount_of_players : playerCount;

        if (gameData != null)
            currentGameId = gameData.game_id;

        PlayerInputHandler.Instance.StartCoroutine(PlayerInputHandler.Instance.StartPlayerInputCollection(currentGameId));
        pitchData = Resources.LoadAll<PitchData>("PitchData");
        Debug.Log($"[RoundManager] Start — gameId={currentGameId ?? "NONE"}, playerCount={playerCount}, pitchData loaded={pitchData.Length}, totalRounds={totalRounds}", this);
        InitializeGame();
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

    public void StartRound()
    {
        PitchData currentPitch = pitchData[CurrentRound];
        Debug.Log($"[RoundManager] StartRound — round {CurrentRound + 1}/{totalRounds} | pitch='{currentPitch.characterName}', gameType={currentPitch.gameType}, gameDurationMs={currentPitch.gameDurationMs}", this);

        DealManager dealManager;
        if (currentPitch.gameType == "BRINK")
            dealManager = gameObject.AddComponent<BrinkDealManager>();
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
                //FindObjectOfType<ScoreViewer>()?.DisplayScores();
            }
            else
            {
                Debug.Log($"[RoundManager] Waiting {waitBeforeStartRoundSeconds}s before next round.", this);
                StartCoroutine(WaitThenNextRound());
            }
        };
    }

    private IEnumerator WaitThenNextRound()
    {
        yield return new WaitForSeconds(waitBeforeStartRoundSeconds);
        Debug.Log("[RoundManager] WaitThenNextRound — proceeding to NextRound.", this);
        NextRound();
    }

    // ---------------------------------------------------- //
    // ----- Client - GameAPI Interaction Logic Below ----- //

    private IEnumerator CallStartRoundAPI(int gameDurationMs)
    {
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

    void Update()
    {
    }
}
