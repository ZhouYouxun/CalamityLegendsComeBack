using System;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.HyperdimensionalMatrixCore
{
    public sealed class MatrixDataNeedle : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.TrailCacheLength[Type] = 14;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = MatrixCoreNumbers.PiercingProjectilePenetration;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = MatrixCoreNumbers.PiercingProjectileLocalHitCooldown;
        }

        public override void OnSpawn(IEntitySource source)
        {
            for (int i = 0; i < Projectile.oldPos.Length; i++)
                Projectile.oldPos[i] = Projectile.position;
        }

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (Main.npc.IndexInRange(targetIndex))
            {
                NPC target = Main.npc[targetIndex];
                if (target.CanBeChasedBy(Projectile, false))
                {
                    Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitY));
                    float newRotation = Projectile.velocity.ToRotation().AngleTowards(
                        desiredDirection.ToRotation(),
                        MatrixCoreNumbers.PiercingProjectileMaxTurn);
                    float speed = MathHelper.Lerp(
                        Projectile.velocity.Length(),
                        MatrixCoreNumbers.PiercingProjectileHomingSpeed,
                        MatrixCoreNumbers.PiercingProjectileAcceleration);
                    Projectile.velocity = newRotation.ToRotationVector2() * speed;
                }
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.07f).ToVector3() * 0.32f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Color color = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.073f);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            for (int i = Projectile.oldPos.Length - 1; i > 0; i--)
            {
                Vector2 start = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                Vector2 end = Projectile.oldPos[i - 1] + Projectile.Size * 0.5f;
                if (Projectile.oldPos[i] == Vector2.Zero ||
                    Projectile.oldPos[i - 1] == Vector2.Zero ||
                    Vector2.DistanceSquared(start, end) > 1600f * 1600f)
                {
                    continue;
                }

                float opacity = 1f - i / (float)Projectile.oldPos.Length;
                Main.spriteBatch.DrawLineBetter(start, end, color * opacity * 0.55f, 1f + opacity * 2f);
            }

            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 16f, Projectile.Center + forward * 11f, color * 0.34f, 7f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center - forward * 16f, Projectile.Center + forward * 11f, Color.White with { A = 0 }, 1.8f);
            HyperdimensionalMatrixVisuals.DrawNode(Projectile.Center, color, 5f);
            return false;
        }
    }

    public sealed class MatrixOrbitalProjection : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1800;
        }

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = MatrixCoreNumbers.OrbitalProjectilePenetration;
            Projectile.timeLeft = MatrixCoreNumbers.OrbitalProjectileLifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = MatrixCoreNumbers.OrbitalProjectileLocalHitCooldown;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.rotation += 0.18f;
            Projectile.velocity.Y = Math.Min(
                MatrixCoreNumbers.OrbitalProjectileMaxFallSpeed,
                Projectile.velocity.Y + MatrixCoreNumbers.OrbitalProjectileGravity);

            int targetIndex = (int)Projectile.ai[0];
            if (Main.npc.IndexInRange(targetIndex))
            {
                NPC target = Main.npc[targetIndex];
                if (target.CanBeChasedBy(Projectile, false))
                {
                    float correction = MathHelper.Clamp(
                        (target.Center.X - Projectile.Center.X) * MatrixCoreNumbers.OrbitalProjectileHorizontalCorrectionStrength,
                        -MatrixCoreNumbers.OrbitalProjectileMaxHorizontalCorrection,
                        MatrixCoreNumbers.OrbitalProjectileMaxHorizontalCorrection);
                    Projectile.velocity.X = MathHelper.Lerp(
                        Projectile.velocity.X,
                        correction,
                        MatrixCoreNumbers.OrbitalProjectileHorizontalCorrectionLerp);
                }
            }

            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(Projectile.ai[1] * 0.1f).ToVector3() * 0.38f);
        }

        public override bool OnTileCollide(Vector2 oldVelocity) => true;

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_Death(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MatrixOrbitalExplosion>(),
                Math.Max(1, (int)(Projectile.damage * MatrixCoreNumbers.OrbitalExplosionDamageMultiplier)),
                Projectile.knockBack,
                Projectile.owner,
                Projectile.ai[1]);

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.3f, Pitch = 0.38f, MaxInstances = 7 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float time = Main.GlobalTimeWrappedHourly + Projectile.ai[1];
            Color color = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.ai[1] * 0.13f);
            Vector2 trailEnd = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 82f;
            Main.spriteBatch.DrawLineBetter(Projectile.Center, trailEnd, color * 0.26f, 11f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center, trailEnd, color, 2.2f);
            HyperdimensionalMatrixVisuals.DrawGeometry(
                Projectile.Center,
                MatrixGeometryShape.Icosahedron,
                24f,
                time * 2.2f,
                0.9f,
                Projectile.identity,
                false);
            return false;
        }
    }

    public sealed class MatrixOrbitalExplosion : ModProjectile, ILocalizedModType
    {
        private const float MaximumRadius = MatrixCoreNumbers.OrbitalExplosionSize * 0.5f;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private float Completion => 1f - Projectile.timeLeft / 20f;
        private float Radius => MaximumRadius * (float)Math.Sin(MathHelper.Pi * MathHelper.Clamp(Completion, 0f, 1f));

        public override void SetStaticDefaults() => ProjectileID.Sets.MinionShot[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = MatrixCoreNumbers.OrbitalExplosionSize;
            Projectile.height = MatrixCoreNumbers.OrbitalExplosionSize;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = MatrixCoreNumbers.OrbitalExplosionPenetration;
            Projectile.timeLeft = 20;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = MatrixCoreNumbers.OrbitalExplosionLocalHitCooldown;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closestPoint = new(
                MathHelper.Clamp(Projectile.Center.X, targetHitbox.Left, targetHitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, targetHitbox.Top, targetHitbox.Bottom));
            return Vector2.DistanceSquared(Projectile.Center, closestPoint) <= Radius * Radius;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            float completion = MathHelper.Clamp(Completion, 0f, 1f);
            Color color = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.ai[0] * 0.11f, 1f - completion);
            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, Radius, completion * 3f, color, 42, 4f);
            HyperdimensionalMatrixVisuals.DrawScanRing(Projectile.Center, Radius * 0.7f, -completion * 5f, color * 0.65f, 30, 2f);

            for (int i = 0; i < 12; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 12f + Projectile.ai[0]).ToRotationVector2();
                Main.spriteBatch.DrawLineBetter(
                    Projectile.Center + direction * Radius * 0.25f,
                    Projectile.Center + direction * Radius,
                    color * 0.48f,
                    1.5f);
            }

            return false;
        }
    }

    public sealed class MatrixFractureField : ModProjectile, ILocalizedModType
    {
        private const int NodeCount = 8;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int Age => MatrixCoreNumbers.FractureProjectileLifetime - Projectile.timeLeft;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 1600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = MatrixCoreNumbers.FractureProjectilePenetration;
            Projectile.timeLeft = MatrixCoreNumbers.FractureProjectileLifetime;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = MatrixCoreNumbers.FractureProjectileLocalHitCooldown;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC target = GetTarget();
            if (target == null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            Lighting.AddLight(Projectile.Center, HyperdimensionalMatrixVisuals.GetDataColor(Projectile.ai[1] * 0.1f).ToVector3() * 0.42f);
        }

        public override bool? CanDamage() => GetTarget() != null;

        public override bool? CanHitNPC(NPC target) => target.whoAmI == (int)Projectile.ai[0] ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            NPC target = GetTarget();
            if (target == null)
                return false;

            if (targetHitbox.Intersects(target.Hitbox))
                return true;

            for (int i = 0; i < 7; i++)
            {
                GetCutLine(i, out Vector2 start, out Vector2 end);
                float collisionPoint = 0f;
                if (Collision.CheckAABBvLineCollision(
                    targetHitbox.TopLeft(),
                    targetHitbox.Size(),
                    start,
                    end,
                    8f,
                    ref collisionPoint))
                {
                    return true;
                }
            }

            return false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (GetTarget() == null)
                return false;

            float buildProgress = MathHelper.Clamp(Age / 18f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 18f, 0f, 1f);
            float opacity = buildProgress * fadeOut;
            Vector2[] nodes = new Vector2[NodeCount];

            for (int i = 0; i < NodeCount; i++)
                nodes[i] = GetNode(i);

            for (int i = 0; i < NodeCount; i++)
            {
                Color cageColor = HyperdimensionalMatrixVisuals.GetDataColor(i / (float)NodeCount, opacity * 0.55f);
                Main.spriteBatch.DrawLineBetter(nodes[i], nodes[(i + 1) % NodeCount], cageColor, 1.7f);
                Main.spriteBatch.DrawLineBetter(nodes[i], nodes[(i + 3) % NodeCount], cageColor * 0.35f, 1f);
                HyperdimensionalMatrixVisuals.DrawNode(nodes[i], cageColor, 7f);
            }

            for (int i = 0; i < 7; i++)
            {
                GetCutLine(i, out Vector2 start, out Vector2 end);
                float flash = 0.55f + 0.45f * (float)Math.Sin(Age * 0.42f + i * 1.7f);
                Color cutColor = HyperdimensionalMatrixVisuals.GetDataColor(i * 0.13f + Projectile.ai[1], opacity * flash);
                Vector2 glitchOffset = new Vector2((float)Math.Sin(Age * 0.7f + i) * 2.5f, 0f);
                Main.spriteBatch.DrawLineBetter(start + glitchOffset, end + glitchOffset, cutColor * 0.25f, 8f);
                Main.spriteBatch.DrawLineBetter(start, end, cutColor, 2.3f);
            }

            HyperdimensionalMatrixVisuals.DrawGeometry(
                Projectile.Center,
                MatrixGeometryShape.Cube,
                GetRadius() * 0.52f,
                Main.GlobalTimeWrappedHourly * 1.9f + Projectile.ai[1],
                opacity * 0.45f,
                Projectile.identity,
                false);
            return false;
        }

        private NPC GetTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (!Main.npc.IndexInRange(targetIndex))
                return null;

            NPC target = Main.npc[targetIndex];
            return target.CanBeChasedBy(Projectile, false) ? target : null;
        }

        private float GetRadius()
        {
            NPC target = GetTarget();
            return target == null ? 110f : Math.Max(105f, Math.Max(target.width, target.height) * 0.72f + 68f);
        }

        private Vector2 GetNode(int index)
        {
            float angle = MathHelper.TwoPi * index / NodeCount + Projectile.ai[1] + Age * 0.012f;
            float radius = GetRadius() * (1f + (float)Math.Sin(Age * 0.09f + index) * 0.06f);
            return Projectile.Center + new Vector2((float)Math.Cos(angle) * radius, (float)Math.Sin(angle) * radius * 0.74f);
        }

        private void GetCutLine(int index, out Vector2 start, out Vector2 end)
        {
            int phase = Age / 5;
            int startIndex = (index * 3 + phase) % NodeCount;
            int endIndex = (index * 5 + phase + 3) % NodeCount;
            if (startIndex == endIndex)
                endIndex = (endIndex + 3) % NodeCount;

            start = GetNode(startIndex);
            end = GetNode(endIndex);
        }
    }

    public sealed class HyperdimensionalMatrixBeam : ModProjectile, ILocalizedModType
    {
        private Vector2 beamDirection;

        public new string LocalizationCategory => "Projectiles.HyperdimensionalMatrixCore";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.MinionShot[Type] = true;
            ProjectileID.Sets.CultistIsResistantTo[Type] = true;
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 3600;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = MatrixCoreNumbers.HyperdimensionalProjectilePenetration;
            Projectile.timeLeft = 2;
            Projectile.DamageType = DamageClass.Summon;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = MatrixCoreNumbers.HyperdimensionalProjectileLocalHitCooldown;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            HyperdimensionalMatrixCoreProjectile core = GetCore();
            NPC target = core?.GetTarget();
            if (core == null ||
                target == null ||
                core.CurrentForm != HyperdimensionalMatrixCoreProjectile.MatrixForm.Hyperdimensional)
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Projectile.Center = core.Projectile.Center;
            Projectile.damage = Math.Max(1, (int)(core.Projectile.damage * MatrixCoreNumbers.HyperdimensionalProjectileDamageMultiplier));

            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
            beamDirection = desiredDirection;
            Projectile.velocity = beamDirection;
            Projectile.rotation = beamDirection.ToRotation();

            Vector3 lightColor = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.03f).ToVector3() * 0.42f;
            for (int i = 0; i <= 12; i++)
                Lighting.AddLight(Projectile.Center + beamDirection * (MatrixCoreNumbers.HyperdimensionalBeamLength * i / 12f), lightColor);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + beamDirection * MatrixCoreNumbers.HyperdimensionalBeamLength,
                22f,
                ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (beamDirection == Vector2.Zero)
                return false;

            Vector2 normal = beamDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 end = Projectile.Center + beamDirection * MatrixCoreNumbers.HyperdimensionalBeamLength;
            float time = Main.GlobalTimeWrappedHourly;
            Color color = HyperdimensionalMatrixVisuals.GetDataColor(Projectile.identity * 0.033f);

            Main.spriteBatch.DrawLineBetter(Projectile.Center, end, color * 0.18f, 34f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center, end, color * 0.6f, 12f);
            Main.spriteBatch.DrawLineBetter(Projectile.Center, end, Color.White with { A = 0 }, 3f);

            const int segments = 48;
            for (int lane = -1; lane <= 1; lane += 2)
            {
                Vector2 previous = Projectile.Center;
                for (int i = 1; i <= segments; i++)
                {
                    float completion = i / (float)segments;
                    float wave = (float)Math.Sin(completion * MathHelper.TwoPi * 6f - time * 8f + lane) * 9f * lane;
                    Vector2 current = Projectile.Center + beamDirection * MatrixCoreNumbers.HyperdimensionalBeamLength * completion + normal * wave;
                    Main.spriteBatch.DrawLineBetter(
                        previous,
                        current,
                        HyperdimensionalMatrixVisuals.GetDataColor(completion + lane * 0.1f, 0.42f),
                        1.4f);

                    if (i % 6 == 0)
                        HyperdimensionalMatrixVisuals.DrawNode(current, color * 0.75f, 4f);

                    previous = current;
                }
            }

            HyperdimensionalMatrixVisuals.DrawHypercube(Projectile.Center, 34f, time * 1.8f, 0.65f);
            return false;
        }

        private HyperdimensionalMatrixCoreProjectile GetCore()
        {
            int coreIndex = (int)Projectile.ai[0];
            if (!Main.projectile.IndexInRange(coreIndex))
                return null;

            Projectile coreProjectile = Main.projectile[coreIndex];
            if (!coreProjectile.active ||
                coreProjectile.owner != Projectile.owner ||
                coreProjectile.type != ModContent.ProjectileType<HyperdimensionalMatrixCoreProjectile>())
            {
                return null;
            }

            return coreProjectile.ModProjectile as HyperdimensionalMatrixCoreProjectile;
        }
    }
}
