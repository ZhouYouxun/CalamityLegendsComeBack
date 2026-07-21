using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
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

        // 侦查：只释放少量可读的扫描光点；飞行中的点阵绘制在叶矢 PreDraw 中完成。
        private static void SpawnReconMuzzle(Vector2 muzzle, Vector2 dir, Vector2 right, Color theme, Color accent, float i)
        {
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                muzzle, dir * 2.2f, Color.Lerp(theme, Color.White, 0.22f),
                new Vector2(0.52f, 1.2f), dir.ToRotation(), 0.06f * i, 0.04f, 8));
            for (int k = 0; k < 5; k++)
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    muzzle + right * Main.rand.NextFloat(-5f, 5f),
                    dir.RotatedByRandom(0.18f) * Main.rand.NextFloat(2.6f, 5.4f),
                    Main.rand.NextFloat(0.16f, 0.24f) * i,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.25f, 0.65f)),
                    Main.rand.Next(10, 15)));
            }
        }

        // 轰炸：借鉴 Helium Flash 的烟火 Dust 与蒸汽 Dust，做成短促的机械点火。
        private static void SpawnBombardMuzzle(Vector2 muzzle, Vector2 dir, Color theme, Color accent, float i)
        {
            for (int k = 0; k < 12; k++)
            {
                Dust dust = Dust.NewDustPerfect(
                    muzzle + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.FireworksRGB,
                    dir.RotatedByRandom(0.42f) * Main.rand.NextFloat(3.5f, 9f),
                    80,
                    Color.Lerp(theme, Main.rand.NextBool(4) ? Color.White : accent, Main.rand.NextFloat(0.2f, 0.7f)),
                    Main.rand.NextFloat(0.65f, 1.05f) * i);
                dust.noGravity = true;
            }

            for (int k = 0; k < 3; k++)
            {
                Dust steam = Dust.NewDustPerfect(
                    muzzle - dir * Main.rand.NextFloat(1f, 5f),
                    DustID.SteampunkSteam,
                    (-dir).RotatedByRandom(0.42f) * Main.rand.NextFloat(0.7f, 2.2f),
                    140,
                    Color.Lerp(Color.SlateGray, theme, 0.4f),
                    Main.rand.NextFloat(0.55f, 0.85f) * i);
                steam.noGravity = false;
            }
        }

        // 瘟疫：取 PBG 掉落武器的毒牙、针筒与燃料罐语言——黄绿毒液核心，带少量橙红警戒光。
        private static void SpawnPlagueMuzzle(Vector2 muzzle, Vector2 dir, Color theme, Color accent, float i)
        {
            Color plagueCore = new(136, 238, 74);
            Color plagueWarning = new(245, 157, 56);
            for (int k = 0; k < 4; k++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    muzzle + Main.rand.NextVector2Circular(6f, 6f),
                    dir.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.8f, 2.8f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.14f, 0.24f) * i,
                    Color.Lerp(plagueCore, Main.rand.NextBool(5) ? plagueWarning : theme, Main.rand.NextFloat(0.12f, 0.45f)),
                    true,
                    false,
                    true));
            }

            for (int k = 0; k < 6; k++)
            {
                Dust venom = Dust.NewDustPerfect(
                    muzzle + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool() ? DustID.PoisonStaff : DustID.VenomStaff,
                    dir.RotatedByRandom(0.55f) * Main.rand.NextFloat(1.4f, 4.4f),
                    110,
                    Color.Lerp(plagueCore, plagueWarning, Main.rand.NextFloat(0f, 0.22f)),
                    Main.rand.NextFloat(0.65f, 1.05f) * i);
                venom.noGravity = true;
            }
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
