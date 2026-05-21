using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFLeftPlagueReaper : ModProjectile, ILocalizedModType
    {
        private static readonly Color PlagueGreen = new(124, 238, 68);
        private static readonly Color PlagueBright = new(214, 255, 104);
        private static readonly Color PlagueDeep = new(34, 145, 46);
        private static readonly Color PlagueMurk = new(18, 74, 28);
        private static readonly Color PlagueAcid = new(188, 255, 54);

        private const int HomingDelayFrames = 25;
        private const int HomingWarmupFrames = 4;

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/LeafProj/BlossomFluxBOMB";

        private ref float Seed => ref Projectile.ai[0];
        private ref float Variant => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private bool homingAwakened;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.arrow = true;
            Projectile.noDropItem = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 155;
            Projectile.extraUpdates = 1;
            BFArrowCommon.ForceLocalNPCImmunity(Projectile, 14);
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (Seed <= 0f)
                Seed = Main.rand.NextFloat(1000f);

            Projectile.rotation = Projectile.velocity.ToRotation();
            SpawnTakeoffEffects();
        }

        public override void AI()
        {
            Timer++;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float homingReadiness = GetHomingReadiness();
            if (homingReadiness > 0f)
            {
                if (!homingAwakened)
                {
                    homingAwakened = true;
                    SpawnHomingAwakenEffects();
                }

                NPC priorityTarget = FindPriorityMarkedTarget();
                NPC target = priorityTarget ?? FindTargetAhead(forward, homingReadiness);
                if (target != null)
                {
                    bool priority = target == priorityTarget;
                    float targetSpeed = priority
                        ? MathHelper.Clamp(Projectile.velocity.Length() * 1.05f + 0.7f, 9.5f, 18.5f)
                        : MathHelper.Clamp(Projectile.velocity.Length() * 1.04f + 0.35f, 9.25f, 17.5f);
                    Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(forward) * targetSpeed;
                    float steering = (priority ? 0.22f : 0.135f) * homingReadiness;
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, steering);
                }
            }
            else
            {
                float preHomingDrift = (float)System.Math.Sin((Timer + Seed) * 0.13f) * 0.012f;
                Projectile.velocity = Projectile.velocity.RotatedBy(preHomingDrift) * 0.998f;
            }

            float wingBeat = (float)System.Math.Sin((Timer + Seed) * 0.82f);
            float drift = wingBeat * 0.009f + (float)System.Math.Sin((Timer + Seed * 0.31f) * 0.17f) * 0.005f;
            Projectile.velocity = Projectile.velocity.RotatedBy(drift);
            Projectile.velocity *= 1.002f;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, PlagueGreen.ToVector3() * 0.48f);

            if (Main.dedServ)
                return;

            EmitFlightEffects(forward, homingReadiness);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            bool markedTarget = target.GetGlobalNPC<BFArrow_CDetecNPC>().IsPriorityMarkedBy(Projectile.owner);
            BFPlaguePollutionNPC pollution = target.GetGlobalNPC<BFPlaguePollutionNPC>();
            pollution.ApplyPollution(target, markedTarget);
            pollution.ApplyPlagueDebuffs(target, markedTarget);
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), markedTarget ? 480 : 240);
            int gasDamage = System.Math.Max(1, (int)(Projectile.damage * 0.18f));
            Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.78f));

            if (Main.dedServ)
                return;

            SpawnImpactEffects(target.Center);
            ReleaseImpactGas(target.Center, gasDamage, markedTarget);
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.32f, Pitch = 0.32f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            if (Projectile.penetrate <= 1)
                return true;

            Vector2 newVelocity = Projectile.velocity;
            if (Projectile.velocity.X != oldVelocity.X)
                newVelocity.X = -oldVelocity.X;
            if (Projectile.velocity.Y != oldVelocity.Y)
                newVelocity.Y = -oldVelocity.Y;

            Projectile.velocity = newVelocity * 0.76f;
            Projectile.penetrate--;
            Projectile.netUpdate = true;

            if (!Main.dedServ)
                SpawnImpactEffects(Projectile.Center, 0.62f);

            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
                SpawnImpactEffects(Projectile.Center, 0.82f);
        }

        private float GetHomingReadiness()
        {
            int homingDelayUpdates = HomingDelayFrames * Projectile.MaxUpdates;
            int homingWarmupUpdates = HomingWarmupFrames * Projectile.MaxUpdates;
            return Utils.GetLerpValue(
                homingDelayUpdates,
                homingDelayUpdates + homingWarmupUpdates,
                Timer,
                true);
        }

        private NPC FindTargetAhead(Vector2 forward, float homingReadiness)
        {
            NPC bestTarget = null;
            float bestDistance = MathHelper.Lerp(760f, 1120f, homingReadiness);

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                Vector2 toNpc = npc.Center - Projectile.Center;
                float distance = toNpc.Length();
                if (distance >= bestDistance)
                    continue;

                float angle = System.Math.Abs(MathHelper.WrapAngle(forward.ToRotation() - toNpc.ToRotation()));
                if (angle > MathHelper.Lerp(MathHelper.PiOver2, MathHelper.Pi, homingReadiness))
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        private NPC FindPriorityMarkedTarget()
        {
            if (!BFArrowCommon.InBounds(Projectile.owner, Main.maxPlayers))
                return null;

            Player owner = Main.player[Projectile.owner];
            BFRightUIPlayer rightUiPlayer = owner.GetModPlayer<BFRightUIPlayer>();
            int targetIndex = rightUiPlayer.ReconPriorityTargetIndex;
            if (!BFArrowCommon.InBounds(targetIndex, Main.maxNPCs))
                return null;

            NPC target = Main.npc[targetIndex];
            if (!target.CanBeChasedBy(Projectile))
                return null;

            if (!target.GetGlobalNPC<BFArrow_CDetecNPC>().IsPriorityMarkedBy(Projectile.owner))
                return null;

            if (Vector2.DistanceSquared(Projectile.Center, target.Center) > 1500f * 1500f)
                return null;

            return target;
        }

        private void SpawnTakeoffEffects()
        {
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.28f, Pitch = 0.48f, PitchVariance = 0.12f }, Projectile.Center);

            Color color = Color.Lerp(PlagueGreen, PlagueBright, 0.35f);
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                color,
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-0.12f, 0.12f),
                0.22f,
                0.04f,
                12,
                true));

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GreenTorch,
                    -Projectile.velocity.RotatedByRandom(0.45f) * Main.rand.NextFloat(0.1f, 0.34f),
                    0,
                    Color.Lerp(PlagueDeep, PlagueBright, Main.rand.NextFloat(0.2f, 0.76f)),
                    Main.rand.NextFloat(0.62f, 1.05f));
                dust.noGravity = true;
            }
        }

        private void EmitFlightEffects(Vector2 forward, float homingReadiness)
        {
            if (!Projectile.FinalExtraUpdate())
                return;

            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float squash = Utils.GetLerpValue(5f, 15f, Projectile.velocity.Length(), true);
            float plagueIntensity = 0.75f + homingReadiness * 0.55f;

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 12f) + side * Main.rand.NextFloat(-4f, 4f),
                    -Projectile.velocity * 0.018f,
                    "CalamityMod/Particles/DualTrail",
                    false,
                    10,
                    0.052f * plagueIntensity,
                    Color.Lerp(PlagueMurk, PlagueBright, 0.42f + homingReadiness * 0.22f) * 0.66f,
                    new Vector2(0.8f - 0.18f * squash, 1.15f + squash),
                    true,
                    false,
                    shrinkSpeed: 0.22f));
            }

            if (Main.rand.NextBool(4))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + side * Main.rand.NextFloat(-7f, 7f),
                    Main.rand.NextVector2Circular(0.7f, 0.7f) - forward * Main.rand.NextFloat(0.2f, 0.75f),
                    "CalamityMod/Particles/SmallBloom",
                    false,
                    Main.rand.Next(7, 12),
                    Main.rand.NextFloat(0.08f, 0.14f) * plagueIntensity,
                    Color.Lerp(PlagueAcid, Color.White, Main.rand.NextFloat(0.04f, 0.18f)),
                    new Vector2(0.9f, 1.2f + squash * 0.6f),
                    true,
                    false,
                    shrinkSpeed: 0.5f));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(2f, 12f) + side * Main.rand.NextFloat(-6f, 6f),
                    Main.rand.NextBool(4) ? DustID.TerraBlade : DustID.GreenTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.035f, 0.095f) + Main.rand.NextVector2Circular(0.26f, 0.26f),
                    40,
                    Color.Lerp(PlagueMurk, PlagueAcid, Main.rand.NextFloat(0.18f, 0.62f)),
                    Main.rand.NextFloat(0.56f, 1.05f) * plagueIntensity);
                dust.noGravity = true;
            }
        }

        private void SpawnHomingAwakenEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Lerp(PlagueMurk, PlagueAcid, 0.42f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Projectile.rotation,
                0.12f,
                0.95f,
                18,
                true));

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center - forward * 6f,
                -forward * 1.2f,
                "CalamityMod/Particles/VerticalSmear",
                false,
                14,
                1.4f,
                Color.Lerp(PlagueGreen, PlagueAcid, 0.5f),
                new Vector2(0.12f, 0.82f)));

            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(3) ? DustID.TerraBlade : DustID.GreenTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.1f, 3.4f) - forward * Main.rand.NextFloat(0.2f, 1.2f),
                    50,
                    Color.Lerp(PlagueMurk, PlagueAcid, Main.rand.NextFloat(0.2f, 0.85f)),
                    Main.rand.NextFloat(0.72f, 1.18f));
                dust.noGravity = true;
            }
        }

        private void SpawnImpactEffects(Vector2 center, float intensity = 1f)
        {
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(PlagueGreen, PlagueAcid, 0.3f),
                0.36f * intensity,
                10));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                Color.Lerp(PlagueMurk, PlagueGreen, 0.42f),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-0.4f, 0.4f),
                0.14f * intensity,
                1.2f * intensity,
                20,
                true));

            for (int i = 0; i < 18; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(4) ? DustID.TerraBlade : DustID.GreenTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.7f, 5.8f) * intensity,
                    40,
                    Color.Lerp(PlagueMurk, PlagueAcid, Main.rand.NextFloat(0.18f, 0.9f)),
                    Main.rand.NextFloat(0.72f, 1.34f) * intensity);
                dust.noGravity = true;
            }
        }

        private void ReleaseImpactGas(Vector2 center, int damage, bool markedTarget)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            int gasCount = markedTarget ? 2 : 1;
            float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
            for (int i = 0; i < gasCount; i++)
            {
                Vector2 velocity = (baseAngle + MathHelper.TwoPi * i / gasCount).ToRotationVector2() * Main.rand.NextFloat(1.1f, 2.4f);
                velocity += Main.rand.NextVector2Circular(0.55f, 0.55f) + new Vector2(0f, Main.rand.NextFloat(-0.6f, -0.15f));
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    damage,
                    0f,
                    Projectile.owner,
                    Main.rand.Next(3),
                    Main.rand.NextFloat(0.76f, markedTarget ? 1.05f : 0.92f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D body = TextureAssets.Projectile[Type].Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float fade = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            float homingReadiness = GetHomingReadiness();
            float bodyPulse = 0.5f + 0.5f * (float)System.Math.Sin((Timer + Seed) * 0.31f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(PlagueMurk, PlagueAcid, completion) with { A = 0 } * (completion * MathHelper.Lerp(0.2f, 0.34f, homingReadiness) * fade);
                Main.EntitySpriteDraw(
                    orb,
                    Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition,
                    null,
                    trailColor,
                    Projectile.rotation,
                    orb.Size() * 0.5f,
                    0.055f + completion * 0.035f,
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                orb,
                center - forward * 8f,
                null,
                (Color.Lerp(PlagueMurk, PlagueGreen, 0.42f) with { A = 0 }) * (0.28f + 0.22f * homingReadiness) * fade,
                Projectile.rotation,
                orb.Size() * 0.5f,
                new Vector2(0.18f + 0.1f * homingReadiness, 0.11f + 0.04f * bodyPulse),
                SpriteEffects.None,
                0);

            for (int i = 0; i < 5; i++)
            {
                Color bodyColor = Color.Lerp(PlagueGreen, Color.Lerp(PlagueAcid, Color.White, 0.2f), i * 0.08f + homingReadiness * 0.18f) with { A = 0 } * (0.34f + bodyPulse * 0.06f) * fade;
                Vector2 scale = new Vector2(0.04f + i * 0.01f, 0.065f + i * 0.013f) * (1f + bodyPulse * 0.12f);
                Main.EntitySpriteDraw(
                    orb,
                    center,
                    null,
                    bodyColor,
                    Projectile.rotation,
                    orb.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0);
            }

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Main.EntitySpriteDraw(
                body,
                center,
                null,
                Color.White * fade,
                Projectile.rotation,
                body.Size() * 0.5f,
                Projectile.scale,
                SpriteEffects.None,
                0);

            return false;
        }
    }
}
