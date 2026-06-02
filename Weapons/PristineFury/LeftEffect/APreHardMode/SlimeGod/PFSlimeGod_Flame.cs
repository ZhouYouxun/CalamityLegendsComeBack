using CalamityMod;
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
        private bool HasBounced => BounceCount > 0f;
        private bool CanHome => Timer >= 120f && !HasBounced;
        private float BloomPower => Utils.Remap(Timer, 0f, 160f, 0.32f, CanHome ? 1.6f : 0.92f, true) * Utils.GetLerpValue(0f, 40f, Projectile.timeLeft, true);

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

            if (Timer == 120f && !HasBounced)
            {
                Projectile.penetrate = 1;
                Projectile.damage = (int)(Projectile.originalDamage * 1.55f);
                Projectile.velocity *= 0.16f;
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                ReleaseSlimeRing(10, 3.1f);
            }
            else if (Timer > 54f && !CanHome)
                Projectile.velocity *= 0.972f;

            if (CanHome)
            {
                Projectile.rotation += MathHelper.ToRadians(2f);
                HomeToCursorBiasedTarget();
            }
            else
                Projectile.rotation += Projectile.velocity.X * 0.02f;

            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (CanHome ? 0.72f : 0.42f));
            EmitSlimeTrail();
        }

        private void HomeToCursorBiasedTarget()
        {
            NPC best = null;
            float bestScore = 620f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float score = Projectile.Distance(npc.Center) + Vector2.Distance(Main.MouseWorld, npc.Center) * 0.12f;
                if (score < bestScore)
                {
                    bestScore = score;
                    best = npc;
                }
            }

            if (best != null)
            {
                float trackingPower = Utils.GetLerpValue(120f, 210f, Timer, true);
                Vector2 currentDirection = Projectile.velocity.SafeNormalize(Projectile.SafeDirectionTo(best.Center));
                Vector2 desiredDirection = Projectile.SafeDirectionTo(best.Center, currentDirection);
                float speed = MathHelper.Lerp(2.2f, 9.2f, trackingPower);
                float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(1.5f), MathHelper.ToRadians(6.5f), trackingPower);
                Vector2 newDirection = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn).ToRotationVector2();
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, newDirection * speed, 0.055f + trackingPower * 0.075f);
            }
        }

        private void EmitSlimeTrail()
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(ThemeColor, Color.White, CanHome ? 0.28f : 0.08f);
            if (CanHome)
            {
                if (Timer % 6f == 0f)
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, theme * 0.75f, Vector2.One, Projectile.rotation, 0.02f, 0.18f + BloomPower * 0.1f, 18));

                if (Timer % 5f == 0f)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                        Main.rand.NextVector2Circular(0.35f, 0.35f),
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.22f, 0.4f) * (0.8f + BloomPower * 0.35f),
                        Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.12f, 0.42f)),
                        true,
                        false,
                        true));
                }

                return;
            }

            if (Main.rand.NextBool(BounceCount > 0f ? 2 : 7))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(10f, 10f), Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch);
                dust.scale = Main.rand.NextFloat(0.35f, 0.75f);
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
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, theme, Vector2.One, Projectile.rotation, 0.02f, 0.38f, 20));

            for (int i = 0; i < count; i++)
            {
                float rot = MathHelper.TwoPi * i / count;
                Vector2 velocity = rot.ToRotationVector2() * speed;
                Dust dust = Dust.NewDustPerfect(Projectile.Center + velocity, Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch, velocity, 0, theme, Main.rand.NextFloat(1.1f, 1.65f));
                dust.noGravity = true;

                if (i % 2 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + velocity,
                        velocity * 0.25f,
                        false,
                        Main.rand.Next(14, 22),
                        Main.rand.NextFloat(0.28f, 0.48f),
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
            Projectile.penetrate = -1;
            if (BounceCount > 7f)
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 80);

            ReleaseSlimeRing(8, 2.4f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color theme = Color.Lerp(ThemeColor, Color.White, CanHome ? 0.24f : 0.08f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, theme, Vector2.One, 0f, 0.01f, 0.42f, 24));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                theme * 0.55f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One,
                Projectile.rotation,
                0.04f,
                CanHome ? 0.38f : 0.24f,
                14,
                true));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Slimed, CanHome ? 480 : 240);
            target.AddBuff(BuffID.Slow, CanHome ? 240 : 120);
            if (CanHome)
                ReleaseSlimeRing(14, 3.4f);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0 && !CanHome)
                Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.85f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color theme = (Color.Lerp(ThemeColor, Color.White, CanHome ? 0.22f : 0.04f) with { A = 0 }) * Projectile.Opacity;

            PFLeftEffectRules.BeginAdditive();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = i / (float)Projectile.oldPos.Length;
                Vector2 trailPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = Color.Lerp(theme, Color.Transparent, completion) * (1f - completion);
                Main.EntitySpriteDraw(texture, trailPos, null, trailColor, 0f, texture.Size() * 0.5f, Projectile.scale * MathHelper.Lerp(0.16f, 1f, 1f - completion), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, theme, Projectile.rotation, texture.Size() * 0.5f, Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, theme * (CanHome ? 0.58f : 0.34f), Projectile.rotation, bloom.Size() * 0.5f, BloomPower * Projectile.scale * 0.24f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, drawPosition, null, Color.Lerp(theme, Color.White with { A = 0 }, 0.32f) * 0.38f, Projectile.rotation, bloom.Size() * 0.5f, BloomPower * Projectile.scale * 0.11f, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
