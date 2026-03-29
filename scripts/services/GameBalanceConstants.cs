namespace Xiuxian.Scripts.Services
{
    public static class GameBalanceConstants
    {
        public static class InputDecay
        {
            public const double ApBaseline = 6.0;
            public const double DecayThreshold = 1.0;
            public const double DecayRate = 0.25;
            public const double MinDecayMultiplier = 0.45;
        }

        public static class ResourceConversion
        {
            public const double LingqiFactor = 0.9;
            public const double InsightFactor = 0.08;
            public const double PetAffinityFactor = 0.03;
            public const double RealmExpFromLingqiRate = 0.25;
            public const double CultivationExpPerInput = 0.35;
        }

        public static class EquipmentGeneration
        {
            public const int CommonToolSubStatCount = 0;
            public const int ArtifactSubStatCount = 1;
            public const int SpiritSubStatCount = 1;
            public const int TreasureSubStatCount = 2;
        }

        public static class Offline
        {
            public const double ApPerInput = 1.0;
        }

        public static class Explore
        {
            public const float ProgressPerInput = 0.02f;
            public const int InputsPerMoveFrame = 4;
            public const int InputsPerBattleRound = 18;
            public const int MaxBossBattleRounds = 20;
            public const float MonsterMovePxPerFrame = 3.8f;
            public const float MonsterRespawnSpacing = 110.0f;
        }

        public static class LevelDefaults
        {
            public const double ProgressPer100Inputs = 2.0;
            public const double EncounterCheckIntervalProgress = 20.0;
            public const double BaseEncounterRate = 0.18;
            public const int DangerLevel = 1;
            public const int PlayerBaseHp = 36;
            public const int PlayerAttackPerRound = 4;
            public const int EnemyDamageDivider = 4;
            public const int EnemyMinDamagePerRound = 1;
        }
    }
}
