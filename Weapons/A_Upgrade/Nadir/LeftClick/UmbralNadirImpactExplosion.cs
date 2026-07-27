using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 左键上挑 / 劈落命中的一次性冥蚀黑洞冲击（第三段的黑洞交给持续奇点）。
    /// 前几帧造成一次范围伤害并把敌人短促地吸向命中点；视觉走共享的分层事件视界。
    /// ai[0] = 连招段数(0/1/2)，决定半径、拉扯与震屏。
    /// </summary>
    public class UmbralNadirImpactExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int Stage => (int)Projectile.ai[0];
        private float Radius => UmbralNadirBalance.GetImpactRadius(Stage);

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 16;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 13 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);

        public override void OnSpawn(IEntitySource source)
        {
            bool finale = Stage >= 2;
            float sizeMult = Radius / 150f;

            SoundEngine.PlaySound(new SoundStyle(finale ? "CalamityMod/Sounds/Item/MeldExplosion" : "CalamityMod/Sounds/Item/MeldBurn")
                with { Volume = finale ? 0.8f : 0.55f, Pitch = finale ? -0.15f : 0.15f + Stage * 0.06f }, Projectile.Center);

            UmbralNadirVisuals.EventHorizon(Projectile.Center, sizeMult, finale);
            UmbralNadirVisuals.MeldSparkBurst(Projectile.Center, 10 + Stage * 6, 5f + Stage * 2f);
            UmbralNadirVisuals.ImplosionDust(Projectile.Center, sizeMult);

            float shake = UmbralNadirBalance.GetImpactScreenShake(Stage);
            if (shake > 0f)
                UmbralNadirVisuals.ScreenShake(Projectile.Center, shake);
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            // 前几帧把敌人短促地吸向命中点（冲刺段的拉扯交给奇点，故其 strength 为 0）
            float strength = UmbralNadirBalance.GetImpactPullStrength(Stage);
            if (strength > 0f && Projectile.timeLeft >= 10)
                UmbralNadirVisuals.PullNPCs(Projectile.Center, UmbralNadirBalance.GetImpactPullRange(Stage), strength);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Voidfrost>(), 90);
            UmbralCorrosionGlobalNPC.AddStacks(target, 1);
        }
    }
}
