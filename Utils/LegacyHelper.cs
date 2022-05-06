// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;

namespace PerformanceCalculator
{
    public static class LegacyHelper
    {
        public static Ruleset GetRulesetFromLegacyID(int id)
        {
            switch (id)
            {
                default:
                    throw new ArgumentException("Invalid ruleset ID provided.");

                case 0:
                    return new OsuRuleset();

                case 1:
                    return new TaikoRuleset();

                case 2:
                    return new CatchRuleset();

                case 3:
                    return new ManiaRuleset();
            }
        }

        public static List<string> ConvertModToList(int mod)
        {
            string modBin = Convert.ToString(mod, 2);

            List<string> mods = new();
            
            //反转mod
            IEnumerable<char> c = modBin.Reverse();
            List<char> charList = c.ToList();
            if (mod != 0)
            {
                for (int i = charList.Count - 1; i >= 0; i--)
                {
                    //字符串中第i个字符是1,意味着第i+1个mod被开启了
                    if (charList[i] == '1')
                    {
                        switch (i)
                        {
                            case 0:
                                mods.Add("NF");
                                break;
                            case 1:
                                mods.Add("EZ");
                                break;
                            //虽然TD已经实装，但是MOD图标还是 不做 不画
                            case 3:
                                mods.Add("HD");
                                break;
                            case 4:
                                mods.Add("HR");
                                break;
                            case 5:
                                mods.Add("SD");
                                break;
                            case 6:
                                mods.Add("DT");
                                break;
                            //7是RX，不会上传成绩
                            case 8:
                                mods.Add("HT");
                                break;
                            case 9:
                                mods.Add("NC");
                                break;
                            case 10:
                                mods.Add("FL");
                                break;
                            //11是Auto
                            case 12:
                                mods.Add("SO");
                                break;
                            //13是AutoPilot
                            case 14:
                                mods.Add("PF");
                                break;
                            case 15:
                                mods.Add("4K");
                                break;
                            case 16:
                                mods.Add("5K");
                                break;
                            case 17:
                                mods.Add("6K");
                                break;
                            case 18:
                                mods.Add("7K");
                                break;
                            case 19:
                                mods.Add("8K");
                                break;
                            case 20:
                                mods.Add("FI");
                                break;
                            //21是RD，Mania的Note重新排布
                            //22是Cinema，但是不知道为什么有一个叫LastMod的名字
                            //23是Target Practice
                            case 24:
                                mods.Add("9K");
                                break;
                            //25是Mania的双人合作模式，Unrank
                            //Using 1K, 2K, or 3K mod will result in an unranked play.
                            //The mod does not work on osu!mania-specific beatmaps.
                            //26 1K，27 3K，28 2K
                        }
                    }
                }

                if (mods.Contains("NC"))
                {
                    mods.Remove("DT");
                }

                if (mods.Contains("PF"))
                {
                    mods.Remove("SD");
                }
            }

            return mods;
        }
    }
}