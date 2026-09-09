using System;

namespace Biathlon.Domain
{
    public readonly struct AthletePhysiologyStats
    {
        public int Endurance { get; }
        public int AnaerobicCapacity { get; }
        public int Recovery { get; }
        public int Strength { get; }
        public AthletePhysiologyStats(int endurance, int anaerobicCapacity, int recovery, int strength)
        {
            Endurance = Validate(endurance);
            AnaerobicCapacity = Validate(anaerobicCapacity);
            Recovery = Validate(recovery);
            Strength = Validate(strength);
        }
        private static int Validate(int stat)
        {
            if (stat < 0 || stat > 100) throw new ArgumentOutOfRangeException(nameof(stat));
            return stat;
        }
    }
}
