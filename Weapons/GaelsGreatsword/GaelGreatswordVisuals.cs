using System;
using CalamityLegendsComeBack.Systems;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    /// <summary>
    /// 盖尔大剑的共享视觉 DNA —— 让整套武器和至尊灾厄 / 灾厄克隆体"一个妈生的"。
    /// 统一硫火红-虚空黑的配色，并复用灾厄自家的三件套：CalamitasMetaball（熔火元球）、
    /// AshesFluidFieldSystem（屏幕级流体火焰，与 SHPC 灰烬武器同一套系统）、以及硫火尘埃。
    /// 旧实现里那种偏蓝的通用"灵魂紫"被收进 <see cref="CrimsonViolet"/> 作为唯一的暗色点缀，
    /// 主色权重全部让给硫火红与黑烟。
    /// </summary>
    internal static class GaelGreatswordVisuals
    {
        // === 至尊灾厄硫火色板 ===
        // 从虚空黑烟 → 深硫红 → 硫火红 → 灼白核心，配一抹烬金作为火舌高光。
        public static readonly Color VoidSmoke = new(20, 6, 9);
        public static readonly Color BrimstoneDeep = new(120, 8, 20);
        public static readonly Color BrimstoneRed = new(196, 18, 32);
        public static readonly Color BrimstoneHot = new(255, 44, 44);
        public static readonly Color EmberGold = new(255, 122, 52);
        // 唯一保留的"紫"：暗红偏品的血怨紫，只当暗部点缀，绝不再用偏蓝的通用魔法紫。
        public static readonly Color CrimsonViolet = new(128, 16, 62);
        // 命中最内层的灼白心。
        public static readonly Color WhiteHot = new(255, 214, 198);

        /// <summary>硫火主色随时间在深硫红↔硫火红之间轻微脉动，读出"活着的火"。</summary>
        public static Color PulsingBrimstone(float phase)
        {
            float t = MathF.Sin(phase) * 0.5f + 0.5f;
            return Color.Lerp(BrimstoneDeep, BrimstoneHot, 0.35f + t * 0.35f);
        }

        /// <summary>血怨主色：深硫红↔血怨紫脉动，用于灵魂 / 骷髅类弹幕的暗部。</summary>
        public static Color PulsingCrimson(float phase)
        {
            float t = MathF.Sin(phase) * 0.5f + 0.5f;
            return Color.Lerp(CrimsonViolet, BrimstoneRed, 0.4f + t * 0.3f);
        }

        /// <summary>
        /// 向共享硫火流体场投喂一个火源（与 SHPC 灰烬武器同一条管线）。
        /// heat 越高越偏烬金，越低越偏硫火红。空闲时该场自动休眠，几乎零开销。
        /// </summary>
        public static void RegisterBrimstoneFire(Vector2 worldPos, Vector2 velocity, float power, float heat = 0.28f)
        {
            if (Main.dedServ)
                return;

            Color fire = Color.Lerp(BrimstoneHot, EmberGold, MathHelper.Clamp(heat, 0f, 1f));
            AshesFluidFieldSystem.RegisterSource(worldPos, velocity, fire, power);
        }

        /// <summary>生成一枚灾厄硫火元球（熔岩质感的流体团块，SCal 战场结界同款技术）。</summary>
        public static CalamitasMetaball.Particle SpawnBrimstoneMetaball(Vector2 worldPos, Vector2 velocity, float size, float sizeScaling = 0.8f)
        {
            if (Main.dedServ)
                return null;

            CalamitasMetaball.Particle blob = CalamitasMetaball.SpawnParticle(worldPos, velocity, size);
            if (blob != null)
                blob.SizeScaling = sizeScaling;
            return blob;
        }

        /// <summary>一撮硫火尘埃：默认走灾厄专用 Brimstone 尘，偶尔掺入黑烟增加体量与暗部。</summary>
        public static Dust SpawnBrimstoneDust(Vector2 position, Vector2 velocity, float scale, bool allowSmoke = true)
        {
            int dustType = allowSmoke && Main.rand.NextBool(4) ? DustID.Smoke : (int)CalamityDusts.Brimstone;
            Dust dust = Dust.NewDustPerfect(position, dustType, velocity);
            dust.noGravity = true;
            dust.scale = scale;
            if (dustType == DustID.Smoke)
                dust.color = VoidSmoke;
            return dust;
        }
    }
}
