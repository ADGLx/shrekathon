using System.Collections.Generic;
using UnityEngine;

public class EndGameData
{
    public string winnerName;
    public int winnerPoints;
    public Sprite winnerSprite;
    public Dictionary<string, int> playerPoints;  // mapping of player names to their points
}
