using CalamityMod.Buffs.DamageOverTime;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.B_Flamethrower
{
    public class YC_FlameLance : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorFog";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private ref float ScaleFactor => ref Projectile.ai[0];
        private ref float Seed => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 58;
            Projectile.height = 58;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 38;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 3;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (ScaleFactor <= 0f)
                ScaleFactor = 1f;

            Projectile.scale = ScaleFactor;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void AI()
        {
            Timer++;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            NPC target = FindSubtleTarget(forward);
            if (target != null)
            {
                Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(forward);
                Projectile.velocity = Vector2.Lerp(forward, desired, 0.018f).SafeNormalize(forward) * Projectile.velocity.Length();
            }

            float turbulence = (float)System.Math.Sin((Timer + Seed) * 0.19f) * 0.011f + (float)System.Math.Sin((Timer * 0.73f + Seed) * 0.043f) * 0.008f;
            Projectile.velocity = Projectile.velocity.RotatedBy(turbulence + Main.rand.NextFloat(-0.0045f, 0.0045f));
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= MathHelper.Lerp(1.006f, 0.992f, Utils.GetLerpValue(6f, 34f, Timer, true));
            float bloomIn = Utils.GetLerpValue(0f, 16f, Timer, true);
            float bloomOut = Utils.GetLerpValue(0f, 12f, Projectile.timeLeft, true);
            Projectile.scale = ScaleFactor * MathHelper.Lerp(0.42f, 1.72f, bloomIn) * bloomOut;
            Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Timer, true) * bloomOut;

            Color flameColor = GetFlameColor(0f);
            Lighting.AddLight(Projectile.Center, flameColor.ToVector3() * (0.55f * Projectile.scale * Projectile.Opacity));

            if (Main.dedServ)
                return;

            EmitFlameParticles(forward, flameColor);
        }

        private void EmitFlameParticles(Vector2 forward, Color flameColor)
        {
            int smokeCount = Main.rand.NextBool(3) ? 2 : 1;
            for (int i = 0; i < smokeCount; i++)
            {
                Vector2 center = Projectile.Center - forward * Main.rand.NextFloat(2f, 20f) + Main.rand.NextVector2Circular(14f, 14f) * Projectile.scale;
                Vector2 drift = Projectile.velocity * Main.rand.NextFloat(0.04f, 0.18f) + Main.rand.NextVector2Circular(1.25f, 1.25f);
                float rotationSpeed = MathHelper.ToRadians(Main.rand.NextFloat(-5f, 5f));
                Color smokeColor = Color.Lerp(flameColor, new Color(64, 52, 48), Main.rand.NextFloat(0.06f, 0.22f));

                GeneralParticleHandler.SpawnParticle(
                    new HeavySmokeParticle(
                        center,
                        drift,
                        smokeColor,
                        Main.rand.Next(13, 23),
                        Projectile.scale * Main.rand.NextFloat(0.46f, 1.05f),
                        Main.rand.NextFloat(0.58f, 0.84f),
                        rotationSpeed,
                        glowing: true,
                        Main.rand.NextFloat(0.003f, 0.012f),
                        required: true));
            }

            int emberCount = Main.rand.Next(2, 5);
            for (int i = 0; i < emberCount; i++)
            {
                int dustType = Main.rand.NextBool(7) ? DustID.Smoke : Main.rand.NextBool(4) ? DustID.SolarFlare : Main.rand.NextBool(2) ? DustID.GoldFlame : DustID.Torch;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 18f) * Projectile.scale,
                    dustType,
                    forward.RotatedByRandom(0.8f) * Main.rand.NextFloat(0.35f, 3.6f) + Main.rand.NextVector2Circular(0.9f, 0.9f),
                    0,
                    Color.Lerp(flameColor, Color.White, Main.rand.NextFloat(0.1f, 0.36f)),
                    Main.rand.NextFloat(0.62f, 1.42f) * Projectile.scale);
                dust.noGravity = true;
                dust.alpha = dustType == DustID.Smoke ? 90 : 25;
                dust.fadeIn = Main.rand.NextFloat(0.1f, 0.34f);
            }
        }

        private Color GetFlameColor(float offset)
        {
            float wave = 0.5f + 0.5f * (float)System.Math.Sin((Timer + Seed * 0.13f + offset) * 0.075f);
            Color auric = Color.Lerp(new Color(255, 82, 34), new Color(255, 218, 92), 0.32f + 0.24f * wave);
            Color spectral = Color.Lerp(new Color(105, 210, 255), new Color(180, 112, 255), wave);
            return Color.Lerp(auric, spectral, 0.22f + 0.12f * (float)System.Math.Sin((Timer + Seed) * 0.037f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 120);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - forward * 20f;
            Vector2 end = Projectile.Center + forward * 62f;
            bool lineHit = Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 30f * Projectile.scale, ref collisionPoint);
            return lineHit || CalamityUtils.CircularHitboxCollision(Projectile.Center, Projectile.width * Projectile.scale * 0.48f, targetHitbox);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D fog = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = Projectile.Center - Main.screenPosition;
            float opacity = Projectile.Opacity;
            Color primary = GetFlameColor(0f);
            Color secondary = GetFlameColor(23f);
            float rollA = Projectile.rotation + Seed * 0.01f + Timer * 0.018f;
            float rollB = -Projectile.rotation + Seed * 0.017f - Timer * 0.014f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            Main.EntitySpriteDraw(
                fog,
                center - forward * 6f,
                null,
                primary * (0.24f * opacity),
                rollA,
                fog.Size() * 0.5f,
                Projectile.scale * 0.72f,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                fog,
                center + forward * 8f,
                null,
                secondary * (0.17f * opacity),
                rollB,
                fog.Size() * 0.5f,
                Projectile.scale * 0.54f,
                SpriteEffects.FlipHorizontally,
                0);

            Main.EntitySpriteDraw(glow, center + forward * 22f, null, Color.Lerp(primary, Color.White, 0.35f) * (0.34f * opacity), Projectile.rotation, glow.Size() * 0.5f, 0.11f * Projectile.scale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(glow, center + forward * 42f, null, secondary * (0.22f * opacity), Projectile.rotation, glow.Size() * 0.5f, 0.08f * Projectile.scale, SpriteEffects.None, 0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private NPC FindSubtleTarget(Vector2 forward)
        {
            float maxDistanceSquared = 620f * 620f;
            NPC nearest = null;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toNpc = npc.Center - Projectile.Center;
                float distanceSquared = toNpc.LengthSquared();
                if (distanceSquared > maxDistanceSquared)
                    continue;

                float angle = System.Math.Abs(MathHelper.WrapAngle(forward.ToRotation() - toNpc.ToRotation()));
                if (angle > MathHelper.ToRadians(28f))
                    continue;

                maxDistanceSquared = distanceSquared;
                nearest = npc;
            }

            return nearest;
        }
    }
}
