using CalamityLegendsComeBack.Weapons.Vesuvius.LeftClick.CStage2;
using CalamityLegendsComeBack.Weapons.Vesuvius.Passive;
using CalamityLegendsComeBack.Weapons.Vesuvius.RightClick.Javelin;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Vesuvius.RightClick
{
    public class VesuviusFaultJavelin : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        // Throw the staff itself — use the weapon's own texture
        public override string Texture => "CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuvius";

        private const int FlightState = 0;
        private const int NpcStickState = 1;
        private const int TileStickState = 2;
        private const int TileFuseTime = 180;
        private const float VisualRotationOffset = MathHelper.PiOver4;

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[2] <= 0f ? 1f : Projectile.ai[2], 1f, 5f);
        private bool Embedded => Projectile.ai[0] == NpcStickState || Projectile.ai[0] == TileStickState;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 30;
            Projectile.height = 30;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 240;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            // Keep the actual throw angle — the staff flies true, it does not tumble or spin
            if (Projectile.ai[0] == FlightState)
            {
                UpdateFlightRotation();
            }

            if (Projectile.ai[0] == NpcStickState)
            {
                RunNpcStickyAI(15);
                if (!Projectile.active)
                    return;
            }

            if (Embedded)
            {
                Projectile.tileCollide = false;
                if (Projectile.ai[0] == TileStickState)
                    Projectile.velocity *= 0f;
            }

            Lighting.AddLight(Projectile.Center, 0.65f, 0.18f, 0.04f);

            if (Embedded)
            {
                if (!Main.dedServ && Main.rand.NextBool(3))
                    VesuviusVolcanicVisuals.SpawnVentMix(Projectile.Center + Main.rand.NextVector2Circular(12f, 10f), 0.55f + Stage * 0.06f, Stage >= 3);
                return;
            }

            if (Projectile.localAI[0]++ < 5f)
                Projectile.tileCollide = false;
            else
            {
                Projectile.tileCollide = true;
                // Slight gravity arc — volcanic staff is heavy
                Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.07f, -20f, 20f);
            }

            if (!Main.dedServ)
                VesuviusVolcanicVisuals.SpawnTravelMix(Projectile.Center, -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.14f), 0.9f + Stage * 0.08f, Stage >= 4);

            // Stage 4+: Pyroclastic debris drops on flight path — irregular spacing
            if (Stage >= 4 && Projectile.owner == Main.myPlayer && Projectile.localAI[0] % Main.rand.Next(6, 15) == 0)
            {
                Vector2 fallVelocity = new Vector2(Projectile.velocity.X * Main.rand.NextFloat(0.06f, 0.12f), Main.rand.NextFloat(5f, 9f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(32f, 18f) - Vector2.UnitY * Main.rand.NextFloat(25f, 75f),
                    fallVelocity,
                    ModContent.ProjectileType<VesuviusPyroclasticFlow>(),
                    Math.Max(1, (int)(Projectile.damage * 0.22f)),
                    Projectile.knockBack * 0.25f,
                    Projectile.owner,
                    Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X));
            }

            // Stage 5+: Gas cloud trailing — irregular intervals
            if (Stage >= 5 && Projectile.owner == Main.myPlayer && Projectile.localAI[0] % Main.rand.Next(13, 25) == 0)
            {
                Vector2 gasVelocity = -Projectile.velocity.SafeNormalize(Vector2.UnitY).RotatedByRandom(0.65f) * Main.rand.NextFloat(1.2f, 3.8f) - Vector2.UnitY * Main.rand.NextFloat(0.2f, 0.9f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 38f + Main.rand.NextVector2Circular(16f, 16f),
                    gasVelocity,
                    ModContent.ProjectileType<VesuviusVolcanicGasCloud>(),
                    Math.Max(1, (int)(Projectile.damage * 0.12f)),
                    0f,
                    Projectile.owner,
                    96f,
                    Math.Sign(Projectile.velocity.X == 0f ? Main.player[Projectile.owner].direction : Projectile.velocity.X));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            ApplyFlightRotation(oldVelocity);
            Projectile.ai[0] = TileStickState;
            Projectile.velocity = Vector2.Zero;
            Projectile.tileCollide = false;
            Projectile.timeLeft = TileFuseTime;
            Projectile.netUpdate = true;
            SpawnFaultCore(oldVelocity);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.ai[0] == TileStickState)
                SpawnTileFuseExplosion();
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            StickToNPC(target, 15);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
            SpawnSmallImpact(target.Center);

            // Release 2 fireballs in random directions on every strike (initial or stuck ticks)
            if (Projectile.owner == Main.myPlayer)
            {
                for (int i = 0; i < 2; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 8f);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<VesuviusFaultFireball>(),
                        (int)(Projectile.damage * 0.45f),
                        Projectile.knockBack * 0.5f,
                        Projectile.owner);
                }
            }

            if (Stage >= 5 && Projectile.localAI[2] == 0f && Projectile.owner == Main.myPlayer)
            {
                Projectile.localAI[2] = 1f;
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * Math.Sign(target.Center.X - Main.player[Projectile.owner].Center.X));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    direction * 8f + Vector2.UnitY * 2f,
                    ModContent.ProjectileType<VesuviusSubductionZone>(),
                    Math.Max(1, (int)(Projectile.damage * 1.85f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    direction.X >= 0f ? 1f : -1f);
            }
        }

        private void UpdateFlightRotation()
        {
            ApplyFlightRotation(Projectile.velocity);
        }

        private void ApplyFlightRotation(Vector2 velocity)
        {
            if (velocity.LengthSquared() <= 0.001f)
                return;

            int direction = velocity.X > 0.001f ? 1 : velocity.X < -0.001f ? -1 : Projectile.direction;
            if (direction == 0)
                direction = 1;

            Projectile.spriteDirection = Projectile.direction = direction;
            // Direction-independent base rotation — mirroring is handled by an actual
            // horizontal flip at draw time (see PreDraw), not by faking it with rotation.
            Projectile.rotation = velocity.ToRotation() + VisualRotationOffset;
        }

        private void StickToNPC(NPC target, int maxStick)
        {
            if (Projectile.owner != Main.myPlayer || target == null || !target.active || target.dontTakeDamage)
                return;

            if (target.reflectsProjectiles && Projectile.CanBeReflected())
            {
                target.ReflectProjectile(Projectile);
                return;
            }

            Projectile.ai[0] = NpcStickState;
            Projectile.ai[1] = target.whoAmI;
            Projectile.velocity = target.Center - Projectile.Center;
            Projectile.localAI[0] = 0f;
            Projectile.tileCollide = false;
            Projectile.timeLeft = 900;
            Projectile.netUpdate = true;
            CullOldStuckJavelins(target.whoAmI, maxStick);
        }

        private void RunNpcStickyAI(int seconds)
        {
            Projectile.tileCollide = false;
            Projectile.localAI[0]++;

            int npcIndex = (int)Projectile.ai[1];
            if (npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC npc = Main.npc[npcIndex];
            if (!npc.active || npc.dontTakeDamage || Projectile.localAI[0] >= 60f * seconds)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = npc.Center - Projectile.velocity * 2f;
            Projectile.gfxOffY = npc.gfxOffY;
            if (Projectile.localAI[0] % 30f == 0f)
                npc.HitEffect(0, 1.0);
        }

        private void CullOldStuckJavelins(int npcIndex, int maxStick)
        {
            Point[] stuckProjectiles = new Point[maxStick];
            int stuckCount = 0;
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (i != Projectile.whoAmI &&
                    projectile.active &&
                    projectile.owner == Projectile.owner &&
                    projectile.type == Projectile.type &&
                    projectile.ai[0] == NpcStickState &&
                    projectile.ai[1] == npcIndex)
                {
                    stuckProjectiles[stuckCount++] = new Point(i, projectile.timeLeft);
                    if (stuckCount >= stuckProjectiles.Length)
                        break;
                }
            }

            if (stuckCount < stuckProjectiles.Length)
                return;

            int oldest = 0;
            for (int i = 1; i < stuckProjectiles.Length; i++)
            {
                if (stuckProjectiles[i].Y < stuckProjectiles[oldest].Y)
                    oldest = i;
            }

            Main.projectile[stuckProjectiles[oldest].X].Kill();
        }

        private void SpawnFaultCore(Vector2 oldVelocity)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusFaultCore>(),
                    Math.Max(1, (int)(Projectile.damage * 0.72f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    Stage,
                    oldVelocity.ToRotation());
            }

            SpawnSmallImpact(Projectile.Center);
        }

        private void SpawnTileFuseExplosion()
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<VesuviusRightMeteorExplosion>(),
                    Math.Max(1, (int)(Projectile.damage * 1.15f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    Stage);

                int fireballCount = 5 + Stage;
                for (int i = 0; i < fireballCount; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / fireballCount + Main.rand.NextFloat(-0.16f, 0.16f)).ToRotationVector2() * Main.rand.NextFloat(8.5f, 13.5f);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        velocity,
                        ModContent.ProjectileType<VesuviusFaultFireball>(),
                        Math.Max(1, (int)(Projectile.damage * 0.5f)),
                        Projectile.knockBack * 0.65f,
                        Projectile.owner,
                        Stage);
                }

                int homingCount = 3 + Stage / 2;
                for (int i = 0; i < homingCount; i++)
                {
                    Vector2 velocity = (-Vector2.UnitY).RotatedBy(MathHelper.Lerp(-0.95f, 0.95f, homingCount == 1 ? 0.5f : i / (float)(homingCount - 1))) * Main.rand.NextFloat(9f, 13f);
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center + Main.rand.NextVector2Circular(12f, 8f),
                        velocity,
                        ModContent.ProjectileType<VesuviusRightRisingSpark>(),
                        Math.Max(1, (int)(Projectile.damage * 0.42f)),
                        Projectile.knockBack * 0.35f,
                        Projectile.owner,
                        Stage);
                }
            }

            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.7f, Pitch = -0.28f }, Projectile.Center);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, 3.5f * Utils.GetLerpValue(1600f, 240f, Main.LocalPlayer.Distance(Projectile.Center), true));
            VesuviusVolcanicVisuals.SpawnImpactMix(Projectile.Center, 1.45f + Stage * 0.15f);
            for (int i = 0; i < 4; i++)
                VesuviusVolcanicVisuals.SpawnHeatPulse(Projectile.Center + Main.rand.NextVector2Circular(24f, 18f), 1.15f + Stage * 0.12f);
        }

        private void SpawnSmallImpact(Vector2 center)
        {
            if (Main.dedServ)
                return;

            SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.65f, Pitch = -0.22f }, center);
            for (int i = 0; i < 16; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, Main.rand.NextBool(3) ? DustID.Torch : DustID.Smoke,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 8f), 100,
                    Color.OrangeRed, Main.rand.NextFloat(0.8f, 1.5f));
                dust.noGravity = Main.rand.NextBool();
            }

            VesuviusProjectileVisuals.SpawnMoltenBloom(center + Main.rand.NextVector2Circular(6f, 6f), Main.rand.NextFloat(28f, 52f), 0.64f);
            VesuviusVolcanicVisuals.SpawnImpactMix(center, 0.85f + Stage * 0.12f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D glow = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Vesuvius/NewVesuviusGlow").Value;

            // Mirror via an actual horizontal flip (matching the holdout) instead of faking
            // it with rotation — otherwise leftward throws end up rotated 180 degrees off.
            bool facingLeft = Projectile.spriteDirection == -1;
            SpriteEffects flip = facingLeft ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            float rotationCorrection = facingLeft ? MathHelper.PiOver2 : 0f;
            float drawRotation = Projectile.rotation + rotationCorrection;
            Vector2 origin = texture.Size() * 0.5f;

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            // Soft heat halo. This replaces the old "golden border", which stamped eight rotating
            // copies of the entire staff sprite around the projectile — at 2.4px offsets that
            // read as a blurry orange smear, not an outline.
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * 0.45f,
                0f, bloom.Size() * 0.5f, 0.85f * Projectile.scale, SpriteEffects.None);

            // Molten afterimage trail, fading fast so the staff stays readable in flight.
            for (int i = Embedded ? 0 : Projectile.oldPos.Length - 1; i >= 1; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 oldCenter = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                float t = (Projectile.oldPos.Length - i) / (float)Projectile.oldPos.Length;
                Color trailC = VesuviusProjectileVisuals.AdditiveColor(VesuviusProjectileVisuals.LavaOrange) * (t * t * 0.34f);
                Main.EntitySpriteDraw(texture, oldCenter - Main.screenPosition, null,
                    trailC,
                    Projectile.oldRot[i] + rotationCorrection, origin,
                    MathHelper.Lerp(0.6f, 1f, t) * Projectile.scale, flip);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);

            // Solid staff, then its own glowmask on top — the mod's normal weapon draw order.
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null,
                lightColor, drawRotation, origin, 1.05f * Projectile.scale, flip);
            Main.EntitySpriteDraw(glow, Projectile.Center - Main.screenPosition, null,
                Color.White with { A = 0 }, drawRotation, glow.Size() * 0.5f, 1.05f * Projectile.scale, flip);

            return false;
        }
    }

    public class VesuviusFaultCore : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int TileFuseTime = 180;

        private int Stage => (int)MathHelper.Clamp(Projectile.ai[0], 1f, 5f);

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = TileFuseTime;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 22;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;

            // Initialize random-interval timers on first frame
            if (Projectile.localAI[0] == 1f)
            {
                Projectile.localAI[1] = Main.rand.Next(Stage >= 3 ? 12 : 22, Stage >= 3 ? 28 : 48);
                Projectile.localAI[2] = Main.rand.Next(18, 50);
            }

            float lightPulse = 0.85f + 0.15f * (float)Math.Sin(Projectile.localAI[0] * 0.11f);
            Lighting.AddLight(Projectile.Center, (0.78f + Stage * 0.12f) * lightPulse, 0.24f * lightPulse, 0.05f * lightPulse);

            if (!Main.dedServ)
            {
                // Irregular vent emissions — lava pressure is never perfectly steady
                if (Main.rand.NextBool(Math.Max(2, 5 - Stage)))
                {
                    Vector2 ventPos = Projectile.Center + new Vector2(Main.rand.NextFloat(-16f, 16f), Main.rand.NextFloat(-4f, 14f));
                    VesuviusVolcanicVisuals.SpawnVentMix(ventPos, 0.82f + Stage * 0.1f, Stage >= 3);
                }

                if (Main.rand.NextBool(Math.Max(6, 18 - Stage * 2)))
                    VesuviusVolcanicVisuals.SpawnHeatPulse(Projectile.Center, 0.78f + Stage * 0.1f);

                // Lava drips from base (not metaball — those are only for fixed surface effects)
                if (Main.rand.NextBool(3))
                {
                    Dust drip = Dust.NewDustPerfect(
                        Projectile.Center + new Vector2(Main.rand.NextFloat(-12f, 12f), Main.rand.NextFloat(4f, 18f)),
                        DustID.InfernoFork,
                        new Vector2(Main.rand.NextFloat(-0.6f, 0.6f), Main.rand.NextFloat(0.4f, 1.8f)),
                        90, VesuviusProjectileVisuals.LavaOrange, Main.rand.NextFloat(0.7f, 1.4f));
                    drip.noGravity = false;
                }
            }

            if (Projectile.owner == Main.myPlayer)
            {
                // Shot timer — irregular intervals make the vent feel alive, not mechanical
                if (Stage >= 2 && --Projectile.localAI[1] <= 0f)
                {
                    Projectile.localAI[1] = Main.rand.Next(
                        Stage >= 4 ? 10 : Stage >= 3 ? 14 : 22,
                        Stage >= 4 ? 28 : Stage >= 3 ? 36 : 52);
                    TryFireTurretShot();
                }

                // Chaotic volcanic event timer
                if (--Projectile.localAI[2] <= 0f)
                {
                    int minWait = Math.Max(14, 44 - Stage * 5);
                    int maxWait = Math.Max(28, 88 - Stage * 10);
                    Projectile.localAI[2] = Main.rand.Next(minWait, maxWait);
                    TriggerChaoticEvent();
                }
            }
        }

        private void TriggerChaoticEvent()
        {
            // Stage 5: chance of a mass eruption — all phenomena at once
            if (Stage >= 5 && Main.rand.NextBool(3))
            {
                TriggerMassEruption();
                return;
            }

            // Event pool grows with each stage
            int choices = Stage switch { >= 4 => 4, >= 3 => 3, >= 2 => 2, _ => 1 };
            switch (Main.rand.Next(choices))
            {
                case 0: SpawnHazardLavaPools(); break;
                case 1: LaunchVolcanicBombSalvo(); break;
                case 2: TriggerPyroclasticBurst(); break;
                case 3: ReleaseGasCloud(); break;
            }
        }

        private void SpawnHazardLavaPools()
        {
            int count = Main.rand.Next(1, Stage >= 3 ? 4 : 3);
            float spread = 72f + Stage * 26f;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = Projectile.Center + new Vector2(
                    Main.rand.NextFloat(-spread, spread),
                    Main.rand.NextFloat(14f, 44f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), pos, Vector2.Zero,
                    ModContent.ProjectileType<VesuviusLingeringLava>(),
                    Math.Max(1, (int)(Projectile.damage * 0.22f)), 0f, Projectile.owner,
                    Main.rand.NextFloat(72f, 114f) + Stage * 7f);
            }
        }

        private void LaunchVolcanicBombSalvo()
        {
            int bombs = Main.rand.Next(2, Stage >= 4 ? 5 : 4);
            for (int i = 0; i < bombs; i++)
            {
                // Scatter angles — not a neat fan, just chaotic upward launches
                float angle = MathHelper.ToRadians(Main.rand.NextFloat(-85f, 85f)) - MathHelper.PiOver2;
                float speed = Main.rand.NextFloat(7f, 15f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 8f),
                    angle.ToRotationVector2() * speed,
                    ModContent.ProjectileType<VesuviusVolcanicBomb>(),
                    Math.Max(1, (int)(Projectile.damage * 0.56f)),
                    Projectile.knockBack * 0.85f,
                    Projectile.owner,
                    Main.rand.Next(6),
                    Main.rand.NextFloat(0.9f, 1.28f));
            }
            if (!Main.dedServ)
                VesuviusVolcanicVisuals.SpawnHeatPulse(Projectile.Center, 1.12f + Stage * 0.08f);
        }

        private void TriggerPyroclasticBurst()
        {
            int flows = Main.rand.Next(1, Stage >= 4 ? 4 : 3);
            for (int i = 0; i < flows; i++)
            {
                int side = Main.rand.NextBool() ? 1 : -1;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Vector2.UnitX * side * Main.rand.NextFloat(8f, 28f),
                    new Vector2(side * Main.rand.NextFloat(5.5f, 12f), Main.rand.NextFloat(-1.5f, 1.5f)),
                    ModContent.ProjectileType<VesuviusPyroclasticFlow>(),
                    Math.Max(1, (int)(Projectile.damage * 0.28f)),
                    Projectile.knockBack * 0.28f,
                    Projectile.owner,
                    side);
            }
        }

        private void ReleaseGasCloud()
        {
            int clouds = Stage >= 5 ? Main.rand.Next(1, 3) : 1;
            for (int i = 0; i < clouds; i++)
            {
                float wind = Main.rand.NextBool() ? 1f : -1f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(26f, 16f),
                    new Vector2(wind * Main.rand.NextFloat(0.5f, 2.3f), -Main.rand.NextFloat(0.3f, 1.4f)),
                    ModContent.ProjectileType<VesuviusVolcanicGasCloud>(),
                    Math.Max(1, (int)(Projectile.damage * 0.16f)), 0f, Projectile.owner,
                    126f + Stage * 14f, wind);
            }
        }

        private void TriggerMassEruption()
        {
            SpawnHazardLavaPools();
            LaunchVolcanicBombSalvo();
            TriggerPyroclasticBurst();
            ReleaseGasCloud();

            if (!Main.dedServ)
            {
                SoundEngine.PlaySound(SoundID.Item89 with { Volume = 0.88f, Pitch = -0.38f }, Projectile.Center);
                for (int i = 0; i < 5; i++)
                    VesuviusVolcanicVisuals.SpawnHeatPulse(
                        Projectile.Center + Main.rand.NextVector2Circular(38f, 26f),
                        1.35f + i * 0.12f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
            if (Projectile.owner == Main.myPlayer)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<VesuviusLingeringLava>(),
                    Math.Max(1, (int)(Projectile.damage * 0.36f)), 0f, Projectile.owner, 58f);
            }
        }

        private void TryFireTurretShot()
        {
            NPC target = FindTarget(780f);
            Vector2 direction = target != null
                ? Projectile.SafeDirectionTo(target.Center + target.velocity * 10f)
                : Vector2.UnitY.RotatedBy(Main.rand.NextFloat(-0.72f, 0.72f));

            float speed = Stage >= 3 ? Main.rand.NextFloat(14f, 17.5f) : Main.rand.NextFloat(10f, 13.5f);
            int damage = (int)(Projectile.damage * (Stage >= 3 ? Main.rand.NextFloat(0.5f, 0.62f) : Main.rand.NextFloat(0.34f, 0.43f)));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center + direction * 42f,
                direction * speed,
                ModContent.ProjectileType<VesuviusFaultFireball>(),
                Math.Max(1, damage),
                Projectile.knockBack,
                Projectile.owner,
                Stage);

            SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.44f, Pitch = Stage >= 3 ? 0.08f : -0.12f }, Projectile.Center);
            if (!Main.dedServ)
                VesuviusVolcanicVisuals.SpawnImpactMix(Projectile.Center + direction * 42f, 0.44f + Stage * 0.06f);
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;
                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < bestDistance && Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                {
                    bestDistance = distance;
                    bestTarget = npc;
                }
            }
            return bestTarget;
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class VesuviusFaultFireball : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/Melee/VolcanicFireball";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 6;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 10;
            Projectile.timeLeft = 300;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 4)
            {
                Projectile.frameCounter = 0;
                Projectile.frame++;
                if (Projectile.frame >= Main.projFrames[Type])
                    Projectile.frame = 0;
            }

            Lighting.AddLight(Projectile.Center, 0.55f, 0.18f, 0f);

            if (!Main.dedServ)
                VesuviusVolcanicVisuals.SpawnTravelMix(Projectile.Center + Projectile.velocity, -Projectile.velocity * 0.08f, 0.74f, false);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 180);
        }

        // Calamity's VolcanicFireball keeps a constant fire tint instead of taking ambient
        // light, so it stays legible in unlit caves. Without this the fireballs went dark.
        public override Color? GetAlpha(Color lightColor) => new Color(255, Main.DiscoG, 53, Projectile.alpha);

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }

    public class VesuviusPyroclasticFlow : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 38;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.ai[1] = 1f;
            Projectile.velocity = new Vector2((Projectile.ai[0] == 0f ? 1f : Projectile.ai[0]) * 7.5f, 0f);
            Projectile.tileCollide = false;
            Projectile.netUpdate = true;
            return false;
        }

        public override void AI()
        {
            if (Projectile.ai[1] == 0f)
            {
                Projectile.velocity.Y += 0.35f;
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            else
            {
                Projectile.velocity.X *= 0.985f;
                Projectile.velocity.Y = 0f;
                Projectile.rotation = 0f;
            }

            if (!Main.dedServ)
            {
                if (Main.rand.NextBool(3))
                {
                    Vector2 lavaPosition = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.42f);
                    VesuviusProjectileVisuals.SpawnMoltenBloom(lavaPosition, Main.rand.NextFloat(16f, 34f), 0.52f);
                }

                if (Main.rand.NextBool(3))
                {
                    Particle ash = new SquareAshParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.5f, Projectile.height * 0.5f),
                        new Vector2(-Projectile.velocity.X * 0.1f, -Main.rand.NextFloat(0.4f, 2f)),
                        Main.rand.Next(22, 42),
                        Main.rand.NextFloat(0.45f, 0.9f),
                        Color.Lerp(Color.Gray, Color.OrangeRed, 0.22f));
                    GeneralParticleHandler.SpawnParticle(ash);
                }

                VesuviusVolcanicVisuals.SpawnPyroclasticMix(
                    Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.height * 0.38f),
                    Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X));
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }

    public class VesuviusSubductionZone : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Vesuvius";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 72;
            Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 105;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Projectile.localAI[0]++;
            Projectile.velocity.Y += 0.12f;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, 1f, 0.24f, 0.04f);

            if (!Main.dedServ)
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(new Vector2(Projectile.ai[0], 0.35f));
                Vector2 start = Projectile.Center - dir * 30f;
                for (int i = 0; i < 3; i++)
                {
                    float along = Main.rand.NextFloat(0f, 260f);
                    Vector2 pos = start + dir * along + Main.rand.NextVector2Circular(18f, 18f);
                    VesuviusProjectileVisuals.SpawnMoltenBloom(pos, Main.rand.NextFloat(26f, 54f), 0.58f);
                    VesuviusVolcanicVisuals.SpawnSubductionMix(pos, dir, i == 0);
                }

                if (Projectile.localAI[0] % 12f == 0f)
                    VesuviusVolcanicVisuals.SpawnHeatPulse(Projectile.Center + dir * Main.rand.NextFloat(32f, 190f), 1.15f);
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 dir = Projectile.velocity.SafeNormalize(new Vector2(Projectile.ai[0], 0.35f));
            Vector2 start = Projectile.Center - dir * 20f;
            Vector2 end = Projectile.Center + dir * 290f + Vector2.UnitY * 80f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 72f, ref collisionPoint);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 360);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 dir = Projectile.velocity.SafeNormalize(new Vector2(Projectile.ai[0], 0.35f));
            Vector2 start = Projectile.Center - dir * 20f;
            Vector2 end = Projectile.Center + dir * 290f + Vector2.UnitY * 80f;
            float fade = Utils.GetLerpValue(0f, 20f, Projectile.timeLeft, true);
            float bloomWidth = bloom.Width;

            // Previously this was a single flat 72px-tall MagicPixel rectangle — a solid orange
            // bar with hard ends and no falloff, which is why it looked like a placeholder.
            // It is now built from overlapping soft nodes that taper toward both ends, giving a
            // molten fissure that widens at the rupture point and thins out along the fault.
            const int Segments = 16;
            Main.spriteBatch.SetBlendState(BlendState.Additive);
            for (int i = 0; i < Segments; i++)
            {
                float completion = i / (float)(Segments - 1);
                Vector2 position = Vector2.Lerp(start, end, completion);

                // Widest just past the impact point, tapering to nothing at the far end.
                float taper = (float)Math.Sin(MathHelper.Pi * (float)Math.Pow(completion, 0.7f));
                float width = 78f * taper;
                if (width <= 1f)
                    continue;

                float flicker = 0.85f + 0.15f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 11f + i * 0.9f);
                Color hot = Color.Lerp(new Color(255, 176, 60), new Color(255, 62, 18), completion);

                Main.EntitySpriteDraw(
                    bloom,
                    position - Main.screenPosition,
                    null,
                    VesuviusProjectileVisuals.AdditiveColor(hot) * 0.3f * fade * flicker,
                    0f,
                    bloom.Size() * 0.5f,
                    width / bloomWidth,
                    SpriteEffects.None);

                // Bright inner seam.
                Main.EntitySpriteDraw(
                    bloom,
                    position - Main.screenPosition,
                    null,
                    VesuviusProjectileVisuals.AdditiveColor(Color.Lerp(Color.White, hot, 0.45f)) * 0.34f * fade * flicker,
                    0f,
                    bloom.Size() * 0.5f,
                    width * 0.38f / bloomWidth,
                    SpriteEffects.None);
            }
            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }
    }

    internal static class VesuviusVolcanicVisuals
    {
        private static readonly Color LavaColor = new(255, 104, 28);
        private static readonly Color HotColor = new(255, 188, 72);
        private static readonly Color SmokeColor = new(92, 78, 72);

        internal static void SpawnTravelMix(Vector2 position, Vector2 velocity, float intensity, bool allowSmoke)
        {
            if (Main.dedServ)
                return;

            intensity = MathHelper.Clamp(intensity, 0.3f, 1.6f);

            if (Main.rand.NextFloat() < 0.72f * intensity)
            {
                Dust flame = Dust.NewDustPerfect(
                    position + Main.rand.NextVector2Circular(7f, 7f),
                    DustID.InfernoFork,
                    velocity.RotatedByRandom(0.28f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    80,
                    Main.rand.NextBool(4) ? HotColor : LavaColor,
                    Main.rand.NextFloat(0.72f, 1.22f) * intensity);
                flame.noGravity = true;
            }

            if (Main.rand.NextFloat() < 0.42f * intensity)
            {
                Particle ember = new SparkParticle(
                    position + Main.rand.NextVector2Circular(8f, 8f),
                    velocity.RotatedByRandom(0.35f) * Main.rand.NextFloat(0.45f, 0.9f),
                    false,
                    Main.rand.Next(8, 15),
                    Main.rand.NextFloat(0.18f, 0.46f) * intensity,
                    Main.rand.NextBool(4) ? Color.White : Color.Lerp(LavaColor, HotColor, Main.rand.NextFloat(0.2f, 0.72f)));
                GeneralParticleHandler.SpawnParticle(ember);
            }

            if (Main.rand.NextFloat() < 0.13f * intensity)
            {
                Particle heatMist = new SmallSmokeParticle(
                    position,
                    velocity * Main.rand.NextFloat(0.25f, 0.55f),
                    Color.Lerp(LavaColor, HotColor, 0.28f),
                    SmokeColor,
                    Main.rand.NextFloat(0.42f, 0.8f) * intensity,
                    Main.rand.Next(100, 140),
                    Main.rand.NextFloat(-0.08f, 0.08f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (allowSmoke && Main.rand.NextFloat() < 0.055f * intensity)
                SpawnRareSmoke(position, velocity * 0.4f, intensity * 0.72f, false);
        }

        internal static void SpawnVentMix(Vector2 position, float intensity, bool glowing)
        {
            if (Main.dedServ)
                return;

            Vector2 riseVelocity = -Vector2.UnitY.RotatedByRandom(0.38f) * Main.rand.NextFloat(1.1f, 3.4f);
            SpawnTravelMix(position, riseVelocity, intensity, false);

            if (Main.rand.NextBool(7))
            {
                Particle heatMist = new MediumMistParticle(
                    position,
                    riseVelocity * Main.rand.NextFloat(0.35f, 0.7f),
                    Color.Lerp(LavaColor, HotColor, 0.36f),
                    SmokeColor,
                    Main.rand.NextFloat(0.5f, 0.92f) * intensity,
                    Main.rand.Next(92, 148),
                    Main.rand.NextFloat(-0.075f, 0.075f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (Main.rand.NextBool(glowing ? 13 : 18))
                SpawnRareSmoke(position, riseVelocity, intensity * 0.85f, glowing);
        }

        internal static void SpawnPyroclasticMix(Vector2 position, int direction)
        {
            if (Main.dedServ)
                return;

            Vector2 drift = new(-direction * Main.rand.NextFloat(0.45f, 1.65f), -Main.rand.NextFloat(0.55f, 2.7f));

            if (Main.rand.NextBool(2))
            {
                Dust flame = Dust.NewDustPerfect(position, DustID.InfernoFork, drift * Main.rand.NextFloat(0.32f, 0.72f), 110, LavaColor, Main.rand.NextFloat(0.62f, 1.12f));
                flame.noGravity = !Main.rand.NextBool(4);
            }

            if (Main.rand.NextBool(3))
            {
                Particle heatMist = new MediumMistParticle(
                    position,
                    drift,
                    Color.Lerp(LavaColor, SmokeColor, 0.32f),
                    Color.DarkSlateGray,
                    Main.rand.NextFloat(0.54f, 1f),
                    Main.rand.Next(108, 172),
                    Main.rand.NextFloat(-0.055f, 0.055f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (Main.rand.NextBool(14))
                SpawnRareSmoke(position, drift, Main.rand.NextFloat(0.52f, 0.82f), false);
        }

        internal static void SpawnSubductionMix(Vector2 position, Vector2 direction, bool allowMist)
        {
            if (Main.dedServ)
                return;

            Vector2 riseVelocity = -Vector2.UnitY.RotatedByRandom(0.5f) * Main.rand.NextFloat(1.4f, 4.6f) - direction * Main.rand.NextFloat(0.1f, 0.8f);

            Dust flame = Dust.NewDustPerfect(
                position,
                DustID.InfernoFork,
                riseVelocity.RotatedByRandom(0.28f),
                90,
                Main.rand.NextBool(3) ? HotColor : LavaColor,
                Main.rand.NextFloat(0.88f, 1.52f));
            flame.noGravity = true;

            if (Main.rand.NextBool(2))
            {
                Particle ember = new GlowOrbParticle(
                    position + Main.rand.NextVector2Circular(9f, 9f),
                    riseVelocity * Main.rand.NextFloat(0.3f, 0.72f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.32f, 0.72f),
                    Color.Lerp(LavaColor, Color.White, Main.rand.NextFloat(0.08f, 0.32f)));
                GeneralParticleHandler.SpawnParticle(ember);
            }

            if (allowMist && Main.rand.NextBool(3))
            {
                Particle heatMist = new MediumMistParticle(
                    position,
                    riseVelocity * 0.42f,
                    Color.Lerp(LavaColor, HotColor, 0.28f),
                    Color.DarkSlateGray,
                    Main.rand.NextFloat(0.62f, 1.12f),
                    Main.rand.Next(96, 164),
                    Main.rand.NextFloat(-0.07f, 0.07f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }

            if (Main.rand.NextBool(26))
                SpawnRareSmoke(position, riseVelocity * 0.7f, Main.rand.NextFloat(0.72f, 1.08f), true);
        }

        internal static void SpawnHeatPulse(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                LavaColor * 0.42f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.015f * intensity,
                0.085f * intensity,
                16));
        }

        internal static void SpawnImpactMix(Vector2 center, float strength)
        {
            if (Main.dedServ)
                return;

            strength = MathHelper.Clamp(strength, 0.3f, 1.8f);
            VesuviusProjectileVisuals.SpawnMoltenBloom(center + Main.rand.NextVector2Circular(7f, 7f), Main.rand.NextFloat(26f, 46f) * strength, 0.66f);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                LavaColor,
                "CalamityMod/Particles/FlameExplosion",
                Vector2.One,
                Main.rand.NextFloat(-5f, 5f),
                0f,
                0.075f * strength,
                18));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                HotColor * 0.52f,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.02f,
                0.11f * strength,
                20));

            int burstCount = Math.Max(6, (int)(11f * strength));
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 burstVelocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 7.2f) * strength;
                Dust flame = Dust.NewDustPerfect(center, DustID.InfernoFork, burstVelocity, 80, Main.rand.NextBool(3) ? LavaColor : HotColor, Main.rand.NextFloat(0.82f, 1.42f));
                flame.noGravity = true;

                if (i % 3 == 0)
                {
                    Particle ember = new GlowOrbParticle(
                        center,
                        burstVelocity * 0.72f,
                        false,
                        Main.rand.Next(9, 16),
                        Main.rand.NextFloat(0.24f, 0.54f) * strength,
                        Main.rand.NextBool(4) ? Color.White : HotColor);
                    GeneralParticleHandler.SpawnParticle(ember);
                }
            }

            if (strength > 0.7f)
            {
                Particle heatMist = new MediumMistParticle(
                    center,
                    -Vector2.UnitY * Main.rand.NextFloat(0.6f, 1.8f),
                    Color.Lerp(LavaColor, HotColor, 0.24f),
                    SmokeColor,
                    Main.rand.NextFloat(0.72f, 1.12f) * strength,
                    Main.rand.Next(86, 138),
                    Main.rand.NextFloat(-0.07f, 0.07f));
                GeneralParticleHandler.SpawnParticle(heatMist);
            }
        }

        private static void SpawnRareSmoke(Vector2 position, Vector2 velocity, float scale, bool glowing)
        {
            Particle smoke = new HeavySmokeParticle(
                position + Main.rand.NextVector2Circular(6f, 6f),
                velocity + Main.rand.NextVector2Circular(0.45f, 0.45f),
                Color.Lerp(SmokeColor, LavaColor, Main.rand.NextFloat(0.08f, 0.24f)),
                Main.rand.Next(18, 34),
                Main.rand.NextFloat(0.32f, 0.72f) * scale,
                0.56f,
                Main.rand.NextFloat(-0.045f, 0.045f),
                glowing);
            GeneralParticleHandler.SpawnParticle(smoke);
        }
    }
}
