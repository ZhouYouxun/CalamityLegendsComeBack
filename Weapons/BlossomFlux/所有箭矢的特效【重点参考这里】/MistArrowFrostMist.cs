using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Ranged
{
    public class MistArrowFrostMist : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Ranged";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 60;
            Projectile.alpha = 96;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.coldDamage = true;
        }

        public override void AI()
        {
            if (Projectile.timeLeft < 20)
            {
                Projectile.alpha += 8;
                if (Projectile.alpha >= 255)
                {
                    Projectile.alpha = 255;
                    Projectile.Kill();
                }
            }

            Projectile.velocity *= 0.975f;

            for (int i = 0; i < 2; i++)
            {
                Vector2 dustPosition = new Vector2(Projectile.position.X + Main.rand.NextFloat(0f, Projectile.width), Projectile.position.Y + Main.rand.NextFloat(0f, Projectile.height));
                Dust frostDust = Dust.NewDustPerfect(dustPosition, DustID.Cloud, Vector2.Zero, 0, default, 0.45f);
                frostDust.noGravity = true;
            }
            Lighting.AddLight(Projectile.Center, 0.4f, 0.4f, 0.4f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            switch (Projectile.ai[0])
            {
                case 0:
                    break;
                case 1:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/MistArrowFrostMist2").Value;
                    break;
                case 2:
                    texture = ModContent.Request<Texture2D>("CalamityMod/Projectiles/Ranged/MistArrowFrostMist3").Value;
                    break;
                default:
                    break;
            }
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Projectile.GetAlpha(lightColor), Projectile.rotation, texture.Size() / 2, 1f, SpriteEffects.None);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.Frostburn2, 45);
    }
}
