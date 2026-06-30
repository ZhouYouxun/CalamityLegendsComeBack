using CalamityMod;
using CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick.Rules;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.DesertEagle.LeftClick
{
    /// <summary>
    /// 沙漠之鹰左键统一弹幕载体。
    /// ai[0] = 装填枪的 ItemType（0 = 无枪，走默认规则）
    /// ai[1] = 规则自定义额外数据（弹射计数、牌型、冰火切换等）
    /// </summary>
    public class DELeftBullet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/Ranged/ShockblastRound";

        private Player Owner => Main.player[Projectile.owner];

        private DEBulletRule ActiveRule =>
            DEBulletRegistry.GetRule((int)Projectile.ai[0]);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 6;
            Projectile.height = 6;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 600;
            Projectile.light = 0.5f;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.tileCollide = true;
        }

        public override void OnSpawn(IEntitySource source)
        {
            DEBulletRule rule = ActiveRule;

            // 应用规则覆盖的数值
            Projectile.penetrate = rule.Penetrate;
            Projectile.extraUpdates = rule.ExtraUpdates;
            Projectile.ArmorPenetration = rule.ArmorPenetration;
            Projectile.velocity *= rule.SpeedMultiplier;

            rule.SetDefaults(Projectile);
            rule.OnSpawn(Projectile, Owner);
        }

        public override void AI()
        {
            ActiveRule.AI(Projectile, Owner);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            ActiveRule.ModifyHitNPC(Projectile, Owner, target, ref modifiers);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            ActiveRule.OnHitNPC(Projectile, Owner, target, hit, damageDone);
        }

        public override bool? CanHitNPC(NPC target)
        {
            return ActiveRule.CanHitNPC(Projectile, Owner, target);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return ActiveRule.OnTileCollide(Projectile, Owner, oldVelocity);
        }

        public override void OnKill(int timeLeft)
        {
            ActiveRule.OnKill(Projectile, Owner, timeLeft);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DEBulletRule rule = ActiveRule;

            bool shouldDrawDefault = rule.PreDraw(Projectile, Owner, ref lightColor);
            if (shouldDrawDefault)
            {
                // 默认后像绘制
                CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            }

            return false;
        }

        public override void PostDraw(Color lightColor)
        {
            ActiveRule.PostDraw(Projectile, Owner, Main.spriteBatch);
        }
    }
}
