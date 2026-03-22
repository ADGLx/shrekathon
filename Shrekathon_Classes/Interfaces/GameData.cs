public class GameData
{
    string game_id;
    string round_id;
    string status;
    int ammount_of_players;
    string[] connected_players;
    int connected_count;
    bool all_connected;
    Dictionary<string, PressData[]> presses_by_player;
}