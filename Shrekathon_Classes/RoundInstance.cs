/**
* // In the spawning class
* RoundInstance round = gameObject.AddComponent<RoundInstance>();
* round.Init(30f);
*/

using System.Collections.Generic;

public abstract class RoundInstance : MonoBehaviour
{
    [SerializeField] private float gameDurationMiliseconds;

    protected readonly Dictionary<string, PressData[]> players = new Dictionary<string, PressData[]>();
    
    protected virtual void Invoke(float durationMiliseconds)
    {
        GameManager.Instance.StartRound();
        gameDurationMiliseconds =  durationMiliseconds / 1000f;
        Invoke(nameof(EndGame), gameDurationMiliseconds);
    }

    protected virtual void Update()
    {
        UpdatePlayerPress();
    }

    private void EndGame()
    {
        // Sends player scores to game manager
        Dictionary<string, int> playerScores = new Dictionary<string, int>();
        foreach (KeyValuePair<string, PlayerData> player in players) {
            playerScores[player.Key] = CalculateScore(player.Key);
            GameManager.Instance.GetPlayerGUI(player.Key).ResetState();
        }

        GameManager.Instance.UpdateScore(playerScores);
        GameManager.Instance.EndGame();
    }

    private void UpdatePlayerPress()
    {
        WebServerManager.Instance.GetRound().ContinueWith(roundTask =>
        {
            if (roundTask.Result == null) return;
            GameManager.Instance.UpdateGameData(roundTask.Result);
            players = roundTask.Result.players;
        });
    }

    protected abstract void RoundLogic();
    protected abstract int CalculateScore(string playerKey);
}

