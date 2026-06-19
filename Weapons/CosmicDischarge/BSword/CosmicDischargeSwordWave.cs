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
    public class CosmicDischargeSwordWave : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private readonly List<Vector2> oldCenters = new();
        private ref float Time => ref Projectile.ai[0];

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 7;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.945f;
            Projectile.Opacity = Utils.GetLerpValue(0f, 4f, Time, true) * Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, CosmicDischargeCommon.DoGSpecialColor.ToVector3() * 0.48f);

            oldCenters.Insert(0, Projectile.Center);
            if (oldCenters.Count > 8)
                oldCenters.RemoveAt(oldCenters.Count - 1);

            if (Time == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.42f, Pitch = 0.28f }, Projectile.Center);
                ApplyScreenShake(3.6f);
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-26f, 26f),
                    DustID.PurpleTorch,
                    direction.RotatedByRandom(0.32f) * Main.rand.NextFloat(0.4f, 1.25f),
                    120,
                    CosmicDischargeCommon.RandomDoGColor(),
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;

                if (Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(34f, 18f),
                        -direction * Main.rand.NextFloat(2.5f, 6.5f),
                        false,
                        Main.rand.Next(12, 18),
                        Main.rand.NextFloat(0.34f, 0.62f),
                        CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.65f));
                }
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - direction * 18f;
            Vector2 end = Projectile.Center + direction * 118f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 42f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CosmicDischargeCommon.ApplyDoGDebuffs(target, 180);
            ApplyScreenShake(4.8f);

            if (!target.boss && target.knockBackResist > 0f)
            {
                Player player = Main.player[Projectile.owner];
                Vector2 pullDir = (player.Center - target.Center).SafeNormalize(Vector2.Zero);
                float dist = Vector2.Distance(player.Center, target.Center);
                if (dist > 100f)
                {
                    float pullSpeed = MathHelper.Clamp(dist / 15f, 8f, 22f);
                    target.velocity = pullDir * pullSpeed;
                    target.netUpdate = true;
                }
            }

            if (Main.dedServ)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LanceofDestinyStrong")
            {
                Volume = 0.42f,
                Pitch = 0.16f,
                MaxInstances = 4
            }, target.Center);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                target.Center,
                direction,
                CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.34f,
                Vector2.One,
                direction.ToRotation(),
                0.035f,
                0.22f,
                14));

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.86f) * Main.rand.NextFloat(2.4f, 8.8f);
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    target.Center + Main.rand.NextVector2Circular(14f, 14f),
                    velocity,
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.34f, 0.62f),
                    CosmicDischargeCommon.RandomDoGColor()));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = bloom.Size() * 0.5f;
            Vector2 scale = new Vector2(1.65f, 0.32f) * Projectile.Opacity;
            Color outer = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGPurpleColor) * 0.18f * Projectile.Opacity;
            Color inner = CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGSpecialColor) * 0.34f * Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = oldCenters.Count - 1; i >= 0; i--)
            {
                float fade = 1f - i / (float)oldCenters.Count;
                Main.EntitySpriteDraw(
                    bloom,
                    oldCenters[i] - Main.screenPosition,
                    null,
                    CosmicDischargeCommon.Transparent(CosmicDischargeCommon.DoGPurpleColor) * 0.12f * fade * Projectile.Opacity,
                    Projectile.rotation,
                    origin,
                    scale * MathHelper.Lerp(0.75f, 1.25f, fade),
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, outer, Projectile.rotation, origin, scale * 1.45f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, inner, Projectile.rotation, origin, scale, SpriteEffects.None);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            return false;
        }

        private void ApplyScreenShake(float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1300f, 120f, Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
