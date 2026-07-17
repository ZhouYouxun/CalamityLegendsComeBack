using System;
using CalamityMod;
using CalamityMod.Particles;
using CalamityLegendsComeBack;
using CalamityLegendsComeBack.Systems;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    // ──────────────────────────────────────────────────────
    // MODULE 1 · DATA GRID
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 数据矩阵模块：在目标附近生成一张 5×6 的数据面板，
    /// 逐格点亮后所有亮格同时向目标射出 MatrixGridCell。
    /// </summary>
    public sealed class MatrixDataGridPanel : ModProjectile, ILocalizedModType
    {
        private const int Cols = 5;
        private const int Rows = 6;
        private const int CellCount = Cols * Rows;
        private const float CellSize = 18f;
        private const int FadeInEnd = 30;
        private const int LightUpEnd = 100;   // 30 + CellCount * 2.33
        private const int FireFrame = 102;
        private const int Lifetime = 130;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;

        // ai[0] = coreWhoAmI, ai[1] = targetIndex
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;
            // Fire all lit cells on the designated frame (owner client only)
            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                SpawnCells();

            // Visual flash at each lit cell when firing (all clients)
            if (age == FireFrame && !Main.dedServ)
                SpawnCellFlashParticles();
        }

        private void SpawnCellFlashParticles()
        {
            Vector2 origin = GetGridOrigin();
            for (int i = 0; i < CellCount; i++)
            {
                if (!CellIsLit(i))
                    continue;
                Vector2 cellWorld = origin + GetCellLocalPos(i) + new Vector2(CellSize * 0.5f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)CellCount);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    cellWorld, Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 2.5f),
                    false, 8 + Main.rand.Next(6), 0.5f, c, true, false, false));
                if (Main.rand.NextBool(3))
                    GeneralParticleHandler.SpawnParticle(new SquareParticle(
                        cellWorld, Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 1.8f),
                        false, 16, 0.8f + Main.rand.NextFloat(0.4f), c * 1.4f));
            }
            // Data burst at panel center
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(
                GetGridOrigin() + new Vector2(Cols * CellSize * 0.5f, Rows * CellSize * 0.5f),
                HyperdimensionalMatrixVisuals.GetDataColor(0.5f), 0.6f);
        }

        private void SpawnCells()
        {
            NPC target = GetTarget();
            Vector2 aimPos = target?.Center ?? Projectile.Center;

            for (int i = 0; i < CellCount; i++)
            {
                if (!CellIsLit(i))
                    continue;

                Vector2 worldCell = GetGridOrigin() + GetCellLocalPos(i) + new Vector2(CellSize * 0.5f);
                Vector2 dir = (aimPos - worldCell).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    worldCell,
                    dir * 22f,
                    ModContent.ProjectileType<MatrixGridCell>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.22f, Pitch = 0.5f, MaxInstances = 5 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float opacity = age < FadeInEnd
                ? age / (float)FadeInEnd
                : age > Lifetime - FadeInEnd
                    ? (Lifetime - age) / (float)FadeInEnd
                    : 1f;

            // GetGridOrigin returns a WORLD position; DrawLineBetter / DrawNode also expect world positions.
            Vector2 origin = GetGridOrigin();

            for (int i = 0; i < CellCount; i++)
            {
                Vector2 cellWorld = origin + GetCellLocalPos(i);
                bool lit = CellIsLit(i);
                Color c = lit
                    ? HyperdimensionalMatrixVisuals.GetDataColor(i / (float)CellCount, opacity)
                    : HyperdimensionalMatrixVisuals.GetDataColor(i / (float)CellCount, opacity * 0.22f);

                DrawCellWorld(cellWorld, CellSize, c, lit ? 1.8f : 1f);
                if (lit)
                    HyperdimensionalMatrixVisuals.DrawNode(cellWorld + Vector2.One * (CellSize * 0.5f), c, 3.5f);
            }

            // Border around entire panel (world coords)
            Color borderColor = HyperdimensionalMatrixVisuals.GetDataColor(0.25f, opacity * 0.55f);
            float w = Cols * CellSize;
            float h = Rows * CellSize;
            Main.spriteBatch.DrawLineBetter(origin, origin + new Vector2(w, 0f), borderColor, 1.5f);
            Main.spriteBatch.DrawLineBetter(origin + new Vector2(w, 0f), origin + new Vector2(w, h), borderColor, 1.5f);
            Main.spriteBatch.DrawLineBetter(origin + new Vector2(w, h), origin + new Vector2(0f, h), borderColor, 1.5f);
            Main.spriteBatch.DrawLineBetter(origin + new Vector2(0f, h), origin, borderColor, 1.5f);

            return false;
        }

        // Draws a cell outline using WORLD space coordinates (DrawLineBetter handles screen offset).
        private static void DrawCellWorld(Vector2 worldOrigin, float size, Color color, float width)
        {
            Vector2 tl = worldOrigin;
            Vector2 tr = worldOrigin + new Vector2(size, 0f);
            Vector2 br = worldOrigin + new Vector2(size, size);
            Vector2 bl = worldOrigin + new Vector2(0f, size);
            Main.spriteBatch.DrawLineBetter(tl, tr, color, width);
            Main.spriteBatch.DrawLineBetter(tr, br, color, width);
            Main.spriteBatch.DrawLineBetter(br, bl, color, width);
            Main.spriteBatch.DrawLineBetter(bl, tl, color, width);
        }

        private Vector2 GetGridOrigin()
        {
            NPC target = GetTarget();
            Vector2 anchor = target?.Center ?? Projectile.Center;
            return anchor + new Vector2(-(Cols * CellSize) * 0.5f, -(Rows * CellSize) - 60f);
        }

        private static Vector2 GetCellLocalPos(int index)
            => new Vector2((index % Cols) * CellSize, (index / Cols) * CellSize);

        private bool CellIsLit(int index)
        {
            int age = Age;
            if (age < FadeInEnd)
                return false;

            // Deterministic lighting order: shuffle by a simple hash
            int lightOrder = (index * 17 + (int)Projectile.ai[0] * 7) % CellCount;
            float litByAge = (age - FadeInEnd) / 2.33f;
            return lightOrder < litByAge;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    /// <summary>矩阵面板射出的单个数据格，归向目标。</summary>
    public sealed class MatrixGridCell : ModProjectile, ILocalizedModType
    {
        private const float HomingInertia = 24f;
        private const float MaxSpeed      = 22f;
        private const int   HomingDelay   = 12;

        private int _timer;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 110;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        public override void AI()
        {
            _timer++;
            int targetIndex = (int)Projectile.ai[0];

            if (_timer <= HomingDelay)
            {
                // Free-flight: gentle wander, no homing
                float wander = (float)Math.Sin((_timer + Projectile.identity * 5f) * 0.08f) * 0.006f;
                Projectile.velocity = Projectile.velocity.RotatedBy(wander) * 0.997f;
            }
            else if (Main.npc.IndexInRange(targetIndex))
            {
                NPC target = Main.npc[targetIndex];
                if (target.CanBeChasedBy(Projectile, false))
                {
                    float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 22f, _timer, true);
                    float closePressure = Utils.GetLerpValue(500f, 80f, Projectile.Distance(target.Center), true);
                    float pull = MathHelper.Lerp(0.25f, 1f, Math.Max(warmup, closePressure * 0.75f));

                    Vector2 curVel = Projectile.velocity;
                    if (curVel.LengthSquared() < 0.01f)
                        curVel = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX) * 4f;

                    Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(curVel) * MaxSpeed;
                    Projectile.velocity = (curVel * HomingInertia + desired * pull + curVel * (1f - pull)) / (HomingInertia + 1f);

                    // Organic side-sway — creates the "lazy arc" feel
                    float sway = (float)Math.Sin((_timer + Projectile.identity * 7f) * 0.065f)
                               * MathHelper.Lerp(0.013f, 0.003f, pull);
                    Projectile.velocity = Projectile.velocity.RotatedBy(sway);

                    // Close-range burst: lances accelerate as they close in, punching through near targets
                    float maxAllowed = MaxSpeed * (1f + closePressure * 0.38f);
                    if (Projectile.velocity.Length() > maxAllowed)
                        Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * maxAllowed;
                }
                else
                {
                    // Chain redirect: switch to nearest live enemy rather than coasting to a stop
                    int bestIdx = -1;
                    float bestDist = 1100f;
                    for (int n = 0; n < Main.maxNPCs; n++)
                    {
                        NPC cand = Main.npc[n];
                        if (!cand.active || !cand.CanBeChasedBy(Projectile, false)) continue;
                        float d = Projectile.Distance(cand.Center);
                        if (d < bestDist) { bestDist = d; bestIdx = n; }
                    }
                    if (bestIdx >= 0) Projectile.ai[0] = bestIdx;
                    else Projectile.velocity *= 0.994f;
                }
            }
            else
            {
                Projectile.velocity *= 0.996f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.07f).ToVector3() * 0.28f);

            // Particle trail
            if (!Main.dedServ && _timer % 3 == 0)
            {
                Color tc = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.073f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, -Projectile.velocity * 0.18f, false, 5, 0.38f, tc, true, false, false));
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // Data siphon: occasional life steal
            if (Projectile.owner == Main.myPlayer && Main.rand.NextBool(3))
            {
                Player owner = Main.player[Projectile.owner];
                owner.statLife = Math.Min(owner.statLife + 1, owner.statLifeMax2);
                owner.HealEffect(1);
            }
            // Impact spark burst — visually punchy feedback on every hit
            if (!Main.dedServ)
            {
                Color hitColor = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.073f);
                for (int i = 0; i < 7; i++)
                {
                    Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 11f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        target.Center, vel, false, 5 + Main.rand.Next(8), 0.5f, hitColor, true, false, i < 3));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color baseColor = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.073f);
            Vector2 center = Projectile.Center;
            float speed = Projectile.velocity.Length();
            Vector2 dir = speed > 0.1f ? Projectile.velocity.SafeNormalize(Vector2.UnitX) : Vector2.UnitX;
            Vector2 perp = dir.RotatedBy(MathHelper.PiOver2);

            // Lance shaft: thick, brightly fading trail
            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;
                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 a = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 b = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f;
                Main.spriteBatch.DrawLineBetter(a, b, baseColor * (pct * 0.70f), 2.8f + pct * 2.2f);
            }

            // V-arrowhead tip pointing in the direction of travel
            float tipLen = 14f;
            float tipWidth = 6.5f;
            Vector2 tip       = center + dir * tipLen * 0.6f;
            Vector2 leftWing  = center - dir * tipLen * 0.3f + perp * tipWidth;
            Vector2 rightWing = center - dir * tipLen * 0.3f - perp * tipWidth;
            Main.spriteBatch.DrawLineBetter(leftWing,  tip, baseColor, 2.2f);
            Main.spriteBatch.DrawLineBetter(rightWing, tip, baseColor, 2.2f);
            Main.spriteBatch.DrawLineBetter(leftWing, rightWing, baseColor * 0.28f, 1.2f);

            // Bright tip node + soft outer halo
            HyperdimensionalMatrixVisuals.DrawNode(tip, baseColor,          6.5f);
            HyperdimensionalMatrixVisuals.DrawNode(tip, baseColor * 0.20f, 16f);

            // Three fading echo nodes along the shaft wake
            for (int w = 1; w <= 3; w++)
            {
                float wPct = w / 4f;
                Vector2 wPos = center - dir * (tipLen * 0.6f + wPct * 30f);
                HyperdimensionalMatrixVisuals.DrawNode(wPos, baseColor * ((1f - wPct) * 0.38f), 3.5f - w * 0.6f);
            }

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color c = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.073f);
            for (int i = 0; i < 3; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 5 + Main.rand.Next(6), 0.5f, c, true, false, false));
            }
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE 2 · GEOMETRY PROJECTION BURST
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 几何投影模块：在核心位置构建不断旋转的几何体，
    /// 展开后爆炸——每条边化作一条 MatrixGeoShard 激光。
    /// </summary>
    public sealed class MatrixGeoBurst : ModProjectile, ILocalizedModType
    {
        private const int BuildEnd   = 65;
        private const int FlashEnd   = 80;
        private const int Lifetime   = 85;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[0] = coreWhoAmI, ai[1] = targetIndex
        // localAI[0] = shapeIndex (set on spawn via identity)
        private int ShapeIndex => (int)(Projectile.identity % 4);
        private int TargetIndex => (int)Projectile.ai[1];
        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;
            // Particle burst: all clients see it
            if (age == FlashEnd && !Main.dedServ)
                SpawnExplosionParticles();
            // Spawn shards at the explosion frame (owner only)
            if (age == FlashEnd && Main.myPlayer == Projectile.owner)
                SpawnShards();

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.01f).ToVector3() * 0.35f);
        }

        private void SpawnExplosionParticles()
        {
            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 9f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.055f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 12 + Main.rand.Next(14),
                    0.6f + Main.rand.NextFloat(0.45f), c, true, false, i < 5));
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.1f + 0.05f);
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    Projectile.Center, vel, false, 26, 1.5f + Main.rand.NextFloat(0.9f), c * 1.6f));
            }
            Color burstColor = HyperdimensionalMatrixVisuals.GetDataColor(Main.GlobalTimeWrappedHourly * 0.55f);
            CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(Projectile.Center, burstColor);
            CLCBLightingBoltsSystem.Spawn_GaussDischargeShards(Projectile.Center);
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, burstColor, 0.7f);
        }

        private void SpawnShards()
        {
            float time = Main.GlobalTimeWrappedHourly;
            MatrixGeometryShape shape = ShapeIndex switch
            {
                0 => MatrixGeometryShape.Tetrahedron,
                1 => MatrixGeometryShape.Icosahedron,
                2 => MatrixGeometryShape.Cube,
                _ => MatrixGeometryShape.Icosahedron
            };

            Vector2[] vertices = HyperdimensionalMatrixVisuals.GetProjectedVertices(
                shape, Projectile.Center, 62f, time, Projectile.identity);

            NPC target = GetTarget();
            Vector2 aimPos = target?.Center ?? Projectile.Center + Vector2.UnitY * 200f;

            foreach (Vector2 v in vertices)
            {
                Vector2 dir = (v - Projectile.Center).SafeNormalize(
                    (aimPos - Projectile.Center).SafeNormalize(Vector2.UnitY));

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + dir * 8f,
                    dir * 28f,
                    ModContent.ProjectileType<MatrixGeoShard>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner,
                    TargetIndex);
            }

            // Hypercube variant also spawns 8 extra shards
            if (ShapeIndex == 3)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 dir = (MathHelper.TwoPi * i / 8f).ToRotationVector2();
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center + dir * 8f,
                        dir * 24f,
                        ModContent.ProjectileType<MatrixGeoShard>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        TargetIndex);
                }
            }

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.36f, Pitch = 0.15f, MaxInstances = 4 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float time = Main.GlobalTimeWrappedHourly;
            float buildPct = MathHelper.Clamp(age / (float)BuildEnd, 0f, 1f);
            float explodePct = age >= FlashEnd ? (age - FlashEnd) / (float)(Lifetime - FlashEnd) : 0f;
            float opacity = buildPct * (1f - explodePct);

            // Grow from 0 to full during build phase, then flash-expand on explosion
            float radius = age < FlashEnd
                ? 62f * buildPct
                : 62f + explodePct * 80f;

            MatrixGeometryShape shape = ShapeIndex switch
            {
                0 => MatrixGeometryShape.Tetrahedron,
                1 => MatrixGeometryShape.Icosahedron,
                2 => MatrixGeometryShape.Cube,
                _ => MatrixGeometryShape.Icosahedron
            };

            HyperdimensionalMatrixVisuals.DrawGeometry(
                Projectile.Center, shape, radius, time * 1.4f, opacity, Projectile.identity);

            // Hypercube overlay for variant 3
            if (ShapeIndex == 3)
                HyperdimensionalMatrixVisuals.DrawHypercube(Projectile.Center, radius * 0.55f, time, opacity * 0.75f);

            // Pre-explosion convergence scan ring
            if (age > BuildEnd * 0.7f)
            {
                float pulse = 0.5f + 0.5f * (float)Math.Sin(time * 12f);
                HyperdimensionalMatrixVisuals.DrawScanRing(
                    Projectile.Center, radius * 1.25f, time * 2f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.85f, opacity * pulse), 16, 2.5f);
            }

            return false;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    /// <summary>几何爆炸射出的单条边线，高速穿透弹。</summary>
    public sealed class MatrixGeoShard : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 80;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.05f).ToVector3() * 0.25f);
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            Color c = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.053f);
            for (int i = 0; i < 4; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 6 + Main.rand.Next(7), 0.5f, c, true, false, false));
            }
            if (Main.rand.NextBool())
            {
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    Projectile.Center, Projectile.velocity * 0.3f, false, 22,
                    1.3f + Main.rand.NextFloat(0.5f), c * 1.5f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.053f);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero || Projectile.oldPos[i - 1] == Vector2.Zero)
                    continue;

                float pct = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 a = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 b = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f;
                Main.spriteBatch.DrawLineBetter(a, b, color * (pct * 0.5f), pct * 3.5f);
            }

            // Head: a glowing short line segment representing a geometry edge
            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 18f, Projectile.Center + forward * 8f, color * 0.28f, 8f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 18f, Projectile.Center + forward * 8f, color, 1.8f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 18f, Projectile.Center + forward * 8f, Color.White with { A = 0 }, 0.8f);

            return false;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE 3 · ENERGY ORBS
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 能量光球模块：五个风格各异的光球绕目标轨道飞行，
    /// 各自持续释放环境粒子，消亡时爆炸为弹幕+粒子。
    /// </summary>
    public sealed class MatrixShaderOrb : ModProjectile, ILocalizedModType
    {
        private const float OrbitRadius = 112f;
        private const int Lifetime = 125;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        // ai[0] = orbType (0-4), ai[1] = targetIndex
        private int OrbType => (int)Projectile.ai[0];
        private int TargetIndex => (int)Projectile.ai[1];
        private int Age => Lifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
        }

        public override bool? CanDamage() => Age < Lifetime - 10 ? false : null;

        public override void AI()
        {
            NPC target = GetTarget();
            if (target != null)
            {
                float baseAngle = MathHelper.TwoPi * OrbType / 5f;
                float angle = baseAngle + Main.GlobalTimeWrappedHourly * 1.4f;
                Projectile.Center = target.Center + angle.ToRotationVector2() * OrbitRadius;
            }

            Projectile.rotation += 0.12f;
            Lighting.AddLight(Projectile.Center, GetOrbColor().ToVector3() * 0.35f);

            // Periodic ambient particles
            if (!Main.dedServ && Age % GetEmitInterval() == 0)
                EmitAmbientParticle();
        }

        private int GetEmitInterval() => OrbType switch
        {
            0 => 7,
            1 => 10,
            2 => 6,
            3 => 12,
            4 => 9,
            _ => 10
        };

        private void EmitAmbientParticle()
        {
            switch (OrbType)
            {
                case 0: // Fire — upward sparks
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(5f),
                        new Vector2(Main.rand.NextFloat(-0.8f, 0.8f), -Main.rand.NextFloat(1f, 2.5f)),
                        false, 8 + Main.rand.Next(6), 0.35f + Main.rand.NextFloat(0.2f),
                        Color.Lerp(new Color(255, 100, 0), new Color(255, 220, 50), Main.rand.NextFloat()),
                        true, false, false));
                    break;

                case 1: // Cryo — drifting crystal flakes
                    GeneralParticleHandler.SpawnParticle(new SquareParticle(
                        Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(8f),
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.3f, 1.2f),
                        false, 18 + Main.rand.Next(8), 0.8f + Main.rand.NextFloat(0.4f),
                        new Color(80, 210, 255) * 1.2f));
                    break;

                case 2: // Chaos — random data sparks
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f),
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f),
                        false, 5 + Main.rand.Next(8), 0.4f + Main.rand.NextFloat(0.3f),
                        HyperdimensionalMatrixVisuals.GetDataColor(Main.rand.NextFloat()),
                        true, false, false));
                    break;

                case 3: // Singularity — wisps drawn inward
                    Vector2 orbOffset = Main.rand.NextVector2Unit() * Main.rand.NextFloat(30f, 45f);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + orbOffset,
                        -orbOffset.SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.8f, 1.8f),
                        false, 10 + Main.rand.Next(8), 0.4f,
                        Color.Lerp(Color.White, new Color(160, 200, 255), Main.rand.NextFloat()),
                        true, false, false));
                    break;

                case 4: // Aurora — slow upward motes
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(10f),
                        new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), -Main.rand.NextFloat(0.5f, 1.5f)),
                        false, 12 + Main.rand.Next(10), 0.4f + Main.rand.NextFloat(0.25f),
                        Color.Lerp(new Color(80, 255, 120), new Color(180, 60, 255), Main.rand.NextFloat()),
                        true, false, false));
                    break;
            }
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            // Universal white flash
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center, Vector2.Zero, false, 8, 1.8f, Color.White, true, false, true));

            switch (OrbType)
            {
                case 0: // Fire — explosive outward burst
                    for (int i = 0; i < 14; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.5f, 7f);
                        Color c = Color.Lerp(new Color(255, 80, 0), new Color(255, 210, 30), Main.rand.NextFloat());
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            Projectile.Center, vel, false, 14 + Main.rand.Next(10), 0.65f + Main.rand.NextFloat(0.4f), c, true, false, i < 3));
                    }
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f);
                        GeneralParticleHandler.SpawnParticle(new SquareParticle(
                            Projectile.Center, vel, false, 28, 1.5f + Main.rand.NextFloat(0.7f), new Color(255, 130, 30) * 1.4f));
                    }
                    break;

                case 1: // Cryo — hexagonal shard ring
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * Main.rand.NextFloat(3.5f, 5.5f);
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            Projectile.Center, vel, false, 20, 0.9f, new Color(20, 200, 255), true, false, true));
                    }
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f);
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            Projectile.Center, vel, false, 10 + Main.rand.Next(12), 0.55f, new Color(80, 235, 220), true, false, false));
                    }
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1f, 3f);
                        GeneralParticleHandler.SpawnParticle(new SquareParticle(
                            Projectile.Center, vel, false, 32, 1.6f + Main.rand.NextFloat(0.6f), new Color(20, 180, 255) * 1.5f));
                    }
                    break;

                case 2: // Chaos — data scatter explosion
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 9f);
                        Color dc = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.083f + Main.rand.NextFloat(0.2f));
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            Projectile.Center, vel, false, 8 + Main.rand.Next(14), 0.6f + Main.rand.NextFloat(0.45f), dc, true, false, i < 3));
                    }
                    for (int i = 0; i < 10; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 6f);
                        GeneralParticleHandler.SpawnParticle(new SquareParticle(
                            Projectile.Center, vel, false, 24, 1.3f + Main.rand.NextFloat(0.9f),
                            HyperdimensionalMatrixVisuals.GetDataColor(i * 0.1f) * 1.5f));
                    }
                    break;

                case 3: // Singularity — white nova
                    for (int i = 0; i < 24; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 24f).ToRotationVector2() * Main.rand.NextFloat(5f, 9f);
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            Projectile.Center, vel, false, 12 + Main.rand.Next(10),
                            0.7f + Main.rand.NextFloat(0.4f),
                            Color.Lerp(Color.White, new Color(160, 200, 255), 0.3f), true, false, i % 4 == 0));
                    }
                    // Central flash orb
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center, Vector2.Zero, false, 14, 4f, Color.White, true, false, true));
                    break;

                case 4: // Aurora — cascade
                    for (int i = 0; i < 16; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.5f, 6f);
                        Color ac = Color.Lerp(new Color(80, 255, 120), new Color(180, 60, 255), Main.rand.NextFloat());
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            Projectile.Center, vel, false, 14 + Main.rand.Next(12), 0.65f + Main.rand.NextFloat(0.4f), ac, true, false, i < 4));
                    }
                    for (int i = 0; i < 7; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 4f);
                        Color sq = i % 2 == 0 ? new Color(80, 255, 120) : new Color(180, 60, 255);
                        GeneralParticleHandler.SpawnParticle(new SquareParticle(
                            Projectile.Center, vel, false, 28, 1.5f + Main.rand.NextFloat(0.7f), sq * 1.5f));
                    }
                    break;
            }

            // CLB sparkle burst — each type has a different signature
            if (OrbType == 3)
                CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(Projectile.Center);
            else
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, GetOrbColor(), 0.78f);

            // Screen shake
            if (Main.LocalPlayer.active)
            {
                float _sd = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                if (_sd < 700f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower, 2.25f * (1f - _sd / 700f));
            }

            if (Main.myPlayer == Projectile.owner)
                SpawnOrbExplosionProjectiles(GetTarget());

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.28f, Pitch = 0.15f + OrbType * 0.08f, MaxInstances = 5 }, Projectile.Center);
        }

        private void SpawnOrbExplosionProjectiles(NPC target)
        {
            IEntitySource src = Projectile.GetSource_FromThis();
            int dmg = Projectile.damage;

            switch (OrbType)
            {
                case 0: // Fire — 8 directions
                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 18f;
                        Projectile.NewProjectile(src, Projectile.Center, vel,
                            ModContent.ProjectileType<MatrixGridCell>(), dmg, Projectile.knockBack, Projectile.owner,
                            target?.whoAmI ?? -1);
                    }
                    break;

                case 1: // Cryo — hexagonal ring
                    for (int i = 0; i < 6; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 22f;
                        Projectile.NewProjectile(src, Projectile.Center, vel,
                            ModContent.ProjectileType<MatrixGridCell>(), dmg, Projectile.knockBack, Projectile.owner,
                            target?.whoAmI ?? -1);
                    }
                    break;

                case 2: // Chaos — 12 random shots
                    for (int i = 0; i < 12; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(14f, 24f);
                        Projectile.NewProjectile(src, Projectile.Center, vel,
                            ModContent.ProjectileType<MatrixGridCell>(), dmg, Projectile.knockBack, Projectile.owner,
                            target?.whoAmI ?? -1);
                    }
                    break;

                case 3: // Singularity — focused fan toward target
                    if (target != null)
                    {
                        for (int i = 0; i < 6; i++)
                        {
                            float spread = MathHelper.ToRadians(28f) * (i / 5f - 0.5f);
                            Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY).RotatedBy(spread);
                            Projectile.NewProjectile(src, Projectile.Center, dir * 20f,
                                ModContent.ProjectileType<MatrixGridCell>(), dmg, Projectile.knockBack, Projectile.owner,
                                target.whoAmI);
                        }
                    }
                    break;

                case 4: // Aurora — 5-point star
                    for (int i = 0; i < 5; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 5f).ToRotationVector2() * 16f;
                        Projectile.NewProjectile(src, Projectile.Center, vel,
                            ModContent.ProjectileType<MatrixGridCell>(), dmg, Projectile.knockBack, Projectile.owner,
                            target?.whoAmI ?? -1);
                    }
                    break;
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Age < Lifetime - 10)
                return false;

            float cp = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - Vector2.One * 28f,
                Projectile.Center + Vector2.One * 28f,
                28f, ref cp);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float opacity = age < 10 ? age / 10f : age > Lifetime - 10 ? (Lifetime - age) / 10f : 1f;
            float t = Main.GlobalTimeWrappedHourly;
            float pulse = 0.85f + 0.15f * MathF.Sin(t * 6f + OrbType);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            Color orbColor = GetOrbColor() * opacity * pulse;

            // ── Pass 1: Additive — soft bloom glow underneath the geometry ──
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.spriteBatch.Draw(bloom, drawPos, null, orbColor * 0.65f, 0f,
                bloom.Size() * 0.5f, 0.20f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, drawPos, null, orbColor * 0.20f, t * 0.8f,
                bloom.Size() * 0.5f, 0.48f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(bloom, drawPos, null, Color.White with { A = 0 } * (opacity * 0.35f), t * 2.2f,
                bloom.Size() * 0.5f, 0.09f, SpriteEffects.None, 0f);

            // ── Pass 2: AlphaBlend — mini geometry wireframe projection ──
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            DrawMiniGeometry(opacity, t);
            DrawOrbDecorations(opacity, t);

            HyperdimensionalMatrixVisuals.DrawScanRing(
                GetTarget()?.Center ?? Projectile.Center, OrbitRadius, t * 0.4f,
                GetOrbColor() * (opacity * 0.10f), 20, 0.8f);

            return false;
        }

        private void DrawMiniGeometry(float opacity, float t)
        {
            // Each orb type projects a distinctive small wireframe geometry body
            switch (OrbType)
            {
                case 0: // Fire Matrix — tetrahedron, fast CW spin
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center, MatrixGeometryShape.Tetrahedron,
                        20f, t * 2.4f, opacity * 0.78f, Projectile.identity);
                    break;

                case 1: // Cryo Data — icosahedron, slow CCW spin
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center, MatrixGeometryShape.Icosahedron,
                        18f, -t * 1.3f, opacity * 0.72f, Projectile.identity);
                    break;

                case 2: // Chaos Matrix — nested cubes, chaotic counter-rotation
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center, MatrixGeometryShape.Cube,
                        22f, t * 3.5f, opacity * 0.82f, Projectile.identity);
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center, MatrixGeometryShape.Cube,
                        13f, -t * 2.8f + 0.6f, opacity * 0.52f, Projectile.identity + 3, false);
                    break;

                case 3: // Singularity Core — cube pulsing between large and small
                {
                    float pulseSz = 16f + 7f * MathF.Sin(t * 6.5f + Projectile.identity);
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center, MatrixGeometryShape.Cube,
                        pulseSz, t * 1.9f, opacity * 0.88f, Projectile.identity);
                    break;
                }

                case 4: // Aurora Cascade — icosahedron + inner tetrahedron counter-rotating
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center, MatrixGeometryShape.Icosahedron,
                        19f, t * 1.7f, opacity * 0.76f, Projectile.identity);
                    HyperdimensionalMatrixVisuals.DrawGeometry(
                        Projectile.Center, MatrixGeometryShape.Tetrahedron,
                        11f, -t * 2.3f, opacity * 0.48f, Projectile.identity + 5, false);
                    break;
            }
        }

        private void DrawOrbDecorations(float opacity, float t)
        {
            switch (OrbType)
            {
                case 0: // Fire Matrix — 4 rotating fire nodes + flame arc
                    for (int i = 0; i < 4; i++)
                    {
                        float angle = t * 2.8f + i * MathHelper.PiOver2 + MathHelper.Pi * 0.12f;
                        HyperdimensionalMatrixVisuals.DrawNode(
                            Projectile.Center + angle.ToRotationVector2() * 18f,
                            new Color(255, 100, 0, 0) * opacity, 4.5f);
                    }
                    HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, 24f, t * 3.2f,
                        new Color(255, 160, 0, 0) * (opacity * 0.45f), 6, 2f);
                    break;

                case 1: // Cryo Data — hexagonal crystal ring
                    HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, 28f, -t * 1.5f,
                        new Color(20, 200, 255, 0) * (opacity * 0.55f), 6, 1.8f);
                    for (int i = 0; i < 6; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 6f - t * 0.85f;
                        HyperdimensionalMatrixVisuals.DrawNode(
                            Projectile.Center + angle.ToRotationVector2() * 26f,
                            new Color(20, 200, 255, 0) * opacity, 3.5f);
                    }
                    break;

                case 2: // Chaos Matrix — multi-ring cycling hues + center node
                    for (int i = 0; i < 3; i++)
                    {
                        Color rc = HyperdimensionalMatrixVisuals.GetDataColor(t * 0.45f + i * 0.33f, opacity * 0.5f);
                        HyperdimensionalMatrixVisuals.DrawScanRing(
                            Projectile.Center, 18f + i * 9f, t * (1.3f - i * 0.4f), rc, 8 + i * 4, 1.4f);
                    }
                    HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center,
                        HyperdimensionalMatrixVisuals.GetDataColor(t * 0.6f, opacity * 0.8f), 6f);
                    break;

                case 3: // Singularity Core — inward convergence lines
                    for (int i = 0; i < 12; i++)
                    {
                        float angle = MathHelper.TwoPi * i / 12f + t * 0.45f;
                        Vector2 outer = Projectile.Center + angle.ToRotationVector2() * 32f;
                        Main.spriteBatch.DrawLineBetter(outer, Projectile.Center,
                            Color.White with { A = 0 } * (opacity * 0.20f), 1.2f);
                        HyperdimensionalMatrixVisuals.DrawNode(outer,
                            Color.White with { A = 0 } * (opacity * 0.5f), 2.5f);
                    }
                    HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, 30f, t * 2.2f,
                        Color.White with { A = 0 } * (opacity * 0.38f), 12, 1.6f);
                    break;

                case 4: // Aurora Cascade — 3 tri-orbit nodes + aurora ring
                    for (int i = 0; i < 3; i++)
                    {
                        float angle = t * (1.6f + i * 0.28f) + i * (MathHelper.TwoPi / 3f);
                        Color ac = Color.Lerp(
                            new Color(80, 255, 120, 0), new Color(180, 60, 255, 0),
                            0.5f + 0.5f * MathF.Sin(t * 2.4f + i));
                        HyperdimensionalMatrixVisuals.DrawNode(
                            Projectile.Center + angle.ToRotationVector2() * (18f + i * 7f),
                            ac * opacity, 4.5f);
                    }
                    HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, 25f, -t * 1.9f,
                        new Color(80, 255, 120, 0) * (opacity * 0.45f), 8, 1.4f);
                    break;
            }
        }

        private Color GetOrbColor()
        {
            float t = Main.GlobalTimeWrappedHourly;
            return OrbType switch
            {
                0 => Color.Lerp(new Color(255, 120, 20), new Color(255, 55, 0), 0.5f + 0.5f * MathF.Sin(t * 4f)) with { A = 0 },
                1 => Color.Lerp(new Color(20, 180, 255), new Color(80, 255, 220), 0.5f + 0.5f * MathF.Sin(t * 3f)) with { A = 0 },
                2 => HyperdimensionalMatrixVisuals.GetDataColor(t * 0.35f),
                3 => Color.Lerp(Color.White, new Color(160, 200, 255), 0.2f) with { A = 0 },
                4 => Color.Lerp(new Color(80, 255, 120), new Color(180, 60, 255), 0.5f + 0.5f * MathF.Sin(t * 1.8f)) with { A = 0 },
                _ => Color.White with { A = 0 }
            };
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE 4 · METABALL FUSION
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 流体聚合模块：8 个能量球从四面八方向目标中心聚合，
    /// 融合成超级核心后坍缩爆炸——通过叠加 Bloom 圆模拟 Metaball 效果。
    /// </summary>
    public sealed class MatrixFusionController : ModProjectile, ILocalizedModType
    {
        private const int BallCount   = 8;
        private const int ConvergeEnd = 85;
        private const int CompressEnd = 115;
        private const int Lifetime    = 125;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;
            NPC target = GetTarget();
            if (target != null)
                Projectile.Center = target.Center;

            if (age == CompressEnd && Main.myPlayer == Projectile.owner)
                SpawnExplosion();

            // Ambient particles on each converging ball
            if (!Main.dedServ && age < ConvergeEnd && age % 5 == 0)
            {
                for (int i = 0; i < BallCount; i++)
                {
                    Vector2 ballPos = GetBallPosition(i, age);
                    Color bc = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)BallCount);
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        ballPos, Main.rand.NextVector2Unit() * Main.rand.NextFloat(0.5f, 1.8f),
                        false, 8, 0.45f, bc, true, false, false));
                }
            }

            // Screen shake build-up during compress phase
            if (!Main.dedServ && age >= ConvergeEnd && age < CompressEnd && Main.LocalPlayer.active)
            {
                float mergePct = (age - ConvergeEnd) / (float)(CompressEnd - ConvergeEnd);
                float _sd = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                if (_sd < 400f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                        mergePct * 1.75f * (1f - _sd / 400f));
            }

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(age * 0.02f).ToVector3() * 0.55f);
        }

        private void SpawnExplosion()
        {
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<MatrixFusionExplosion>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndFusionBoom), Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            if (age < ConvergeEnd)
            {
                // Cache all ball positions for metaball bridge pass
                Vector2[] ballPositions = new Vector2[BallCount];
                float[] ballSizes = new float[BallCount];
                Color[] ballColors = new Color[BallCount];
                for (int i = 0; i < BallCount; i++)
                {
                    ballPositions[i] = GetBallPosition(i, age);
                    float distPct = 1f - Vector2.Distance(ballPositions[i], Projectile.Center) / GetBallInitialRadius(i);
                    ballSizes[i] = MathHelper.Lerp(0.12f, 0.38f, distPct);
                    ballColors[i] = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)BallCount);
                }

                // Pass A: metaball connection bridges between nearby balls (drawn first, under the balls)
                for (int i = 0; i < BallCount; i++)
                {
                    for (int j = i + 1; j < BallCount; j++)
                    {
                        float dist = Vector2.Distance(ballPositions[i], ballPositions[j]);
                        if (dist > 90f) continue;
                        float bridgeAlpha = MathHelper.Lerp(0.55f, 0f, dist / 90f);
                        Color bridgeColor = Color.Lerp(ballColors[i], ballColors[j], 0.5f) * bridgeAlpha;
                        // Stretched bloom along the bridge midpoint — simulates metaball neck
                        Vector2 mid = (ballPositions[i] + ballPositions[j]) * 0.5f - Main.screenPosition;
                        float bridgeScale = (ballSizes[i] + ballSizes[j]) * 0.35f * (1f - dist / 90f);
                        float bridgeRot = (ballPositions[j] - ballPositions[i]).ToRotation();
                        Main.spriteBatch.Draw(bloom, mid, null, bridgeColor, bridgeRot,
                            bloom.Size() * 0.5f, new Vector2(dist / bloom.Width * 0.6f, bridgeScale),
                            SpriteEffects.None, 0f);
                    }
                }

                // Pass B: the balls themselves
                for (int i = 0; i < BallCount; i++)
                {
                    Vector2 ballScreen = ballPositions[i] - Main.screenPosition;
                    Main.spriteBatch.Draw(bloom, ballScreen, null, ballColors[i], t * 0.8f + i, bloom.Size() * 0.5f, ballSizes[i], SpriteEffects.None, 0f);
                    Main.spriteBatch.Draw(bloom, ballScreen, null, ballColors[i] * 0.35f, -t * 0.5f, bloom.Size() * 0.5f, ballSizes[i] * 1.6f, SpriteEffects.None, 0f);
                }
            }
            else if (age < CompressEnd)
            {
                // Merge: single growing mass
                float mergePct = (age - ConvergeEnd) / (float)(CompressEnd - ConvergeEnd);
                float coreScale = MathHelper.Lerp(0.35f, 0.95f, mergePct);
                float pulse = 1f + 0.12f * (float)Math.Sin(t * 14f);
                Color mergeColor = HyperdimensionalMatrixVisuals.GetDataColor(t * 0.3f);

                Main.spriteBatch.Draw(bloom, center, null, mergeColor, t, bloom.Size() * 0.5f, coreScale * pulse, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(bloom, center, null, mergeColor * 0.4f, -t * 0.6f, bloom.Size() * 0.5f, coreScale * 1.9f * pulse, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(bloom, center, null, Color.White with { A = 0 } * 0.3f, t * 2f, bloom.Size() * 0.5f, coreScale * 0.4f, SpriteEffects.None, 0f);

                HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, coreScale * 95f * pulse, t * 2f,
                    mergeColor, 20, 2f);
            }
            else
            {
                // Flash before explosion
                float flashPct = (age - CompressEnd) / (float)(Lifetime - CompressEnd);
                Color flashColor = Color.White with { A = 0 };
                Main.spriteBatch.Draw(bloom, center, null, flashColor * (1f - flashPct), 0f, bloom.Size() * 0.5f, flashPct * 2.5f, SpriteEffects.None, 0f);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            // AlphaBlend pass: mini geometry wireframes on each converging ball (no state switching in loop)
            if (age < ConvergeEnd)
            {
                for (int i = 0; i < BallCount; i++)
                {
                    Vector2 bp = GetBallPosition(i, age);
                    float sz = MathHelper.Lerp(0.12f, 0.38f, 1f - Vector2.Distance(bp, Projectile.Center) / GetBallInitialRadius(i));
                    if (sz > 0.16f)
                    {
                        HyperdimensionalMatrixVisuals.DrawGeometry(
                            bp, MatrixGeometryShape.Tetrahedron,
                            10f + sz * 20f, t * (1.7f + i * 0.20f), sz * 1.5f, i + 100);
                    }
                }
            }

            return false;
        }

        private Vector2 GetBallPosition(int index, int age)
        {
            float convergeT = Math.Min(age / (float)ConvergeEnd, 1f);
            float eased = convergeT * convergeT * (3f - 2f * convergeT); // smoothstep
            float angle = MathHelper.TwoPi * index / BallCount + index * 0.42f + (int)Projectile.ai[0] * 0.07f;
            float radius = GetBallInitialRadius(index);
            return Projectile.Center + angle.ToRotationVector2() * (radius * (1f - eased));
        }

        private static float GetBallInitialRadius(int index)
            => 90f + (index * 23 % 7) * 12f;

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    /// <summary>聚合爆炸：膨胀圆形伤害场 + 数据外爆特效。</summary>
    public sealed class MatrixFusionExplosion : ModProjectile, ILocalizedModType
    {
        private const float MaxRadius = 155f;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Completion => 1f - Projectile.timeLeft / 26f;
        private float Radius => MaxRadius * (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(Completion, 0f, 1f));

        public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = (int)(MaxRadius * 2f);
            Projectile.height = (int)(MaxRadius * 2f);
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 26;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 26; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 11f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.038f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 16 + Main.rand.Next(18),
                    0.6f + Main.rand.NextFloat(0.5f), c, true, false, i < 6));
            }
            for (int i = 0; i < 18; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.5f, 8f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.055f + 0.1f);
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    Projectile.Center, vel, false, 30, 1.6f + Main.rand.NextFloat(1.1f), c * 1.5f));
            }
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                Projectile.Center, Vector2.Zero, false, 12, 3.5f, Color.White, true, false, true));

            Color fuseColor = HyperdimensionalMatrixVisuals.GetDataColor(Main.GlobalTimeWrappedHourly * 0.4f);
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, fuseColor, 1.4f);
            CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(Projectile.Center, fuseColor);

            if (Main.LocalPlayer.active)
            {
                float _sd = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                if (_sd < 850f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower, 4f * (1f - _sd / 850f));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float r = Radius;
            Vector2 closest = new(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= r * r;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float c = MathHelper.Clamp(Completion, 0f, 1f);
            float r = Radius;
            Color color = HyperdimensionalMatrixVisuals.GetDataColor(c * 0.5f, 1f - c);

            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, r, c * 4f, color, 40, 4.5f);
            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, r * 0.65f, -c * 6f, color * 0.55f, 30, 2.5f);

            for (int i = 0; i < 16; i++)
            {
                Vector2 dir = (MathHelper.TwoPi * i / 16f).ToRotationVector2();
                Main.spriteBatch.DrawLineBetter(
                    Projectile.Center + dir * r * 0.2f,
                    Projectile.Center + dir * r,
                    color * 0.5f, 2f);
            }

            return false;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE 5 · SPACE WARP
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 空间扭曲模块：对大型敌人触发。
    /// 激活全屏 BlackHoleDistortion 并在目标周围绘制扭曲环。
    /// 同时进行连续小伤害（扭曲能量）。
    /// </summary>
    public sealed class MatrixSpaceWarpField : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 85;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC target = GetTarget();
            if (target != null)
                Projectile.Center = target.Center;

            // Periodic screen shake during warp
            int _age = Age;
            if (!Main.dedServ && _age % 25 == 0 && Main.LocalPlayer.active)
            {
                float _sd = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                if (_sd < 500f)
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower, 1.25f * (1f - _sd / 500f));
            }

            Lighting.AddLight(Projectile.Center, new Vector3(0.15f, 0.05f, 0.3f) * 0.5f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            NPC target = GetTarget();
            if (target == null)
                return false;

            float r = Math.Max(target.width, target.height) * 0.6f + 30f;
            Vector2 closest = new(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= r * r;
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetIndex ? null : false;

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
            {
                Color warpColor = new Color(180, 60, 255);
                CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, warpColor, 1f);
                CLCBLightingBoltsSystem.Spawn_GaussSingularityPulse(Projectile.Center);

                if (Main.LocalPlayer.active)
                {
                    float _sd = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                    if (_sd < 700f)
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                            Main.LocalPlayer.Calamity().GeneralScreenShakePower, 2.5f * (1f - _sd / 700f));
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float fade = age < 12 ? age / 12f : age > Lifetime - 12 ? (Lifetime - age) / 12f : 1f;
            float t = Main.GlobalTimeWrappedHourly;
            NPC target = GetTarget();
            float targetR = target != null
                ? Math.Max(target.width, target.height) * 0.6f + 40f
                : 80f;

            Color distortColor = new Color(180, 80, 255, 0) * fade;
            Color edgeColor = HyperdimensionalMatrixVisuals.GetDataColor(0.75f, fade * 0.7f);

            // Concentric distortion rings
            for (int ring = 0; ring < 4; ring++)
            {
                float ringR = targetR * (0.5f + ring * 0.22f);
                float rot = t * (ring % 2 == 0 ? 0.8f : -1.1f) + ring * 0.5f;
                HyperdimensionalMatrixVisuals.DrawScanRing(
                    Projectile.Center, ringR, rot,
                    Color.Lerp(distortColor, edgeColor, ring / 3f), 20 + ring * 6, 1.5f + ring * 0.4f);
            }

            // Radial distortion spokes
            for (int i = 0; i < 12; i++)
            {
                float angle = MathHelper.TwoPi * i / 12f + t * 0.6f;
                float spoke = targetR * (0.85f + 0.15f * (float)Math.Sin(t * 5f + i));
                Vector2 dir = angle.ToRotationVector2();
                Main.spriteBatch.DrawLineBetter(
                    Projectile.Center + dir * spoke * 0.3f,
                    Projectile.Center + dir * spoke,
                    distortColor * 0.4f, 1.5f);
                HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center + dir * spoke, edgeColor, 4f);
            }

            return false;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // PASSIVE · DATA FIELD AURA
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 被动数据场：跟随核心，对周围敌人造成持续轻微伤害。
    /// 每120帧对同一敌人最多触发一次。
    /// </summary>
    public sealed class MatrixDataAura : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int CoreWhoAmI => (int)Projectile.ai[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
        }

        public override void SetDefaults()
        {
            Projectile.width  = 480;
            Projectile.height = 480;
            Projectile.friendly     = true;
            Projectile.ignoreWater  = true;
            Projectile.tileCollide  = false;
            Projectile.penetrate    = -1;
            Projectile.timeLeft     = int.MaxValue / 2;
            Projectile.DamageType   = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity   = true;
            Projectile.localNPCHitCooldown    = 120;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => true;

        public override void AI()
        {
            int coreId = CoreWhoAmI;
            if (!Main.projectile.IndexInRange(coreId)
                || !Main.projectile[coreId].active
                || Main.projectile[coreId].type != ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>())
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = Main.projectile[coreId].Center;
            Projectile.damage = Math.Max(1, (int)(Main.projectile[coreId].damage * 0.10f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ) return;
            Color c = HyperdimensionalMatrixVisuals.GetDataColor(
                Main.GlobalTimeWrappedHourly * 0.55f + target.whoAmI * 0.14f);
            for (int i = 0; i < 5; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.5f, 5f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    target.Center + Main.rand.NextVector2Circular(target.width * 0.35f, target.height * 0.35f),
                    vel, false, 8 + Main.rand.Next(10), 0.38f, c, true, false, false));
            }
        }
    }

    // ──────────────────────────────────────────────────────
    // SPECIAL · COMPILE STORM
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 编译风暴：所有模块同时启动。
    /// 阶段1 能量汇聚 → 阶段2 同时引爆所有模块 → 阶段3 奇点。
    /// </summary>
    public sealed class MatrixCompileStorm : ModProjectile, ILocalizedModType
    {
        private const int Phase1End  = 60;   // buildup
        private const int Phase2Fire = 62;   // spawn all modules
        private const int Phase3Sing = 95;   // spawn singularity
        private const int Lifetime   = 155;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        // ai[0] = coreWhoAmI, ai[1] = targetIndex
        private int CoreIndex  => (int)Projectile.ai[0];
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;
            NPC target = GetTarget();

            // Lock to core position during buildup
            if (age < Phase1End)
            {
                Projectile coreProj = GetCore();
                if (coreProj != null)
                    Projectile.Center = Vector2.Lerp(Projectile.Center, coreProj.Center, 0.15f);
            }

            // Fire all modules simultaneously
            if (age == Phase2Fire && Main.myPlayer == Projectile.owner && target != null)
            {
                IEntitySource src = Projectile.GetSource_FromThis();
                int dmg = Projectile.damage;
                float kb = Projectile.knockBack;
                int owner = Projectile.owner;
                int tIdx = target.whoAmI;

                // Module 1: Data Grid
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixDataGridPanel>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.DataGridDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 2: Geometry Burst (3 shapes at once)
                for (int s = 0; s < 3; s++)
                {
                    Vector2 offset = (MathHelper.TwoPi * s / 3f).ToRotationVector2() * 55f;
                    Projectile.NewProjectile(src, Projectile.Center + offset, Vector2.Zero,
                        ModContent.ProjectileType<MatrixGeoBurst>(),
                        Math.Max(1, (int)(dmg * MatrixModuleNumbers.GeoBurstDamage)), kb, owner,
                        Projectile.whoAmI, tIdx);
                }

                // Module 3: All 5 shader orbs
                for (int i = 0; i < 5; i++)
                {
                    Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                        ModContent.ProjectileType<MatrixShaderOrb>(),
                        Math.Max(1, (int)(dmg * MatrixModuleNumbers.ShaderOrbDamage)), kb, owner,
                        i, tIdx);
                }

                // Module 4: Fusion
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixFusionController>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.FusionDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 5: Runic Inscription
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixRunicStamp>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.InscriptionDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 6: Möbius Data Ring
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixMobiusRing>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.MobiusRingDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 7: Fractal Tree
                Projectile.NewProjectile(src, Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixFractalTree>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.FractalTreeDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 8: Voronoi Shatter
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixVoronoiShatter>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.VoronoiDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 9: Torus Knot
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixTorusKnot>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.TorusKnotDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 10: Lorenz Attractor
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixLorenzSwarm>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.LorenzDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 11: Fibonacci Spiral
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixFibonacciSpiral>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.FibonacciDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 12: Penrose Tiling Collapse
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixPenroseTiling>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.PenroseDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 13: Paraboloid Warp
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixParaboloidWarp>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.ParaboloidDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 14: Clifford Torus
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixCliffordTorus>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.CliffordDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 15: Superformula Morph
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixSuperformulaMorph>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.SuperformulaDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                // Module 16: Sierpinski Collapse
                Projectile.NewProjectile(src, target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixSierpinskiCollapse>(),
                    Math.Max(1, (int)(dmg * MatrixModuleNumbers.SierpinskiDamage)), kb, owner,
                    Projectile.whoAmI, tIdx);

                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndCompileStorm), target.Center);

                if (!Main.dedServ && Main.LocalPlayer.active)
                {
                    float _sd = Vector2.Distance(Main.LocalPlayer.Center, target.Center);
                    if (_sd < 1200f)
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                            Main.LocalPlayer.Calamity().GeneralScreenShakePower, 6f * (1f - _sd / 1200f));

                    Color stormColor = HyperdimensionalMatrixVisuals.GetDataColor(Main.GlobalTimeWrappedHourly * 0.55f);
                    CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(target.Center, stormColor, 1.2f);
                    CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(target.Center, stormColor);
                }
            }

            // Spawn singularity
            if (age == Phase3Sing && Main.myPlayer == Projectile.owner && target != null)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center, Vector2.Zero,
                    ModContent.ProjectileType<MatrixSingularity>(),
                    Projectile.damage * 6,
                    Projectile.knockBack,
                    Projectile.owner);

                SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndSingularity), target.Center);

                if (!Main.dedServ && Main.LocalPlayer.active)
                {
                    float _sd = Vector2.Distance(Main.LocalPlayer.Center, target.Center);
                    if (_sd < 1500f)
                        Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                            Main.LocalPlayer.Calamity().GeneralScreenShakePower, 8f * (1f - _sd / 1500f));

                    CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(target.Center);
                    CLCBLightingBoltsSystem.Spawn_GaussSingularityPulse(target.Center);
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            Projectile coreProj = GetCore();
            NPC target = GetTarget();
            if (coreProj == null && target == null)
                return false;

            Vector2 corePos = coreProj?.Center ?? Projectile.Center;
            Vector2 tgtPos = target?.Center ?? Projectile.Center;

            if (age < Phase1End)
            {
                // Phase 1: data streams converging from all directions toward target
                float buildPct = age / (float)Phase1End;
                for (int i = 0; i < 18; i++)
                {
                    float angle = MathHelper.TwoPi * i / 18f + t * 0.4f;
                    float streamLen = MathHelper.Lerp(400f, 50f, buildPct * buildPct);
                    Vector2 streamStart = tgtPos + angle.ToRotationVector2() * streamLen;
                    Color streamColor = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.055f, buildPct * 0.65f);
                    Main.spriteBatch.DrawLineBetter(streamStart, tgtPos, streamColor, 1.5f);

                    if (i % 3 == 0)
                    {
                        float nodeT = ((t * 1.8f + i * 0.2f) % 1f);
                        HyperdimensionalMatrixVisuals.DrawNode(Vector2.Lerp(streamStart, tgtPos, nodeT), streamColor, 4f);
                    }
                }

                // Expanding scan rings around core
                float buildR = 60f + buildPct * 120f;
                HyperdimensionalMatrixVisuals.DrawScanRing(corePos, buildR, t * 1.6f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.12f, buildPct * 0.5f), 28, 2f);
                HyperdimensionalMatrixVisuals.DrawScanRing(corePos, buildR * 1.3f, -t * 1.2f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.62f, buildPct * 0.35f), 22, 1.5f);

                // Targeting line (bright during storm)
                HyperdimensionalMatrixVisuals.DrawTargetingLine(corePos, tgtPos, buildPct * 0.7f);

                // 矩阵UI — holographic compile status panel floating between core and target
                DrawCompilePanel(corePos, tgtPos, buildPct, t);
            }
            else if (age >= Phase2Fire && age < Phase3Sing)
            {
                // Phase 2: everything exploding — just draw connecting arcs
                float pct = (age - Phase2Fire) / (float)(Phase3Sing - Phase2Fire);
                for (int i = 0; i < 6; i++)
                {
                    Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.16f, (1f - pct) * 0.5f);
                    HyperdimensionalMatrixVisuals.DrawScanRing(tgtPos, pct * 200f, t * 2f + i, c, 16, 2f);
                }
            }
            else if (age >= Phase3Sing)
            {
                // Phase 3: singularity forming — draw nothing extra; MatrixSingularity handles itself
            }

            return false;
        }

        private void DrawCompilePanel(Vector2 corePos, Vector2 tgtPos, float buildPct, float t)
        {
            // Holographic module-compile status panel floating above the midpoint
            Vector2 panelCenter = Vector2.Lerp(corePos, tgtPos, 0.38f) + new Vector2(0f, -90f);

            const float cellSize = 4.2f;
            const float gap      = 1.5f;
            const int   cols     = 17;   // one column per module
            const int   rows     = 4;
            float panelW = cols * cellSize + (cols - 1) * gap;
            float panelH = rows * cellSize + (rows - 1) * gap;

            Vector2 topLeft  = panelCenter - new Vector2(panelW * 0.5f, panelH * 0.5f);
            Vector2 botRight = panelCenter + new Vector2(panelW * 0.5f, panelH * 0.5f);

            Color frameColor = HyperdimensionalMatrixVisuals.GetDataColor(0.28f, buildPct * 0.65f);

            // Corner brackets
            float bl = 9f;
            void Corner(Vector2 a, float sx, float sy)
            {
                Main.spriteBatch.DrawLineBetter(a, a + Vector2.UnitX * (bl * sx), frameColor, 1.5f);
                Main.spriteBatch.DrawLineBetter(a, a + Vector2.UnitY * (bl * sy), frameColor, 1.5f);
            }
            Corner(topLeft,                          1f,  1f);
            Corner(new Vector2(botRight.X, topLeft.Y), -1f,  1f);
            Corner(new Vector2(topLeft.X, botRight.Y),  1f, -1f);
            Corner(botRight,                         -1f, -1f);

            // Module cell grid: rows fill from bottom as buildPct rises
            for (int m = 0; m < cols; m++)
            {
                Color mColor = GetPanelModuleColor(m, t);
                float cx = topLeft.X + m * (cellSize + gap) + cellSize * 0.5f;

                for (int r = 0; r < rows; r++)
                {
                    float threshold = (rows - 1 - r) / (float)(rows - 1); // 0=top, 1=bottom
                    float mOffset = m * (1f / (cols * 6f));                // stagger per module
                    bool lit = buildPct > threshold * 0.9f + mOffset;
                    float cellAlpha = lit
                        ? MathHelper.Lerp(0.55f, 1f, buildPct) * (0.78f + 0.22f * MathF.Sin(t * 9f + m * 1.4f))
                        : 0.12f;
                    float cy = topLeft.Y + r * (cellSize + gap) + cellSize * 0.5f;
                    HyperdimensionalMatrixVisuals.DrawNode(
                        new Vector2(cx, cy), mColor * cellAlpha, cellSize * 0.85f);
                }

                // Module ready indicator dot below grid
                bool ready = buildPct > 0.85f + m * 0.02f;
                float dotSize = ready
                    ? 4f + 2f * MathF.Sin(t * 12f + m)
                    : 2f;
                HyperdimensionalMatrixVisuals.DrawNode(
                    new Vector2(cx, botRight.Y + 7f), mColor * (ready ? buildPct : buildPct * 0.3f), dotSize);
            }

            // Horizontal scan line sweeping across the panel
            float scanX = topLeft.X + panelW * ((t * 0.72f) % 1f);
            Main.spriteBatch.DrawLineBetter(
                new Vector2(scanX, topLeft.Y - 2f),
                new Vector2(scanX, botRight.Y + 2f),
                frameColor * 0.38f, 1f);

            // Subtle outer ring
            HyperdimensionalMatrixVisuals.DrawScanRing(
                panelCenter, Math.Max(panelW, panelH) * 0.72f + 6f, t * 0.55f,
                frameColor * 0.22f, 10, 1f);
        }

        private static Color GetPanelModuleColor(int m, float t) => m switch
        {
            0 => new Color(255, 100, 30,  0),
            1 => new Color(40,  200, 255, 0),
            2 => HyperdimensionalMatrixVisuals.GetDataColor(t * 0.35f),
            3 => new Color(200, 220, 255, 0),
            4 => new Color(80,  255, 120, 0),
            5 => new Color(255, 200, 255, 0),
            6 => new Color(180, 255, 200, 0),
            7 => new Color(120, 255, 80,  0),
            8 => new Color(255, 160, 200, 0),
            9 => new Color(255, 220, 100, 0),
            10 => new Color(200, 120, 255, 0),
            11 => new Color(255, 200, 60,  0),
            12 => new Color(100, 180, 255, 0),
            13 => new Color(255, 100, 100, 0),
            14 => new Color(150, 200, 255, 0),
            15 => new Color(200, 255, 100, 0),
            16 => new Color(255, 100, 255, 0),
            _ => Color.White with { A = 0 }
        };

        private Projectile GetCore()
        {
            if (!Main.projectile.IndexInRange(CoreIndex))
                return null;

            Projectile p = Main.projectile[CoreIndex];
            return p.active && p.type == ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>()
                ? p : null;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;

            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    // ──────────────────────────────────────────────────────
    // MODULE 6 · RUNIC INSCRIPTION
    // ──────────────────────────────────────────────────────

    /// <summary>
    /// 超维刻印：在目标位置逐笔描绘一个七芒星阵，完成后从每个顶点射出非追踪几何碎片，
    /// 并在中心释放一枚追踪数据矛。描绘笔画实时可见，是矩阵运算过程的直观化表达。
    /// </summary>
    public sealed class MatrixRunicStamp : ModProjectile, ILocalizedModType
    {
        private const int DrawEnd   = 60;  // 七芒星完全描绘完毕
        private const int FlashEnd  = 76;  // 闪烁/脉冲阶段
        private const int FireFrame = 77;  // 射出碎片
        private const int Lifetime  = 105;

        private const float StampRadius = 82f;
        private const int   VertexCount = 7;

        // {7/2} 七芒星描绘顺序：0→2→4→6→1→3→5→0（连续单笔画完）
        private static readonly int[] StarTrace = { 0, 2, 4, 6, 1, 3, 5 };

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private int TargetIndex => (int)Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int age = Age;
            NPC target = GetTarget();
            if (target != null)
                Projectile.Center = target.Center;

            if (age == FireFrame && !Main.dedServ)
                SpawnFireParticles();
            if (age == FireFrame && Main.myPlayer == Projectile.owner)
                FireInscriptionShards();

            Lighting.AddLight(Projectile.Center,
                HyperdimensionalMatrixVisuals.GetDataColor(age * 0.016f).ToVector3() * 0.42f);
        }

        private static Vector2[] GetStarVerts(Vector2 center, float rotation)
        {
            var verts = new Vector2[VertexCount];
            for (int i = 0; i < VertexCount; i++)
                verts[i] = center + (MathHelper.TwoPi * i / VertexCount + rotation).ToRotationVector2() * StampRadius;
            return verts;
        }

        private void FireInscriptionShards()
        {
            float rot = Main.GlobalTimeWrappedHourly * 0.3f;
            Vector2[] verts = GetStarVerts(Projectile.Center, rot);
            IEntitySource src = Projectile.GetSource_FromThis();

            // 7枚非追踪碎片从各顶点射出（非追踪 → 双倍伤害奖励已计入 InscriptionDamage）
            for (int i = 0; i < VertexCount; i++)
            {
                Vector2 dir = (verts[i] - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.NewProjectile(src, verts[i], dir * 26f,
                    ModContent.ProjectileType<MatrixGeoShard>(),
                    Projectile.damage, Projectile.knockBack, Projectile.owner, -1);
            }

            // 1枚追踪数据矛从中心射出
            NPC target = GetTarget();
            if (target != null)
            {
                Vector2 dir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.NewProjectile(src, Projectile.Center, dir * 20f,
                    ModContent.ProjectileType<MatrixGridCell>(),
                    Math.Max(1, (int)(Projectile.damage * 0.6f)),
                    Projectile.knockBack, Projectile.owner, target.whoAmI);
            }

            SoundEngine.PlaySound(new SoundStyle(MatrixModuleNumbers.SndInscFire), Projectile.Center);
        }

        private void SpawnFireParticles()
        {
            for (int i = 0; i < 24; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 10f);
                Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.042f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center, vel, false, 10 + Main.rand.Next(14),
                    0.55f + Main.rand.NextFloat(0.45f), c, true, false, i < 5));
            }
            for (int i = 0; i < 10; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 7f);
                GeneralParticleHandler.SpawnParticle(new SquareParticle(
                    Projectile.Center, vel, false, 28, 1.4f + Main.rand.NextFloat(0.9f),
                    HyperdimensionalMatrixVisuals.GetDataColor(i * 0.1f) * 1.5f));
            }
            Color burstColor = HyperdimensionalMatrixVisuals.GetDataColor(Main.GlobalTimeWrappedHourly * 0.5f);
            CLCBLightingBoltsSystem.Spawn_MatrixGeometryShatter(Projectile.Center, burstColor);
            CLCBLightingBoltsSystem.Spawn_MatrixDataBurst(Projectile.Center, burstColor, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            float rot = t * 0.3f;
            Vector2[] verts = GetStarVerts(Projectile.Center, rot);

            float fadeOpacity = age < 12 ? age / 12f : age > Lifetime - 14 ? (Lifetime - age) / 14f : 1f;
            float flashPulse  = age >= DrawEnd ? 1f + 0.45f * MathF.Sin(t * 15f) : 1f;

            // 七芒星逐笔描绘
            float edgeProgress = age < DrawEnd
                ? age / (float)DrawEnd * VertexCount
                : VertexCount;

            for (int i = 0; i < VertexCount; i++)
            {
                float edgePct = MathHelper.Clamp(edgeProgress - i, 0f, 1f);
                if (edgePct <= 0f)
                    break;

                int fromIdx = StarTrace[i];
                int toIdx   = StarTrace[(i + 1) % VertexCount];
                Vector2 edgeStart = verts[fromIdx];
                Vector2 edgeEnd   = Vector2.Lerp(verts[fromIdx], verts[toIdx], edgePct);

                Color edgeColor = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)VertexCount)
                    * fadeOpacity * flashPulse;

                Main.spriteBatch.DrawLineBetter(edgeStart, edgeEnd, edgeColor, 2.4f);
                Main.spriteBatch.DrawLineBetter(edgeStart, edgeEnd, edgeColor * 0.22f, 7f);

                // 笔锋光点
                if (edgePct < 0.98f)
                {
                    float tipPulse = 1f + 0.6f * MathF.Sin(t * 18f);
                    HyperdimensionalMatrixVisuals.DrawNode(edgeEnd, edgeColor, 5f * tipPulse);
                    HyperdimensionalMatrixVisuals.DrawNode(edgeEnd, edgeColor * 0.28f, 13f * tipPulse);
                }
            }

            // 已描绘顶点亮起
            int litVerts = (int)Math.Min(edgeProgress + 1f, VertexCount);
            for (int i = 0; i < litVerts; i++)
            {
                int vIdx = StarTrace[i];
                Color nodeColor = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)VertexCount)
                    * fadeOpacity * flashPulse;
                HyperdimensionalMatrixVisuals.DrawNode(verts[vIdx], nodeColor, 4.5f + flashPulse * 1.2f);
            }

            // 描绘完成后出现内层几何体与扫描环
            if (age >= DrawEnd)
            {
                float innerFade = MathHelper.SmoothStep(0f, 1f, (age - DrawEnd) / (float)(FlashEnd - DrawEnd));
                HyperdimensionalMatrixVisuals.DrawGeometry(Projectile.Center, MatrixGeometryShape.Tetrahedron,
                    18f, t * 2.8f, fadeOpacity * innerFade * 0.75f, Projectile.identity);
                HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, StampRadius * 1.18f, t * 1.6f,
                    HyperdimensionalMatrixVisuals.GetDataColor(0.65f, fadeOpacity * innerFade * 0.45f), 14, 2f);
                HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center,
                    HyperdimensionalMatrixVisuals.GetDataColor(t * 0.5f, fadeOpacity * innerFade * 0.9f),
                    6f * flashPulse);
            }

            return false;
        }

        private NPC GetTarget()
        {
            if (!Main.npc.IndexInRange(TargetIndex))
                return null;
            NPC npc = Main.npc[TargetIndex];
            return npc.CanBeChasedBy(Projectile, false) ? npc : null;
        }
    }

    /// <summary>
    /// 数据奇点：编译风暴的终章。
    /// 白色奇点聚焦 → 坍缩 → 大范围数据爆发。
    /// </summary>
    public sealed class MatrixSingularity : ModProjectile, ILocalizedModType
    {
        private const int FocusEnd    = 35;
        private const int CollapseEnd = 55;
        private const int Lifetime    = 90;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => Lifetime - Projectile.timeLeft;
        private float Completion => Age / (float)Lifetime;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1200;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Age >= CollapseEnd ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (Age < CollapseEnd)
                return false;

            // Expanding blast radius
            float blastR = (Age - CollapseEnd) / (float)(Lifetime - CollapseEnd) * 280f;
            Vector2 closest = new(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closest) <= blastR * blastR;
        }

        public override void AI()
        {
            int age = Age;
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.9f, 1f) * 0.85f);

            if (!Main.dedServ)
            {
                // Collapse → explosion transition: massive particle burst
                if (age == CollapseEnd)
                {
                    for (int i = 0; i < 32; i++)
                    {
                        Vector2 vel = (MathHelper.TwoPi * i / 32f).ToRotationVector2() * Main.rand.NextFloat(5f, 13f);
                        Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.031f);
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            Projectile.Center, vel, false, 18 + Main.rand.Next(16),
                            0.7f + Main.rand.NextFloat(0.55f), c, true, false, i % 5 == 0));
                    }
                    for (int i = 0; i < 22; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(4f, 11f);
                        Color c = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.045f + 0.2f);
                        GeneralParticleHandler.SpawnParticle(new SquareParticle(
                            Projectile.Center, vel, false, 35, 1.6f + Main.rand.NextFloat(1.3f), c * 1.8f));
                    }
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center, Vector2.Zero, false, 16, 5.5f, Color.White, true, false, true));

                    CLCBLightingBoltsSystem.Spawn_MatrixSingularityCollapse(Projectile.Center);
                    CLCBLightingBoltsSystem.Spawn_GaussSingularityPulse(Projectile.Center);

                    if (Main.LocalPlayer.active)
                    {
                        float _sd = Vector2.Distance(Main.LocalPlayer.Center, Projectile.Center);
                        if (_sd < 1000f)
                            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                                Main.LocalPlayer.Calamity().GeneralScreenShakePower, 7f * (1f - _sd / 1000f));
                    }
                }

                // Trailing sparks during explosion expansion
                if (age > CollapseEnd && age % 4 == 0)
                {
                    float blastR = (age - CollapseEnd) / (float)(Lifetime - CollapseEnd) * 280f;
                    for (int i = 0; i < 3; i++)
                    {
                        float a = Main.rand.NextFloat(MathHelper.TwoPi);
                        Vector2 pos = Projectile.Center + a.ToRotationVector2() * (blastR * Main.rand.NextFloat(0.85f, 1.1f));
                        GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                            pos, Vector2.Zero, false, 7, 0.7f,
                            HyperdimensionalMatrixVisuals.GetDataColor(Main.rand.NextFloat()), true, false, false));
                    }
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            int age = Age;
            float t = Main.GlobalTimeWrappedHourly;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            if (age < FocusEnd)
            {
                // Focus phase: white point appears and stabilizes
                float focusPct = age / (float)FocusEnd;
                float scale = MathHelper.SmoothStep(0f, 0.55f, focusPct);
                Main.spriteBatch.Draw(bloom, center, null, Color.White with { A = 0 }, t, bloom.Size() * 0.5f, scale, SpriteEffects.None, 0f);
                Main.spriteBatch.Draw(ring, center, null, Color.White with { A = 0 } * 0.7f, -t * 1.4f, ring.Size() * 0.5f, scale * 1.5f, SpriteEffects.None, 0f);

                // Inward scan rings
                for (int i = 0; i < 3; i++)
                {
                    float ringR = (1f - focusPct) * (180f - i * 40f);
                    Color rColor = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.28f, focusPct * 0.5f);
                    HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, ringR, t * (1.2f + i * 0.3f), rColor, 20, 2f);
                }
            }
            else if (age < CollapseEnd)
            {
                // Collapse: singularity shrinks before exploding
                float collapsePct = (age - FocusEnd) / (float)(CollapseEnd - FocusEnd);
                float scale = MathHelper.SmoothStep(0.55f, 0.12f, collapsePct);
                float flashIntensity = collapsePct * collapsePct;
                Main.spriteBatch.Draw(bloom, center, null, Color.White with { A = 0 } * (0.9f + flashIntensity * 2f), t * 2f, bloom.Size() * 0.5f, scale, SpriteEffects.None, 0f);
            }
            else
            {
                // Explosion phase: expanding burst of data
                float blastPct = (age - CollapseEnd) / (float)(Lifetime - CollapseEnd);
                float blastR = blastPct * 280f;
                Color blastColor = HyperdimensionalMatrixVisuals.GetDataColor(blastPct * 0.6f, 1f - blastPct);

                Main.spriteBatch.Draw(bloom, center, null, Color.White with { A = 0 } * ((1f - blastPct) * 3f), t, bloom.Size() * 0.5f, blastPct * 4f, SpriteEffects.None, 0f);

                Main.spriteBatch.End();
                Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                    DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

                HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, blastR, t * 3f, blastColor, 40, 5f);
                HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, blastR * 0.72f, -t * 4f, blastColor * 0.6f, 32, 3f);

                for (int i = 0; i < 24; i++)
                {
                    float angle = MathHelper.TwoPi * i / 24f + t;
                    Vector2 dir = angle.ToRotationVector2();
                    Main.spriteBatch.DrawLineBetter(
                        Projectile.Center + dir * blastR * 0.12f,
                        Projectile.Center + dir * blastR,
                        HyperdimensionalMatrixVisuals.GetDataColor(i * 0.042f, (1f - blastPct) * 0.6f),
                        2f);
                    HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center + dir * blastR, blastColor, 5f);
                }

                return false;
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }
}
