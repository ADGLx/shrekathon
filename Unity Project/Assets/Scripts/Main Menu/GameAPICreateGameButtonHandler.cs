using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.UI;

public class GameAPICreateGameButtonHandler : MonoBehaviour
{
    private const int MaxPlayers = 4;

    [Header("API")]
    [SerializeField] private GameAPI gameAPI;
    [SerializeField] private int playerCount = MaxPlayers;
    [SerializeField] private float getGamePollIntervalSeconds = 1f;
    [SerializeField] private int roundDurationSeconds = 10;

    [Header("Scene")]
    [SerializeField] private string roundSceneName;

    [Header("UI")]
    [SerializeField] private TMP_Text gameIdText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private TMP_Text playerNamesText;
    [SerializeField] private TMP_Text errorText;

    [Header("Connected Player Slots (4)")]
    [SerializeField] private TMP_Text player1NameText;
    [SerializeField] private TMP_Text player2NameText;
    [SerializeField] private TMP_Text player3NameText;
    [SerializeField] private TMP_Text player4NameText;
    [SerializeField] private Image player1StatusImage;
    [SerializeField] private Image player2StatusImage;
    [SerializeField] private Image player3StatusImage;
    [SerializeField] private Image player4StatusImage;
    [SerializeField] private Color disconnectedPlayerColor = Color.gray;
    [SerializeField] private Color connectedPlayerColor = Color.white;
    [SerializeField] private string waitingForPlayerLabel = "Waiting...";

    private bool isRequestInFlight;
    private string currentGameId;
    private Coroutine getGamePollingCoroutine;

    private void Awake()
    {
        playerCount = MaxPlayers;
        UpdateConnectedPlayerSlots(Array.Empty<string>());
    }

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
            amount_of_players = MaxPlayers.ToString()
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
                    playerNamesText.text = string.Empty;
                }

                UpdateConnectedPlayerSlots(Array.Empty<string>());
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

        UpdateConnectedPlayerSlots(Array.Empty<string>());
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
            int count = response.amount_of_players > 0 ? response.amount_of_players : MaxPlayers;
            playerCountText.text = $" {count}";
        }

        string[] names = GetNamesFromResponse(response);
        UpdateConnectedPlayerSlots(names);

        if (playerNamesText != null)
        {
            playerNamesText.text = names.Length > 0
                ? $" {string.Join(", ", names)}"
                : string.Empty;
        }

        gameAPI.StoreGameData(new GameData
        {
            game_id           = response.game_id,
            amount_of_players = response.amount_of_players,
            connected_players = names
        });
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
            int count = response.amount_of_players > 0 ? response.amount_of_players : MaxPlayers;
            playerCountText.text = $" {count}";
        }

        string[] names = response.connected_players ?? Array.Empty<string>();
        UpdateConnectedPlayerSlots(names);

        if (playerNamesText != null)
        {
            playerNamesText.text = names.Length > 0
                ? $" {string.Join(", ", names)}"
                : string.Empty;
        
        }

        GameData existing = gameAPI.CurrentGameData;

        gameAPI.StoreGameData(new GameData
        {
            game_id           = currentGameId,
            amount_of_players = existing?.amount_of_players ?? MaxPlayers,
            connected_players = names
        });
    }

    private void UpdateConnectedPlayerSlots(string[] names)
    {
        SetPlayerSlot(player1NameText, player1StatusImage, GetPlayerNameAtIndex(names, 0));
        SetPlayerSlot(player2NameText, player2StatusImage, GetPlayerNameAtIndex(names, 1));
        SetPlayerSlot(player3NameText, player3StatusImage, GetPlayerNameAtIndex(names, 2));
        SetPlayerSlot(player4NameText, player4StatusImage, GetPlayerNameAtIndex(names, 3));
    }

    private string GetPlayerNameAtIndex(string[] names, int index)
    {
        if (names == null || index < 0 || index >= names.Length)
        {
            return null;
        }

        return names[index];
    }

    private void SetPlayerSlot(TMP_Text nameText, Image statusImage, string playerName)
    {
        bool isConnected = IsConnectedPlayerName(playerName);

        if (nameText != null)
        {
            nameText.text = isConnected ? playerName.Trim() : waitingForPlayerLabel;
        }

        if (statusImage != null)
        {
            statusImage.color = isConnected ? connectedPlayerColor : disconnectedPlayerColor;
        }
    }

    private bool IsConnectedPlayerName(string playerName)
    {
        if (string.IsNullOrWhiteSpace(playerName))
        {
            return false;
        }

        string normalized = playerName.Trim();

        return !normalized.Equals("none", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals("none yet", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals("null", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals("undefined", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals("waiting", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals("waiting...", StringComparison.OrdinalIgnoreCase)
            && !normalized.Equals("-", StringComparison.OrdinalIgnoreCase);
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

        return Array.Empty<string>();
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
