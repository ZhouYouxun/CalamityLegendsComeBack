using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite.RightGeneral
{
    public sealed class MalachiteRightNovaShard : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/NovaChargedShot";

        public override string LocalizationCategory => "Projectiles.Malachite";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 75;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 0.985f;
            Lighting.AddLight(Projectile.Center, 0.1f, 0.45f, 0.16f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 8f,
                    DustID.Terra,
                    -Projectile.velocity * 0.06f + Main.rand.NextVector2Circular(0.6f, 0.6f),
                    90,
                    new Color(120, 255, 145),
                    Main.rand.NextFloat(0.65f, 1.05f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Poisoned, 5 * 60);

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Color trail = new Color(70, 255, 130, 0);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float fade = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, null, trail * fade * 0.42f, Projectile.oldRot[i], origin, Projectile.scale * MathHelper.Lerp(0.45f, 0.95f, fade), SpriteEffects.None);
                Main.EntitySpriteDraw(bloom, drawPosition, null, trail * fade * 0.18f, 0f, bloom.Size() * 0.5f, Projectile.scale * 0.38f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(trail, Color.White, 0.24f) * Projectile.Opacity, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
