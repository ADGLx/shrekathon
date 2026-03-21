using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PitchData", menuName = "Scriptable Objects/PitchData")]
[Serializable]
public class PitchData : ScriptableObject
{
    [Header("Character")]
    public string       characterName;
    public string       characterDescription;
    public Sprite       characterSprite;

    [Header("Contract")]
    public string       contractTitle;
    public Sprite       contractSprite;

    [Header("GamePlay")]
    public int gameDurationMs;
    public string gameType;
}
