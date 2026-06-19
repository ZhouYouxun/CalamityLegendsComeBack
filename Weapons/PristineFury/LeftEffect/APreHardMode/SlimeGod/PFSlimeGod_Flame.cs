using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFSlimeGod_Flame : ModProjectile, ILocalizedModType
    {
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 224, 92));

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/ExtraTextures/TinyGreyscaleCircle";

        private ref float Timer => ref Projectile.localAI[0];
        private ref float BounceCount => ref Projectile.localAI[1];
        private const float VisualScale = 0.5f;
        private float BloomPower => Utils.Remap(Timer, 0f, 130f, 0.82f, 1.22f, true) * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 270;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 45;
        }

        public override void AI()
        {
            Timer++;

            if (Timer > 30f)
                Projectile.velocity *= 0.982f;

            Projectile.rotation += Projectile.velocity.X * 0.02f;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * 0.42f);
            EmitSlimeTrail();
        }

        private void EmitSlimeTrail()
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(ThemeColor, Color.White, 0.08f);
            if ((int)Timer % 8 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.25f, 0.9f),
                    false,
                    Main.rand.Next(10, 16),
                    Main.rand.NextFloat(0.24f, 0.42f) * (0.9f + BloomPower * 0.26f) * VisualScale,
                    Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.12f, 0.4f)),
                    true,
                    false,
                    true));
            }

            if (Main.rand.NextBool(BounceCount > 0f ? 2 : 7))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch);
                dust.scale = Main.rand.NextFloat(0.35f, 0.75f) * VisualScale;
                dust.velocity = -Projectile.velocity * 0.7f;
                dust.noGravity = true;
                dust.color = Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.04f, 0.22f));
            }
        }

        private void ReleaseSlimeRing(int count, float speed)
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(ThemeColor, Color.White, 0.24f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, theme, Vector2.One, Projectile.rotation, 0.02f, 0.38f * VisualScale, 20));

            for (int i = 0; i < count; i++)
            {
                float rot = MathHelper.TwoPi * i / count;
                Vector2 velocity = rot.ToRotationVector2() * speed;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity, Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch, velocity, 0, theme, Main.rand.NextFloat(1.1f, 1.65f) * VisualScale);
                dust.noGravity = true;

                if (i % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + velocity,
                        velocity * 0.25f,
                        false,
                        Main.rand.Next(14, 22),
                        Main.rand.NextFloat(0.28f, 0.48f) * VisualScale,
                        Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.12f, 0.38f)),
                        true,
                        false,
                        true));
                }
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            float bouncePower = BounceCount <= 0f ? Utils.Remap(Timer, 0f, 120f, 2.05f, 2.75f) : MathHelper.Clamp(1.14f - BounceCount * 0.12f, 0.62f, 1f);
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * bouncePower;
            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * bouncePower;

            BounceCount++;
            if (BounceCount > 7f)
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 80);

            ReleaseSlimeRing(8, 2.4f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(ThemeColor, Color.White, 0.08f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, theme, Vector2.One, 0f, 0.01f, 0.42f * VisualScale, 24));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                theme * 0.55f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Projectile.rotation,
                0.04f,
                0.24f * VisualScale,
                14,
                true));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Slimed, 240);
            target.AddBuff(BuffID.Slow, 120);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.85f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = Color.Lerp(ThemeColor, Color.White, 0.04f) * Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(theme, Color.Transparent, completion) * (1f - completion);
                float trailScale = Projectile.scale * MathHelper.Lerp(0.45f, 1.2f, 1f - completion) * VisualScale;
                Main.EntitySpriteDraw(texture, trailPos, null, trailColor, Projectile.rotation, texture.Size() * 0.5f, trailScale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, theme, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale * 1.15f * VisualScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, theme * 0.48f, Projectile.rotation, bloom.Size() * 0.5f, BloomPower * Projectile.scale * 0.65f * VisualScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.Lerp(theme, Color.White, 0.4f) * 0.55f, -Projectile.rotation * 0.5f, bloom.Size() * 0.5f, BloomPower * Projectile.scale * 0.32f * VisualScale, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();

            return false;
        }
    }
}
