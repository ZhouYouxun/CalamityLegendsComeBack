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

            if (Time >= 4f)
            {
                NPC target = FindBestTarget(980f);
                if (target != null)
                {
                    Vector2 desiredVel = Projectile.SafeDirectionTo(target.Center) * 18.5f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVel, 0.18f);
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
                Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    back * Main.rand.NextFloat(2f, 5f),
                    false,
                    12,
                    Main.rand.NextFloat(0.38f, 0.7f) * 0.3f,
                    CosmicDischargeCommon.ThreeColorSpark,
                    new Vector2(0.2f, 2.1f),
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
            if (!Main.dedServ)
                CosmicDischargeCommon.SpawnDoGImpact(target.Center, Projectile.velocity, false, false);
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
