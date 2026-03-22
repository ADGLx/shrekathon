using System.Collections.Generic;
using UnityEngine;
public class MaxTapDeal : DealManager
{
    protected override void RoundLogic() { }

    protected override int CalculateScore(string playerKey)
    {
        Dictionary<string, List<PlayerPress>> playerPress = PlayerInputHandler.Instance.GetPlayerPress();

        int myCount = playerPress.ContainsKey(playerKey) ? playerPress[playerKey].Count : 0;
        int maxCount = 0;
        foreach (var kvp in playerPress)
            if (kvp.Value.Count > maxCount) maxCount = kvp.Value.Count;

        return myCount == maxCount ? gamePoints : 0;
    }
}