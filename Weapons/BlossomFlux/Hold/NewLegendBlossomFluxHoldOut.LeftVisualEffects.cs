using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal sealed partial class NewLegendBlossomFluxHoldOut
    {
        private void TriggerLeftStarFlash()
        {
            leftStarFlashTimer = LeftStarFlashFrames;
            leftOutlinePulseTimer = LeftOutlinePulseFrames + 10;

            // 每次左键攻击只推进星芒相位，不做额外状态分支。
            // 这样五种左键形态共享同一套 SHPC 风格星芒，只靠颜色区分。
            leftStarburstSpinKick = (leftStarburstSpinKick + MathHelper.Pi / 8f) % MathHelper.TwoPi;
        }

        private float GetLeftAttackBuildGlow()
        {
            if (!leftHeldLastFrame || rightChargeActive || HasActiveSelectionPanel(Owner))
                return 0f;

            int interval = GetCurrentLeftGlowInterval();
            if (interval <= 0)
                return 0.12f + 0.06f * (0.5f + 0.5f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 13f + Projectile.identity * 0.31f));

            float build = 1f - leftBurstTimer / (float)interval;
            float intervalWeight = MathHelper.Clamp(interval / (float)ReconFireInterval, 0.16f, 1f);
            intervalWeight = MathHelper.Lerp(0.16f, 1f, (float)Math.Pow(intervalWeight, 1.35f));
            return MathHelper.Clamp(build, 0f, 1f) * intervalWeight;
        }

        private int GetCurrentLeftGlowInterval()
        {
            return CurrentPreset switch
            {
                BlossomFluxChloroplastPresetType.Chlo_ABreak => Math.Max(1, BFBreakthroughLeftBalance.GetStats().UseInterval),
                BlossomFluxChloroplastPresetType.Chlo_BRecov => burstGroupsStarted == 0 && leftBurstTimer > RecoveryBurstInterval ? BFRecoveryLeftBalance.GetStats().VolleyPauseFrames : RecoveryBurstInterval,
                BlossomFluxChloroplastPresetType.Chlo_CDetec => leftBurstTimer > ReconFireInterval ? ReconCyclePause : ReconFireInterval,
                BlossomFluxChloroplastPresetType.Chlo_DBomb => Math.Max(1, BFBombardLeftBalance.GetStats().FireInterval),
                BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueFireInterval,
                _ => BreakthroughFireInterval
            };
        }

        private void SpawnSHPCLeftMuzzleParticles(Vector2 center, Vector2 velocity, BlossomFluxChloroplastPresetType preset, float intensity, bool includeBreakthroughCritSpark = true)
        {
            if (Main.dedServ)
                return;

            Color themeColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 muzzle = center + direction * 2f;
            float ci = MathHelper.Clamp(intensity, 0.55f, 1.45f);

            Lighting.AddLight(muzzle, themeColor.ToVector3() * (0.18f + ci * 0.18f));

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    SpawnBreakthroughMuzzle(muzzle, direction, right, themeColor, accentColor, ci, includeBreakthroughCritSpark);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    SpawnRecoveryMuzzle(muzzle, direction, themeColor, accentColor, ci);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    SpawnReconMuzzle(muzzle, direction, right, themeColor, accentColor, ci);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    SpawnBombardMuzzle(muzzle, direction, themeColor, accentColor, ci);
                    break;
                case BlossomFluxChloroplastPresetType.Chlo_EPlague:
                    SpawnPlagueMuzzle(muzzle, direction, themeColor, accentColor, ci);
                    break;
                default:
                    SpawnBreakthroughMuzzle(muzzle, direction, right, themeColor, accentColor, ci, includeBreakthroughCritSpark);
                    break;
            }
        }

        // 突击：叶片飞溅 — 高速 SparkParticle 正向爆出 + 侧向 CritSpark
        private static void SpawnBreakthroughMuzzle(Vector2 muzzle, Vector2 dir, Vector2 right, Color theme, Color accent, float i, bool includeCritSpark)
        {
            for (int k = 0; k < 6; k++)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    muzzle + Main.rand.NextVector2Circular(4f, 4f),
                    dir.RotatedByRandom(0.52f) * Main.rand.NextFloat(5f, 11f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.5f, 0.9f) * i,
                    Main.rand.NextBool() ? theme : Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.3f, 0.7f))));
            }
            if (includeCritSpark)
            {
                for (int k = 0; k < 3; k++)
                {
                    GeneralParticleHandler.SpawnParticle(new CritSpark(
                        muzzle + Main.rand.NextVector2Circular(6f, 6f),
                        right * Main.rand.NextFloat(-8f, 8f) + dir * Main.rand.NextFloat(1f, 3f),
                        theme,
                        Color.Lerp(accent, Color.White, 0.5f),
                        Main.rand.NextFloat(0.35f, 0.6f) * i,
                        Main.rand.Next(8, 14)));
                }
            }
            for (int k = 0; k < 5; k++)
            {
                Dust d = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(5f, 5f), DustID.RainbowMk2);
                d.velocity = dir.RotatedByRandom(0.38f) * Main.rand.NextFloat(4f, 8f);
                d.color = Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.2f, 0.7f));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.7f, 1.1f) * i;
            }
        }

        // 复苏：生命光球托起十字治愈粒子，不再混入云雾与普通 Dust。
        private static void SpawnRecoveryMuzzle(Vector2 muzzle, Vector2 dir, Color theme, Color accent, float i)
        {
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                muzzle, dir * 0.35f, Color.Lerp(theme, Color.White, 0.5f), 0.24f * i, 9));
            for (int k = 0; k < 4; k++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    muzzle + Main.rand.NextVector2Circular(7f, 7f),
                    dir * Main.rand.NextFloat(0.7f, 1.7f) + Vector2.UnitY * Main.rand.NextFloat(-1.6f, -0.45f),
                    false, Main.rand.Next(16, 25),
                    Main.rand.NextFloat(0.18f, 0.3f) * i,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.3f, 0.7f)),
                    true, false, true));
            }

            for (int k = 0; k < 3; k++)
            {
                HealingPlus plus = new(
                    muzzle + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextFloat(0.35f, 0.58f) * i,
                    dir * Main.rand.NextFloat(0.45f, 1.25f) + Vector2.UnitY * Main.rand.NextFloat(-2.5f, -1.2f),
                    Color.Lerp(theme, Color.White, 0.2f),
                    Color.Lerp(accent, Color.White, 0.65f),
                    Main.rand.Next(16, 24));
                GeneralParticleHandler.SpawnParticle(plus);
            }
        }

        // 侦查：电磁扫描式 — 全向 CritSpark 短爆 + 正前方 DirectionalPulseRing
        private static void SpawnReconMuzzle(Vector2 muzzle, Vector2 dir, Vector2 right, Color theme, Color accent, float i)
        {
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                muzzle, dir * 2.2f, Color.Lerp(theme, Color.White, 0.22f),
                new Vector2(0.52f, 1.2f), dir.ToRotation(), 0.06f * i, 0.04f, 8));
            for (int k = 0; k < 8; k++)
            {
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    muzzle + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6.5f),
                    theme,
                    Color.Lerp(accent, Color.White, 0.55f),
                    Main.rand.NextFloat(0.28f, 0.52f) * i,
                    Main.rand.Next(7, 12)));
            }
            for (int k = 0; k < 4; k++)
            {
                Dust d = Dust.NewDustPerfect(muzzle + right * Main.rand.NextFloat(-7f, 7f), DustID.Electric);
                d.velocity = dir.RotatedByRandom(0.25f) * Main.rand.NextFloat(3f, 7f);
                d.color = Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.3f, 0.85f));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.7f, 1.1f) * i;
            }
        }

        // 轰炸：爆炸冲击式 — StrongBloom 强闪 + 四面放射 SparkParticle
        private static void SpawnBombardMuzzle(Vector2 muzzle, Vector2 dir, Color theme, Color accent, float i)
        {
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                muzzle, Vector2.Zero, Color.Lerp(theme, Color.White, 0.25f), 0.45f * i, 12));
            for (int k = 0; k < 10; k++)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    muzzle + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3.5f, 9.5f),
                    false, Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.45f, 0.85f) * i,
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(theme, accent, Main.rand.NextFloat())));
            }
            for (int k = 0; k < 6; k++)
            {
                Dust d = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(6f, 6f), DustID.GoldFlame);
                d.velocity = dir.RotatedByRandom(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 7f);
                d.color = Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.45f));
                d.noGravity = true;
                d.scale = Main.rand.NextFloat(0.75f, 1.2f) * i;
            }
        }

        // 瘟疫枪口 = 一具生化排放阀（取 PBG 掉落武器的毒牙/针筒/纳米蜂群语言）。
        // 一次开火串起六段动作：①阀口泄压环 ②六边形蜂巢排放格 ③涡流毒液喷射 ④纳米疫孢 ⑤警戒频闪 ⑥酸液滴落与余雾。
        // 每发相位推进 30°、旋涡逐发反向、每三发一次强排毒，所以连射时阀口是活的，不会两发长得一模一样。
        // Bombard left-click only: an aimed, forward-cone counterpart to the all-direction target marker.
        private static void SpawnBombardGunMuzzle(Vector2 center, Vector2 aimDirection, float intensity)
        {
            if (Main.dedServ)
                return;

            Color theme = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Color accent = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Vector2 dir = aimDirection.SafeNormalize(Vector2.UnitX);
            Vector2 muzzle = center + dir * 2f;
            float i = MathHelper.Clamp(intensity, 0.55f, 1.45f);

            Lighting.AddLight(muzzle, theme.ToVector3() * (0.18f + i * 0.18f));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                muzzle, dir * 0.5f, Color.Lerp(theme, Color.White, 0.25f), 0.38f * i, 10));

            for (int k = 0; k < 10; k++)
            {
                Vector2 velocity = dir.RotatedByRandom(0.38f) * Main.rand.NextFloat(3.5f, 9.5f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    muzzle + Main.rand.NextVector2Circular(4f, 4f), velocity,
                    false, Main.rand.Next(10, 18), Main.rand.NextFloat(0.45f, 0.85f) * i,
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(theme, accent, Main.rand.NextFloat())));
            }

            for (int k = 0; k < 6; k++)
            {
                Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(4f, 4f), DustID.GoldFlame);
                dust.velocity = dir.RotatedByRandom(0.44f) * Main.rand.NextFloat(2f, 7f);
                dust.color = Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.1f, 0.45f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.75f, 1.2f) * i;
            }
        }

        // Plague muzzle routine.
        private void SpawnPlagueMuzzle(Vector2 muzzle, Vector2 dir, Color theme, Color accent, float i)
        {
            Color plagueCore = new(124, 238, 68);      // 毒液主绿
            Color plagueAcid = new(218, 255, 116);     // 高亮酸黄
            Color plagueDeep = new(48, 128, 46);       // 深疫绿（余雾用）
            Color plagueWarning = new(245, 157, 56);   // 生化警戒橙

            plagueVentShotIndex++;
            plagueVentSpin += MathHelper.Pi / 6f;                        // 每发转 30°，相邻两发的六边形互补成十二角
            float swirl = plagueVentShotIndex % 2 == 0 ? 1f : -1f;       // 旋涡逐发反向，喷口不会一直朝同一侧甩
            bool purge = plagueVentShotIndex % 3 == 0;                   // 每三发一次强排毒：更大的环、更多毒液、橙色警报
            plagueVentPurgeActive = purge;
            plagueVentResidueTimer = purge ? 24 : 13;                    // 开火后阀口继续漏毒的帧数

            Vector2 perp = dir.RotatedBy(MathHelper.PiOver2);
            float power = i * (purge ? 1.28f : 1f);

            // ① 阀口泄压环：沿枪管压扁的硬边环被推出去；强排毒时再叠一圈更慢更大的橙色警戒环
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                muzzle, dir * 1.5f, plagueCore with { A = 0 },
                new Vector2(0.4f, 1f), dir.ToRotation(), 0.05f * power, 0.26f * power, 10));
            if (purge)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    muzzle - dir * 2f, dir * 0.6f, plagueWarning with { A = 0 },
                    new Vector2(0.28f, 1f), dir.ToRotation(), 0.03f, 0.4f, 15));
            }

            // ② 蜂巢排放格：六枚发光方格严格落在正六边形顶点上，沿顶点法线外扩、被旋涡拧偏后飞散
            for (int k = 0; k < 6; k++)
            {
                Vector2 vertex = (plagueVentSpin + MathHelper.TwoPi * k / 6f).ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new GlowSquareParticle(
                    muzzle + vertex * (5f + power * 2.6f),
                    vertex.RotatedBy(swirl * 0.45f) * Main.rand.NextFloat(1.4f, 2.3f) + dir * Main.rand.NextFloat(1.5f, 3.1f),
                    false,
                    15,
                    Main.rand.NextFloat(0.2f, 0.3f) * power,
                    Color.Lerp(plagueCore, plagueAcid, k / 5f),
                    true,
                    swirl * 0.14f));
            }

            // ③ 涡流毒液喷射：毒液不是乱喷，而是被阀门拧成一股旋流——越远离枪口偏角越大、轴向速度越低
            int jetCount = purge ? 11 : 8;
            for (int k = 0; k < jetCount; k++)
            {
                float t = k / (float)(jetCount - 1);
                float spin = plagueVentSpin + swirl * (0.5f + t * 2.5f);
                Vector2 jetPosition = muzzle + dir * (2f + t * 9f) + perp * MathF.Sin(spin) * (2.4f + t * 6.5f);
                Vector2 jetVelocity = dir * MathHelper.Lerp(5.4f, 2.1f, t) + perp * (MathF.Cos(spin) * swirl * 2.2f);
                Dust venom = Dust.NewDustPerfect(
                    jetPosition,
                    (int)CalamityDusts.Plague,
                    jetVelocity,
                    90,
                    Color.Lerp(plagueCore, plagueAcid, t),
                    Main.rand.NextFloat(0.85f, 1.25f) * power);
                venom.noGravity = true;
            }

            // ③b 原有的毒牙/针筒毒雾保留，但收进旋涡同向的窄锥里给喷射打底
            for (int k = 0; k < 5; k++)
            {
                Dust fang = Dust.NewDustPerfect(
                    muzzle + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool() ? DustID.PoisonStaff : DustID.VenomStaff,
                    dir.RotatedBy(swirl * Main.rand.NextFloat(0.05f, 0.3f)) * Main.rand.NextFloat(1.4f, 4.2f),
                    110,
                    Color.Lerp(plagueCore, plagueWarning, Main.rand.NextFloat(0f, 0.22f)),
                    Main.rand.NextFloat(0.65f, 1.05f) * i);
                fang.noGravity = true;
            }

            // ④ 纳米疫孢：几粒被压出阀门的纳米颗粒，高速前冲并轻微下坠
            for (int k = 0; k < (purge ? 5 : 3); k++)
            {
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    muzzle + Main.rand.NextVector2Circular(4f, 4f),
                    dir.RotatedByRandom(0.3f) * Main.rand.NextFloat(2.6f, 5.4f),
                    Main.rand.NextBool(4) ? plagueWarning : Color.Lerp(plagueCore, plagueAcid, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.7f, 1.1f) * power,
                    Main.rand.Next(15, 23),
                    false,
                    true,
                    true,
                    Vector2.UnitY * 0.05f));
            }

            // ⑤ 警戒频闪：常态是一记酸绿阀芯闪，强排毒帧额外补一记橙色警报闪
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                muzzle + dir * 2f, dir * 0.35f, Color.Lerp(plagueAcid, Color.White, 0.32f), 0.19f * power, 8));
            if (purge)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    muzzle, Vector2.Zero, plagueWarning with { A = 0 }, 0.3f, 13));
            }

            // ⑥ 酸液滴落：阀门漏下来的重毒液滴，真的受重力往下掉，落在开火节奏的尾巴上
            for (int k = 0; k < (purge ? 3 : 2); k++)
            {
                Dust drip = Dust.NewDustPerfect(
                    muzzle - dir * Main.rand.NextFloat(0f, 6f) + perp * Main.rand.NextFloat(-3f, 3f),
                    (int)CalamityDusts.Plague,
                    -dir * Main.rand.NextFloat(0.2f, 0.7f) + Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.3f),
                    120,
                    Color.Lerp(plagueCore, plagueDeep, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.NextFloat(0.7f, 1.05f));
                drip.noGravity = false;
            }

            // ⑥b 余雾：阀口向后上方吐出的低亮疫雾，让枪口在两发之间也不干净
            for (int k = 0; k < (purge ? 3 : 2); k++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    muzzle - dir * Main.rand.NextFloat(2f, 8f) + Main.rand.NextVector2Circular(4f, 4f),
                    -dir * Main.rand.NextFloat(0.3f, 0.9f) + new Vector2(Main.rand.NextFloat(-0.3f, 0.3f), Main.rand.NextFloat(-0.8f, -0.25f)),
                    Color.Lerp(plagueDeep, plagueCore, Main.rand.NextFloat(0.15f, 0.5f)),
                    Main.rand.Next(24, 38),
                    Main.rand.NextFloat(0.16f, 0.26f) * power,
                    0.42f,
                    Main.rand.NextFloat(-0.14f, 0.14f)));
            }

            Lighting.AddLight(muzzle, Color.Lerp(plagueCore, plagueWarning, purge ? 0.4f : 0.1f).ToVector3() * (0.3f + power * 0.25f));
        }

        // 开火之外的枪口状态：蓄压时毒液向阀口汇聚，开火后阀口继续滴漏，让瘟疫形态在两发之间也是“活的排放阀”。
        private void UpdatePlagueVentAmbience()
        {
            if (Main.dedServ || CurrentPreset != BlossomFluxChloroplastPresetType.Chlo_EPlague)
            {
                plagueVentResidueTimer = 0;
                return;
            }

            Color plagueCore = new(124, 238, 68);
            Color plagueAcid = new(218, 255, 116);
            Color plagueDeep = new(48, 128, 46);
            Vector2 vent = GunTipPosition;

            // 残压滴漏：跟着弓口走，所以移动时会拖出一条毒迹，而不是钉在开火那一点
            if (plagueVentResidueTimer > 0)
            {
                plagueVentResidueTimer--;
                float residue = plagueVentResidueTimer / 24f;

                if (plagueVentResidueTimer % 3 == 0)
                {
                    Dust leak = Dust.NewDustPerfect(
                        vent + Main.rand.NextVector2Circular(4f, 4f),
                        (int)CalamityDusts.Plague,
                        new Vector2(Main.rand.NextFloat(-0.35f, 0.35f), Main.rand.NextFloat(0.3f, 1f)),
                        130,
                        Color.Lerp(plagueDeep, plagueCore, Main.rand.NextFloat(0.25f, 0.7f)),
                        Main.rand.NextFloat(0.5f, 0.85f));
                    leak.noGravity = false;
                }

                if (plagueVentResidueTimer % 5 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                        vent + Main.rand.NextVector2Circular(3f, 3f),
                        new Vector2(Main.rand.NextFloat(-0.25f, 0.25f), Main.rand.NextFloat(-0.6f, -0.2f)),
                        Color.Lerp(plagueDeep, plagueCore, 0.3f),
                        Main.rand.Next(20, 32),
                        Main.rand.NextFloat(0.12f, 0.2f),
                        0.3f,
                        Main.rand.NextFloat(-0.1f, 0.1f)));
                }

                Lighting.AddLight(vent, plagueCore.ToVector3() * 0.16f * residue);
            }

            // 蓄压汇聚：只在下一发临近的几帧里，从四周把毒液光点吸进阀口，做成可读的开火前摇
            if (!leftHeldLastFrame || rightChargeActive || HasActiveSelectionPanel(Owner))
                return;

            if (leftBurstTimer <= 0 || leftBurstTimer > 5)
                return;

            float convergence = 1f - leftBurstTimer / 5f;
            Vector2 inbound = Main.rand.NextVector2CircularEdge(1f, 1f) * MathHelper.Lerp(24f, 12f, convergence);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                vent + inbound,
                -inbound.SafeNormalize(Vector2.UnitY) * MathHelper.Lerp(1.8f, 3.4f, convergence),
                false,
                Main.rand.Next(7, 11),
                Main.rand.NextFloat(0.1f, 0.17f) * (0.7f + convergence * 0.5f),
                Color.Lerp(plagueCore, plagueAcid, Main.rand.NextFloat(0.2f, 0.7f)),
                true,
                false,
                true));
        }

        // 瘟疫专属枪口绘制层：六边形泄压光圈 + 反向旋转的三角警戒环 + 阀芯辉光与排气缝。
        // 调用点在 PreDraw 的加法混合批次里，批次恢复由 PreDraw 统一负责。
        private void DrawPlagueVentIris(float flash)
        {
            if (Main.dedServ || flash <= 0.015f)
                return;

            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 lineOrigin = line.Size() * 0.5f;
            Vector2 direction = AimDirection;
            Vector2 vent = GunTipPosition - Main.screenPosition;

            float grow = 1f - MathHelper.Clamp(flash, 0f, 1f);   // 0 = 刚开火，1 = 闪光结束
            float expand = 1f - (1f - grow) * (1f - grow);       // 外扩用 easeOut：开阀瞬间最快
            float fade = flash * flash;                          // 亮度衰减快于外扩，避免长时间亮着
            float radius = MathHelper.Lerp(6f, 27f, expand) * (plagueVentPurgeActive ? 1.22f : 1f);

            Color hexColor = new Color(150, 240, 84) with { A = 0 };
            Color warnColor = new Color(245, 157, 56) with { A = 0 };

            // 六边形泄压圈：六条边首尾相接成正六边形，整体跟着开火相位旋转
            for (int k = 0; k < 6; k++)
            {
                Vector2 p1 = vent + (plagueVentSpin + MathHelper.TwoPi * k / 6f).ToRotationVector2() * radius;
                Vector2 p2 = vent + (plagueVentSpin + MathHelper.TwoPi * (k + 1) / 6f).ToRotationVector2() * radius;
                Vector2 edge = p2 - p1;

                Main.EntitySpriteDraw(
                    line,
                    (p1 + p2) * 0.5f,
                    null,
                    hexColor * (0.5f * fade),
                    edge.ToRotation() + MathHelper.PiOver2,
                    lineOrigin,
                    new Vector2(0.0016f + 0.0008f * fade, edge.Length() / 1960f),
                    SpriteEffects.None,
                    0);
            }

            // 内层警戒三角：反向旋转、半径更小，强排毒帧偏橙，常态偏酸黄
            float innerRadius = radius * 0.52f;
            float innerSpin = -plagueVentSpin * 1.7f + grow * 1.15f;
            Color innerColor = Color.Lerp(hexColor, warnColor, plagueVentPurgeActive ? 0.85f : 0.35f);
            for (int k = 0; k < 3; k++)
            {
                Vector2 p1 = vent + (innerSpin + MathHelper.TwoPi * k / 3f).ToRotationVector2() * innerRadius;
                Vector2 p2 = vent + (innerSpin + MathHelper.TwoPi * (k + 1) / 3f).ToRotationVector2() * innerRadius;
                Vector2 edge = p2 - p1;

                Main.EntitySpriteDraw(
                    line,
                    (p1 + p2) * 0.5f,
                    null,
                    innerColor * (0.34f * fade),
                    edge.ToRotation() + MathHelper.PiOver2,
                    lineOrigin,
                    new Vector2(0.0013f, edge.Length() / 1960f),
                    SpriteEffects.None,
                    0);
            }

            // 排气缝：沿枪管方向的两道短亮线，表现压力从阀缝里挤出去
            for (int k = 0; k < 2; k++)
            {
                float side = k == 0 ? 1f : -1f;
                Main.EntitySpriteDraw(
                    line,
                    vent + direction.RotatedBy(MathHelper.PiOver2) * side * (3f + 4f * expand),
                    null,
                    hexColor * (0.3f * fade),
                    direction.ToRotation() + MathHelper.PiOver2,
                    lineOrigin,
                    new Vector2(0.0012f, MathHelper.Lerp(0.004f, 0.012f, expand)),
                    SpriteEffects.None,
                    0);
            }

            // 阀芯：贴着枪口的一小团酸绿辉光，中心压一点白
            Main.EntitySpriteDraw(bloom, vent, null, hexColor * (0.32f * fade), 0f, bloom.Size() * 0.5f, 0.15f + 0.05f * fade, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, vent, null, (Color.White with { A = 0 }) * (0.16f * fade), 0f, bloom.Size() * 0.5f, 0.055f, SpriteEffects.None, 0);
        }

        private void DrawSHPCLeftAttackVisuals(BlossomFluxChloroplastPresetType preset, float leftFlash)
        {
            // 左键唯一绘制外包：五种状态只换颜色，其余完全共享 SHPC 星芒结构。
            float flashPulse = MathHelper.SmoothStep(0f, 1f, MathHelper.Clamp(leftFlash, 0f, 1f));
            float sustainPulse = leftHeldLastFrame && !rightChargeActive ? 0.22f + GetLeftAttackBuildGlow() * 0.18f : 0f;
            float power = MathHelper.Clamp(Math.Max(flashPulse, sustainPulse), 0f, 1f);
            if (power <= 0.02f)
                return;

            DrawSHPCMagicCore(preset, power, leftStarburstSpinKick, false, false);
        }
    }
}
