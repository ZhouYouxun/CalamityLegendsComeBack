using CalamityMod.Projectiles.BaseProjectiles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack
{
    public class BrinyBaron_LeftClick_Swing : BaseCustomUseStyleProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/BrinyBaron/NewLegendBrinyBaron";

        // ===============================
        // ❗【必须重写】绑定武器
        // 作用：玩家不拿这把武器时，自动销毁弹幕
        // ===============================
        public override int AssignedItemID => ModContent.ItemType<NewLegendBrinyBaron>();

        // ===============================
        // ❗【必须重写】命中判定范围
        // ===============================
        public override float HitboxOutset => 110f;
        public override Vector2 HitboxSize => new Vector2(160, 160);

        // ===============================
        // ❗【必须重写】判定角度修正
        // ===============================
        public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);

        // ===============================
        // ❗【必须重写】贴图旋转原点
        // ===============================
        public override Vector2 SpriteOrigin => new Vector2(0, 120);

        // ===============================
        // 🔵【可用】位置偏移（一般不用，但可微调）
        // ===============================
        // public override Vector2 Offset => new Vector2(0, 0);

        // ===============================
        // 🔵【可用】帧数（多帧动画时用）
        // ===============================
        // public override int FrameCount => 1;

        // ===============================
        // 内部变量
        // ===============================
        private Vector2 mousePos;
        private Vector2 aimVel;
        private int useAnim;

        private bool doSwing = true;
        private bool postSwing = false;

        private float fade = 0f;

        // ===============================
        // ❗【建议重写】初始化
        // ===============================
        public override void WhenSpawned()
        {
            Projectile.timeLeft = Owner.HeldItem.useAnimation + 1;

            // ai[1]：用于控制左右挥舞方向（1 或 -1）
            Projectile.ai[1] = -1;

            mousePos = Main.MouseWorld;
            aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 60f;

            useAnim = Owner.itemAnimationMax;

            Owner.direction = mousePos.X < Owner.Center.X ? -1 : 1;
            FlipAsSword = Owner.direction == -1;
        }

        // ===============================
        // 🔵【可用】开始挥舞时触发（你之前没用）
        // 作用：第一帧触发，比如音效、初始化连段
        // ===============================
        public override void OnBeginUse()
        {
            // 示例：播放挥舞音效（默认不写）
        }

        // ===============================
        // 🔵【可用】挥舞结束时触发
        // 作用：收尾、爆炸、连段++
        // ===============================
        public override void OnEndUse()
        {
            // 示例：结束特效（默认不写）
        }

        // ===============================
        // 🔵【可用】停止使用时（松开或动画结束）
        // ===============================
        public override void ResetStyle()
        {
            // 重置状态（防止卡状态）
            CanHit = false;
        }

        // ===============================
        // ❗【核心必须重写】挥舞逻辑
        // ===============================
        public override void UseStyle()
        {
            AnimationProgress = Animation % useAnim;

            // =========================
            // 方向控制（跟随鼠标）
            // =========================
            mousePos = Main.MouseWorld;
            aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 60f;

            Owner.direction = mousePos.X < Owner.Center.X ? -1 : 1;
            FlipAsSword = Owner.direction == -1;

            Projectile.rotation = Projectile.rotation.AngleLerp(
                Owner.AngleTo(mousePos) + MathHelper.ToRadians(45f),
                0.15f
            );

            // =========================
            // 挥舞阶段1：前摇
            // =========================
            if (AnimationProgress < useAnim * 0.4f)
            {
                CanHit = false;

                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(120f * Owner.direction * Projectile.ai[1]),
                    0.2f
                );
            }
            // =========================
            // 挥舞阶段2：攻击段
            // =========================
            else
            {
                float time = AnimationProgress - (useAnim * 0.4f);
                float max = useAnim * 0.6f;

                // =========================
                // ❗命中窗口（控制能不能打人）
                // =========================
                if (time > max * 0.2f && time < max * 0.7f)
                {
                    CanHit = true;

                    // =========================
                    // 🌊 海洋特效
                    // =========================
                    for (int i = 0; i < 3; i++)
                    {
                        Vector2 pos = Owner.Center +
                            new Vector2(Main.rand.Next(40, 140), 0)
                            .RotatedBy(FinalRotation);

                        Vector2 vel = new Vector2(0, -8)
                            .RotatedBy(FinalRotation)
                            .RotatedByRandom(0.3f);

                        Dust d = Dust.NewDustPerfect(pos, 33, vel);
                        d.noGravity = true;

                        Dust splash = Dust.NewDustPerfect(pos, DustID.Water);
                        splash.velocity = vel * 0.6f;
                        splash.noGravity = true;
                    }
                }
                else
                    CanHit = false;

                // =========================
                // ❗核心：挥舞角度
                // =========================
                RotationOffset = MathHelper.Lerp(
                    RotationOffset,
                    MathHelper.ToRadians(
                        MathHelper.Lerp(150f * Owner.direction,
                                        -120f * Owner.direction,
                                        time / max)
                    ),
                    0.25f
                );
            }

            // =========================
            // ❗手臂跟随（一般固定）
            // =========================
            ArmRotationOffset = MathHelper.ToRadians(-140f);
            ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
        }

        // ===============================
        // 🔵【可用】命中时逻辑（你现在没用）
        // ===============================
        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 示例：命中特效
        }

        // ===============================
        // 🔵【可用】伤害修改
        // ===============================
        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            // 示例：暴击倍率 / 衰减
        }










    }
}