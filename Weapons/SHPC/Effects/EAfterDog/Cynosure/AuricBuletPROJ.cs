using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// Cynosure 的主穿甲弹。它不使用普通 SHPC 光球的表现，而是直接承担命中和展开整套火力的职责。
    /// </summary>
    public class CynosureArmorPiercingRound : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private bool PayloadReleased
        {
            get => Projectile.localAI[0] == 1f;
            set => Projectile.localAI[0] = value ? 1f : 0f;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 28;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 300;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.ArmorPenetration = 100;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.18f, 0.65f, 1f) * 0.72f);

            // 飞行中的双轨示波器尾迹。这里刻意使用金蓝双色，方便之后单独调色。
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = forward.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 2; i++)
            {
                float wave = MathF.Sin((Projectile.timeLeft + i * 6f) * 0.34f) * 6f;
                Dust blue = Dust.NewDustPerfect(Projectile.Center + normal * wave, DustID.Electric, -forward * 1.4f, 0, Color.Cyan, 0.95f);
                blue.noGravity = true;
                Dust gold = Dust.NewDustPerfect(Projectile.Center - normal * wave, DustID.GoldFlame, -forward * 1.1f, 0, Color.Gold, 0.85f);
                gold.noGravity = true;
            }

            // 这些小型火花模拟 SHPS 的高速椭圆护航弹幕。它们是纯视觉，不会额外占用大量实体。
            for (int i = 0; i < 3; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * (17f + i * 2f) + i * MathHelper.TwoPi / 3f;
                Vector2 offset = new(MathF.Cos(angle) * 19f, MathF.Sin(angle) * 7f);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(
                    Projectile.Center + offset.RotatedBy(Projectile.rotation),
                    -forward * 1.3f,
                    Color.Cyan,
                    Color.White,
                    0.45f,
                    5,
                    0f,
                    0.8f));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Electrified, 300);
            if (!PayloadReleased)
                ReleasePayload(target.whoAmI);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!PayloadReleased)
                ReleasePayload(-1);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!PayloadReleased)
                ReleasePayload(-1);
        }

        private void ReleasePayload(int preferredTarget)
        {
            PayloadReleased = true;
            if (Projectile.owner != Main.myPlayer)
                return;

            // 命中音效沿用金源弹 / 金源地雷的爆裂声，音色和用户要求的重型攻击更接近。
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/AuricBulletHit") { Volume = 0.9f, Pitch = -0.12f }, Projectile.Center);

            SpawnImpactSparks();

            int burstDamage = Math.Max(1, (int)(Projectile.damage * 0.72f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<CynosureLightningExplosion>(), burstDamage, Projectile.knockBack, Projectile.owner);

            // 两层椭圆：小椭圆更密集，大椭圆更宽。金源珠先从命中点展开，再暂停，最后强追踪。
            SpawnEllipse(preferredTarget, ellipseGroup: 0, count: 12);
            SpawnEllipse(preferredTarget, ellipseGroup: 1, count: 18);

            // 外层充能环：停留后向目标释放细电弧，再原地爆炸。
            const int chargedCount = 12;
            for (int i = 0; i < chargedCount; i++)
            {
                float angle = MathHelper.TwoPi * i / chargedCount;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureChargedCell>(), Math.Max(1, (int)(Projectile.damage * 0.18f)),
                    Projectile.knockBack, Projectile.owner, preferredTarget, angle);
            }
        }

        private void SpawnEllipse(int preferredTarget, int ellipseGroup, int count)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count;
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureAuricBall>(), Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack, Projectile.owner, preferredTarget, ellipseGroup, angle);
            }
        }

        private void SpawnImpactSparks()
        {
            // 绯红恶魔式火花：数量固定，但每条火花的飞行距离在 0.5 到 2 倍之间变化。
            for (int i = 0; i < 28; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f, 20f);
                GeneralParticleHandler.SpawnParticle(new GlowSparkParticle(
                    Projectile.Center,
                    velocity,
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.04f, 0.09f),
                    Main.rand.NextBool(4) ? Color.White : Color.Cyan,
                    new Vector2(2.4f, 0.55f),
                    true));
            }
        }

        internal float TrailWidth(float completion, Vector2 _) => MathHelper.Lerp(18f, 0f, completion);

        internal Color TrailColor(float completion, Vector2 _)
        {
            Color color = Color.Lerp(Color.White, new Color(30, 145, 255), completion);
            return color * (1f - completion);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            GameShaders.Misc["CalamityMod:OverpoweredTouhouSpearShader"]
                .SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(Projectile.oldPos,
                new PrimitiveSettings(TrailWidth, TrailColor, (_, _) => Projectile.Size * 0.5f, shader: GameShaders.Misc["CalamityMod:OverpoweredTouhouSpearShader"]),
                42);
            return false;
        }
    }
}
