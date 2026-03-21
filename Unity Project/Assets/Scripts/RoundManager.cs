using System;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine;

public class RoundManager : MonoBehaviour
{
    // Components required for round management
    [SerializeField] protected CharacterController characterController;
    [SerializeField] protected ContractController contractController;
    [SerializeField] private PitchData[] pitchData;
    [SerializeField] private int numberOfRounds;
    private int currentRound;

    // GameAPI calls
    private GameAPI gameAPI;
    private GameData gameData;
    private bool isRequestInFlight;
    private string currentGameId;
    private Coroutine getGamePollingCoroutine;

    void Start()
    {
        this.gameAPI = GameObject.Find("GameAPI").GetComponent<GameAPI>();
        gameData = GameAPI.Instance?.CurrentGameData;
        pitchData = Resources.LoadAll<PitchData>("PitchData");
        currentRound = 0;
        StartRound();
    }

    public void StartRound()
    {
        PitchData currentPitch = pitchData[currentRound];
        currentRound++;

        // Handles isnstantiation
        DealManager dealManager;
        if (currentPitch.gameType == "BRINK")
            dealManager = gameObject.AddComponent<BrinkDealManager>();
        else
            throw new Exception($"Unsupported game type: {currentPitch.gameType}");

        dealManager.GetComponent<DealManager>().Init(characterController, contractController);
        dealManager.Load(pitchData[0]);
        WaitUntil dealIsLoaded = new WaitUntil(() => dealManager.CurrentData != null);
        StartCoroutine(dealIsLoaded);

        dealManager.OnDestroyed += EndRound();
        dealManager.displayDeal();
    }

    System.Action EndRound()
    {
        return () => {
            if (numberOfRounds >= currentRound)
                print("TODO: End of Game Logic;");
            else
                StartRound();
        };
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    // ---------------------------------------------------- //
    // ----- Client - GameAPI Interaction Logic Below ----- //
    void StartRound()
    {
        if (isRequestInFlight)
        {
            return;
        }

        if (gameAPI == null)
        {
            SetError("GameAPI reference is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(currentGameId))
        {
            SetError("No game_id available. Create a game first.");
            return;
        }

        isRequestInFlight = true;
        ClearError();

        StartRoundRequest request = new StartRoundRequest
        {
            game_id = currentGameId,
            time_limit_ms = roundDurationSeconds * 1000
        };

        gameAPI.StartRound(
            request,
            response =>
            {
                isRequestInFlight = false;

            },
            error =>
            {
                isRequestInFlight = false;
                SetError($"StartRound failed: {error}");
            });
    }

    public void EndGameFromButton()
    {
        if (isRequestInFlight)
        {
            return;
        }

        if (gameAPI == null)
        {
            SetError("GameAPI reference is missing.");
            return;
        }

        if (string.IsNullOrWhiteSpace(currentGameId))
        {
            SetError("No game_id available. Create a game first.");
            return;
        }

        isRequestInFlight = true;
        ClearError();

        EndGameRequest request = new EndGameRequest
        {
            game_id = currentGameId
        };

        gameAPI.EndGame(
            request,
            response =>
            {
                isRequestInFlight = false;
                StopGetGamePolling();

                if (playerNamesText != null)
                {
                    playerNamesText.text = "none yet";
                }
            },
            error =>
            {
                isRequestInFlight = false;
                SetError($"EndGame failed: {error}");
            });
    }

    private string[] GetNamesFromResponse(CreateGameResponse response)
    {
        if (response.connected_players != null && response.connected_players.Length > 0)
        {
            return response.connected_players;
        }

        if (response.player_names != null && response.player_names.Length > 0)
        {
            return response.player_names;
        }

        return System.Array.Empty<string>();
    }

    private void SetError(string message)
    {
        Debug.LogError($"GameAPICreateGameButtonHandler: {message}", this);

        if (errorText != null)
        {
            errorText.text = message;
        }
    }

    private void ClearError()
    {
        if (errorText != null)
        {
            errorText.text = string.Empty;
        }
    }

    private void OnDisable()
    {
        StopGetGamePolling();
    }
}
/**
* // In the spawning class
* RoundInstance round = gameObject.AddComponent<RoundInstance>();
* round.Init(30f);
*/