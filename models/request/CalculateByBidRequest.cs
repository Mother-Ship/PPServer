using PPServer.models;

namespace PPServer.models.request;

public class CalculateByBidRequest
{
    public int bid{ get; set; }
    public bool refresh{ get; set; }
    public UserScore userScore{ get; set; }
}