using UnityEngine;

public class LongTapCountDeal : DealManager
{
    private const int LongTapThresholdMs = 2000;

    protected override void RoundLogic() { }

    protected override int CalculateScore(string playerKey)
    {
        PlayerInputHandler inputHandler = PlayerInputHandler.Instance;
        if (inputHandler == null)
            return 0;

        int requiredLongTapCount = Mathf.Max(1, CurrentData != null ? CurrentData.min : 1);
        int myLongTapCount = inputHandler.GetLongTapCount(playerKey, LongTapThresholdMs);
        bool qualifies = myLongTapCount >= requiredLongTapCount;

        int award = qualifies ? gamePoints : 0;
        Debug.Log($"[LongTapCountDeal] Score calc for player={playerKey}: myLongTapCount={myLongTapCount}, requiredLongTapCount={requiredLongTapCount}, longTapThresholdMs={LongTapThresholdMs}, award={award}", this);
        return award;
    }
}
