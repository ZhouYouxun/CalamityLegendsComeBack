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
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 0;
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
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.075f, 0.035f) * Projectile.Opacity * VesuviusProjectileVisuals.VisualIntensity);

            VesuviusProjectileVisuals.SpawnVolcanicAshCloud(Projectile, 0.9f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 120);
        }

        public override void OnKill(int timeLeft)
        {
            VesuviusProjectileVisuals.SpawnAshBurst(Projectile.Center, Projectile.scale);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            float fade = Projectile.Opacity;

            Main.EntitySpriteDraw(
                bloom,
                Projectile.Center - Main.screenPosition,
                null,
                Color.Lerp(Color.Black, VesuviusProjectileVisuals.AshGray, 0.55f) * 0.28f * fade * VesuviusProjectileVisuals.VisualIntensity,
                Projectile.rotation,
                bloom.Size() * 0.5f,
                Projectile.scale * 0.38f * VesuviusProjectileVisuals.VisualScale,
                SpriteEffects.None);

            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float trailFade = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Main.EntitySpriteDraw(
                    texture,
                    oldCenter - Main.screenPosition,
                    frame,
                    Color.Lerp(Color.DarkGray, VesuviusProjectileVisuals.LavaOrange, 0.18f) * fade * 0.42f * trailFade * VesuviusProjectileVisuals.VisualIntensity,
                    Projectile.rotation,
                    frame.Size() * 0.5f,
                    Projectile.scale * (0.85f + trailFade * 0.2f),
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                texture,
                Projectile.Center - Main.screenPosition,
                frame,
                Color.Lerp(Color.DimGray, VesuviusProjectileVisuals.LavaOrange, 0.2f) * fade,
                Projectile.rotation,
                frame.Size() * 0.5f,
                Projectile.scale * 1.08f,
                SpriteEffects.None);

            return false;
        }
    }
}
