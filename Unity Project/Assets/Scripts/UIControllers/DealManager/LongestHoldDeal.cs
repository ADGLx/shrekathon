using System.Collections.Generic;
using UnityEngine;

public class LongestHoldDeal : DealManager
{
    protected override void RoundLogic() { }

    protected override int CalculateScore(string playerKey)
    {
        PlayerInputHandler inputHandler = PlayerInputHandler.Instance;
        if (inputHandler == null)
            return 0;

        Dictionary<string, List<PlayerPress>> playerPress = inputHandler.GetPlayerPress();
        int myLongestMs = inputHandler.GetLongestHoldDurationMs(playerKey);
        int globalLongestMs = 0;

        foreach (var kvp in playerPress)
        {
            int playerLongestMs = inputHandler.GetLongestHoldDurationMs(kvp.Key);
            if (playerLongestMs > globalLongestMs)
                globalLongestMs = playerLongestMs;
        }

        int award = myLongestMs > 0 && myLongestMs == globalLongestMs ? gamePoints : 0;
        Debug.Log($"[LongestHoldDeal] Score calc for player={playerKey}: myLongestMs={myLongestMs}, globalLongestMs={globalLongestMs}, award={award}", this);
        return award;
    }
}
