using System;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 通用平雷/落雷弹幕：通过 ai[0] 的 bit flag 切换充能、静电、巨雷和纯视觉模式。
    internal sealed class AzureThunderFlatLightning : ModProjectile, ILocalizedModType
    {
        // 命中后给一息万变充能。
        public const int GainChargeFlag = 1;

        // 命中后施加静电放电；终极状态下改为终极 DoT。
        public const int StaticDischargeFlag = 2;

        // 放大视觉、碰撞和音效，给右键终结或终极技使用。
        public const int BigLightningFlag = 4;

        // 犽戎后特定右键终结雷击附加碎裂。
        public const int CrumblingFlag = 8;

        // 纯视觉雷用于造剑/消散演出，必须保证不会变成隐藏伤害源。
        public const int VisualOnlyFlag = 16;

        // 跳过基础 Electrified，用于只想表现终极 DoT 的雷击。
        public const int NoBaseElectricDebuffFlag = 32;

        public new string LocalizationCategory => "Projectiles.AzureThunder";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[0] 统一解释为标志位，避免在生成函数里传多个 ai 参数。
        private int Flags => (int)Projectile.ai[0];
        private bool GainCharge => (Flags & GainChargeFlag) != 0;
        private bool ApplyStaticDischarge => (Flags & StaticDischargeFlag) != 0;
        private bool BigLightning => (Flags & BigLightningFlag) != 0;
        private bool ApplyCrumbling => (Flags & CrumblingFlag) != 0;
        private bool VisualOnly => (Flags & VisualOnlyFlag) != 0;
        private bool ApplyBaseElectricDebuff => (Flags & NoBaseElectricDebuffFlag) == 0;
        public int time;
        public float colorValue;
        public float sizeMult = 1f;

        public override void SetDefaults()
        {
            // 平雷本体不可见，依靠粒子表现；extraUpdates 让雷线移动更顺滑。
            Projectile.width = 15;
            Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 100;
            Projectile.extraUpdates = 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        // 纯视觉模式完全关闭伤害判定。
        public override bool? CanDamage() => VisualOnly ? false : null;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];

            // colorValue 逐渐推向 50，用于在青色和紫色之间漂移。
            colorValue = MathHelper.Lerp(colorValue, 50f, 0.025f);
            Color usedColor = Color.Lerp(Color.Cyan, Color.Orchid, Utils.GetLerpValue(0f, 50f, colorValue));

            if (time == 0)
            {
                // 首帧初始化视觉强度并播放出生音，巨雷会略微放大粒子。
                colorValue += 30f;
                sizeMult = BigLightning ? 1.35f : 1f;
                AzureThunderSounds.PlayLightningSpawn(Projectile.Center, BigLightning);
            }

            float targetDist = Vector2.Distance(owner.Center, Projectile.Center);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            if (targetDist < 1400f && Projectile.timeLeft > 5)
            {
                Vector2 pos = Projectile.Center;
                if (time % 2 == 0)
                {
                    // 前 120 帧喷射正向火花，之后保留较轻的雷粒。
                    if (time < 120)
                    {
                        Particle forwardSpark = new CustomSpark(pos, Projectile.velocity * 3.6f * sizeMult, "CalamityMod/Particles/GlowSpark", false, 11, 0.15f * sizeMult, usedColor, new Vector2(2f, 0.8f), true, true, shrinkSpeed: 1f);
                        GeneralParticleHandler.SpawnParticle(forwardSpark);
                        sizeMult *= 0.985f;
                    }

                    Particle bolt = new BoltParticle(pos, -Projectile.velocity * 0.05f, false, 30, 0.6f, usedColor, new Vector2(1.8f, 0.8f), true, true, false, 0.3f);
                    GeneralParticleHandler.SpawnParticle(bolt);
                }

                if (Main.rand.NextBool(18))
                {
                    // 少量侧向电弧增加雷击的不稳定感。
                    Particle sideBolt = new BoltParticle(pos, Projectile.velocity.RotatedByRandom(0.6f) * Main.rand.NextFloat(0.3f, 1.9f), false, 23, Main.rand.NextFloat(0.2f, 0.25f), usedColor, new Vector2(1.8f, 0.8f), true, true, false, 0.3f);
                    GeneralParticleHandler.SpawnParticle(sideBolt);
                }

                if (Main.rand.NextBool(5))
                {
                    // DrainLineBloom 拉出长尾，强调雷线速度。
                    Particle drainLine = new CustomSpark(pos, Projectile.velocity * Main.rand.NextFloat(-0.4f, 0.4f), "CalamityMod/Particles/DrainLineBloom", false, 80, Main.rand.NextFloat(1.2f, 1.3f) * sizeMult, usedColor, new Vector2(1f, 4f), true, true);
                    GeneralParticleHandler.SpawnParticle(drainLine);
                }

                if (time % 3 == 0)
                {
                    // RGB dust 是兜底的原版粒子，确保低配置下仍有电光反馈。
                    Dust dust = Dust.NewDustPerfect(pos, DustID.FireworksRGB, new Vector2(5f, 5f).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f), 0, default, Main.rand.NextFloat(0.45f, 0.6f));
                    dust.noGravity = true;
                    dust.color = usedColor;
                }
            }

            time++;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (VisualOnly)
                return;

            // 同一道雷连续穿透时伤害递减，避免多段命中溢出。
            float damageMult = Utils.Remap(Projectile.numHits, 0f, 3f, 1f, 0.15f, true);
            modifiers.SourceDamage *= damageMult;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (VisualOnly)
                return;

            Player owner = Main.player[Projectile.owner];
            AzureThunderPlayer thunderPlayer = owner.GetModPlayer<AzureThunderPlayer>();

            // 最后一段左键雷可读取目标身上的电系 debuff 并转化为充能层数。
            if (GainCharge)
                thunderPlayer.TryGainThunderChargeFromTarget(target);

            // 基础电击可被 NoBaseElectricDebuffFlag 关闭，供终极右键精确控制 debuff。
            if (ApplyBaseElectricDebuff)
                target.AddBuff(BuffID.Electrified, 300);

            // 饰品联动统一在命中点触发。
            AzureThunderAccessoryPlayer.ApplyAzureThunderAccessoryOnHit(Projectile, target);
            if (Projectile.numHits == 0)
            {
                // 第一次命中后快速消失并生成主爆点，后续穿透不重复播放完整爆炸。
                Projectile.timeLeft = Math.Min(Projectile.timeLeft, 5);
                float fxScale = BigLightning ? 3.6f : 3f;
                Vector2 pos = target.Center;
                AzureThunderSounds.PlayLightningImpact(pos, BigLightning);
                for (int i = 0; i < (int)(7 * fxScale); i++)
                {
                    Particle bolt = new BoltParticle(pos, (new Vector2(4f, 4f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.3f, 1.9f), true, 13, Main.rand.NextFloat(0.1f, 0.15f) * fxScale, Main.rand.NextBool(5) ? Color.Cyan : Color.Orchid, new Vector2(1.8f, 0.8f), true, true, false, 0.7f);
                    GeneralParticleHandler.SpawnParticle(bolt);

                    Dust dust = Dust.NewDustPerfect(pos, ModContent.DustType<LightDust>(), (new Vector2(5f, 5f) * fxScale).RotatedByRandom(100f) * Main.rand.NextFloat(0.5f, 1f), 0, default, Main.rand.NextFloat(0.4f, 0.55f) * fxScale);
                    dust.noGravity = !Main.rand.NextBool(3);
                    dust.color = Main.rand.NextBool(5) ? Color.Cyan : Color.Orchid;
                }

                Particle pulse = new CustomPulse(pos, Vector2.Zero, Color.Cyan, "CalamityMod/Particles/HighResFoggyCircleHardEdge", new Vector2(1f, 1f), 0f, 0f, 0.0815f * fxScale, 10);
                GeneralParticleHandler.SpawnParticle(pulse);
                for (int i = 0; i < 2; i++)
                {
                    Particle orb = new CustomPulse(pos, Vector2.Zero, Color.Orchid, "CalamityMod/Particles/BloomCircle", new Vector2(1f, 1f), Main.rand.NextFloat(-10f, 10f), 1.38f * fxScale, 0.5f * fxScale, 14);
                    GeneralParticleHandler.SpawnParticle(orb);
                    Particle whiteOrb = new CustomPulse(pos, Vector2.Zero, Color.White, "CalamityMod/Particles/BloomCircle", new Vector2(1f, 1f), Main.rand.NextFloat(-10f, 10f), 0.925f * fxScale, 0.2f * fxScale, 14);
                    GeneralParticleHandler.SpawnParticle(whiteOrb);
                }
            }

            if (ApplyStaticDischarge)
            {
                // 终极状态下静电标志升级为 ApplyUltimateDot。
                if (thunderPlayer.HarmonyActive)
                    AzureThunderPlayer.ApplyUltimateDot(target, 180);
                else
                    target.AddBuff(ModContent.BuffType<StaticDischarge>(), 180);
            }

            if (ApplyCrumbling)
                target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.StatDebuffs.Crumbling>(), 180);

            // ai[2] 存放本次命中奖励的终极能量。
            if (Projectile.ai[2] > 0f)
                owner.GetModPlayer<AzureThunderPlayer>().AddUltimateEnergy((int)Projectile.ai[2]);

            colorValue += 18f;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Player owner = Main.player[Projectile.owner];
            float size = 45f * Math.Max(sizeMult, BigLightning ? 1.15f : 0.9f) * (Projectile.numHits > 0 ? 6f : 1f);
            if (time <= 1)
            {
                // 首帧用玩家到雷点的线段碰撞，表现“从天/远处劈向目标”的路径。
                float collisionPoint = float.NaN;
                return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, owner.Center, size, ref collisionPoint);
            }

            // 之后退化为圆形命中盒，防止粒子已到目标时漏判。
            return CalamityUtils.CircularHitboxCollision(Projectile.Center, size, targetHitbox);
        }

        public override bool? CanCutTiles() => false;
    }
}
