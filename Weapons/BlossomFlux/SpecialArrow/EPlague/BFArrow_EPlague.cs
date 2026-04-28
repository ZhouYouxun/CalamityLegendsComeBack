using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
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
    // E 战术右键箭：扎附在目标或地形后，持续朝周围排出毒云。
    internal class BFArrow_EPlague : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/EPlague/BFArrow_EPlague";
        private const float LeftSporeState = -1f;
        private const int MaxLeftSporeBounces = 3;
        private const float LeftSporeSearchRange = 620f;

        private int gasTimer;
        private int storedGasDamage = 1;
        private Vector2 stickOffset;

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];
        private ref float FlightTimer => ref Projectile.localAI[0];
        private ref float BounceCounter => ref Projectile.localAI[1];
        private bool IsLeftSpore => State == LeftSporeState;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 300, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            storedGasDamage = System.Math.Max(Projectile.damage, 1);
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;

            if (!IsLeftSpore)
                return;

            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 170;
            Projectile.extraUpdates = 1;
            Projectile.localNPCHitCooldown = 12;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.noDropItem = true;
            BFArrowCommon.TagBlossomFluxLeftArrow(Projectile);
        }

        public override bool? CanDamage() => State <= 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State <= 0f ? null : false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague).ToVector3() * 0.42f);

            if (IsLeftSpore)
            {
                UpdateLeftSporeFlight();
                return;
            }

            if (State == 0f)
            {
                FlightTimer++;
                if (FlightTimer > 5f)
                {
                    Projectile.velocity.Y += 0.18f;
                    Projectile.velocity.X *= 0.9835f;
                }

                BFArrowCommon.FaceForward(Projectile);
                BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 1.02f);
                EmitPlagueFlightFX(false);

                return;
            }

            Projectile.friendly = false;
            Projectile.tileCollide = false;

            if (State == 1f)
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

                Projectile.Center = attachedNpc.Center + stickOffset;
                Projectile.gfxOffY = attachedNpc.gfxOffY;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
            }

            EmitPlagueGas();
            EmitPlagueAnchorAura();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (IsLeftSpore)
            {
                HandleLeftSporeHit(target);
                return;
            }

            if (State != 0f)
                return;

            stickOffset = Projectile.Center - target.Center;
            storedGasDamage = System.Math.Max(Projectile.damage, storedGasDamage);
            State = 1f;
            AttachedNpcIndex = target.whoAmI;
            Projectile.velocity = Vector2.Zero;
            Projectile.damage = 0;
            Projectile.timeLeft = 180;
            Projectile.netUpdate = true;

            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 120);
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 12, 0.9f, 3f, 0.85f, 1.15f);
            SpawnPlagueAnchorFX(target.Center, 1.1f);
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.38f, Pitch = 0.1f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (IsLeftSpore)
            {
                ReflectLeftSpore(oldVelocity);
                return false;
            }

            State = 2f;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 180;
            Projectile.netUpdate = true;

            SpawnPlagueAnchorFX(Projectile.Center, 0.9f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.22f, Pitch = 0.1f }, Projectile.Center);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 14, 1.2f, 4.2f, 0.9f, 1.25f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (State <= 0f)
            {
                DrawPlagueSprayOverlay();
                BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_EPlague);
            }
            else
                BFArrowCommon.DrawProjectile(Projectile, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value, Projectile.GetAlpha(lightColor), Projectile.rotation, Projectile.scale);

            return false;
        }

        private void UpdateLeftSporeFlight()
        {
            FlightTimer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            if (FlightTimer > 8f)
                Projectile.velocity = Projectile.velocity.RotatedBy(System.Math.Sin((FlightTimer + Projectile.identity) * 0.19f) * 0.006f);

            if (Projectile.velocity.LengthSquared() > 15.5f * 15.5f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(direction) * 15.5f;

            BFArrowCommon.FaceForward(Projectile);
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 1.2f);
            EmitPlagueFlightFX(true);

            if (Main.dedServ || !Projectile.FinalExtraUpdate())
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            for (int i = 0; i < 2; i++)
            {
                GlowOrbParticle orb = new(
                    Projectile.Center - direction * Main.rand.NextFloat(6f, 20f) + side * Main.rand.NextFloat(-10f, 10f),
                    -Projectile.velocity * 0.035f + Main.rand.NextVector2Circular(0.42f, 0.42f),
                    false,
                    Main.rand.Next(7, 11),
                    Main.rand.NextFloat(0.2f, 0.34f),
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.12f, 0.34f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }

        private void HandleLeftSporeHit(NPC target)
        {
            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 100);
            Projectile.localNPCImmunity[target.whoAmI] = 45;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 10, 0.9f, 3.4f, 0.76f, 1.18f);
            SpawnLeftSporeImpactFX(target.Center, 1f);
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.28f, Pitch = 0.34f }, target.Center);

            NPC nextTarget = FindNextSporeTarget(target);
            if (BounceCounter >= MaxLeftSporeBounces || nextTarget == null)
            {
                Projectile.Kill();
                return;
            }

            BounceCounter++;
            float speed = MathHelper.Clamp(Projectile.velocity.Length() * 0.92f + 1.2f, 8.5f, 15.5f);
            Vector2 fallback = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.Center = target.Center + fallback * 12f;
            Projectile.velocity = (nextTarget.Center - Projectile.Center).SafeNormalize(fallback).RotatedByRandom(0.06f) * speed;
            Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.86f));
            Projectile.netUpdate = true;
        }

        private NPC FindNextSporeTarget(NPC currentTarget)
        {
            NPC bestTarget = null;
            float bestDistance = LeftSporeSearchRange;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.whoAmI == currentTarget.whoAmI || npc.dontTakeDamage || npc.friendly || npc.life <= 0)
                    continue;

                if (!Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private void ReflectLeftSpore(Vector2 oldVelocity)
        {
            Vector2 newVelocity = Projectile.velocity;
            if (Projectile.velocity.X != oldVelocity.X)
                newVelocity.X = -oldVelocity.X;

            if (Projectile.velocity.Y != oldVelocity.Y)
                newVelocity.Y = -oldVelocity.Y;

            if (newVelocity.LengthSquared() < 4f)
                newVelocity = oldVelocity.SafeNormalize(Vector2.UnitY).RotatedBy(MathHelper.PiOver2) * 7f;

            Projectile.velocity = newVelocity * 0.92f;
            Projectile.timeLeft = System.Math.Min(Projectile.timeLeft, 90);
            Projectile.netUpdate = true;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 6, 0.75f, 2.6f, 0.64f, 1.05f);
            SpawnLeftSporeImpactFX(Projectile.Center, 0.72f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.18f, Pitch = 0.42f }, Projectile.Center);
        }

        private void EmitPlagueGas()
        {
            gasTimer++;

            if (Main.rand.NextBool())
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(1.2f, 1.2f) + new Vector2(0f, -0.18f),
                    100,
                    new Color(172, 228, 92),
                    Main.rand.NextFloat(0.85f, 1.35f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                HeavySmokeParticle smoke = new(
                    Projectile.Center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.16f),
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), Color.White, 0.12f),
                    18,
                    Main.rand.NextFloat(0.45f, 0.7f),
                    0.58f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (!Main.dedServ && gasTimer % 10 == 0)
            {
                DirectionalPulseRing pulse = new(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(0.3f, 0.3f),
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), Color.White, 0.18f),
                    new Vector2(Main.rand.NextFloat(0.95f, 1.3f), Main.rand.NextFloat(1.2f, 1.8f)),
                    Main.rand.NextFloat(-0.6f, 0.6f),
                    Main.rand.NextFloat(0.12f, 0.18f),
                    0.024f,
                    Main.rand.Next(12, 18));
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            if (gasTimer % 12 != 0 || Projectile.owner != Main.myPlayer)
                return;

            int burstCount = 3;
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < burstCount; i++)
            {
                Vector2 velocity = (baseAngle + MathHelper.TwoPi * i / burstCount + Main.rand.NextFloat(-0.22f, 0.22f)).ToRotationVector2() * Main.rand.NextFloat(1.8f, 3.6f) + new Vector2(0f, Main.rand.NextFloat(-0.9f, -0.2f));
                int gasIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity,
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    System.Math.Max(1, (int)(storedGasDamage * 0.38f)),
                    0f,
                    Projectile.owner,
                    Main.rand.Next(3),
                    Main.rand.NextFloat(0.9f, 1.2f));

                if (BFArrowCommon.InBounds(gasIndex, Main.maxProjectiles))
                    BFArrowCommon.ForceLocalNPCImmunity(Main.projectile[gasIndex], 12);
            }
        }

        private void SpawnPlagueAnchorFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.1f),
                new Vector2(1.08f, 1.32f),
                Main.rand.NextFloat(-0.25f, 0.25f),
                0.16f * intensity,
                0.034f,
                15);
            GeneralParticleHandler.SpawnParticle(pulse);

            HeavySmokeParticle smoke = new(
                center,
                Main.rand.NextVector2Circular(0.3f, 0.3f),
                Color.Lerp(mainColor, Color.Black, 0.18f),
                18,
                0.62f * intensity,
                0.6f,
                Main.rand.NextFloat(-0.03f, 0.03f),
                false);
            GeneralParticleHandler.SpawnParticle(smoke);

            for (int i = 0; i < 3; i++)
            {
                DetailedExplosion explosion = new(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(0.8f, 0.8f),
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.08f, 0.24f)),
                    Vector2.One,
                    Main.rand.NextFloat(-0.4f, 0.4f),
                    0f,
                    0.16f * intensity,
                    10 + i);
                GeneralParticleHandler.SpawnParticle(explosion);
            }
        }

        private void SpawnLeftSporeImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            Color acidColor = new(190, 255, 84);

            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.2f),
                0.38f * intensity,
                9));

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                Projectile.velocity.SafeNormalize(Vector2.UnitY) * 0.5f,
                Color.Lerp(mainColor, acidColor, 0.35f),
                new Vector2(0.72f, 1.55f),
                Projectile.velocity.ToRotation(),
                0.11f * intensity,
                0.028f,
                10));

            for (int i = 0; i < 6; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 3.2f),
                    0,
                    Color.Lerp(mainColor, acidColor, Main.rand.NextFloat(0.2f, 0.68f)),
                    Main.rand.NextFloat(0.72f, 1.1f) * intensity);
                dust.noGravity = true;
            }
        }

        private void EmitPlagueFlightFX(bool leftSpore)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            float intensity = leftSpore ? 1.25f : 0.88f;

            if (Main.rand.NextBool(leftSpore ? 2 : 3))
            {
                Vector2 placement = Projectile.Center - direction * Main.rand.NextFloat(2f, 14f) + normal * Main.rand.NextFloat(-9f, 9f);
                float speed = Main.rand.NextFloat(0.12f, 0.44f);
                Particle spark = new GlowOrbParticle(
                    placement,
                    -Projectile.velocity * speed,
                    false,
                    Main.rand.Next(6, leftSpore ? 10 : 8),
                    Main.rand.NextFloat(0.26f, 0.46f) * intensity,
                    Color.Lerp(mainColor, Color.White, Main.rand.NextFloat(0.12f, 0.34f)),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (leftSpore && Projectile.FinalExtraUpdate() && (int)FlightTimer % 3 == 0)
            {
                DirectionalPulseRing pulse = new(
                    Projectile.Center + direction * 7f,
                    Projectile.velocity * 0.035f,
                    Color.Lerp(mainColor, new Color(190, 255, 84), 0.32f),
                    new Vector2(0.42f, 1.2f),
                    direction.ToRotation(),
                    0.105f,
                    0.034f,
                    9);
                GeneralParticleHandler.SpawnParticle(pulse);
            }

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - direction * Main.rand.NextFloat(2f, 10f) + normal * Main.rand.NextFloat(-5f, 5f),
                DustID.GreenTorch,
                -Projectile.velocity.RotatedByRandom(leftSpore ? 0.44f : 0.22f) * Main.rand.NextFloat(0.12f, leftSpore ? 0.36f : 0.5f),
                0,
                Color.Lerp(mainColor, new Color(172, 228, 92), 0.42f),
                Main.rand.NextFloat(0.46f, 0.92f) * intensity);
            dust.noGravity = true;
        }

        private void DrawPlagueSprayOverlay()
        {
            Texture2D pointTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float squash = Utils.GetLerpValue(-3f, 10f, Projectile.velocity.Length(), true);
            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                DepthStencilState.None,
                Main.Rasterizer,
                null,
                Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                mainColor * 0.5f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.22f + squash * 0.08f,
                SpriteEffects.None,
                0);

            for (int i = 0; i < 3; i++)
            {
                float fade = 0.55f + 0.2f * i;
                Main.EntitySpriteDraw(
                    pointTexture,
                    drawPosition + Projectile.velocity * 5f * fade,
                    null,
                    Color.Lerp(Color.White, mainColor, i * 0.4f) * 0.55f,
                    Projectile.velocity.ToRotation() + MathHelper.PiOver2,
                    pointTexture.Size() * 0.5f,
                    new Vector2(0.68f - 0.16f * i, 0.72f + 0.28f * i) * 0.04f * (0.8f + squash * 0.55f),
                    SpriteEffects.None,
                    0);
            }

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

        private void EmitPlagueAnchorAura()
        {
            if (Main.dedServ || gasTimer % 6 != 0)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            for (int i = 0; i < 2; i++)
            {
                GlowOrbParticle orb = new(
                    Projectile.Center + Main.rand.NextVector2Circular(20f, 20f),
                    Main.rand.NextVector2Circular(0.6f, 0.6f) + new Vector2(0f, -0.2f),
                    false,
                    12,
                    Main.rand.NextFloat(0.22f, 0.34f),
                    Color.Lerp(mainColor, Color.White, 0.22f),
                    true,
                    false,
                    true);
                GeneralParticleHandler.SpawnParticle(orb);
            }
        }
    }
}
