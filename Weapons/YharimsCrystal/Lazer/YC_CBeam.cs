using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.DataStructures;
using Terraria.Enums;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.YCLeft;
using YCRight = CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRight;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    public class YC_CBeam : ModProjectile, ILocalizedModType
    {
        public enum BeamAnchorKind : byte
        {
            LeftDrone,
            ExDrone,
            RightDrone,
            RightHoldout,
            LeftHoldout
        }

        private int elapsedTime;
        private bool lifetimeInitialized;

        public new string LocalizationCategory => "Projectiles";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int SourceIndex => (int)Projectile.ai[0];
        public BeamAnchorKind AnchorKind => (BeamAnchorKind)(int)Projectile.ai[1];

        public float ConfiguredLength { get; set; } = 1400f;
        public float BeamWidth { get; set; } = 12f;
        public int FixedLifetime { get; set; }
        public bool PersistentWhileSourceAlive { get; set; }
        public bool UseLaserScanLength { get; set; }
        public float ForwardOffset { get; set; } = 20f;
        public float TurnRateRadians { get; set; }
        public int DamageDelayFrames { get; set; }
        public int MaxTotalHits { get; set; } = -1;
        public Color OuterColor { get; set; } = new(255, 228, 150);
        public Color InnerColor { get; set; } = Color.White;

        private float BeamLength
        {
            get => Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        public static int SpawnBeam(
            IEntitySource source,
            Vector2 position,
            Vector2 direction,
            int damage,
            float knockback,
            int owner,
            int anchorIndex,
            BeamAnchorKind anchorKind,
            float beamLength,
            float beamWidth,
            int fixedLifetime,
            bool persistentWhileSourceAlive,
            bool useLaserScanLength,
            Color outerColor,
            Color innerColor,
            float forwardOffset = 20f,
            float turnRateRadians = 0f,
            int maxTotalHits = -1,
            int hitCooldown = 5,
            int damageDelayFrames = 0)
        {
            direction = direction.SafeNormalize(Vector2.UnitX);

            int index = Projectile.NewProjectile(
                source,
                position,
                direction,
                ModContent.ProjectileType<YC_CBeam>(),
                damage,
                knockback,
                owner,
                anchorIndex,
                (float)anchorKind);

            if (index >= 0 && index < Main.maxProjectiles && Main.projectile[index].ModProjectile is YC_CBeam beam)
            {
                beam.ConfiguredLength = beamLength;
                beam.BeamWidth = beamWidth;
                beam.FixedLifetime = fixedLifetime;
                beam.PersistentWhileSourceAlive = persistentWhileSourceAlive;
                beam.UseLaserScanLength = useLaserScanLength;
                beam.ForwardOffset = forwardOffset;
                beam.TurnRateRadians = turnRateRadians;
                beam.DamageDelayFrames = damageDelayFrames;
                beam.MaxTotalHits = maxTotalHits;
                beam.OuterColor = outerColor;
                beam.InnerColor = innerColor;
                beam.Projectile.localNPCHitCooldown = hitCooldown;
                beam.Projectile.netUpdate = true;
            }

            return index;
        }

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.hide = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 5;
            Projectile.timeLeft = 18000;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => elapsedTime >= DamageDelayFrames ? null : false;

        public override bool? CanHitNPC(NPC target)
        {
            if (MaxTotalHits >= 0 && Projectile.numHits >= MaxTotalHits)
                return false;

            return base.CanHitNPC(target);
        }

        public override void DrawBehind(
            int index,
            System.Collections.Generic.List<int> behindNPCsAndTiles,
            System.Collections.Generic.List<int> behindNPCs,
            System.Collections.Generic.List<int> behindProjectiles,
            System.Collections.Generic.List<int> overPlayers,
            System.Collections.Generic.List<int> overWiresUI)
        {
            overPlayers.Add(index);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(ConfiguredLength);
            writer.Write(BeamWidth);
            writer.Write(FixedLifetime);
            writer.Write(PersistentWhileSourceAlive);
            writer.Write(UseLaserScanLength);
            writer.Write(ForwardOffset);
            writer.Write(TurnRateRadians);
            writer.Write(DamageDelayFrames);
            writer.Write(MaxTotalHits);
            writer.Write(OuterColor.PackedValue);
            writer.Write(InnerColor.PackedValue);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            ConfiguredLength = reader.ReadSingle();
            BeamWidth = reader.ReadSingle();
            FixedLifetime = reader.ReadInt32();
            PersistentWhileSourceAlive = reader.ReadBoolean();
            UseLaserScanLength = reader.ReadBoolean();
            ForwardOffset = reader.ReadSingle();
            TurnRateRadians = reader.ReadSingle();
            DamageDelayFrames = reader.ReadInt32();
            MaxTotalHits = reader.ReadInt32();
            OuterColor = ReadColor(reader);
            InnerColor = ReadColor(reader);
        }

        public override void AI()
        {
            if (!TryResolveAnchor(out Projectile sourceProjectile, out Vector2 desiredDirection))
            {
                Projectile.Kill();
                return;
            }

            if (!lifetimeInitialized)
            {
                if (!PersistentWhileSourceAlive && FixedLifetime > 0)
                    Projectile.timeLeft = FixedLifetime;

                lifetimeInitialized = true;
            }

            if (PersistentWhileSourceAlive)
                Projectile.timeLeft = 2;

            Vector2 fallbackDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX * Main.player[Projectile.owner].direction);
            desiredDirection = desiredDirection.SafeNormalize(fallbackDirection);

            if (AnchorKind == BeamAnchorKind.LeftDrone && sourceProjectile.ModProjectile is IYCLeftBeamSource leftBeamSource)
            {
                ConfiguredLength = leftBeamSource.GetBeamLength(ConfiguredLength, ForwardOffset);
                TurnRateRadians = leftBeamSource.GetBeamTurnRateRadians(TurnRateRadians);
            }

            if (Projectile.velocity == Vector2.Zero)
                Projectile.velocity = desiredDirection;
            else if (TurnRateRadians > 0f)
                Projectile.velocity = RotateTowards(Projectile.velocity.SafeNormalize(fallbackDirection), desiredDirection, TurnRateRadians);
            else
                Projectile.velocity = desiredDirection;

            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.Center = sourceProjectile.Center + Projectile.velocity * ForwardOffset;

            UpdateBeamLength();
            EmitBeamDust();
            Lighting.AddLight(Projectile.Center, OuterColor.ToVector3() * 0.28f);
            elapsedTime++;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 end = Projectile.Center + direction * BeamLength;

            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                end,
                BeamWidth,
                ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (AnchorKind != BeamAnchorKind.LeftDrone || SourceIndex < 0 || SourceIndex >= Main.maxProjectiles)
                return;

            Projectile source = Main.projectile[SourceIndex];
            if (!source.active || source.owner != Projectile.owner)
                return;

            if (source.ModProjectile is IYCLeftBeamSource leftBeamSource)
                leftBeamSource.OnLeftBeamHit(target, hit, damageDone, Projectile);
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Vector2 unit = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Utils.PlotTileLine(Projectile.Center, Projectile.Center + unit * BeamLength, BeamWidth + 8f, DelegateMethods.CutTiles);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
                return false;

            Texture2D outer = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineFade").Value;
            Texture2D inner = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineThick").Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 unit = Projectile.rotation.ToRotationVector2();
            float fx = MathHelper.Max(0.2f, CalculateOpacity());
            float beamLengthScale = BeamLength / 1000f;
            Vector2 beamCenter = start + unit * BeamLength * 0.5f;
            Color outerColor = OuterColor with { A = 0 };
            Color innerColor = InnerColor with { A = 0 };
            float randomSize = 1f;
            float widthFactor = MathHelper.Clamp(BeamWidth / 10f, 0.9f, 2.6f);

            Main.EntitySpriteDraw(
                outer,
                beamCenter,
                null,
                outerColor * fx,
                Projectile.rotation + MathHelper.PiOver2,
                outer.Size() * 0.5f,
                new Vector2(2.5f * randomSize * fx * widthFactor, 55f * beamLengthScale) * Projectile.scale * 0.01f,
                SpriteEffects.FlipVertically,
                0f);

            Main.EntitySpriteDraw(
                inner,
                beamCenter,
                null,
                Color.Lerp(innerColor, Color.White with { A = 0 }, 0.3f) * MathHelper.Min(fx, 1f),
                Projectile.rotation + MathHelper.PiOver2,
                inner.Size() * 0.5f,
                new Vector2(0.4f * MathHelper.Min(fx, 1f) * MathHelper.Lerp(0.95f, 1.35f, (widthFactor - 0.9f) / 1.7f), 55f * beamLengthScale) * Projectile.scale * 0.01f,
                SpriteEffects.FlipVertically,
                0f);

            for (int i = 0; i < 2; i++)
            {
                Main.EntitySpriteDraw(
                    glow,
                    start + unit * 7f * Projectile.scale,
                    null,
                    Color.Lerp(outerColor, Color.White with { A = 0 }, i) * MathHelper.Max(fx - 0.15f, 0f),
                    Projectile.rotation + MathHelper.PiOver2,
                    glow.Size() * 0.5f,
                    new Vector2((2f + 0.05f * (fx + 25f)) * MathHelper.Lerp(0.9f, 1.45f, (widthFactor - 0.9f) / 1.7f), 1f) * Projectile.scale * MathHelper.Lerp(fx, 1f, 0.7f) * (0.03f - 0.01f * i),
                    SpriteEffects.FlipVertically,
                    0f);
            }

            return false;
        }

        private bool TryResolveAnchor(out Projectile sourceProjectile, out Vector2 desiredDirection)
        {
            sourceProjectile = null;
            desiredDirection = Vector2.Zero;

            if (SourceIndex < 0 || SourceIndex >= Main.maxProjectiles)
                return false;

            Projectile source = Main.projectile[SourceIndex];
            if (!source.active || source.owner != Projectile.owner)
                return false;

            switch (AnchorKind)
            {
                case BeamAnchorKind.LeftDrone:
                    if (source.ModProjectile is not IYCLeftBeamSource leftDrone)
                        return false;

                    desiredDirection = leftDrone.DesiredAimDirection.SafeNormalize(leftDrone.ForwardDirection);
                    break;

                case BeamAnchorKind.ExDrone:
                    if (source.ModProjectile is not IYCEXBeamSource exBeamSource)
                        return false;

                    Player exOwner = Main.player[source.owner];
                    Vector2 outward = exBeamSource.CurrentForwardDirection.SafeNormalize((source.Center - exOwner.Center).SafeNormalize(Vector2.UnitY));
                    NPC nearestTarget = FindNearestTarget(source.Center, ConfiguredLength);
                    desiredDirection = nearestTarget != null
                        ? (nearestTarget.Center - source.Center).SafeNormalize(outward)
                        : outward;
                    break;

                case BeamAnchorKind.RightDrone:
                    if (source.ModProjectile is not YCRight.IYCRightBeamSource rightDrone)
                        return false;

                    desiredDirection = rightDrone.CurrentForwardDirection.SafeNormalize(source.velocity.SafeNormalize(Vector2.UnitX));
                    break;

                case BeamAnchorKind.RightHoldout:
                    if (source.type != ModContent.ProjectileType<YCRight.YC_RightHoldOut>() || source.ModProjectile is not YCRight.YC_RightHoldOut rightHoldout)
                        return false;

                    desiredDirection = rightHoldout.ForwardDirection;
                    break;

                case BeamAnchorKind.LeftHoldout:
                    if (source.type != ModContent.ProjectileType<YC_LeftHoldOut>() || source.ModProjectile is not YC_LeftHoldOut leftHoldout)
                        return false;

                    desiredDirection = leftHoldout.ForwardDirection;
                    break;

                default:
                    return false;
            }

            sourceProjectile = source;
            return desiredDirection != Vector2.Zero && !desiredDirection.HasNaNs();
        }

        private void UpdateBeamLength()
        {
            if (UseLaserScanLength)
            {
                float[] samples = new float[3];
                Collision.LaserScan(Projectile.Center, Projectile.velocity.SafeNormalize(Vector2.UnitX), BeamWidth, ConfiguredLength, samples);
                BeamLength = samples.Length > 0 ? (samples[0] + samples[1] + samples[2]) / samples.Length : ConfiguredLength;

                if (BeamLength <= 0f)
                    BeamLength = ConfiguredLength;

                return;
            }

            BeamLength = ConfiguredLength;
        }

        private float CalculateOpacity()
        {
            float fadeIn = Utils.GetLerpValue(0f, 8f, elapsedTime, true);
            if (PersistentWhileSourceAlive || FixedLifetime <= 0)
                return fadeIn;

            float fadeOutFrames = System.Math.Min(12f, FixedLifetime * 0.35f);
            float fadeOut = Utils.GetLerpValue(0f, fadeOutFrames, Projectile.timeLeft, true);
            return fadeIn * fadeOut;
        }

        private void EmitBeamDust()
        {
            if (Main.dedServ || Main.GameUpdateCount % 8 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + direction * Main.rand.NextFloat(12f, 34f),
                DustID.RainbowTorch,
                direction.RotatedByRandom(0.18f) * Main.rand.NextFloat(0.6f, 1.5f),
                0,
                Color.Lerp(OuterColor, InnerColor, 0.35f),
                Main.rand.NextFloat(0.75f, 1.05f));
            dust.noGravity = true;
        }

        private static Vector2 RotateTowards(Vector2 currentDirection, Vector2 desiredDirection, float maxTurnRadians)
        {
            float currentAngle = currentDirection.ToRotation();
            float desiredAngle = desiredDirection.ToRotation();
            float delta = MathHelper.WrapAngle(desiredAngle - currentAngle);
            delta = MathHelper.Clamp(delta, -maxTurnRadians, maxTurnRadians);
            return (currentAngle + delta).ToRotationVector2();
        }

        private NPC FindNearestTarget(Vector2 source, float maxDistance)
        {
            NPC nearest = null;
            float maxDistanceSquared = maxDistance * maxDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distanceSquared = Vector2.DistanceSquared(source, npc.Center);
                if (distanceSquared > maxDistanceSquared)
                    continue;

                if (!Collision.CanHitLine(source, 1, 1, npc.Center, 1, 1))
                    continue;

                maxDistanceSquared = distanceSquared;
                nearest = npc;
            }

            return nearest;
        }

        private static Color ReadColor(BinaryReader reader)
        {
            Color color = default;
            color.PackedValue = reader.ReadUInt32();
            return color;
        }
    }
}
