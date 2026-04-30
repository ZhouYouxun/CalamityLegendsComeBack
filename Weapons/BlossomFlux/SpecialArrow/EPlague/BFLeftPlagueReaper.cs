using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFLeftPlagueReaper : ModProjectile, ILocalizedModType
    {
        private static readonly Color PlagueGreen = new(124, 238, 68);
        private static readonly Color PlagueBright = new(214, 255, 104);
        private static readonly Color PlagueDeep = new(34, 145, 46);

        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Seed => ref Projectile.ai[0];
        private ref float Variant => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];

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
            NPC target = FindTargetAhead(forward);
            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center - Projectile.Center).SafeNormalize(forward) * MathHelper.Clamp(Projectile.velocity.Length() * 1.01f, 10.5f, 17.5f);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.045f);
            }

            float wingBeat = (float)System.Math.Sin((Timer + Seed) * 0.82f);
            float drift = wingBeat * 0.009f + (float)System.Math.Sin((Timer + Seed * 0.31f) * 0.17f) * 0.005f;
            Projectile.velocity = Projectile.velocity.RotatedBy(drift);
            Projectile.velocity *= 1.002f;
            Projectile.rotation = Projectile.velocity.ToRotation();

            Lighting.AddLight(Projectile.Center, PlagueGreen.ToVector3() * 0.48f);

            if (Main.dedServ)
                return;

            EmitFlightEffects(forward);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 120);
            target.AddBuff(ModContent.BuffType<MiracleBlight>(), 240);
            Projectile.damage = System.Math.Max(1, (int)(Projectile.damage * 0.78f));

            if (Main.dedServ)
                return;

            SpawnImpactEffects(target.Center);
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

        private NPC FindTargetAhead(Vector2 forward)
        {
            NPC bestTarget = null;
            float bestDistance = 560f;

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
                if (angle > MathHelper.PiOver2)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
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

        private void EmitFlightEffects(Vector2 forward)
        {
            if (!Projectile.FinalExtraUpdate())
                return;

            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float squash = Utils.GetLerpValue(5f, 15f, Projectile.velocity.Length(), true);

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 12f) + side * Main.rand.NextFloat(-4f, 4f),
                    -Projectile.velocity * 0.018f,
                    "CalamityMod/Particles/DualTrail",
                    false,
                    10,
                    0.052f,
                    Color.Lerp(PlagueDeep, PlagueBright, 0.46f) * 0.58f,
                    new Vector2(0.8f - 0.18f * squash, 1.15f + squash),
                    true,
                    false,
                    shrinkSpeed: 0.22f));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - forward * Main.rand.NextFloat(2f, 12f) + side * Main.rand.NextFloat(-6f, 6f),
                    DustID.GreenTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.035f, 0.095f) + Main.rand.NextVector2Circular(0.26f, 0.26f),
                    0,
                    Color.Lerp(PlagueGreen, PlagueBright, Main.rand.NextFloat(0.1f, 0.45f)),
                    Main.rand.NextFloat(0.52f, 0.86f));
                dust.noGravity = true;
            }
        }

        private void SpawnImpactEffects(Vector2 center, float intensity = 1f)
        {
            GeneralParticleHandler.SpawnParticle(new StrongBloom(
                center,
                Vector2.Zero,
                Color.Lerp(PlagueGreen, Color.White, 0.16f),
                0.26f * intensity,
                8));

            for (int i = 0; i < 10; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(5f, 5f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.5f, 4.8f) * intensity,
                    0,
                    Color.Lerp(PlagueDeep, PlagueBright, Main.rand.NextFloat(0.18f, 0.78f)),
                    Main.rand.NextFloat(0.62f, 1.12f) * intensity);
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D orb = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D wing = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/muzzle_02").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);
            float fade = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            float wingBeat = (float)System.Math.Sin((Timer + Seed) * 1.72f);
            float wingOpen = 0.22f + 0.34f * System.Math.Abs(wingBeat);
            float bodyPulse = 0.5f + 0.5f * (float)System.Math.Sin((Timer + Seed) * 0.31f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.PointClamp, DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Color trailColor = Color.Lerp(PlagueDeep, PlagueBright, completion) with { A = 0 } * (completion * 0.26f * fade);
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

            DrawWing(wing, center, side, 1f, wingOpen, fade);
            DrawWing(wing, center, -side, -1f, wingOpen, fade);

            for (int i = 0; i < 5; i++)
            {
                Color bodyColor = Color.Lerp(PlagueGreen, Color.White, i * 0.08f) with { A = 0 } * (0.34f + bodyPulse * 0.06f) * fade;
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
            return false;
        }

        private void DrawWing(Texture2D wing, Vector2 center, Vector2 wingDirection, float sideSign, float wingOpen, float fade)
        {
            Vector2 origin = new(wing.Width * 0.5f, wing.Height * 0.84f);
            float baseRotation = wingDirection.ToRotation() + MathHelper.PiOver2;
            float flutter = (float)System.Math.Sin((Timer + Seed) * 2.45f + sideSign * 0.8f) * wingOpen;
            Vector2 basePosition = center + wingDirection * 4f - Projectile.velocity.SafeNormalize(Vector2.UnitX) * 1.5f;

            for (int i = 0; i < 3; i++)
            {
                float ghostOffset = i - 1f;
                float opacity = (i == 1 ? 0.34f : 0.13f) * fade;
                Color wingColor = Color.Lerp(PlagueGreen, PlagueBright, 0.32f + 0.18f * i) with { A = 0 } * opacity;
                Main.EntitySpriteDraw(
                    wing,
                    basePosition + wingDirection * ghostOffset * 1.6f,
                    null,
                    wingColor,
                    baseRotation + flutter + ghostOffset * 0.2f,
                    origin,
                    new Vector2(0.072f, 0.105f),
                    SpriteEffects.None,
                    0);
            }
        }
    }
}
