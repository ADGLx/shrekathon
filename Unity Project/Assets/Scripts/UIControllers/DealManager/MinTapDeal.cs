using System.Collections.Generic;
using UnityEngine;

public class MinTapDeal : DealManager
{
    protected override void RoundLogic() { }

    protected override int CalculateScore(string playerKey)
    {
        Dictionary<string, List<PlayerPress>> playerPress = PlayerInputHandler.Instance.GetPlayerPress();

        int myCount = playerPress.ContainsKey(playerKey) ? playerPress[playerKey].Count : 0;
        int minCount = int.MaxValue;
        foreach (var kvp in playerPress)
            if (kvp.Value.Count < minCount) minCount = kvp.Value.Count;

        return myCount == minCount ? gamePoints : 0;
    }
}