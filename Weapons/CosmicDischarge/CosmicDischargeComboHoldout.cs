using System;
using System.Collections.Generic;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class CosmicDischargeComboHoldout : ModProjectile, ILocalizedModType
    {
        private const int WhipDuration = 40;
        private const int WhipThrustDuration = 44;
        private const int WhipThrustWindup = 11;
        private const int SwordSwingDuration = 31;
        private const int SwordFinisherDuration = 64;
        private const int SwordFinisherWindup = 34;
        private const int QuickDrawDuration = 34;
        private const float WhipReach = 460f;
        private const float ThrustReach = 500f;
        private const float QuickDrawReach = 550f;
        private const float CollisionWidth = 28f;

        public new string LocalizationCategory => "Projectiles.Melee";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private readonly List<Vector2> bladePoints = new();
        private readonly HashSet<int> tipHitTargets = new();
        private bool wasRightHeld;
        private bool spawnedSwordWave;
        private int spawnedBombBursts;

        private CosmicDischargeAttackKind Kind
        {
            get => (CosmicDischargeAttackKind)(int)Projectile.ai[0];
            set => Projectile.ai[0] = (int)value;
        }

        private ref float AimAngle => ref Projectile.ai[1];
        private ref float Time => ref Projectile.localAI[0];
        private ref float QuickDrawQueued => ref Projectile.localAI[1];
        private Player Owner => Main.player[Projectile.owner];
        private Vector2 AimDirection => AimAngle.ToRotationVector2();

        public bool CanBecomeQuickDraw =>
            (Kind == CosmicDischargeAttackKind.WhipThrust && Time <= WhipThrustWindup) ||
            (Kind == CosmicDischargeAttackKind.SwordFinisher && Time <= SwordFinisherWindup);

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.MeleeNoSpeed;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
            Projectile.ownerHitCheck = true;
            Projectile.coldDamage = true;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.timeLeft = 2;
            if (AimAngle == 0f)
                AimAngle = Vector2.UnitX.RotatedByRandom(0.01f).ToRotation();
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendCosmicDischarge>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.timeLeft = 2;
            Owner.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == Projectile.owner)
                Owner.Calamity().rightClickListener = true;

            bool rightHeld = Main.myPlayer == Projectile.owner && Owner.Calamity().mouseRight;
            if (rightHeld && !wasRightHeld && CanBecomeQuickDraw)
                QuickDrawQueued = 1f;
            wasRightHeld = rightHeld;

            if (QuickDrawQueued > 0f && Kind != CosmicDischargeAttackKind.QuickDraw)
                BeginQuickDraw();

            Time++;
            Projectile.localNPCHitCooldown = Kind == CosmicDischargeAttackKind.QuickDraw ? 3 : 8;

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                    UpdateWhipArc(-1f);
                    break;
                case CosmicDischargeAttackKind.WhipUnder:
                    UpdateWhipArc(1f);
                    break;
                case CosmicDischargeAttackKind.WhipThrust:
                    UpdateWhipThrust(false);
                    break;
                case CosmicDischargeAttackKind.SwordSwingOne:
                    UpdateSwordSwing(false);
                    break;
                case CosmicDischargeAttackKind.SwordSwingTwo:
                    UpdateSwordSwing(true);
                    break;
                case CosmicDischargeAttackKind.SwordFinisher:
                    UpdateSwordFinisher();
                    break;
                case CosmicDischargeAttackKind.QuickDraw:
                    UpdateWhipThrust(true);
                    break;
            }

            if (bladePoints.Count > 0)
            {
                Projectile.Center = bladePoints[^1];
                Projectile.rotation = (bladePoints[^1] - bladePoints[^2]).ToRotation() + MathHelper.PiOver2;
            }

            SpawnAmbientDust();
        }

        public bool TryRequestQuickDraw()
        {
            if (!CanBecomeQuickDraw)
                return false;

            QuickDrawQueued = 1f;
            Projectile.netUpdate = true;
            return true;
        }

        private void BeginQuickDraw()
        {
            Vector2 direction = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection);
            Kind = CosmicDischargeAttackKind.QuickDraw;
            AimAngle = direction.ToRotation();
            Time = 0f;
            QuickDrawQueued = 0f;
            tipHitTargets.Clear();
            spawnedBombBursts = 0;
            spawnedSwordWave = false;
            Projectile.localNPCHitCooldown = 3;
            Projectile.netUpdate = true;

            Owner.GetModPlayer<CosmicDischargePlayer>().AddUltimateEnergy(CosmicDischargePlayer.RightThrustEnergyGain);
            SoundEngine.PlaySound(SoundID.Item125 with { Volume = 0.82f, Pitch = 0.22f }, Owner.Center);
            Owner.SetImmuneTimeForAllTypes(8);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                    Owner.MountedCenter,
                    direction * 1.5f,
                    CosmicDischargeCommon.FrostGlowColor * 0.38f,
                    Vector2.One,
                    direction.ToRotation(),
                    0.02f,
                    0.16f,
                    14));
            }
        }

        private void UpdateWhipArc(float side)
        {
            Vector2 direction = AimDirection;
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction);

            float extend = Time <= 18f
                ? Smooth(Time / 18f)
                : Time <= 22f
                    ? 1f
                    : 1f - MathF.Pow(Utils.GetLerpValue(22f, WhipDuration, Time, true), 0.55f);

            float reach = MathHelper.Lerp(56f, WhipReach, MathHelper.Clamp(extend, 0f, 1f));
            float sideBend = side * MathHelper.Lerp(150f, 76f, extend);
            float curl = side * MathHelper.Lerp(90f, 14f, extend);

            bladePoints.Clear();
            bladePoints.AddRange(CosmicDischargeCommon.BuildCurvedBlade(Owner, direction, reach, sideBend, curl));

            if (Time == 1f)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.58f, Pitch = side < 0f ? -0.18f : 0.08f }, Owner.Center);

            if (Time >= WhipDuration)
                Projectile.Kill();
        }

        private void UpdateWhipThrust(bool quickDraw)
        {
            int duration = quickDraw ? QuickDrawDuration : WhipThrustDuration;
            int windup = quickDraw ? 4 : WhipThrustWindup;
            int extendFrames = quickDraw ? 8 : 11;
            int holdFrames = quickDraw ? 5 : 5;
            float reachMax = quickDraw ? QuickDrawReach : ThrustReach;

            if (Time <= windup)
            {
                Vector2 liveDirection = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection);
                AimAngle = liveDirection.ToRotation();
            }

            Vector2 direction = AimDirection;
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction);

            float reach;
            if (Time <= windup)
            {
                reach = MathHelper.Lerp(52f, 115f, Smooth(Time / windup));
            }
            else if (Time <= windup + extendFrames)
            {
                float t = Smooth((Time - windup) / extendFrames);
                reach = MathHelper.Lerp(115f, reachMax, t);
            }
            else if (Time <= windup + extendFrames + holdFrames)
            {
                reach = reachMax;
            }
            else
            {
                float t = MathF.Pow(Utils.GetLerpValue(windup + extendFrames + holdFrames, duration, Time, true), quickDraw ? 0.38f : 0.55f);
                reach = MathHelper.Lerp(reachMax, 58f, t);
            }

            float sideBend = quickDraw ? 8f * Owner.direction : 22f * MathF.Sin(Time * 0.2f);
            bladePoints.Clear();
            bladePoints.AddRange(CosmicDischargeCommon.BuildCurvedBlade(Owner, direction, reach, sideBend, quickDraw ? 0f : 12f, quickDraw ? 20 : 18));

            if (Time == windup + 1f)
                SoundEngine.PlaySound(SoundID.Item122 with { Volume = quickDraw ? 0.8f : 0.62f, Pitch = quickDraw ? 0.25f : 0.05f }, Projectile.Center);

            if (quickDraw)
                SpawnQuickDrawBombs();

            if (Time >= duration)
                Projectile.Kill();
        }

        private void UpdateSwordSwing(bool second)
        {
            Vector2 direction = AimDirection;
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction);

            float progress = Smooth(Time / SwordSwingDuration);
            float swingSide = Owner.direction;
            float start = second ? 1.05f : -1.15f;
            float end = second ? -1.15f : 1.05f;
            float angle = AimAngle + MathHelper.Lerp(start * swingSide, end * swingSide, progress);
            Vector2 bladeDirection = angle.ToRotationVector2();
            float reach = MathHelper.Lerp(154f, 240f, MathF.Sin(MathHelper.Pi * progress));
            float bend = (second ? -1f : 1f) * Owner.direction * 34f * MathF.Sin(MathHelper.Pi * progress);

            bladePoints.Clear();
            bladePoints.AddRange(CosmicDischargeCommon.BuildCurvedBlade(Owner, bladeDirection, reach, bend, 18f, 12));

            if (Time == 1f)
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.52f, Pitch = second ? 0.12f : -0.08f }, Owner.Center);

            if (Time >= SwordSwingDuration)
                Projectile.Kill();
        }

        private void UpdateSwordFinisher()
        {
            if (Time <= SwordFinisherWindup)
            {
                Vector2 liveDirection = CosmicDischargeCommon.GetAimDirection(Owner, AimDirection);
                AimAngle = liveDirection.ToRotation();
            }

            Vector2 direction = AimDirection;
            CosmicDischargeCommon.HoldPlayer(Owner, Projectile, direction);

            if (Time <= SwordFinisherWindup)
            {
                float spin = Time / SwordFinisherWindup;
                float angle = AimAngle + Owner.direction * MathHelper.TwoPi * 2f * spin - MathHelper.PiOver2 * Owner.direction;
                Vector2 bladeDirection = angle.ToRotationVector2();
                float reach = MathHelper.Lerp(110f, 178f, 0.45f + 0.55f * MathF.Sin(MathHelper.Pi * spin));

                bladePoints.Clear();
                bladePoints.AddRange(CosmicDischargeCommon.BuildCurvedBlade(Owner, bladeDirection, reach, Owner.direction * 18f, 12f, 12));

                if (Time == 1f)
                    SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.48f, Pitch = 0.18f }, Owner.Center);

                if (Time % 8f == 0f && !Main.dedServ)
                {
                    Dust dust = Dust.NewDustPerfect(
                        Owner.MountedCenter + Main.rand.NextVector2Circular(38f, 38f),
                        DustID.Frost,
                        bladeDirection * Main.rand.NextFloat(1f, 3.4f),
                        120,
                        CosmicDischargeCommon.FrostGlowColor,
                        Main.rand.NextFloat(0.85f, 1.2f));
                    dust.noGravity = true;
                }
            }
            else
            {
                float slashProgress = Smooth((Time - SwordFinisherWindup) / (SwordFinisherDuration - SwordFinisherWindup));
                float angle = AimAngle + MathHelper.Lerp(-1.28f * Owner.direction, 1.02f * Owner.direction, slashProgress);
                Vector2 bladeDirection = angle.ToRotationVector2();
                float reach = MathHelper.Lerp(265f, 208f, Utils.GetLerpValue(0.65f, 1f, slashProgress, true));

                bladePoints.Clear();
                bladePoints.AddRange(CosmicDischargeCommon.BuildCurvedBlade(Owner, bladeDirection, reach, Owner.direction * 30f, 8f, 13));

                if (!spawnedSwordWave)
                {
                    spawnedSwordWave = true;
                    SpawnSwordWave(direction);
                    SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.72f, Pitch = -0.24f }, Owner.Center);
                }
            }

            if (Time >= SwordFinisherDuration)
                Projectile.Kill();
        }

        private void SpawnSwordWave(Vector2 direction)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Owner.MountedCenter + direction * 70f,
                direction * 15f,
                ModContent.ProjectileType<CosmicDischargeSwordWave>(),
                (int)(Projectile.damage * 0.72f),
                Projectile.knockBack,
                Projectile.owner);
        }

        private void SpawnQuickDrawBombs()
        {
            int bombCount = CosmicDischargeProgression.QuickDrawIceBombCount;
            if (bombCount <= 0 || Main.myPlayer != Projectile.owner || bladePoints.Count < 4)
                return;

            if (spawnedBombBursts >= CosmicDischargeProgression.QuickDrawIceBombBursts)
                return;

            bool shouldBurst = Time == 10f || Time == 16f || Time == 22f;
            if (!shouldBurst)
                return;

            spawnedBombBursts++;
            for (int i = 0; i < bombCount; i++)
            {
                float t = (i + 0.5f) / bombCount;
                int pointIndex = (int)MathHelper.Clamp(t * (bladePoints.Count - 1), 1, bladePoints.Count - 1);
                Vector2 spawnPosition = bladePoints[pointIndex] + Main.rand.NextVector2Circular(22f, 22f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Main.rand.NextVector2Circular(1.4f, 1.4f),
                    ModContent.ProjectileType<CosmicDischargeIceBomb>(),
                    (int)(Projectile.damage * CosmicDischargeProgression.QuickDrawIceBombDamageFactor),
                    0f,
                    Projectile.owner,
                    Main.rand.NextFloat(16f, 28f));
            }
        }

        private void SpawnAmbientDust()
        {
            if (Main.dedServ || bladePoints.Count < 2 || Main.rand.NextBool(2))
                return;

            Vector2 point = bladePoints[Main.rand.Next(1, bladePoints.Count)];
            Vector2 direction = (point - Owner.MountedCenter).SafeNormalize(AimDirection);
            Dust dust = Dust.NewDustPerfect(
                point + Main.rand.NextVector2Circular(6f, 6f),
                Main.rand.NextBool() ? DustID.Frost : DustID.SnowflakeIce,
                direction.RotatedByRandom(0.5f) * Main.rand.NextFloat(0.25f, 1.6f),
                120,
                CosmicDischargeCommon.FrostCoreColor,
                Main.rand.NextFloat(0.85f, 1.28f));
            dust.noGravity = true;
        }

        public override bool? CanDamage()
        {
            return Kind switch
            {
                CosmicDischargeAttackKind.WhipOver or CosmicDischargeAttackKind.WhipUnder
                    => Time >= 4f && Time <= WhipDuration - 4f,
                CosmicDischargeAttackKind.WhipThrust
                    => Time >= WhipThrustWindup + 2f && Time <= WhipThrustDuration - 5f,
                CosmicDischargeAttackKind.SwordSwingOne or CosmicDischargeAttackKind.SwordSwingTwo
                    => Time >= 4f && Time <= SwordSwingDuration - 4f,
                CosmicDischargeAttackKind.SwordFinisher
                    => Time >= SwordFinisherWindup + 2f && Time <= SwordFinisherDuration - 5f,
                CosmicDischargeAttackKind.QuickDraw
                    => Time >= 6f && Time <= QuickDrawDuration - 3f,
                _ => false
            };
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            return CosmicDischargeCommon.CheckCurveCollision(bladePoints, targetHitbox, CollisionWidth);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            bool tip = CosmicDischargeCommon.TargetIntersectsTip(bladePoints, target.Hitbox, Kind == CosmicDischargeAttackKind.QuickDraw ? 42f : 36f);

            switch (Kind)
            {
                case CosmicDischargeAttackKind.WhipOver:
                case CosmicDischargeAttackKind.WhipUnder:
                    if (Time > 22f)
                    {
                        modifiers.FinalDamage *= 0.38f;
                        modifiers.Knockback *= 0.35f;
                    }
                    break;

                case CosmicDischargeAttackKind.WhipThrust:
                    modifiers.FinalDamage *= tip ? 2.25f : 0.78f;
                    modifiers.Knockback *= tip ? 1.3f : 0.65f;
                    break;

                case CosmicDischargeAttackKind.SwordFinisher:
                    modifiers.FinalDamage *= 1.45f;
                    modifiers.Knockback *= 1.35f;
                    break;

                case CosmicDischargeAttackKind.QuickDraw:
                    modifiers.FinalDamage *= tip && !tipHitTargets.Contains(target.whoAmI) ? 2.8f : 0.54f;
                    modifiers.Knockback *= tip ? 1.45f : 0.35f;
                    break;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool tip = CosmicDischargeCommon.TargetIntersectsTip(bladePoints, target.Hitbox, Kind == CosmicDischargeAttackKind.QuickDraw ? 42f : 36f);
            CosmicDischargeCommon.ApplyColdDebuffs(target, Kind == CosmicDischargeAttackKind.QuickDraw ? 180 : 120);

            if (tip)
                tipHitTargets.Add(target.whoAmI);

            if (Main.myPlayer == Projectile.owner && (tip || Kind == CosmicDischargeAttackKind.SwordFinisher))
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<CalamityMod.Projectiles.Melee.CosmicIceBurst>(),
                    (int)(Projectile.damage * (Kind == CosmicDischargeAttackKind.QuickDraw ? 0.5f : 0.36f)),
                    0f,
                    Projectile.owner,
                    0f,
                    0.9f);
            }

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(
                    target.Center,
                    Vector2.Zero,
                    CosmicDischargeCommon.FrostCoreColor * 0.32f,
                    0.38f,
                    16));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (bladePoints.Count < 2)
                return false;

            Color drawColor = Color.Lerp(CosmicDischargeCommon.FrostDarkColor, CosmicDischargeCommon.FrostCoreColor, 0.72f);
            float scale = Kind == CosmicDischargeAttackKind.QuickDraw ? 1.06f : 0.96f;
            CosmicDischargeCommon.DrawCurvedChain(Main.spriteBatch, bladePoints, drawColor, scale, Owner.gfxOffY);
            DrawBladeGlow();

            if (CanBecomeQuickDraw)
                CosmicDischargeCommon.DrawRightHoldIndicator(Main.spriteBatch, Owner, 0.95f);

            return false;
        }

        private void DrawBladeGlow()
        {
            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Color color = Color.Lerp(CosmicDischargeCommon.FrostGlowColor, Color.White, 0.2f) * 0.36f;

            for (int i = 0; i < bladePoints.Count - 1; i++)
            {
                Vector2 start = bladePoints[i] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 end = bladePoints[i + 1] - Main.screenPosition + Vector2.UnitY * Owner.gfxOffY;
                Vector2 segment = end - start;
                float length = segment.Length();
                if (length < 2f)
                    continue;

                Main.EntitySpriteDraw(
                    pixel,
                    start,
                    new Rectangle(0, 0, 1, 1),
                    color * (i / (float)bladePoints.Count),
                    segment.ToRotation(),
                    new Vector2(0f, 0.5f),
                    new Vector2(length, Kind == CosmicDischargeAttackKind.QuickDraw ? 6f : 4f),
                    SpriteEffects.None);
            }
        }

        private static float Smooth(float value)
        {
            value = MathHelper.Clamp(value, 0f, 1f);
            return value * value * (3f - 2f * value);
        }
    }
}
