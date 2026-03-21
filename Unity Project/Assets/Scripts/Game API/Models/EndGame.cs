using System;

[Serializable]
public class EndGameRequest
{
    public string game_id;
}

[Serializable]
public class EndGameResponse
{
    public string game_id;
    public string status;
}