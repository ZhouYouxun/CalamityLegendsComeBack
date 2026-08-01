using Microsoft.Xna.Framework;
using System;
using CalamityMod;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    public sealed class SeasSearingPlayer : ModPlayer
    {
        public const int UltimateCooldownFrames    = 75 * 60;
        private const float BasePressureRadius     = 430f;
        private const float MaxPressureRadiusBonus = 170f;

        // 每级辐射需要的击中次数（累计）
        private static readonly int[] RadiationThresholds = { 15, 40, 80, 140 };
        private const int RadiationBuffDuration = 10 * 60; // 10秒内不攻击才会失效

        public bool  HoldingSeasSearing  { get; private set; }
        public int   UltimateCooldown    { get; private set; }
        public float PressureVisualPower { get; private set; }

        // 玩家辐射系统（4级正面效果）
        public bool HasRadiationBuff    { get; set; }
        public int  RadiationLevel      { get; private set; }   // 0-4（原始积累等级）
        public int  RadiationAccumulator { get; private set; }  // 击中计数器

        // 辐射兴奋等级同样受酸雨进度限制：进度 0/1/2/3 分别最高 1/2/3/4 级。
        // 积累等级保持原始值，封顶只在实际生效与显示时应用，因此提升酸雨进度可立即解锁更高等级。
        public int MaxRadiationLevel       => SS_Balance.GetAcidRainTier() + 1;
        public int EffectiveRadiationLevel => Math.Min(RadiationLevel, MaxRadiationLevel);

        public override void ResetEffects()
        {
            HoldingSeasSearing = false;
            HasRadiationBuff   = false;
        }

        public override void PostUpdate()
        {
            if (UltimateCooldown > 0) UltimateCooldown--;
            SyncUltimateCooldownDisplay();

            UpdateRadiationBuff();

            if (!HoldingSeasSearing)
            {
                PressureVisualPower = MathHelper.Clamp(PressureVisualPower - 0.04f, 0f, 1f);
                return;
            }

            int   totalPollution  = SeasSearingPollutionNPC.CountPollutionForOwner(Player.whoAmI);
            float pollutionFactor = MathHelper.Clamp(totalPollution / 200f, 0f, 1f);
            PressureVisualPower   = MathHelper.Lerp(PressureVisualPower, 0.32f + pollutionFactor * 0.68f, 0.08f);

            ApplyPressureField(pollutionFactor);
            EmitPressureAtmosphere(pollutionFactor);
        }

        // 玩家击中敌人时调用，增加辐射积累
        public void OnHitWithSeasSearing()
        {
            if (!HoldingSeasSearing) return;
            RadiationAccumulator++;
            int newLevel = 0;
            for (int i = RadiationThresholds.Length - 1; i >= 0; i--)
                if (RadiationAccumulator >= RadiationThresholds[i]) { newLevel = i + 1; break; }
            RadiationLevel = newLevel;
            if (RadiationLevel > 0)
                Player.AddBuff(ModContent.BuffType<SeasSearingPlayerRadiationBuff>(), RadiationBuffDuration);
        }

        private void UpdateRadiationBuff()
        {
            // 未持有武器时不能获得新积累，但积累值保留（buff也保留）
            // 持有时buff被 HasRadiationBuff=true 一直维持
            if (HasRadiationBuff && HoldingSeasSearing && RadiationLevel > 0)
                Player.AddBuff(ModContent.BuffType<SeasSearingPlayerRadiationBuff>(), 2);

            // 没有buff时重置等级和积累（玩家主动取消buff后生效）
            if (!HasRadiationBuff && RadiationLevel > 0)
            {
                if (Player.FindBuffIndex(ModContent.BuffType<SeasSearingPlayerRadiationBuff>()) < 0)
                {
                    RadiationLevel       = 0;
                    RadiationAccumulator = 0;
                }
            }

            ApplyRadiationEffects();
        }

        private void ApplyRadiationEffects()
        {
            int level = EffectiveRadiationLevel;
            if (!HasRadiationBuff || level <= 0) return;

            // 每级辐射带来的正面效果（远程伤害与远程穿透相较旧版本减半，移速与暴击不变）
            // Level 1: 轻微伤害加成
            // Level 2: 额外穿透
            // Level 3: 移动速度
            // Level 4: 最强效果
            switch (level)
            {
                case 1:
                    Player.GetDamage(DamageClass.Ranged) += 0.04f;
                    break;
                case 2:
                    Player.GetDamage(DamageClass.Ranged) += 0.08f;
                    Player.GetArmorPenetration(DamageClass.Ranged) += 4;
                    break;
                case 3:
                    Player.GetDamage(DamageClass.Ranged) += 0.13f;
                    Player.GetArmorPenetration(DamageClass.Ranged) += 8;
                    Player.moveSpeed += 0.12f;
                    break;
                case 4:
                    Player.GetDamage(DamageClass.Ranged) += 0.19f;
                    Player.GetArmorPenetration(DamageClass.Ranged) += 13;
                    Player.moveSpeed += 0.20f;
                    Player.GetCritChance(DamageClass.Ranged) += 10f;
                    break;
            }
        }

        public void SetHoldingSeasSearing() => HoldingSeasSearing = true;

        public bool CanUseUltimate => UltimateCooldown <= 0;

        public void StartUltimateCooldown() => UltimateCooldown = UltimateCooldownFrames;

        public void FastForwardUltimateCooldown(int frames)
        {
            UltimateCooldown = Math.Max(0, UltimateCooldown - Math.Max(0, frames));
            SyncUltimateCooldownDisplay();
        }

        private void SyncUltimateCooldownDisplay()
        {
            if (!HoldingSeasSearing && UltimateCooldown <= 0)
                return;

            if (Player.Calamity().cooldowns.TryGetValue(SeasSearingUltimateCooldown.ID, out var cooldown))
            {
                cooldown.duration = UltimateCooldownFrames;
                cooldown.timeLeft = UltimateCooldown;
                return;
            }

            Player.AddCooldown(SeasSearingUltimateCooldown.ID, UltimateCooldownFrames).timeLeft = UltimateCooldown;
        }

        private void ApplyPressureField(float pollutionFactor)
        {
            float radius        = BasePressureRadius + MaxPressureRadiusBonus * pollutionFactor;
            float radiusSquared = radius * radius;
            int   owner         = Player.whoAmI;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy()) continue;

                float distanceSquared = Vector2.DistanceSquared(Player.Center, npc.Center);
                if (distanceSquared > radiusSquared) continue;

                float proximity  = 1f - MathHelper.Clamp((float)Math.Sqrt(distanceSquared) / radius, 0f, 1f);
                float slowPower  = MathHelper.Lerp(0.035f, 0.13f, proximity) * MathHelper.Lerp(0.6f, 1.25f, pollutionFactor);
                if (npc.knockBackResist <= 0f || npc.boss) slowPower *= 0.38f;

                npc.position -= npc.velocity * slowPower;
                npc.velocity *= 1f - slowPower * 0.35f;

                SeasSearingPollutionNPC pollution = npc.GetGlobalNPC<SeasSearingPollutionNPC>();
                pollution.ExposeToPressure(npc, owner, proximity);
            }
        }

        private void EmitPressureAtmosphere(float pollutionFactor)
        {
            if (Main.dedServ) return;
            Lighting.AddLight(Player.Center, new Vector3(0.03f, 0.16f, 0.22f) * (0.8f + pollutionFactor));

            int interval = pollutionFactor > 0.6f ? 4 : 7;
            if (Main.GameUpdateCount % interval != 0) return;

            float   radius   = BasePressureRadius * Main.rand.NextFloat(0.58f, 1.04f);
            Vector2 offset   = Main.rand.NextVector2CircularEdge(radius, radius * Main.rand.NextFloat(0.5f, 0.95f));
            Vector2 position = Player.Center + offset;
            Vector2 velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.35f, 1.1f);
            Color   color    = Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat(0.12f, 0.75f));

            Dust dust = Dust.NewDustPerfect(position,
                Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                velocity, 135, color, Main.rand.NextFloat(0.55f, 1.05f));
            dust.noGravity = true;
        }
    }
}
