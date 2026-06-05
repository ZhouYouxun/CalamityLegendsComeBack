using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClickTurret
{
    internal sealed class MilitaryTurretSelfDestruct : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Color ExplosionColor =>
            MilitaryTurretUtility.GetStats((MilitaryTurretKind)Utils.Clamp((int)Projectile.ai[0], 0, 6)).ThemeColor;

        public override void SetDefaults()
        {
            Projectile.width = 180;
            Projectile.height = 180;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 8;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.9f, Pitch = -0.18f }, Projectile.Center);

            if (Main.dedServ)
                return;

            for (int i = 0; i < 48; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(28f, 28f),
                    Main.rand.NextBool(3) ? DustID.Electric : DustID.Torch);

                dust.velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 13f);
                dust.color = Color.Lerp(ExplosionColor, Color.White, Main.rand.NextFloat(0.2f, 0.8f));
                dust.noGravity = true;
                dust.scale = Main.rand.NextFloat(1f, 2f);
            }
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 6 ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float completion = 1f - Projectile.timeLeft / 8f;
            float opacity = 1f - completion;
            Color color = ExplosionColor with { A = 0 };

            Main.EntitySpriteDraw(
                bloom,
                drawPosition,
                null,
                color * opacity * 0.8f,
                0f,
                bloom.Size() * 0.5f,
                MathHelper.Lerp(0.35f, 1.35f, completion),
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                ring,
                drawPosition,
                null,
                Color.White with { A = 0 } * opacity,
                0f,
                ring.Size() * 0.5f,
                MathHelper.Lerp(0.18f, 0.72f, completion),
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
