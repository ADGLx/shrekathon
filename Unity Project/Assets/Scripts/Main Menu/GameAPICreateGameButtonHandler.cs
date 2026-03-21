using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class GameAPICreateGameButtonHandler : MonoBehaviour
{
    [Header("API")]
    [SerializeField] private GameAPI gameAPI;
    [SerializeField] private int playerCount = 2;
    [SerializeField] private float getGamePollIntervalSeconds = 1f;
    [SerializeField] private int roundDurationSeconds = 10;

    [Header("Scene")]
    [SerializeField] private string roundSceneName;

    [Header("UI")]
    [SerializeField] private TMP_Text gameIdText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text playerNamesText;
    [SerializeField] private TMP_Text errorText;

    private bool isRequestInFlight;
    private string currentGameId;
    private Coroutine getGamePollingCoroutine;

    // Hook this directly to a Unity UI Button OnClick event.
    public void CreateGameFromButton()
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

        isRequestInFlight = true;
        ClearError();
        SetLoadingState();

        CreateGameRequest request = new CreateGameRequest
        {
            amount_of_players = playerCount.ToString()
        };

        gameAPI.CreateGame(
            request,
            response =>
            {
                isRequestInFlight = false;
                ApplyCreateGameResponse(response);
                StartGetGamePolling();
            },
            error =>
            {
                isRequestInFlight = false;
                SetError($"CreateGame failed: {error}");
            });
    }

    // Hook this directly to a Unity UI Button OnClick event.
    // Starts the round without checking player count and then changes scene.
    public void StartGameFromButton()
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

        StopGetGamePolling();
        LoadGameScene();
    }

    // Hook this directly to a Unity UI Button OnClick event.
    // Ends the current game regardless of connected players.
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

    private void SetLoadingState()
    {
        if (gameIdText != null)
        {
            gameIdText.text = "Creating game...";
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"Players: {playerCount}";
        }

        if (playerNamesText != null)
        {
            playerNamesText.text = "Player names: loading...";
        }
    }

    private void ApplyCreateGameResponse(CreateGameResponse response)
    {
        if (response == null)
        {
            SetError("CreateGame returned an empty response.");
            return;
        }

        currentGameId = response.game_id;

        if (gameIdText != null)
        {
            gameIdText.text = $"Game ID: {response.game_id}";
        }

        if (playerCountText != null)
        {
            int count = response.amount_of_players > 0 ? response.amount_of_players : playerCount;
            playerCountText.text = $" {count}";
        }

        if (playerNamesText != null)
        {
            string[] names = GetNamesFromResponse(response);
            playerNamesText.text = names.Length > 0
                ? $" {string.Join(", ", names)}"
                : "none yet";
        }

        GameData gameData = new GameData
        {
            game_id = response.game_id,
            amount_of_players = response.amount_of_players,
            connected_players = GetNamesFromResponse(response)
        };
        GameAPI.Instance.StoreGameData(gameData);
    }

    private void StartGetGamePolling()
    {
        if (string.IsNullOrWhiteSpace(currentGameId))
        {
            return;
        }

        if (getGamePollingCoroutine != null)
        {
            StopCoroutine(getGamePollingCoroutine);
        }

        getGamePollingCoroutine = StartCoroutine(PollGetGameLoop());
    }

    private void StopGetGamePolling()
    {
        if (getGamePollingCoroutine == null)
        {
            return;
        }

        StopCoroutine(getGamePollingCoroutine);
        getGamePollingCoroutine = null;
    }

    private void LoadGameScene()
    {
        if (string.IsNullOrWhiteSpace(roundSceneName))
        {
            SetError("Scene name is missing. Set roundSceneName in the inspector.");
            return;
        }

        SceneManager.LoadScene(roundSceneName);
    }

    private IEnumerator PollGetGameLoop()
    {
        while (true)
        {
            bool isDone = false;
            bool shouldStopPolling = false;

            GetGameRequest request = new GetGameRequest
            {
                game_id = currentGameId
            };

            gameAPI.GetGame(
                request,
                response =>
                {
                    ApplyGetGameResponse(response);
                    shouldStopPolling = response != null && response.all_connected;
                    isDone = true;
                },
                error =>
                {
                    SetError($"GetGame failed: {error}");
                    isDone = true;
                });

            yield return new WaitUntil(() => isDone);

            if (shouldStopPolling)
            {
                Debug.Log("All players connected. Stopping GetGame polling.", this);
                getGamePollingCoroutine = null;
                yield break;
            }

            yield return new WaitForSeconds(getGamePollIntervalSeconds);
        }
    }

    private void ApplyGetGameResponse(GetGameResponse response)
    {
        if (response == null)
        {
            return;
        }

        if (playerCountText != null)
        {
            int count = response.amount_of_players > 0 ? response.amount_of_players : playerCount;
            playerCountText.text = $" {count}";
        }

        if (playerNamesText != null)
        {
            string[] names = response.connected_players ?? System.Array.Empty<string>();
            playerNamesText.text = names.Length > 0
                ? $" {string.Join(", ", names)}"
                : "none yet";
        }
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
