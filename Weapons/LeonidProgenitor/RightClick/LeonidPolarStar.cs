using System;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    // Stealth-right-click payload: a deliberately straight, non-homing star shot.
    public class LeonidPolarStar : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/PolarStar";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        private ref float Time => ref Projectile.localAI[0];
        private ref float FadeTimer => ref Projectile.localAI[1];
        private bool DamageDisabled => Projectile.ai[1] == 1f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 180;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Time++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (!DamageDisabled && Time <= 32f)
                Projectile.velocity = Projectile.velocity.RotatedBy(MathF.Sin(Time * 0.28f) * 0.0028f);

            Color starColor = GetStarColor();
            Lighting.AddLight(Projectile.Center, starColor.ToVector3() * 0.7f);

            if (DamageDisabled)
            {
                FadeTimer++;
                Projectile.velocity *= 0.95f;
                Projectile.scale = MathHelper.Lerp(Projectile.scale, 0.45f, 0.07f);
                Projectile.alpha = Math.Min(255, Projectile.alpha + 6);
                if (Main.rand.NextBool(2))
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, Main.rand.NextVector2Circular(1.8f, 1.8f),
                        false, 16, Main.rand.NextFloat(0.45f, 0.75f), starColor));
                if (FadeTimer >= 42f)
                    Projectile.Kill();
                return;
            }

            if (Main.rand.NextBool(2))
                LeonidStarlight.Shed(Projectile.Center, Projectile.velocity, starColor, LeonidStarlightShape.Mote,
                    0.58f, hoverTime: 16, lifetime: 80);
            if (Time % 4f == 0f)
                GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center - Projectile.velocity * 0.35f,
                    -Projectile.velocity * 0.035f, false, 18, 0.58f, starColor));
            if (Time % 9f == 0f)
                GeneralParticleHandler.SpawnParticle(new BloomParticle(Projectile.Center, Vector2.Zero,
                    Color.Lerp(starColor, LeonidVisualUtils.MoonWhite, 0.45f), 0.12f, 0.2f, 2, false));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.ai[1] = 1f;
            Projectile.friendly = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Math.Min(Projectile.timeLeft, 48);
            Projectile.netUpdate = true;
        }

        public override void OnKill(int timeLeft)
        {
            Color color = GetStarColor();
            LeonidPolarStarBurst.Spawn(Projectile.Center, Projectile.oldVelocity, color, 0.72f);
            LeonidStarlight.Burst(Projectile.Center, 2, color, LeonidStarlightShape.Mote,
                speed: 3.5f, scale: 0.55f, hoverTime: 16, lifetime: 84);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = GetStarColor();

            LeonidVisualUtils.BeginAdditiveSpriteBatch();
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 oldPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, oldPosition, null, color * (completion * 0.34f), Projectile.rotation, origin,
                    Projectile.scale * (0.65f + completion * 0.35f), SpriteEffects.None, 0f);
            }
            LeonidVisualUtils.DrawBloom(Projectile.Center, color * 0.46f, 0.1f);
            LeonidVisualUtils.DrawCelestialHead(Projectile.Center, color, 0.72f, 0.82f, Projectile.rotation);
            LeonidStarlight.DrawFlare(Projectile.Center, LeonidVisualUtils.MoonWhite, 0.62f, 0.095f, -Projectile.rotation * 0.45f);
            LeonidStarlight.DrawSunburst(Projectile.Center, color, 0.22f, 0.026f, Main.GlobalTimeWrappedHourly * 1.1f);
            LeonidVisualUtils.BeginAlphaBlendSpriteBatch();
            Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
            return false;
        }

        public override bool? CanDamage() => DamageDisabled ? false : null;

        private Color GetStarColor()
        {
            float phase = 0.5f + 0.5f * MathF.Sin(Main.GlobalTimeWrappedHourly * 4.2f + Projectile.whoAmI * 0.7f);
            return Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.StarGold, phase * 0.7f);
        }
    }
}
