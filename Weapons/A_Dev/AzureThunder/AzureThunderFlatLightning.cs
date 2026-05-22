using System;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    internal sealed class AzureThunderFlatLightning : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float BounceUsed => ref Projectile.localAI[0];
        private int time;
        private float colorValue;
        private float sizeMult = 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 60;
            Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 18;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            colorValue = MathHelper.Lerp(colorValue, 50f, 0.025f);
            Color usedColor = Color.Lerp(AzureThunderColors.Yellow, AzureThunderColors.Azure, Utils.GetLerpValue(0f, 50f, colorValue, true) * 0.22f);

            if (time == 0)
            {
                colorValue += 30f;
                sizeMult = Projectile.ai[1] <= 0f ? 1f : Projectile.ai[1];
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, usedColor.ToVector3() * 0.58f);

            if (Vector2.Distance(owner.Center, Projectile.Center) < 1600f && Projectile.timeLeft > 5)
                SpawnFlightFX(usedColor);

            if (Projectile.ai[1] == 0.5f && Projectile.timeLeft == 1)
                SpawnEndPulse();

            time++;
        }

        private void SpawnFlightFX(Color usedColor)
        {
            Vector2 position = Projectile.Center;

            if (Projectile.timeLeft % 4 == 0)
            {
                if (time < 120)
                {
                    float velocityMult = Projectile.ai[1] == 0.5f ? 0.2f : 3f * sizeMult;
                    Particle spark = new CustomSpark(
                        position,
                        Projectile.velocity * 1.2f * velocityMult,
                        "CalamityMod/Particles/GlowSpark",
                        false,
                        11,
                        0.15f * sizeMult,
                        usedColor,
                        new Vector2(2f, 0.8f),
                        true,
                        true,
                        shrinkSpeed: 1f);
                    GeneralParticleHandler.SpawnParticle(spark);
                    sizeMult *= 0.97f;
                }

                GeneralParticleHandler.SpawnParticle(new BoltParticle(
                    position,
                    -Projectile.velocity * 0.05f,
                    false,
                    30,
                    0.6f,
                    usedColor,
                    new Vector2(1.8f, 0.8f),
                    true,
                    true,
                    false,
                    0.3f));
            }

            if (Main.rand.NextBool(35))
            {
                GeneralParticleHandler.SpawnParticle(new BoltParticle(
                    position,
                    Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.9f),
                    false,
                    23,
                    Main.rand.NextFloat(0.2f, 0.25f),
                    usedColor,
                    new Vector2(1.8f, 0.8f),
                    true,
                    true,
                    false,
                    0.3f));
            }

            if (time % 5 == 0)
            {
                Dust dust = Dust.NewDustPerfect(
                    position,
                    DustID.FireworksRGB,
                    new Vector2(5f, 5f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f),
                    0,
                    default,
                    Main.rand.NextFloat(0.45f, 0.6f));
                dust.noGravity = true;
                dust.color = usedColor;
            }
        }

        private void SpawnEndPulse()
        {
            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, AzureThunderColors.Yellow, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.1f, 1.48f, 15));
                GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.1f, 0.925f, 15));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMult = Utils.Remap(Projectile.numHits, 0f, 3f, 1f, 0.15f, true);
            modifiers.SourceDamage *= damageMult;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            SpawnHitFX(target.Center, BounceUsed >= 1f ? 2.1f : 3f);

            Vector2 launchVelocity = Projectile.Center.DirectionTo(target.Center);
            target.MoveNPC(launchVelocity, 20f, true);

            if (BounceUsed >= 1f)
            {
                Projectile.Kill();
                return;
            }

            BounceUsed = 1f;
            NPC nextTarget = FindBounceTarget(target);
            if (nextTarget == null)
            {
                Projectile.Kill();
                return;
            }

            float speed = MathHelper.Max(Projectile.velocity.Length(), 14.4f);
            Projectile.Center = target.Center;
            Projectile.velocity = (nextTarget.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, 90);
            sizeMult = MathHelper.Max(sizeMult, 0.72f);
            colorValue += 18f;
            Projectile.netUpdate = true;
        }

        private void SpawnHitFX(Vector2 position, float fxScale)
        {
            for (int i = 0; i < (int)(7 * fxScale); i++)
            {
                Particle spark = new BoltParticle(
                    position,
                    (new Vector2(4f, 4f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.3f, 1.9f),
                    true,
                    13,
                    Main.rand.NextFloat(0.1f, 0.15f) * fxScale,
                    Main.rand.NextBool(5) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure,
                    new Vector2(1.8f, 0.8f),
                    true,
                    true,
                    false,
                    0.7f);
                GeneralParticleHandler.SpawnParticle(spark);

                Dust dust = Dust.NewDustPerfect(
                    position,
                    ModContent.DustType<LightDust>(),
                    (new Vector2(5f, 5f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f),
                    0,
                    default,
                    Main.rand.NextFloat(0.4f, 0.55f) * fxScale);
                dust.noGravity = !Main.rand.NextBool(3);
                dust.color = Main.rand.NextBool(5) ? AzureThunderColors.PaleYellow : AzureThunderColors.Azure;
            }

            GeneralParticleHandler.SpawnParticle(new CustomPulse(position, Vector2.Zero, AzureThunderColors.Yellow, "CalamityMod/Particles/HighResFoggyCircleHardEdge", Vector2.One, 0f, 0f, 0.05705f * fxScale, 10));
        }

        private NPC FindBounceTarget(NPC previousTarget)
        {
            NPC bestTarget = null;
            float bestDistance = 1300f;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.whoAmI == previousTarget.whoAmI || !npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Projectile.Distance(npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float size = 45f * sizeMult * (Projectile.numHits > 0 ? 1.35f : 1f);
            if (time <= 1)
            {
                float collisionPoint = float.NaN;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - Projectile.velocity, Projectile.Center, size, ref collisionPoint);
            }

            return CalamityUtils.CircularHitboxCollision(Projectile.Center, size, targetHitbox);
        }

        public override bool? CanCutTiles() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 previous = Projectile.Center;

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                Vector2 current = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (current == Projectile.Size * 0.5f)
                    continue;

                float fade = 1f - i / (float)Projectile.oldPos.Length;
                DrawSegment(pixel, previous, current, AzureThunderColors.Yellow * fade, 3.2f * fade * sizeMult);
                DrawSegment(pixel, previous, current, Color.White * 0.55f * fade, 1.35f * fade);
                previous = current;
            }

            return false;
        }

        private static void DrawSegment(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), width),
                SpriteEffects.None);
        }
    }
}
