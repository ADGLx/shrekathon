using System;

[Serializable]
public class GetGameRequest
{
    public string game_id;
}

[Serializable]
public class GetGameResponse
{
    // This is the error response:
    public string detail;

    // This is the success response:
    public string game_id;
    public int amount_of_players;
    public string[] connected_players;
    public int connected_count;
    public bool all_connected;
    public string status;
    public string round_id;
    public string round_status;
    public string started_at_ms;
    public string time_limit_ms;
    public int remaining_ms;
}