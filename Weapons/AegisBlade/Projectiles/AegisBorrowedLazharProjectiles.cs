using System;
using System.Linq;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles
{
    internal sealed class AegisBorrowedLazharLaser : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.AegisBlade";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const float MinimumHomingSpeed = 26f * 0.67f;
        private int timer;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 150;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 3;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            Projectile.alpha = Math.Max(0, Projectile.alpha - 35);

            float currentSpeed = Math.Max(Projectile.velocity.Length(), MinimumHomingSpeed);
            NPC target = FindTargetInFront(950f, MathHelper.ToRadians(58f));
            if (target != null)
            {
                Vector2 desiredDirection = Projectile.SafeDirectionTo(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX));
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * currentSpeed, 0.045f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.65f, 0.52f, 0.15f);

            if (!Main.dedServ && timer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    -Projectile.velocity * 0.08f,
                    false,
                    7,
                    Main.rand.NextFloat(0.18f, 0.32f),
                    Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.7f)),
                    true,
                    true));
            }
        }

        private NPC FindTargetInFront(float range, float coneHalfAngle)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            Vector2 heading = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || npc.life <= 0 || !npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                Vector2 toNpc = Projectile.SafeDirectionTo(npc.Center, heading);
                float angleDifference = Math.Abs(MathHelper.WrapAngle(toNpc.ToRotation() - heading.ToRotation()));
                if (angleDifference > coneHalfAngle)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            Vector2 contactPoint = Projectile.Center;
            Vector2 reflectDir = -Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 5; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    contactPoint,
                    reflectDir.RotatedByRandom(0.45f) * Main.rand.NextFloat(3f, 10f),
                    false,
                    12,
                    Main.rand.NextFloat(0.3f, 0.65f),
                    Color.Lerp(Color.Gold, Color.White, Main.rand.NextFloat(0.2f, 0.8f)),
                    true,
                    true));
            }

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(contactPoint, Vector2.Zero, 0.6f, Color.Gold, 15));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 2; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    Main.rand.NextVector2Circular(2f, 2f),
                    false,
                    6,
                    0.25f,
                    Color.Gold,
                    true,
                    true));
            }
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] points = BuildTrailPoints();
            if (points.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));

            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(WidthFunction, ColorFunction, OffsetFunction, true, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                points.Length * 2);

            Vector2[] corePoints = points.Take(Math.Min(12, points.Length)).ToArray();
            if (corePoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(CoreWidthFunction, CoreColorFunction, OffsetFunction, true, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                corePoints.Length * 2);
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] points = Projectile.oldPos
                .Where(pos => pos != Vector2.Zero)
                .Select(pos => pos + Projectile.Size * 0.5f)
                .ToArray();

            if (points.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (points[0] != Projectile.Center)
                points = new[] { Projectile.Center }.Concat(points).ToArray();

            return points;
        }

        private Vector2 OffsetFunction(float completion, Vector2 _)
        {
            float waviness = (float)Math.Sin(completion * MathHelper.Pi * 1.5f + Main.GlobalTimeWrappedHourly * 16f) * 0.8f;
            return Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * waviness;
        }

        private float WidthFunction(float completion, Vector2 _)
        {
            const float ratio = 0.15f;
            float baseWidth = Projectile.scale * 14f;
            if (completion < ratio)
                return MathF.Sin(completion / ratio * MathHelper.PiOver2) * baseWidth + 0.1f;

            return Utils.Remap(completion, ratio, 1f, baseWidth, 0f);
        }

        private Color ColorFunction(float completion, Vector2 _)
        {
            Color body = Color.Lerp(Color.Lerp(Color.White, Color.Gold, 0.3f), Color.OrangeRed, completion * 0.7f) * Projectile.Opacity;
            Color fade = Color.Lerp(body, Color.Transparent, Utils.GetLerpValue(0.7f, 1f, completion, true));
            fade.A = 0;
            return Color.Lerp(body, fade, completion);
        }

        private float CoreWidthFunction(float completion, Vector2 _) => WidthFunction(completion, _) * 0.46f;

        private Color CoreColorFunction(float completion, Vector2 _)
        {
            Color body = Color.Lerp(Color.White, Color.Gold, completion * 0.4f) * Projectile.Opacity;
            Color fade = Color.Lerp(body, Color.Transparent, Utils.GetLerpValue(0.75f, 1f, completion, true));
            fade.A = 0;
            return Color.Lerp(body, fade, completion);
        }
    }

    internal sealed class AegisBorrowedOrbitalStrike : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.AegisBlade";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int MaxLifetime = 90;
        private const float BeamHeight = 1200f;
        private int timer;
        private bool impacted;

        private int TargetIndex => (int)Projectile.ai[0];
        private Vector2 Destination => new(Projectile.ai[1], Projectile.ai[2]);

        public override void SetDefaults()
        {
            Projectile.width = 48;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = MaxLifetime;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            timer++;
            if (timer == 1)
            {
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.48f, Pitch = 0.18f }, Projectile.Center);
                Player owner = Main.player[Projectile.owner];
                if (owner.active && Projectile.owner == Main.myPlayer)
                    owner.Calamity().GeneralScreenShakePower = Math.Max(owner.Calamity().GeneralScreenShakePower, 3f);
            }

            Vector2 destination = Destination;
            if (destination == Vector2.Zero)
                destination = Projectile.Center + Vector2.UnitY * 800f;

            Projectile.velocity = Vector2.UnitY * Math.Max(34f, Projectile.velocity.Y);
            Projectile.rotation = MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.72f, 0.54f, 0.16f);

            if (!Main.dedServ && timer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 8f),
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    false,
                    10,
                    Main.rand.NextFloat(0.22f, 0.38f),
                    Color.Lerp(Color.Gold, Color.White, 0.28f),
                    true,
                    true));
            }

            if (Projectile.Center.Y >= destination.Y)
            {
                Projectile.Center = destination;
                Impact();
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 startPoint = Projectile.Center - Vector2.UnitY * BeamHeight;
            Vector2 endPoint = Projectile.Center;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), startPoint, endPoint, 28f * Projectile.scale, ref collisionPoint);
        }

        private void Impact()
        {
            if (impacted)
                return;

            impacted = true;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;
            SpawnExplosionParticles();
            Projectile.Kill();
        }

        private void SpawnExplosionParticles()
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.52f, Pitch = 0.12f }, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(24f, 12f),
                    new Vector2(Main.rand.NextFloat(-4f, 4f), Main.rand.NextFloat(-6f, -1f)),
                    false,
                    18,
                    Main.rand.NextFloat(0.6f, 1f),
                    Color.Lerp(Color.Gold, Color.OrangeRed, Main.rand.NextFloat(0.1f, 0.5f)),
                    true,
                    true));
            }

            GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(Projectile.Center, Vector2.Zero, 1.15f, Color.Gold, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(Color.Gold, Color.White, 0.18f),
                new Vector2(1.2f, 0.55f),
                0f,
                0.18f,
                0.04f,
                18));
        }

        public override void OnKill(int timeLeft)
        {
            if (!impacted)
                SpawnExplosionParticles();
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] beamPoints = new Vector2[12];
            for (int i = 0; i < beamPoints.Length; i++)
            {
                float ratio = i / (float)(beamPoints.Length - 1);
                beamPoints[i] = Projectile.Center - new Vector2(0f, BeamHeight * (1f - ratio));
            }

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(
                beamPoints,
                new PrimitiveSettings(WidthFunction, ColorFunction, OffsetFunction, true, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                beamPoints.Length * 2);

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(
                beamPoints,
                new PrimitiveSettings(CoreWidthFunction, CoreColorFunction, OffsetFunction, true, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                beamPoints.Length * 2);
        }

        private Vector2 OffsetFunction(float completion, Vector2 _)
        {
            float shimmer = (float)Math.Sin(completion * MathHelper.TwoPi + Main.GlobalTimeWrappedHourly * 20f) * 1.5f;
            return Vector2.UnitX * shimmer;
        }

        private float WidthFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            float taper = (float)Math.Sin(completion * MathHelper.Pi);
            return 26f * progress * (0.8f + taper * 0.2f) * Projectile.scale;
        }

        private Color ColorFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            Color color = Color.Lerp(Color.Gold, Color.OrangeRed, completion * 0.5f) * progress;
            color.A = 0;
            return color;
        }

        private float CoreWidthFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            float taper = (float)Math.Sin(completion * MathHelper.Pi);
            return 11f * progress * (0.9f + taper * 0.1f) * Projectile.scale;
        }

        private Color CoreColorFunction(float completion, Vector2 _)
        {
            float progress = Projectile.timeLeft / (float)MaxLifetime;
            Color color = Color.Lerp(Color.White, Color.Gold, completion * 0.3f) * progress;
            color.A = 0;
            return color;
        }
    }
}
