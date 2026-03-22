using System;

[Serializable]
public class StartRoundRequest
{
    public string game_id;
    public int time_limit_ms;
}

[Serializable]
public class StartRoundResponse
{
    public string game_id;
    public string round_id;
    public string status;
}