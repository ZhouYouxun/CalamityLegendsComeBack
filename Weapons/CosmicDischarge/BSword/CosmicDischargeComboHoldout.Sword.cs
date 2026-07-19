using System;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public partial class CosmicDischargeComboHoldout
    {
        private void UpdateSwordSwing(bool second)
        {
            if (Time <= SwordSwingWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            float swingSign = (second ? -1f : 1f) * Math.Sign(Owner.direction == 0 ? 1 : Owner.direction);
            int strikeEnd = SwordSwingWindup + 8;
            int holdEnd = strikeEnd + 3;

            if (Time <= SwordSwingWindup)
            {
                float prep = EaseOutCubic(Time / SwordSwingWindup);
                float angle = AimAngle + swingSign * MathHelper.Lerp(-1.42f, -0.86f, prep);
                SetBlade(angle.ToRotationVector2(), SwordReach, second ? 0.12f : -0.12f, 28f);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordSwingWindup) / 8f;
                float strike = EaseOutCubic(t);
                float angle = AimAngle + MathHelper.Lerp(-0.86f * swingSign, 1.1f * swingSign, strike);
                Vector2 slashDirection = angle.ToRotationVector2();
                SetBlade(slashDirection, SwordReach, second ? 0.12f : -0.12f, 38f);
                PlayReleaseOnce(SoundID.Item71, 0.82f, second ? 0.14f : -0.08f, 3.8f);

                if (!impactEffectsPlayed && t >= 0.58f)
                {
                    // 整次挥剑只画一道弧。正反挥靠角度差 π 区分，不靠换粒子种类。
                    CosmicDischargeCommon.SpawnSwingSmear(
                        Owner.MountedCenter + slashDirection * (SwordReach * 0.5f),
                        angle + MathHelper.PiOver2 + (second ? MathHelper.Pi : 0f),
                        4.2f,
                        CosmicDischargeCommon.RiftMagenta);

                    EmitAirCrack(TipPosition, slashDirection, 0.74f);
                    SpawnSwordHomingBolts(TipPosition, slashDirection, second ? 4 : 3, second ? 0.42f : 0.36f);
                }
            }
            else if (Time <= holdEnd)
            {
                SetBlade((AimAngle + 1.14f * swingSign).ToRotationVector2(), SwordReach, 0f, 36f);
            }
            else
            {
                float t = Utils.GetLerpValue(holdEnd, SwordSwingDuration, Time, true);
                float recover = MathF.Sin(t * MathHelper.PiOver2);
                float angle = AimAngle + MathHelper.Lerp(1.14f * swingSign, 0.18f * swingSign, recover);
                SetBlade(angle.ToRotationVector2(), SwordReach, 0f, MathHelper.Lerp(30f, 18f, recover));
            }

            if (Time >= SwordSwingDuration)
                Projectile.Kill();
        }
        private void UpdateSwordFinisher()
        {
            if (Time <= SwordFinisherWindup)
                AimAngle = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection).ToRotation();

            Vector2 direction = AimDirection;
            if (Time <= SwordFinisherWindup)
            {
                float t = Time / SwordFinisherWindup;
                float spinAngle = AimAngle + Owner.direction * (MathHelper.TwoPi * 2f * t - MathHelper.PiOver2);
                float chargeBump = 0.5f + 0.5f * MathF.Sin(MathHelper.Pi * t);
                SetBlade(spinAngle.ToRotationVector2(), FinisherReach, 0.05f * Owner.direction, 32f + chargeBump * 7f);

                bool ultActive = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive;

                if (Time == 1f)
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DevourerRiftBuilding") { Volume = 0.45f, Pitch = -0.12f, MaxInstances = 2 }, Owner.Center);

                if (Time % 8f == 0f)
                    ApplyScreenShake(1.2f + t * 1.5f);

                // 蓄力照 DoG 的节奏：整个 36 帧前摇只脉冲 3 次（12/24/36），
                // 不再逐帧撒漩涡火花 —— 那是原先"疯"的最大来源。
                if (Time == 12f || Time == 24f || Time == 36f)
                    CosmicDischargeCommon.SpawnChargePulse(Owner.MountedCenter, t, ultActive ? 1f : 0.8f);

                SpawnSpinChargeDust(t);

                if (Main.myPlayer == Projectile.owner)
                {
                    float pullRadius = ultActive ? 320f : 260f;
                    for (int i = 0; i < Main.maxNPCs; i++)
                    {
                        NPC npc = Main.npc[i];
                        if (npc.active && !npc.friendly && !npc.dontTakeDamage && !npc.boss && npc.knockBackResist > 0f)
                        {
                            float dist = Vector2.Distance(Owner.Center, npc.Center);
                            if (dist < pullRadius)
                            {
                                float pullSpeed = ultActive ? 8.8f : 5f;
                                Vector2 pull = (Owner.Center - npc.Center).SafeNormalize(Vector2.Zero) * pullSpeed;
                                npc.velocity = pull;
                                npc.netUpdate = true;

                                int tickRate = ultActive ? 4 : 6;
                                if (Time % tickRate == 0)
                                {
                                    npc.StrikeNPC(npc.CalculateHitInfo((int)(Projectile.damage * (ultActive ? 0.45f : 0.33f)), Math.Sign(pull.X), false, 0f));
                                }
                            }
                        }
                    }
                }
                // 蓄力结束的释放：一次 Heavy 档爆发，玩家为中心。
                if (Time == SwordFinisherWindup)
                    CosmicDischargeCommon.SpawnRiftBurst(Owner.MountedCenter, RiftTier.Heavy, default, CosmicDischargeCommon.RiftMagenta);
                return;
            }

            int strikeFrames = 10;
            int strikeEnd = SwordFinisherSlamFrame + strikeFrames;

            if (Time <= SwordFinisherSlamFrame)
            {
                float lift = EaseOutCubic(Utils.GetLerpValue(SwordFinisherWindup, SwordFinisherSlamFrame, Time, true));
                float angle = AimAngle - Owner.direction * MathHelper.Lerp(1.38f, 1.05f, lift);
                SetBlade(angle.ToRotationVector2(), FinisherReach, 0.05f * Owner.direction, 38f);
            }
            else if (Time <= strikeEnd)
            {
                float t = (Time - SwordFinisherSlamFrame) / strikeFrames;
                float slam = EaseOutCubic(t);
                float angle = AimAngle + MathHelper.Lerp(-1.05f * Owner.direction, 1.22f * Owner.direction, slam);
                SetBlade(angle.ToRotationVector2(), FinisherReach, 0f, 48f);

                if (!releaseSoundPlayed && t >= 0.12f)
                {
                    releaseSoundPlayed = true;
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.7f, Pitch = -0.18f, MaxInstances = 2 }, Owner.Center);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HeavySwing") { Volume = 0.55f, Pitch = -0.18f }, Owner.Center);
                    ApplyScreenShake(7.2f);
                }

                if (!spawnedSwordWave && t >= 0.45f)
                {
                    spawnedSwordWave = true;

                    // 终结斩的落刃弧 —— 全武器最大的一道，但仍然只有一道。
                    CosmicDischargeCommon.SpawnSwingSmear(
                        Owner.MountedCenter + direction * (FinisherReach * 0.5f),
                        angle + MathHelper.PiOver2,
                        6f,
                        CosmicDischargeCommon.RiftMagenta);

                    SpawnSwordWave(direction);
                    EmitAirCrack(TipPosition, direction, 1.35f);
                    SpawnSwordHomingBolts(TipPosition, direction, 6, 0.52f);
                }
            }
            else
            {
                float t = Utils.GetLerpValue(strikeEnd, SwordFinisherDuration, Time, true);
                float recover = MathF.Sin(t * MathHelper.PiOver2);
                float angle = AimAngle + MathHelper.Lerp(1.22f * Owner.direction, 0.16f * Owner.direction, recover);
                SetBlade(angle.ToRotationVector2(), FinisherReach, 0f, MathHelper.Lerp(38f, 18f, recover));
            }

            if (Time >= SwordFinisherDuration)
                Projectile.Kill();
        }
        private void SpawnSwordWave(Vector2 direction)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + direction * 78f,
                direction * 18f,
                ModContent.ProjectileType<CosmicDischargeSwordWave>(),
                (int)(Projectile.damage * 0.86f),
                Projectile.knockBack,
                Projectile.owner);
        }
        private void SpawnSwordHomingBolts(Vector2 position, Vector2 direction, int count, float damageFactor)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            count = Math.Max(1, count);
            float spread = MathHelper.ToRadians(18f);
            for (int i = 0; i < count; i++)
            {
                float offset = count == 1 ? 0f : MathHelper.Lerp(-spread, spread, i / (float)(count - 1));
                Vector2 velocity = direction.RotatedBy(offset) * Main.rand.NextFloat(10f, 14.5f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    position + direction * 12f,
                    velocity,
                    ModContent.ProjectileType<CosmicDischargeDoGEnergyBolt>(),
                    (int)(Projectile.damage * damageFactor),
                    Projectile.knockBack * 0.45f,
                    Projectile.owner);
            }
        }
        /// <summary>旋转蓄力时刀锋扫出的环形尘。DoGFire 的 dust 频率，不是每帧撒。</summary>
        private void SpawnSpinChargeDust(float charge)
        {
            if (Main.dedServ || !Main.rand.NextBool(12))
                return;

            Vector2 radius = Main.rand.NextVector2CircularEdge(58f + charge * 42f, 58f + charge * 42f);
            Dust dust = Dust.NewDustPerfect(
                Owner.MountedCenter + radius,
                DustID.TintableDustLighted,
                radius.RotatedBy(MathHelper.PiOver2 * Owner.direction).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 3.8f),
                0,
                CosmicDischargeCommon.RiftColor(),
                Main.rand.NextFloat(0.6f, 0.8f));
            dust.noGravity = true;
        }
    }
}
