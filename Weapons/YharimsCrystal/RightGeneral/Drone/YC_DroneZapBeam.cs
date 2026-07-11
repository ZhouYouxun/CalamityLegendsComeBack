using CalamityLegendsComeBack.Weapons.YharimsCrystal.Passive;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.RightGeneral
{
    // AlphaRay-inspired attached ray. One lives on every ship for the full channel and
    // continuously follows that ship's deliberately lazy aim instead of flashing once.
    internal sealed class YC_DroneZapBeam : ModProjectile, ILocalizedModType
    {
        private const float VisualLength = 2600f;
        private const float CollisionWidth = 18f;
        private static readonly Color ZapGold = new(255, 220, 92);
        private static readonly Color ZapRed = new(255, 80, 36);

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float SpinSeed => ref Projectile.localAI[0];
        private ref float Timer => ref Projectile.localAI[1];
        private int ParentIndex => (int)Projectile.ai[0];

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
            Projectile.localNPCHitCooldown = 12;
            Projectile.timeLeft = 2;
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
            if (ParentIndex < 0 || ParentIndex >= Main.maxProjectiles)
            {
                Projectile.Kill();
                return;
            }

            Projectile parent = Main.projectile[ParentIndex];
            if (!parent.active || parent.owner != Projectile.owner || parent.type != ModContent.ProjectileType<YC_RightDrone>())
            {
                Projectile.Kill();
                return;
            }

            Vector2 direction = parent.rotation.ToRotationVector2();
            Projectile.Center = parent.Center + direction * 22f;
            Projectile.velocity = direction;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.timeLeft = 2;
            Timer++;
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
            return Utils.GetLerpValue(0f, 8f, Timer, true);
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

            float breathing = 0.88f + System.MathF.Sin(Timer * 0.19f + SpinSeed) * 0.12f;
            Color outerColor = Color.Lerp(ZapRed, ZapGold, 0.28f) with { A = 0 } * opacity;
            Color innerColor = Color.White with { A = 0 } * opacity;

            Main.EntitySpriteDraw(outerLine, lineCenter, null, outerColor * 0.72f, Projectile.rotation + MathHelper.PiOver2, outerLine.Size() * 0.5f, new Vector2(1.55f * breathing, 54f * lengthScale) * 0.01f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(outerLine, lineCenter, null, ZapGold with { A = 0 } * 0.38f * opacity, Projectile.rotation + MathHelper.PiOver2, outerLine.Size() * 0.5f, new Vector2(0.72f * breathing, 54f * lengthScale) * 0.01f, SpriteEffects.FlipVertically);
            Main.EntitySpriteDraw(innerLine, lineCenter, null, innerColor, Projectile.rotation + MathHelper.PiOver2, innerLine.Size() * 0.5f, new Vector2(0.2f * breathing, 54f * lengthScale) * 0.01f, SpriteEffects.FlipVertically);

            for (int i = 0; i < 3; i++)
            {
                float spin = SpinSeed + Main.GlobalTimeWrappedHourly * (2.2f + i * 0.6f) * (i % 2 == 0 ? 1f : -1f);
                Main.EntitySpriteDraw(ring, start, null, ZapGold with { A = 0 } * 0.46f * opacity, spin, ring.Size() * 0.5f, (0.1f + i * 0.035f) * breathing, SpriteEffects.None);
            }

            for (int i = 0; i < 5; i++)
            {
                float orbitAngle = SpinSeed + Timer * (0.045f + i * 0.006f) + MathHelper.TwoPi * i / 5f;
                Vector2 orbit = orbitAngle.ToRotationVector2() * (5f + i * 1.3f) * breathing;
                Main.EntitySpriteDraw(bloom, start + orbit, null, (i % 2 == 0 ? ZapGold : Color.White) with { A = 0 } * 0.32f * opacity, orbitAngle, bloom.Size() * 0.5f, 0.07f + i * 0.012f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, start, null, Color.White with { A = 0 } * 0.72f * opacity, 0f, bloom.Size() * 0.5f, 0.11f * breathing, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, start + unit * (VisualLength - 16f), null, ZapGold with { A = 0 } * 0.48f * opacity, 0f, bloom.Size() * 0.5f, 0.14f * breathing, SpriteEffects.None);

            return false;
        }
    }
}
