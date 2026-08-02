using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick
{
    /// <summary>
    /// 命中火山灾祸目标后喷出的岩浆。它沿触发弹幕原来的运动方向冲出去，
    /// 既能再次碰到原目标，也能扫到其身后的敌人，但不会继续复制新的岩浆喷流。
    /// </summary>
    public sealed class VesuviusMagmaSpout : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/AsteroidMolten";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 90;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public static void SpawnFromMarkedHit(Projectile source, NPC target, Vector2 direction, int damageDone)
        {
            int damage = Math.Max(1, (int)(damageDone * 0.18f));
            for (int i = -1; i <= 1; i++)
            {
                Vector2 velocity = direction.RotatedBy(i * 0.19f + Main.rand.NextFloat(-0.055f, 0.055f)) *
                    Main.rand.NextFloat(8.5f, 12.5f);
                Projectile.NewProjectile(
                    source.GetSource_FromThis(),
                    target.Center + direction * 10f,
                    velocity,
                    ModContent.ProjectileType<VesuviusMagmaSpout>(),
                    damage,
                    source.knockBack * 0.15f,
                    source.owner,
                    Main.rand.NextFloat(0.32f, 0.48f));
            }
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.scale = Projectile.ai[0] <= 0f ? 0.38f : Projectile.ai[0];
                Projectile.rotation = Projectile.velocity.ToRotation();
            }

            Projectile.velocity.Y += 0.085f;
            Projectile.velocity *= 0.995f;
            Projectile.rotation += Projectile.velocity.X * 0.025f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.55f, 0.17f, 0.035f));

            if (Projectile.numUpdates == 0 && !Main.dedServ)
            {
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.CopperCoin : DustID.InfernoFork,
                    backward.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.7f, 2.3f),
                    80,
                    Main.rand.NextBool(4) ? VesuviusProjectileVisuals.HotWhite : VesuviusProjectileVisuals.LavaOrange,
                    Main.rand.NextFloat(0.75f, 1.25f));
                ember.noGravity = true;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.25f, Pitch = 0.2f }, Projectile.Center);
            for (int i = 0; i < 5; i++)
            {
                Dust splash = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.CopperCoin,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4f),
                    80,
                    VesuviusProjectileVisuals.LavaOrange,
                    Main.rand.NextFloat(0.7f, 1.15f));
                splash.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D body = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Magic/AsteroidMoltenGlow").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null,
                VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * 0.5f,
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.38f, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1, body);
            Main.EntitySpriteDraw(glow, drawPosition, null, Color.White, Projectile.rotation,
                glow.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
