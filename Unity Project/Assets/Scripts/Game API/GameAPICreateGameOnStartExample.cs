using UnityEngine;
using System.Collections;

public class GameAPICreateGameOnStartExample : MonoBehaviour
{
    [SerializeField] private GameAPI gameAPI;
    [SerializeField] private int playerCount = 2;
    [SerializeField] private float getGamePollIntervalSeconds = 2f;
    [SerializeField] private float waitBeforeStartRoundSeconds = 2f;
    [SerializeField] private int roundDurationSeconds = 10;
    [SerializeField] private float getRoundRetryIntervalSeconds = 1f;

    private string currentGameId;

    private void Start()
    {
        if (gameAPI == null)
        {
            Debug.LogError("GameAPICreateGameOnStartExample: GameAPI reference is missing.", this);
            return;
        }

        StartCoroutine(RunGameFlow());
    }

    private IEnumerator RunGameFlow()
    {
        yield return CreateGameStep();

        if (string.IsNullOrWhiteSpace(currentGameId))
        {
            yield break;
        }

        yield return WaitForAllPlayersConnectedStep();
        yield return WaitBeforeStartRoundStep();
        yield return StartRoundStep();

        yield return new WaitForSeconds(roundDurationSeconds);
        yield return WaitForRoundFinishedAndEndGameStep();
    }

    private IEnumerator CreateGameStep()
    {
        bool isDone = false;

        CreateGameRequest request = new CreateGameRequest
        {
            amount_of_players = playerCount.ToString()
        };

        gameAPI.CreateGame(
            request,
            response =>
            {
                currentGameId = response.game_id;
                Debug.Log($"CreateGame completed. game_id={currentGameId}", this);
                isDone = true;
            },
            error =>
            {
                Debug.LogError($"CreateGame step failed: {error}", this);
                isDone = true;
            });

        yield return new WaitUntil(() => isDone);
    }

    private IEnumerator WaitForAllPlayersConnectedStep()
    {
        while (true)
        {
            bool isDone = false;
            bool allConnected = false;

            GetGameRequest request = new GetGameRequest
            {
                game_id = currentGameId
            };

            gameAPI.GetGame(
                request,
                response =>
                {
                    allConnected = response.all_connected;
                    Debug.Log($"GetGame: connected_count={response.connected_count}/{response.amount_of_players}, all_connected={response.all_connected}", this);
                    isDone = true;
                },
                error =>
                {
                    Debug.LogError($"GetGame polling failed: {error}", this);
                    isDone = true;
                });

            yield return new WaitUntil(() => isDone);

            if (allConnected)
            {
                Debug.Log("All players connected. Proceeding to start round.", this);
                yield break;
            }

            yield return new WaitForSeconds(getGamePollIntervalSeconds);
        }
    }

    private IEnumerator WaitBeforeStartRoundStep()
    {
        Debug.Log($"Waiting {waitBeforeStartRoundSeconds:0.##} seconds before calling StartRound...", this);
        yield return new WaitForSeconds(waitBeforeStartRoundSeconds);
    }

    private IEnumerator StartRoundStep()
    {
        bool isDone = false;

        StartRoundRequest request = new StartRoundRequest
        {
            game_id = currentGameId,
            time_limit_ms = roundDurationSeconds * 1000
        };

        gameAPI.StartRound(
            request,
            response =>
            {
                Debug.Log($"StartRound completed. round_id={response.round_id}, status={response.status}", this);
                isDone = true;
            },
            error =>
            {
                Debug.LogError($"StartRound failed: {error}", this);
                isDone = true;
            });

        yield return new WaitUntil(() => isDone);
    }

    private IEnumerator WaitForRoundFinishedAndEndGameStep()
    {
        while (true)
        {
            bool isDone = false;
            bool isFinished = false;

            GetRoundRequest getRoundRequest = new GetRoundRequest
            {
                game_id = currentGameId
            };

            gameAPI.GetRound(
                getRoundRequest,
                response =>
                {
                    isFinished = string.Equals(response.status, "finished", System.StringComparison.OrdinalIgnoreCase);
                    Debug.Log($"GetRound: round_id={response.round_id}, status={response.status}", this);
                    isDone = true;
                },
                error =>
                {
                    Debug.LogError($"GetRound failed: {error}", this);
                    isDone = true;
                });

            yield return new WaitUntil(() => isDone);

            if (isFinished)
            {
                yield return EndGameStep();
                yield break;
            }

            Debug.Log($"Round not finished yet. Retrying GetRound in {getRoundRetryIntervalSeconds:0.##} seconds...", this);
            yield return new WaitForSeconds(getRoundRetryIntervalSeconds);
        }
    }

    private IEnumerator EndGameStep()
    {
        bool isDone = false;

        EndGameRequest request = new EndGameRequest
        {
            game_id = currentGameId
        };

        gameAPI.EndGame(
            request,
            response =>
            {
                Debug.Log($"EndGame completed. game_id={response.game_id}, status={response.status}", this);
                isDone = true;
            },
            error =>
            {
                Debug.LogError($"EndGame failed: {error}", this);
                isDone = true;
            });

        yield return new WaitUntil(() => isDone);
    }
}
