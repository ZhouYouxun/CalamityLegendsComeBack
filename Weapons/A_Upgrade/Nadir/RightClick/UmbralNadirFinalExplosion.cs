using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.General;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 第三发投矛命中的终爆——冥蚀天底"呼应循环"的结算点。
    /// 生成时消耗半径内所有敌人的蚀痕层数，消耗越多，本次爆发伤害越高；随后一次范围坍缩。
    /// 只由第三发命中 NPC 时生成；撞墙 / 超时绝不生成它。
    /// </summary>
    public class UmbralNadirFinalExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 18;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 16 ? null : false; // 仅前 2 帧

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, UmbralNadirBalance.FinalExplosionRadius, targetHitbox);

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 c = Projectile.Center;
            float radius = UmbralNadirBalance.FinalExplosionRadius;

            // 消耗半径内所有蚀痕，按总层数放大本次伤害
            int totalStacks = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || Vector2.Distance(npc.Center, c) > radius)
                    continue;
                int s = UmbralCorrosionGlobalNPC.ConsumeStacks(npc);
                if (s > 0)
                {
                    totalStacks += s;
                    // 每个被引爆的蚀痕点上闪一记小黑洞
                    UmbralNadirVisuals.EventHorizon(npc.Center, 0.32f, false);
                    npc.AddBuff(ModContent.BuffType<Voidfrost>(), 180);
                }
            }
            if (totalStacks > 0)
                Projectile.damage += (int)(Projectile.damage * UmbralNadirBalance.FinalStackBonusFraction * totalStacks);

            bool big = totalStacks >= 6;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") with { Volume = big ? 1f : 0.85f, Pitch = big ? -0.25f : -0.1f }, c);
            UmbralNadirVisuals.EventHorizon(c, big ? 1.5f : 1.15f, true);
            UmbralNadirVisuals.ImplosionDust(c, big ? 1.6f : 1.2f);
            UmbralNadirVisuals.MeldSparkBurst(c, big ? 26 : 18, big ? 10f : 8f);
            UmbralNadirVisuals.ScreenShake(c, UmbralNadirBalance.FinalExplosionScreenShake + (big ? 1.5f : 0f));

            // 碎渊：把"引爆"重新变成"再叠层"的起点
            if (Projectile.owner == Main.myPlayer)
            {
                int shards = big ? 6 : 4;
                int shardDamage = Math.Max(1, (int)(Projectile.damage * 0.32f));
                for (int i = 0; i < shards; i++)
                {
                    Vector2 v = (MathHelper.TwoPi * i / shards + Main.rand.NextFloat(-0.3f, 0.3f)).ToRotationVector2() * Main.rand.NextFloat(5f, 9f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, v,
                        ModContent.ProjectileType<UmbralNadirVoidShard>(), shardDamage, Projectile.knockBack * 0.4f, Projectile.owner);
                }
            }
        }

        public override void AI() => Projectile.velocity = Vector2.Zero;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(ModContent.BuffType<Voidfrost>(), 180);
    }
}
