using System;
using System.Linq;
using CalamityLegendsComeBack.Weapons.BlossomFlux;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightClick;
using CalamityMod;
using CalamityMod.Enums;
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

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.LeftClick
{
    internal sealed class BFLeafProj : ModProjectile, ILocalizedModType, IPixelatedPrimitiveRenderer
    {
        private const int ReconPostHitStraightFrames = 10;
        private const int ReconHomingDelayFrames = 1;

        public GeneralDrawLayer LayerToRenderTo => GeneralDrawLayer.BeforeProjectiles;
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/LeftClick/BFLeafProj";

        private BlossomFluxChloroplastPresetType Preset => Projectile.ai[0] switch
        {
            1f => BlossomFluxChloroplastPresetType.Chlo_BRecov,
            2f => BlossomFluxChloroplastPresetType.Chlo_CDetec,
            3f => BlossomFluxChloroplastPresetType.Chlo_DBomb,
            _ => BlossomFluxChloroplastPresetType.Chlo_ABreak
        };

        private ref float StoredSpeed => ref Projectile.localAI[0];
        private ref float FlightTimer => ref Projectile.localAI[1];
        private ref float ReconWanderTimer => ref Projectile.ai[1];
        private ref float BombardExplosionsLeft => ref Projectile.ai[1];
        private ref float LastReconHitNpcIndex => ref Projectile.ai[2];
        private readonly bool[] ignoredReconTargets = new bool[Main.maxNPCs];
        private int bombardExplosionCooldown;
        private int bombardMarkedExplosionCooldown;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.arrow = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            Projectile.noDropItem = true;
            BFArrowCommon.TagBlossomFluxLeftArrow(Projectile);

            StoredSpeed = MathHelper.Clamp(Projectile.velocity.Length(), 10f, 24f);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;

            switch (Preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    Projectile.penetrate = 3;
                    Projectile.localNPCHitCooldown = -1;
                    Projectile.timeLeft = 150;
                    StoredSpeed = MathHelper.Clamp(StoredSpeed + 2.2f, 15f, 27f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    Projectile.penetrate = 1;
                    Projectile.timeLeft = 135;
                    StoredSpeed = MathHelper.Clamp(StoredSpeed, 12f, 22f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    Projectile.penetrate = 3;
                    Projectile.timeLeft = 170;
                    StoredSpeed = MathHelper.Clamp(StoredSpeed, 12f, 20f);
                    Projectile.localNPCHitCooldown = 18;
                    LastReconHitNpcIndex = -1f;
                    Array.Clear(ignoredReconTargets, 0, ignoredReconTargets.Length);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    Projectile.penetrate = -1;
                    Projectile.tileCollide = false;
                    Projectile.timeLeft = 140;
                    Projectile.extraUpdates = 1;
                    if (BombardExplosionsLeft <= 0f)
                        BombardExplosionsLeft = 1f;
                    StoredSpeed = MathHelper.Clamp(StoredSpeed * 0.84f, 8.5f, 17f);
                    break;
            }
        }

        public override Color? GetAlpha(Color lightColor) => Color.White * Projectile.Opacity;

        public override void AI()
        {
            FlightTimer++;
            if (bombardExplosionCooldown > 0)
                bombardExplosionCooldown--;

            if (bombardMarkedExplosionCooldown > 0)
                bombardMarkedExplosionCooldown--;

            BlossomFluxChloroplastPresetType preset = Preset;
            Color mainColor = BFArrowCommon.GetPresetColor(preset);

            Lighting.AddLight(Projectile.Center, mainColor.ToVector3() * (0.28f + Projectile.Opacity * 0.28f));

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_ABreak:
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * StoredSpeed;
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * StoredSpeed;
                    Projectile.velocity = Projectile.velocity.RotatedBy(Math.Sin(FlightTimer * 0.05f + Projectile.identity) * 0.0025f);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    UpdateReconHoming();
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    Projectile.velocity.Y += 0.2f;
                    Projectile.velocity.X *= 0.998f;
                    break;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.Pi;
            EmitLeafTrail(preset);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Preset == BlossomFluxChloroplastPresetType.Chlo_ABreak && IsReconPriorityTarget(target))
            {
                modifiers.FinalDamage *= 1.4f;
                return;
            }

            if (Preset == BlossomFluxChloroplastPresetType.Chlo_CDetec && IsReconPriorityTarget(target))
                modifiers.FinalDamage *= 1.25f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BlossomFluxChloroplastPresetType preset = Preset;
            Vector2 impactPosition = GetImpactPosition(target);
            SpawnLeafImpactFX(impactPosition, preset, 1.05f);

            switch (preset)
            {
                case BlossomFluxChloroplastPresetType.Chlo_BRecov:
                    if (BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                        Main.player[Projectile.owner].GetModPlayer<BFRecoveryEcologyPlayer>().TrySpawnRecoveryTransfer(impactPosition, IsReconPriorityTarget(target));
                    SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.42f, Pitch = 0.12f }, impactPosition);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_CDetec:
                    if (BFArrowCommon.InBounds(target.whoAmI, ignoredReconTargets.Length))
                        ignoredReconTargets[target.whoAmI] = true;

                    Projectile.localNPCImmunity[target.whoAmI] = Projectile.timeLeft + 30;
                    target.GetGlobalNPC<BFArrow_CDetecNPC>().ApplyPriorityMark(Projectile.owner, BFReconLeftBalance.MarkDuration);
                    if (BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                        Main.player[Projectile.owner].GetModPlayer<BFRightUIPlayer>().SetReconPriorityTarget(target.whoAmI, BFReconLeftBalance.MarkDuration);

                    LastReconHitNpcIndex = target.whoAmI;
                    Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                    ReconWanderTimer = ReconPostHitStraightFrames * Projectile.MaxUpdates;
                    Projectile.velocity = currentDirection * StoredSpeed;

                    Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.78f));

                    Projectile.netUpdate = true;
                    SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.28f, Pitch = 0.36f }, impactPosition);
                    SoundEngine.PlaySound(SoundID.Item114 with { Volume = 0.45f, Pitch = 0.22f }, impactPosition);
                    break;

                case BlossomFluxChloroplastPresetType.Chlo_DBomb:
                    SpawnBombardExplosion(impactPosition, target);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/PlantyMushMine") { Volume = 0.45f, PitchVariance = 0.18f }, impactPosition);
                    break;
            }
        }

        private Vector2 GetImpactPosition(NPC target)
        {
            Rectangle hitbox = target.Hitbox;
            return new Vector2(
                MathHelper.Clamp(Projectile.Center.X, hitbox.Left, hitbox.Right),
                MathHelper.Clamp(Projectile.Center.Y, hitbox.Top, hitbox.Bottom));
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Preset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
                SpawnLeafImpactFX(Projectile.Center, Preset, 0.72f);
            else
                SpawnLeafVanishFX(Projectile.Center, Preset, 0.72f);

            return true;
        }

        public override void OnKill(int timeLeft)
        {
            SpawnLeafVanishFX(Projectile.Center, Preset, Preset == BlossomFluxChloroplastPresetType.Chlo_BRecov ? 1.28f : 0.9f);
            if (Preset == BlossomFluxChloroplastPresetType.Chlo_ABreak)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SylvestaffProjectileBounce") { Volume = 0.32f, PitchVariance = 0.12f }, Projectile.Center);
            }
            else if (Preset == BlossomFluxChloroplastPresetType.Chlo_BRecov)
            {
                SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.28f, Pitch = 0.24f }, Projectile.Center);
            }
            else if (Preset == BlossomFluxChloroplastPresetType.Chlo_CDetec)
            {
                SoundEngine.PlaySound(SoundID.Item115 with { Volume = 0.32f, Pitch = 0.12f }, Projectile.Center);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D projectileTexture = TextureAssets.Projectile[Type].Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D sparkTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(Preset) * Projectile.Opacity;
            Color accentColor = BFArrowCommon.GetPresetAccentColor(Preset) * Projectile.Opacity;
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7.2f + Projectile.identity * 0.23f);

            DrawHelix(drawPosition, forward);

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < 7; i++)
            {
                float angle = MathHelper.TwoPi * i / 7f;
                float side = (float)Math.Sin(angle + Main.GlobalTimeWrappedHourly * 2.6f);
                float along = (float)Math.Cos(angle) * (2.6f + 1.2f * pulse);
                Vector2 offset = right * side * (3.2f + 1.4f * pulse) + forward * along;
                Main.EntitySpriteDraw(
                    sparkTexture,
                    drawPosition + offset,
                    null,
                    Color.Lerp(mainColor, accentColor, 0.35f) * 0.24f,
                    Projectile.rotation + MathHelper.PiOver2,
                    sparkTexture.Size() * 0.5f,
                    new Vector2(0.02f, 0.095f + 0.035f * pulse) * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                mainColor * 0.5f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.26f * pulse,
                SpriteEffects.None,
                0);

            BFArrowCommon.DrawCentredRotatingStar(Projectile, Preset, isLeftClick: true, manageBlendState: false);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            Main.EntitySpriteDraw(
                projectileTexture,
                drawPosition,
                null,
                Projectile.GetAlpha(lightColor),
                Projectile.rotation,
                projectileTexture.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }

        private void SpawnBombardExplosion(Vector2 center, NPC target)
        {
            bool markedTarget = IsReconPriorityTarget(target);
            bool enhanced = markedTarget && Main.rand.NextBool(3);

            if (markedTarget)
            {
                if (bombardMarkedExplosionCooldown > 0)
                    return;

                bombardMarkedExplosionCooldown = 8;
            }
            else
            {
                if (bombardExplosionCooldown > 0 || BombardExplosionsLeft <= 0f)
                    return;

                BombardExplosionsLeft--;
                bombardExplosionCooldown = 14;
            }

            if (Projectile.owner == Main.myPlayer)
            {
                int explosionDamage = enhanced
                    ? Math.Max(1, (int)(Projectile.damage * 1.18f))
                    : Math.Max(1, (int)(Projectile.damage * 0.65f));
                float explosionSize = (enhanced ? 158f : 75f) * Projectile.scale;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BFLeafBombExplosion>(),
                    explosionDamage,
                    Projectile.knockBack * (enhanced ? 0.95f : 0.65f),
                    Projectile.owner,
                    explosionSize,
                    enhanced ? 1f : 0f);
            }

            SpawnLeafImpactFX(center, BlossomFluxChloroplastPresetType.Chlo_DBomb, markedTarget ? 1.8f : 1.35f);
        }

        private void UpdateReconHoming()
        {
            if (FlightTimer < ReconHomingDelayFrames * Projectile.MaxUpdates)
            {
                BFArrowCommon.MaintainSpeed(Projectile, StoredSpeed, 0.35f);
                return;
            }

            if (ReconWanderTimer > 0f)
            {
                ReconWanderTimer--;
                ApplyReconMagicMissileSteering(null, 0.1f);
                return;
            }

            NPC target = FindReconTarget();
            if (target == null)
            {
                ApplyReconMagicMissileSteering(null, 0.08f);
                return;
            }

            ApplyReconMagicMissileSteering(
                target,
                IsReconPriorityTarget(target) ? BFReconLeftBalance.PriorityHomingTurnResponsiveness : BFReconLeftBalance.HomingTurnResponsiveness);
        }

        private void ApplyReconMagicMissileSteering(NPC target, float responsiveness)
        {
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float currentSpeed = Math.Max(Projectile.velocity.Length(), StoredSpeed);

            if (target == null)
            {
                float idleCurve = (float)Math.Sin(FlightTimer * 0.045f + Projectile.identity * 0.37f) * 0.018f;
                Projectile.velocity = currentDirection.RotatedBy(idleCurve) * MathHelper.Lerp(currentSpeed, StoredSpeed, 0.16f);
                return;
            }

            bool priority = IsReconPriorityTarget(target);
            Vector2 aimPoint = target.Center + target.velocity * (priority ? 14f : 9f);
            Vector2 toTarget = aimPoint - Projectile.Center;
            float distance = toTarget.Length();
            Vector2 desiredDirection = toTarget.SafeNormalize(currentDirection);
            float closeTargetBoost = Utils.GetLerpValue(360f, 96f, distance, true) * 0.2f;
            float steering = MathHelper.Clamp(responsiveness + closeTargetBoost, 0.08f, 0.58f);
            float desiredSpeed = MathHelper.Clamp(StoredSpeed + (priority ? 3.4f : 1.4f) + Utils.GetLerpValue(640f, 180f, distance, true) * 1.8f, 12f, 25f);

            Vector2 steeredDirection = Vector2.Lerp(currentDirection, desiredDirection, steering).SafeNormalize(desiredDirection);
            Projectile.velocity = steeredDirection * MathHelper.Lerp(currentSpeed, desiredSpeed, 0.24f);
        }

        private NPC FindReconTarget(int excludedNpcIndex = -1)
        {
            if (!BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                return null;

            Player owner = Main.player[Projectile.owner];
            BFRightUIPlayer rightUiPlayer = owner.GetModPlayer<BFRightUIPlayer>();
            int priorityTargetIndex = rightUiPlayer.ReconPriorityTargetIndex;
            if (BFArrowCommon.InBounds(priorityTargetIndex, Main.maxNPCs))
            {
                NPC priorityTarget = Main.npc[priorityTargetIndex];
                if (priorityTarget.whoAmI != excludedNpcIndex &&
                    priorityTarget.whoAmI != (int)LastReconHitNpcIndex &&
                    !HasAlreadyHitReconTarget(priorityTarget.whoAmI) &&
                    Projectile.localNPCImmunity[priorityTarget.whoAmI] <= 0 &&
                    priorityTarget.active &&
                    priorityTarget.CanBeChasedBy(Projectile) &&
                    priorityTarget.GetGlobalNPC<BFArrow_CDetecNPC>().IsPriorityMarkedBy(Projectile.owner) &&
                    Vector2.DistanceSquared(Projectile.Center, priorityTarget.Center) <= 2000f * 2000f)
                {
                    return priorityTarget;
                }
            }

            NPC bestTarget = null;
            float bestDistance = 880f;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                if (npc.whoAmI == excludedNpcIndex || npc.whoAmI == (int)LastReconHitNpcIndex)
                    continue;

                if (HasAlreadyHitReconTarget(npc.whoAmI))
                    continue;

                if (Projectile.localNPCImmunity[npc.whoAmI] > 0)
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private bool HasAlreadyHitReconTarget(int npcIndex)
        {
            return BFArrowCommon.InBounds(npcIndex, ignoredReconTargets.Length) && ignoredReconTargets[npcIndex];
        }

        private bool IsReconPriorityTarget(NPC target)
        {
            if (target == null || !BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                return false;

            BFRightUIPlayer rightUiPlayer = Main.player[Projectile.owner].GetModPlayer<BFRightUIPlayer>();
            return target.whoAmI == rightUiPlayer.ReconPriorityTargetIndex &&
                target.GetGlobalNPC<BFArrow_CDetecNPC>().IsPriorityMarkedBy(Projectile.owner);
        }

        private void EmitLeafTrail(BlossomFluxChloroplastPresetType preset)
        {
            if (preset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
            {
                EmitBombardLeafTrail();
                return;
            }

            if (Main.rand.NextBool(2))
                BFArrowCommon.EmitPresetTrail(Projectile, preset, 0.95f);

            if (Main.dedServ || !Projectile.FinalExtraUpdate())
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            if ((int)FlightTimer % 3 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(2f, 8f) + side * Main.rand.NextFloat(-5f, 5f),
                    -Projectile.velocity * 0.025f + Main.rand.NextVector2Circular(0.25f, 0.25f),
                    Main.rand.NextFloat(0.12f, 0.2f),
                    Color.Lerp(mainColor, Color.White, 0.22f),
                    Main.rand.Next(8, 12)));
            }

            if (preset == BlossomFluxChloroplastPresetType.Chlo_CDetec && (int)FlightTimer % 6 == 0)
            {
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    Projectile.Center + side * Main.rand.NextFloat(-6f, 6f),
                    forward * Main.rand.NextFloat(1.2f, 2.4f),
                    Color.White,
                    accentColor,
                    0.48f,
                    10));
            }
        }

        private void EmitBombardLeafTrail()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);

            if (!Projectile.FinalExtraUpdate())
                return;

            if ((int)FlightTimer % 4 == 0)
            {
                GlowOrbParticle orb = new(
                    Projectile.Center - forward * Main.rand.NextFloat(2f, 8f) + side * Main.rand.NextFloat(-3f, 3f),
                    Main.rand.NextVector2Circular(0.12f, 0.12f),
                    false,
                    5,
                    Main.rand.NextFloat(0.46f, 0.62f),
                    Color.Lerp(mainColor, Color.White, 0.18f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }

            if ((int)FlightTimer % 6 != 0)
                return;

            Particle trail = new SparkParticle(
                Projectile.Center - forward * Main.rand.NextFloat(6f, 16f) + side * Main.rand.NextFloat(-4f, 4f),
                -Projectile.velocity * 0.16f + side * Main.rand.NextFloat(-0.24f, 0.24f),
                false,
                Main.rand.Next(24, 34),
                Main.rand.NextFloat(0.5f, 0.72f),
                Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.2f, 0.55f)));
            GeneralParticleHandler.SpawnParticle(trail);
        }

        private void SpawnLeafImpactFX(Vector2 center, BlossomFluxChloroplastPresetType preset, float intensity)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, preset, (int)(8 * intensity), 0.8f, 3.2f, 0.72f, 1.12f);

            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.18f) * 0.5f,
                0.42f * intensity,
                10));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Projectile.velocity.SafeNormalize(Vector2.UnitX) * 0.6f,
                Color.Lerp(mainColor, accentColor, 0.35f),
                new Vector2(0.72f, 1.55f),
                Projectile.velocity.ToRotation(),
                0.12f * intensity,
                0.032f,
                11));
        }

        private void SpawnLeafVanishFX(Vector2 center, BlossomFluxChloroplastPresetType preset, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(preset);
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitY);

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.22f),
                Vector2.One,
                Main.rand.NextFloat(-0.3f, 0.3f),
                0.1f * intensity,
                0.024f,
                10));

            int orbCount = preset == BlossomFluxChloroplastPresetType.Chlo_BRecov ? 8 : 4;
            for (int i = 0; i < orbCount; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    forward.RotatedByRandom(0.92f) * Main.rand.NextFloat(0.7f, 2.2f),
                    false,
                    Main.rand.Next(10, 15),
                    Main.rand.NextFloat(0.16f, 0.3f) * intensity,
                    Color.Lerp(mainColor, accentColor, Main.rand.NextFloat(0.15f, 0.55f)),
                    true,
                    false,
                    true));
            }

            if (preset != BlossomFluxChloroplastPresetType.Chlo_BRecov)
                return;

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.24f) * 0.5f,
                0.72f * intensity,
                14));

            for (int i = 0; i < 6; i++)
            {
                GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(1.3f, 1.3f),
                    Color.Lerp(mainColor, Color.White, 0.14f),
                    Color.Black,
                    Main.rand.NextFloat(0.45f, 0.8f) * intensity,
                    Main.rand.Next(130, 190)));
            }
        }

        private void DrawHelix(Vector2 drawPosition, Vector2 forward)
        {
            Texture2D helixTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/WaterFlavored").Value;
            Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
            Color mainColor = BFArrowCommon.GetPresetColor(Preset);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(Preset);
            float time = Main.GlobalTimeWrappedHourly * 5.8f + Projectile.identity * 0.31f;
            const int helixCount = 7;

            for (int i = 0; i < helixCount; i++)
            {
                float progress = i / (float)(helixCount - 1);
                float helixAngle = time - progress * 2.1f + MathHelper.TwoPi * i / helixCount;
                Vector2 spiralOffset = right.RotatedBy(helixAngle) * MathHelper.Lerp(9f, 4f, progress);
                Vector2 backwardOffset = -forward * MathHelper.Lerp(4f, 28f, progress);
                Color orbitColor = Color.Lerp(mainColor, accentColor, progress * 0.45f) * MathHelper.Lerp(0.58f, 0.1f, progress) * Projectile.Opacity;

                Main.EntitySpriteDraw(
                    helixTexture,
                    drawPosition + spiralOffset + backwardOffset,
                    null,
                    orbitColor,
                    forward.ToRotation() - MathHelper.PiOver2,
                    helixTexture.Size() * 0.5f,
                    new Vector2(0.2f, MathHelper.Lerp(0.78f, 0.34f, progress)) * Projectile.scale,
                    SpriteEffects.None,
                    0);
            }
        }

        private Vector2[] BuildTrailPoints()
        {
            Vector2[] trailPoints = Projectile.oldPos
                .Where(pos => pos != Vector2.Zero)
                .Select(pos => pos + Projectile.Size * 0.5f)
                .ToArray();

            if (trailPoints.Length == 0)
                return new[] { Projectile.Center - Projectile.velocity, Projectile.Center };

            if (trailPoints[0] != Projectile.Center)
                trailPoints = new[] { Projectile.Center }.Concat(trailPoints).ToArray();

            return trailPoints;
        }

        private float TrailWidthFunction(float completion, Vector2 _)
        {
            float maxBodyWidth = Projectile.scale * (Preset == BlossomFluxChloroplastPresetType.Chlo_DBomb ? 18f : 15f);
            float curveRatio = 0.16f;
            if (completion < curveRatio)
                return MathF.Sin(completion / curveRatio * MathHelper.PiOver2) * maxBodyWidth + curveRatio;

            return Utils.Remap(completion, curveRatio, 1f, maxBodyWidth, 0f);
        }

        private Color TrailColorFunction(float completion, Vector2 _)
        {
            Color bodyColor = Color.Lerp(BFArrowCommon.GetPresetColor(Preset), BFArrowCommon.GetPresetAccentColor(Preset), Utils.GetLerpValue(0f, 0.5f, completion, true));
            bodyColor = Color.Lerp(bodyColor, Color.White, Utils.GetLerpValue(0.72f, 0f, completion, true) * 0.12f);
            return Color.Lerp(bodyColor * Projectile.Opacity, Color.Transparent, Utils.GetLerpValue(0.78f, 1f, completion, true));
        }

        public void RenderPixelatedPrimitives(SpriteBatch spriteBatch, GeneralDrawLayer layer)
        {
            if (Preset == BlossomFluxChloroplastPresetType.Chlo_DBomb)
                return;

            Vector2[] trailPoints = BuildTrailPoints();
            if (trailPoints.Length < 2)
                return;

            GameShaders.Misc["CalamityMod:ImpFlameTrail"].SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SylvestaffStreak"));

            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    TrailWidthFunction,
                    TrailColorFunction,
                    (_, _) => Vector2.Zero,
                    true,
                    true,
                    GameShaders.Misc["CalamityMod:ImpFlameTrail"]),
                trailPoints.Length * 2);
        }
    }

    internal sealed class BFLeafBombExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool initialized;

        public override void SetDefaults()
        {
            Projectile.width = 75;
            Projectile.height = 75;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            if (initialized)
                return;

            initialized = true;
            Vector2 center = Projectile.Center;
            int blastSize = Projectile.ai[0] > 0f ? (int)Projectile.ai[0] : 75;
            Projectile.width = blastSize;
            Projectile.height = blastSize;
            Projectile.Center = center;

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SubsumingVortexExplosion") { Volume = 0.35f, Pitch = 0.15f }, center);
            SpawnExplosionFX(center);

            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center,
                    Vector2.Zero,
                    ModContent.ProjectileType<FuckYou>(),
                    0,
                    0f,
                    Projectile.owner,
                    Math.Max(Projectile.ai[0], 88f));
            }
        }

        private static void SpawnExplosionFX(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color blastColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_DBomb);
            Color highlight = Color.Lerp(Color.Goldenrod, BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_DBomb), 0.45f);

            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(
                center,
                Vector2.Zero,
                Color.Lerp(blastColor, highlight, 0.35f),
                Vector2.One,
                Main.rand.NextFloat(-0.35f, 0.35f),
                0f,
                0.18f,
                10));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Vector2.Zero,
                highlight,
                new Vector2(1.25f, 1.25f),
                0f,
                0.16f,
                0.032f,
                12));

            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center,
                    Main.rand.NextBool(3) ? DustID.FireworksRGB : DustID.Torch,
                    Main.rand.NextVector2CircularEdge(3.2f, 3.2f) * Main.rand.NextFloat(1.8f, 4.6f),
                    0,
                    Main.rand.NextBool(3) ? highlight : blastColor,
                    Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }
    }
}
