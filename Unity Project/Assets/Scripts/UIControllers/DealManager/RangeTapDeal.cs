using System.Collections.Generic;
using UnityEngine;
public class RangeTapDeal : DealManager
{
    protected override void RoundLogic() { }

    protected override int CalculateScore(string playerKey)
    {
        Dictionary<string, List<PlayerPress>> playerPress = PlayerInputHandler.Instance.GetPlayerPress();

        int myCount = playerPress.ContainsKey(playerKey) ? playerPress[playerKey].Count : 0;
        Debug.Log($"[RangeTapDeal] Score calc for player={playerKey}: myCount={myCount}, range=[{CurrentData.min},{CurrentData.max}], award={(myCount >= CurrentData.min && myCount <= CurrentData.max ? gamePoints : 0)}", this);
        return (myCount >= CurrentData.min && myCount <= CurrentData.max) ? gamePoints : 0;
    }
}