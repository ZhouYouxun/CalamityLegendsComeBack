using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick.Javelin
{
    public sealed class VesuviusRightJavelinVent : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0], 1f, 5f);
        private Vector2 VentAxis => Projectile.ai[1].ToRotationVector2().SafeNormalize(-Vector2.UnitY);

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 128;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            Lighting.AddLight(Projectile.Center, 0.72f, 0.22f, 0.04f);

            if (Projectile.localAI[0] == 1f)
                SpawnOpeningEffects();

            if (Projectile.owner == Main.myPlayer)
            {
                if (Projectile.localAI[1] < 6f && Projectile.localAI[0] % 16f == 3f)
                {
                    Projectile.localAI[1]++;
                    SpawnGravityFlame();
                }

                if (Projectile.localAI[2] < 5f && Projectile.localAI[0] % 20f == 8f)
                {
                    Projectile.localAI[2]++;
                    SpawnRisingSpark();
                }
            }

            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                Vector2 lift = -Vector2.UnitY.RotatedByRandom(0.42f) * Main.rand.NextFloat(0.8f, 2.8f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 10f),
                    DustID.InfernoFork,
                    lift,
                    90,
                    Main.rand.NextBool(3) ? Color.White : VesuviusProjectileVisuals.LavaOrange,
                    Main.rand.NextFloat(0.65f, 1.25f));
                dust.noGravity = true;
            }
        }

        private void SpawnGravityFlame()
        {
            Vector2 axis = VentAxis;
            Vector2 side = axis.RotatedBy(MathHelper.PiOver2);
            Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(5.2f, 8.6f) +
                side * Main.rand.NextFloat(-2.8f, 2.8f) +
                axis * Main.rand.NextFloat(0.6f, 2.2f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + Main.rand.NextVector2Circular(14f, 8f) - Vector2.UnitY * 4f,
                velocity,
                ModContent.ProjectileType<VesuviusRightGravityFlame>(),
                Math.Max(1, (int)(Projectile.damage * 0.32f)),
                Projectile.knockBack * 0.28f,
                Projectile.owner,
                Stage);
        }

        private void SpawnRisingSpark()
        {
            Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.52f) * Main.rand.NextFloat(7.5f, 11.5f) +
                Vector2.UnitX * Main.rand.NextFloat(-2f, 2f);

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + Main.rand.NextVector2Circular(10f, 6f) - Vector2.UnitY * 6f,
                velocity,
                ModContent.ProjectileType<VesuviusRightRisingSpark>(),
                Math.Max(1, (int)(Projectile.damage * 0.24f)),
                Projectile.knockBack * 0.18f,
                Projectile.owner,
                Stage);
        }

        private void SpawnOpeningEffects()
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.46f, Pitch = -0.28f }, Projectile.Center);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaGold, 0.62f, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, VesuviusProjectileVisuals.LavaOrange, new Vector2(1f, 0.68f), Main.rand.NextFloat(MathHelper.TwoPi), 0.07f, 0.88f, 18));

            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.InfernoFork,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f),
                    90,
                    Main.rand.NextBool(3) ? Color.White : VesuviusProjectileVisuals.LavaGold,
                    Main.rand.NextFloat(0.7f, 1.4f));
                dust.noGravity = Main.rand.NextBool(3);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float opacity = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 12f, Projectile.localAI[0], true);

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(255, 120, 36, 0) * 0.22f * opacity,
                0f,
                bloom.Size() * 0.5f,
                0.62f + Stage * 0.05f,
                SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    public sealed class VesuviusRightGravityFlame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Melee/VolcanicFireball";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0], 1f, 5f);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 150;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.velocity.X *= 0.992f;
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.27f + Stage * 0.012f, -18f, 16f);
            Projectile.rotation = Projectile.velocity.ToRotation();

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            Lighting.AddLight(Projectile.Center, 0.65f, 0.22f, 0.04f);
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.InfernoFork,
                    -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.12f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    90,
                    Main.rand.NextBool(3) ? Color.White : VesuviusProjectileVisuals.LavaOrange,
                    Main.rand.NextFloat(0.45f, 0.85f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 150);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.velocity = oldVelocity * 0.25f;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, 14);
            Projectile.tileCollide = false;
            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Color color = new Color(255, 172, 64, 0) * Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, oldCenter, frame, color * 0.28f * t, Projectile.oldRot[i], frame.Size() * 0.5f, Projectile.scale * MathHelper.Lerp(0.58f, 0.95f, t), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, color, Projectile.rotation, frame.Size() * 0.5f, Projectile.scale, SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    public sealed class VesuviusRightRisingSpark : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0], 1f, 5f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 132;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.Opacity = Utils.GetLerpValue(0f, 14f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 22f, Projectile.timeLeft, true);

            NPC target = FindTarget(760f);
            if (target != null && Projectile.localAI[0] > 12f)
            {
                float speed = MathHelper.Clamp(Projectile.velocity.Length() + 0.18f, 10f, 17f + Stage);
                Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center + target.velocity * 6f) * speed;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
            }
            else
            {
                Projectile.velocity.X *= 0.985f;
                Projectile.velocity.Y *= 0.988f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 0.62f * Projectile.Opacity, 0.28f * Projectile.Opacity, 0.08f * Projectile.Opacity);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center,
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.1f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.16f, 0.34f),
                    Main.rand.NextBool(4) ? Color.White : VesuviusProjectileVisuals.LavaGold));
            }
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }

            return bestTarget;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D streak = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;
            Color color = new Color(255, 188, 76, 0) * Projectile.Opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, color * 0.45f, 0f, bloom.Size() * 0.5f, 0.16f + Stage * 0.018f, SpriteEffects.None);
            Main.EntitySpriteDraw(streak, Projectile.Center - Main.screenPosition, null, color * 0.72f, Projectile.rotation, streak.Size() * 0.5f, new Vector2(0.28f, 0.74f), SpriteEffects.None);
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            Vector2[] points = Projectile.oldPos
                .Where(position => position != Vector2.Zero)
                .Select(position => position + Projectile.Size * 0.5f)
                .ToArray();

            if (points.Length == 0)
                points = new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (points[0] != Projectile.Center)
                points = new[] { Projectile.Center }.Concat(points).ToArray();

            if (points.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                points,
                new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Vector2.Zero, true, true, GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                points.Length * 3);
        }

        private float TrailWidth(float completion, Vector2 _) => Projectile.scale * 8f * (1f - completion) * Projectile.Opacity;

        private Color TrailColor(float completion, Vector2 _)
        {
            Color start = Color.Lerp(Color.White, VesuviusProjectileVisuals.LavaGold, 0.35f);
            Color end = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, Color.Transparent, completion);
            return Color.Lerp(start, end, completion) * (1f - completion) * Projectile.Opacity;
        }
    }
}
