using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using NuGet.Packaging.Signing;
using osu.Game.Rulesets.Catch.Difficulty;
using osu.Game.Rulesets.Osu.Difficulty;
using osu.Game.Rulesets.Taiko.Difficulty;
using osu.Game.Scoring;
using PerformanceCalculator;
using PPServer.models;
using PPServer.models.result;

namespace PPServer.Services;

public class CalcService : ICalcService
{
    public CalcResult calc(string beatmapFile, UserScore userScore)
    {
        WorkingBeatmapFromFile workingBeatmap = new(beatmapFile);

        #region ==转换为osu格式的分数==

        Score score = new();
        score.ScoreInfo.Ruleset = LegacyHelper.GetRulesetFromLegacyID(userScore.mode).RulesetInfo;
        score.ScoreInfo.Ruleset.CreateInstance();

        score.ScoreInfo.Accuracy = userScore.acc;
        score.ScoreInfo.MaxCombo = userScore.combo;
        score.ScoreInfo.TotalScore = 0;
        //PP计算不仅需要acc，也需要具体的miss/50/100的分布
        JObject StatisticsJson = new()
        {
            { "Great", userScore.count300 },
            { "Ok", userScore.count100 },
            { "Meh", userScore.count50 },
            { "Miss", userScore.countMiss },
        };
   
        score.ScoreInfo.StatisticsJson = StatisticsJson.ToString();
        Console.WriteLine(score.ScoreInfo.StatisticsJson);
        Console.WriteLine(score.ScoreInfo.MaxCombo);
        #endregion

        #region ==添加Mod信息==

        JArray ModsList = new();
        List<string> Mods = LegacyHelper.ConvertModToList(userScore.mods);
        foreach (string Mod in Mods)
        {
            ModsList.Add(new JObject()
            {
                { "acronym", Mod },
                { "settings", new JObject() }
            });
        }
        ModsList.Add(new JObject() {
                    { "acronym", "CL" },
                    { "settings", new JObject() }
                });

        score.ScoreInfo.ModsJson = ModsList.ToString();

        Console.WriteLine(score.ScoreInfo.ModsJson);
        #endregion


        var performanceCalculator = score.ScoreInfo.Ruleset.CreateInstance()
            .CreatePerformanceCalculator();

        var attributes = score.ScoreInfo.Ruleset.CreateInstance().CreateDifficultyCalculator(workingBeatmap)
            .Calculate(score.ScoreInfo.Mods.ToArray());
        var ppAttributes = performanceCalculator?.Calculate(score.ScoreInfo, attributes)!;

        #region =转换谱面信息为对外提供的数据结构=

        BeatmapInfo info = new();
        switch (attributes)
        {
            case OsuDifficultyAttributes osu:
                info.aimStars = osu.AimDifficulty.ToString("N4");
                info.speedStars = osu.SpeedDifficulty.ToString("N4");
                info.maxCombo = osu.MaxCombo;
                info.ar = osu.ApproachRate.ToString("N4");
                info.od = osu.OverallDifficulty.ToString("N4");
                break;

            case TaikoDifficultyAttributes taiko:
                info.maxCombo = taiko.MaxCombo;
                // Json.Add("HitWindow", taiko.GreatHitWindow.ToString("N4"));
                break;

            case CatchDifficultyAttributes @catch:
                info.maxCombo = @catch.MaxCombo;
                info.ar = @catch.ApproachRate.ToString("N4");
                break;
        }

        double CS = workingBeatmap.BeatmapInfo.Difficulty.CircleSize;
        double HP = workingBeatmap.BeatmapInfo.Difficulty.DrainRate;

        if (Mods.Contains("HR"))
        {
            HP = (HP * 0.4) + HP;

            if (score.ScoreInfo.RulesetID == 0 || score.ScoreInfo.RulesetID == 2)
            {
                CS = (CS * 0.3) + CS;
            }
        }
        else if (Mods.Contains("EZ"))
        {
            HP -= (HP * 0.5);

            if (score.ScoreInfo.RulesetID == 0 || score.ScoreInfo.RulesetID == 2)
            {
                CS -= (CS * 0.5);
            }
        }

        info.cs = CS.ToString("N4");
        info.hp = HP.ToString("N4");
        info.stars = attributes.StarRating.ToString("N4");

        #endregion

        Console.WriteLine(beatmapFile);

        ScoreResult result = new();
        result.pp = ppAttributes.Total;
        foreach(var attr in ppAttributes.GetAttributesForDisplay())
        {
            Console.WriteLine(attr.PropertyName);
            Console.WriteLine(attr.Value);
            if(attr.PropertyName == "Accuracy")
            {
                result.acc = attr.Value;
            }
            if (attr.PropertyName == "Aim")
            {
                result.aim = attr.Value;
            }
            if (attr.PropertyName == "Speed")
            {
                result.speed = attr.Value;
            }
        }
       

        CalcResult data = new();
        data.scoreResult = result;
        data.beatmapInfo = info;
        return data;
    }
}