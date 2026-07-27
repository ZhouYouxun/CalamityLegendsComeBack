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

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Combo
{
    /// <summary>
    /// 回旋奇点新星——回旋斩迹把敌人卷成一团并将奇点蓄满后，以玩家为中心释放的黑洞坍缩。
    /// 强力拉扯 + 消耗范围内全部蚀痕（消耗越多伤害越高）+ 一次大范围坍缩。回旋的高潮结算。
    /// ai[0] = 左键基础伤害（生成时写入）。
    /// </summary>
    public class UmbralNadirSpinNova : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 60;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 24;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 20 ? null : false; // 前 4 帧

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, UmbralNadirBalance.SpinNovaRadius, targetHitbox);

        public override void OnSpawn(IEntitySource source)
        {
            // 基准伤害 = 左键基础 × 新星倍率
            Projectile.damage = Math.Max(1, (int)(Projectile.ai[0] * UmbralNadirBalance.SpinNovaDamageMult));

            Vector2 c = Projectile.Center;
            float radius = UmbralNadirBalance.SpinNovaRadius;

            int totalStacks = 0;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || Vector2.Distance(npc.Center, c) > radius)
                    continue;
                int s = UmbralCorrosionGlobalNPC.ConsumeStacks(npc);
                if (s > 0)
                {
                    totalStacks += s;
                    UmbralNadirVisuals.EventHorizon(npc.Center, 0.34f, false);
                    npc.AddBuff(ModContent.BuffType<Voidfrost>(), 240);
                }
            }
            if (totalStacks > 0)
                Projectile.damage += (int)(Projectile.damage * UmbralNadirBalance.FinalStackBonusFraction * totalStacks);

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") with { Volume = 1f, Pitch = -0.3f }, c);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/EarthMeteor") with { Volume = 0.8f, Pitch = -0.2f }, c);
            UmbralNadirVisuals.EventHorizon(c, 2f, true);
            UmbralNadirVisuals.ImplosionDust(c, 2f);
            UmbralNadirVisuals.MeldSparkBurst(c, 32, 12f);
            UmbralNadirVisuals.ScreenShake(c, 7f);

            // 碎渊四散，命中再叠蚀痕，把新星的引爆续成下一轮
            if (Projectile.owner == Main.myPlayer)
            {
                int shardDamage = Math.Max(1, (int)(Projectile.damage * 0.28f));
                for (int i = 0; i < 8; i++)
                {
                    Vector2 v = (MathHelper.TwoPi * i / 8f + Main.rand.NextFloat(-0.2f, 0.2f)).ToRotationVector2() * Main.rand.NextFloat(6f, 11f);
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), c, v,
                        ModContent.ProjectileType<RightClick.UmbralNadirVoidShard>(), shardDamage, Projectile.knockBack * 0.4f, Projectile.owner);
                }
            }
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            // 强力吸拢
            UmbralNadirVisuals.PullNPCs(Projectile.Center, UmbralNadirBalance.SpinNovaRadius, 2.4f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(ModContent.BuffType<Voidfrost>(), 240);
    }
}
