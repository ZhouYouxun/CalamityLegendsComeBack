using System;
using System.IO;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill;
using CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.E_TyrantPrism;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.NPCs;
using CalamityMod.Particles;
using CalamityMod.Utilities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Enums;
using Terraria.GameContent.Shaders;
using Terraria.Graphics.Effects;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.YharimsCrystal
{
    internal static class YC_BeamWorldSafety
    {
        private const float WorldClampPadding = 64f;
        private const float MinimumLaserScanWidth = 1f;

        public static void SafePlotTileLine(Vector2 start, Vector2 end, float width, Utils.TileActionAttempt plot)
        {
            if (width <= 0f || start.HasNaNs() || end.HasNaNs())
                return;

            start = ClampToWorld(start);
            end = ClampToWorld(end);
            if (Vector2.DistanceSquared(start, end) < 1f)
                return;

            Utils.PlotTileLine(start, end, width, plot);
        }

        public static bool TryLaserScan(Vector2 start, Vector2 direction, float width, float maxLength, float[] samples)
        {
            if (samples == null ||
                samples.Length == 0 ||
                !float.IsFinite(width) ||
                !float.IsFinite(maxLength) ||
                maxLength <= 0f ||
                start.HasNaNs() ||
                direction.HasNaNs() ||
                direction == Vector2.Zero)
            {
                return false;
            }

            start = ClampToWorld(start);
            direction = direction.SafeNormalize(Vector2.UnitX);
            Collision.LaserScan(start, direction, Math.Max(width, MinimumLaserScanWidth), maxLength, samples);
            return true;
        }

        public static Vector2 ClampToWorld(Vector2 point)
        {
            float maxX = System.Math.Max(WorldClampPadding, Main.maxTilesX * 16f - WorldClampPadding);
            float maxY = System.Math.Max(WorldClampPadding, Main.maxTilesY * 16f - WorldClampPadding);
            return new Vector2(
                MathHelper.Clamp(point.X, WorldClampPadding, maxX),
                MathHelper.Clamp(point.Y, WorldClampPadding, maxY));
        }
    }

    [PierceResistException]
    public class YC_YharimsCrystalBeam : ModProjectile, ILocalizedModType
    {
        public enum BeamHostKind
        {
            MainHoldout,
            TyrantDrone
        }

        public const int NumBeams = 6;
        private const float PiBeamDivisor = MathHelper.Pi / NumBeams;
        private const float MaxDamageMultiplier = 3f;
        private const float BeamPosOffset = 16f;
        private const float MaxBeamScale = 1.8f;
        private const float DroneAttackBeamScaleFactor = 0.5f;
        private const float MaxBeamLength = 2400f;
        private const float BeamTileCollisionWidth = 1f;
        private const float BeamHitboxCollisionWidth = 22f;
        private const int NumSamplePoints = 3;
        private const float BeamLengthChangeFactor = 0.75f;
        private const float VisualEffectThreshold = 0.1f;
        private const float OuterBeamOpacityMultiplier = 0.75f;
        private const float InnerBeamOpacityMultiplier = 0.1f;
        private const float BeamLightBrightness = 0.75f;
        private const float MainDustBeamEndOffset = 14.5f;
        private const float SidewaysDustBeamEndOffset = 4f;
        private const float BeamRenderTileOffset = 10.5f;
        private const float BeamLengthReductionFactor = 14.5f;
        private const float MaxCharge = 180f;
        private const float DamageStart = 30f;

        public new string LocalizationCategory => "Projectiles.YharimsCrystal";
        public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";

        private int BeamID => (int)Projectile.ai[0];
        private int HostIndex => (int)Projectile.ai[1];
        private BeamHostKind HostKind => (BeamHostKind)(int)Projectile.ai[2];
        private ref float BeamLength => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = -1;
            Projectile.alpha = 255;
            Projectile.tileCollide = false;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void SendExtraAI(BinaryWriter writer) => writer.Write(Projectile.localAI[1]);
        public override void ReceiveExtraAI(BinaryReader reader) => Projectile.localAI[1] = reader.ReadSingle();

        public override void AI()
        {
            if (!TryGetHostData(out Vector2 hostCenter, out Vector2 hostDirection, out float charge, out int hostDamage, out bool canDealDamage))
            {
                Projectile.Kill();
                return;
            }

            hostDirection = hostDirection.SafeNormalize(Vector2.UnitX * Main.player[Projectile.owner].direction);
            float chargeRatio = MathHelper.Clamp(charge / MaxCharge, 0f, 1f);

            Projectile.damage = (int)(hostDamage * GetDamageMultiplier(chargeRatio));
            Projectile.friendly = canDealDamage && charge > DamageStart;

            float beamIdOffset = BeamID - NumBeams / 2f + 0.5f;
            float beamSpread;
            float spinRate;
            float beamStartSidewaysOffset;
            float beamStartForwardsOffset;

            if (chargeRatio < 1f)
            {
                Projectile.scale = MathHelper.Lerp(0f, MaxBeamScale, chargeRatio);
                beamSpread = MathHelper.Lerp(1.22f, 0f, chargeRatio);
                beamStartSidewaysOffset = MathHelper.Lerp(20f, 6f, chargeRatio);
                beamStartForwardsOffset = MathHelper.Lerp(-17f, -13f, chargeRatio);

                if (chargeRatio <= 0.66f)
                {
                    float phaseRatio = chargeRatio * 1.5f;
                    Projectile.Opacity = MathHelper.Lerp(0f, 0.4f, phaseRatio);
                    spinRate = MathHelper.Lerp(20f, 16f, phaseRatio);
                }
                else
                {
                    float phaseRatio = (chargeRatio - 0.66f) * 3f;
                    Projectile.Opacity = MathHelper.Lerp(0.4f, 1f, phaseRatio);
                    spinRate = MathHelper.Lerp(16f, 6f, phaseRatio);
                }
            }
            else
            {
                Projectile.scale = MaxBeamScale;
                Projectile.Opacity = 1f;
                beamSpread = 0f;
                spinRate = 6f;
                beamStartSidewaysOffset = 6f;
                beamStartForwardsOffset = -13f;
            }

            if (HostKind == BeamHostKind.TyrantDrone && chargeRatio >= 0.98f)
                Projectile.scale *= DroneAttackBeamScaleFactor;

            float deviationAngle = (charge + beamIdOffset * spinRate) / (spinRate * NumBeams) * MathHelper.TwoPi;
            Vector2 unitRot = Vector2.UnitY.RotatedBy(deviationAngle);
            float sinusoidYOffset = unitRot.Y * PiBeamDivisor * beamSpread;
            float hostAngle = hostDirection.ToRotation();
            Vector2 yVec = new(4f, beamStartSidewaysOffset);
            Vector2 beamSpanVector = (unitRot * yVec).RotatedBy(hostAngle);

            Projectile.Center = hostCenter;
            Projectile.position += hostDirection * BeamPosOffset;
            Projectile.position += hostDirection * beamStartForwardsOffset;
            Projectile.position += beamSpanVector;

            Projectile.velocity = hostDirection.RotatedBy(sinusoidYOffset);
            if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
                Projectile.velocity = -Vector2.UnitY;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Vector2 samplingPoint = Projectile.Center;
            if (charge >= MaxCharge)
                samplingPoint = hostCenter;
            if (!Collision.CanHitLine(Main.player[Projectile.owner].Center, 0, 0, hostCenter, 0, 0))
                samplingPoint = Main.player[Projectile.owner].Center;

            if (Projectile.scale <= 0f)
            {
                BeamLength = 0f;
                return;
            }

            float[] laserScanResults = new float[NumSamplePoints];
            if (!YC_BeamWorldSafety.TryLaserScan(samplingPoint, Projectile.velocity, BeamTileCollisionWidth * Projectile.scale, MaxBeamLength, laserScanResults))
            {
                BeamLength = 0f;
                return;
            }

            float avg = 0f;
            for (int i = 0; i < laserScanResults.Length; ++i)
                avg += laserScanResults[i];
            avg /= NumSamplePoints;
            BeamLength = MathHelper.Lerp(BeamLength, avg, BeamLengthChangeFactor);

            Vector2 beamDims = new(Projectile.velocity.Length() * BeamLength, Projectile.width * Projectile.scale);
            Color beamColor = GetBeamColor();
            if (chargeRatio >= VisualEffectThreshold)
            {
                ProduceBeamDust(beamColor);

                if (!Main.dedServ)
                {
                    WaterShaderData wsd = (WaterShaderData)Filters.Scene["WaterDistortion"].GetShader();
                    float waveSine = 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 20f);
                    Vector2 ripplePos = Projectile.position + new Vector2(beamDims.X * 0.5f, 0f).RotatedBy(Projectile.rotation);
                    Color waveData = new(0.5f, 0.1f * Math.Sign(waveSine) + 0.5f, 0f, 1f);
                    waveData *= Math.Abs(waveSine);
                    wsd.QueueRipple(ripplePos, waveData, beamDims, RippleShape.Square, Projectile.rotation);
                }
            }

            if (chargeRatio > 0.02f && BeamLength > 0f && beamDims.Y > 0f)
            {
                DelegateMethods.v3_1 = beamColor.ToVector3() * BeamLightBrightness * chargeRatio;
                YC_BeamWorldSafety.SafePlotTileLine(
                    Projectile.Center,
                    Projectile.Center + Projectile.velocity * BeamLength,
                    beamDims.Y,
                    DelegateMethods.CastLight);
            }

            if (!Main.dedServ && chargeRatio >= 0.95f && (Main.GameUpdateCount + BeamID * 7) % 42 == 0)
                Terraria.Audio.SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.08f, Pitch = -0.1f + BeamID * 0.025f, MaxInstances = 8 }, Projectile.Center);
        }

        private bool TryGetHostData(out Vector2 center, out Vector2 direction, out float charge, out int damage, out bool canDealDamage)
        {
            center = Vector2.Zero;
            direction = Vector2.UnitX;
            charge = 0f;
            damage = Projectile.damage;
            canDealDamage = false;

            if (HostIndex < 0 || HostIndex >= Main.maxProjectiles)
                return false;

            Projectile host = Main.projectile[HostIndex];
            if (!host.active || host.owner != Projectile.owner)
                return false;

            if (HostKind == BeamHostKind.MainHoldout)
            {
                if (host.type != ModContent.ProjectileType<YC_TyrantPrismHoldout>() ||
                    host.ModProjectile is not YC_TyrantPrismHoldout holdout)
                {
                    return false;
                }

                center = holdout.MainMuzzle;
                direction = holdout.ForwardDirection;
                charge = MathHelper.Lerp(0f, MaxCharge, holdout.ConvergenceRatio);
                if (holdout.CurrentState != YC_TyrantPrismHoldout.TyrantPrismState.Converging)
                    charge = MaxCharge;
                damage = host.damage;
                canDealDamage = holdout.MainBeamCanDamage;
                return true;
            }

            if (host.type != ModContent.ProjectileType<YC_TyrantPrismDrone>() ||
                host.ModProjectile is not YC_TyrantPrismDrone drone)
            {
                return false;
            }

            direction = drone.CurrentForwardDirection.SafeNormalize(host.velocity.SafeNormalize(Vector2.UnitX * Main.player[Projectile.owner].direction));
            center = host.Center + direction * 10f;
            damage = host.damage;

            if (YC_EXHelper.TryGetActiveVip(Projectile.owner, out _, out YC_EX_VIP vip))
            {
                if (vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge)
                    charge = MathHelper.Lerp(0f, MaxCharge, MathHelper.Clamp(vip.CurrentStateTimer / (float)YC_EX_VIP.DroneChargeTime, 0f, 1f));
                else if (vip.CurrentState is YC_EX_VIP.EXVipState.AwaitingFireCommand or YC_EX_VIP.EXVipState.Firing)
                    charge = MaxCharge;
                else
                    charge = 0f;

                if (vip.CurrentState == YC_EX_VIP.EXVipState.Firing)
                    damage = (int)(host.damage * 5.5f);

                canDealDamage = vip.CurrentState == YC_EX_VIP.EXVipState.Firing;
                return true;
            }

            if (drone.TryGetActiveHoldout(out _, out YC_TyrantPrismHoldout activeHoldout))
            {
                charge = MathHelper.Lerp(0f, MaxCharge, activeHoldout.ConvergenceRatio);
                if (activeHoldout.CurrentState != YC_TyrantPrismHoldout.TyrantPrismState.Converging)
                    charge = MaxCharge;
                canDealDamage = activeHoldout.MainBeamCanDamage;
                return true;
            }

            charge = 0f;
            canDealDamage = false;
            return true;
        }

        private static float GetDamageMultiplier(float chargeRatio)
        {
            float f = chargeRatio * chargeRatio * chargeRatio;
            return MathHelper.Lerp(1f, MaxDamageMultiplier, f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<Dragonfire>(), 180);
        public override void OnHitPlayer(Player target, Player.HurtInfo info) => target.AddBuff(ModContent.BuffType<Dragonfire>(), 180);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (projHitbox.Intersects(targetHitbox))
                return true;

            float _ = float.NaN;
            return Collision.CheckAABBvLineCollision(
                targetHitbox.TopLeft(),
                targetHitbox.Size(),
                Projectile.Center,
                Projectile.Center + Projectile.velocity * BeamLength,
                BeamHitboxCollisionWidth * Projectile.scale,
                ref _);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Projectile.velocity == Vector2.Zero)
                return false;

            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            float beamLength = BeamLength;
            Vector2 centerFloored = Projectile.Center.Floor() + Projectile.velocity * Projectile.scale * BeamRenderTileOffset;
            Vector2 scaleVec = new(Projectile.scale);

            beamLength -= BeamLengthReductionFactor * Projectile.scale * Projectile.scale;
            if (beamLength <= 0f)
                return false;

            DelegateMethods.f_1 = 1f;
            Vector2 beamStartPos = centerFloored - Main.screenPosition;
            Vector2 beamEndPos = beamStartPos + Projectile.velocity * beamLength;
            Utils.LaserLineFraming llf = new(DelegateMethods.RainbowLaserDraw);

            Color outerBeamColor = GetBeamColor();
            DelegateMethods.c_1 = outerBeamColor * OuterBeamOpacityMultiplier * Projectile.Opacity;
            Utils.DrawLaser(Main.spriteBatch, tex, beamStartPos, beamEndPos, scaleVec, llf);

            scaleVec *= 0.5f;
            DelegateMethods.c_1 = Color.White * InnerBeamOpacityMultiplier * Projectile.Opacity;
            Utils.DrawLaser(Main.spriteBatch, tex, beamStartPos, beamEndPos, scaleVec, llf);
            return false;
        }

        private void ProduceBeamDust(Color beamColor)
        {
            if (BeamLength <= MainDustBeamEndOffset * Projectile.scale)
                return;

            Vector2 laserEndPos = Projectile.Center + Projectile.velocity * (BeamLength - MainDustBeamEndOffset * Projectile.scale);
            for (int i = 0; i < 2; ++i)
            {
                float dustAngle = Projectile.rotation + (Main.rand.NextBool() ? 1f : -1f) * MathHelper.PiOver2;
                float dustStartDist = Main.rand.NextFloat(1f, 1.8f);
                Vector2 dustVel = dustAngle.ToRotationVector2() * dustStartDist;
                int d = Dust.NewDust(laserEndPos, 0, 0, DustID.CopperCoin, dustVel.X, dustVel.Y, 0, beamColor, 3.3f);
                Main.dust[d].color = beamColor;
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 1.2f;

                if (Projectile.scale > 1f)
                {
                    Main.dust[d].velocity *= Projectile.scale;
                    Main.dust[d].scale *= Projectile.scale;
                }

                if (Projectile.scale != MaxBeamScale)
                {
                    Dust smallDust = Dust.NewDustPerfect(
                        Main.dust[d].position,
                        DustID.CopperCoin,
                        Main.dust[d].velocity,
                        0,
                        beamColor,
                        Main.dust[d].scale * 0.5f);
                    smallDust.noGravity = true;
                }
            }

            if (Main.rand.NextBool(5))
            {
                Vector2 dustOffset = Projectile.velocity.RotatedBy(MathHelper.PiOver2) * (Main.rand.NextFloat() - 0.5f) * Projectile.width;
                Vector2 dustPos = laserEndPos + dustOffset - Vector2.One * SidewaysDustBeamEndOffset;
                int d = Dust.NewDust(dustPos, 8, 8, DustID.CopperCoin, 0f, 0f, 100, beamColor, 5f);
                Main.dust[d].velocity *= 0.5f;
                Main.dust[d].velocity.Y = -Math.Abs(Main.dust[d].velocity.Y);
            }

            if (HostKind == BeamHostKind.TyrantDrone && Main.rand.NextBool(6))
            {
                Vector2 glowPosition = Projectile.Center + Projectile.velocity * Main.rand.NextFloat(24f, Math.Max(25f, BeamLength - 18f));
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    glowPosition + Projectile.velocity.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-4f, 4f),
                    -Projectile.velocity * Main.rand.NextFloat(0.15f, 0.65f),
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.14f, 0.26f),
                    Color.Lerp(beamColor, Color.White, Main.rand.NextFloat(0.2f, 0.55f)),
                    true,
                    false,
                    true));
            }
        }

        private Color GetBeamColor()
        {
            float customHue = GetHue(Projectile.ai[0]);
            const float sat = 0.66f;
            Color c = Main.hslToRgb(customHue, sat, 0.53f);
            c.A = 64;
            return c;
        }

        private float GetHue(float indexing)
        {
            string name = Main.player[Projectile.owner].name ?? "";
            if (Main.player[Projectile.owner].active)
            {
                switch (name)
                {
                    case "Ziggums":
                        return 2f;
                    case "Poly":
                        return 0.83f;
                    case "Zach":
                        return 1.5f + (float)Math.Cos(Main.time / 180.0 * Math.PI * 2.0) * 0.1f;
                    case "Grox the Great":
                        return 1.27f;
                    case "Jenosis":
                        return 0.65f + (float)Math.Cos(Main.time / 180.0 * Math.PI * 2.0) * 0.1f;
                    case "DM DOKURO":
                        return 0f;
                    case "Uncle Danny":
                    case "Phoenix":
                        return 1.7f + (float)Math.Cos(Main.time / 180.0 * Math.PI * 2.0) * 0.07f;
                    case "Minecat":
                        return 0.15f + (float)Math.Cos(Main.time / 180.0 * Math.PI * 2.0) * 0.07f;
                    case "Khaelis":
                        return 1.15f + (float)Math.Cos(Main.time / 180.0 * Math.PI * 2.0) * 0.18f;
                    case "Purple Necromancer":
                        return 1.7f + (float)Math.Cos(Main.time / 120.0 * Math.PI * 2.0) * 0.05f;
                    case "gamagamer64":
                        return 0.83f + (float)Math.Cos(Main.time / 120.0 * Math.PI * 2.0) * 0.03f;
                    case "Svante":
                        return 1.4f + (float)Math.Cos(Main.time / 180.0 * Math.PI * 2.0) * 0.06f;
                    case "Puff":
                        return 0.31f + (float)Math.Cos(Main.time / 120.0 * Math.PI * 2.0) * 0.13f;
                    case "Leviathan":
                        return 1.9f + (float)Math.Cos(Main.time / 180.0 * Math.PI * 2.0) * 0.1f;
                    case "Testdude":
                        return Main.rand.NextFloat();
                }
            }

            return indexing / NumBeams % 0.12f;
        }

        public override void CutTiles()
        {
            DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
            Utils.TileActionAttempt cut = DelegateMethods.CutTiles;
            Vector2 beamStartPos = Projectile.Center;
            Vector2 beamEndPos = beamStartPos + Projectile.velocity * BeamLength;
            YC_BeamWorldSafety.SafePlotTileLine(beamStartPos, beamEndPos, Projectile.width * Projectile.scale, cut);
        }
    }
}
