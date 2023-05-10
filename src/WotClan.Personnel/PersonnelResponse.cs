namespace WotClan.Personnel;

using System.Text.Json.Serialization;

public class PersonnelResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("items")]
    public Player[] Players { get; set; } = Array.Empty<Player>();

    public class Player
    {
        [JsonPropertyName("days_in_clan")]
        public int DaysInClan { get; set; }

        [JsonPropertyName("last_battle_time")]
        public int LastBattleTime { get; set; }

        [JsonPropertyName("battles_per_day")]
        public float BattlesPerDay { get; set; }

        [JsonPropertyName("personal_rating")]
        public int PersonalRating { get; set; }

        [JsonPropertyName("exp_per_battle")]
        public float ExperiencePerBattle { get; set; }

        [JsonPropertyName("damage_per_battle")]
        public float DamagePerBattle { get; set; }

        [JsonPropertyName("frags_per_battle")]
        public float FragsPerBattle { get; set; }

        [JsonPropertyName("is_press")]
        public bool IsPress { get; set; }

        [JsonPropertyName("wins_percentage")]
        public float WinsPercentage { get; set; }

        [JsonPropertyName("role")]
        public Role? Role { get; set; }

        [JsonPropertyName("abnormal_results")]
        public bool AbnormalResults { get; set; }

        [JsonPropertyName("battles_count")]
        public int BattlesCount { get; set; }

        [JsonPropertyName("id")]
        public int AccountId { get; set; }

        [JsonPropertyName("profile_link")]
        public string AccountProfileLink { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string AccountName { get; set; } = string.Empty;
    }

    public class Role
    {
        [JsonPropertyName("localized_name")]
        public string LocalizedName { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("rank")]
        public int Rank { get; set; }

        [JsonPropertyName("order")]
        public int Order { get; set; }
    }
}
