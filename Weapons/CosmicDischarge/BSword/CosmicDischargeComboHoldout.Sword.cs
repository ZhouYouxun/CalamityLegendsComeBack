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
                CosmicDischargeCommon.SpawnSwordSwingTrail(TipPosition, Owner.MountedCenter + slashDirection * (SwordReach * 0.56f), angle, second);

                if (!impactEffectsPlayed && t >= 0.58f)
                {
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

            if (!second && Time == 15f)
                CosmicDischargeCommon.SpawnSwordSwingFlare(TipPosition);

            if (Time > SwordSwingWindup && Time <= strikeEnd)
            {
                float previousTime = Math.Max(SwordSwingWindup, Time - 1f);
                for (int i = 1; i <= SwordTipTrailSubsteps; i++)
                {
                    float subTime = MathHelper.Lerp(previousTime, Time, i / (float)SwordTipTrailSubsteps);
                    tipHistory.Add(GetSwordSwingTipPosition(subTime, swingSign));
                }

                TrimTipHistory(SwordSwingTipTrailFrames * SwordTipTrailSubsteps);
            }
            else
            {
                FadeTipHistory(SwordTipTrailSubsteps);
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

                CosmicDischargeCommon.SpawnSwordFinisherCharge(Owner, Time);
                SpawnSpinChargeDust(t);

                if (!Main.dedServ)
                {
                    // Swirling vortex particle effects
                    int spawnCount = ultActive ? 3 : 1;
                    for (int pIndex = 0; pIndex < spawnCount; pIndex++)
                    {
                        float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        float dist = MathHelper.Lerp(280f, 15f, t) + Main.rand.NextFloat(-20f, 20f);
                        Vector2 particlePos = Owner.Center + angle.ToRotationVector2() * dist;
                        Vector2 toPlayer = Owner.Center - particlePos;
                        Vector2 particleVel = toPlayer.RotatedBy(0.55f * Owner.direction).SafeNormalize(Vector2.Zero) * (ultActive ? 7.5f : 4.8f);
                        
                        GeneralParticleHandler.SpawnParticle(new SparkParticle(
                            particlePos,
                            particleVel,
                            false,
                            Main.rand.Next(12, 22),
                            Main.rand.NextFloat(0.45f, 0.82f),
                            CosmicDischargeCommon.RandomDoGColor()
                        ));
                    }
                }

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
                float previousTime = Math.Max(0f, Time - 1f);
                for (int i = 1; i <= SwordTipTrailSubsteps; i++)
                {
                    float subTime = MathHelper.Lerp(previousTime, Time, i / (float)SwordTipTrailSubsteps);
                    float subT = MathHelper.Clamp(subTime / SwordFinisherWindup, 0f, 1f);
                    float subSpinAngle = AimAngle + Owner.direction * (MathHelper.TwoPi * 2f * subT - MathHelper.PiOver2);
                    Vector2 subTip = Owner.MountedCenter + subSpinAngle.ToRotationVector2() * FinisherReach;

                    tipHistory.Add(subTip);
                }
                TrimTipHistory(24 * SwordTipTrailSubsteps);
                if (Time == SwordFinisherWindup)
                    CosmicDischargeCommon.SpawnSwordFinisherRelease(Owner);
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

            if (Time > SwordFinisherWindup && Time <= strikeEnd)
            {
                float previousTime = Math.Max(SwordFinisherWindup, Time - 1f);
                for (int i = 1; i <= SwordTipTrailSubsteps; i++)
                {
                    float subTime = MathHelper.Lerp(previousTime, Time, i / (float)SwordTipTrailSubsteps);

                    if (subTime <= SwordFinisherSlamFrame)
                    {
                        float lift = EaseOutCubic(Utils.GetLerpValue(SwordFinisherWindup, SwordFinisherSlamFrame, subTime, true));
                        float angle = AimAngle - Owner.direction * MathHelper.Lerp(1.38f, 1.05f, lift);
                        Vector2 subTip = Owner.MountedCenter + angle.ToRotationVector2() * FinisherReach;
                        tipHistory.Add(subTip);
                    }
                    else
                    {
                        float t = (subTime - SwordFinisherSlamFrame) / strikeFrames;
                        float slam = EaseOutCubic(t);
                        float angle = AimAngle + MathHelper.Lerp(-1.05f * Owner.direction, 1.22f * Owner.direction, slam);
                        Vector2 subTip = Owner.MountedCenter + angle.ToRotationVector2() * FinisherReach;
                        tipHistory.Add(subTip);
                    }
                }
                TrimTipHistory(SwordFinisherTipTrailFrames * SwordTipTrailSubsteps);
            }
            else
            {
                FadeTipHistory(SwordTipTrailSubsteps);
            }

            if (Time >= SwordFinisherDuration)
                Projectile.Kill();
        }
        private Vector2 GetSwordSwingTipPosition(float sampleTime, float swingSign)
        {
            float t = MathHelper.Clamp((sampleTime - SwordSwingWindup) / 8f, 0f, 1f);
            float strike = EaseOutCubic(t);
            float angle = AimAngle + MathHelper.Lerp(-0.86f * swingSign, 1.1f * swingSign, strike);
            return Owner.MountedCenter + angle.ToRotationVector2() * SwordReach;
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
        private void SpawnSpinChargeDust(float charge)
        {
            if (Main.dedServ || Main.rand.NextBool(2))
                return;

            Vector2 radius = Main.rand.NextVector2CircularEdge(58f + charge * 42f, 58f + charge * 42f);
            Dust dust = Dust.NewDustPerfect(
                Owner.MountedCenter + radius,
                DustID.PurpleTorch,
                radius.RotatedBy(MathHelper.PiOver2 * Owner.direction).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.2f, 3.8f),
                110,
                CosmicDischargeCommon.RandomDoGColor(),
                Main.rand.NextFloat(0.9f, 1.25f));
            dust.noGravity = true;
        }
        private void DrawSwordSmear()
        {
            if (tipHistory.Count < 2)
                return;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            bool ultActive = Owner.GetModPlayer<CosmicDischargePlayer>().UltimateFieldActive;
            bool empActive = Owner.GetModPlayer<CosmicDischargePlayer>().DevourerAscensionActive;
            
            float glowWidth = empActive ? 55f : (ultActive ? 40f : 25f);
            float coreWidth = empActive ? 20f : (ultActive ? 14f : 8f);

            for (int i = 0; i < tipHistory.Count - 1; i++)
            {
                float fade = 1f - i / (float)tipHistory.Count;
                Color glowColor = Color.Lerp(CosmicDischargeCommon.DoGFuchsiaColor, CosmicDischargeCommon.DoGCyanColor, 1f - fade);
                glowColor = CosmicDischargeCommon.Transparent(glowColor) * (0.55f * fade * Projectile.Opacity);
                Color coreColor = CosmicDischargeCommon.DoGWhiteColor * (0.78f * fade * Projectile.Opacity);

                Vector2 start = tipHistory[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 end = tipHistory[i + 1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 segment = end - start;

                if (segment.LengthSquared() < 0.1f)
                    continue;

                DrawLine(pixel, start, segment, glowColor, glowWidth * fade);
                DrawLine(pixel, start, segment, CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGCyanColor) * (0.28f * fade * Projectile.Opacity), glowWidth * 0.42f * fade);
                DrawLine(pixel, start, segment, coreColor, coreWidth * fade);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }
    }
}
