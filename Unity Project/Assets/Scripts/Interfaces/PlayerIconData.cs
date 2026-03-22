using System;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerIconData", menuName = "Scriptable Objects/PlayerIconData")]
[Serializable]
public class PlayerIconData
{
    [Header("Character")]
    public string       characterName;
    public Sprite       characterSprite;
}