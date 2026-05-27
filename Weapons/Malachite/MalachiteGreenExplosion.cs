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
            int dustCount = IsFinaleExplosion ? 9 : 5;
            float radius = IsFinaleExplosion ? 108f : 54f;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.6f, IsFinaleExplosion ? 7.5f : 4.6f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(radius, radius),
                    DustID.Terra,
                    velocity,
                    80,
                    IsFinaleExplosion ? new Color(90, 255, 100) : new Color(185, 255, 100),
                    Main.rand.NextFloat(0.8f, IsFinaleExplosion ? 1.75f : 1.2f));
                dust.noGravity = true;
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

            for (int i = 0; i < 6; i++)
            {
                float rotation = MathHelper.TwoPi * i / 6f + Projectile.localAI[0] * 0.05f;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    color * (0.28f + pulse * 0.45f),
                    rotation,
                    origin,
                    baseScale * (0.65f + pulse * 0.55f),
                    SpriteEffects.None);
            }

            return false;
        }
    }
}
