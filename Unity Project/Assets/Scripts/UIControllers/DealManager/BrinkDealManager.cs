/**
 * BrinkRound.cs used to manage the brink round, where the last person to release their button before the timer hits zero wins the pot. 
 * If anyone holds past zero, everyone loses.
 */

using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class BrinkDealManager : DealManager
{
    protected override void RoundLogic()
    {
        // Implement specific logic for the brink round
        Dictionary<string, List<PlayerPress>> playerPress = getPlayerPress();
        /*@todo: impliment game modeLogic
        foreach (KeyValuePair<string, List<PlayerPress>> player in playerPress)
        {
            // If the player has released the button at least once lock it
            if (player.Value.Length >= 2)
            {
                GameManager.Instance.GetPlayerGUI(player.Key).SetButtonState(false, true);
                break;
            }

            // If the player has pressed it once, show that the button is actively being pressed
            if (player.Value.Length == 1)
            {
                GameManager.Instance.GetPlayerGUI(player.Key).SetButtonState(true, false);
            }


            if (player.Value.Length > 0)
            {
                PressData lastPress = player.Value[player.Value.Length - 1];
                if (lastPress.time >= gameDuration)
                {
                    // Player held past zero, they lose
                    GameManager.Instance.PlayerLost(player.Key);
                }
            }
        }
        */
    }

    protected override int CalculateScore(string playerKey)
    {
        string lastPlayer = "";
        int lastPressTime = 0;

        /* @todo: impliment game modeLogic
        foreach (KeyValuePair<string, PressData[]> player in players)
        {
            // If anyone holds the button past zero award no points (pressed once and not released)
            if (player.Value.Length == 1)
                return 0;

            if (lastPressTime < player.Value[1].time)
            {
                lastPlayer = player.Key;
                lastPressTime = player.Value[1].time;
            }
        }

        // If this player released the button last, return the number of points
        if (lastPlayer == playerKey)
            return 100;
        */

        // Gets no points otherwise
        return 0;
    }
}

