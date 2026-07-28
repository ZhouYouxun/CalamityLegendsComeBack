using System;
using System.IO;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

using CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.Shared;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AethersWhisper.RightClick
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

        private Vector2 startPos;         // 枪口出生点
        private float rangeBudget;        // 剩余射程
        private bool reflected;
        private Vector2 reflectPos;
        private Vector2 reflectNormal;
        private int reflectAge = -1;      // 反射后计时（画折射标记 8 tick）
        private bool disassembled;

        public override void SetStaticDefaults()
        {
            // 伪激光弹体的“粗光痕”用历史位置缓存做 L3 图元流光。
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

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
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, AethersWhisperVisuals.ShimmerCyan.ToVector3() * 0.5f);

            if (reflectAge >= 0)
                reflectAge++;

            float step = Projectile.velocity.Length();

            // 直接飞行：不再飞向鼠标点/命中鼠标，鼠标很近就直接穿过去。
            // 结束条件只有：首个敌人命中 / 第二次墙碰撞 / 射程耗尽。
            HandleTileCollision();

            // 射程耗尽即分解。
            rangeBudget -= step;
            if (rangeBudget <= 0f)
            {
                Disassemble(Projectile.Center);
                return;
            }

            if (!Main.dedServ)
            {
                // 电弧尘 + 军械库 DualTrail 光条，让粗光痕更有能量密度。
                if (Main.rand.NextBool())
                {
                    Dust d = Dust.NewDustPerfect(Projectile.Center, AethersWhisperVisuals.ElectricDust,
                        Projectile.velocity * -0.02f, 120, AethersWhisperVisuals.ShimmerCyan, Main.rand.NextFloat(0.8f, 1.2f));
                    d.noGravity = true;
                }
                if (Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new CustomSpark(Projectile.Center, Projectile.velocity * 0.05f,
                        AethersWhisperVisuals.DualTrailTex, false, Main.rand.Next(9, 14), 0.06f,
                        AethersWhisperVisuals.Lerp(Main.rand.NextFloat()), new Vector2(0.7f, 1.5f), true, false, shrinkSpeed: 0.25f));
                }
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
                // 折射铃：沿墙面法线弹出的电弧尘 + 硬光方块，表现「这一束折过了」。
                GeneralParticleHandler.SpawnParticle(new CustomSpark(reflectPos, reflectNormal * 2f,
                    "CalamityMod/Particles/BloomCircle", false, 12, 0.1f, AethersWhisperVisuals.ShimmerCyan,
                    new Vector2(0.7f, 1.4f), true, true, glowCenterScale: 0.7f, shrinkSpeed: 0.2f));
                for (int i = 0; i < 6; i++)
                {
                    Color c = AethersWhisperVisuals.ToWhite(AethersWhisperVisuals.ShimmerCyan, Main.rand.NextFloat(0.5f));
                    Dust d = Dust.NewDustPerfect(reflectPos, AethersWhisperVisuals.ElectricDust,
                        reflectNormal.RotatedByRandom(0.6f) * Main.rand.NextFloat(1.5f, 4f), 80, c, Main.rand.NextFloat(0.9f, 1.4f));
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
            // L3 图元带：粗冷青外层 + 细珠白核心的双通道流光拖尾——伪激光弹体的“粗光痕”（文档 4.5）。
            // smoothen:false → 反射处保留真实折角，不被样条抹圆；OffsetFunction 恒为零（禁止叠 screenPosition）。
            DrawShaderTrail();

            SpriteBatch sb = Main.spriteBatch;
            AethersWhisperVisuals.BeginAdditive(sb);

            // 头部珠白核心 bloom。
            Texture2D bloom = AethersWhisperVisuals.BloomCircle.Value;
            sb.Draw(bloom, Projectile.Center - Main.screenPosition, null, AethersWhisperVisuals.PearlWhite with { A = 0 },
                0f, bloom.Size() * 0.5f, AethersWhisperBalance.BeamVisualWidth / bloom.Width, SpriteEffects.None, 0f);

            // 折射标记：反射后只持续 8 tick 的薄折射环（复用 HollowCircleHardEdge 空心环）；
            // 一出现即最亮、cos 爆发淡出。
            if (reflected && reflectAge >= 0 && reflectAge < 8)
            {
                float p = AethersWhisperVisuals.BurstFade(reflectAge / 8f);
                AethersWhisperVisuals.DrawShimmerRing(sb, reflectPos, 26f, reflectNormal.ToRotation(), p);
            }

            AethersWhisperVisuals.EndAdditive(sb);
            return false;
        }

        private void DrawShaderTrail()
        {
            Vector2[] points = BuildTrailPoints();
            if (points.Length < 3)
                return;

            MiscShaderData shader = GameShaders.Misc["CalamityMod:TrailStreak"];
            var streak = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail");

            // 粗冷青外层（可见体明显粗于碰撞）。
            shader.SetShaderTexture(streak);
            PrimitiveRenderer.RenderTrail(points,
                new PrimitiveSettings(OuterWidth, OuterColor, (_, _) => Vector2.Zero,
                    smoothen: false, pixelate: false, shader: shader),
                points.Length * 2);

            // 细珠白核心（双绘制的窄核心）。
            shader.SetShaderTexture(streak);
            PrimitiveRenderer.RenderTrail(points,
                new PrimitiveSettings(CoreWidth, CoreColor, (_, _) => Vector2.Zero,
                    smoothen: false, pixelate: false, shader: shader),
                points.Length * 2);
        }

        // 用历史位置构造拖尾控制点：去掉零点、合并过近点，至少 3 个（反射折角靠这些点保留）。
        private Vector2[] BuildTrailPoints()
        {
            Vector2[] points = new Vector2[Projectile.oldPos.Length + 3];
            int count = 0;
            points[count++] = Projectile.Center;

            Vector2 last = Projectile.Center;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;
                Vector2 point = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (Vector2.DistanceSquared(point, last) < 4f)
                    continue;
                points[count++] = point;
                last = point;
            }

            while (count < 3)
            {
                points[count] = Projectile.Center - Projectile.velocity * count;
                count++;
            }

            Array.Resize(ref points, count);
            return points;
        }

        private static float OuterWidth(float completion, Vector2 _) => TaperWidth(completion, AethersWhisperBalance.BeamVisualWidth);
        private static float CoreWidth(float completion, Vector2 _) => TaperWidth(completion, AethersWhisperBalance.BeamVisualWidth * 0.32f);

        private static Color OuterColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.35f, 1f, completion, true);
            return AethersWhisperVisuals.ShimmerCyan * (tailFade * 0.55f);
        }

        private static Color CoreColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.55f, 1f, completion, true);
            return AethersWhisperVisuals.PearlWhite * tailFade;
        }

        // 头部快速起宽、尾部按幂次收细——头粗尾尖的梭形光痕。
        private static float TaperWidth(float completion, float maxWidth)
        {
            const float headFraction = 0.15f;
            if (completion < headFraction)
                return MathHelper.SmoothStep(maxWidth * 0.35f, maxWidth, completion / headFraction);
            float tail = (completion - headFraction) / (1f - headFraction);
            return maxWidth * MathF.Pow(1f - tail, 0.6f);
        }
    }
}
