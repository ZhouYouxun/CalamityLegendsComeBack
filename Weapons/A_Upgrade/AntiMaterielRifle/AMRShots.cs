using System;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle
{
    internal sealed class AMRRound : ModProjectile, ILocalizedModType
    {
        private const int HiddenSubSteps = 10;
        private const float GoldenAngle = 2.39996323f;

        private bool hitAnyTarget;
        private bool configured;
        private int visualAgeFrames;
        private int visualAgeSubSteps;

        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        private bool IsAimedShot => Projectile.ai[0] > 0.001f;
        private bool IsMarkerRound => Projectile.ai[1] >= 0.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 7;
            Projectile.timeLeft = 240;
            Projectile.scale = 1.55f;
            Projectile.light = 0.75f;
            Projectile.ArmorPenetration = 25;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (!configured)
            {
                configured = true;
                Projectile.penetrate = IsAimedShot ? -1 : 1;
                Projectile.ArmorPenetration = IsAimedShot ? 45 : 25;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(255, 196, 64).ToVector3() * 0.7f);

            // Counted in sub-steps rather than real frames: extraUpdates 7 means
            // one frame is eight steps, so a frame-based delay would keep the
            // round invisible for over a thousand pixels of flight.
            visualAgeSubSteps++;
            if (visualAgeSubSteps == HiddenSubSteps + 1)
                SpawnBreakthroughReveal();

            if (!CalamityUtils.FinalExtraUpdate(Projectile))
                return;

            visualAgeFrames++;
            SpawnFlightWake();
        }

        // ─────────────────────────────────────────────────────────────────
        // 音爆点：出膛后固定位置的一次性事件。
        // 世界锚定（velocity = Zero），所以它留在空气里当地标，
        // 而不是跟着子弹跑变成装饰。
        // ─────────────────────────────────────────────────────────────────
        private void SpawnBreakthroughReveal()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 center = Projectile.Center - forward * 5f;
            float rotation = forward.ToRotation();

            // 冷（激波）在外、暖（弹芯余热）在内。冷暖对比才分得开层次，
            // 三条同色橙环只会糊成一团。
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(118, 146, 178), new Vector2(0.30f, 1.34f), rotation, 0.05f, 0.74f, 16));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(255, 236, 178), new Vector2(0.18f, 0.72f), rotation, 0.02f, 0.30f, 10));

            // 单个 V 形激波楔，世界锚定、向后飘散。
            // 取代原本每帧重绘、粘在弹体上的两组 chevron。
            SpawnShockChevron(center, forward, 1f);
        }

        // ─────────────────────────────────────────────────────────────────
        // 巡航期：不再每帧喷 6~11 个粒子。
        // 路径已经由图元带完整表达，粒子再说一遍只会糊。
        // 每 7 帧留一枚世界锚定的激波环当"航迹标记"，单发约 4 枚。
        // ─────────────────────────────────────────────────────────────────
        private void SpawnFlightWake()
        {
            if (Main.dedServ)
                return;

            if (visualAgeFrames % 7 != 0)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float frameTravel = Projectile.velocity.Length() * (Projectile.extraUpdates + 1f);

            float cullingMargin = frameTravel + 180f;
            Rectangle expandedView = new(
                (int)(Main.screenPosition.X - cullingMargin),
                (int)(Main.screenPosition.Y - cullingMargin),
                (int)(Main.screenWidth + cullingMargin * 2f),
                (int)(Main.screenHeight + cullingMargin * 2f));
            if (!expandedView.Contains(Projectile.Center.ToPoint()))
                return;

            // velocity = Zero：环停在生成处，子弹飞走，环留下。
            // 这是"空气被撕开"而不是"子弹拖着东西"。
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center - forward * Math.Min(frameTravel * 0.5f, 190f),
                Vector2.Zero,
                new Color(126, 152, 180),
                new Vector2(0.11f, 0.86f),
                forward.ToRotation(),
                0.02f,
                0.40f,
                13));
        }

        // 一枚向后张开的 V 形激波楔。用于音爆点与每次贯穿。
        private static void SpawnShockChevron(Vector2 origin, Vector2 forward, float strength)
        {
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Vector2 velocity = -forward.RotatedBy(MathHelper.ToRadians(19f) * sign) * 7.5f * strength;
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    origin, velocity, false, 13, 0.52f * strength, new Color(186, 208, 232)));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += IsAimedShot ? 0.55f : 0.35f;

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                AMRPlayer player = Main.player[Projectile.owner].GetModPlayer<AMRPlayer>();
                modifiers.SourceDamage *= player.GetCalibrationMultiplier(target.whoAmI);
            }

            if (AMRBalance.CriticalOverflowUnlocked && Projectile.CritChance > 100)
                modifiers.CritDamage += (Projectile.CritChance - 100) / 100f;

            if (AMRBalance.CoreRuptureUnlocked)
                modifiers.CritDamage += IsAimedShot ? 0.75f : 0.5f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitAnyTarget = true;

            if (AMRBalance.DeathMarkUnlocked)
            {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 5 * 60);
                int defenseLoss = Math.Max(25, (int)(target.defense * (IsAimedShot ? 0.6f : 0.4f)));
                target.Calamity().miscDefenseLoss = Math.Max(target.Calamity().miscDefenseLoss, defenseLoss);
            }

            if (AMRBalance.MetalJetUnlocked)
                SpawnMetalJet(target.Center);

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<AMRPlayer>().RegisterCalibrationHit(target.whoAmI);

            if (AMRBalance.OnyxSequenceUnlocked)
                ResolveOnyxSequence(target, damageDone);

            SpawnImpact(target.Center, hit.Crit);
        }

        private void ResolveOnyxSequence(NPC target, int damageDone)
        {
            AMRMarkerGlobalNPC marker = target.GetGlobalNPC<AMRMarkerGlobalNPC>();
            if (IsMarkerRound)
            {
                marker.SetMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone));
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, Pitch = -0.45f }, target.Center);
                return;
            }

            if (!marker.TryConsumeMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone), out int detonationDamage))
                return;

            if (Projectile.owner != Main.myPlayer)
                return;

            int detonation = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AMROnyxDetonation>(),
                detonationDamage,
                Projectile.knockBack * 1.5f,
                Projectile.owner,
                target.whoAmI);
            if (Main.projectile.IndexInRange(detonation))
                Main.projectile[detonation].CritChance = Projectile.CritChance;
        }

        private void SpawnMetalJet(Vector2 impactCenter)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-direction.Y, direction.X);
            for (int i = 0; i < 12; i++)
            {
                float distance = MathHelper.Lerp(-10f, 46f, i / 11f);
                float sineOffset = MathF.Sin(i * GoldenAngle) * 2.2f;
                Dust jet = Dust.NewDustPerfect(
                    impactCenter + direction * distance + normal * sineOffset,
                    i % 3 == 0 ? DustID.Torch : DustID.GoldFlame,
                    direction * MathHelper.Lerp(1.5f, 5f, i / 11f),
                    55,
                    new Color(255, 202, 81),
                    MathHelper.Lerp(0.6f, 1.05f, i / 11f));
                jet.noGravity = true;
            }
        }

        // ─────────────────────────────────────────────────────────────────
        // 命中 = 穿透，不是爆炸。
        //
        // 对称环（TwoPi * i / count）说的是"它在这里炸了"；
        // 前向锥说的才是"它穿过去了"。这是本武器"突破力"的全部来源，
        // 与粒子数量无关。
        //
        // 三层，按视觉重量排序：
        //   1. 穿孔痕 —— 世界锚定、垂直于弹道压扁、寿命最长，是"洞"
        //   2. 出口锥 —— 全部朝前，±22° 紧锥，占绝大多数
        //   3. 入口反溅 —— 朝后，数量只有出口的 1/4，且更暗更慢
        // ─────────────────────────────────────────────────────────────────
        private void SpawnImpact(Vector2 center, bool critical)
        {
            if (Main.dedServ)
                return;

            float strength = critical ? 1.3f : 1f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float rotation = forward.ToRotation();

            // ① 穿孔痕：Squish 的 X 分量极小 → 环被压成垂直于弹道的一道"缝"。
            //    寿命 34 帧，远长于其他层，所以打完之后洞还留在那里。
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(28, 20, 10), new Vector2(0.16f, 1.0f), rotation,
                0.06f, 0.92f * strength, 34));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(center, Vector2.Zero,
                new Color(255, 208, 96), new Vector2(0.12f, 0.74f), rotation,
                0.03f, 0.52f * strength, 20));

            // ② 出口锥：紧 ±22°，全部朝前。速度差 2.4 倍拉出纵深。
            int coneCount = critical ? 14 : 10;
            for (int i = 0; i < coneCount; i++)
            {
                float spread = (i / (coneCount - 1f) - 0.5f) * 2f;              // -1 → 1
                float angle = MathHelper.ToRadians(22f) * spread;
                // 越靠锥心越快越亮，边缘慢而暗 —— 一个变量同时控三个属性。
                float axial = 1f - MathF.Abs(spread);
                float speed = MathHelper.Lerp(6.5f, 15.5f, axial) * strength;
                Color color = axial > 0.66f ? new Color(255, 250, 216)
                            : axial > 0.33f ? new Color(255, 199, 74)
                                            : new Color(150, 96, 20);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + forward * 6f,
                    forward.RotatedBy(angle) * speed,
                    false,
                    (int)(11 + axial * 7),
                    (0.30f + axial * 0.26f) * strength,
                    color));
            }

            // ③ 入口反溅：只有 3 支，朝后、慢、暗。存在感刚好够读出"这里是入口"。
            for (int i = -1; i <= 1; i++)
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center,
                    -forward.RotatedBy(MathHelper.ToRadians(26f) * i) * 3.6f,
                    false, 9, 0.24f * strength, new Color(120, 74, 16)));
            }

            // ④ 入口白闪：一帧的过曝核心，标记命中瞬间。
            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                center, Vector2.Zero, new Color(255, 246, 214), 0.34f * strength, 9, false));

            // ⑤ 烟：只在出口侧，2 支，不再是四方对称，也不再 required。
            for (int sign = -1; sign <= 1; sign += 2)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + forward * 14f,
                    forward.RotatedBy(MathHelper.ToRadians(30f) * sign) * 1.5f,
                    new Color(46, 36, 26), 22, 0.42f * strength, 0.62f,
                    sign * 0.035f, false));
            }

            // ⑥ 贯穿楔：与音爆点同一语汇，把"又穿过一个"读出来。
            SpawnShockChevron(center, forward, 0.85f * strength);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);
            SpawnImpact(Projectile.Center, false);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f }, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!hitAnyTarget && Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<AMRPlayer>().ResetCalibration();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 248, 205) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawShaderTrail();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;

            // 弹体只在音爆点之后可见（原逻辑保留）。
            bool bodyVisible = visualAgeSubSteps > HiddenSubSteps;
            if (!bodyVisible)
                return false;

            // 高频脉动被去掉了：一颗超音速弹丸不应该在"呼吸"。
            // 恒定亮度 + 细弹芯 = 冷静、锐利，这是"清爽"的一部分。

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;

            // 三次绘制，不是六次：
            //   ① 暖色外晕（表明"这里很烫"）
            //   ② 过曝白心（表明"这里是弹头"）
            //   ③ 沿弹道拉长的本体
            Main.EntitySpriteDraw(bloom, drawCenter, null, new Color(255, 168, 28, 0), 0f,
                bloomOrigin, 0.19f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawCenter + forward * 2f, null, new Color(255, 252, 232, 0), 0f,
                bloomOrigin, 0.075f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawCenter, null, new Color(255, 244, 198, 0),
                Projectile.rotation, origin,
                new Vector2(Projectile.scale * 1.9f, Projectile.scale * 0.92f),
                SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawShaderTrail()
        {
            Vector2[] trailPoints = BuildFlightTrailPoints();
            if (trailPoints.Length < 3)
                return;

            MiscShaderData trailShader = GameShaders.Misc["CalamityMod:TrailStreak"];
            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"));

            // 两条，不是三条。
            //
            // 原本压力带(172,96,13)/热带(255,244,190)/核心(255,255,244) 三条
            // 同处橙-白色域，宽度 14 / 8.2 / 3.35 也没拉开数量级，叠在同一批
            // 像素上只会读成一条脏橙色 —— 这是"不清爽"的头号来源。
            //
            // 现在：冷激波 18px 低 alpha  ×  热弹芯 2.6px 高 alpha
            //       宽度比 ≈ 7:1，色相冷暖相反。两者才分得开。
            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    ShockTrailWidth,
                    ShockTrailColor,
                    (_, _) => Vector2.Zero,
                    smoothen: false,
                    pixelate: false,
                    shader: trailShader),
                trailPoints.Length * 2);

            int corePointCount = Math.Min(10, trailPoints.Length);
            if (corePointCount < 3)
                return;

            Vector2[] corePoints = new Vector2[corePointCount];
            Array.Copy(trailPoints, corePoints, corePointCount);
            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    PenetratorCoreWidth,
                    PenetratorCoreColor,
                    (_, _) => Vector2.Zero,
                    smoothen: false,
                    pixelate: false,
                    shader: trailShader),
                corePoints.Length * 2);
        }

        private Vector2[] BuildFlightTrailPoints()
        {
            Vector2[] points = new Vector2[Projectile.oldPos.Length + 3];
            int count = 0;
            points[count++] = Projectile.Center;

            Vector2 lastPoint = Projectile.Center;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 point = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (Vector2.DistanceSquared(point, lastPoint) < 4f)
                    continue;

                points[count++] = point;
                lastPoint = point;
            }

            while (count < 3)
            {
                points[count] = Projectile.Center - Projectile.velocity * count;
                count++;
            }

            Array.Resize(ref points, count);
            return points;
        }

        // 激波：宽、冷、淡。表达"空气被撕开的那一层"，不表达弹体本身。
        private float ShockTrailWidth(float completion, Vector2 _)
        {
            return TaperedTrailWidth(completion, 18f * Projectile.scale / 1.55f, 0.06f, 0.55f);
        }

        private Color ShockTrailColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.42f, 1f, completion, true);
            // 冷灰蓝 → 更深的蓝。与弹芯的暖白构成冷暖对比，这是分层的关键。
            Color shock = Color.Lerp(new Color(150, 176, 205), new Color(48, 63, 84), completion);
            // A 不能设 0：TrailStreak 在 AlphaBlend 下渲染，像素输出=顶点色×opacity，
            // A=0 会让整条拖尾透明（这正是主体拖尾此前"缺失"的原因）。让 A 随强度走。
            return shock * (tailFade * 0.34f);
        }

        // 弹芯：极细、极亮、极短。它是"子弹在哪"的唯一读数。
        private float PenetratorCoreWidth(float completion, Vector2 _)
        {
            return TaperedTrailWidth(completion, 2.6f * Projectile.scale / 1.55f, 0.05f, 0.85f);
        }

        private Color PenetratorCoreColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.5f, 1f, completion, true);
            Color core = Color.Lerp(new Color(255, 255, 248), new Color(255, 186, 46), completion * 0.8f);
            // 同 ShockTrailColor：primitive 拖尾在 AlphaBlend 下 A=0 即隐形，这里让弹芯拿满 A。
            return core * tailFade;
        }

        private static float TaperedTrailWidth(float completion, float maximumWidth, float headFraction, float tailPower)
        {
            if (completion < headFraction)
            {
                float headCompletion = MathHelper.Clamp(completion / headFraction, 0f, 1f);
                return MathHelper.SmoothStep(0.4f, maximumWidth, headCompletion);
            }

            float tailCompletion = MathHelper.Clamp((completion - headFraction) / (1f - headFraction), 0f, 1f);
            return maximumWidth * MathF.Pow(1f - tailCompletion, tailPower);
        }

    }

    internal sealed class AMRMarkerGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private readonly int[] markerTimers = new int[Main.maxPlayers];
        private readonly int[] markerDamages = new int[Main.maxPlayers];

        public override void PostAI(NPC npc)
        {
            for (int i = 0; i < markerTimers.Length; i++)
            {
                if (markerTimers[i] > 0)
                    markerTimers[i]--;
                else
                    markerDamages[i] = 0;
            }
        }

        internal void SetMarker(int owner, int damage)
        {
            if (owner < 0 || owner >= markerTimers.Length)
                return;

            markerTimers[owner] = 5 * 60;
            markerDamages[owner] = Math.Max(1, damage);
        }

        internal bool TryConsumeMarker(int owner, int detonatorDamage, out int damage)
        {
            damage = 0;
            if (owner < 0 || owner >= markerTimers.Length || markerTimers[owner] <= 0)
                return false;

            damage = Math.Max((int)(detonatorDamage * 1.8f), markerDamages[owner] * 2);
            markerTimers[owner] = 0;
            markerDamages[owner] = 0;
            return true;
        }
    }

    internal sealed class AMROnyxDetonation : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].active)
                Projectile.Center = Main.npc[targetIndex].Center;

            if (Projectile.timeLeft == 3)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = -0.35f }, Projectile.Center);
                for (int i = 0; i < 28; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 12f), 50,
                        Main.rand.NextBool() ? new Color(233, 102, 238) : new Color(255, 202, 81),
                        Main.rand.NextFloat(0.9f, 1.7f));
                    dust.noGravity = true;
                }
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == (int)Projectile.ai[0] ? null : false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float completion = 1f - Projectile.timeLeft / 3f;
            float scale = MathHelper.Lerp(0.15f, 1.25f, completion);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                new Color(233, 102, 238) * (1f - completion), 0f, bloom.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                new Color(255, 245, 213) * (0.8f - completion * 0.6f), 0f, bloom.Size() * 0.5f,
                scale * 0.45f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            return false;
        }
    }

    internal sealed class AMRSlideExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.timeLeft == 3)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.85f, Pitch = -0.25f }, Projectile.Center);
                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                        new Color(255, 195, 58), new Vector2(1f, 1f), 0f, 0.08f, 1.4f, 18));
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                        new Color(150, 70, 20), new Vector2(1.5f, 1.5f), 0f, 0.05f, 2.0f, 22));

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(12f, 12f);
                        GeneralParticleHandler.SpawnParticle(new LineParticle(Projectile.Center, vel, false, 15, 0.5f,
                            i % 2 == 0 ? new Color(255, 210, 82) : new Color(200, 100, 30)));
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                        GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center, vel,
                            new Color(25, 20, 15), 24, 0.85f, 0.8f, 0.03f, false, required: true));
                    }
                }
            }
        }
    }
}
