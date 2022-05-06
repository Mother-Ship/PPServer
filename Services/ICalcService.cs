using PPServer.models;
using PPServer.models.result;

namespace PPServer.Services;

public interface ICalcService
{
    public CalcResult calc(string beatmapFile, UserScore userScore);

}