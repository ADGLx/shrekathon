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
}