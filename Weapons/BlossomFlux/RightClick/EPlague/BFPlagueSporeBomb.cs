using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    // 瘟疫形态右键蓄力体：在准星前方凝聚一颗孢子球，松手甩出，撞击或到时引爆成两圈瘟疫箭 + 三层疫爆。
    // 结构对标狞桀的蓄力球（图元圆环 + 甩出 + 环形散射），粒子语汇与配色沿用叶流自己的一套。
    internal class BFPlagueSporeBomb : ModProjectile, IPixelatedPrimitiveRenderer
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";

        // 球本体不画贴图，看到的全是图元圆环和辉光。
        public override string Texture => "CalamityMod/Projectiles/Ranged/SporeBomb";

        // 画在 NPC 之前，让怪物压在环上，读起来才像一个「场」而不是贴片。
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeNPCs;

        #region 常量

        // 蓄力最低 / 最满时的环半径。
        private const float MinRadius = 13.2f;
        private const float MaxRadius = 70.8f;

        // 甩出后的飞行速度与总飞行时长。
        private const float LaunchSpeed = 34f;
        private const int FlightFrames = 52;
        private const int HomingDelayFrames = 8;
        private const float HomingRange = 960f;
        private const float HomingResponsiveness = 0.11f;
        private const int DirectGasCount = 5;

        // 命中敌人后强制引爆的倒计时，留几帧让收束动画走完。
        private const int HitDetonateDelay = 9;

        // 引爆前的收束阶段长度：环急速缩小、球减速，给爆炸一个明确的「吸气」。
        private const int CollapseFrames = 9;

        #endregion

        #region 配色（全部取自叶流瘟疫形态）

        private static Color PlagueMain => ChloroplastCommon.PresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
        private static Color PlagueAccent => ChloroplastCommon.PresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
        private static readonly Color PlagueAcid = new(198, 255, 78);
        private static readonly Color PlagueDeep = new(18, 92, 30);

        #endregion

        #region 状态

        // 0 = 蓄力悬停，1 = 已甩出飞行中。
        private ref float State => ref Projectile.ai[0];

        // 蓄力完成度 0~1，由持握弹幕每帧写入。
        private ref float ChargePower => ref Projectile.ai[1];

        private float radius;
        private int flightTimer;
        private int detonateCountdown = -1;
        private bool detonated;

        // 蓄力锚点：持握弹幕每帧把自己的中心推过来，球就贴着武器聚，不再往准星方向甩出去。
        private Vector2 hoverAnchor;
        private bool hasHoverAnchor;

        private bool Charging => State == 0f;

        #endregion

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 52;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;

            // 蓄力期间由持握弹幕每帧续命；持握一没，球自己在半秒内散掉。
            Projectile.timeLeft = 30;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            radius = MinRadius;
            SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjAction, Projectile.Center);
        }

        // 只有甩出去、且还没进入收束尾巴时才吃伤害判定。
        public override bool? CanDamage() => !Charging && detonateCountdown != 0 ? null : false;

        public override void AI()
        {
            if (Charging)
            {
                UpdateHover();
                EmitChargeFX();
                return;
            }

            UpdateFlight();
            EmitFlightFX();
        }

        #region 蓄力悬停

        private void UpdateHover()
        {
            Player owner = Main.player[Projectile.owner];

            // 锚点来自持握弹幕正中心；拿不到就退回玩家身上，绝不自己往准星方向偏。
            Projectile.Center = hasHoverAnchor ? hoverAnchor : owner.MountedCenter;

            float target = MathHelper.Lerp(MinRadius, MaxRadius, MathHelper.Clamp(ChargePower, 0f, 1f));
            radius = MathHelper.Lerp(radius, target, 0.14f);

            Lighting.AddLight(Projectile.Center, PlagueMain.ToVector3() * (0.35f + 0.45f * ChargePower));
        }

        // 蓄力表现：四周孢子向环内收拢，环上跑光点，蓄满后额外冒毒雾。
        private void EmitChargeFX()
        {
            if (Main.dedServ)
                return;

            float power = MathHelper.Clamp(ChargePower, 0f, 1f);

            // 从环外往中心收拢的孢子，蓄得越满吸得越急——球贴在武器上，所以是「向内聚」而不是「从手里流出去」。
            if (Main.rand.NextFloat() < 0.35f + power * 0.55f)
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                float distance = radius * Main.rand.NextFloat(1.6f, 2.9f);
                Vector2 from = Projectile.Center + angle.ToRotationVector2() * distance;
                Vector2 inward = (Projectile.Center - from).SafeNormalize(Vector2.UnitX);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    from,
                    inward.RotatedByRandom(0.3f) * (distance / MathHelper.Lerp(14f, 8f, power)),
                    false, Main.rand.Next(8, 13),
                    Main.rand.NextFloat(0.2f, 0.36f),
                    Color.Lerp(PlagueMain, PlagueAccent, Main.rand.NextFloat(0.1f, 0.5f)),
                    true, false, true));
            }

            // 环上滚动的孢子颗粒，让圆环不只是一条几何线。
            if (Main.rand.NextBool(2))
            {
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 onRing = Projectile.Center + angle.ToRotationVector2() * radius;
                Dust spore = Dust.NewDustPerfect(
                    onRing,
                    DustID.GreenTorch,
                    angle.ToRotationVector2().RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(0.6f, 2.2f),
                    100,
                    Color.Lerp(PlagueMain, PlagueAcid, Main.rand.NextFloat(0.2f, 0.8f)),
                    Main.rand.NextFloat(0.7f, 1.15f) * (0.6f + power * 0.6f));
                spore.noGravity = true;
            }

            // 蓄满后环内开始翻涌毒雾，作为「可以放了」的视觉信号。
            if (power >= 0.99f && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(radius * 0.55f, radius * 0.55f),
                    Main.rand.NextVector2Circular(0.8f, 0.8f) + new Vector2(0f, -0.25f),
                    Color.Lerp(PlagueMain, PlagueDeep, 0.35f),
                    Color.Lerp(Color.Black, PlagueMain, 0.16f),
                    Main.rand.NextFloat(0.3f, 0.58f),
                    Main.rand.Next(40, 70),
                    Main.rand.NextFloat(0.006f, 0.016f)));
            }
        }

        #endregion

        #region 飞行与引爆

        private void UpdateFlight()
        {
            flightTimer++;

            if (detonateCountdown < 0 && flightTimer >= FlightFrames)
                detonateCountdown = CollapseFrames;

            if (detonateCountdown >= 0)
            {
                // 收束：环急速内缩、球明显减速，把注意力钉在爆心上。
                radius = MathHelper.Lerp(radius, 7.8f, 0.26f);
                Projectile.velocity *= 0.86f;
                detonateCountdown--;

                if (detonateCountdown < 0)
                {
                    Detonate();
                    return;
                }
            }
            else
            {
                radius = MathHelper.Lerp(radius, MaxRadius * 0.92f, 0.08f);
                HomeTowardsTarget();
            }

            Lighting.AddLight(Projectile.Center, PlagueMain.ToVector3() * 0.75f);
        }

        private void HomeTowardsTarget()
        {
            if (flightTimer < HomingDelayFrames)
                return;

            NPC target = FindHomingTarget();
            if (target is null)
                return;

            float speed = Math.Max(Projectile.velocity.Length(), LaunchSpeed);
            Vector2 desiredVelocity = (target.Center + target.velocity * 5f - Projectile.Center)
                .SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, HomingResponsiveness);
        }

        private NPC FindHomingTarget()
        {
            NPC bestTarget = null;
            float bestDistance = HomingRange;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc.friendly || npc.dontTakeDamage || npc.life <= 0)
                    continue;

                if (!Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void EmitFlightFX()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            // 拖在球后面的毒雾尾。
            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(10f, 46f) + Main.rand.NextVector2Circular(20f, 20f),
                    -forward * Main.rand.NextFloat(0.4f, 1.6f) + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    Color.Lerp(PlagueMain, PlagueAcid, 0.4f),
                    Color.Lerp(Color.Black, PlagueDeep, 0.4f),
                    Main.rand.NextFloat(0.4f, 0.8f),
                    Main.rand.Next(34, 58),
                    Main.rand.NextFloat(0.008f, 0.02f)));
            }

            // 沿环甩出的酸液光条，转速跟着环走。
        }

        // 环形判定：整个环都是杀伤范围，而不只是中心那 8 像素。
        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CircleIntersectsRect(Projectile.Center, radius * 0.78f, targetHitbox);
        }

        private static bool CircleIntersectsRect(Vector2 circleCenter, float circleRadius, Rectangle rect)
        {
            float nearestX = MathHelper.Clamp(circleCenter.X, rect.Left, rect.Right);
            float nearestY = MathHelper.Clamp(circleCenter.Y, rect.Top, rect.Bottom);
            return Vector2.DistanceSquared(circleCenter, new Vector2(nearestX, nearestY)) <= circleRadius * circleRadius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 直击上「疫亡」——这是瘟疫形态最重的那一档。
            target.GetGlobalNPC<BFPlaguePollutionNPC>().ApplyDeath(target);

            if (detonateCountdown < 0 || detonateCountdown > HitDetonateDelay)
                detonateCountdown = HitDetonateDelay;
        }

        private void Detonate()
        {
            if (detonated)
                return;

            detonated = true;

            SpawnDetonationVisuals();
            BlossomFluxSounds.PlayRightPlagueProjExplode(Projectile.Center);
            SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjHit3, Projectile.Center);
            Main.player[Projectile.owner].SetScreenshake(7.5f);

            if (Main.myPlayer == Projectile.owner)
            {
                SpawnDirectGasBurst();
                SpawnBooms();
            }

            Projectile.Kill();
        }

        // 两圈瘟疫箭，内圈快外圈慢，内圈整体偏转半格，撒出去像一张网而不是一个星形。
        private void SpawnDirectGasBurst()
        {
            int gasDamage = Math.Max(1, (int)(Projectile.damage * 0.38f));
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);

            for (int i = 0; i < DirectGasCount; i++)
            {
                float angle = baseAngle + MathHelper.TwoPi * i / DirectGasCount + Main.rand.NextFloat(-0.18f, 0.18f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(3.4f, 6f);
                int index = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity,
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    gasDamage,
                    Projectile.knockBack * 0.45f,
                    Projectile.owner,
                    Main.rand.Next(3),
                    Main.rand.NextFloat(1.35f, 1.75f));

                if (BFArrowCommon.InBounds(index, Main.maxProjectiles) &&
                    Main.projectile[index].ModProjectile is BFArrow_EPlagueGas gas)
                {
                    gas.ConfigureFromLargeSpore();
                }
            }
        }

        private void SpawnArrowRings()
        {
            int ringCount = 2;
            int perRing = 6;
            int arrowDamage = Math.Max(1, (int)(Projectile.damage * 0.38f));

            for (int ring = 0; ring < ringCount; ring++)
            {
                float speed = MathHelper.Lerp(9f, 5.5f, ring / (ringCount - 1f));

                for (int i = 0; i < perRing; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / perRing).ToRotationVector2() * speed;
                    if (ring % 2 == 0)
                        velocity = velocity.RotatedBy(MathHelper.Pi / perRing);

                    int index = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<BFArrow_EPlague>(),
                        arrowDamage,
                        Projectile.knockBack * 0.6f,
                        Projectile.owner);

                    // 标记成疫球子箭：缩短驻留、减半放毒、压掉命中特效，避免 14 根一起刷爆弹幕上限。
                    if (BFArrowCommon.InBounds(index, Main.maxProjectiles) &&
                        Main.projectile[index].ModProjectile is BFArrow_EPlague sporeArrow)
                    {
                        sporeArrow.ConfigureFromSporeBomb();
                    }
                }

                perRing += 2;
            }
        }

        private void SpawnBooms()
        {
            int boomDamage = Math.Max(1, (int)(Projectile.damage * 0.25f));

            for (int i = 0; i < 3; i++)
            {
                Projectile boom = Projectile.NewProjectileDirect(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(18f, 18f),
                    Vector2.Zero,
                    ModContent.ProjectileType<BFPlagueBoom>(),
                    boomDamage,
                    0f,
                    Projectile.owner);

                // ai[1] 是灾厄爆炸基类的最大半径，三层拉开档次才有纵深。
                boom.ai[1] = Main.rand.NextFloat(190f, 250f) + i * 34f;
                boom.Opacity = MathHelper.Lerp(0.22f, 0.62f, i / 3f) + Main.rand.NextFloat(-0.08f, 0.08f);
                boom.netUpdate = true;
            }
        }

        // 引爆闪光：沿用你们 SpawnPlagueCollapseBurst 的那套语汇，量级放大到蓄力大招该有的程度。
        private void SpawnDetonationVisuals()
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(PlagueDeep, PlagueMain, 0.42f),
                "CalamityMod/Particles/SmallBloom", Vector2.One,
                Main.rand.NextFloat(-0.4f, 0.4f), 0f, 2.1f, 36, true));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(PlagueMain, PlagueAcid, 0.55f),
                "CalamityMod/Particles/BloomRing", Vector2.One,
                Main.rand.NextFloat(-0.25f, 0.25f), 0.3f, 4.4f, 40));

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(PlagueAccent, Color.White, 0.25f), 1.5f, 22));

            // 狞桀那 75 粒径向散射，换成你们的孢子光点。
            for (int i = 0; i < 64; i++)
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(6f, 21f),
                    1f,
                    Color.Lerp(PlagueMain, PlagueAcid, Main.rand.NextFloat()),
                    64, 1.3f, 2.6f, 3f, 0f));
            }

            for (int i = 0; i < 14; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(7f, 17f),
                    "CalamityMod/Particles/VerticalSmear",
                    false, Main.rand.Next(16, 26),
                    Main.rand.NextFloat(2.2f, 3.4f),
                    Color.Lerp(PlagueMain, PlagueAcid, Main.rand.NextFloat(0.25f, 0.85f)),
                    new Vector2(0.18f, 1f)));
            }

            for (int i = 0; i < 8; i++)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(26f, 26f),
                    Main.rand.NextVector2Circular(1.8f, 1.8f),
                    Color.Lerp(PlagueMain, PlagueDeep, 0.34f),
                    Main.rand.Next(24, 36),
                    Main.rand.NextFloat(0.9f, 1.35f), 0.62f,
                    Main.rand.NextFloat(-0.05f, 0.05f), false));
            }

            for (int i = 0; i < 56; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(16f, 16f),
                    Main.rand.NextBool(4) ? DustID.TerraBlade : DustID.GreenTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(3f, 11f),
                    80,
                    Color.Lerp(PlagueDeep, PlagueAcid, Main.rand.NextFloat(0.25f, 0.95f)),
                    Main.rand.NextFloat(1.1f, 1.9f));
                dust.noGravity = true;
            }
        }

        // 蓄力被取消 / 持握弹幕消失时走这里：只散一小口气，不引爆。
        public override void OnKill(int timeLeft)
        {
            if (detonated || Main.dedServ)
                return;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 10, 0.8f, 2.6f, 0.7f, 1.05f);
            SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueGas, Projectile.Center);
        }

        #endregion

        #region 外部接口（由持握弹幕调用）

        // 持握弹幕每帧喂蓄力进度和锚点，同时续命——它一死，球就自己散掉。
        internal void PushCharge(float completion, Vector2 anchor, Vector2 aim)
        {
            if (!Charging)
                return;

            ChargePower = MathHelper.Clamp(completion, 0f, 1f);
            hoverAnchor = anchor;
            hasHoverAnchor = true;
            Projectile.velocity = aim.SafeNormalize(Vector2.UnitX);
            Projectile.timeLeft = 30;
        }

        // 松手：把球甩向准星，进入飞行阶段。
        internal void Launch(Vector2 direction, int damage, float knockback)
        {
            if (!Charging)
                return;

            State = 1f;
            Projectile.damage = damage;
            Projectile.knockBack = knockback;
            Projectile.velocity = direction.SafeNormalize(Vector2.UnitX) * LaunchSpeed;
            Projectile.timeLeft = FlightFrames + CollapseFrames + 30;
            Projectile.netUpdate = true;

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(BlossomFluxSounds.RightPlagueProjAction, Projectile.Center);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero,
                Color.Lerp(PlagueMain, PlagueAccent, 0.3f),
                new Vector2(1.1f, 1.35f), Projectile.velocity.ToRotation(),
                0.34f, 0.05f, 18));
        }

        // 蓄力未满就松手 / 切形态：直接散掉。
        internal bool TryFizzle()
        {
            if (!Charging)
                return false;

            Projectile.Kill();
            return true;
        }

        #endregion

        #region 绘制

        // 双环：外环主色顺时针，内环酸色逆时针，宽度带呼吸起伏——
        // 圆环骨架来自狞桀，呼吸与双股的读感来自你们瘟疫箭的双螺旋。
        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            // 蓄力主体已改为大型孢子贴图，不能再绘制环形图元。
        }

        private void RenderRing(float ringRadius, float spin, float glow, float phase, Color inner, Color outer, float baseWidth)
        {
            const int Segments = 70;

            // 多铺两个点把接缝盖住，否则环会有一道明显的断口。
            Vector2[] points = new Vector2[Segments + 3];
            for (int i = 0; i < points.Length; i++)
            {
                float angle = MathHelper.TwoPi * i / Segments * spin + phase;
                points[i] = Projectile.Center + angle.ToRotationVector2() * ringRadius;
            }

            PrimitiveSettings settings = new(
                (t, _) =>
                {
                    // 沿圆周三个波峰的呼吸，蓄力越满环越粗。
                    float breathe = 1f + 0.24f * MathF.Sin(t * MathHelper.TwoPi * 3f + phase * 2.4f);
                    return baseWidth * glow * breathe;
                },
                (t, _) =>
                {
                    float mix = 0.5f + 0.5f * MathF.Sin(t * MathHelper.TwoPi * 2f - phase * 1.7f);
                    Color color = Color.Lerp(inner, outer, mix) * glow;
                    color.A = 0;
                    return color;
                },
                (_, _) => Vector2.Zero,
                smoothen: false,
                pixelate: true,
                GameShaders.Misc["CalamityMod:ImpFlameTrail"]);

            PrimitiveRenderer.RenderTrail(points, settings, Segments);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Texture2D spore = ModContent.Request<Texture2D>(Texture).Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float glow = Charging ? MathHelper.Lerp(0.3f, 1f, MathHelper.Clamp(ChargePower, 0f, 1f)) : 1f;
            float pulse = 0.92f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 7f);
            float sporeScale = Charging
                ? MathHelper.Lerp(1.3f, 3.2f, MathHelper.Clamp(ChargePower, 0f, 1f))
                : MathHelper.Lerp(0.65f, 3.2f, Utils.GetLerpValue(0f, CollapseFrames, detonateCountdown, true));
            float sporeRotation = Charging
                ? Main.GlobalTimeWrappedHourly * 0.7f
                : Projectile.velocity.ToRotation() + MathHelper.PiOver2;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // 环内的整体染色，让圆里面不是一片空。
            Main.EntitySpriteDraw(bloom, drawPosition, null,
                (PlagueMain with { A = 0 }) * (0.30f * glow), 0f, bloom.Size() * 0.5f,
                radius / bloom.Width * 2.35f * pulse, SpriteEffects.None, 0);

            // 核心亮点。
            Main.EntitySpriteDraw(bloom, drawPosition, null,
                (Color.Lerp(PlagueAccent, Color.White, 0.4f) with { A = 0 }) * (0.55f * glow), 0f, bloom.Size() * 0.5f,
                radius / bloom.Width * 0.7f * pulse, SpriteEffects.None, 0);

            Main.EntitySpriteDraw(spore, drawPosition, null,
                (Color.Lerp(PlagueMain, Color.White, 0.18f) with { A = 0 }) * glow,
                sporeRotation,
                spore.Size() * 0.5f,
                sporeScale * pulse,
                SpriteEffects.None,
                0);

            // 中心旋转星芒，和你们右键箭矢的 DrawCentredRotatingStar 保持同一读感。
            for (int i = 0; i < 3; i++)
            {
                float layer = i / 3f;
                float angle = Main.GlobalTimeWrappedHourly * (i % 2 == 0 ? 3.6f : -2.4f) + layer * MathHelper.TwoPi;
                for (int b = -1; b <= 1; b += 2)
                {
                    Main.EntitySpriteDraw(star, drawPosition, null,
                        (Color.Lerp(PlagueMain, PlagueAccent, layer) with { A = 0 }) * (0.55f * glow * (1f - i * 0.2f)),
                        angle + MathHelper.PiOver4 * b, star.Size() * 0.5f,
                        new Vector2(0.24f, 1.15f) * (radius / 190f) * pulse * (1f - i * 0.18f),
                        SpriteEffects.None, 0);
                }
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        #endregion
    }
}
