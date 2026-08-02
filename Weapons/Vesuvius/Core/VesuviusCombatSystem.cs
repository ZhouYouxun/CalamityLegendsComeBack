using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.Core
{
    /// <summary>
    /// “火山灾祸”只负责显示标记，伤害倍率和命中喷发都由同目录下的全局状态统一处理。
    /// 这样后续新增维苏威阿斯弹幕时，不需要每一种弹幕再复制一遍倍率判断。
    /// </summary>
    public sealed class VesuviusVolcanicCalamity : ModBuff, ILocalizedModType
    {
        public new string LocalizationCategory => "Buffs";
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
        }
    }

    internal static class VesuviusCombatSystem
    {
        public const int VolcanicCalamityDuration = 17 * 60;

        public static bool IsVesuviusProjectile(Projectile projectile)
        {
            string projectileNamespace = projectile.ModProjectile?.GetType().Namespace;
            const string rootNamespace = "CalamityLegendsComeBack.Weapons.Vesuvius";
            return projectileNamespace == rootNamespace ||
                projectileNamespace?.StartsWith(rootNamespace + ".") == true;
        }

        public static void ApplyVolcanicCalamity(NPC target, int duration = VolcanicCalamityDuration)
        {
            target.AddBuff(ModContent.BuffType<VesuviusVolcanicCalamity>(), duration);
        }

        public static void ApplyStrongestUnlockedFire(NPC target)
        {
            int onFire = BuffID.OnFire;
            int brimstone = ModContent.BuffType<BrimstoneFlames>();
            int daybreak = BuffID.Daybreak;
            int holyFire = ModContent.BuffType<HolyFlames>();
            int dragonFire = ModContent.BuffType<Dragonfire>();

            int[] fireDebuffs = { onFire, brimstone, daybreak, holyFire, dragonFire };
            int selectedIndex = DownedBossSystem.downedYharon ? 4 :
                DownedBossSystem.downedProvidence ? 3 :
                NPC.downedGolemBoss ? 2 :
                Main.hardMode ? 1 : 0;
            int keptIndex = selectedIndex;

            // 当前时期决定维苏威阿斯能够施加的档位；如果目标身上已经有别的来源施加的更高级火焰，
            // 则保留更高级者而不反向降级。最后清掉其余四档，界面和实际结算都只剩一个最高 DoT。
            for (int i = fireDebuffs.Length - 1; i > selectedIndex; i--)
            {
                if (target.HasBuff(fireDebuffs[i]))
                {
                    keptIndex = i;
                    break;
                }
            }

            for (int i = 0; i < fireDebuffs.Length; i++)
            {
                if (i == keptIndex)
                    continue;

                int index = target.FindBuffIndex(fireDebuffs[i]);
                if (index >= 0)
                    target.DelBuff(index);
            }

            if (keptIndex == selectedIndex)
                target.AddBuff(fireDebuffs[selectedIndex], 240);
        }
    }

    internal sealed class VesuviusVolcanicCalamityNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private bool ashSoulAwarded;

        public bool TryAwardAshSoul(int owner)
        {
            if (ashSoulAwarded || !Main.player.IndexInRange(owner))
                return false;

            ashSoulAwarded = true;
            Main.player[owner].GetModPlayer<VesuviusPassivePlayer>().AddAshSoul();
            return true;
        }

        public override void ModifyIncomingHit(NPC npc, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff<VesuviusVolcanicCalamity>())
                modifiers.FinalDamage *= 1.05f;
        }

        public override void ModifyHitByProjectile(NPC npc, Projectile projectile, ref NPC.HitModifiers modifiers)
        {
            if (npc.HasBuff<VesuviusVolcanicCalamity>() && VesuviusCombatSystem.IsVesuviusProjectile(projectile))
                modifiers.FinalDamage *= 1.2f;
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (!npc.HasBuff<VesuviusVolcanicCalamity>() || Main.rand.NextBool(4) == false)
                return;

            Dust ash = Dust.NewDustDirect(
                npc.position,
                npc.width,
                npc.height,
                Main.rand.NextBool(3) ? DustID.InfernoFork : DustID.Smoke,
                0f,
                -0.6f,
                120,
                Color.Lerp(new Color(63, 35, 51), new Color(255, 104, 35), Main.rand.NextFloat(0.18f, 0.52f)),
                Main.rand.NextFloat(0.55f, 0.95f));
            ash.noGravity = true;
            ash.velocity += Main.rand.NextVector2Circular(0.7f, 0.7f);
        }
    }

    internal sealed class VesuviusProjectileGlobal : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        private bool isVesuviusProjectile;
        private Vector2 originalVelocity;

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            isVesuviusProjectile = VesuviusCombatSystem.IsVesuviusProjectile(projectile);
            originalVelocity = projectile.velocity;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!isVesuviusProjectile || damageDone <= 0 || !Main.player.IndexInRange(projectile.owner))
                return;

            VesuviusCombatSystem.ApplyStrongestUnlockedFire(target);

            bool marked = target.HasBuff<VesuviusVolcanicCalamity>();
            if (marked && projectile.type != ModContent.ProjectileType<VesuviusMagmaSpout>() && projectile.owner == Main.myPlayer)
            {
                Vector2 eruptionDirection = projectile.velocity.SafeNormalize(
                    originalVelocity.SafeNormalize(Vector2.UnitY));
                VesuviusMagmaSpout.SpawnFromMarkedHit(projectile, target, eruptionDirection, damageDone);
            }

            if (target.life <= 0)
                target.GetGlobalNPC<VesuviusVolcanicCalamityNPC>().TryAwardAshSoul(projectile.owner);
        }
    }
}
