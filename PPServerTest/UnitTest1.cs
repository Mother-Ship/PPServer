using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using PPServer.models;
using PPServer.Services;

namespace PPServerTest;

public class CalcServiceTest
{
    private  CalcService _myDependency;


    [SetUp]
    public void Setup()
    {
    }

    [Test]
    public void Test1()
    {
        UserScore userScore = new();
       
        userScore.combo = 588;
        userScore.mode = 0;
        userScore.mods = 0;
        userScore.count300 = 1072;
        userScore.count100 = 58;
        userScore.countMiss = 20;
        userScore.count50 = 1;
        _myDependency = new CalcService();
        _myDependency.calc(
            "H:\\osu!\\Songs\\440423 Kushi - Yuumeikyou o Wakatsu Koto\\Kushi - Yuumeikyou o Wakatsu Koto (09kami) [Yuumei].osu",
            userScore);
    }
}