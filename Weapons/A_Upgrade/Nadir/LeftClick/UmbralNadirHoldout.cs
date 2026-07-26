using System;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick;
using CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.Shared;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using CalamityMod.Projectiles.BaseProjectiles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 左键近战三段连招：上劈(0) → 下劈(1) → 冲刺贯穿(2)。
    /// 沿用镀金长喙 BaseSwordHoldoutProjectile 的持握挥舞骨架，配合冥思系黑绿虚空特效。
    /// 每段命中都会释放冥融虚空核，冲刺段追加黑日坍缩爆裂与虚空触须。
    /// </summary>
    public class UmbralNadirHoldout : BaseSwordHoldoutProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Upgrade/Nadir/UmbralNadirSpear";
        public override Item BaseItem => ModContent.GetModItem(ModContent.ItemType<UmbralNadir>()).Item;

        // 冥思配色（与灾厄冥思系列一致：纯黑 + Color.LightGreen）
        private static readonly Color MeldGreen = Color.LightGreen;
        private static readonly Color MeldGreenSoft = Color.LightGreen;

        // === BaseSwordHoldout 配置 ===
        public override bool AlternateSwings => false;   // 连招阶段由 UmbralNadirPlayer 掌控
        public override bool useAttackSpeed => true;
        public override bool useMeleeSize => true;
        public override int swingWidth => 220;
        public override float lineCollisionLength => 200f;
        public override int AfterImageLength => 0;
        // 自绘长柄贴图（矛头在左上，按原版天底戳刺弹幕的口径 radialDir + 135°），
        // 故关闭 base 的剑用着色器刀光，改由本类 PreDraw 绘制冥思黑影拖尾。
        public override bool drawSwordTrail => false;

        /// <summary>当前挥砍所处连招阶段（0 上劈 / 1 下劈 / 2 冲刺）。</summary>
        private int stage;

        private float StageDamageMult => stage switch
        {
            0 => UmbralNadirBalance.UpSlashDamageMult,
            1 => UmbralNadirBalance.DownSlashDamageMult,
            _ => UmbralNadirBalance.DashDamageMult,
        };

        public override void Defaults()
        {
            Projectile.width = Projectile.height = 120;
            Projectile.extraUpdates = 3; // 更顺滑的挥舞 VFX
            Projectile.noEnchantmentVisuals = true;
        }

        public override void Spawn()
        {
            Player player = Main.player[Projectile.owner];
            stage = player.GetModPlayer<UmbralNadirPlayer>().ConsumeComboStage();

            // 阶段尺寸成长（会与后续的近战体型加成叠乘）
            Projectile.scale *= UmbralNadirBalance.GetLeftScale();

            // 三段各自的手感参数（在 base 乘算 MaxUpdates/攻速之前设定）
            switch (stage)
            {
                case 0: // 上劈：上挑起手，快速斩落
                    StartupTime = 6;
                    CooldownTime = 8;
                    OffsetDistance = 48;
                    RotateInStartup = 0.35f;
                    RotateInCooldown = 0.22f;
                    UseSound = SoundID.Item71;
                    break;
                case 1: // 下劈：反向重斩
                    StartupTime = 6;
                    CooldownTime = 8;
                    OffsetDistance = 48;
                    RotateInStartup = 0.35f;
                    RotateInCooldown = 0.22f;
                    UseSound = SoundID.Item71;
                    break;
                default: // 冲刺贯穿
                    StartupTime = 5;
                    CooldownTime = 12;
                    OffsetDistance = 30;
                    RotateInStartup = 0.5f;
                    RotateInCooldown = 0.15f;
                    UseSound = SoundID.DD2_BetsysWrathImpact;
                    Projectile.CritChance = 100; // 冲刺贯穿必定暴击
                    break;
            }

            // 与镀金长喙一致：从总挥舞时长里划出起手与收招
            swingTime -= StartupTime + CooldownTime;
            if (swingTime < 6)
                swingTime = 6;
        }

        public override float SwingFunction()
        {
            // 返回相对瞄准中心的挥砍偏移角（弧度）
            return stage switch
            {
                // 上劈：自下方上挑再劈落
                0 => MathHelper.ToRadians(MathHelper.SmoothStep(-80f, 118f, SwingCompletion)),
                // 下劈：自上方反向劈落
                1 => MathHelper.ToRadians(MathHelper.SmoothStep(118f, -80f, SwingCompletion)),
                // 冲刺：几乎直指瞄准方向的小幅贯穿
                _ => MathHelper.ToRadians(MathHelper.SmoothStep(26f, -26f, SwingCompletion)),
            };
        }

        public override void AdditionalAI()
        {
            Player player = Main.player[Projectile.owner];

            // 右键优先：右键三连投掷接管时，左键连招立即让位
            if (player.ownedProjectileCounts[ModContent.ProjectileType<UmbralNadirThrowController>()] > 0)
            {
                Projectile.Kill();
                return;
            }

            player.GetModPlayer<UmbralNadirPlayer>().KeepComboAlive();

            // 冲刺段：像 Byleth 侧必杀那样把玩家沿瞄准水平方向推进
            if (stage == 2 && inSwing)
            {
                float forwardX = -MathF.Sign(angle.X); // angle 指向背离鼠标；-angle 朝向鼠标
                if (forwardX == 0f)
                    forwardX = player.direction;
                const float dashStrength = 13.5f;
                ref Vector2 vel = ref player.velocity;
                if (forwardX * vel.X < dashStrength)
                    vel.X += forwardX * 1.2f;

                OffsetDistance = (int)MathHelper.Lerp(-18f, 112f, MathF.Pow(SwingCompletion, 1.5f));
            }

            // 挥舞期间的冥思黑绿粒子（每真实帧一次，避免 extraUpdates 刷爆）
            if (inSwing && Projectile.FinalExtraUpdate())
                SpawnSwingParticles(player);
            // 朝向不再依赖 base 的剑用旋转——由 PreDraw 按 radialDir + 135° 自绘。
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player player = Main.player[Projectile.owner];
            Texture2D tex = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 origin = tex.Size() * 0.5f;
            Vector2 armCenter = player.MountedCenter;

            float SpearRotation(Vector2 center) =>
                (center - armCenter).SafeNormalize(Vector2.UnitX).ToRotation() + MathHelper.PiOver2 + MathHelper.PiOver4;

            // 冥思黑影拖尾：沿挥舞轨迹回溯几帧画纯黑吸光残影
            if (inSwing)
            {
                int trail = Math.Min(8, Projectile.oldPos.Length);
                for (int i = trail - 1; i >= 1; i--)
                {
                    if (Projectile.oldPos[i] == Vector2.Zero)
                        continue;
                    Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                    float fade = (1f - i / (float)trail) * 0.42f;
                    Main.EntitySpriteDraw(tex, oldCenter - Main.screenPosition, null, Color.Black * fade,
                        SpearRotation(oldCenter), origin, Projectile.scale * 0.98f, SpriteEffects.None, 0);
                }
            }

            // 本体：纯黑剪影垫底（负光）+ 主体
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float rot = SpearRotation(Projectile.Center);
            Main.EntitySpriteDraw(tex, drawPos, null, Color.Black * 0.5f, rot, origin, Projectile.scale * 1.08f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, drawPos, null, lightColor, rot, origin, Projectile.scale, SpriteEffects.None, 0);

            player.heldProj = Projectile.whoAmI;
            return false;
        }

        private void SpawnSwingParticles(Player player)
        {
            Vector2 bladeDir = (Projectile.Center - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            Vector2 tangent = bladeDir.RotatedBy(MathHelper.PiOver2);
            bool dash = stage == 2;

            // 沿整根长柄多点撒冥思粒子（刀身 + 刀尖），密集且饱满
            float[] reaches = { 26f, 44f, 62f };
            foreach (float reach in reaches)
            {
                Vector2 pos = Projectile.Center + bladeDir * reach * Projectile.scale + tangent * Main.rand.NextFloat(-8f, 8f);

                // 黑色发光火花（GlowSpark2，AlphaBlend 吸光拖尾）
                GeneralParticleHandler.SpawnParticle(new CustomSpark(pos,
                    tangent.RotatedByRandom(0.5f) * Main.rand.NextFloat(1.5f, 4.5f), "CalamityMod/Particles/GlowSpark2",
                    false, Main.rand.Next(12, 18), Main.rand.NextFloat(0.045f, 0.07f), Color.Black, new Vector2(0.6f, 1.3f), false));

                // 荧光绿发光火花（GlowSpark，顶层加色，事件视界高光）
                GeneralParticleHandler.SpawnParticle(new CustomSpark(pos,
                    tangent.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.2f, 3.5f), "CalamityMod/Particles/GlowSpark",
                    false, Main.rand.Next(11, 16), Main.rand.NextFloat(0.022f, 0.04f), MeldGreen, new Vector2(0.6f, 1.3f), true, false),
                    false, GeneralDrawLayer.AfterEverything);
            }

            Vector2 tip = Projectile.Center + bladeDir * 56f * Projectile.scale;

            // 纯黑球体（GenericBloom, AlphaBlend）—— 冥思负光核心
            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                tip + Main.rand.NextVector2Circular(6f, 6f), tangent * Main.rand.NextFloat(-1f, 1f),
                Color.Black, (dash ? 0.4f : 0.28f) * Projectile.scale, Main.rand.Next(9, 13), true, false));

            // 荧光绿加色辉光（顶层）
            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                tip, bladeDir * Main.rand.NextFloat(0.4f, 1.4f), MeldGreen with { A = 0 },
                (dash ? 0.34f : 0.24f) * Projectile.scale, Main.rand.Next(9, 13), false, true),
                false, GeneralDrawLayer.AfterEverything);

            // 黑色重烟（挥舞黑气流）
            if (Main.rand.NextBool(2))
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + bladeDir * Main.rand.NextFloat(20f, 60f) * Projectile.scale,
                    tangent * Main.rand.NextFloat(-2f, 2f), Color.Black, Main.rand.Next(18, 28),
                    Main.rand.NextFloat(0.4f, 0.7f), 0.5f, Main.rand.NextFloat(-0.05f, 0.05f), false));

            // 反向虚空尘（内黑外绿）
            for (int i = 0; i < 2; i++)
            {
                Vector2 dpos = Projectile.Center + bladeDir * Main.rand.NextFloat(18f, 64f) * Projectile.scale;
                Dust vd = Dust.NewDustPerfect(dpos, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = tangent.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.5f, 2.4f);
                vd.scale = Main.rand.NextFloat(0.9f, 1.5f);
                vd.color = MeldGreen;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            base.ModifyHitNPC(target, ref modifiers); // 保留 base 的 HitDirectionOverride
            modifiers.SourceDamage *= StageDamageMult;
            // 冲刺段的强制暴击通过 Spawn() 里的 Projectile.CritChance = 100 实现
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 左键命中永远同时召唤"多种"来自深渊的弹幕：深渊裂隙(手搓纯特效) + 虚空触须，
            // 裂隙自身还会持续吐出触须与碎渊——让战斗一刻不停。虚空核则专属右键，此处不用。
            switch (stage)
            {
                case 0: // 上挑斩：撕开一道小型深渊裂隙 + 两条触须
                    target.AddBuff(ModContent.BuffType<Voidfrost>(), 150);
                    SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.4f, Pitch = -0.15f }, target.Center);
                    SpawnEventHorizon(target.Center, 0.55f, 0.95f, 20);
                    MeldSparkBurst(target.Center, 12, 5.5f);
                    SpawnAbyssRift(target.Center, 0.55f);
                    SpawnTentacleSpread(target.Center, Main.rand.Next(2, 4), 4f, 7f);
                    break;

                case 1: // 劈落斩：更大裂隙 + 触须扇 + 尘暴
                    target.AddBuff(ModContent.BuffType<Voidfrost>(), 210);
                    SoundEngine.PlaySound(SoundID.Item104 with { Volume = 0.5f }, target.Center);
                    SpawnEventHorizon(target.Center, 0.85f, 1.35f, 24);
                    MeldSparkBurst(target.Center, 18, 7f);
                    VoidDustBurst(target.Center, 20, 6.5f);
                    SpawnAbyssRift(target.Center, 0.72f);
                    SpawnTentacleSpread(target.Center, Main.rand.Next(3, 5), 4.5f, 8f);
                    break;

                default: // 冲刺贯穿：黑日坍缩 + 双错位裂隙 + 触须环 + 震屏
                    target.AddBuff(ModContent.BuffType<Voidfrost>(), 300);
                    SoundEngine.PlaySound(SoundID.Item104 with { Pitch = -0.1f }, target.Center);
                    SpawnDeadSunCollapse(target.Center);
                    MeldSparkBurst(target.Center, 28, 9f);
                    VoidDustBurst(target.Center, 36, 9f);
                    SpawnAbyssRift(target.Center, 0.95f);
                    SpawnAbyssRift(target.Center + Main.rand.NextVector2Circular(46f, 46f), 0.7f);
                    // 触须环，带随机相位与速度，绝不整齐划一
                    int arms = Main.rand.Next(6, 9);
                    for (int i = 0; i < arms; i++)
                    {
                        float ang = MathHelper.TwoPi * i / arms + Main.rand.NextFloat(-0.25f, 0.25f);
                        SpawnTentacle(target.Center, ang.ToRotationVector2() * Main.rand.NextFloat(4.5f, 8f));
                    }
                    ApplyScreenShake(target.Center, 6.5f);
                    break;
            }
        }

        // ===== 特效辅助 =====

        /// <summary>撕开一道深渊裂隙（手搓无贴图纯特效弹幕），受同屏数量约束。</summary>
        private void SpawnAbyssRift(Vector2 center, float damageMult)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            Player player = Main.player[Projectile.owner];
            if (player.ownedProjectileCounts[ModContent.ProjectileType<UmbralNadirAbyssRift>()] >= 14)
                return;
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), center, Vector2.Zero,
                ModContent.ProjectileType<UmbralNadirAbyssRift>(),
                Math.Max(1, (int)(Projectile.damage * damageMult)), Projectile.knockBack * 0.5f, Projectile.owner);
        }

        private void SpawnTentacleSpread(Vector2 center, int count, float minSpeed, float maxSpeed)
        {
            for (int i = 0; i < count; i++)
                SpawnTentacle(center, Main.rand.NextVector2Unit() * Main.rand.NextFloat(minSpeed, maxSpeed));
        }

        private void SpawnTentacle(Vector2 pos, Vector2 vel)
        {
            if (Projectile.owner != Main.myPlayer)
                return;
            float curl0 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            float curl1 = Main.rand.NextFloat(0.01f, 0.08f) * (Main.rand.NextBool() ? -1f : 1f);
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), pos, vel,
                ModContent.ProjectileType<VoidTentacle>(),
                (int)(Projectile.damage * UmbralNadirBalance.TentacleDamageMult),
                Projectile.knockBack, Projectile.owner, curl0, curl1);
        }

        /// <summary>纯黑事件视界（AlphaBlend 吸光核）+ 荧光绿加色光环。</summary>
        private void SpawnEventHorizon(Vector2 center, float blackFinalScale, float greenFinalScale, int life)
        {
            // 黑核必须用透明底贴图(SmallBloom)，否则 AlphaBlend 会把 BloomCircle 的不透明黑底画成方块。
            // SmallBloom(400px) 约为 BloomCircle(200px) 两倍，故缩放减半。
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, Color.Black,
                "CalamityMod/Particles/SmallBloom", Vector2.One, 0f, blackFinalScale * 0.175f, blackFinalScale * 0.5f, life, false));

            // 绿环走加色(UseAdditiveBlend=true)，with{A=0}，黑底加 0 不出框，BloomCircle 安全。
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, MeldGreen with { A = 0 },
                "CalamityMod/Particles/BloomCircle", Vector2.One, 0f, greenFinalScale * 0.3f, greenFinalScale, life, true),
                false, GeneralDrawLayer.AfterEverything);
        }

        /// <summary>冥思招牌「外绿内黑」黑日坍缩爆裂（用于冲刺终段）。</summary>
        private void SpawnDeadSunCollapse(Vector2 center)
        {
            // 外围绿色扩散光晕（顶层加色）
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, MeldGreen with { A = 0 },
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.18f, 0.85f, 28),
                false, GeneralDrawLayer.AfterEverything);

            // 双重纯黑坍缩核心（AlphaBlend 吸光）
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Black,
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.12f, 0.55f, 26, false));
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(center, Vector2.Zero, Color.Black,
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.1f, 0.35f, 24, false));

            // 慢速吞噬的事件视界脉冲
            SpawnEventHorizon(center, 1.5f, 2.1f, 40);
        }

        private void MeldSparkBurst(Vector2 center, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(speed * 0.4f, speed);
                // 黑火花（底层）
                GeneralParticleHandler.SpawnParticle(new AltSparkParticle(center, vel, false,
                    Main.rand.Next(14, 22), Main.rand.NextFloat(0.7f, 1.2f), Color.Black));
                // 绿火花（顶层加色）
                if (Main.rand.NextBool())
                    GeneralParticleHandler.SpawnParticle(new SparkParticle(center, vel * 0.75f, false,
                        Main.rand.Next(12, 18), Main.rand.NextFloat(0.45f, 0.8f), MeldGreen),
                        false, GeneralDrawLayer.AfterEverything);
            }
        }

        private void VoidDustBurst(Vector2 center, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(speed * 0.35f, speed);
                int d = Dust.NewDust(center - new Vector2(8f), 16, 16, ModContent.DustType<VoidDustInverted>(),
                    vel.X, vel.Y, 0, MeldGreenSoft, Main.rand.NextFloat(1.0f, 1.7f));
                Main.dust[d].noGravity = true;
                Main.dust[d].velocity = vel;
                Main.dust[d].color = MeldGreenSoft;
            }
        }

        private static void ApplyScreenShake(Vector2 source, float power)
        {
            float distanceFactor = Utils.GetLerpValue(1400f, 0f, Vector2.Distance(source, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }
    }
}
