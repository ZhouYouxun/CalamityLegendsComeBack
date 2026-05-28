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
                Projectile.localAI[0] = 1f;
            }

            Projectile.velocity *= 0.95f;
            Projectile.velocity.Y += 0.04f;
            Projectile.rotation += Projectile.velocity.X * 0.03f + 0.04f;
            Projectile.alpha = (int)MathHelper.Lerp(0f, 210f, Utils.GetLerpValue(24f, 0f, Projectile.timeLeft, true));

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new SquareAshParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Projectile.velocity * -0.2f + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    Main.rand.Next(22, 36),
                    Main.rand.NextFloat(0.42f, 0.9f),
                    Color.Lerp(Color.DarkGray, new Color(255, 96, 36), 0.25f)));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.Lerp(Color.DarkGray, Color.OrangeRed, 0.28f) * Projectile.Opacity,
                Projectile.rotation,
                frame.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None);

            return false;
        }
    }
}
