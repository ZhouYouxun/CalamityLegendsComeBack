using System;
using System.IO;
using System.Linq;
using CalamityLegendsComeBack.Accssory.BF.Common;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick;
using CalamityMod;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using CalamityMod.Projectiles.Melee;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick
{
    // D tactical right-click arrow: mortar trajectory with a bombard anchor on impact.
    internal class BFArrow_DBomb : ModProjectile, IPixelatedPrimitiveRenderer
    {
        private const float FlightState = 0f;
        private const float AttachedNpcState = 1f;
        private const float GroundAnchorState = 2f;
        private const int DefaultBombardWaveCount = 8;
        private const int FramesPerBombardWave = 5;
        private const float MortarGravity = 0.28f;
        private const float MortarFallGravityMultiplier = 2.5f;
        private const float MinMortarApexHeight = 620f;
        private const float MaxMortarApexHeight = 1120f;
        private const int MortarCollisionDelay = 12;
        private const int ReturnDelayFrames = 45;
        private const float ReturnSpeedMultiplier = 0.7f;

        private int rainCounter;
        private int bombardWaveCount = DefaultBombardWaveCount;
        private int storedRainDamage = 1;
        private int storedAmmoType = ProjectileID.WoodenArrowFriendly;
        private float storedAmmoSpeed = 14f;
        private float storedAmmoKnockback = 2f;
        private float explosionSize = 190f;
        private float skyRainMultiplier = 1f;
        private Vector2 stickOffset;
        private Vector2 targetPoint;
        private Vector2 groundAnchorPoint;
        private Vector2 delayedReturnVelocity;
        private Vector2 bombardFallStart;
        private bool passedBombardTarget;
        private bool pendingBombardTeleport;
        private bool detonated;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/RightClick/DBomb/BFArrow_DBomb";
        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];
        private ref float ReturnDelayTimer => ref Projectile.localAI[1];

        private bool InFlight => State == FlightState;
        private bool AttachedToNpc => State == AttachedNpcState;
        private bool AnchoredToGround => State == GroundAnchorState;
        private int BombardDuration => Math.Max(1, bombardWaveCount) * FramesPerBombardWave;
        private static Color HighlightColor => Color.Lerp(Color.Goldenrod, Color.Khaki, 0.5f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 16;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 240, penetrate: -1, extraUpdates: 1, tileCollide: false);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => InFlight && ReturnDelayTimer <= 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => InFlight && ReturnDelayTimer <= 0f ? null : false;

        public static Vector2 CalculateMortarLaunchVelocity(Vector2 start, Vector2 target, float desiredSpeed)
        {
            float distance = Vector2.Distance(start, target);
            float speedFactor = Utils.GetLerpValue(14f, 22f, desiredSpeed, true);
            float apexHeight = MathHelper.Lerp(MinMortarApexHeight, MaxMortarApexHeight, Utils.GetLerpValue(120f, 900f, distance, true));
            apexHeight = MathHelper.Lerp(apexHeight + 72f, apexHeight - 24f, speedFactor);

            float apexY = Math.Min(start.Y, target.Y) - apexHeight;
            float riseDistance = Math.Max(start.Y - apexY, 96f);
            float fallDistance = Math.Max(target.Y - apexY, 96f);
            float verticalSpeed = (float)Math.Sqrt(2f * MortarGravity * riseDistance);
            float travelTime = verticalSpeed / MortarGravity + (float)Math.Sqrt(2f * fallDistance / (MortarGravity * MortarFallGravityMultiplier));
            float horizontalSpeed = (target.X - start.X) / Math.Max(travelTime, 1f);

            return new Vector2(horizontalSpeed, -verticalSpeed);
        }

        public void ConfigureBombardTarget(Vector2 bombardTarget, float strikeExplosionSize = 190f, float rainMultiplier = 1f, int waveCount = DefaultBombardWaveCount)
        {
            targetPoint = bombardTarget;
            explosionSize = MathHelper.Clamp(strikeExplosionSize, 96f, 720f);
            skyRainMultiplier = MathHelper.Clamp(rainMultiplier, 1f, 3f);
            bombardWaveCount = Utils.Clamp(waveCount, 1, 30);
            float desiredSpeed = Projectile.velocity.Length();
            if (desiredSpeed <= 0.01f)
                desiredSpeed = 18f;

            Player owner = Main.player[Projectile.owner];
            float gravDir = owner.active ? owner.gravDir : 1f;
            Vector2 fallStart = bombardTarget - Vector2.UnitY * 980f * gravDir + new Vector2(Main.rand.NextFloat(-84f, 84f), 0f);
            Vector2 fallDirection = (bombardTarget - fallStart).SafeNormalize(Vector2.UnitY * gravDir);
            bombardFallStart = fallStart;
            delayedReturnVelocity = fallDirection * Math.Max(42f, desiredSpeed * 2.2f);
            pendingBombardTeleport = true;
            ReturnDelayTimer = ReturnDelayFrames * (Projectile.extraUpdates + 1);
            FlightTimer = 0f;
            Projectile.tileCollide = false;
            passedBombardTarget = false;
            BFArrowCommon.FaceForward(Projectile);
            Projectile.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(targetPoint);
            writer.WriteVector2(groundAnchorPoint);
            writer.WriteVector2(stickOffset);
            writer.WriteVector2(delayedReturnVelocity);
            writer.WriteVector2(bombardFallStart);
            writer.Write(rainCounter);
            writer.Write(storedRainDamage);
            writer.Write(storedAmmoType);
            writer.Write(storedAmmoSpeed);
            writer.Write(storedAmmoKnockback);
            writer.Write(explosionSize);
            writer.Write(skyRainMultiplier);
            writer.Write(bombardWaveCount);
            writer.Write(ReturnDelayTimer);
            writer.Write(passedBombardTarget);
            writer.Write(pendingBombardTeleport);
            writer.Write(detonated);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            targetPoint = reader.ReadVector2();
            groundAnchorPoint = reader.ReadVector2();
            stickOffset = reader.ReadVector2();
            delayedReturnVelocity = reader.ReadVector2();
            bombardFallStart = reader.ReadVector2();
            rainCounter = reader.ReadInt32();
            storedRainDamage = reader.ReadInt32();
            storedAmmoType = reader.ReadInt32();
            storedAmmoSpeed = reader.ReadSingle();
            storedAmmoKnockback = reader.ReadSingle();
            explosionSize = reader.ReadSingle();
            skyRainMultiplier = reader.ReadSingle();
            bombardWaveCount = reader.ReadInt32();
            ReturnDelayTimer = reader.ReadSingle();
            passedBombardTarget = reader.ReadBoolean();
            pendingBombardTeleport = reader.ReadBoolean();
            detonated = reader.ReadBoolean();
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            storedRainDamage = Math.Max(Projectile.damage, 1);
            Projectile.tileCollide = false;

            Player owner = Main.player[Projectile.owner];
            if (BFArrowCommon.TryPickBlossomFluxAmmo(owner, out int ammoType, out float ammoSpeed, out _, out float ammoKnockback))
            {
                storedAmmoType = ammoType;
                storedAmmoSpeed = ammoSpeed;
                storedAmmoKnockback = ammoKnockback;
            }

            if (targetPoint == Vector2.Zero)
                targetPoint = Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitY) * 480f;

            BFArrowCommon.FaceForward(Projectile);
        }

        public override void AI()
        {
            Lighting.AddLight(
                Projectile.Center,
                BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb).ToVector3() * (InFlight ? 0.48f : 0.62f));

            if (InFlight)
            {
                UpdateMortarFlight();
                return;
            }

            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.velocity = Vector2.Zero;

            if (AttachedToNpc)
            {
                if (!BFArrowCommon.InBounds(AttachedNpcIndex, Main.maxNPCs))
                {
                    Projectile.Kill();
                    return;
                }

                NPC attachedNpc = Main.npc[(int)AttachedNpcIndex];
                if (!attachedNpc.active || attachedNpc.dontTakeDamage)
                {
                    Projectile.Kill();
                    return;
                }

                groundAnchorPoint = attachedNpc.Center;
                Projectile.Center = attachedNpc.Center + stickOffset;
                Projectile.gfxOffY = attachedNpc.gfxOffY;
                UpdateBombardAnchor(attachedNpc.Center);
                return;
            }

            if (!AnchoredToGround)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = groundAnchorPoint;
            Projectile.gfxOffY = 0f;
            UpdateBombardAnchor(groundAnchorPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!InFlight)
                return;

            DetonateBombardStrike(Projectile.Center, target);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!InFlight)
                return false;

            DetonateBombardStrike(Projectile.Center, null);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 14, 1.5f, 5.5f, 0.95f, 1.35f);
            if (!Main.dedServ)
                SpawnBombardImpactFX(Projectile.Center, 1.2f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 originalCenter = Projectile.Center;
            float rumble = GetRumbleStrength();
            if (rumble > 0f)
                Projectile.Center += Main.rand.NextVector2Circular(rumble, rumble);

            if (InFlight)
            {
                Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                DrawArrowHelix(Projectile.Center - Main.screenPosition, forward);
            }

            BFArrowCommon.DrawPresetArrow(
                Projectile,
                lightColor,
                BlossomFluxChloroplastPresetType.Chlo_DBomb,
                AnchoredToGround ? 1.03f : 1f,
                InFlight);

            Projectile.Center = originalCenter;
            DrawBombardHighlightOverlay();
            return false;
        }

        // ─── 双层拖尾（橙火风格，仅飞行阶段）────────────────────────────────
        private Vector2[] BuildTrailPoints()
        {
            Vector2[] pts = Projectile.oldPos
                .Where(p => p != Vector2.Zero)
                .Select(p => p + Projectile.Size * 0.5f)
                .ToArray();
            if (pts.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };
            if (pts[0] != Projectile.Center)
                pts = new[] { Projectile.Center }.Concat(pts).ToArray();
            return pts;
        }

        private float OuterWidthFunc(float t, Vector2 _)
        {
            float max = Projectile.scale * 22f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color OuterColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(new Color(255, 175, 35), new Color(160, 55, 10), t * 0.65f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.70f, 1f, t, true));
            c.A = 0;
            return c;
        }

        private float CoreWidthFunc(float t, Vector2 _)
        {
            float max = Projectile.scale * 11f;
            return t < 0.16f
                ? MathF.Sin(t / 0.16f * MathHelper.PiOver2) * max
                : Utils.Remap(t, 0.16f, 1f, max, 0f);
        }

        private Color CoreColorFunc(float t, Vector2 _)
        {
            Color c = Color.Lerp(Color.White, new Color(255, 225, 110), t * 0.5f) * Projectile.Opacity;
            c = Color.Lerp(c, Color.Transparent, Utils.GetLerpValue(0.72f, 1f, t, true));
            c.A = 0;
            return c;
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (!InFlight) return;

            Vector2[] pts = BuildTrailPoints();
            if (pts.Length < 2) return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/ScarletDevilStreak"));
            PrimitiveRenderer.RenderTrail(pts,
                new PrimitiveSettings(OuterWidthFunc, OuterColorFunc, (_, _) => Vector2.Zero, true, true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                pts.Length * 2);

            Vector2[] core = pts.Take(Math.Min(9, pts.Length)).ToArray();
            if (core.Length < 2) return;
            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));
            PrimitiveRenderer.RenderTrail(core,
                new PrimitiveSettings(CoreWidthFunc, CoreColorFunc, (_, _) => Vector2.Zero, true, true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                core.Length * 2);
        }

        // ─── 三股炮弹螺旋（橙/金/琥珀，宽展散开）─────────────────────────────
        private void DrawArrowHelix(Vector2 drawPos, Vector2 forward)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            float time = Main.GlobalTimeWrappedHourly * 3.8f + Projectile.identity * 0.29f;
            const int len = 10;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Color[] strandColors = { mainColor, HighlightColor, accentColor };
            for (int strand = 0; strand < 3; strand++)
            {
                float strandOff = strand * MathHelper.TwoPi / 3f;
                Color sc = strandColors[strand];
                for (int i = 0; i < len; i++)
                {
                    float t = i / (float)(len - 1);
                    float angle = time + strandOff - t * 2.8f;
                    float radius = MathHelper.Lerp(18f, 5f, t);
                    Vector2 off = right.RotatedBy(angle) * radius - forward * MathHelper.Lerp(2f, 44f, t);
                    float opacity = MathHelper.Lerp(0.52f, 0.04f, t) * Projectile.Opacity;
                    Main.EntitySpriteDraw(tex, drawPos + off, null, sc with { A = 0 } * opacity,
                        forward.ToRotation() - MathHelper.PiOver2, tex.Size() * 0.5f,
                        new Vector2(0.22f, MathHelper.Lerp(0.92f, 0.32f, t)) * Projectile.scale,
                        SpriteEffects.None, 0);
                }
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
        }

        // ─── 原有逻辑（保留）──────────────────────────────────────────────────
        private void UpdateMortarFlight()
        {
            FlightTimer++;
            if (ReturnDelayTimer > 0f)
            {
                ReturnDelayTimer--;
                UpdateOffscreenBombardTarget();
                Projectile.velocity *= 0.995f;
                Projectile.tileCollide = false;
                Projectile.friendly = false;
                if (ReturnDelayTimer % 6f == 0f)
                    BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 0.55f);//damage

                if (ReturnDelayTimer <= 0f)
                {
                    if (pendingBombardTeleport)
                    {
                        Projectile.Center = bombardFallStart;
                        pendingBombardTeleport = false;
                    }

                    Projectile.velocity = delayedReturnVelocity;
                    Projectile.friendly = true;
                    Projectile.netUpdate = true;
                }

                BFArrowCommon.FaceForward(Projectile);
                return;
            }

            Projectile.velocity.Y += Projectile.velocity.Y > 0f ? MortarGravity * MortarFallGravityMultiplier : MortarGravity;
            AccelerateThroughBombardTarget();
            TryDetonateAtTargetPoint();
            Projectile.tileCollide = false;

            BFArrowCommon.FaceForward(Projectile);
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 1.08f);
            EmitBombardFlightFX();
        }

        private void UpdateOffscreenBombardTarget()
        {
            if (!BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers) || ReturnDelayTimer <= 4f)
                return;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
                return;

            targetPoint = owner.Calamity().mouseWorld == Vector2.Zero ? Main.MouseWorld : owner.Calamity().mouseWorld;
            bombardFallStart = targetPoint - Vector2.UnitY * 980f * owner.gravDir + new Vector2(Main.rand.NextFloat(-84f, 84f), 0f);
            Vector2 fallDirection = (targetPoint - bombardFallStart).SafeNormalize(Vector2.UnitY * owner.gravDir);
            float desiredSpeed = Math.Max(42f, storedAmmoSpeed * 2.2f);
            delayedReturnVelocity = fallDirection * desiredSpeed;

            if (Projectile.owner == Main.myPlayer)
                Projectile.netUpdate = true;
        }

        private void TryDetonateAtTargetPoint()
        {
            if (detonated || !passedBombardTarget || targetPoint == Vector2.Zero)
                return;

            Vector2 toTarget = targetPoint - Projectile.Center;
            if (toTarget.LengthSquared() <= 46f * 46f || Vector2.Dot(Projectile.velocity, toTarget) < 0f)
                DetonateBombardStrike(targetPoint, null);
        }

        private void DetonateBombardStrike(Vector2 center, NPC directTarget)
        {
            if (detonated)
                return;

            detonated = true;
            storedRainDamage = Math.Max(Projectile.damage, storedRainDamage);
            Projectile.damage = 0;
            Projectile.friendly = false;
            Projectile.velocity = Vector2.Zero;
            Projectile.Center = center;
            Projectile.timeLeft = BombardDuration;
            Projectile.tileCollide = false;
            rainCounter = 0;

            if (directTarget != null && directTarget.active && !directTarget.dontTakeDamage)
            {
                State = AttachedNpcState;
                AttachedNpcIndex = directTarget.whoAmI;
                stickOffset = center - directTarget.Center;
                Projectile.Center = directTarget.Center + stickOffset;
            }
            else
            {
                State = GroundAnchorState;
                AttachedNpcIndex = -1f;
                groundAnchorPoint = center;
            }

            Projectile.netUpdate = true;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 18, 1.35f, 5.4f, 1f, 1.55f);
            SpawnBombardImpactFX(center, MathHelper.Clamp(explosionSize / 180f, 1.35f, 2.8f));
            SpawnBombardAuraFX(center, 1.35f);
            Main.player[Projectile.owner].SetScreenshake(MathHelper.Clamp(explosionSize / 24f, 8f, 22f));
            SoundEngine.PlaySound(BlossomFluxSounds.RightBombardImpact1, center);
            SoundEngine.PlaySound(BlossomFluxSounds.RightBombardImpact2, center);
        }

        private void AccelerateThroughBombardTarget()
        {
            if (passedBombardTarget || Projectile.velocity.Y <= 0f || targetPoint == Vector2.Zero)
                return;

            Vector2 toTarget = targetPoint - Projectile.Center;
            float distance = toTarget.Length();
            if (distance <= 0.001f || Vector2.Dot(Projectile.velocity, toTarget) < 0f)
            {
                passedBombardTarget = true;
                Projectile.netUpdate = true;
                return;
            }

            float speed = Math.Max(Projectile.velocity.Length(), 32f);
            Vector2 desiredVelocity = toTarget / distance * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.22f);

            if (distance < Math.Max(42f, speed * 1.15f))
            {
                Projectile.velocity = desiredVelocity;
                passedBombardTarget = true;
                Projectile.netUpdate = true;
            }
        }

        private void UpdateBombardAnchor(Vector2 bombardCenter)
        {
            rainCounter++;

            if (rainCounter % 5 == 0)
                SpawnArrowRain(bombardCenter);

            if (rainCounter % 20 == 0)
                SoundEngine.PlaySound(BlossomFluxSounds.RightBombardSkyRain, bombardCenter);

            if (rainCounter % 12 == 0)
                SpawnBombardAuraFX(bombardCenter, 0.78f);

            EmitBombardAnchorFX(bombardCenter);
        }

        private void EmitBombardFlightFX()
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Player owner = Main.player[Projectile.owner];
            float ownerDistance = owner.active ? Vector2.Distance(owner.Center, Projectile.Center) : 0f;

            int smokeCount = Main.rand.Next(2, 4);
            float spiralPhase = FlightTimer * 0.22f + Projectile.identity * 0.37f;
            for (int i = 0; i < smokeCount; i++)
            {
                float fan = Main.rand.NextFloat(-0.75f, 0.75f);
                float spiral = (float)Math.Sin(spiralPhase + i * 1.7f) * Main.rand.NextFloat(0.4f, 1.25f);
                Vector2 smokeVelocity =
                    -direction.RotatedBy(fan) * Main.rand.NextFloat(0.55f, 2.1f) +
                    normal * spiral +
                    Main.rand.NextVector2Circular(0.75f, 0.75f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(2f, 18f) + normal * Main.rand.NextFloat(-12f, 12f),
                    smokeVelocity,
                    Color.Lerp(Color.Black, mainColor, Main.rand.NextFloat(0.06f, 0.18f)),
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.18f, 0.34f),
                    Main.rand.NextFloat(0.45f, 0.62f),
                    Main.rand.NextFloat(-0.16f, 0.16f),
                    true));
            }

            if ((int)FlightTimer % 2 == 0 && ownerDistance < 1400f)
            {
                GlowSparkParticle spark = new(
                    Projectile.Center + Projectile.velocity * Main.rand.NextFloat(-2f, -1f),
                    -Projectile.velocity * 0.3f,
                    false, 5, 0.06f,
                    Color.Lerp(HighlightColor, mainColor, 0.35f) * 0.68f,
                    new Vector2(1f, 0.3f), true, false, 1.5f);
                GeneralParticleHandler.SpawnParticle(spark);

                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + normal * Main.rand.NextFloat(-10f, 10f),
                    -Projectile.velocity.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.14f, 0.28f),
                    false, Main.rand.Next(9, 14), Main.rand.NextFloat(0.22f, 0.44f),
                    Main.rand.NextBool() ? HighlightColor : Color.Lerp(Color.Black, HighlightColor, 0.45f)));
            }
            else
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Torch,
                    (Projectile.velocity * -4f).RotatedByRandom(0.2f) * Main.rand.NextFloat(0.2f, 1f),
                    0,
                    Main.rand.NextBool(3) ? Color.Goldenrod : Color.Lerp(mainColor, HighlightColor, 0.45f),
                    Main.rand.NextFloat(0.4f, 0.65f));
                dust.noGravity = true;
            }

            if ((int)FlightTimer % 6 == 0)
            {
                DirectionalPulseRing pulse = new(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 8f,
                    Projectile.velocity * 0.05f,
                    HighlightColor * 0.46f,
                    new Vector2(0.86f, 2.3f),
                    Projectile.velocity.ToRotation(),
                    0.18f, 0.038f, 10);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            // 新增：六臂火星环 + SparkParticle 拖火
            EmitBombardSpiralFX(direction, normal, mainColor);
        }

        // 六臂 GlowOrb 旋转火星环 + SparkParticle 落火拖尾
        private void EmitBombardSpiralFX(Vector2 direction, Vector2 normal, Color mainColor)
        {
            float time = Main.GlobalTimeWrappedHourly * 5.4f + Projectile.identity * 0.38f;

            for (int i = 0; i < 6; i++)
            {
                float orbAngle = time + i * MathHelper.TwoPi / 6f;
                float radius = 13f + 4f * (float)Math.Sin(time * 1.8f + i * 0.72f);
                Vector2 orbOff = orbAngle.ToRotationVector2() * radius;
                Vector2 orbVel = -direction * Main.rand.NextFloat(0.3f, 0.8f) + orbOff.SafeNormalize(normal) * Main.rand.NextFloat(0.3f, 0.7f);

                if (Main.rand.NextBool(3))
                {
                    GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                        Projectile.Center + orbOff - direction * Main.rand.NextFloat(1f, 6f),
                        orbVel, false, 6,
                        Main.rand.NextFloat(0.22f, 0.38f),
                        Color.Lerp(HighlightColor, mainColor, Main.rand.NextFloat(0.2f, 0.65f)),
                        true, false, true));
                }
            }

            if ((int)FlightTimer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + normal * Main.rand.NextFloat(-8f, 8f),
                    -Projectile.velocity * Main.rand.NextFloat(0.12f, 0.24f) + normal * Main.rand.NextFloat(-0.4f, 0.4f),
                    false,
                    Main.rand.Next(16, 26),
                    Main.rand.NextFloat(0.40f, 0.64f),
                    Color.Lerp(mainColor, HighlightColor, Main.rand.NextFloat(0.25f, 0.65f))));
            }
        }

        private void EmitBombardAnchorFX(Vector2 center)
        {
            if (Main.dedServ || rainCounter % 4 != 0)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            HeavySmokeParticle smoke = new(
                center + Main.rand.NextVector2Circular(10f, 10f),
                Main.rand.NextVector2Circular(0.35f, 0.35f) + new Vector2(0f, -0.12f),
                Color.Lerp(mainColor, Color.Black, 0.18f),
                18,
                Main.rand.NextFloat(0.42f, 0.62f),
                0.58f,
                Main.rand.NextFloat(-0.04f, 0.04f),
                true);
            GeneralParticleHandler.SpawnParticle(smoke);

            GlowOrbParticle ember = new(
                center + Main.rand.NextVector2Circular(16f, 16f),
                Main.rand.NextVector2Circular(0.45f, 0.45f),
                false, 12,
                Main.rand.NextFloat(0.18f, 0.28f),
                Color.Lerp(HighlightColor, mainColor, Main.rand.NextFloat(0.2f, 0.65f)),
                true, false, true);
            GeneralParticleHandler.SpawnParticle(ember);
        }

        private void SpawnArrowRain(Vector2 center)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int rainCount = Math.Max(4, (int)Math.Round(5f * skyRainMultiplier));
            for (int i = 0; i < rainCount; i++)
            {
                Vector2 spawnPosition = center + new Vector2(Main.rand.NextFloat(-260f, 260f), -940f - Main.rand.NextFloat(0f, 260f));
                Vector2 targetPosition = center + Main.rand.NextVector2Circular(82f, 42f);
                Vector2 velocity = (targetPosition - spawnPosition).SafeNormalize(Vector2.UnitY) * (storedAmmoSpeed * Main.rand.NextFloat(1.25f, 1.62f));

                int projectileIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition, velocity,
                    ModContent.ProjectileType<BFLeafProj>(),
                    Math.Max(1, (int)(storedRainDamage * 0.8f)),
                    storedAmmoKnockback,
                    Projectile.owner,
                    (float)BlossomFluxChloroplastPresetType.Chlo_DBomb,
                    1f);

                if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                    continue;

                Projectile rainArrow = Main.projectile[projectileIndex];
                rainArrow.friendly = true;
                rainArrow.hostile = false;
                rainArrow.tileCollide = false;
                rainArrow.noDropItem = true;
            }

            SpawnBombardAuraFX(center, 0.92f);
        }

        private void SpawnSwordsplosionRainSword(Vector2 spawnPosition, Vector2 velocity)
        {
            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition, velocity,
                GetRandomSwordsplosionProjectileType(),
                storedRainDamage,
                storedAmmoKnockback,
                Projectile.owner,
                2f);

            if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                return;

            Projectile sword = Main.projectile[projectileIndex];
            sword.DamageType = DamageClass.Ranged;
            sword.penetrate = 1;
            sword.usesLocalNPCImmunity = true;
            sword.localNPCHitCooldown = -1;
            sword.noDropItem = true;

            BFAccessoryGlobalProjectile accessoryEffect = sword.GetGlobalProjectile<BFAccessoryGlobalProjectile>();
            accessoryEffect.BlossomFluxArrow = true;
            accessoryEffect.Preset = BlossomFluxChloroplastPresetType.Chlo_DBomb;
        }

        private static int GetRandomSwordsplosionProjectileType()
        {
            return Main.rand.Next(4) switch
            {
                0 => ModContent.ProjectileType<SwordsplosionBlue>(),
                1 => ModContent.ProjectileType<SwordsplosionGreen>(),
                2 => ModContent.ProjectileType<SwordsplosionPurple>(),
                _ => ModContent.ProjectileType<SwordsplosionRed>()
            };
        }

        private void SpawnBombardImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Color flashColor = Color.Lerp(mainColor, HighlightColor, 0.45f);

            CustomPulse outerBlast = new(
                center, Vector2.Zero, Color.Orange,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One, Main.rand.NextFloat(-0.2f, 0.2f), 0f, 0.34f * intensity, 16);
            GeneralParticleHandler.SpawnParticle(outerBlast);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, flashColor, 1.18f * intensity, 20));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero,
                Color.Lerp(HighlightColor, Color.White, 0.18f),
                new Vector2(1.55f, 2.3f), Main.rand.NextFloat(-0.3f, 0.3f),
                0.24f * intensity, 0.045f, 15));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero,
                Color.Lerp(flashColor, Color.White, 0.12f),
                new Vector2(1.15f, 3.4f), Main.rand.NextFloat(-0.15f, 0.15f),
                0.2f * intensity, 0.036f, 18));

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.FireworksRGB : DustID.Torch,
                    Main.rand.NextVector2CircularEdge(3.5f, 3.5f) * Main.rand.NextFloat(2.4f, 5.1f),
                    0,
                    Main.rand.NextBool(3) ? HighlightColor : Color.Goldenrod,
                    Main.rand.NextFloat(1.05f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void SpawnBombardAuraFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center, Vector2.Zero,
                Color.Lerp(HighlightColor, accentColor, 0.35f),
                new Vector2(1.75f, 1.75f), 0f,
                0.17f * intensity, 0.032f, 13));

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextVector2Circular(0.65f, 0.65f),
                    false, 12,
                    Main.rand.NextFloat(0.26f, 0.42f) * intensity,
                    Color.Lerp(mainColor, HighlightColor, Main.rand.NextFloat(0.25f, 0.65f)),
                    true, false, true));
            }
        }

        private void DrawBombardHighlightOverlay()
        {
            Texture2D texture = TextureAssets.Projectile[Projectile.type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            float pulse = 0.9f + 0.1f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f + Projectile.identity * 0.33f);
            float impactFlash = GetImpactFlashStrength();
            float highlightOpacity = InFlight ? 0.16f : 0.28f + impactFlash * 0.26f;
            float outlineDistance = 1.45f + 0.9f * pulse + impactFlash * 0.9f;
            Color outlineColor = HighlightColor * highlightOpacity;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.Additive,
                SamplerState.PointClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f;
                Vector2 offset = angle.ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, outlineColor,
                    Projectile.rotation, origin, Projectile.scale * (1.02f + 0.05f * pulse), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null,
                Color.Lerp(Color.Goldenrod, Color.White, 0.28f) * (0.16f + impactFlash * 0.2f),
                Projectile.rotation, origin, Projectile.scale * (1.06f + 0.04f * pulse), SpriteEffects.None, 0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred, BlendState.AlphaBlend,
                SamplerState.PointClamp, DepthStencilState.None,
                Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
        }

        private float GetRumbleStrength()
        {
            if (InFlight)
                return 0f;

            return 0.75f + GetImpactFlashStrength() * 2.2f;
        }

        private float GetImpactFlashStrength() =>
            InFlight ? 0f : Utils.GetLerpValue(BombardDuration - 18f, BombardDuration, Projectile.timeLeft, true);

        private static Vector2 RotateTowards(Vector2 currentDirection, Vector2 desiredDirection, float maxTurnRadians)
        {
            float currentAngle = currentDirection.ToRotation();
            float desiredAngle = desiredDirection.ToRotation();
            float delta = MathHelper.WrapAngle(desiredAngle - currentAngle);
            delta = MathHelper.Clamp(delta, -maxTurnRadians, maxTurnRadians);
            return (currentAngle + delta).ToRotationVector2();
        }
    }
}
