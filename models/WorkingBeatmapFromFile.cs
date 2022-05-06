﻿// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.IO;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics.Textures;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Catch;
using osu.Game.Rulesets.Mania;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Taiko;
using osu.Game.Skinning;

namespace PerformanceCalculator
{
    /// <summary>
    /// A <see cref="WorkingBeatmap"/> which reads from a .osu file.
    /// 代码来自@momodesu
    /// </summary>
    public class WorkingBeatmapFromFile : WorkingBeatmap
    {
        private readonly Beatmap beatmap;

        /// <summary>
        /// Constructs a new <see cref="WorkingBeatmapFromFile"/> from a .osu file.
        /// </summary>
        /// <param name="file">The .osu file.</param>
        /// <param name="beatmapId">An optional beatmap ID (for cases where .osu file doesn't have one).</param>
        public WorkingBeatmapFromFile(string file, int? beatmapId = null)
            : this(readFromFile(file), beatmapId)
        {
        }

        private WorkingBeatmapFromFile(Beatmap beatmap, int? beatmapId = null)
            : base(beatmap.BeatmapInfo, null)
        {
            this.beatmap = beatmap;

            beatmap.BeatmapInfo.Ruleset = LegacyHelper.GetRulesetFromLegacyID(beatmap.BeatmapInfo.RulesetID).RulesetInfo;

            if (beatmapId.HasValue)
                beatmap.BeatmapInfo.OnlineID = beatmapId;
        }

        private static Beatmap readFromFile(string filename)
        {
            using (var stream = File.OpenRead(filename))
            using (var reader = new LineBufferedReader(stream))
                return Decoder.GetDecoder<Beatmap>(reader).Decode(reader);
        }

        protected override IBeatmap GetBeatmap() => beatmap;
        protected override Texture GetBackground() => null;
        protected override Track GetBeatmapTrack() => null;
        protected override ISkin GetSkin() => null;
        public override Stream GetStream(string storagePath) => null;
        
    }
}
