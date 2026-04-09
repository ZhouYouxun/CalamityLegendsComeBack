using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // 散播箭的毒气子弹幕，主要承担滞留区域和持续伤害。
    internal class BFArrow_EPlagueGas : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Variant => ref Projectile.ai[0];
        private ref float MaxVisualScale => ref Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 54;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 70;
            Projectile.alpha = 255;
            Projectile.usesIDStaticNPCImmunity = true;
            Projectile.idStaticNPCHitCooldown = 12;
        }

        public override bool? CanDamage() => Projectile.localAI[0] > 4f && Projectile.timeLeft > 8 ? null : false;

        public override void AI()
        {
            Projectile.localAI[0]++;

            if (Projectile.localAI[0] == 1f)
                Projectile.scale = 0.35f;

            Projectile.rotation += 0.01f * Projectile.direction;
            Projectile.velocity *= 0.967f;
            Projectile.velocity.Y -= 0.015f;

            float targetScale = MaxVisualScale <= 0f ? 1.15f : MaxVisualScale;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, targetScale, 0.06f);
            Projectile.Opacity = Utils.GetLerpValue(0f, 8f, Projectile.localAI[0], true) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);

            Lighting.AddLight(Projectile.Center, new Color(130, 205, 80).ToVector3() * 0.35f * Projectile.Opacity);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    100,
                    new Color(172, 228, 92),
                    Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int textureType = (int)Variant switch
            {
                0 => ProjectileID.ToxicCloud,
                1 => ProjectileID.ToxicCloud2,
                _ => ProjectileID.ToxicCloud3
            };

            Texture2D texture = TextureAssets.Projectile[textureType].Value;
            Color drawColor = new Color(178, 255, 132, 0) * Projectile.Opacity;

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                null,
                drawColor,
                Projectile.rotation,
                texture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
