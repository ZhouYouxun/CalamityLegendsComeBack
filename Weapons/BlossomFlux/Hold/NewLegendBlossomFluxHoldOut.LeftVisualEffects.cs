using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.EXSkill;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.Visuals;
using CalamityMod;
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
                BlossomFluxChloroplastPresetType.Chlo_DBomb => Math.Max(1, BFBombardLeftBalance.GetStats().FireInterval / 2),
                BlossomFluxChloroplastPresetType.Chlo_EPlague => PlagueFireInterval,
                _ => BreakthroughFireInterval
            };
        }

        private void SpawnSHPCLeftMuzzleParticles(Vector2 center, Vector2 velocity, BlossomFluxChloroplastPresetType preset, float intensity)
        {
            if (Main.dedServ)
                return;

            // 左键唯一粒子外包：直接照着 SHPC 的枪口电火花思路走。
            // 五种状态只换颜色，不换轨迹、不换节奏、不换粒子结构。
            Color themeColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Color white = new(235, 255, 255);
            Vector2 direction = velocity.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 right = direction.RotatedBy(MathHelper.PiOver2);
            Vector2 muzzle = center + direction * 2f;
            float clampedIntensity = MathHelper.Clamp(intensity, 0.55f, 1.45f);

            Lighting.AddLight(muzzle, themeColor.ToVector3() * (0.18f + clampedIntensity * 0.18f));

            int dustCount = 8 + (int)(clampedIntensity * 4f);
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(muzzle + Main.rand.NextVector2Circular(5f, 5f), DustID.RainbowMk2);
                dust.velocity = direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(4.5f, 9.5f) + right * Main.rand.NextFloat(-1.1f, 1.1f);
                dust.color = Color.Lerp(themeColor, white, Main.rand.NextFloat(0.25f, 0.85f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(0.82f, 1.22f) * clampedIntensity;
            }

            for (int i = 0; i < 4; i++)
            {
                Dust spark = Dust.NewDustPerfect(muzzle + right * Main.rand.NextFloat(-6f, 6f), DustID.Electric);
                spark.velocity = direction.RotatedByRandom(0.24f) * Main.rand.NextFloat(3.2f, 6.8f);
                spark.color = Color.Lerp(accentColor, white, Main.rand.NextFloat(0.35f, 0.9f));
                spark.noGravity = true;
                spark.scale = Main.rand.NextFloat(0.72f, 1.05f) * clampedIntensity;
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
