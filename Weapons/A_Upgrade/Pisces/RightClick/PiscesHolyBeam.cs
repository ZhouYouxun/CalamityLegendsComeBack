using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Anchor;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using static Terraria.ModLoader.ModContent;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.RightClick
{
    /// <summary>
    /// 满蓄释放的双束短神圣激光——取长补短：借 Purge Guzzler/HolyLaser 的 Start/Mid/End 三段结构与双束略向内收拢，
    /// 但削弱掉持续射击的密度与大束高倍率；借 Hyperdeath 的“爆发前预告-中心亮核”节拍，却砍掉超长超粗与黑色内层。
    /// 层级从内到外固定：白核 → 青蓝主带 → 金白薄边 → 极低透明度椭圆 Bloom；无黑描边、外发光不比主体粗。
    /// 长度 ≤ ~1200px、碰撞宽 20-28px、寿命 ≤ 18 tick；视觉先迅速展开再收束，绝不持续扫射。
    /// </summary>
    public class PiscesHolyBeam : BaseLaserbeamProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Pisces";

        public override float Lifetime => IsRapidBeam ? 10f : PiscesBalance.HolyBeamLifetime;
        public override float MaxScale => IsRapidBeam ? 0.23f : 0.46f;
        public override float MaxLaserLength => IsRapidBeam ? 620f : PiscesBalance.HolyBeamLength;
        public override float ScaleExpandRate => 5f;

        private const string LaserTexturePath = "CalamityMod/ExtraTextures/Lasers/ProvidenceHolyRay";
        public override Texture2D LaserBeginTexture => Request<Texture2D>(LaserTexturePath + "Start").Value;
        public override Texture2D LaserMiddleTexture => Request<Texture2D>(LaserTexturePath + "Mid").Value;
        public override Texture2D LaserEndTexture => Request<Texture2D>(LaserTexturePath + "End").Value;
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override Color LaserOverlayColor => PiscesVisuals.AuroraWhite with { A = 0 };
        public override Color LightCastColor => Color.Transparent;

        // ai[0] = 双束侧向符号(-1/+1)，ai[1] = 持械弹幕索引，ai[2] = 武器基准伤害（供联动）
        private int SideSign => Math.Sign(Projectile.ai[0]);
        private bool IsRapidBeam => Projectile.ai[2] < 0f;
        private int BaseWeaponDamage => Math.Abs((int)Projectile.ai[2]);
        private Projectile Holdout => Main.projectile[(int)Projectile.ai[1]];
        private bool HoldoutValid => (int)Projectile.ai[1] >= 0 && (int)Projectile.ai[1] < Main.maxProjectiles
            && Holdout.active && Holdout.type == ModContent.ProjectileType<PiscesOpticHoldout>();

        private Player Owner;
        private float extraRot;
        private bool triedLink;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 20;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.friendly = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.DamageType = DamageClass.Ranged;
        }

        public override float DetermineLaserLength() => Math.Min(MaxLaserLength, DetermineLaserLength_CollideWithTiles());

        public override void UpdateLaserMotion()
        {
            Vector2 baseDir = HoldoutValid ? Holdout.velocity : Projectile.velocity;
            if (Time == 0)
                extraRot = IsRapidBeam ? 0f : SideSign * PiscesBalance.HolyBeamHalfAngle;
            else
                extraRot *= 0.9f; // 双束在寿命内略微向内收拢

            Projectile.velocity = baseDir.RotatedBy(extraRot);
            Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
        }

        public override void AttachToSomething()
        {
            Owner ??= Main.player[Projectile.owner];
            if (HoldoutValid && Holdout.ModProjectile is PiscesOpticHoldout optic)
                Projectile.Center = optic.GunTipPosition;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float _ = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, PiscesBalance.HolyBeamHitWidth, ref _);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);
            // 多段命中递减，避免长激光过强
            if (Projectile.numHits > 1)
                Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.62f));
        }

        public override void ExtraBehavior()
        {
            Owner ??= Main.player[Projectile.owner];

            // 联动②：满蓄激光擦过锚点 → 沿方向串链（全链 0.75s 内部冷却在系统里自限）。
            if (!IsRapidBeam && !triedLink && Time >= 3)
            {
                triedLink = true;
                PiscesLinkSystem.TryLinkFromBeam(Projectile, Projectile.Center, Projectile.velocity, BaseWeaponDamage);
            }

            if (Main.dedServ)
                return;

            // 干净的沿线光点（少量、稳定）
            Vector2 point = Vector2.Lerp(Projectile.Center, Projectile.Center + Projectile.velocity * LaserLength, Main.rand.NextFloat());
            if (Main.rand.NextBool())
            {
                Dust d = Dust.NewDustPerfect(point + Main.rand.NextVector2Circular(5f, 5f), PiscesVisuals.HolyDust,
                    Projectile.velocity * Main.rand.NextFloat(1f, 6f), 60, PiscesVisuals.AuroraLerp(Main.rand.NextFloat()), Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            PiscesVisuals.BeginAdditive(Main.spriteBatch);

            // 速射光束比终结双束更轻：两条实体光带 + 稀疏辅光，不抢走满蓄释放的视觉位阶。
            DrawBloomAlongBeam();
            if (!IsRapidBeam)
                DrawBeamWithColor(PiscesVisuals.GoldWhite with { A = 0 } * 0.5f, Projectile.scale * 1.12f);
            // 青蓝主光带
            DrawBeamWithColor(PiscesVisuals.AuroraCyan with { A = 0 } * 0.95f, Projectile.scale);
            // 白色细核心
            DrawBeamWithColor(PiscesVisuals.AuroraWhite with { A = 0 }, Projectile.scale * 0.5f);

            PiscesVisuals.EndAdditive(Main.spriteBatch);
            return false;
        }

        private void DrawBloomAlongBeam()
        {
            Texture2D bloom = PiscesVisuals.SmallBloom.Value;
            Vector2 origin = bloom.Size() * 0.5f;
            float len = LaserLength;
            int steps = Math.Max(1, (int)(len / (IsRapidBeam ? 155f : 60f)));
            float opacity = IsRapidBeam ? 0.10f : 0.16f;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 pos = Projectile.Center + Projectile.velocity * (len * i / steps) - Main.screenPosition;
                Main.spriteBatch.Draw(bloom, pos, null, PiscesVisuals.AuroraCyan with { A = 0 } * opacity, Projectile.rotation,
                    origin, new Vector2(Projectile.scale * (IsRapidBeam ? 1.25f : 1.8f), Projectile.scale * (IsRapidBeam ? 1.25f : 1.8f)), SpriteEffects.None, 0f);
            }
        }
    }
}
