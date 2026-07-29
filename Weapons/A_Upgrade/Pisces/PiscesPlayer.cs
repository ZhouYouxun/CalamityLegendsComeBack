using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces.Anchor;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Pisces
{
    /// <summary>
    /// 双鱼座的玩家侧状态——只放“仅该玩家拥有”的东西：
    ///   · 左键 3 小 1 大 的发射计数；
    ///   · 联动全链冷却（满蓄激光串链 0.75 秒内部冷却）；
    ///   · 局部锚点索引（owner 维护的活跃锚点列表，供联动做半径 / 方向查询，禁止每 tick 扫全表）。
    /// 锚点本体是弹幕（<see cref="PiscesAnchor"/>），本表只保存它们的 whoAmI，spawn 注册、despawn 注销。
    /// </summary>
    public sealed class PiscesPlayer : ModPlayer
    {
        /// <summary>左键“3 小 1 大”循环计数（0..BigShotInterval-1）。</summary>
        public int LeftShotCounter;

        /// <summary>满蓄激光联动的全链内部冷却剩余 tick。</summary>
        public int BeamLinkCooldown;

        /// <summary>owner 维护的活跃锚点弹幕索引（按注册先后 = 生成先后排序）。</summary>
        public readonly List<int> ActiveAnchors = new();

        public override void ResetEffects()
        {
            if (BeamLinkCooldown > 0)
                BeamLinkCooldown--;
        }

        public void RegisterAnchor(int projIndex)
        {
            if (!ActiveAnchors.Contains(projIndex))
                ActiveAnchors.Add(projIndex);
        }

        public void UnregisterAnchor(int projIndex)
        {
            ActiveAnchors.Remove(projIndex);
        }

        /// <summary>去掉已失效（非活跃 / 类型不符）的索引，返回仍然有效的锚点弹幕。</summary>
        public IEnumerable<Projectile> EnumerateAnchors()
        {
            int anchorType = ModContent.ProjectileType<PiscesAnchor>();
            for (int i = ActiveAnchors.Count - 1; i >= 0; i--)
            {
                int idx = ActiveAnchors[i];
                if (idx < 0 || idx >= Main.maxProjectiles)
                {
                    ActiveAnchors.RemoveAt(i);
                    continue;
                }
                Projectile p = Main.projectile[idx];
                if (!p.active || p.type != anchorType || p.owner != Player.whoAmI)
                {
                    ActiveAnchors.RemoveAt(i);
                    continue;
                }
                yield return p;
            }
        }
    }
}
