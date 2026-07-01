using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeDoGEnergyBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Time => ref Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 6;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.32f);
            float homingProgress = Utils.GetLerpValue(4f, 54f, Time, true);

            if (Time >= 4f)
            {
                NPC target = FindBestTarget(980f);
                if (target != null)
                {
                    float currentSpeed = Projectile.velocity.Length();
                    float baseSpeed = MathHelper.Clamp(currentSpeed, 10f, 18f);
                    float targetSpeed = MathHelper.Lerp(baseSpeed, 23f, homingProgress);
                    float turnRate = MathHelper.Lerp(0.055f, 0.24f, homingProgress);
                    Vector2 desiredVel = Projectile.SafeDirectionTo(target.Center) * targetSpeed;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, turnRate);
                }
            }

            if (Main.rand.NextBool(3))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.PurpleTorch,
                    Projectile.velocity * 0.2f,
                    100,
                    CosmicDischargeCommon.RandomDoGColor(false),
                    0.9f * 0.3f
                );
                d.noGravity = true;
            }

            if (!Main.dedServ)
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Vector2 back = -direction;

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(12f, 8f),
                        back * Main.rand.NextFloat(2.5f, 6.5f),
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(0.34f, 0.62f) * 0.3f,
                        CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.65f));
                }

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new NanoParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                        Main.rand.NextVector2Circular(1.5f, 1.5f) + back * Main.rand.NextFloat(0.5f, 1.6f),
                        CosmicDischargeCommon.DoGSpecialColor,
                        Main.rand.NextFloat(0.16f, 0.30f) * 0.3f,
                        12,
                        emitsLight: true));
                }

                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    back * Main.rand.NextFloat(0.25f, 0.9f) + Main.rand.NextVector2Circular(0.12f, 0.12f),
                    false,
                    10,
                    Main.rand.NextFloat(0.07f, 0.13f),
                    CosmicDischargeCommon.ThreeColorSpark,
                    true,
                    false,
                    true));

                if (Main.rand.NextBool(4))
                    GeneralParticleHandler.SpawnParticle(new BoltParticle(
                        Projectile.Center,
                        back.RotatedByRandom(0.5f) * Main.rand.NextFloat(2f, 6f),
                        false,
                        10,
                        0.45f * 0.3f,
                        CosmicDischargeCommon.DoGCyanColor,
                        new Vector2(0.1f, 3.2f),
                        true,
                        true));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 180);
            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LanceofDestinyStrong")
            {
                Volume = 0.34f,
                Pitch = 0.28f,
                MaxInstances = 5
            }, target.Center);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center,
                direction,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.3f,
                Vector2.One,
                direction.ToRotation(),
                0.032f,
                0.16f * 0.3f * CosmicDischargeCommon.ShockwaveFinalScaleMultiplier,
                12));

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new BoltParticle(
                    target.Center + Main.rand.NextVector2Circular(10f, 10f),
                    direction.RotatedByRandom(0.6f) * Main.rand.NextFloat(2f, 6f),
                    false,
                    Main.rand.Next(9, 14),
                    Main.rand.NextFloat(0.26f, 0.48f) * 0.3f,
                    Main.rand.NextBool() ? CosmicDischargeCommon.DoGCyanColor : CosmicDischargeCommon.DoGFuchsiaColor,
                    new Vector2(0.08f, 2.7f),
                    true,
                    true));
            }

            for (int i = 0; i < 7; i++)
            {
                GeneralParticleHandler.SpawnParticle(new NanoParticle(
                    target.Center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextVector2Circular(2.2f, 2.2f),
                    CosmicDischargeCommon.DoGSpecialColor,
                    Main.rand.NextFloat(0.18f, 0.32f) * 0.3f,
                    13,
                    emitsLight: true));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float factor = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    bloom,
                    drawPos,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGPurpleColor) * 0.32f * factor,
                    0f,
                    origin,
                    0.12f * factor,
                    SpriteEffects.None
                );
            }

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                CosmicDischargeCommon.DoGWhiteColor * 0.65f,
                0f,
                origin,
                0.16f,
                SpriteEffects.None
            );

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private NPC FindBestTarget(float maxDistance)
        {
            NPC marked = null;
            NPC normal = null;
            float closestMarked = maxDistance;
            float closestNormal = maxDistance;

            int markDebuff = ModContent.BuffType<CosmicDischargeDoGMarkDebuff>();

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float dist = Projectile.Distance(npc.Center);
                if (npc.HasBuff(markDebuff))
                {
                    if (dist < closestMarked)
                    {
                        closestMarked = dist;
                        marked = npc;
                    }
                }
                else
                {
                    if (dist < closestNormal)
                    {
                        closestNormal = dist;
                        normal = npc;
                    }
                }
            }

            return marked ?? normal;
        }
    }
}
