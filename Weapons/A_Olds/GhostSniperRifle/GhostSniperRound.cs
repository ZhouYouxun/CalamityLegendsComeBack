using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Olds.GhostSniperRifle
{
    public class GhostSniperRound : ModProjectile, ILocalizedModType
    {
        private static readonly Color GhostWhite = new(245, 255, 255);
        private static readonly Color GhostBlue = new(150, 238, 255);

        public new string LocalizationCategory => "Projectiles.GhostSniperRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        private int time;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 24;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.light = 0.55f;
            Projectile.alpha = 255;
            Projectile.extraUpdates = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 5;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.ArmorPenetration = 25;
        }

        public override void AI()
        {
            time++;
            float speed = Projectile.velocity.Length();

            if (Projectile.alpha > 0)
                Projectile.alpha -= (int)(speed * 0.9f);
            if (Projectile.alpha < 0)
                Projectile.alpha = 0;

            Projectile.scale = 1.38f;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, GhostBlue.ToVector3() * 0.65f);

            NPC target = FindTarget();
            if (target != null)
                HomeInto(target);
            else
                Projectile.velocity *= 1.002f;

            if (Projectile.timeLeft == 597)
                SpawnInitialBurst();

            SpawnGhostTrail();
        }

        public override Color? GetAlpha(Color lightColor) =>
            Projectile.alpha < 140 ? new Color(230, 255, 255, 120) : Color.Transparent;

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.Center, Projectile.velocity, Projectile.width, Projectile.height);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.55f }, Projectile.Center);
            SpawnImpactBurst(Projectile.Center, oldVelocity.SafeNormalize(Vector2.UnitX));
            return true;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            SpawnImpactBurst(target.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX));

            if (Projectile.numHits > 0)
                Projectile.damage = (int)(Projectile.damage * 0.88f);

            if (Projectile.damage < 1)
                Projectile.damage = 1;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Vector2 releaseVelocity = -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.45f) * Main.rand.NextFloat(4f, 7f);
                Projectile.NewProjectile(
                    Projectile.GetSource_OnHit(target),
                    target.Center,
                    releaseVelocity,
                    ModContent.ProjectileType<GhostSniperSpectreOrb>(),
                    Math.Max(18, damageDone / 5),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner);
            }

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/EtherealCoreUse") with { Volume = 0.55f, Pitch = 0.25f, PitchVariance = 0.12f }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 trailDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2[] trailPoints = BuildTrailPoints(trailDirection);

            if (GameShaders.Misc.TryGetValue("CalamityMod:SideStreakTrail", out MiscShaderData shader))
            {
                shader.UseImage1("Images/Misc/Perlin");

                PrimitiveRenderer.RenderTrail(
                    trailPoints,
                    new PrimitiveSettings(TrailWidthFunction, TrailColorFunction, (_, _) => Projectile.Size * 0.5f, shader: shader),
                    46);
            }

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Vector2 origin = texture.Size() * 0.5f;
            for (int i = 0; i < 7; i++)
            {
                float completion = i / 6f;
                Vector2 ghostPosition = drawPosition - trailDirection * completion * 42f;
                Color color = Color.Lerp(GhostWhite, GhostBlue, completion * 0.75f) with { A = 0 };
                Main.EntitySpriteDraw(texture, ghostPosition, null, color * (1f - completion) * 0.75f, Projectile.rotation, origin, Projectile.scale * (0.62f - completion * 0.16f), SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White with { A = 0 } * 0.85f, Projectile.rotation, origin, Projectile.scale * 0.6f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private NPC FindTarget()
        {
            NPC best = null;
            float bestScore = 2200f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toTarget = npc.Center - Projectile.Center;
                float distance = toTarget.Length();
                if (distance > 2200f)
                    continue;

                float alignment = Vector2.Dot(forward, toTarget.SafeNormalize(forward));
                float score = distance * MathHelper.Lerp(1.1f, 0.45f, Utils.GetLerpValue(-0.25f, 1f, alignment, true));
                if (npc.boss)
                    score *= 0.65f;

                if (score >= bestScore)
                    continue;

                best = npc;
                bestScore = score;
            }

            return best;
        }

        private void HomeInto(NPC target)
        {
            float homingPower = Utils.GetLerpValue(0f, 46f, time, true);
            Vector2 predictedCenter = target.Center + target.velocity * MathHelper.Lerp(10f, 24f, homingPower);
            Vector2 desiredDirection = (predictedCenter - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
            float desiredSpeed = MathHelper.Clamp(Projectile.velocity.Length() + 0.025f, 12f, MathHelper.Lerp(16f, 24f, homingPower));
            float turnStrength = MathHelper.Lerp(0.018f, 0.072f, homingPower);

            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredDirection * desiredSpeed, turnStrength);
            Projectile.velocity = Projectile.velocity.SafeNormalize(desiredDirection) * MathHelper.Clamp(Projectile.velocity.Length(), 10f, 26f);
        }

        private void SpawnInitialBurst()
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i <= 15; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.position,
                    Main.rand.NextBool(3) ? DustID.SpectreStaff : DustID.GemDiamond,
                    forward.RotatedByRandom(MathHelper.ToRadians(30f)) * Main.rand.NextFloat(0.4f, 3.2f),
                    70,
                    Main.rand.NextBool(3) ? GhostBlue : Color.White,
                    Main.rand.NextFloat(0.6f, 1.1f));
                dust.noGravity = true;
            }
        }

        private void SpawnGhostTrail()
        {
            if (Projectile.timeLeft < 450)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                    Projectile.Center,
                    -Projectile.velocity * 0.045f,
                    false,
                    15,
                    Main.rand.NextFloat(0.65f, 1.05f),
                    GhostBlue * 0.28f));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(6f, 22f),
                    DustID.SpectreStaff,
                    -forward.RotatedByRandom(0.3f) * Main.rand.NextFloat(0.2f, 1.2f),
                    110,
                    Color.White,
                    Main.rand.NextFloat(0.45f, 0.9f));
                dust.noGravity = true;
            }
        }

        private static void SpawnImpactBurst(Vector2 center, Vector2 direction)
        {
            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.SpectreStaff : DustID.GemDiamond,
                    direction.RotatedByRandom(0.75f) * Main.rand.NextFloat(-1.2f, 4.2f) + Main.rand.NextVector2Circular(1.2f, 1.2f),
                    70,
                    Main.rand.NextBool(3) ? GhostBlue : Color.White,
                    Main.rand.NextFloat(0.75f, 1.35f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, GhostBlue with { A = 0 } * 0.45f, 0.36f, 16));
        }

        private Vector2[] BuildTrailPoints(Vector2 trailDirection)
        {
            Vector2 fallbackTopLeft = Projectile.Center - Projectile.Size * 0.5f;
            Vector2[] trailPoints = Projectile.oldPos
                .Select((oldPosition, index) =>
                {
                    bool invalid = oldPosition == Vector2.Zero || Vector2.DistanceSquared(oldPosition, fallbackTopLeft) > 2400f * 2400f;
                    return invalid ? fallbackTopLeft - trailDirection * 44f * index / Math.Max(1, Projectile.oldPos.Length - 1) : oldPosition;
                })
                .ToArray();

            return new[] { fallbackTopLeft }.Concat(trailPoints).ToArray();
        }

        private float TrailWidthFunction(float completion, Vector2 _) =>
            Projectile.scale * 16f * (float)Math.Sin(completion * MathHelper.Pi) * Projectile.Opacity;

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color color = Color.Lerp(GhostWhite, GhostBlue, completion * 0.8f);
            color.A = 0;
            return color * (1f - completion) * Projectile.Opacity * 0.95f;
        }
    }
}
