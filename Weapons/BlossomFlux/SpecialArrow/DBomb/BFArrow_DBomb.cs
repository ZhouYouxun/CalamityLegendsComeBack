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
    // D 战术右键箭：改成迫击炮式上抛轰炸，命中后持续向目标区域呼叫箭雨。
    internal class BFArrow_DBomb : ModProjectile
    {
        private const float FlightState = 0f;
        private const float AttachedNpcState = 1f;
        private const float GroundAnchorState = 2f;
        private const int MortarRiseFrames = 18;
        private const int BombardDuration = 96;
        private const float MinMortarSpeed = 15f;
        private const float MaxMortarSpeed = 25.5f;
        private const float MinDiveTurnRate = 0.16f;
        private const float MaxDiveTurnRate = 0.62f;

        private int rainCounter;
        private int storedRainDamage = 1;
        private int storedAmmoType = ProjectileID.WoodenArrowFriendly;
        private float storedAmmoSpeed = 14f;
        private float storedAmmoKnockback = 2f;
        private Vector2 stickOffset;
        private Vector2 targetPoint;
        private Vector2 groundAnchorPoint;
        private bool diveTargetLocked;

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

        public void ConfigureBombardTarget(Vector2 bombardTarget)
        {
            targetPoint = bombardTarget;
            diveTargetLocked = true;
            Projectile.netUpdate = true;
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.WriteVector2(targetPoint);
            writer.WriteVector2(groundAnchorPoint);
            writer.WriteVector2(stickOffset);
            writer.Write(diveTargetLocked);
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
            diveTargetLocked = reader.ReadBoolean();
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
            SpawnBombardImpactFX(target.Center, 1.2f);
            SpawnBombardAuraFX(target.Center, 0.95f);
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
            SpawnBombardImpactFX(groundAnchorPoint, 1.05f);
            SpawnBombardAuraFX(groundAnchorPoint, 0.8f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.3f, Pitch = -0.2f }, Projectile.Center);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_DBomb, 14, 1.5f, 5.5f, 0.95f, 1.35f);
            if (!Main.dedServ)
                SpawnBombardImpactFX(Projectile.Center, 0.8f);
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

            if (FlightTimer > MortarRiseFrames)
            {
                Vector2 targetOffset = targetPoint - Projectile.Center;
                float targetDistance = targetOffset.Length();
                Vector2 desiredDirection = targetOffset.SafeNormalize(Vector2.UnitY);
                Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitY);
                float turnProgress = Utils.GetLerpValue(MortarRiseFrames, MortarRiseFrames + 18f, FlightTimer, true);
                float alignment = Vector2.Dot(currentDirection, desiredDirection);
                float turnRate = MathHelper.Lerp(MinDiveTurnRate, MaxDiveTurnRate, turnProgress);
                turnRate = MathHelper.Lerp(turnRate, MaxDiveTurnRate + 0.2f, Utils.GetLerpValue(-0.2f, -1f, alignment, true));
                turnRate = MathHelper.Lerp(turnRate, MaxDiveTurnRate + 0.28f, Utils.GetLerpValue(160f, 30f, targetDistance, true));
                Vector2 rotatedDirection = RotateTowards(currentDirection, desiredDirection, turnRate);
                float desiredSpeed = MathHelper.Lerp(MaxMortarSpeed, MinMortarSpeed * 0.72f, Utils.GetLerpValue(280f, 24f, targetDistance, true));
                float speed = MathHelper.Lerp(Projectile.velocity.Length(), desiredSpeed, 0.16f + 0.18f * turnProgress);
                speed = MathHelper.Clamp(speed, MinMortarSpeed * 0.62f, MaxMortarSpeed);
                
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, rotatedDirection * speed, 0.08f);
                // 加重力
                Projectile.velocity.Y += 0.3f;


                Projectile.tileCollide = FlightTimer >= MortarRiseFrames + 6f;
            }
            else
            {
                Vector2 riseDirection = Projectile.velocity.SafeNormalize(-Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, riseDirection * MaxMortarSpeed, 0.04f);
                Projectile.tileCollide = false;
            }

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
                new Vector2(0.72f, 1.85f),
                Projectile.velocity.ToRotation(),
                0.15f,
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
                Vector2 spawnPosition = center + new Vector2(Main.rand.NextFloat(-140f, 140f), -620f - Main.rand.NextFloat(0f, 140f));
                Vector2 targetPosition = center + Main.rand.NextVector2Circular(30f, 20f);
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
                0.24f * intensity,
                12);
            GeneralParticleHandler.SpawnParticle(outerBlast);

            StrongBloom bloom = new(center, Vector2.Zero, flashColor, 0.9f * intensity, 16);
            GeneralParticleHandler.SpawnParticle(bloom);

            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(HighlightColor, Color.White, 0.18f),
                new Vector2(1.2f, 1.8f),
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.18f * intensity,
                0.045f,
                13);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.FireworksRGB : DustID.Torch,
                    Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(1.8f, 3.8f),
                    0,
                    Main.rand.NextBool(3) ? HighlightColor : Color.Goldenrod,
                    Main.rand.NextFloat(0.95f, 1.3f));
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
                new Vector2(1.3f, 1.3f),
                0f,
                0.13f * intensity,
                0.032f,
                11);
            GeneralParticleHandler.SpawnParticle(pulse);

            for (int i = 0; i < 2; i++)
            {
                GlowOrbParticle ember = new(
                    center + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextVector2Circular(0.65f, 0.65f),
                    false,
                    12,
                    Main.rand.NextFloat(0.2f, 0.34f) * intensity,
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
