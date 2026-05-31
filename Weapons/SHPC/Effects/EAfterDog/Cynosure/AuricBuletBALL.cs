using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// 命中点展开的普通金源珠。ai[0] 是优先目标，ai[1] 是椭圆层级，ai[2] 是槽位角度。
    /// </summary>
    public class CynosureAuricBall : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Cynosure/AuricBall";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private Vector2 OrbitCenter
        {
            get => new(Projectile.localAI[1], Projectile.localAI[2]);
            set
            {
                Projectile.localAI[1] = value.X;
                Projectile.localAI[2] = value.Y;
            }
        }

        private float majorAxis;
        private float minorAxis;
        private float orbitSpeed;
        private float orbitDirection;
        private float axisPulse;
        private float ellipseRotation;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 240;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                OrbitCenter = Projectile.Center;

                bool outer = Projectile.ai[1] != 0f;
                majorAxis = outer ? Main.rand.NextFloat(230f, 336f) : Main.rand.NextFloat(112f, 174f);
                minorAxis = outer ? Main.rand.NextFloat(150f, 214f) : Main.rand.NextFloat(82f, 132f);
                orbitDirection = Main.rand.NextBool() ? 1f : -1f;
                orbitSpeed = Main.rand.NextFloat(0.058f, 0.126f) * orbitDirection;
                axisPulse = Main.rand.NextFloat(MathHelper.TwoPi);
                ellipseRotation = Main.rand.NextFloat(MathHelper.TwoPi);
            }

            float age = 240f - Projectile.timeLeft;

            if (age < 42f)
            {
                // 展开阶段：从中心逐渐形成两个尺寸不同的椭圆。
                float progress = CalamityUtils.SineOutEasing(age / 42f, 1);
                Projectile.Center = OrbitCenter + GetOrbitOffset(age) * progress;
                Projectile.velocity = Vector2.Zero;
            }
            else if (age < 82f)
            {
                // 停留阶段：金源珠略微旋转，让两层结构可读，而不是一闪即逝。
                Projectile.Center = OrbitCenter + GetOrbitOffset(age);
                Projectile.velocity = Vector2.Zero;
            }
            else
            {
                NPC target = CynosureTargeting.FindTarget((int)Projectile.ai[0], Projectile.Center);
                if (target != null)
                {
                    Vector2 wantedVelocity = Projectile.SafeDirectionTo(target.Center) * 24f;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, wantedVelocity, 0.16f);
                }
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.5f, 0.92f) * 0.55f);
            Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, -Projectile.velocity * 0.05f, 0, Color.Cyan, 0.72f);
            dust.noGravity = true;
        }

        private Vector2 GetOrbitOffset(float age)
        {
            float axisWarpA = 1f + 0.13f * (float)Math.Sin(age * 0.055f + axisPulse);
            float axisWarpB = 1f + 0.16f * (float)Math.Cos(age * 0.061f + axisPulse * 0.7f);
            float angle = Projectile.ai[2] + age * orbitSpeed + (float)Math.Sin(age * 0.049f + axisPulse) * 0.12f;
            float rotation = ellipseRotation + (float)Math.Sin(age * 0.025f + axisPulse) * 0.55f + age * 0.006f * orbitDirection;

            return new Vector2(
                MathF.Cos(angle) * majorAxis * axisWarpA,
                MathF.Sin(angle) * minorAxis * axisWarpB
            ).RotatedBy(rotation);
        }

        public override bool? CanDamage() => 240f - Projectile.timeLeft >= 82f ? null : false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;

            // 蓝色半透明幻影拖尾。越靠后的残影越淡。
            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                Color afterimageColor = new Color(32, 154, 255, 0) * ((Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length) * 0.55f;
                Main.EntitySpriteDraw(texture, Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition, null,
                    afterimageColor, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }
}
