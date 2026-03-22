using System.Collections.Generic;
using UnityEngine;
public class RangeTapDeal : DealManager
{
    protected override void RoundLogic() { }

    protected override int CalculateScore(string playerKey)
    {
        Dictionary<string, List<PlayerPress>> playerPress = PlayerInputHandler.Instance.GetPlayerPress();

        int myCount = playerPress.ContainsKey(playerKey) ? playerPress[playerKey].Count : 0;
        int minBound = Mathf.Min(CurrentData.min, CurrentData.max);
        int maxBound = Mathf.Max(CurrentData.min, CurrentData.max);
        bool inRange = myCount >= minBound && myCount <= maxBound;

        Debug.Log($"[RangeTapDeal] Score calc for player={playerKey}: myCount={myCount}, range=[{CurrentData.min},{CurrentData.max}], normalizedRange=[{minBound},{maxBound}], award={(inRange ? gamePoints : 0)}", this);
        return inRange ? gamePoints : 0;
    }
}