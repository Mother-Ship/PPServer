using PPServer.models;

namespace PPServer.models.request;

public class CalculateRequest
{
    public int bid{ get; set; }
    public string osuFile{ get; set; }
    public UserScore userScore{ get; set; }
}