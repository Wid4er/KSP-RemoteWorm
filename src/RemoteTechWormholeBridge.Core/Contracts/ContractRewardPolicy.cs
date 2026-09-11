using System;

namespace RemoteTechWormholeBridge.Core.Contracts
{
    public static class ContractRewardPolicy
    {
        public const double BaseFunds = 300000.0;
        public const double FundsExponent = 1.6;
        public const double BaseScience = 20.0;
        public const double ScienceExponent = 1.35;
        public const double ReputationReward = 30.0;
        public const int StableDurationDays = 5;

        public static double Funds(int tier)
        {
            ValidateTier(tier);
            return RoundTo(Math.Pow(tier, FundsExponent) * BaseFunds, 1000.0);
        }

        public static double Science(int tier)
        {
            ValidateTier(tier);
            return Math.Round(Math.Pow(tier, ScienceExponent) * BaseScience);
        }

        private static double RoundTo(double value, double increment)
        {
            return Math.Round(value / increment) * increment;
        }

        private static void ValidateTier(int tier)
        {
            if (tier < 1)
                throw new ArgumentOutOfRangeException(nameof(tier));
        }
    }
}
