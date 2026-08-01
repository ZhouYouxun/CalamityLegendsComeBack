using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingPollutionRound : ModProjectile, ILocalizedModType
    {
        private static readonly Color CoreColor  = new(170, 255, 238);
        private static readonly Color TrailColor = new(26, 128, 190);
        // ai[0] = burst index (0-based, which shot in the current burst)
        // ai[1] = left-click phase at fire time (1-6)
        // ai[2] = 1 if this round triggers the small nuke on hit
        private int  BurstIndex   => (int)Projectile.ai[0];
        private int  Phase        => (int)Projectile.ai[1];
        private bool NukeEligible => Projectile.ai[2] >= 0.5f;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture          => "CalamityLegendsComeBack/Texture/Calamity/RangePROJ/PlagueTaintedProjectile";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width          = 14;
            Projectile.height         = 14;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.tileCollide    = true;
            Projectile.ignoreWater    = true;
            Projectile.penetrate      = 2;
            Projectile.timeLeft       = 540;
            Projectile.extraUpdates   = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 10;
            Projectile.ArmorPenetration     = 18;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();

            if (Projectile.localAI[0] == 0f)
            {
                int phase = Phase;
                Projectile.penetrate = phase >= 4 ? 4 : (phase >= 2 ? 3 : 2);
                Projectile.localAI[1] = Main.rand.NextFloat(0.82f, 1.38f);
                Projectile.netUpdate  = true;
            }

            Projectile.spriteDirection = Projectile.direction;

            int phase2 = Phase;
            Lighting.AddLight(Projectile.Center, phase2 >= 4
                ? new Vector3(0.09f, 0.28f, 0.14f)
                : new Vector3(0.05f, 0.22f, 0.24f));

            if (Projectile.localAI[0]++ < 2f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            bool emitDust = phase2 >= 3 ? Main.rand.NextBool() : Main.rand.NextBool(2);
            if (emitDust)
            {
                Color dustColor;
                int dustType;
                if (phase2 >= 4)
                {
                    dustColor = Color.Lerp(SeasSearingPalette.BiohazardLime, SeasSearingPalette.ToxicGreen, Main.rand.NextFloat());
                    dustType  = Main.rand.NextBool(3) ? DustID.Vortex : 89;
                }
                else if (phase2 == 3)
                {
                    dustColor = Main.rand.NextBool() ? CoreColor : SeasSearingPalette.BiohazardLime;
                    dustType  = Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald;
                }
                else
                {
                    dustColor = Main.rand.NextBool() ? CoreColor : SeasSearingPalette.ToxicGreen;
                    dustType  = Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald;
                }

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    dustType,
                    -direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(0.6f, 1.9f),
                    110, dustColor,
                    Main.rand.NextFloat(0.45f, 0.78f + phase2 * 0.06f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Projectile.localAI[1] > 1.18f && Main.rand.NextBool(5))
            {
                Color sparkColor = phase2 >= 4 ? SeasSearingPalette.BiohazardLime : CoreColor;
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(3.5f, 3.5f),
                    DustID.GemDiamond,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    90, sparkColor,
                    Main.rand.NextFloat(0.5f, 0.88f));
                spark.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float distance       = Vector2.Distance(Main.player[Projectile.owner].Center, target.Center);
            float precisionBonus = 1f + Utils.GetLerpValue(380f, 1050f, distance, true) * 0.28f;
            modifiers.FinalDamage *= precisionBonus;
            modifiers.ScalingArmorPenetration += 0.08f;
            if (Projectile.localAI[1] > 1.2f)
                modifiers.FinalDamage *= 1f + (Projectile.localAI[1] - 1.2f) * 0.55f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int phase = Phase;

            int stackAmount = hit.Crit ? 6 : 4;
            if (target.boss || target.realLife >= 0) stackAmount += 1;
            if (phase >= 3) stackAmount += 1;
            if (phase >= 4) stackAmount += 1;

            // 普通弹幕击中也积累玩家辐射
            if (Main.myPlayer == Projectile.owner)
                Main.player[Projectile.owner].GetModPlayer<SeasSearingPlayer>().OnHitWithSeasSearing();

            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, stackAmount);
            target.AddBuff(BuffID.Venom, 240);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
            SpawnImpact(target.Center, true);

            if (Main.netMode != NetmodeID.Server && Main.myPlayer == Projectile.owner)
            {
                // Phase 3+: mid-burst rounds scatter a few pollution bubbles on hit.
                // In full-auto (phase 6) every round counts as a mid round.
                int  burstTotal = SS_Balance.GetPhaseBurstCount(phase);
                bool isMidRound = BurstIndex > 0 && BurstIndex < burstTotal - 1;
                if (phase >= 6 || (phase >= 3 && isMidRound))
                {
                    int bubbleCount = Main.rand.Next(2, 5);
                    for (int i = 0; i < bubbleCount; i++)
                    {
                        float   angle = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 vel   = angle.ToRotationVector2() * Main.rand.NextFloat(1.5f, 3.5f);
                        Projectile.NewProjectile(
                            Projectile.GetSource_OnHit(target), target.Center, vel,
                            ModContent.ProjectileType<SeasSearingPollutionBubble>(),
                            Math.Max(1, Projectile.damage * 35 / 100), 0f, Projectile.owner);
                    }
                }

                // Small nuke on the rounds the holdout flagged as nuke-eligible
                // (phase 5: last shot of every Nth burst; phase 6: every 30th round).
                if (NukeEligible)
                {
                    Projectile.NewProjectile(
                        Projectile.GetSource_OnHit(target), target.Center, Vector2.Zero,
                        ModContent.ProjectileType<SeasSearingNukesplosion>(),
                        Math.Max(1, Projectile.damage * 2), 0f, Projectile.owner);
                }
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.numHits <= 0)
                SpawnImpact(Projectile.Center, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture    = TextureAssets.Projectile[Type].Value;
            Texture2D bloom      = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   origin     = texture.Size() * 0.5f;
            Vector2   bloomOrigin = bloom.Size() * 0.5f;
            float     rawBoost  = Projectile.localAI[1];
            float     boost     = rawBoost > 0f ? MathHelper.Clamp(rawBoost, 0.82f, 1.38f) : 1f;

            int   phase     = Phase;
            Color headColor = phase >= 4 ? SeasSearingPalette.BiohazardLime : CoreColor;
            Color tailColor = phase >= 3
                ? Color.Lerp(TrailColor, SeasSearingPalette.ToxicGreen, 0.55f)
                : TrailColor;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;

                float   completion   = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                float   rotation     = Projectile.oldRot[i] + MathHelper.PiOver2;
                Color   color        = Color.Lerp(tailColor, headColor, completion) * (0.08f + completion * 0.42f * boost);
                color.A = 0;

                Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.72f, rotation, bloomOrigin, new Vector2(0.12f, 0.038f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture, drawPosition, null, color, rotation, origin, Projectile.scale * MathHelper.Lerp(0.72f, 1f, completion), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                (headColor with { A = 0 }) * (0.6f * boost), Projectile.rotation + MathHelper.PiOver2, bloomOrigin, new Vector2(0.15f, 0.045f), SpriteEffects.None, 0);

            Color outline = (Color.Lerp(headColor, SeasSearingPalette.ToxicGreen, 0.35f) with { A = 0 }) * (0.42f * boost);
            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * (2f + 0.8f * boost);
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition + offset, null,
                    outline, Projectile.rotation + MathHelper.PiOver2, origin, Projectile.scale * 1.08f, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                Color.White, Projectile.rotation + MathHelper.PiOver2, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        private static void SpawnImpact(Vector2 center, bool hitTarget)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(center, hitTarget ? 18 : 10, hitTarget ? 5.2f : 2.8f, 5f, hitTarget ? 1.05f : 0.75f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = hitTarget ? 0.24f : 0.14f, Pitch = -0.15f }, center);
        }
    }
}
