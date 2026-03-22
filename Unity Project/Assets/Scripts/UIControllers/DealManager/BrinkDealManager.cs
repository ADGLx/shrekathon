/**
 * BrinkRound.cs used to manage the brink round, where the last person to release their button before the timer hits zero wins the pot.
 * If anyone holds past zero, everyone loses.
 */

using System.Collections.Generic;
using UnityEngine;

public class BrinkDealManager : DealManager
{
    // Tracks press counts per player from the previous frame to avoid per-frame log spam
    private Dictionary<string, int> _prevPressCounts = new Dictionary<string, int>();

    protected override void RoundLogic()
    {
        Dictionary<string, List<PlayerPress>> playerPress = PlayerInputHandler.Instance.GetPlayerPress();

        foreach (var kvp in playerPress)
        {
            string playerKey = kvp.Key;
            List<PlayerPress> presses = kvp.Value;

            if (presses == null || presses.Count == 0) continue;

            // Locked if they pressed more than once, or if their only press is already complete
            bool isLocked  = presses.Count > 1 || presses[^1].end_offset_ms > 0;
            // Currently holding if not locked and the latest press has no release time yet
            bool isPressed = !isLocked && presses[^1].end_offset_ms == 0;

            // Only log on change to avoid per-frame spam
            int currentCount = presses.Count;
            if (!_prevPressCounts.TryGetValue(playerKey, out int prevCount) || prevCount != currentCount)
            {
                Debug.Log($"[BrinkDealManager] '{playerKey}' press count {prevCount}->{currentCount} | isPressed={isPressed}, isLocked={isLocked}");
                _prevPressCounts[playerKey] = currentCount;
            }

            RoundManager.Instance.SetPlayerStatus(playerKey, isPressed, isLocked);
        }
    }

    protected override int CalculateScore(string playerKey)
    {
        Dictionary<string, List<PlayerPress>> playerPress = PlayerInputHandler.Instance.GetPlayerPress();

        string lastPlayer    = "";
        int    lastReleaseMs = 0;

        foreach (var kvp in playerPress)
        {
            foreach (PlayerPress press in kvp.Value)
            {
                if (press.end_offset_ms > lastReleaseMs)
                {
                    lastReleaseMs = press.end_offset_ms;
                    lastPlayer    = kvp.Key;
                }
            }
        }

        // If the last release was after the game ended, everyone loses
        if (lastReleaseMs > gameDurationMs)
        {
            Debug.Log($"[BrinkDealManager] Last release ({lastReleaseMs}ms) exceeded game duration ({gameDurationMs}ms) — all players score 0.");
            return 0;
        }

        return playerKey == lastPlayer ? 100 : 0;
    }
}