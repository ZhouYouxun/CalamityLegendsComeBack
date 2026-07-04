using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.EAfterDog.Cynosure
{
    /// <summary>
    /// 外层充能单元。它们围成圆环，短暂停留后向目标释放细电弧并自爆。
    /// </summary>
    public class CynosureChargedCell : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/SHPC/Effects/EAfterDog/Cynosure/注能金源珠九帧图";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private Vector2 OrbitCenter
        {
            get => new(Projectile.localAI[1], Projectile.localAI[2]);
            set
            {
                Projectile.localAI[1] = value.X;
                Projectile.localAI[2] = value.Y;
            }
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 9;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 74;
        }

        public override void AI()
        {
            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
            }

            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                OrbitCenter = Projectile.Center;
                Projectile.velocity = Projectile.ai[1].ToRotationVector2() * Main.rand.NextFloat(27f, 37.5f);
            }

            float age = 74f - Projectile.timeLeft;
            Projectile.rotation += 0.22f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.72f, 0.52f, 0.10f) * 0.55f);

            if (age < 38f)
            {
                Projectile.velocity *= 0.9f;

                // 充能蓄力特效：随机电火花 + 偶发光球
                if (Projectile.timeLeft % 6 == 0)
                    CynosureVisuals.SpawnElectricBurst(Projectile.Center, 1, 0.8f, 2.5f);

                if (!Main.dedServ && Projectile.timeLeft % 10 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                        Main.rand.NextVector2Circular(1.2f, 1.2f),
                        Main.rand.NextFloat(0.10f, 0.20f),
                        Color.Lerp(new Color(255, 214, 88), Color.White, Main.rand.NextFloat(0.2f, 0.6f)),
                        Main.rand.Next(5, 10)));
                }
                return;
            }

            Projectile.velocity *= 0.82f;
            if (age >= 58f)
                Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SoundEngine.PlaySound(SoundID.DD2_LightningAuraZap with { Volume = 0.22f, Pitch = 0.28f, PitchVariance = 0.14f, MaxInstances = 4 }, Projectile.Center);

            NPC target = CynosureTargeting.FindTarget((int)Projectile.ai[0], Projectile.Center);
            if (target != null && Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<CynosureLightningArc>(), Projectile.damage, Projectile.knockBack,
                    Projectile.owner, target.whoAmI, target.Center.X, target.Center.Y);

                // 在目标处同时产生闪电爆炸
                if (Main.rand.NextBool(3))
                {
                    Projectile.NewProjectile(Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                        ModContent.ProjectileType<CynosureLightningExplosion>(), Projectile.damage, Projectile.knockBack,
                        Projectile.owner);
                }
                else
                {
                    SpawnSimpleLightningImpact(target.Center);
                }
            }

            CynosureVisuals.SpawnElectricBurst(Projectile.Center, 7, 2.4f, 10f);

            // 脉冲环
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero,
                new Color(255, 214, 88),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.035f, 0.22f, 14));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero,
                Color.White,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.30f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.55f, 0.04f, 10));

            // 放射状线条火花（仿 AuricBall OnKill）
            for (int i = 0; i < 5; i++)
            {
                float angle = MathHelper.TwoPi * i / 5f;
                Vector2 dir = angle.ToRotationVector2();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + dir * Main.rand.NextFloat(2f, 7f),
                    dir * Main.rand.NextFloat(2.5f, 10f),
                    false,
                    Main.rand.Next(8, 15),
                    Main.rand.NextFloat(0.16f, 0.38f),
                    i % 2 == 0 ? new Color(255, 214, 88) : new Color(255, 240, 160)));
            }
        }

        private static void SpawnSimpleLightningImpact(Vector2 center)
        {
            if (Main.dedServ)
                return;

            CynosureVisuals.SpawnElectricBurst(center, 7, 2.2f, 10f);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                new Color(255, 218, 84),
                "CalamityMod/Particles/BloomRing",
                Vector2.One * 0.42f,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.04f,
                0.16f,
                10));

            for (int i = 0; i < 5; i++)
            {
                Vector2 direction = Main.rand.NextVector2Unit();
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    center + direction * Main.rand.NextFloat(2f, 8f),
                    direction * Main.rand.NextFloat(3f, 9f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.12f, 0.28f),
                    Main.rand.NextBool(3) ? Color.White : new Color(54, 205, 255)));
            }
        }

        public override bool? CanDamage() => false;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D circle = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 texOrigin = frame.Size() * 0.5f;
            Vector2 circleOrigin = circle.Size() * 0.5f;
            float pulse = 1f + 0.08f * MathF.Sin(Main.GlobalTimeWrappedHourly * 8f + Projectile.identity * 1.4f);
            Color gold = new Color(255, 214, 88, 0);
            Color white = Color.White with { A = 0 };

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // 光晕 bloom
            Main.EntitySpriteDraw(circle, center, null, gold * 0.70f, 0f, circleOrigin, 0.30f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(circle, center, null, gold * 0.45f, 0f, circleOrigin, 0.50f * pulse, SpriteEffects.None);
            Main.EntitySpriteDraw(circle, center, null, white * 0.35f, 0f, circleOrigin, 0.15f, SpriteEffects.None);

            // 贴图轮廓辉光（10 圈，仿 AuricBall）
            float outlinePulse = 1f + 0.10f * MathF.Sin(Main.GlobalTimeWrappedHourly * 12f + Projectile.identity);
            for (int i = 0; i < 4; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * (2.1f * outlinePulse);
                Main.EntitySpriteDraw(texture, center + offset, frame,
                    gold * 0.45f,
                    Projectile.rotation, texOrigin, Projectile.scale * 1.08f, SpriteEffects.None);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // 实体贴图（正常混合，保证可见）
            Main.EntitySpriteDraw(texture, center, frame, Color.White, Projectile.rotation, texOrigin, Projectile.scale, SpriteEffects.None);
            return false;
        }
    }

    /// <summary>
    /// 命中点的闪电爆炸。视觉上参考金源地雷，但半径被压缩到适合武器命中的尺寸。
    /// </summary>
    public class CynosureLightningExplosion : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private const int ExplosionLifetime = 18;
        private const float TenTileRadius = 10f * 16f;
        private const float OuterVisualRadius = TenTileRadius * 1.32f * 0.4f;
        private static readonly Color CynosureBlue = new(54, 205, 255);
        private static readonly Color CynosureGold = new(255, 218, 84);
        private static readonly Color CynosureWhite = new(232, 250, 255);

        private readonly List<List<Vector2>> lightningTrails = new();
        private readonly List<List<Vector2>> sigilTrails = new();

        public override void SetDefaults()
        {
            Projectile.width = 260;
            Projectile.height = 260;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = ExplosionLifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.timeLeft == ExplosionLifetime)
            {
                BuildLightning();
                BuildSigil();
                SpawnOpeningBlast();
            }

            if (Projectile.timeLeft % 20 == 0)
                BuildLightning();

            float fade = Projectile.timeLeft / (float)ExplosionLifetime;
            Lighting.AddLight(Projectile.Center, new Vector3(0.16f, 0.74f, 1f) * (0.95f + fade * 0.6f));
            CynosureVisuals.SpawnScarletStyleBurst(
                Projectile.Center,
                Math.Max(1, (int)MathF.Ceiling(fade * 5f)),
                12f,
                29f,
                1.12f + fade * 0.62f);

            SpawnMathematicalResidue(fade);
        }

        private void BuildLightning()
        {
            // Rebuild the outer cracks periodically so the sigil flickers like a real high-energy fracture.
            lightningTrails.Clear();
            const int crackCount = 4;
            const int pointCount = 6;

            for (int i = 0; i < crackCount; i++)
            {
                List<Vector2> points = new();
                float baseAngle = MathHelper.TwoPi * i / crackCount + Main.rand.NextFloat(-0.10f, 0.10f);
                Vector2 direction = baseAngle.ToRotationVector2();
                Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 current = Projectile.Center + Main.rand.NextVector2Circular(14f, 14f);
                points.Add(current);

                for (int j = 1; j < pointCount; j++)
                {
                    float completion = j / (float)(pointCount - 1);
                    float targetRadius = MathHelper.Lerp(18f, OuterVisualRadius, completion);
                    Vector2 target = Projectile.Center + direction * targetRadius;
                    float sine = MathF.Sin(completion * MathHelper.TwoPi * 3f + i * 0.63f) * MathHelper.Lerp(8f, 34f, completion);
                    current = Vector2.Lerp(current, target, 0.46f);
                    current += direction * Main.rand.NextFloat(7f, 19f) + normal * (sine * 0.34f + Main.rand.NextFloat(-18f, 18f));

                    Vector2 offset = current - Projectile.Center;
                    if (offset.Length() > OuterVisualRadius)
                        current = Projectile.Center + offset.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(TenTileRadius * 0.92f, OuterVisualRadius);

                    points.Add(current);
                }

                lightningTrails.Add(points);
            }
        }

        private void BuildSigil()
        {
            sigilTrails.Clear();

            AddRoseTrail(0f, 5, TenTileRadius * 0.34f, 14, 0.08f);
            AddLissajousTrail(MathHelper.PiOver4, TenTileRadius * 0.24f, 16);

            const int starPoints = 6;
            for (int i = 0; i < starPoints; i++)
            {
                float start = MathHelper.TwoPi * i / starPoints - MathHelper.PiOver2;
                float end = MathHelper.TwoPi * ((i + 3) % starPoints) / starPoints - MathHelper.PiOver2;
                AddChordTrail(start, end, TenTileRadius * 0.4f, 0.16f + (i % 2) * 0.10f);
            }

            for (int arm = 0; arm < 3; arm++)
            {
                float phase = MathHelper.TwoPi * arm / 6f;
                AddFermatSpiral(phase, TenTileRadius * 0.38f, clockwise: arm % 2 == 0);
            }
        }

        private void AddRoseTrail(float phase, int petals, float radius, int pointCount, float swirl)
        {
            List<Vector2> points = new();
            for (int i = 0; i <= pointCount; i++)
            {
                float angle = MathHelper.TwoPi * i / pointCount;
                float petal = 0.68f + 0.32f * MathF.Cos(petals * angle);
                float drawAngle = angle + phase + MathF.Sin(angle * petals) * swirl;
                points.Add(Projectile.Center + drawAngle.ToRotationVector2() * radius * petal);
            }
            sigilTrails.Add(points);
        }

        private void AddLissajousTrail(float phase, float radius, int pointCount)
        {
            List<Vector2> points = new();
            for (int i = 0; i <= pointCount; i++)
            {
                float t = MathHelper.TwoPi * i / pointCount;
                Vector2 offset = new(
                    MathF.Sin(3f * t + phase) * radius,
                    MathF.Sin(2f * t) * radius * 0.62f);
                points.Add(Projectile.Center + offset.RotatedBy(phase * 0.5f));
            }
            sigilTrails.Add(points);
        }

        private void AddChordTrail(float startAngle, float endAngle, float radius, float bend)
        {
            Vector2 start = Projectile.Center + startAngle.ToRotationVector2() * radius;
            Vector2 end = Projectile.Center + endAngle.ToRotationVector2() * radius;
            Vector2 midpoint = Vector2.Lerp(start, end, 0.5f);
            Vector2 controlDirection = (midpoint - Projectile.Center).SafeNormalize(Vector2.UnitY);
            Vector2 control = Projectile.Center + controlDirection * radius * bend;
            List<Vector2> points = new();

            for (int i = 0; i <= 10; i++)
            {
                float t = i / 10f;
                Vector2 a = Vector2.Lerp(start, control, t);
                Vector2 b = Vector2.Lerp(control, end, t);
                points.Add(Vector2.Lerp(a, b, t));
            }
            sigilTrails.Add(points);
        }

        private void AddFermatSpiral(float phase, float radius, bool clockwise)
        {
            List<Vector2> points = new();
            float direction = clockwise ? 1f : -1f;
            for (int i = 0; i <= 10; i++)
            {
                float completion = i / 10f;
                float angle = phase + direction * completion * MathHelper.TwoPi * 2.45f;
                float r = MathF.Sqrt(completion) * radius;
                points.Add(Projectile.Center + angle.ToRotationVector2() * r);
            }
            sigilTrails.Add(points);
        }

        private void SpawnOpeningBlast()
        {
            float rotation = Main.rand.NextFloat(MathHelper.TwoPi);

            CynosureVisuals.SpawnElectricBurst(Projectile.Center, 10, 5f, 30f);
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, CynosureBlue, Vector2.One * 0.66f, 0f, 0.08f, 0.42f, 24));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, CynosureGold, new Vector2(0.29f, 0.78f), rotation, 0.05f, 0.47f, 22));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center, Vector2.Zero, CynosureWhite, new Vector2(0.78f, 0.29f), rotation + MathHelper.PiOver2, 0.04f, 0.37f, 20));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, CynosureGold * 0.66f,
                "CalamityMod/Particles/ShatteredExplosion", Vector2.One, rotation,
                0f, 0.42f, 32));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, CynosureBlue * 0.72f,
                "CalamityMod/Particles/ShatteredExplosion", Vector2.One, rotation + MathHelper.TwoPi / 3f,
                0f, 0.33f, 29));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, CynosureWhite * 0.56f,
                "CalamityMod/Particles/ShatteredExplosion", Vector2.One, rotation + MathHelper.TwoPi * 2f / 3f,
                0f, 0.23f, 26));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center, Vector2.Zero, new Color(255, 224, 92),
                "CalamityMod/Particles/PlasmaExplosion", Vector2.One,
                rotation + MathHelper.PiOver4, 0.04f, 0.18f, 18));

            float goldenAngle = MathHelper.Pi * (3f - MathF.Sqrt(5f));
            for (int i = 0; i < 8; i++)
            {
                float angle = goldenAngle * i + rotation;
                float rose = 0.76f + 0.24f * MathF.Sin(angle * 5f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 velocity = direction * Main.rand.NextFloat(8f, 22f) * rose + tangent * Main.rand.NextFloat(-2.6f, 2.6f);
                Color color = Color.Lerp(CynosureGold, CynosureBlue, i % 3 / 2f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center + direction * Main.rand.NextFloat(4f, 18f),
                    velocity,
                    false,
                    Main.rand.Next(14, 26),
                    Main.rand.NextFloat(0.38f, 0.88f),
                    i % 5 == 0 ? CynosureWhite : color));
            }

            for (int i = 0; i < 6; i++)
            {
                float angle = MathHelper.TwoPi * i / 72f + rotation;
                Vector2 direction = angle.ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + direction * Main.rand.NextFloat(18f, 46f),
                    i % 3 == 0 ? DustID.GoldCoin : DustID.Electric,
                    direction * Main.rand.NextFloat(6f, 18f),
                    80,
                    i % 2 == 0 ? CynosureGold : CynosureBlue,
                    Main.rand.NextFloat(1.05f, 1.85f));
                dust.noGravity = true;
            }
        }

        private void SpawnMathematicalResidue(float fade)
        {
            if (Main.dedServ)
                return;

            float age = ExplosionLifetime - Projectile.timeLeft;
            int count = Math.Max(1, (int)MathF.Ceiling(2f * fade));
            float expansion = Utils.GetLerpValue(0f, ExplosionLifetime * 0.62f, age, true);

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count + age * 0.13f;
                float rose = 0.72f + 0.28f * MathF.Cos(angle * 5f + age * 0.21f);
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tangent = direction.RotatedBy(MathHelper.PiOver2);
                Vector2 position = Projectile.Center + direction * TenTileRadius * 0.4f * MathHelper.Lerp(0.38f, 1f, expansion) * rose;
                Vector2 velocity = direction * Main.rand.NextFloat(1.2f, 4.8f) + tangent * MathF.Sin(age * 0.2f + i) * Main.rand.NextFloat(0.8f, 3.2f);
                Color color = Color.Lerp(CynosureGold, CynosureBlue, (i % 5) / 4f);

                Dust dust = Dust.NewDustPerfect(
                    position,
                    i % 4 == 0 ? DustID.GoldCoin : DustID.Electric,
                    velocity,
                    100,
                    i % 6 == 0 ? CynosureWhite : color,
                    Main.rand.NextFloat(0.78f, 1.34f) * (0.8f + fade * 0.55f));
                dust.noGravity = true;

                if (Projectile.timeLeft % 2 == 0 && i % 3 == 0)
                {
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        position,
                        velocity + tangent * Main.rand.NextFloat(-4.5f, 4.5f),
                        false,
                        Main.rand.Next(8, 16),
                        Main.rand.NextFloat(0.16f, 0.34f) * (0.7f + fade),
                        color));
                }
            }
        }

        private float TrailVisibility()
        {
            float age = ExplosionLifetime - Projectile.timeLeft;
            float fadeIn = Utils.GetLerpValue(0f, 4f, age, true);
            float fadeOut = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            return MathHelper.Clamp(Math.Max(0.28f, fadeIn) * fadeOut, 0f, 1f);
        }

        internal float LightningWidth(float completion, Vector2 _) => MathHelper.Lerp(8.5f, 1.15f, completion) * TrailVisibility();
        internal Color LightningColor(float completion, Vector2 _)
        {
            float wave = 0.5f + 0.5f * MathF.Sin(completion * MathHelper.TwoPi * 2f + Projectile.identity * 0.13f);
            Color color = Color.Lerp(CynosureWhite, CynosureBlue, completion * 0.78f);
            color = Color.Lerp(color, CynosureGold, wave * 0.26f);
            return color * TrailVisibility() * (1f - completion * 0.32f);
        }

        internal float LightningBackWidth(float completion, Vector2 vertexPos) => LightningWidth(completion, vertexPos) * 2.35f;
        internal Color LightningBackColor(float completion, Vector2 vertexPos) => LightningColor(completion, vertexPos) * 0.34f;

        internal float SigilWidth(float completion, Vector2 _) => MathHelper.Lerp(3.1f, 1.15f, completion) * TrailVisibility();
        internal Color SigilColor(float completion, Vector2 _)
        {
            float wave = 0.5f + 0.5f * MathF.Sin(completion * MathHelper.TwoPi * 5f + Projectile.identity * 0.09f);
            Color color = Color.Lerp(CynosureGold, CynosureBlue, wave);
            return Color.Lerp(color, CynosureWhite, 0.16f) * TrailVisibility() * 0.72f;
        }

        internal float SigilBackWidth(float completion, Vector2 vertexPos) => SigilWidth(completion, vertexPos) * 2.1f;
        internal Color SigilBackColor(float completion, Vector2 vertexPos) => SigilColor(completion, vertexPos) * 0.28f;

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            foreach (List<Vector2> points in sigilTrails)
                DrawTrailSegments(pixel, points, sigil: true);

            foreach (List<Vector2> points in lightningTrails)
                DrawTrailSegments(pixel, points, sigil: false);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private void DrawTrailSegments(Texture2D pixel, List<Vector2> trailPoints, bool sigil)
        {
            if (trailPoints.Count < 2)
                return;

            for (int i = 1; i < trailPoints.Count; i++)
            {
                float completion = i / (float)(trailPoints.Count - 1);
                Vector2 start = trailPoints[i - 1];
                Vector2 end = trailPoints[i];
                float width = sigil ? SigilWidth(completion, end) : LightningWidth(completion, end);
                Color color = sigil ? SigilColor(completion, end) : LightningColor(completion, end);

                DrawExplosionSegment(pixel, start, end, color, width * 1.55f, 0.22f);
                DrawExplosionSegment(pixel, start, end, color, Math.Max(1f, width), 0.72f);
            }
        }

        private static void DrawExplosionSegment(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width, float opacity)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color * opacity,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), width),
                SpriteEffects.None);
        }
    }

    /// <summary>
    /// 充能单元释放的细电弧。线段本身有伤害，但只存在极短时间。
    /// </summary>
    public class CynosureLightningArc : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.SHPC";

        private readonly List<Vector2> points = new();

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (points.Count > 0)
                return;

            Vector2 start = Projectile.Center;
            Vector2 end = new(Projectile.ai[1], Projectile.ai[2]);
            NPC target = CynosureTargeting.FindTarget((int)Projectile.ai[0], start);
            if (target != null)
                end = target.Center;

            for (int i = 0; i <= 10; i++)
            {
                float progress = i / 10f;
                Vector2 point = Vector2.Lerp(start, end, progress);
                if (i != 0 && i != 10)
                    point += Main.rand.NextVector2Circular(12f, 12f);
                points.Add(point);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (points.Count < 2)
                return false;

            float collisionPoint = 0f;
            for (int i = 1; i < points.Count; i++)
            {
                if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), points[i - 1], points[i], 5f, ref collisionPoint))
                    return true;
            }
            return false;
        }

        internal float Width(float completion, Vector2 _) => MathHelper.Lerp(4.2f, 1.15f, completion);
        internal Color ColorFunction(float completion, Vector2 _)
        {
            Color hotCore = Color.Lerp(Color.White, new Color(255, 224, 92), 0.4f);
            Color coldEdge = Color.Lerp(new Color(255, 196, 54), Color.Cyan, completion * 0.55f);
            return Color.Lerp(hotCore, coldEdge, completion) * (1f - completion * 0.18f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (points.Count < 2)
                return false;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 1; i < points.Count; i++)
            {
                float completion = i / (float)(points.Count - 1);
                DrawLightningSegment(pixel, points[i - 1], points[i], ColorFunction(completion, points[i]), Width(completion, points[i]) * 1.55f, 0.28f);
                DrawLightningSegment(pixel, points[i - 1], points[i], Color.White, Math.Max(1f, Width(completion, points[i]) * 0.42f), 0.62f);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private static void DrawLightningSegment(Texture2D pixel, Vector2 start, Vector2 end, Color color, float width, float opacity)
        {
            Vector2 edge = end - start;
            if (edge.LengthSquared() <= 0.001f)
                return;

            Main.EntitySpriteDraw(
                pixel,
                start - Main.screenPosition,
                new Rectangle(0, 0, 1, 1),
                color * opacity,
                edge.ToRotation(),
                new Vector2(0f, 0.5f),
                new Vector2(edge.Length(), width),
                SpriteEffects.None);
        }
    }

    internal static class CynosureTargeting
    {
        internal static NPC FindTarget(int preferredTarget, Vector2 center)
        {
            if (Main.npc.IndexInRange(preferredTarget))
            {
                NPC preferred = Main.npc[preferredTarget];
                if (preferred.CanBeChasedBy())
                    return preferred;
            }

            NPC closest = null;
            float closestDistance = 1200f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;
                float distance = Vector2.Distance(center, npc.Center);
                if (distance < closestDistance)
                {
                    closest = npc;
                    closestDistance = distance;
                }
            }
            return closest;
        }
    }

    internal static class CynosureVisuals
    {
        internal static void SpawnElectricBurst(Vector2 center, int count, float minSpeed, float maxSpeed)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(minSpeed, maxSpeed);
                Color color = Main.rand.NextBool(3) ? new Color(255, 214, 86) : (Main.rand.NextBool(4) ? Color.White : Color.Cyan);
                Dust dust = Dust.NewDustPerfect(center, DustID.Electric, velocity, 0, color, Main.rand.NextFloat(0.8f, 1.5f));
                dust.noGravity = true;
            }
        }

        internal static void SpawnScarletStyleBurst(Vector2 center, int count, float minSpeed, float maxSpeed, float scale)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(minSpeed, maxSpeed);
                Color color = Main.rand.NextBool(5)
                    ? Color.White
                    : (Main.rand.NextBool() ? new Color(255, 218, 84) : new Color(54, 205, 255));
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    DustID.Electric,
                    velocity,
                    100,
                    color,
                    scale * Main.rand.NextFloat(0.86f, 1.16f));
                dust.noGravity = true;
            }
        }
    }
}
