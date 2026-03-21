//@todo: Impliment all calls to the WebServerManager in the GameManager class.

public sealed class GameManager : MonoBehaviour
{
    // Singleton Boilerplate | https://csharpindepth.com/articles/singleton
    private static GameManager _instance;
    public static GameManager Instance => _instance;

    public Dictionary<string, int> playerScores = new Dictionary<string, int>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Class methods
    private GameData currentGameData;
    protected readonly Dictionary<string, PlayerGUI> playerGUIs = new Dictionary<string, PlayerGUI>();

    // Updating the playerGUI
    public PlayerGUI GetPlayerGUI(string playerKey)
    {
        return playerGUIs[playerKey];
    }

    public GameData GetCurrentGameData()
    {
        return currentGameData;
    }

    public void UpdateGameData(GameData newGameData)
    {
        currentGameData = newGameData;
    }

    public void StartRound()
    {
        WebServerManager.Instance.GetRound().ContinueWith(roundTask =>
        {
            if (roundTask.Result == null)
            {
                print("Failed to get round data");
                return;
            }

            currentGameData = (GameData) roundTask.Result;
        });
    }

    public void UpdateScore(Dictionary<string, int> playerScores)
    {
        this.playerScores = playerScores;
        //@todo update in the platerGUI
    }
}
