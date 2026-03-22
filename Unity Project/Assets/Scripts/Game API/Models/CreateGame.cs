using System;

[Serializable]
public class CreateGameRequest
{
    public string amount_of_players;
}

[Serializable]
public class CreateGameResponse
{
    public string game_id;
    public string status;

    public int amount_of_players;

    // Optional fields if backend includes player names in create response.
    public string[] connected_players;
    public string[] player_names;
}