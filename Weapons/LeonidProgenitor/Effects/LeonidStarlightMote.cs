using System;
using System.Collections.Generic;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects
{
    // 星光形态：决定贴图、体积、拖尾长度与节奏。
    // 灾厄贴图库里这几张原图都很大（512²~972²），必须大幅缩小并压低不透明度才不会糊屏。
    public enum LeonidStarlightShape
    {
        Mote,   // ShineFlare 512² → 0.062：细长十字芒，最常用的小星点
        Shard,  // PulseStar  165² → 0.26 ：四芒星碎片，偏"实体"，适合流星炸开
        Halo,   // BloomFlare 972² → 0.036：放射状大耀斑，只在重击时少量使用
        Needle  // ShineFlare 512² 纵向拉伸：细针状光矢，跟随速度方向
    }

    // 「星光锁定」粒子——学自救赎「盲目正义」的 Lightmass：
    // 慢速悬停 → 极速转向锁定 → 撞击后虚化收尾。
    // 这里换成狮子座的视觉语言（层云蓝／月光紫／月白／星金），并加了几段自己的节奏：
    //   ① 点亮 Kindle  ：从零膨胀出来的一瞬爆闪，不是凭空出现
    //   ② 悬停 Drift   ：阻尼减速 + 垂直正弦摆动 + 微弱上浮，像悬在夜空里的星
    //   ③ 锁定 Lock    ：选中目标后先"往后拉弓"并张开准星环，给出预告帧
    //   ④ 突刺 Lance   ：速度由慢到快爬升，转向率反而逐渐收紧，最后一段是直线
    //   ⑤ 漂流 Wander  ：找不到目标就缓缓上浮并淡出，而不是硬生生消失
    //   ⑥ 熄灭 Quench  ：命中后本体隐形、速度归零，只留拖尾自然收缩
    // 另外还有两条横向机制：悬停期彼此连成星座连线；终结技引力场会把星光一起往下拽。
    internal sealed class LeonidStarlightMote : Particle
    {
        public override string Texture => "CalamityMod/ExtraTextures/ShineFlare";
        public override bool SetLifetime => true;
        public override bool UseAdditiveBlend => true;
        public override bool UseCustomDraw => true;

        private enum Phase
        {
            Kindle,
            Drift,
            Lock,
            Lance,
            Wander,
            Quench
        }

        private const int KindleTime = 6;
        private const int LockTime = 8;
        private const int QuenchTime = 11;

        // 悬停期互相连线用的存活登记表。每颗星光每帧盖一次帧戳，
        // 查询时顺手剔除掉超过两帧没更新的（世界卸载/粒子被强制清空时的残留）。
        private static readonly List<LeonidStarlightMote> LiveMotes = new();

        // 退出世界时粒子会被处理器整批清空，登记表要跟着清，别把引用留到下一个世界。
        internal static void ClearRegistry() => LiveMotes.Clear();

        private readonly LeonidStarlightShape shape;
        private readonly Vector2[] trail;
        private readonly Color coreColor;
        private readonly Color edgeColor;
        private readonly float baseScale;
        private readonly float shapeOpacity;
        private readonly float driftDamping;
        private readonly float wobbleStrength;
        private readonly float wobblePhase;
        private readonly float spinSpeed;
        private readonly float lanceSpeed;
        private readonly float homingRange;
        private readonly int hoverTime;
        private readonly bool alignToVelocity;
        private readonly bool linksToSiblings;

        private Phase phase = Phase.Kindle;
        private int phaseTimer;
        private int quenchTimer = -1;
        private NPC target;
        private float lockRingProgress;
        private float renderScale;
        private float flashBoost;
        private uint lastUpdateFrame;
        private Vector2 linkPoint;
        private float linkStrength;

        public LeonidStarlightMote(
            Vector2 position,
            Vector2 velocity,
            Color color,
            LeonidStarlightShape shape,
            float scaleMultiplier = 1f,
            int hoverTime = 26,
            int lifetime = 150,
            float lanceSpeed = 17f,
            float homingRange = 760f,
            bool linksToSiblings = true)
        {
            Position = position;
            Velocity = velocity;
            Lifetime = lifetime;
            this.shape = shape;
            this.hoverTime = hoverTime;
            this.lanceSpeed = lanceSpeed;
            this.homingRange = homingRange;
            this.linksToSiblings = linksToSiblings;

            coreColor = Color.Lerp(color, LeonidVisualUtils.MoonWhite, 0.35f);
            edgeColor = Color.Lerp(color, LeonidVisualUtils.NightSkyBlue, 0.3f);
            Color = coreColor;

            wobblePhase = Main.rand.NextFloat(MathHelper.TwoPi);
            Rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            switch (shape)
            {
                case LeonidStarlightShape.Shard:
                    baseScale = 0.26f * scaleMultiplier;
                    shapeOpacity = 0.95f;
                    trail = new Vector2[12];
                    driftDamping = 0.945f;
                    wobbleStrength = 0.22f;
                    spinSpeed = Main.rand.NextFloat(0.05f, 0.09f) * (Main.rand.NextBool() ? 1f : -1f);
                    alignToVelocity = false;
                    break;

                case LeonidStarlightShape.Halo:
                    // 972² 的大耀斑，缩到 0.036 并把亮度压到一半，否则一颗就吃掉半个屏幕。
                    baseScale = 0.036f * scaleMultiplier;
                    shapeOpacity = 0.5f;
                    trail = new Vector2[8];
                    driftDamping = 0.92f;
                    wobbleStrength = 0.1f;
                    spinSpeed = Main.rand.NextFloat(0.008f, 0.02f) * (Main.rand.NextBool() ? 1f : -1f);
                    alignToVelocity = false;
                    break;

                case LeonidStarlightShape.Needle:
                    baseScale = 0.05f * scaleMultiplier;
                    shapeOpacity = 0.8f;
                    trail = new Vector2[14];
                    driftDamping = 0.955f;
                    wobbleStrength = 0.3f;
                    spinSpeed = 0f;
                    alignToVelocity = true;
                    break;

                default: // Mote
                    baseScale = 0.062f * scaleMultiplier;
                    shapeOpacity = 0.85f;
                    trail = new Vector2[10];
                    driftDamping = 0.95f;
                    wobbleStrength = 0.26f;
                    spinSpeed = Main.rand.NextFloat(0.012f, 0.03f) * (Main.rand.NextBool() ? 1f : -1f);
                    alignToVelocity = false;
                    break;
            }

            for (int i = 0; i < trail.Length; i++)
                trail[i] = position;

            renderScale = baseScale * 0.2f;
            Scale = baseScale;

            if (linksToSiblings)
                LiveMotes.Add(this);
        }

        // ── 每帧更新 ───────────────────────────────────────────────
        public override void Update()
        {
            lastUpdateFrame = Main.GameUpdateCount;

            for (int i = trail.Length - 1; i > 0; i--)
                trail[i] = trail[i - 1];
            trail[0] = Position;

            phaseTimer++;
            Rotation += spinSpeed;

            switch (phase)
            {
                case Phase.Kindle:
                    UpdateKindle();
                    break;
                case Phase.Drift:
                    UpdateDrift();
                    break;
                case Phase.Lock:
                    UpdateLock();
                    break;
                case Phase.Lance:
                    UpdateLance();
                    break;
                case Phase.Wander:
                    UpdateWander();
                    break;
                case Phase.Quench:
                    UpdateQuench();
                    break;
            }

            if (alignToVelocity && Velocity.LengthSquared() > 0.05f)
                Rotation = Velocity.ToRotation() + MathHelper.PiOver2;

            flashBoost *= 0.82f;
            Color = Color.Lerp(edgeColor, coreColor, 0.5f + 0.5f * flashBoost);

            // 一次爆发可能有上百颗星光同时在场，单颗的打光要压得很轻，
            // 否则整片区域会被洗白。熄灭中的不再发光。
            if (phase != Phase.Quench)
                Lighting.AddLight(Position, coreColor.ToVector3() * 0.075f * (renderScale / MathF.Max(baseScale, 0.001f)));

            if (Time >= Lifetime - 1)
                LiveMotes.Remove(this);
        }

        private void UpdateKindle()
        {
            // 从一个亮点炸开成完整星光，同时保留初速的大部分。
            float t = phaseTimer / (float)KindleTime;
            renderScale = MathHelper.Lerp(baseScale * 0.2f, baseScale * 1.35f, MathF.Sin(t * MathHelper.PiOver2));
            flashBoost = 1f;
            Velocity *= 0.99f;

            if (phaseTimer >= KindleTime)
                EnterPhase(Phase.Drift);
        }

        private void UpdateDrift()
        {
            // 阻尼减速造成"悬在夜空里"的静谧感，再叠一层垂直正弦摆动和微弱上浮，
            // 让它是"浮着"而不是单纯"停下来"。
            Velocity *= driftDamping;

            Vector2 sway = Velocity.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2);
            Velocity += sway * MathF.Sin(Time * 0.28f + wobblePhase) * wobbleStrength;
            Velocity.Y -= 0.024f;

            ApplyGravityWell();

            renderScale = MathHelper.Lerp(renderScale, baseScale, 0.18f);

            if (linksToSiblings && Time % 6 == 0)
                RefreshConstellationLink();
            linkStrength *= 0.94f;

            if (Main.rand.NextBool(14))
                LeonidVisualUtils.SpawnStratusDust(Position, Velocity, 0.42f, 140);

            if (phaseTimer >= hoverTime)
            {
                target = FindTarget();
                EnterPhase(target != null ? Phase.Lock : Phase.Wander);
            }
        }

        private void UpdateLock()
        {
            // 拉弓帧：向目标反方向后撤一点，同时准星环收拢、体积涨大。
            // 这段预告让后面的突刺有"蓄势—释放"的重量感，而不是原地瞬移。
            if (!TargetValid())
            {
                target = FindTarget();
                if (target == null)
                {
                    EnterPhase(Phase.Wander);
                    return;
                }
            }

            float t = phaseTimer / (float)LockTime;
            lockRingProgress = t;

            Vector2 away = (Position - target.Center).SafeNormalize(Vector2.UnitY);
            Velocity = Vector2.Lerp(Velocity, away * 2.6f * (1f - t), 0.3f);

            renderScale = MathHelper.Lerp(baseScale, baseScale * 1.5f, t);

            if (phaseTimer >= LockTime)
            {
                flashBoost = 1f;
                EnterPhase(Phase.Lance);
            }
        }

        private void UpdateLance()
        {
            if (!TargetValid())
            {
                target = FindTarget();
                if (target == null)
                {
                    EnterPhase(Phase.Wander);
                    return;
                }
            }

            // 速度从 0.6 倍爬到 1.55 倍，转向率却从 0.16 收到 0.045——
            // 起手灵活、末段笔直，画出的是一条渐渐拉直的弧线。
            float ramp = Utils.GetLerpValue(0f, 26f, phaseTimer, true);
            float speed = lanceSpeed * MathHelper.Lerp(0.6f, 1.55f, ramp);
            float turnRate = MathHelper.Lerp(0.16f, 0.045f, ramp);

            Vector2 desired = (target.Center - Position).SafeNormalize(Vector2.UnitY) * speed;
            Velocity = Vector2.Lerp(Velocity, desired, turnRate);

            ApplyGravityWell();

            renderScale = MathHelper.Lerp(renderScale, baseScale * 1.15f, 0.2f);
            lockRingProgress *= 0.8f;

            // 末段速度能到 26 像素/帧，只测当前点会直接穿过小怪的判定框，
            // 所以拿"上一帧位置 → 当前位置"这条线段做扫掠检测。trail[1] 就是上一帧的位置。
            if (Collision.CheckAABBvLineCollision(target.position, target.Size, trail[1], Position))
                Quench();
            else if (Collision.SolidCollision(Position - new Vector2(3f), 6, 6))
                Quench();
        }

        private void UpdateWander()
        {
            // 没有目标：不硬删，改为缓缓上浮、微微转向并淡出，收尾干净。
            Velocity *= 0.985f;
            Velocity.Y -= 0.03f;
            Velocity = Velocity.RotatedBy(MathF.Sin(Time * 0.05f + wobblePhase) * 0.02f);

            ApplyGravityWell();

            float remaining = Utils.GetLerpValue(Lifetime, Lifetime - 40f, Time, true);
            renderScale = baseScale * remaining;

            if (linksToSiblings && Time % 10 == 0)
                RefreshConstellationLink();
            linkStrength *= 0.94f;

            // 目标可能中途出现，给它一次改命的机会。
            if (Time % 20 == 0)
            {
                target = FindTarget();
                if (target != null)
                    EnterPhase(Phase.Lock);
            }
        }

        private void UpdateQuench()
        {
            // 伪死亡：本体隐形、速度归零，只留拖尾自然收缩。
            Velocity = Vector2.Zero;
            quenchTimer--;
            linkStrength = 0f;

            if (quenchTimer <= 0)
                Lifetime = Time;
        }

        private void EnterPhase(Phase next)
        {
            phase = next;
            phaseTimer = 0;
        }

        // 命中/撞墙时触发：不瞬间蒸发，而是留几帧让拖尾收拢，同时炸出一小圈星尘。
        private void Quench()
        {
            if (phase == Phase.Quench)
                return;

            EnterPhase(Phase.Quench);
            quenchTimer = QuenchTime;
            Lifetime = Math.Max(Lifetime, Time + QuenchTime + 1);
            LiveMotes.Remove(this);

            int dustCount = shape == LeonidStarlightShape.Halo ? 9 : 5;
            for (int i = 0; i < dustCount; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Position,
                    Main.rand.NextBool(3) ? DustID.Electric : DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(2.6f, 2.6f),
                    120,
                    Color.Lerp(coreColor, LeonidVisualUtils.StarGold, Main.rand.NextFloat(0.45f)),
                    Main.rand.NextFloat(0.5f, 0.95f));
                dust.noGravity = true;
            }
        }

        // ── 辅助 ───────────────────────────────────────────────────
        private bool TargetValid() => target != null && target.active && !target.dontTakeDamage && target.CanBeChasedBy();

        private NPC FindTarget()
        {
            NPC best = null;
            float sqrRange = homingRange * homingRange;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || npc.dontTakeDamage)
                    continue;

                float sqrDistance = Vector2.DistanceSquared(npc.Center, Position);
                if (sqrDistance > sqrRange)
                    continue;

                sqrRange = sqrDistance;
                best = npc;
            }

            return best;
        }

        // 终结技引力场展开时，连星光也会被一起拽下去——特效跟着武器机制走。
        private void ApplyGravityWell()
        {
            if (!LeonidStarlight.TryGetGravityWell(out Vector2 center, out float strength))
                return;

            float dx = MathF.Abs(Position.X - center.X);
            float dy = Position.Y - center.Y;
            if (dx < 600f && dy > -900f && dy < 300f)
                Velocity.Y += 0.12f * strength;
        }

        // 悬停期在最近的同伴之间连一条极淡的星座连线。
        private void RefreshConstellationLink()
        {
            float bestSqr = 150f * 150f;
            LeonidStarlightMote best = null;

            for (int i = LiveMotes.Count - 1; i >= 0; i--)
            {
                LeonidStarlightMote other = LiveMotes[i];

                // 帧戳过期 = 粒子已被处理器移除，清掉残留登记。
                if (other == null || Main.GameUpdateCount - other.lastUpdateFrame > 2)
                {
                    LiveMotes.RemoveAt(i);
                    continue;
                }

                if (ReferenceEquals(other, this) || other.phase == Phase.Quench)
                    continue;

                float sqrDistance = Vector2.DistanceSquared(other.Position, Position);
                if (sqrDistance > bestSqr)
                    continue;

                bestSqr = sqrDistance;
                best = other;
            }

            if (best == null)
            {
                linkStrength = 0f;
                return;
            }

            linkPoint = best.Position;
            linkStrength = 1f - MathF.Sqrt(bestSqr) / 150f;
        }

        // ── 绘制 ───────────────────────────────────────────────────
        // 处理器已经用 BlendState.Additive 开好了批次，所以这里保留 alpha
        // 并整体乘不透明度（Calamity 加法粒子的标准写法）。
        public override void CustomDraw(SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>(ShapeTexture(shape)).Value;
            Texture2D pinpoint = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/BloomCirclePinpoint").Value;
            Vector2 origin = texture.Size() * 0.5f;

            float lifeFade = Utils.GetLerpValue(Lifetime, Lifetime - 26f, Time, true);
            float quenchFade = phase == Phase.Quench ? quenchTimer / (float)QuenchTime : 1f;
            float opacity = shapeOpacity * lifeFade * quenchFade;
            if (opacity <= 0.002f)
                return;

            DrawConstellationLink(spriteBatch, opacity);

            // ① 历史拖尾：越老越暗、越细，并带一点扭转，像被拉长的星芒。
            for (int i = trail.Length - 1; i >= 1; i--)
            {
                float t = 1f - i / (float)trail.Length;
                float fade = t * t;
                Vector2 drawPosition = trail[i] - Main.screenPosition;
                Color trailColor = Color.Lerp(edgeColor, coreColor, t) * (opacity * 0.5f * fade);

                spriteBatch.Draw(
                    texture,
                    drawPosition,
                    null,
                    trailColor,
                    Rotation + i * 0.16f,
                    origin,
                    ShapeScale(renderScale * (0.3f + 0.7f * t)),
                    SpriteEffects.None,
                    0f);
            }

            if (phase == Phase.Quench)
                return;

            Vector2 headPosition = Position - Main.screenPosition;

            // ② 准星环：只在拉弓帧出现，由大收小，给突刺一个预告。
            if (lockRingProgress > 0.02f)
            {
                Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/FadedStarRing").Value;
                float ringScale = MathHelper.Lerp(0.3f, 0.085f, lockRingProgress) * (baseScale / 0.062f);
                spriteBatch.Draw(
                    ring,
                    headPosition,
                    null,
                    LeonidVisualUtils.StarGold * (0.55f * lockRingProgress * opacity),
                    -Rotation * 1.6f,
                    ring.Size() * 0.5f,
                    ringScale,
                    SpriteEffects.None,
                    0f);
            }

            // ③ 星体本身：柔光底 + 主贴图 + 月白高光核心。
            spriteBatch.Draw(
                pinpoint,
                headPosition,
                null,
                edgeColor * (0.4f * opacity),
                0f,
                pinpoint.Size() * 0.5f,
                renderScale * 1.9f + 0.02f,
                SpriteEffects.None,
                0f);

            spriteBatch.Draw(
                texture,
                headPosition,
                null,
                Color * (opacity * (0.85f + 0.35f * flashBoost)),
                Rotation,
                origin,
                ShapeScale(renderScale),
                SpriteEffects.None,
                0f);

            spriteBatch.Draw(
                texture,
                headPosition,
                null,
                LeonidVisualUtils.MoonWhite * (opacity * 0.6f),
                Rotation,
                origin,
                ShapeScale(renderScale * 0.45f),
                SpriteEffects.None,
                0f);
        }

        private void DrawConstellationLink(SpriteBatch spriteBatch, float opacity)
        {
            if (linkStrength <= 0.03f)
                return;

            Vector2 delta = linkPoint - Position;
            float length = delta.Length();
            if (length < 1f)
                return;

            spriteBatch.Draw(
                TextureAssets.MagicPixel.Value,
                Position - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                Color.Lerp(edgeColor, LeonidVisualUtils.MoonWhite, 0.4f) * (0.16f * linkStrength * opacity),
                delta.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(length, 1.4f),
                SpriteEffects.None,
                0f);
        }

        private Vector2 ShapeScale(float scale) =>
            shape == LeonidStarlightShape.Needle ? new Vector2(scale * 0.32f, scale * 1.75f) : new Vector2(scale);

        private static string ShapeTexture(LeonidStarlightShape shape) => shape switch
        {
            LeonidStarlightShape.Shard => "CalamityMod/Particles/PulseStar",
            LeonidStarlightShape.Halo => "CalamityMod/ExtraTextures/BloomFlare",
            _ => "CalamityMod/ExtraTextures/ShineFlare"
        };
    }
}
