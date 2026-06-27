using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.BStage1
{
    public class VesuviusVolcanicAsh : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Magic/RancorSmallCinder";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 3;
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 54;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.frame = Main.rand.Next(Main.projFrames[Type]);
                Projectile.rotation = Main.rand.NextFloat(MathHelper.TwoPi);
                Projectile.scale = Main.rand.NextFloat(0.85f, 1.35f);
                Projectile.localAI[1] = Main.rand.Next(5, 16); // initial turbulence timer
                Projectile.localAI[0] = 1f;
            }

            // Irregular ash movement — hot updrafts and cross-winds create non-linear paths
            if (--Projectile.localAI[1] <= 0f)
            {
                Projectile.localAI[1] = Main.rand.Next(8, 22);
                Projectile.velocity += new Vector2(Main.rand.NextFloat(-1.6f, 1.6f), Main.rand.NextFloat(-0.55f, 0.18f));
            }

            Projectile.velocity *= Main.rand.NextFloat(0.91f, 0.97f); // variable drag
            Projectile.velocity.Y += Main.rand.NextFloat(0.01f, 0.07f); // variable gravity
            Projectile.rotation += Projectile.velocity.X * 0.03f + Main.rand.NextFloat(-0.04f, 0.08f);
            Projectile.alpha = (int)MathHelper.Lerp(0f, 210f, Utils.GetLerpValue(24f, 0f, Projectile.timeLeft, true));
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.075f, 0.035f) * Projectile.Opacity * VesuviusProjectileVisuals.VisualIntensity);

            VesuviusProjectileVisuals.SpawnVolcanicAshCloud(Projectile, 0.9f);
            SpawnMagicAshParticles();
        }

        private void SpawnMagicAshParticles()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 backward = -forward;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    backward * Main.rand.NextFloat(0.4f, 1.8f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.18f, 0.42f) * Projectile.scale,
                    Main.rand.NextBool(4) ? VesuviusProjectileVisuals.HotWhite : Color.Lerp(VesuviusProjectileVisuals.LavaOrange, VesuviusProjectileVisuals.LavaGold, Main.rand.NextFloat()),
                    true,
                    false,
                    true));
            }

            if (Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new SmallSmokeParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 16f) + Main.rand.NextVector2Circular(8f, 8f),
                    backward * Main.rand.NextFloat(0.2f, 1.2f),
                    Color.Lerp(VesuviusProjectileVisuals.ScoriaSmoke, VesuviusProjectileVisuals.LavaOrange, 0.18f),
                    Color.Black,
                    Main.rand.NextFloat(0.42f, 0.78f) * Projectile.scale,
                    0.45f,
                    Main.rand.NextFloat(-0.05f, 0.05f)));
            }

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.Torch,
                    backward.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.5f, 2.8f),
                    100,
                    Color.Lerp(VesuviusProjectileVisuals.AshGray, VesuviusProjectileVisuals.LavaGold, 0.22f),
                    Main.rand.NextFloat(0.55f, 1.05f));
                dust.noGravity = Main.rand.NextBool(3);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override void OnKill(int timeLeft)
        {
            VesuviusProjectileVisuals.SpawnAshBurst(Projectile.Center, Projectile.scale);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D fire = ModContent.Request<Texture2D>("CalamityMod/Projectiles/FireProj").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            float fade = Projectile.Opacity;

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                (Color.Lerp(VesuviusProjectileVisuals.LavaOrange, VesuviusProjectileVisuals.LavaGold, 0.35f) with { A = 0 }) * 0.22f * fade,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.42f * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float trailFade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    texture,
                    oldCenter - Main.screenPosition,
                    frame,
                    Color.Lerp(Color.DarkGray, VesuviusProjectileVisuals.LavaOrange, 0.18f) * fade * 0.42f * trailFade * VesuviusProjectileVisuals.VisualIntensity,
                    Projectile.rotation,
                    frame.Size() * 0.5f,
                    Projectile.scale * (0.85f + trailFade * 0.2f),
                    SpriteEffects.None);
            }

            // FireProj has a black background designed for additive blending — use A=0 trick
            Color fireC = Color.Lerp(VesuviusProjectileVisuals.LavaOrange, Color.White, 0.22f) * (0.34f * fade * VesuviusProjectileVisuals.VisualIntensity);
            Main.EntitySpriteDraw(
                fire,
                Projectile.Center - Main.screenPosition,
                null,
                new Color(fireC.R, fireC.G, fireC.B, 0),
                -Projectile.rotation * 0.6f,
                fire.Size() * 0.5f,
                Projectile.scale * 0.52f * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.Lerp(lightColor, VesuviusProjectileVisuals.LavaOrange, 0.55f) * fade,
                Projectile.rotation,
                frame.Size() * 0.5f,
                Projectile.scale * 1.08f,
                SpriteEffects.None);

            return false;
        }
    }
}
