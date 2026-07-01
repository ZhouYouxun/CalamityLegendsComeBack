using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.B_HardMode
{
    public class DERule_Helstorm : DEBulletRule
    {
        private static readonly Color Ember = new(255, 86, 32);

        public override int GunItemType =>
            ModContent.ItemType<CalamityMod.Items.Weapons.Ranged.Helstorm>();

        public override int Penetrate => -1;
        public override int ExtraUpdates => 0;
        public override float SpeedMultiplier => 0.42f;
        public override float DamageMultiplier => 0.42f;

        public override void SetDefaults(Projectile projectile)
        {
            projectile.width = 54;
            projectile.height = 54;
            projectile.timeLeft = 72;
            projectile.tileCollide = false;
            projectile.usesLocalNPCImmunity = true;
            projectile.localNPCHitCooldown = 8;
            projectile.light = 0.85f;
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.localAI[0]++;
            projectile.rotation += 0.52f * (projectile.velocity.X >= 0f ? 1f : -1f);
            projectile.velocity *= 0.992f;
            DEBulletUtils.SimpleHoming(projectile, 420f, 0.035f, MathHelper.Clamp(projectile.velocity.Length() + 0.05f, 7f, 13f));

            for (int i = 0; i < 3; i++)
            {
                Vector2 offset = (projectile.rotation + i * MathHelper.TwoPi / 3f).ToRotationVector2() * 24f;
                Dust dust = Dust.NewDustPerfect(projectile.Center + offset, DustID.Torch, offset.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * 2.2f, 100, Ember, 1f);
                dust.noGravity = true;
            }

            Lighting.AddLight(projectile.Center, Ember.ToVector3() * 0.65f);
        }

        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 360);
        }

        public override bool PreDraw(Projectile projectile, Player owner, ref Color lightColor)
        {
            Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[ProjectileID.SolarWhipSwordExplosion].Value;
            Rectangle frame = texture.Frame();
            Vector2 origin = frame.Size() * 0.5f;
            Color color = Ember * 0.72f;

            for (int i = 0; i < 4; i++)
            {
                float rotation = projectile.rotation + MathHelper.PiOver2 * i;
                Main.EntitySpriteDraw(texture, projectile.Center - Main.screenPosition, frame, color, rotation, origin, 0.42f, SpriteEffects.None, 0);
            }

            return false;
        }

        public override string TooltipEffectEN => "Launches a spinning circular hell saw that grinds through enemies";
        public override string TooltipEffectZH => "发射圆形地狱弹幕锯，持续切割敌人";
    }
}
