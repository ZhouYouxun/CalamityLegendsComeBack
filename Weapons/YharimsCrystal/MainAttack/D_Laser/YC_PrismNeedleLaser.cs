using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.D_Laser
{
    public class YC_PrismNeedleLaser : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.YharimsCrystal";

        private ref float HueSeed => ref Projectile.ai[0];
        private ref float ScaleSeed => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 6000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 4;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.extraUpdates = 65;
            Projectile.timeLeft = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 4;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (ScaleSeed <= 0f)
                ScaleSeed = 1f;

            Projectile.scale = ScaleSeed;
            Projectile.rotation = Projectile.velocity.ToRotation();
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Color color = GetColor();
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.18f);

            if (Main.dedServ || (int)Timer % 42 != 0)
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center,
                DustID.GoldFlame,
                -Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.2f, 0.55f),
                0,
                Color.Lerp(color, Color.White, 0.25f),
                Main.rand.NextFloat(0.55f, 0.9f) * Projectile.scale);
            dust.noGravity = true;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center - direction * 42f,
                Projectile.Center + direction * 18f,
                7f * Projectile.scale,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Dragonfire>(), 120);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Color color = GetColor() with { A = 0 };
            float opacity = Utils.GetLerpValue(0f, 3f, Timer / (Projectile.extraUpdates + 1f), true) *
                Utils.GetLerpValue(0f, 3f, Projectile.timeLeft, true);

            Vector2 start = Projectile.Center - direction * 54f - Main.screenPosition;
            Vector2 end = Projectile.Center + direction * 18f - Main.screenPosition;
            DrawLine(line, start, end, color * (0.68f * opacity), 0.14f * Projectile.scale);

            for (int i = 0; i < Projectile.oldPos.Length - 1; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i + 1] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 trailStart = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Vector2 trailEnd = Projectile.oldPos[i + 1] + Projectile.Size * 0.5f - Main.screenPosition;
                DrawLine(line, trailStart, trailEnd, color * (0.18f * completion * opacity), 0.09f * Projectile.scale);
            }

            Main.EntitySpriteDraw(
                glow,
                Projectile.Center - Main.screenPosition,
                null,
                color * (0.38f * opacity),
                Projectile.rotation,
                glow.Size() * 0.5f,
                0.055f * Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        private Color GetColor()
        {
            float hue = 0.06f + (HueSeed % 4f) * 0.018f + 0.01f * (float)System.Math.Sin(Timer * 0.015f);
            return Color.Lerp(Main.hslToRgb(hue, 0.72f, 0.56f), new Color(255, 246, 198), 0.28f);
        }

        private static void DrawLine(Texture2D texture, Vector2 start, Vector2 end, Color color, float width)
        {
            Vector2 difference = end - start;
            float length = difference.Length();
            if (length <= 0.01f)
                return;

            Main.EntitySpriteDraw(
                texture,
                start + difference * 0.5f,
                null,
                color,
                difference.ToRotation() + MathHelper.PiOver2,
                texture.Size() * 0.5f,
                new Vector2(width, length / texture.Height),
                SpriteEffects.None,
                0);
        }
    }
}
