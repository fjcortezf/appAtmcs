using System.Text.Json.Serialization;

namespace AtmRedcqBank.Web.Models.Dtos;

public class BcrpResponse
{
    [JsonPropertyName("periods")]
    public List<BcrpPeriod> Periods { get; set; } = new();
}

public class BcrpPeriod
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public List<string> Values { get; set; } = new();
}