using System;
using System.IO;
using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // D tactical right-click arrow: mortar trajectory with a bombard anchor on impact.
    internal class BFArrow_DBomb : ModProjectile
    {
        private const float FlightState = 0f;
        private const float AttachedNpcState = 1f;
        private const float GroundAnchorState = 2f;
        private const int BombardDuration = 96;
        private const float MortarGravity = 0.28f;
        private const float MinMortarApexHeight = 440f;
        private const float MaxMortarApexHeight = 840f;
        private const int MortarCollisionDelay = 12;

        private int rainCounter;
        private int storedRainDamage = 1;
        private int storedAmmoType = ProjectileID.WoodenArrowFriendly;
        private float storedAmmoSpeed = 14f;
        private float storedAmmoKnockback = 2f;
        private Vector2 stickOffset;
        private Vector2 targetPoint;
        private Vector2 groundAnchorPoint;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/DBomb/BFArrow_DBomb";

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];

        private bool InFlight => State == FlightState;
        private bool AttachedToNpc => State == AttachedNpcState;
        private bool AnchoredToGround => State == GroundAnchorState;
        private static Color HighlightColor => Color.Lerp(Color.Goldenrod, Color.Khaki, 0.5f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 240, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => InFlight ? null : false;

        public override bool? CanHitNPC(NPC target) => InFlight ? null : false;

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
            float travelTime = verticalSpeed / MortarGravity + (float)Math.Sqrt(2f * fallDistance / MortarGravity);
            float horizontalSpeed = (target.X - start.X) / Math.Max(travelTime, 1f);

            return new Vector2(horizontalSpeed, -verticalSpeed);
        }

        public void ConfigureBombardTarget(Vector2 bombardTarget)
        {
            targetPoint = bombardTarget;
            float desiredSpeed = Projectile.velocity.Length();
            if (desiredSpeed <= 0.01f)
                desiredSpeed = 18f;

            Projectile.velocity = CalculateMortarLaunchVelocity(Projectile.Center, bombardTarget, desiredSpeed);
            Projectile.tileCollide = false;
            BFArrowCommon.FaceForward(Projectile);
            Projectile.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(targetPoint);
            writer.WriteVector2(groundAnchorPoint);
            writer.WriteVector2(stickOffset);
            writer.Write(rainCounter);
            writer.Write(storedRainDamage);
            writer.Write(storedAmmoType);
            writer.Write(storedAmmoSpeed);
            writer.Write(storedAmmoKnockback);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            targetPoint = reader.ReadVector2();
            groundAnchorPoint = reader.ReadVector2();
            stickOffset = reader.ReadVector2();
            rainCounter = reader.ReadInt32();
            storedRainDamage = reader.ReadInt32();
            storedAmmoType = reader.ReadInt32();
            storedAmmoSpeed = reader.ReadSingle();
            storedAmmoKnockback = reader.ReadSingle();
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

            stickOffset = Projectile.Center - target.Center;
            groundAnchorPoint = target.Center;
            storedRainDamage = Math.Max(Projectile.damage, storedRainDamage);
            State = AttachedNpcState;
            AttachedNpcIndex = target.whoAmI;
            Projectile.damage = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = BombardDuration;
            Projectile.netUpdate = true;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 12, 1f, 3.2f, 0.9f, 1.2f);
            SpawnBombardImpactFX(target.Center, 1.55f);
            SpawnBombardAuraFX(target.Center, 1.25f);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.5f, Pitch = -0.08f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (!InFlight)
                return false;

            State = GroundAnchorState;
            groundAnchorPoint = Projectile.Center;
            Projectile.damage = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.rotation = oldVelocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;
            Projectile.tileCollide = false;
            Projectile.timeLeft = BombardDuration;
            Projectile.netUpdate = true;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 10, 1f, 2.8f, 0.9f, 1.15f);
            SpawnBombardImpactFX(groundAnchorPoint, 1.4f);
            SpawnBombardAuraFX(groundAnchorPoint, 1.15f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f, Pitch = -0.2f }, Projectile.Center);
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

        private void UpdateMortarFlight()
        {
            FlightTimer++;
            Projectile.velocity.Y += MortarGravity;
            Projectile.tileCollide = FlightTimer >= MortarCollisionDelay && Projectile.velocity.Y >= 0f;

            BFArrowCommon.FaceForward(Projectile);
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 1.08f);
            EmitBombardFlightFX();
        }

        private void UpdateBombardAnchor(Vector2 bombardCenter)
        {
            rainCounter++;

            if (rainCounter % 8 == 0)
                SpawnArrowRain(bombardCenter);

            if (rainCounter % 20 == 0)
                SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.2f, Pitch = 0.42f }, bombardCenter);

            if (rainCounter % 12 == 0)
                SpawnBombardAuraFX(bombardCenter, 0.78f);

            EmitBombardAnchorFX(bombardCenter);
        }

        private void EmitBombardFlightFX()
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Player owner = Main.player[Projectile.owner];
            float ownerDistance = owner.active ? Vector2.Distance(owner.Center, Projectile.Center) : 0f;

            if ((int)FlightTimer % 2 == 0 && ownerDistance < 1400f)
            {
                GlowSparkParticle spark = new(
                    Projectile.Center + Projectile.velocity * Main.rand.NextFloat(-2f, -1f),
                    -Projectile.velocity * 0.3f,
                    false,
                    5,
                    0.06f,
                    Color.Lerp(HighlightColor, mainColor, 0.35f) * 0.68f,
                    new Vector2(1f, 0.3f),
                    true,
                    false,
                    1.5f);
                GeneralParticleHandler.SpawnParticle(spark);
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

            if ((int)FlightTimer % 6 != 0)
                return;

            DirectionalPulseRing pulse = new(
                Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 8f,
                Projectile.velocity * 0.05f,
                HighlightColor * 0.46f,
                new Vector2(0.86f, 2.3f),
                Projectile.velocity.ToRotation(),
                0.18f,
                0.038f,
                10);
            GeneralParticleHandler.SpawnParticle(pulse);
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
                false,
                12,
                Main.rand.NextFloat(0.18f, 0.28f),
                Color.Lerp(HighlightColor, mainColor, Main.rand.NextFloat(0.2f, 0.65f)),
                true,
                false,
                true);
            GeneralParticleHandler.SpawnParticle(ember);
        }

        private void SpawnArrowRain(Vector2 center)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < 2; i++)
            {
                Vector2 spawnPosition = center + new Vector2(Main.rand.NextFloat(-180f, 180f), -920f - Main.rand.NextFloat(0f, 220f));
                Vector2 targetPosition = center + Main.rand.NextVector2Circular(46f, 28f);
                Vector2 velocity = (targetPosition - spawnPosition).SafeNormalize(Vector2.UnitY) * (storedAmmoSpeed * Main.rand.NextFloat(1.15f, 1.45f));

                int projectileIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    storedAmmoType,
                    Math.Max(1, (int)(storedRainDamage * 0.55f)),
                    storedAmmoKnockback,
                    Projectile.owner);

                if (!BFArrowCommon.InBounds(projectileIndex, Main.maxProjectiles))
                    continue;

                Projectile rainArrow = Main.projectile[projectileIndex];
                rainArrow.friendly = true;
                rainArrow.hostile = false;
                rainArrow.arrow = true;
                rainArrow.noDropItem = true;
                BFArrowCommon.ForceLocalNPCImmunity(rainArrow, 12);
                BFArrowCommon.TagBlossomFluxLeftArrow(rainArrow);
            }

            SpawnBombardAuraFX(center, 0.92f);
        }

        private void SpawnBombardImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Color flashColor = Color.Lerp(mainColor, HighlightColor, 0.45f);

            CustomPulse outerBlast = new(
                center,
                Vector2.Zero,
                Color.Orange,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(-0.2f, 0.2f),
                0f,
                0.34f * intensity,
                16);
            GeneralParticleHandler.SpawnParticle(outerBlast);

            StrongBloom bloom = new(center, Vector2.Zero, flashColor, 1.18f * intensity, 20);
            GeneralParticleHandler.SpawnParticle(bloom);

            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(HighlightColor, Color.White, 0.18f),
                new Vector2(1.55f, 2.3f),
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.24f * intensity,
                0.045f,
                15);
            GeneralParticleHandler.SpawnParticle(pulse);

            DirectionalPulseRing crossPulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(flashColor, Color.White, 0.12f),
                new Vector2(1.15f, 3.4f),
                Main.rand.NextFloat(-0.15f, 0.15f),
                0.2f * intensity,
                0.036f,
                18);
            GeneralParticleHandler.SpawnParticle(crossPulse);

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

            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(HighlightColor, accentColor, 0.35f),
                new Vector2(1.75f, 1.75f),
                0f,
                0.17f * intensity,
                0.032f,
                13);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 4; i++)
            {
                GlowOrbParticle ember = new(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextVector2Circular(0.65f, 0.65f),
                    false,
                    12,
                    Main.rand.NextFloat(0.26f, 0.42f) * intensity,
                    Color.Lerp(mainColor, HighlightColor, Main.rand.NextFloat(0.25f, 0.65f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(ember);
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
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            for (int i = 0; i < 10; i++)
            {
                float angle = MathHelper.TwoPi * i / 10f;
                Vector2 offset = angle.ToRotationVector2() * outlineDistance;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition + offset,
                    null,
                    outlineColor,
                    Projectile.rotation,
                    origin,
                    Projectile.scale * (1.02f + 0.05f * pulse),
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                Color.Lerp(Color.Goldenrod, Color.White, 0.28f) * (0.16f + impactFlash * 0.2f),
                Projectile.rotation,
                origin,
                Projectile.scale * (1.06f + 0.04f * pulse),
                SpriteEffects.None,
                0);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);
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

