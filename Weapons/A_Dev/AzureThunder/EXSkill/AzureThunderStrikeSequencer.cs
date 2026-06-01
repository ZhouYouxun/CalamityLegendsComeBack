using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 右键雷击序列器：不造成直接伤害，只按节奏在目标周围生成落雷和终极 AOE。
    internal sealed class AzureThunderStrikeSequencer : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[0] 锁定目标，ai[1] 是雷击次数，ai[2] 编码消耗层数和是否处于天理真和。
        private int TargetIndex => (int)Projectile.ai[0];
        private int StrikeCount => Math.Max(1, (int)Projectile.ai[1]);
        private int ConsumedCharge => Math.Max(0, (int)Projectile.ai[2] / 10);
        private bool HarmonyMode => (int)Projectile.ai[2] % 10 == 1;

        // 本地状态控制序列节奏、地剑脉冲和绘制中心。
        private int timer;
        private int strikesDone;
        private bool commandedSwords;
        private Vector2 focusPoint;

        public override void SetDefaults()
        {
            // 序列器本身不可见、不可伤害，存在时间覆盖完整右键演出。
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            timer++;

            // 目标失效时尝试重新锁最近敌人，仍然没有目标就结束序列。
            NPC target = ResolveTarget();
            if (target == null)
            {
                Projectile.Kill();
                return;
            }

            focusPoint = target.Center;
            Projectile.Center = focusPoint;

            if (HarmonyMode && !commandedSwords)
            {
                // 天理真和只升级演出和范围，不再把地剑强制拖到目标身边。
                AzureThunderSounds.PlayCommandPulse(focusPoint);
                commandedSwords = true;
            }

            // 第一发略有前摇，之后每 5 帧打一发雷。
            int firstStrikeFrame = 12;
            int interval = 5;

            if (strikesDone < StrikeCount && timer >= firstStrikeFrame + strikesDone * interval)
            {
                PulseGroundSwords();
                SpawnStrike(target, strikesDone == StrikeCount - 1);
                strikesDone++;
            }

            if (strikesDone >= StrikeCount && timer > firstStrikeFrame + StrikeCount * interval + 20)
                Projectile.Kill();
        }

        private NPC ResolveTarget()
        {
            // 优先使用创建时锁定的目标，失效后回退到当前位置附近搜索。
            if (TargetIndex >= 0 && Main.npc.IndexInRange(TargetIndex))
            {
                NPC target = Main.npc[TargetIndex];
                if (target.active && target.CanBeChasedBy(Projectile))
                    return target;
            }

            return AzureThunderPlayer.FindNearestTarget(Projectile.Center, 1600f);
        }

        private void CommandGroundSwordsToFocus()
        {
            // 旧逻辑保留：可把范围内地剑命令为俯冲目标，目前不在 AI 中调用。
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            float maxDistance = AzureThunderAccessoryPlayer.GetGroundSwordEffectRadius(Main.player[Projectile.owner]);
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != groundType)
                    continue;

                if (projectile.Distance(focusPoint) > maxDistance)
                    continue;

                if (projectile.ModProjectile is AzureThunderGroundSword sword)
                    sword.BeginDive(focusPoint + Main.rand.NextVector2Circular(60f, 60f), Math.Max(1, (int)(Projectile.damage * 0.9f)), Projectile.knockBack);
            }
        }

        private void PulseGroundSwords()
        {
            // 每次雷击前让范围内地剑闪一下描边，建立“地剑参与右键”的视觉关联。
            int groundType = ModContent.ProjectileType<AzureThunderGroundSword>();
            Player owner = Main.player[Projectile.owner];
            Vector2 pulseCenter = owner.Center;
            float maxDistance = AzureThunderAccessoryPlayer.GetGroundSwordEffectRadius(owner);

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != Projectile.owner || projectile.type != groundType)
                    continue;

                if (projectile.Distance(pulseCenter) > maxDistance)
                    continue;

                if (projectile.ModProjectile is AzureThunderGroundSword sword)
                    sword.PulseLightningOutline();
            }
        }

        private void SpawnStrike(NPC target, bool finalStrike)
        {
            // 最后一击倍率最高；终极模式按消耗层数进一步放大。
            float damageFactor;
            if (finalStrike)
                damageFactor = HarmonyMode ?
                    AzureThunderProgression.UltimateRightClickFinalDamageFactor + ConsumedCharge * AzureThunderProgression.UltimateRightClickChargeDamageBonus :
                    6f;
            else
                damageFactor = HarmonyMode ? 0.81f : 0.45f;

            Vector2 strikePoint = target.Center + Main.rand.NextVector2Circular(HarmonyMode ? 95f : 55f, HarmonyMode ? 55f : 35f);
            bool applyCrumbling = finalStrike &&
                !HarmonyMode &&
                AzureThunderProgression.DownedYharon &&
                AzureThunderPlayer.CountOwnedGroundSwords(Main.player[Projectile.owner]) >= AzureThunderGroundSword.MaxGroundSwords;
            bool applyElectricDebuff = true;

            // 右键序列用竖直雷击，终极模式最后一击生成巨雷。
            AzureThunderPlayer.SpawnVerticalLightning(
                Projectile.GetSource_FromThis(),
                strikePoint,
                target,
                Math.Max(1, (int)(Projectile.damage * damageFactor)),
                Projectile.knockBack,
                Projectile.owner,
                gainCharge: false,
                applyStaticDischarge: applyElectricDebuff,
                big: finalStrike && HarmonyMode,
                ultimateEnergyGain: AzureThunderAccessoryPlayer.GetRightClickLightningEnergyGain(Main.player[Projectile.owner]),
                applyCrumbling: applyCrumbling,
                applyBaseElectricDebuff: true,
                weak: !HarmonyMode,
                speedLines: HarmonyMode,
                lightningScale: HarmonyMode ? 1.5f : 0.72f);

            // 终极右键每发落雷额外补透明 AOE，最后一发范围更大。
            if (HarmonyMode)
                SpawnHarmonyAoe(strikePoint, finalStrike);

            if (finalStrike && HarmonyMode)
            {
                // 终极最后一击补一圈纯视觉平雷，不额外造成隐藏伤害。
                int visualFlags = AzureThunderFlatLightning.VisualOnlyFlag | AzureThunderFlatLightning.BigLightningFlag | AzureThunderFlatLightning.SpeedLineFlag;
                for (int i = 0; i < 5; i++)
                {
                    AzureThunderPlayer.SpawnFlatLightning(
                        Projectile.GetSource_FromThis(),
                        strikePoint,
                        Vector2.UnitX.RotatedByRandom(MathHelper.TwoPi) * Main.rand.NextFloat(16f, 24f),
                        Math.Max(1, (int)(Projectile.damage * 0.45f)),
                        Projectile.knockBack,
                        Projectile.owner,
                        1.5f,
                        visualFlags);
                }
            }
        }

        private void SpawnHarmonyAoe(Vector2 strikePoint, bool finalStrike)
        {
            // AOE 只由本地拥有者生成，避免多人重复生成同一爆炸。
            if (Main.myPlayer != Projectile.owner)
                return;

            // NewLegendSHPE 是透明伤害弹幕，这里只负责设置中心和尺寸。
            int aoeDamage = Math.Max(1, (int)(Projectile.damage * (finalStrike ? 0.95f : 0.38f)));
            int aoe = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                strikePoint,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                aoeDamage,
                Projectile.knockBack,
                Projectile.owner);

            if (!Main.projectile.IndexInRange(aoe))
                return;

            // SHPE 本身不可见，改尺寸就是终极右键透明 AOE 的实际范围。
            Terraria.Projectile explosion = Main.projectile[aoe];
            Vector2 center = explosion.Center;
            int size = finalStrike ? 260 : 190;
            explosion.width = size;
            explosion.height = size;
            explosion.Center = center;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 普通右键没有额外指示圈；终极右键在目标脚下画青金法阵。
            if (!HarmonyMode || focusPoint == Vector2.Zero)
                return false;

            Texture2D circle = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_04").Value;
            Vector2 drawPosition = focusPoint - Main.screenPosition;
            float opacity = Utils.GetLerpValue(0f, 22f, timer, true) * Utils.GetLerpValue(StrikeCount * 5 + 48f, StrikeCount * 5 + 18f, timer, true);
            float scale = 1.6f + (float)Math.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.08f;

            // 加法混合绘制双层反向旋转圆环，强化终极右键的聚焦点。
            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(circle, drawPosition, null, AzureThunderColors.Azure with { A = 0 } * opacity * 0.45f, Main.GlobalTimeWrappedHourly * 0.7f, circle.Size() * 0.5f, scale, SpriteEffects.None);
            Main.EntitySpriteDraw(circle, drawPosition, null, AzureThunderColors.PaleYellow with { A = 0 } * opacity * 0.36f, -Main.GlobalTimeWrappedHourly * 0.55f, circle.Size() * 0.5f, scale * 0.78f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            return false;
        }
    }
}
