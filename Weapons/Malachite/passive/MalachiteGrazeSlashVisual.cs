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

namespace CalamityLegendsComeBack.Weapons.Malachite.passive
{
    public sealed class MalachiteGrazeSlashVisual : ModProjectile, ILocalizedModType
    {
        public override string Texture => "Terraria/Images/Extra_98";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 14;
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.ai[0] + Timer * 0.45f;
            Projectile.Opacity = Utils.GetLerpValue(14f, 3f, Timer, true);
            Projectile.scale = 0.3f + Utils.GetLerpValue(0f, 8f, Timer, true) * 0.08f;
            Lighting.AddLight(Projectile.Center, 0.06f, 0.26f, 0.09f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = new Color(100, 255, 145, 0) * Projectile.Opacity;

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                color,
                Projectile.rotation,
                origin,
                new Vector2(1.6f, 0.26f) * Projectile.scale,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.White * 0.34f * Projectile.Opacity,
                Projectile.rotation + MathHelper.PiOver2,
                origin,
                new Vector2(0.72f, 0.1f) * Projectile.scale,
                SpriteEffects.None);

            return false;
        }
    }
}
