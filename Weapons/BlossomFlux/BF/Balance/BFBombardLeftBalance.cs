namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal readonly struct BFBombardLeftStats
    {
        public readonly int MinArrowCount;
        public readonly int MaxArrowCount;
        public readonly int FireInterval;
        public readonly int ExplosionsPerArrow;
        public readonly float ExplosionRadiusMultiplier;
        public readonly float ProjectileSpeedMultiplier;

        public BFBombardLeftStats(int minArrowCount, int maxArrowCount, int fireInterval, int explosionsPerArrow, float explosionRadiusMultiplier, float projectileSpeedMultiplier)
        {
            MinArrowCount = minArrowCount;
            MaxArrowCount = maxArrowCount;
            FireInterval = fireInterval;
            ExplosionsPerArrow = explosionsPerArrow;
            ExplosionRadiusMultiplier = explosionRadiusMultiplier;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
        }
    }

    internal static class BFBombardLeftBalance
    {
        public static BFBombardLeftStats GetStats()
        {
            // ### 歼灭普攻
            // 歼灭普攻从空中降下轰炸箭，下面这些就是平衡组常用调整项。
            //
            // 初始 / Initial：每次降下 4 支箭，发射间隔 20 帧，每支箭爆炸 1 次。
            int minCount = 4;
            int maxCount = 4;
            int interval = 20;
            int explosionLimit = 1;
            float radius = 1f;
            float speed = 1f;

            // 击败世纪之花 / Plantera：最大箭数变为 5。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                maxCount = 5;

            // 击败瘟疫使者歌莉娅 / Plaguebringer Goliath：固定 5 支箭，发射间隔变为 17 帧。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
            {
                minCount = 5;
                maxCount = 5;
                interval = 17;
            }

            // 击败月亮领主 / Moon Lord：5-6 支箭，发射间隔变为 16 帧，每支箭爆炸 2 次。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                minCount = 5;
                maxCount = 6;
                interval = 16;
                explosionLimit = 2;
            }

            // 击败噬魂幽花 / Polterghast：爆炸范围变为 1.25 倍，基础弹速变为 1.18 倍。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                radius = 1.25f;
                speed = 1.18f;
            }

            // 击败神明吞噬者 / Devourer of Gods：固定 6 支箭，发射间隔变为 14 帧，每支箭爆炸 3 次。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
            {
                minCount = 6;
                maxCount = 6;
                interval = 14;
                explosionLimit = 3;
            }

            // -------------------- 内部返回结构 --------------------
            return new BFBombardLeftStats(minCount, maxCount, interval, explosionLimit, radius, speed);
        }
    }
}
