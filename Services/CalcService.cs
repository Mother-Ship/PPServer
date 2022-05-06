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
        score.ScoreInfo.Ruleset = workingBeatmap.BeatmapInfo.Ruleset.ID == 0
            ? LegacyHelper.GetRulesetFromLegacyID(userScore.mode).RulesetInfo
            : workingBeatmap.BeatmapInfo.Ruleset;
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

        score.ScoreInfo.ModsJson = ModsList.ToString();

        #endregion


        Dictionary<string, double> categoryAttribs = new();
        var performanceCalculator = score.ScoreInfo.Ruleset.CreateInstance()
            .CreatePerformanceCalculator(workingBeatmap, score.ScoreInfo);
        Trace.Assert(performanceCalculator != null);
        var attributes = score.ScoreInfo.Ruleset.CreateInstance().CreateDifficultyCalculator(workingBeatmap)
            .Calculate(score.ScoreInfo.Mods.ToArray());

        #region =转换谱面信息为对外提供的数据结构=

        BeatmapInfo info = new();
        switch (attributes)
        {
            case OsuDifficultyAttributes osu:
                info.aimStars = osu.AimStrain.ToString("N4");
                info.speedStars = osu.SpeedStrain.ToString("N4");
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

        double CS = workingBeatmap.BeatmapInfo.BaseDifficulty.CircleSize;
        double HP = workingBeatmap.BeatmapInfo.BaseDifficulty.DrainRate;

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


        ScoreResult result = new();
        result.pp = performanceCalculator.Calculate(categoryAttribs);
        result.acc = categoryAttribs["Accuracy"];
        result.aim = categoryAttribs["Aim"];
        result.speed = categoryAttribs["Speed"];

        CalcResult data = new();
        data.scoreResult = result;
        data.beatmapInfo = info;
        return data;
    }

    public JObject PPCalculator(
        string Beatmap,
        bool is_passed,
        string Accuracy,
        string MaxCombo,
        List<string> Mods,
        int Mode,
        JObject StatisticsJson,
        string TotalScore = null,
        bool IsMore = false)
    {
        WorkingBeatmapFromFile workingBeatmap = new(Beatmap);
        Score score = new();
        Dictionary<string, double> categoryAttribs = new();
        osu.Game.Rulesets.Difficulty.PerformanceCalculator performanceCalculator;
        osu.Game.Rulesets.Difficulty.DifficultyAttributes attributes;
        int n100, n300;

        #region ==赋值==

        score.ScoreInfo.Ruleset = workingBeatmap.BeatmapInfo.Ruleset.ID == 0
            ? LegacyHelper.GetRulesetFromLegacyID(Mode).RulesetInfo
            : workingBeatmap.BeatmapInfo.Ruleset;
        score.ScoreInfo.Accuracy = double.Parse(Accuracy);
        score.ScoreInfo.MaxCombo = int.Parse(MaxCombo);
        score.ScoreInfo.TotalScore = TotalScore != null ? long.Parse(TotalScore) : 0;
        if (!is_passed && Mode == 0)
        {
            n100 = (int)(1.50 * (1.00 - score.ScoreInfo.Accuracy) * (double)workingBeatmap.Beatmap.HitObjects.Count);
            n300 = workingBeatmap.Beatmap.HitObjects.Count - n100;
            StatisticsJson = new()
            {
                { "Great", n300 },
                { "Ok", n100 },
                { "Meh", 0 },
                { "Miss", 0 },
            };
        }

        score.ScoreInfo.StatisticsJson = StatisticsJson.ToString();

        #region ==Mod==

        JArray ModsList = new();
        foreach (string Mod in Mods)
        {
            ModsList.Add(new JObject()
            {
                { "acronym", Mod },
                { "settings", new JObject() }
            });
        }

        score.ScoreInfo.ModsJson = ModsList.ToString();

        #endregion

        #endregion

        performanceCalculator = score.ScoreInfo.Ruleset.CreateInstance()
            .CreatePerformanceCalculator(workingBeatmap, score.ScoreInfo);
        Trace.Assert(performanceCalculator != null);
        attributes = score.ScoreInfo.Ruleset.CreateInstance().CreateDifficultyCalculator(workingBeatmap)
            .Calculate(score.ScoreInfo.Mods.ToArray());

        double pp = performanceCalculator.Calculate(categoryAttribs);

        double CS = workingBeatmap.BeatmapInfo.BaseDifficulty.CircleSize;
        double HP = workingBeatmap.BeatmapInfo.BaseDifficulty.DrainRate;

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
            HP = HP - (HP * 0.5);

            if (score.ScoreInfo.RulesetID == 0 || score.ScoreInfo.RulesetID == 2)
            {
                CS = CS - (CS * 0.5);
            }
        }

        JObject Json = new()
        {
            {
                "Mods",
                new JArray()
                {
                    score.ScoreInfo.Mods.Length > 0
                        ? score.ScoreInfo.Mods.Select(m => m.Acronym).Aggregate((c, n) => $"{c}, {n}")
                        : "None"
                }
            },
            { "Star", attributes.StarRating.ToString("N4") },
            { "CS", CS.ToString("N4") },
            { "HP", HP.ToString("N4") }
        };

        switch (attributes)
        {
            case OsuDifficultyAttributes osu:
                Json.Add("Aim", osu.AimStrain.ToString("N4"));
                Json.Add("Speed", osu.SpeedStrain.ToString("N4"));
                Json.Add("MaxCombo", osu.MaxCombo);
                Json.Add("AR", osu.ApproachRate.ToString("N4"));
                Json.Add("OD", osu.OverallDifficulty.ToString("N4"));
                break;

            case TaikoDifficultyAttributes taiko:
                Json.Add("MaxCombo", taiko.MaxCombo);
                Json.Add("HitWindow", taiko.GreatHitWindow.ToString("N4"));
                break;

            case CatchDifficultyAttributes @catch:
                Json.Add("MaxCombo", @catch.MaxCombo);
                Json.Add("AR", @catch.ApproachRate.ToString("N4"));
                break;
        }

        JObject PPInfo = new();
        if (IsMore)
        {
            Dictionary<string, double> PPList = new()
            {
                { "100", 1 },
                { "99", 0.99 },
                { "98", 0.98 },
                { "97", 0.97 },
                { "95", 0.95 },
            };
            score.ScoreInfo.MaxCombo = int.Parse(Json["MaxCombo"].ToString());
            //score.ScoreInfo.Combo = score.ScoreInfo.MaxCombo;
            JObject Statistics_more = new();
            foreach (KeyValuePair<string, double> PPName in PPList)
            {
                score.ScoreInfo.Accuracy = PPName.Value;
                switch (Mode)
                {
                    case 0:
                        n100 = (int)(1.50 * (1.00 - score.ScoreInfo.Accuracy) *
                                     (double)workingBeatmap.Beatmap.HitObjects.Count);
                        n300 = workingBeatmap.Beatmap.HitObjects.Count - n100;
                        Statistics_more = new()
                        {
                            { "Great", n300 },
                            { "Ok", n100 },
                            { "Meh", 0 },
                            { "Miss", 0 },
                        };
                        break;
                    case 1:
                        Statistics_more = new()
                        {
                            {
                                "Great",
                                int.Parse(StatisticsJson["Great"].ToString()) +
                                int.Parse(StatisticsJson["Miss"].ToString())
                            },
                            { "Ok", int.Parse(StatisticsJson["Ok"].ToString()) },
                            { "Miss", 0 },
                        };
                        break;
                    case 2:
                        Statistics_more = new()
                        {
                            {
                                "Great",
                                int.Parse(StatisticsJson["Great"].ToString()) +
                                int.Parse(StatisticsJson["Miss"].ToString())
                            },
                            { "LargeTickHit", int.Parse(StatisticsJson["Ok"].ToString()) },
                            {
                                "SmallTickHit",
                                int.Parse(StatisticsJson["Meh"].ToString()) +
                                int.Parse(StatisticsJson["Katu"].ToString())
                            },
                            { "SmallTickMiss", 0 },
                            { "Miss", 0 }
                        };
                        break;
                    case 3:
                        //do nothing
                        break;
                }

                score.ScoreInfo.StatisticsJson = Statistics_more.ToString();
                performanceCalculator = score.ScoreInfo.Ruleset.CreateInstance()
                    .CreatePerformanceCalculator(workingBeatmap, score.ScoreInfo);
                categoryAttribs = new();
                pp = performanceCalculator.Calculate(categoryAttribs);

                JObject Temp = new()
                {
                    { "Total", pp.ToString(CultureInfo.InvariantCulture) }
                };

                foreach (var kvp in categoryAttribs)
                    Temp.Add(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));

                Temp.Remove("OD");
                Temp.Remove("AR");
                Temp.Remove("Max Combo");
                PPInfo.Add(PPName.Key, Temp);
            }

            // FullCombo
            score.ScoreInfo.Accuracy = double.Parse(Accuracy);
            switch (Mode)
            {
                case 0:
                    n100 = (int)(1.50 * (1.00 - score.ScoreInfo.Accuracy) *
                                 (double)workingBeatmap.Beatmap.HitObjects.Count);
                    n300 = workingBeatmap.Beatmap.HitObjects.Count - n100;
                    Statistics_more = new()
                    {
                        { "Great", n300 },
                        { "Ok", n100 },
                        { "Meh", 0 },
                        { "Miss", 0 },
                    };
                    break;
                case 1:
                    Statistics_more = new()
                    {
                        {
                            "Great",
                            int.Parse(StatisticsJson["Great"].ToString()) + int.Parse(StatisticsJson["Miss"].ToString())
                        },
                        { "Ok", int.Parse(StatisticsJson["Ok"].ToString()) },
                        { "Miss", 0 },
                    };
                    break;
                case 2:
                    Statistics_more = new()
                    {
                        {
                            "Great",
                            int.Parse(StatisticsJson["Great"].ToString()) + int.Parse(StatisticsJson["Miss"].ToString())
                        },
                        { "LargeTickHit", int.Parse(StatisticsJson["Ok"].ToString()) },
                        {
                            "SmallTickHit",
                            int.Parse(StatisticsJson["Meh"].ToString()) + int.Parse(StatisticsJson["Katu"].ToString())
                        },
                        { "SmallTickMiss", 0 },
                        { "Miss", 0 }
                    };
                    break;
                case 3:
                    //do nothing
                    break;
            }

            score.ScoreInfo.StatisticsJson = Statistics_more.ToString();
            performanceCalculator = score.ScoreInfo.Ruleset.CreateInstance()
                .CreatePerformanceCalculator(workingBeatmap, score.ScoreInfo);
            categoryAttribs = new();
            pp = performanceCalculator.Calculate(categoryAttribs);
            JObject Temp1 = new()
            {
                { "Total", pp.ToString(CultureInfo.InvariantCulture) }
            };

            foreach (var kvp in categoryAttribs)
                Temp1.Add(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));
            Temp1.Remove("OD");
            Temp1.Remove("AR");
            Temp1.Remove("Max Combo");
            PPInfo.Add("FullCombo", Temp1);
            Json.Add("PPInfo", PPInfo);
        }
        else
        {
            PPInfo = new()
            {
                { "Total", pp.ToString(CultureInfo.InvariantCulture) }
            };

            foreach (var kvp in categoryAttribs)
                PPInfo.Add(kvp.Key, kvp.Value.ToString(CultureInfo.InvariantCulture));

            PPInfo.Remove("OD");
            PPInfo.Remove("AR");
            PPInfo.Remove("Max Combo");
            Json.Add("PPInfo", PPInfo);
        }

        return Json;
    }
}