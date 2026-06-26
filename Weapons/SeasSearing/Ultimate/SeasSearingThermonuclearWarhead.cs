using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingThermonuclearWarhead : ModProjectile, ILocalizedModType
    {
        private bool detonated;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_134";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type]     = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width      = 34;
            Projectile.height     = 34;
            Projectile.penetrate  = -1;
            Projectile.timeLeft   = 240;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.rotation   = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y = System.Math.Min(Projectile.velocity.Y + 0.18f, 34f);
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.WarningOrange.ToVector3() * 0.48f);

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(12f, 34f) + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool() ? DustID.Torch : DustID.GemEmerald,
                    -direction * Main.rand.NextFloat(1.2f, 4.6f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    100,
                    Main.rand.NextBool() ? SeasSearingPalette.WarningOrange : SeasSearingPalette.RadioactiveCyan,
                    Main.rand.NextFloat(0.7f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            TriggerDetonation();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!detonated) TriggerDetonation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom   = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2   origin  = texture.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float   completion   = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color   color        = Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.RadioactiveCyan, completion * 0.55f) * (completion * 0.5f);
                color.A = 0;
                Main.EntitySpriteDraw(bloom, drawPosition, null, color, Projectile.rotation, bloom.Size() * 0.5f, 0.18f + completion * 0.16f, SpriteEffects.None, 0);
            }

            Color armorColor = Color.Lerp(new Color(18, 26, 42), SeasSearingPalette.DeepBlue, 0.35f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, armorColor, Projectile.rotation, origin, Projectile.scale * 1.45f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom,   Projectile.Center - Main.screenPosition, null, (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * 0.45f, Projectile.rotation, bloom.Size() * 0.5f, 0.2f, SpriteEffects.None, 0);
            return false;
        }

        private void TriggerDetonation()
        {
            if (detonated) return;
            detonated = true;
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingNuclearDetonation>(),
                    Projectile.damage, Projectile.knockBack, Projectile.owner);
            }
            Projectile.Kill();
        }
    }
}
