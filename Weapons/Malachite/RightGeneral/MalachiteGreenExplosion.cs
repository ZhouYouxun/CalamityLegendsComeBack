using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite
{
    public class MalachiteGreenExplosion : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private bool IsFinaleExplosion => Projectile.ai[0] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 18;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool? CanDamage()
        {
            if (IsFinaleExplosion)
                return Projectile.localAI[0] % 6f <= 1f;

            return Projectile.localAI[0] <= 3f;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] == 1f)
            {
                Projectile.Resize(IsFinaleExplosion ? 236 : 132, IsFinaleExplosion ? 236 : 132);
                Projectile.timeLeft = IsFinaleExplosion ? 54 : 18;
            }

            if (IsFinaleExplosion && Projectile.localAI[0] % 6f == 1f)
                Projectile.Damage();
            else if (!IsFinaleExplosion && Projectile.localAI[0] == 2f)
                Projectile.Damage();

            SpawnDust();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 8 * 60);
        }

        private void SpawnDust()
        {
            int dustCount = IsFinaleExplosion ? 5 : 4;
            float radius = IsFinaleExplosion ? 92f : 48f;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, IsFinaleExplosion ? 4.8f : 3.7f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(radius, radius),
                    DustID.Terra,
                    velocity,
                    80,
                    IsFinaleExplosion ? new Color(90, 255, 100) : new Color(185, 255, 100),
                    Main.rand.NextFloat(0.68f, IsFinaleExplosion ? 1.18f : 1.05f));
                dust.noGravity = true;
            }

            if (!IsFinaleExplosion || Projectile.localAI[0] % 5f != 1f)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 radial = Main.rand.NextVector2Unit();
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + radial * Main.rand.NextFloat(18f, 96f),
                    DustID.Terra,
                    radial.RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloatDirection()) * Main.rand.NextFloat(1.4f, 3.8f),
                    60,
                    Main.rand.NextBool() ? new Color(170, 255, 110) : new Color(45, 255, 135),
                    Main.rand.NextFloat(0.76f, 1.2f));
                spark.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float completion = Projectile.localAI[0] / (IsFinaleExplosion ? 54f : 18f);
            float pulse = MathF.Sin(MathHelper.Clamp(completion, 0f, 1f) * MathHelper.Pi);
            float baseScale = IsFinaleExplosion ? 3.1f : 1.75f;
            Color color = IsFinaleExplosion ? new Color(70, 255, 95, 0) : new Color(190, 255, 95, 0);

            if (IsFinaleExplosion)
                DrawFinaleBlackHole(drawPosition, pulse, completion);

            for (int i = 0; i < 6; i++)
            {
                float rotation = MathHelper.TwoPi * i / 6f + Projectile.localAI[0] * 0.05f;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    color * (0.16f + pulse * 0.25f),
                    rotation,
                    origin,
                    baseScale * (0.52f + pulse * 0.38f),
                    SpriteEffects.None);
            }

            return false;
        }

        private void DrawFinaleBlackHole(Vector2 drawPosition, float pulse, float completion)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            float fade = Utils.GetLerpValue(1f, 0.72f, completion, true);

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                new Color(30, 255, 115, 0) * (0.22f * fade),
                0f,
                bloomOrigin,
                1.55f + pulse * 0.72f,
                SpriteEffects.None);

            for (int i = 0; i < 2; i++)
            {
                float ringRotation = Main.GlobalTimeWrappedHourly * (1.8f + i * 0.55f) + Projectile.identity * 0.17f;
                float ringScale = 0.86f + i * 0.54f + pulse * 0.24f;

                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition,
                    null,
                    new Color(95, 255, 120, 0) * (0.11f * fade),
                    ringRotation,
                    bloomOrigin,
                    new Vector2(ringScale * 1.5f, ringScale * 0.34f),
                    SpriteEffects.None);
            }
        }


    }
}
