using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    internal sealed class SHPBowArrow : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
        public new string LocalizationCategory => "Projectiles.A_Dev";

        private ref float BounceCount => ref Projectile.localAI[0];
        private float PackedSequence => Projectile.ai[0];
        private bool Charged => Projectile.ai[1] == 1f;
        private int SequenceLength => SHPBowModeHelpers.SequenceLength(PackedSequence);
        private int PierceCount => SHPBowModeHelpers.CountMode(PackedSequence, SHPBowMode.Pierce);
        private int RicochetCount => SHPBowModeHelpers.CountMode(PackedSequence, SHPBowMode.Ricochet);
        private int ScatterCount => SHPBowModeHelpers.CountMode(PackedSequence, SHPBowMode.Scatter);
        private int HomingCount => SHPBowModeHelpers.CountMode(PackedSequence, SHPBowMode.Homing);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.arrow = true;
            Projectile.noDropItem = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 220;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            int length = SequenceLength;
            int pierce = PierceCount;
            int ricochet = RicochetCount;
            int scatter = ScatterCount;
            int homing = HomingCount;

            Projectile.noDropItem = true;
            Projectile.scale = 0.95f + length * 0.04f + (Charged ? 0.22f : 0f);
            Projectile.penetrate = 1 + pierce * 2 + ricochet + scatter / 2 + (Charged ? length : 0);
            Projectile.extraUpdates = 1 + System.Math.Min(2, pierce / 2 + (Charged && pierce > 0 ? 1 : 0));
            Projectile.ArmorPenetration = pierce * 11 + (Charged ? length * 6 : 0);
            Projectile.localNPCHitCooldown = System.Math.Max(6, 13 - pierce - ricochet - (Charged ? 2 : 0));

            if (homing >= 3)
                Projectile.timeLeft += 40;
        }

        public override void AI()
        {
            Color lightColor = SHPBowModeHelpers.SequenceColor(PackedSequence, Projectile.timeLeft % 40 / 39f);
            Lighting.AddLight(Projectile.Center, lightColor.ToVector3() * (Charged ? 0.72f : 0.4f));

            if (HomingCount > 0)
                HomeTowardsTarget();

            if (PierceCount > 0 && Projectile.velocity.LengthSquared() > 1f)
            {
                float speedCap = Charged ? 34f : 28f;
                float acceleration = PierceCount * (Charged ? 0.035f : 0.018f);
                float speed = MathHelper.Clamp(Projectile.velocity.Length() + acceleration, 0f, speedCap);
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * speed;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.Pi;
            EmitTrailDust();
        }

        private void HomeTowardsTarget()
        {
            int homing = HomingCount;
            NPC target = FindTarget(480f + homing * 170f + (Charged ? 220f : 0f));
            if (target is null)
                return;

            float speed = MathHelper.Clamp(Projectile.velocity.Length(), 10f, Charged ? 26f : 20f);
            float responsiveness = 0.035f + homing * 0.045f + (Charged ? 0.035f : 0f);
            Vector2 aimPoint = target.Center + target.velocity * 0.16f;
            Vector2 desiredVelocity = (aimPoint - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, MathHelper.Clamp(responsiveness, 0.02f, 0.28f));
        }

        private NPC FindTarget(float range)
        {
            NPC bestTarget = null;
            float closest = range;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= closest)
                    continue;

                if (!Collision.CanHit(Projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                closest = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int ricochet = RicochetCount;
            if (ricochet <= 0)
                return true;

            int maxBounces = ricochet * (Charged ? 3 : 2);
            BounceCount++;
            if (BounceCount > maxBounces)
                return true;

            float retention = 0.9f + ricochet * 0.05f + (Charged ? 0.08f : 0f);
            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * retention;

            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * retention;

            Projectile.timeLeft = System.Math.Max(Projectile.timeLeft, 80);
            Projectile.netUpdate = true;
            EmitBurst(Charged ? 12 : 7, 0.6f, Charged ? 3.8f : 2.5f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = Charged ? 0.55f : 0.34f, Pitch = 0.18f }, Projectile.Center);
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (RicochetCount > 0)
                RetargetRicochet(target);

            if (ScatterCount > 0)
                target.AddBuff(BuffID.OnFire, Charged ? 240 : 120);

            if (HomingCount > 0)
                target.AddBuff(BuffID.Electrified, Charged ? 180 : 90);

            EmitBurst(Charged ? 14 : 6, 0.7f, Charged ? 4.2f : 2.2f);
        }

        private void RetargetRicochet(NPC hitTarget)
        {
            int ricochet = RicochetCount;
            int maxBounces = ricochet * (Charged ? 3 : 2);
            if (BounceCount >= maxBounces)
                return;

            NPC nextTarget = null;
            float closest = 360f + ricochet * 140f + (Charged ? 180f : 0f);
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (npc == hitTarget || !npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= closest)
                    continue;

                closest = distance;
                nextTarget = npc;
            }

            if (nextTarget is null)
                return;

            BounceCount++;
            float speed = MathHelper.Clamp(Projectile.velocity.Length() + 0.8f + ricochet * 0.45f, 12f, Charged ? 28f : 22f);
            Projectile.velocity = (nextTarget.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.netUpdate = true;
        }

        public override void OnKill(int timeLeft)
        {
            EmitBurst(Charged ? 18 : 8, 0.8f, Charged ? 4.8f : 2.6f);
        }

        private void EmitTrailDust()
        {
            if (!Main.rand.NextBool(Charged ? 1 : 2))
                return;

            int length = SequenceLength;
            SHPBowMode mode = SHPBowModeHelpers.SequenceMode(PackedSequence, Projectile.timeLeft % length);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center - Projectile.velocity * 0.12f + normal * Main.rand.NextFloat(-3f, 3f),
                SHPBowModeHelpers.DustType(mode),
                -Projectile.velocity * 0.04f + Main.rand.NextVector2Circular(0.6f, 0.6f),
                100,
                Color.Lerp(SHPBowModeHelpers.MainColor(mode), SHPBowModeHelpers.AccentColor(mode), Main.rand.NextFloat()),
                Main.rand.NextFloat(0.72f, Charged ? 1.35f : 1.05f));
            dust.noGravity = true;

            if (!Main.dedServ && Main.rand.NextBool(Charged ? 2 : 4))
            {
                Color sparkColor = EnergyColor(Projectile.timeLeft * 0.017f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center - direction * 10f,
                    -Projectile.velocity * 0.05f + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    "CalamityMod/Particles/FadeStreak",
                    false,
                    Charged ? 15 : 11,
                    Charged ? 0.045f : 0.032f,
                    sparkColor,
                    new Vector2(0.7f, Charged ? 1.8f : 1.35f),
                    shrinkSpeed: 0.82f));
            }
        }

        private void EmitBurst(int amount, float minSpeed, float maxSpeed)
        {
            int length = SequenceLength;
            for (int i = 0; i < amount; i++)
            {
                SHPBowMode mode = SHPBowModeHelpers.SequenceMode(PackedSequence, i % length);
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(minSpeed, maxSpeed);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    SHPBowModeHelpers.DustType(mode),
                    velocity,
                    100,
                    Color.Lerp(SHPBowModeHelpers.MainColor(mode), SHPBowModeHelpers.AccentColor(mode), Main.rand.NextFloat(0.18f, 0.75f)),
                Main.rand.NextFloat(0.75f, Charged ? 1.45f : 1.1f));
                dust.noGravity = true;
            }

            if (Main.dedServ)
                return;

            int sparkCount = System.Math.Max(3, amount / 2);
            for (int i = 0; i < sparkCount; i++)
            {
                float angle = MathHelper.TwoPi * i / sparkCount + Main.rand.NextFloat(-0.22f, 0.22f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(minSpeed + 1.2f, maxSpeed + 4.4f);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center,
                    velocity,
                    "CalamityMod/Particles/FadeStreak",
                    false,
                    Charged ? 20 : 13,
                    Main.rand.NextFloat(Charged ? 0.045f : 0.028f, Charged ? 0.07f : 0.046f),
                    EnergyColor(i / (float)sparkCount),
                    new Vector2(0.65f, Charged ? 2.2f : 1.55f),
                    shrinkSpeed: 0.78f));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D streakTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/FadeStreak").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float drawRotation = direction.ToRotation();
            float scale = Projectile.scale * (Charged ? 1.16f : 1f);
            float time = Main.GlobalTimeWrappedHourly + Projectile.identity * 0.017f;

            Main.spriteBatch.SetBlendState(BlendState.Additive);

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                if (completion <= 0f)
                    continue;

                Vector2 trailPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color trailColor = EnergyColor(completion + time * 0.24f) * (0.34f * completion * completion);
                float trailLength = MathHelper.Lerp(0.42f, Charged ? 1.18f : 0.86f, completion) * scale;
                float trailWidth = MathHelper.Lerp(0.11f, Charged ? 0.26f : 0.2f, completion) * scale;

                Main.EntitySpriteDraw(
                    streakTexture,
                    trailPosition - direction * (10f + i * 0.7f),
                    null,
                    MakeAdditive(trailColor),
                    drawRotation,
                    new Vector2(streakTexture.Width * 0.5f, streakTexture.Height * 0.5f),
                    new Vector2(trailLength, trailWidth),
                    SpriteEffects.None,
                    0);

                Main.EntitySpriteDraw(
                    bloomTexture,
                    trailPosition,
                    null,
                    MakeAdditive(trailColor) * 0.55f,
                    0f,
                    bloomTexture.Size() * 0.5f,
                    (0.035f + completion * 0.055f) * scale,
                    SpriteEffects.None,
                    0);
            }

            int starPoints = Charged ? 7 : 5;
            for (int i = 0; i < starPoints; i++)
            {
                float completion = i / (float)starPoints;
                float rayRotation = drawRotation + MathHelper.TwoPi * completion + time * (Charged ? 1.8f : 1.15f);
                Color rayColor = EnergyColor(completion + time * 0.19f) * (Charged ? 0.48f : 0.34f);

                Main.EntitySpriteDraw(
                    streakTexture,
                    drawPosition,
                    null,
                    MakeAdditive(rayColor),
                    rayRotation,
                    new Vector2(streakTexture.Width * 0.5f, streakTexture.Height * 0.5f),
                    new Vector2((Charged ? 0.54f : 0.38f) * scale, (Charged ? 0.14f : 0.095f) * scale),
                    SpriteEffects.None,
                    0);
            }

            Main.EntitySpriteDraw(
                streakTexture,
                drawPosition + direction * (Charged ? 8f : 5f),
                null,
                MakeAdditive(EnergyColor(0.68f + time * 0.24f)) * (Charged ? 0.88f : 0.66f),
                drawRotation,
                new Vector2(streakTexture.Width * 0.5f, streakTexture.Height * 0.5f),
                new Vector2((Charged ? 1.05f : 0.74f) * scale, (Charged ? 0.3f : 0.22f) * scale),
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition,
                null,
                MakeAdditive(EnergyColor(0.12f + time * 0.2f)) * (Charged ? 0.88f : 0.65f),
                0f,
                bloomTexture.Size() * 0.5f,
                (Charged ? 0.16f : 0.105f) * scale,
                SpriteEffects.None,
                0);

            Main.EntitySpriteDraw(
                bloomTexture,
                drawPosition + direction * (Charged ? 4f : 2f),
                null,
                Color.White * (Charged ? 0.92f : 0.78f),
                0f,
                bloomTexture.Size() * 0.5f,
                (Charged ? 0.055f : 0.038f) * scale,
                SpriteEffects.None,
                0);

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private Color EnergyColor(float completion)
        {
            Color sequenceColor = SHPBowModeHelpers.SequenceColor(PackedSequence, completion);
            float hue = completion + Main.GlobalTimeWrappedHourly * (Charged ? 0.42f : 0.28f) + Projectile.identity * 0.013f;
            hue -= (float)System.Math.Floor(hue);
            Color rainbowColor = Main.hslToRgb(hue, 0.96f, Charged ? 0.72f : 0.64f, byte.MaxValue);
            Color color = Color.Lerp(sequenceColor, rainbowColor, Charged ? 0.48f : 0.34f);
            color.A = 0;
            return color;
        }

        private static Color MakeAdditive(Color color)
        {
            color.A = 0;
            return color;
        }
    }
}
