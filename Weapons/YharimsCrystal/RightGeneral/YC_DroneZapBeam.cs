using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    // Fired by the rear pair of the drone battery: an instant raycast zap, not a sustained
    // beam. Same "flash and gone" reference as the flanking gun's burst laser, but the
    // line-art and color story are entirely our gold/red palette instead of blue/white.
    internal sealed class YC_DroneZapBeam : ModProjectile, ILocalizedModType
    {
        private const float VisualLength = 2200f;
        private const float CollisionWidth = 20f;
        private static readonly Color ZapGold = new(255, 220, 92);
        private static readonly Color ZapRed = new(255, 80, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float SpinSeed => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 4400;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 9;
            Projectile.timeLeft = 14;
            Projectile.tileCollide = false;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            YharimsCrystalHellBladeGlobalProjectile.Mark(Projectile, YCWeaponForm.Crystal);
            SpinSeed = Main.rand.NextFloat(MathHelper.TwoPi);
        }

        public override void AI()
        {
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, Color.Lerp(ZapRed, ZapGold, 0.5f).ToVector3() * 0.6f * FadeOpacity());
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * VisualLength,
                CollisionWidth,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(new BalanceYharimsCrystal().GetFireDebuffType(), 180);
        }

        private float FadeOpacity()
        {
            float fadeIn = Utils.GetLerpValue(0f, 3f, 14f - Projectile.timeLeft, true);
            float fadeOut = Utils.GetLerpValue(0f, 4f, Projectile.timeLeft, true);
            return fadeIn * fadeOut;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float opacity = FadeOpacity();
            if (opacity <= 0f)
                return false;

            Texture2D outerLine = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D innerLine = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 unit = Projectile.velocity;
            Vector2 lineCenter = start + unit * VisualLength * 0.5f;
            float lengthScale = VisualLength / 1000f;

            Color outerColor = ZapRed with { A = 0 } * opacity;
            Color innerColor = Color.White with { A = 0 } * opacity;

            Main.EntitySpriteDraw(outerLine, lineCenter, null, outerColor, Projectile.rotation + MathHelper.PiOver2, outerLine.Size() * 0.5f, new Vector2(1.2f, 46f * lengthScale) * 0.01f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(innerLine, lineCenter, null, innerColor, Projectile.rotation + MathHelper.PiOver2, innerLine.Size() * 0.5f, new Vector2(0.26f, 46f * lengthScale) * 0.01f, SpriteEffects.FlipVertically);

            for (int i = 0; i < 3; i++)
            {
                float spin = SpinSeed + Main.GlobalTimeWrappedHourly * (2.2f + i * 0.6f);
                Main.EntitySpriteDraw(ring, start, null, ZapGold with { A = 0 } * 0.5f * opacity, spin, ring.Size() * 0.5f, 0.12f + i * 0.03f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, start, null, Color.White with { A = 0 } * 0.65f * opacity, 0f, bloom.Size() * 0.5f, 0.1f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, start + unit * (VisualLength - 16f), null, ZapGold with { A = 0 } * 0.55f * opacity, 0f, bloom.Size() * 0.5f, 0.16f, SpriteEffects.None);

            return false;
        }
    }
}
