using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class YC_WarshipPulse : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float PulseScaleFactor => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 24;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (PulseScaleFactor <= 0f)
                PulseScaleFactor = 1f;

            Projectile.scale = 0.7f + PulseScaleFactor * 0.16f;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 0.95f;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.18f, 0.12f);

            Lighting.AddLight(Projectile.Center, new Vector3(0.34f, 0.62f, 0.88f) * 0.55f * Projectile.scale);

            if (Main.dedServ || !Main.rand.NextBool(3))
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Vector2 spawnPosition = Projectile.Center + side * Main.rand.NextFloat(-8f, 8f);
            Dust dust = Dust.NewDustPerfect(
                spawnPosition,
                DustID.RainbowTorch,
                Projectile.velocity * 0.08f + side * Main.rand.NextFloat(-0.65f, 0.65f),
                0,
                Color.Lerp(new Color(120, 220, 255), Color.White, Main.rand.NextFloat(0.15f, 0.45f)),
                Main.rand.NextFloat(0.8f, 1.15f) * PulseScaleFactor);
            dust.noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.12f, Pitch = 0.32f }, Projectile.Center);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.4f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.RainbowTorch,
                    velocity,
                    0,
                    Color.Lerp(new Color(130, 235, 255), Color.White, Main.rand.NextFloat(0.2f, 0.55f)),
                    Main.rand.NextFloat(0.8f, 1.2f) * PulseScaleFactor);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D trail = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 velocityDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float opacity = Utils.GetLerpValue(0f, 5f, Projectile.timeLeft, true) * Utils.GetLerpValue(24f, 12f, Projectile.timeLeft, true);
            float length = 28f * Projectile.scale;

            Main.EntitySpriteDraw(
                trail,
                drawPosition - velocityDirection * length * 0.35f,
                null,
                new Color(110, 225, 255, 0) * (0.5f * opacity),
                velocityDirection.ToRotation() + MathHelper.PiOver2,
                trail.Size() * 0.5f,
                new Vector2(0.26f * Projectile.scale, length / trail.Height),
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                glow,
                drawPosition,
                null,
                new Color(185, 245, 255, 0) * (0.82f * opacity),
                Projectile.rotation,
                glow.Size() * 0.5f,
                Projectile.scale * 0.18f,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                glow,
                drawPosition,
                null,
                (Color.White with { A = 0 }) * (0.5f * opacity),
                Projectile.rotation,
                glow.Size() * 0.5f,
                Projectile.scale * 0.1f,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
