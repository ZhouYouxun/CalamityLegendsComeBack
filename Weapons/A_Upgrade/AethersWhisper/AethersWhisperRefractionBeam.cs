using System;
using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper
{
    /// <summary>
    /// 右键的单束微光折射伪激光（文档第 4.2 节）。
    /// 出生时锁定本束鼠标世界坐标；向前飞行，可借实体砖块真实反射一次（保留 55% 剩余射程）。
    /// 到达锁定准星点 / 首个敌人 / 第二次墙碰撞 / 射程耗尽 → 立即在终点分解为一对回收晶片。
    /// 禁止自动索敌；反射只来自真实碰撞法线。
    /// </summary>
    internal sealed class AethersWhisperRefractionBeam : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AethersWhisper";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Vector2 AimWorld => new(Projectile.ai[0], Projectile.ai[1]);

        private Vector2 startPos;         // 枪口出生点
        private float rangeBudget;        // 剩余射程
        private float mouseDistLimit;     // 到锁定准星点的距离（仅反射前有效）
        private bool reflected;
        private Vector2 reflectPos;
        private Vector2 reflectNormal;
        private int reflectAge = -1;      // 反射后计时（画折射标记 8 tick）
        private bool disassembled;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.penetrate = 1;                 // 首个敌人即终止
            Projectile.tileCollide = false;           // 手动检测砖块以精确控制“反射一次 / 第二次分解”
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 300;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(IEntitySource source)
        {
            startPos = Projectile.Center;
            rangeBudget = AethersWhisperBalance.BeamMaxRange;
            mouseDistLimit = MathF.Min(Vector2.Distance(startPos, AimWorld), AethersWhisperBalance.BeamMaxRange);
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, AethersWhisperVisuals.ShimmerCyan.ToVector3() * 0.5f);

            if (reflectAge >= 0)
                reflectAge++;

            float step = Projectile.velocity.Length();

            // 反射前：到达锁定准星点即空点分解（“打鼠标位置”有真实用途）。
            if (!reflected && Vector2.Distance(startPos, Projectile.Center) >= mouseDistLimit)
            {
                Disassemble(Projectile.Center);
                return;
            }

            // 手动砖块检测与一次反射。
            HandleTileCollision();

            // 射程耗尽即分解。
            rangeBudget -= step;
            if (rangeBudget <= 0f)
            {
                Disassemble(Projectile.Center);
                return;
            }

            if (!Main.dedServ && Main.rand.NextBool())
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                    Projectile.velocity * -0.02f, 120, AethersWhisperVisuals.ShimmerCyan, Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }
        }

        private void HandleTileCollision()
        {
            const int probe = 6;
            Vector2 half = new(probe * 0.5f);
            Vector2 vel = Projectile.velocity;
            Vector2 next = Projectile.Center + vel;

            if (!Collision.SolidCollision(next - half, probe, probe))
                return;

            if (reflected)
            {
                // 第二次撞墙：在碰撞点分解。
                Disassemble(Projectile.Center);
                return;
            }

            // 第一次撞墙：按真实法线反射一次，剩余射程保留 55%，反射点不造成直接伤害。
            bool xBlocked = Collision.SolidCollision(new Vector2(next.X, Projectile.Center.Y) - half, probe, probe);
            bool yBlocked = Collision.SolidCollision(new Vector2(Projectile.Center.X, next.Y) - half, probe, probe);

            Vector2 normal;
            if (xBlocked && !yBlocked)
                normal = new Vector2(-MathF.Sign(vel.X), 0f);
            else if (yBlocked && !xBlocked)
                normal = new Vector2(0f, -MathF.Sign(vel.Y));
            else
                normal = new Vector2(-MathF.Sign(vel.X), -MathF.Sign(vel.Y)).SafeNormalize(-vel.SafeNormalize(Vector2.UnitX));

            reflected = true;
            reflectPos = Projectile.Center;
            reflectNormal = normal.SafeNormalize(Vector2.UnitY);
            reflectAge = 0;
            Projectile.velocity = Vector2.Reflect(vel, reflectNormal);
            rangeBudget *= AethersWhisperBalance.BeamReflectRangeRetain;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item27 with { Volume = 0.35f, Pitch = 0.5f }, reflectPos);
            if (!Main.dedServ)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust d = Dust.NewDustPerfect(reflectPos, DustID.PurpleTorch,
                        reflectNormal.RotatedByRandom(0.6f) * Main.rand.NextFloat(1.5f, 4f), 80, AethersWhisperVisuals.ShimmerCyan, Main.rand.NextFloat(0.9f, 1.4f));
                    d.noGravity = true;
                }
            }
        }

        // 线段首碰（宽度 16，明显细于 32 的可见体）。
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 start = Projectile.Center - Projectile.velocity;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                start, Projectile.Center, AethersWhisperBalance.BeamHitWidth, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 首个敌人受直接伤害，该束立即在其中心开始分解。
            Disassemble(target.Center);
        }

        private void Disassemble(Vector2 endPos)
        {
            if (disassembled)
                return;
            disassembled = true;

            SpawnShards(endPos);
            SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.35f, Pitch = 0.4f }, endPos);
            Projectile.Kill();
        }

        private void SpawnShards(Vector2 endPos)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 endDir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 endNormal = endDir.RotatedBy(MathHelper.PiOver2); // 主束末段法线
            int groupId = Projectile.identity;                        // 一对晶片共享的 returnGroupId
            int weaponDamage = Main.player[Projectile.owner].GetWeaponDamage(Main.player[Projectile.owner].HeldItem);
            int shardDamage = Math.Max(1, (int)(weaponDamage * AethersWhisperBalance.ShardReturnDamageMult));

            for (int side = -1; side <= 1; side += 2)
            {
                // 控制点（文档 4.3 固定镜像公式）：
                Vector2 control = reflected
                    ? endPos + reflectNormal * AethersWhisperBalance.ShardControlReflectWall + endNormal * side * AethersWhisperBalance.ShardControlReflectNormal
                    : endPos + endNormal * side * AethersWhisperBalance.ShardControlNoReflectNormal;

                int idx = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    endPos,
                    Vector2.Zero,
                    ModContent.ProjectileType<AethersWhisperReturnShard>(),
                    shardDamage,
                    AethersWhisperBalance.KnockBack,
                    Projectile.owner,
                    groupId,
                    side);

                if (Main.projectile.IndexInRange(idx) && Main.projectile[idx].ModProjectile is AethersWhisperReturnShard shard)
                {
                    shard.Setup(endPos, control, endNormal * side);
                    Main.projectile[idx].netUpdate = true;
                }
            }
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(startPos);
            writer.Write(reflected);
            writer.WriteVector2(reflectPos);
            writer.WriteVector2(reflectNormal);
            writer.Write(rangeBudget);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            startPos = reader.ReadVector2();
            reflected = reader.ReadBoolean();
            reflectPos = reader.ReadVector2();
            reflectNormal = reader.ReadVector2();
            rangeBudget = reader.ReadSingle();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            SpriteBatch sb = Main.spriteBatch;
            AethersWhisperVisuals.BeginAdditive(sb);

            Vector2 head = Projectile.Center;
            if (reflected)
            {
                DrawBeam(sb, startPos, reflectPos);
                DrawBeam(sb, reflectPos, head);

                // 折射标记：反射后只持续 8 tick 的薄六边形折射环（占位：HollowCircleHardEdge；
                // 正式请换 Assets/AetherReflectionGlyph.png）。
                if (reflectAge >= 0 && reflectAge < 8)
                {
                    float p = 1f - reflectAge / 8f;
                    AethersWhisperVisuals.DrawShimmerRing(sb, reflectPos, 26f, reflectNormal.ToRotation(), p);
                }
            }
            else
            {
                DrawBeam(sb, startPos, head);
            }

            // 头部珠白核心。
            Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
            sb.Draw(bloom, head - Main.screenPosition, null, AethersWhisperVisuals.PearlWhite with { A = 0 },
                0f, bloom.Size() * 0.5f, AethersWhisperBalance.BeamVisualWidth / bloom.Width, SpriteEffects.None, 0f);

            AethersWhisperVisuals.EndAdditive(sb);
            return false;
        }

        private static void DrawBeam(SpriteBatch sb, Vector2 from, Vector2 to)
        {
            // 冷青粗外层 + 珠白细核心（可见体明显粗于碰撞）。
            AethersWhisperVisuals.DrawBeamSegment(sb, from, to, AethersWhisperVisuals.ShimmerCyan with { A = 0 }, AethersWhisperBalance.BeamVisualWidth);
            AethersWhisperVisuals.DrawBeamSegment(sb, from, to, AethersWhisperVisuals.PearlWhite with { A = 0 }, AethersWhisperBalance.BeamVisualWidth * 0.28f);
        }
    }
}
