using System;
using System.IO;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;
using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Holdout;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.RightClick
{
    /// <summary>
    /// 回收晶片（文档第 4.3 / 4.4 节）。主伪激光在终点分解出的一对晶片之一。
    /// 三幕：6 tick 展开（沿末端法线移开 28px，无伤害）→ 24 tick 沿二次贝塞尔镜像弧飞回枪口
    /// （线段伤害）→ 5 tick 收束钻入枪口回收环（无伤害）后消失。
    /// 一对晶片共享 returnGroupId：同组对同一 NPC 只结算一次；每片自身只伤首个有效目标。
    /// </summary>
    internal sealed class AethersWhisperReturnShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AethersWhisper";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly int TotalLife =
            AethersWhisperBalance.ShardExpandTicks + AethersWhisperBalance.ShardReturnTicks + AethersWhisperBalance.ShardReassembleTicks;

        private int GroupId => (int)Projectile.ai[0];
        private int Side => (int)Projectile.ai[1];

        private Vector2 endPos;        // 主束终点（贝塞尔起点基准）
        private Vector2 controlPoint;  // 贝塞尔控制点
        private Vector2 expandNormal;  // 展开方向（末端法线 × side）
        private Vector2 prevCenter;
        private bool hasDealtDamage;

        private int Age => TotalLife - Projectile.timeLeft;

        /// <summary>由父束在生成后立即写入（并触发 netUpdate 同步）。</summary>
        public void Setup(Vector2 shardEndPos, Vector2 shardControl, Vector2 shardExpandNormal)
        {
            endPos = shardEndPos;
            controlPoint = shardControl;
            expandNormal = shardExpandNormal;
            Projectile.Center = shardEndPos;
            prevCenter = shardEndPos;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.penetrate = -1;                // 单目标由 hasDealtDamage 控制，不靠 penetrate
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = TotalLife;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false; // 位置完全由贝塞尔/展开驱动

        // 只有回收折返段（且尚未伤过目标）才造成伤害。
        public override bool? CanDamage() =>
            !hasDealtDamage && Age >= AethersWhisperBalance.ShardExpandTicks &&
            Age < AethersWhisperBalance.ShardExpandTicks + AethersWhisperBalance.ShardReturnTicks;

        public override void AI()
        {
            prevCenter = Projectile.Center;

            int age = Age;
            Vector2 expandedPos = endPos + expandNormal * AethersWhisperBalance.ShardExpandOffset;

            if (age < AethersWhisperBalance.ShardExpandTicks)
            {
                // 展开：由粗光束收缩成细晶片，沿法线移开 28px。
                float t = age / (float)AethersWhisperBalance.ShardExpandTicks;
                Projectile.Center = Vector2.Lerp(endPos, expandedPos, MathHelper.SmoothStep(0f, 1f, t));
            }
            else if (age < AethersWhisperBalance.ShardExpandTicks + AethersWhisperBalance.ShardReturnTicks)
            {
                // 回收：二次贝塞尔镜像弧 expandedPos → controlPoint → 枪口回收环（实时）。
                float t = (age - AethersWhisperBalance.ShardExpandTicks) / (float)AethersWhisperBalance.ShardReturnTicks;
                Projectile.Center = QuadraticBezier(expandedPos, controlPoint, GetMuzzle(), t);
            }
            else
            {
                // 收束重组：钻入枪口回收环。
                Projectile.Center = GetMuzzle();
                if (Projectile.timeLeft <= 1 && !Main.dedServ)
                    ReassembleFlash();
            }

            Vector2 motion = Projectile.Center - prevCenter;
            if (motion.LengthSquared() > 0.01f)
                Projectile.rotation = motion.ToRotation();

            Lighting.AddLight(Projectile.Center, AethersWhisperVisuals.AetherPurple.ToVector3() * 0.4f);
        }

        // 线段首碰（上一帧 → 当前帧，宽度 12）。
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (CanDamage() != true)
                return false;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                prevCenter, Projectile.Center, AethersWhisperBalance.ShardHitWidth, ref collisionPoint);
        }

        // 同组防重：该 NPC 已被本组伤过 → 穿过而不结算（不消耗自己的唯一命中）。
        public override bool? CanHitNPC(NPC target)
        {
            if (hasDealtDamage)
                return false;
            if (AethersWhisperGlobalNPC.IsGroupBlocked(target, Projectile.owner, GroupId))
                return false;
            return null;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            AethersWhisperGlobalNPC.RegisterGroupHit(target, Projectile.owner, GroupId);
            hasDealtDamage = true; // 每片只伤首个有效目标
        }

        private Vector2 GetMuzzle()
        {
            Player owner = Main.player[Projectile.owner];
            // 优先跟随当前持握弹幕（左/右 holdout）的枪口方向；其枪口即回收环所在。
            int sweep = ModContent.ProjectileType<AethersWhisperSweepHoldout>();
            int hold = ModContent.ProjectileType<AethersWhisperHoldout>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == Projectile.owner && (p.type == sweep || p.type == hold))
                {
                    Vector2 aim = p.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
                    return owner.MountedCenter + aim * (62f + AethersWhisperBalance.MuzzleRingRadius) + new Vector2(0f, -6f * owner.gravDir);
                }
            }
            // 兜底：朝向面向方向的枪口前方。
            return owner.MountedCenter + new Vector2(owner.direction * 46f, -6f * owner.gravDir);
        }

        private void ReassembleFlash()
        {
            // 24px 收束闪光。
            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                Projectile.Center, Vector2.Zero, AethersWhisperVisuals.ShimmerCyan with { A = 0 }, 0.3f, 10, false, true),
                false, GeneralDrawLayer.AfterEverything);
            SoundEngine.PlaySound(SoundID.Item25 with { Volume = 0.2f, Pitch = 0.6f }, Projectile.Center);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(endPos);
            writer.WriteVector2(controlPoint);
            writer.WriteVector2(expandNormal);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            endPos = reader.ReadVector2();
            controlPoint = reader.ReadVector2();
            expandNormal = reader.ReadVector2();
        }

        private static Vector2 QuadraticBezier(Vector2 p0, Vector2 c, Vector2 p1, float t)
        {
            float u = 1f - t;
            return u * u * p0 + 2f * u * t * c + t * t * p1;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            AethersWhisperVisuals.BeginAdditive(sb);

            Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
            Vector2 boxCenter = Projectile.Size * 0.5f;

            // 曲线尾迹（冷青，逐渐变细变淡）。
            for (int i = 1; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;
                Vector2 a = Projectile.oldPos[i] + boxCenter;
                Vector2 b = Projectile.oldPos[i - 1] + boxCenter;
                float fade = (1f - i / (float)Projectile.oldPos.Length) * 0.6f;
                AethersWhisperVisuals.DrawBeamSegment(sb, a, b, AethersWhisperVisuals.ShimmerCyan with { A = 0 } * fade, 8f * (1f - i / (float)Projectile.oldPos.Length));
            }

            // 晶片本体：深紫外缘 + 珠白核心（占位程序绘制；正式请换 Assets/AetherReturnShard.png）。
            Vector2 pos = Projectile.Center - Main.screenPosition;
            sb.Draw(bloom, pos, null, AethersWhisperVisuals.AetherPurple with { A = 0 }, Projectile.rotation, bloom.Size() * 0.5f, 0.14f, SpriteEffects.None, 0f);
            sb.Draw(bloom, pos, null, AethersWhisperVisuals.PearlWhite with { A = 0 }, Projectile.rotation, bloom.Size() * 0.5f, 0.06f, SpriteEffects.None, 0f);

            AethersWhisperVisuals.EndAdditive(sb);
            return false;
        }
    }
}
