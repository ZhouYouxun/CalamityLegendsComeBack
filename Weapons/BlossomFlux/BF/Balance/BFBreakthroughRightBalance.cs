namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal readonly struct BFBreakthroughRightStats
    {
        public readonly int FramesPerArrow;
        public readonly int MaxLoadedArrows;
        public readonly int Penetrate;
        public readonly bool IgnorePenetrationDamageFalloff;
        public readonly float ProjectileSpeedMultiplier;

        public BFBreakthroughRightStats(int framesPerArrow, int maxLoadedArrows, int penetrate, bool ignorePenetrationDamageFalloff, float projectileSpeedMultiplier)
        {
            FramesPerArrow = framesPerArrow;
            MaxLoadedArrows = maxLoadedArrows;
            Penetrate = penetrate;
            IgnorePenetrationDamageFalloff = ignorePenetrationDamageFalloff;
            ProjectileSpeedMultiplier = projectileSpeedMultiplier;
        }
    }

    internal static class BFBreakthroughRightBalance
    {
        public static BFBreakthroughRightStats GetStats()
        {
            // ### 突击蓄力
            // 突击蓄力会搭载穿透箭矢，连续蓄力会搭载多支，松开右键一起发射。
            //
            // 初始 / Initial：蓄力时间 45 帧，最多搭载 3 支箭，穿透 4 次。
            int framesPerArrow = 45;
            int maxArrows = 3;
            int penetrate = 4;
            bool noFalloff = false;
            float speedMult = 1f;

            // 击败任意 Boss 或小 Boss / Any Boss or Miniboss：穿透 5 次。
            if (BlossomFluxProgression.DownedAnyBossOrMiniboss())
                penetrate = 5;

            // 击败克苏鲁之眼 / Eye of Cthulhu：蓄力时间变为 40 帧。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.EyeOfCthulhu))
                framesPerArrow = 40;

            // 击败蜂王 / Queen Bee：蓄力箭矢上限变为 4。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.QueenBee))
                maxArrows = 4;

            // 击败血肉墙 / Wall of Flesh：蓄力时间变为 35 帧，蓄力箭矢上限变为 5。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.WallOfFlesh))
            {
                framesPerArrow = 35;
                maxArrows = 5;
            }

            // 击败任意机械 Boss / Any Mechanical Boss：蓄力箭矢上限变为 6。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MechBoss))
                maxArrows = 6;

            // 击败世纪之花 / Plantera：穿透 7 次。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Plantera))
                penetrate = 7;

            // 击败瘟疫使者歌莉娅 / Plaguebringer Goliath：蓄力箭矢上限变为 7。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.PlaguebringerGoliath))
                maxArrows = 7;

            // 击败月亮领主 / Moon Lord：穿透 15 次，无视穿透衰减，蓄力时间变为 30 帧。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.MoonLord))
            {
                penetrate = 15;
                noFalloff = true;
                framesPerArrow = 30;
            }

            // 击败噬魂幽花 / Polterghast：无限穿透，提高基础弹速。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.Polterghast))
            {
                penetrate = -1;
                speedMult = 1.65f;
            }

            // 击败神明吞噬者 / Devourer of Gods：再次提高基础弹速。
            if (BlossomFluxProgression.DownedAtLeast(BlossomFluxProgressionStage.DevourerOfGods))
                speedMult = 2.15f;

            // -------------------- 内部返回结构 --------------------
            return new BFBreakthroughRightStats(framesPerArrow, maxArrows, penetrate, noFalloff, speedMult);
        }
    }
}
