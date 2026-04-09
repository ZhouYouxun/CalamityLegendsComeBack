using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight
{
    public class YC_Right_HeavyBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YCRight/YC_Right_HeavyBolt";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 90;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(255, 138, 96).ToVector3() * 0.4f);

            if (Main.dedServ || !Main.rand.NextBool(3))
                return;

            YC_RightHelper.EmitRightDust(
                Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 6f,
                -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                Color.Lerp(new Color(255, 150, 90), new Color(255, 230, 180), Main.rand.NextFloat()),
                Main.rand.NextFloat(0.8f, 1.1f),
                DustID.Torch);
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.Resize(52, 52);
                Projectile.penetrate = -1;
                Projectile.maxPenetrate = -1;
                Projectile.usesLocalNPCImmunity = true;
                Projectile.localNPCHitCooldown = -1;
                Projectile.Damage();
            }

            Terraria.Audio.SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.24f, Pitch = 0.12f }, Projectile.Center);
            for (int i = 0; i < 12; i++)
            {
                Vector2 direction = Main.rand.NextVector2CircularEdge(1f, 1f);
                YC_RightHelper.EmitRightDust(Projectile.Center, direction * Main.rand.NextFloat(2f, 4.6f), new Color(255, 150, 90), Main.rand.NextFloat(0.85f, 1.2f), DustID.Torch);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    new Color(255, 160, 95, 0) * (0.12f + completion * 0.2f),
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (0.9f + completion * 0.15f),
                    SpriteEffects.None,
                    0);
            }

            return true;
        }
    }
}
