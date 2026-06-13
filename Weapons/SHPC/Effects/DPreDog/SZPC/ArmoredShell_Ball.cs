using CalamityMod.Particles;
using CalamityMod.Projectiles.Magic;
using CalamityMod.Dusts;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using static CalamityMod.CalamityUtils;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    internal class ArmoredShell_Ball : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        private const float ExplosionLifetime = 10f;
        private const float ExplosionMaxScale = 2.35f;
        private const float VolterionShotSpeed = 14.4f;
        private const int VolterionShotLifetime = 12 * 15;

        public ref float OrbType => ref Projectile.ai[0];
        public ref float ExplosionTimer => ref Projectile.ai[1];
        public ref float InwardDirectionX => ref Projectile.localAI[0];
        public ref float InwardDirectionY => ref Projectile.localAI[1];

        public static Asset<Texture2D> Bloom;
        public static Asset<Texture2D> Explosion;

        public override string Texture => "CalamityMod/Projectiles/Magic/VolterionOrb";

        private Vector2 InwardDirection =>
            new Vector2(InwardDirectionX, InwardDirectionY).SafeNormalize(-Projectile.velocity.SafeNormalize(Vector2.UnitX));

        public override void Load()
        {
            Bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Explosion = ModContent.Request<Texture2D>("CalamityMod/Particles/PlasmaExplosion");
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 38;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.timeLeft = 40;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(InwardDirectionX);
            writer.Write(InwardDirectionY);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            InwardDirectionX = reader.ReadSingle();
            InwardDirectionY = reader.ReadSingle();
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];

            Lighting.AddLight(Projectile.Center, GetColor(OrbType).ToVector3());

            if (ExplosionTimer > 0f)
            {
                if (ExplosionTimer == 1f)
                    SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.55f, Pitch = 0.15f }, Projectile.Center);

                float progress = ExplosionTimer / ExplosionLifetime;
                float scaleLevel = PiecewiseAnimation(progress, new CurveSegment[]
                {
                    new CurveSegment(EasingType.PolyOut, 0f, 0f, 1f, 3)
                });

                Projectile.scale = MathHelper.Lerp(1f, ExplosionMaxScale, scaleLevel);
                Projectile.Opacity = 1f - progress;
                Projectile.velocity = Vector2.Zero;
                Projectile.rotation += 0.08f;
                Projectile.friendly = false;

                if (ExplosionTimer++ >= ExplosionLifetime)
                    Projectile.Kill();

                return;
            }

            Projectile.rotation += 0.01f;
            Projectile.velocity *= 0.95f;
            Projectile.Opacity = 1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            TriggerSmallExplosion();
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            TriggerSmallExplosion();
        }

        public override void OnKill(int timeLeft)
        {
            Vector2 inwardDirection = InwardDirection;
            Vector2 shotDirection = FindMouseNearestTargetDirection(inwardDirection);
            SpawnDeathFanFX(shotDirection);
            SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap, Projectile.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile lightning = Projectile.NewProjectileDirect(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                shotDirection * VolterionShotSpeed,
                ModContent.ProjectileType<ArmoredShell_Lightning>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                1f,
                1f
            );
            lightning.timeLeft = VolterionShotLifetime;
            lightning.DamageType = DamageClass.Magic;
        }

        private Vector2 FindMouseNearestTargetDirection(Vector2 fallbackDirection)
        {
            NPC target = null;
            float bestDistance = 2400f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float mouseDistance = Vector2.Distance(Main.MouseWorld, npc.Center);
                if (mouseDistance >= bestDistance)
                    continue;

                bestDistance = mouseDistance;
                target = npc;
            }

            if (target == null)
                return fallbackDirection;

            return (target.Center - Projectile.Center).SafeNormalize(fallbackDirection);
        }

        private void TriggerSmallExplosion()
        {
            if (ExplosionTimer > 0f)
                return;

            ExplosionTimer = 1f;
            Projectile.penetrate = -1;
            Projectile.netUpdate = true;
        }

        private void SpawnDeathFanFX(Vector2 inwardDirection)
        {
            Color color = GetColor(OrbType);
            Vector2 normal = inwardDirection.RotatedBy(MathHelper.PiOver2);

            for (int i = -3; i <= 3; i++)
            {
                float spread = i / 3f;
                Vector2 fanDirection = inwardDirection.RotatedBy(spread * 0.5f).SafeNormalize(inwardDirection);
                Vector2 fanVelocity = fanDirection * (4.4f + (1f - MathF.Abs(spread)) * 2.6f) + normal * (spread * 1.3f);

                BoltParticle bolt = new BoltParticle(
                    Projectile.Center + fanDirection * 4f,
                    fanVelocity,
                    false,
                    12 + (3 - Math.Abs(i)),
                    0.2f + (1f - MathF.Abs(spread)) * 0.08f,
                    Color.Lerp(color, Color.White, 0.18f),
                    new Vector2(0.34f, 0.92f + (1f - MathF.Abs(spread)) * 0.18f),
                    true
                );
                GeneralParticleHandler.SpawnParticle(bolt);

                Dust chemicalDust = Dust.NewDustPerfect(
                    Projectile.Center + fanDirection * 6f,
                    DustID.FireworksRGB,
                    fanVelocity * 0.7f
                );
                chemicalDust.noGravity = true;
                chemicalDust.noLight = true;
                chemicalDust.scale = 1f + (1f - MathF.Abs(spread)) * 0.18f;
                chemicalDust.fadeIn = 1.12f;
                chemicalDust.color = Color.Lerp(color, new Color(185, 255, 240), 0.35f);
            }

            for (int i = 0; i < 5; i++)
            {
                float angleOffset = MathHelper.Lerp(-0.32f, 0.32f, i / 4f);
                Vector2 puffDirection = inwardDirection.RotatedBy(angleOffset).SafeNormalize(inwardDirection);
                Vector2 puffVelocity = puffDirection * (2.1f + i * 0.45f);

                Dust mistDust = Dust.NewDustPerfect(
                    Projectile.Center + puffDirection * (4f + i),
                    DustID.Smoke,
                    puffVelocity
                );
                mistDust.noGravity = true;
                mistDust.scale = 1.1f + i * 0.08f;
                mistDust.fadeIn = 1.18f;
                mistDust.color = Color.Lerp(color, new Color(124, 255, 211), 0.42f);
            }

            SpawnSkytideDragoonMuzzleFX(inwardDirection, color);
        }

        private void SpawnSkytideDragoonMuzzleFX(Vector2 direction, Color baseColor)
        {
            Vector2 muzzlePos = Projectile.Center + direction * 38f;

            for (int i = 0; i < 2; i++)
            {
                Color color = Color.Lerp(Color.White, Main.rand.NextBool() ? Color.Orchid : Color.Cyan, 0.65f);
                Vector2 pos = muzzlePos + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-10f, 10f) + Main.rand.NextVector2Circular(3f, 3f);
                Vector2 vel = direction.RotatedByRandom(0.4f) * Main.rand.NextFloat(2f, 8f);

                Dust dust = Dust.NewDustPerfect(pos, ModContent.DustType<LightDust>(), vel, 0, default, Main.rand.NextFloat(1.85f, 2.2f));
                dust.noGravity = true;
                dust.color = color;

                Particle spark = new BoltParticle(
                    pos,
                    -vel.RotatedBy(MathHelper.ToRadians(45f)) * Main.rand.NextFloat(1.5f, 1.8f),
                    false,
                    13,
                    Main.rand.NextFloat(0.2f, 0.35f),
                    color * 0.8f,
                    new Vector2(1.2f, 1f),
                    true,
                    true,
                    false,
                    0.25f);
                GeneralParticleHandler.SpawnParticle(spark);

                if (i % 2 == 0)
                {
                    Particle orb = new CustomSpark(pos, Vector2.Zero, "CalamityMod/Particles/BloomCircle", false, 35, 0.75f, color * 0.75f, Vector2.One);
                    GeneralParticleHandler.SpawnParticle(orb);
                }
            }
        }

        public static Color GetColor(float type) =>
            Color.Lerp(new Color(51, 197, 255), new Color(143, 51, 255), 0.2f + 0.15f * type + 0.2f * MathF.Sin(Main.GlobalTimeWrappedHourly * 10f));

        public override Color? GetAlpha(Color lightColor) => GetColor(OrbType);

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color color = Projectile.GetAlpha(lightColor);

            if (ExplosionTimer > 0f)
            {
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                Texture2D explosionTex = Explosion.Value;
                Main.EntitySpriteDraw(explosionTex, drawPos, null, color * Projectile.Opacity, Projectile.rotation, explosionTex.Size() * 0.5f, 0.02f * Projectile.scale, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
                return false;
            }

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Texture2D bloomTex = Bloom.Value;
            Main.EntitySpriteDraw(bloomTex, drawPos, null, color * 0.5f, 0f, bloomTex.Size() * 0.5f, 0.42f, SpriteEffects.None);
            DrawSkytideDragoonTipFlash(color);
            Main.spriteBatch.ExitShaderRegion();
            return true;
        }

        private void DrawSkytideDragoonTipFlash(Color baseColor)
        {
            Asset<Texture2D> archSmear = ModContent.Request<Texture2D>("CalamityMod/Particles/ArchSmear");
            Asset<Texture2D> orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 direction = Projectile.velocity.SafeNormalize(InwardDirection);
            float fxRot = direction.ToRotation() + MathHelper.PiOver2;
            float sine = (float)Math.Sin(Main.GlobalTimeWrappedHourly * 55.5f / MathHelper.Pi);
            Vector2 tipCenter = Projectile.Center + direction * 30f;

            for (int i = 0; i < 6; i++)
            {
                Color tipColor = Color.Lerp(Color.Cyan, Color.Orchid, (i + 4) / 6f) with { A = 0 };
                tipColor *= 0.3f;
                Vector2 scale = new Vector2(0.25f - i * 0.04f, 1.5f + i * 0.15f) * Main.rand.NextFloat(0.9f, 1.1f);

                Main.EntitySpriteDraw(
                    archSmear.Value,
                    tipCenter - Main.screenPosition + direction * 10f,
                    null,
                    tipColor,
                    fxRot,
                    archSmear.Size() * 0.5f,
                    scale,
                    SpriteEffects.None);
            }

            for (int i = 0; i < 6; i++)
            {
                Color orbColor = Color.Lerp(Color.Lerp(Color.Cyan, Color.Orchid, (i + 2) / 6f), Color.White, i / 6f) with { A = 0 };
                orbColor *= 0.5f;
                Vector2 scale = new Vector2(Math.Abs(sine * 0.5f) + 0.1f, 1f) * (0.05f + i * 0.01f) * Main.rand.NextFloat(0.9f, 1.1f) * 2f;

                Main.EntitySpriteDraw(
                    orb.Value,
                    tipCenter - Main.screenPosition + direction * 22f,
                    null,
                    orbColor,
                    Main.rand.NextFloat(-5f, 5f),
                    orb.Size() * 0.5f,
                    scale,
                    SpriteEffects.None);
            }
        }

        public override bool? CanDamage() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
            CircularHitboxCollision(Projectile.Center, 20 * Projectile.scale, targetHitbox);
    }
}
