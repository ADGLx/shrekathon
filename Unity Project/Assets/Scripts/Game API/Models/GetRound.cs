using System;
using System.Collections.Generic;
using Newtonsoft.Json;

[Serializable]
public class GetRoundRequest
{
    public string game_id;
}

[Serializable]
public class GetRoundResponse
{
    public string detail;

    public string game_id;
    public string round_id;
    public string status;
    public Dictionary<string, List<PlayerPress>> presses_by_player;
}

[Serializable]
public class PlayerPress
{
    public int start_offset_ms;
    public int end_offset_ms;
}