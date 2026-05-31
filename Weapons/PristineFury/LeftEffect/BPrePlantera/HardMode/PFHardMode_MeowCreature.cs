using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFHardMode_HeavyFireball : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/FireProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 54;
            Projectile.scale = 1.35f;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.2f, -18f, 15f);
            Projectile.rotation += Projectile.velocity.X * 0.025f;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.85f);

            if (Main.dedServ || !Main.rand.NextBool(2))
                return;

            GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                Projectile.Center - Projectile.velocity * 0.45f + Main.rand.NextVector2Circular(8f, 8f),
                -Projectile.velocity * 0.12f + Main.rand.NextVector2Circular(0.7f, 0.7f),
                Color.Lerp(ThemeColor, Color.DarkGoldenrod, 0.52f),
                Main.rand.Next(20, 32),
                Main.rand.NextFloat(0.75f, 1.25f),
                0.7f,
                Main.rand.NextFloat(-0.05f, 0.05f),
                glowing: true));

            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(8f, 18f) + Main.rand.NextVector2Circular(7f, 7f),
                -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.11f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                false,
                Main.rand.Next(12, 20),
                Main.rand.NextFloat(0.34f, 0.62f),
                Color.Lerp(ThemeColor, Color.White, Main.rand.NextFloat(0.12f, 0.42f)),
                true,
                false,
                true));

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    -Projectile.velocity * Main.rand.NextFloat(0.035f, 0.09f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    Color.Lerp(ThemeColor, Color.Goldenrod, Main.rand.NextFloat(0.16f, 0.38f)),
                    Color.Black,
                    Main.rand.NextFloat(0.36f, 0.72f),
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(-0.035f, 0.035f)));
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                int oldWidth = Projectile.width;
                int oldHeight = Projectile.height;
                Vector2 center = Projectile.Center;
                Projectile.width = Projectile.height = 170;
                Projectile.Center = center;
                Projectile.penetrate = -1;
                Projectile.Damage();
                Projectile.width = oldWidth;
                Projectile.height = oldHeight;
                Projectile.Center = center;

                for (int i = 0; i < 12; i++)
                {
                    float angle = MathHelper.Lerp(-MathHelper.Pi * 0.92f, -MathHelper.Pi * 0.08f, i / 11f);
                    Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(4.8f, 10.6f);
                    int fragment = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        center,
                        velocity,
                        ModContent.ProjectileType<PFHardMode_TotalityFire>(),
                        Math.Max(1, (int)(Projectile.damage * 0.54f)),
                        Projectile.knockBack * 0.25f,
                        Projectile.owner,
                        0f,
                        Main.rand.Next(8, 18));
                    PFLeftEffectRules.ApplyTheme(fragment, (PristineFuryMark)(int)Projectile.ai[2]);
                }
            }

            if (!Main.dedServ)
            {
                PFHardMode_TotalityFire.SpawnBurstEffects(Projectile.Center, ThemeColor, 1.6f);
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.78f, Pitch = -0.18f }, Projectile.Center);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CalamityUtils.CircularHitboxCollision(Projectile.Center, 34f * Projectile.scale, targetHitbox);

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 360);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Color theme = ThemeColor with { A = 0 };
            Vector2 center = Projectile.Center - Main.screenPosition;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, center, null, theme * 0.75f, Projectile.rotation, bloom.Size() * 0.5f, Projectile.scale * 0.58f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, center, null, Color.Lerp(theme, Color.White with { A = 0 }, 0.36f), Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.3f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
