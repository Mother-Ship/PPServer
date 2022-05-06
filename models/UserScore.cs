namespace PPServer.models
{
    public class UserScore
    {
        public int count50 { get; set; }
        public int count100 { get; set; }
        public int count300 { get; set; }
        public int countMiss { get; set; }
        public int mods { get; set; }
        public int combo { get; set; }
        public int mode { get; set; }
        public double acc =>
            1D* (6 * count300 + 2 * count100 + count50)
            / (6 * (count50 + count100 + count300 + countMiss));
    }
}
