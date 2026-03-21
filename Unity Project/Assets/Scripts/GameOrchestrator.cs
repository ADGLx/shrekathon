using UnityEngine;

public class GameManager : MonoBehaviour
{

      
     public static GameManager Instance { get; private set; }

     private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);   // kill duplicate (e.g. if scene reloads)
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        InitializeGame();
    }



    [Header("Game Configuration")]
    [SerializeField] private int totalRounds = 6;
    [SerializeField] private int playerCount  = 4;

    public int TotalRounds => totalRounds;
    public int PlayerCount  => playerCount;


    public int  CurrentRound    { get; private set; }   // 0-indexed
    public bool GameIsOver      { get; private set; }

    public void InitializeGame() {
      CurrentRound = 0;
      GameIsOver = false;
      


      Debug.Log("[GameManager] Game initialised.");
    }


    public void NextRound() {
      CurrentRound++;
      
      




    }
}
