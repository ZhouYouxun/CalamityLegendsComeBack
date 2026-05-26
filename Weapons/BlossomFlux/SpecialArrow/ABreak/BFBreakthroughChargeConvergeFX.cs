using System;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux
{
    internal sealed class BFBreakthroughChargeConvergeFX : ModProjectile
    {
        private const int Lifetime = 14;
        private static readonly Color MainColor = new(126, 255, 126);
        private static readonly Color AccentColor = new(238, 255, 214);

        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float HoldoutIndex => ref Projectile.ai[0];
        private ref float Phase => ref Projectile.ai[1];
        private ref float ChargeAtSpawn => ref Projectile.ai[2];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Timer++;
            Projectile.Opacity = BFChargeVisualHelper.PopOpacity(Timer, Lifetime);
            Vector2 anchor = GetAnchor(out Vector2 aimDirection);
            Vector2 side = aimDirection.RotatedBy(MathHelper.PiOver2);
            float progress = MathHelper.Clamp(Timer / Lifetime, 0f, 1f);

            Vector2 snap = anchor -
                aimDirection * MathHelper.Lerp(52f, -18f - ChargeAtSpawn * 18f, progress) +
                side * MathF.Sin(progress * MathHelper.TwoPi * 1.6f + Phase) * MathHelper.Lerp(26f, 0f, progress);

            Projectile.velocity = snap - Projectile.Center;
            Projectile.Center = snap;
            Projectile.rotation = aimDirection.ToRotation();
            Projectile.scale = MathHelper.Lerp(0.36f + ChargeAtSpawn * 0.12f, 0.05f, progress);
            Lighting.AddLight(Projectile.Center, MainColor.ToVector3() * 0.38f * Projectile.Opacity);

            if (!Main.dedServ)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.TerraBlade,
                    -aimDirection * Main.rand.NextFloat(0.8f, 2.2f) + Main.rand.NextVector2Circular(0.35f, 0.35f),
                    90,
                    Color.Lerp(MainColor, AccentColor, Main.rand.NextFloat(0.18f, 0.5f)),
                    Main.rand.NextFloat(0.72f, 1.08f) * Projectile.Opacity);
                dust.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                        Projectile.Center,
                        -aimDirection * Main.rand.NextFloat(0.6f, 1.4f),
                        false,
                        Main.rand.Next(5, 9),
                        Main.rand.NextFloat(0.014f, 0.026f),
                        AccentColor * Projectile.Opacity,
                        new Vector2(1.2f, 0.32f),
                        true));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, MainColor with { A = 0 } * (0.38f * Projectile.Opacity), 0f, bloom.Size() * 0.5f, Projectile.scale * 1.7f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(spark, drawPosition, null, AccentColor with { A = 0 } * (0.72f * Projectile.Opacity), Projectile.rotation + MathHelper.PiOver2, spark.Size() * 0.5f, new Vector2(0.04f, 0.24f), SpriteEffects.None, 0);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private Vector2 GetAnchor(out Vector2 aimDirection)
        {
            aimDirection = Vector2.UnitX;
            if (BFArrowCommon.InBounds(HoldoutIndex, Main.maxProjectiles))
            {
                Projectile holdout = Main.projectile[(int)HoldoutIndex];
                if (holdout.active && holdout.type == ModContent.ProjectileType<NewLegendBlossomFluxHoldOut>())
                {
                    aimDirection = holdout.velocity.SafeNormalize(Vector2.UnitX * Main.player[Projectile.owner].direction);
                    return holdout.Center + aimDirection * 42f;
                }
            }

            Player owner = Main.player[Projectile.owner];
            aimDirection = Vector2.UnitX * owner.direction;
            return owner.MountedCenter + aimDirection * 28f;
        }
    }
}
