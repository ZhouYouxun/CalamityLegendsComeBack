using System;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class TwistingNether_Blade : ModProjectile, ILocalizedModType
    {
        private static readonly Color BladePurple = new(145, 90, 220);
        private static readonly Color BladeDark = new(60, 12, 95);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float TargetIndex => ref Projectile.ai[0];
        public ref float State => ref Projectile.ai[1];
        public ref float SpawnOrder => ref Projectile.ai[2];

        public ref float Timer => ref Projectile.localAI[0];
        public ref float HelixAngle => ref Projectile.localAI[1];

        private const int HiddenState = 0;
        private const int DiveState = 1;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 240;
            Projectile.extraUpdates = 3;
            Projectile.Opacity = 0f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override bool? CanCutTiles() => false;

        public override void AI()
        {
            bool firstSubstep = Projectile.numUpdates == 0;
            if (firstSubstep)
                Timer++;

            if (State == HiddenState)
                DoHiddenState(firstSubstep);
            else
                DoDiveState(firstSubstep);
        }

        private void DoHiddenState(bool firstSubstep)
        {
            Projectile.velocity *= 0.94f;
            Projectile.Opacity = 0f;

            if (!Main.dedServ && firstSubstep)
            {
                float chargePulse = 0.5f + 0.5f * (float)Math.Sin(Timer * 0.22f);
                Vector2 ringOffset = (Timer * 0.24f).ToRotationVector2() * (8f + chargePulse * 10f);

                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    Projectile.Center,
                    Vector2.Zero,
                    Color.Lerp(BladeDark, BladePurple, chargePulse) * 0.45f,
                    "CalamityMod/Particles/LargeBloom",
                    new Vector2(0.8f, 1.4f),
                    Main.rand.NextFloat(-0.15f, 0.15f),
                    0.18f + chargePulse * 0.08f,
                    0f,
                    8,
                    false));

                Dust warningDust = Dust.NewDustPerfect(
                    Projectile.Center + ringOffset,
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    (-ringOffset).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.3f, 1.2f),
                    0,
                    Color.Lerp(BladePurple, Color.White, 0.2f),
                    Main.rand.NextFloat(0.9f, 1.3f));
                warningDust.noGravity = true;
            }

            int delay = 18 + ((int)SpawnOrder % 3) * 10;
            if (Timer < delay)
                return;

            NPC target = FindNearestTarget(1500f);
            Vector2 destination = target?.Center ?? (Projectile.Center + Vector2.UnitY * 400f);
            Vector2 desiredVelocity = (destination - Projectile.Center).SafeNormalize(Vector2.UnitY) * 24f;

            Projectile.velocity = desiredVelocity;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = 1f;
            Timer = 0f;
            State = DiveState;

            SoundEngine.PlaySound(SoundID.Item104 with { Pitch = -0.25f, Volume = 0.55f }, Projectile.Center);
        }

        private void DoDiveState(bool firstSubstep)
        {
            NPC target = FindNearestTarget(1500f);
            if (target != null)
            {
                Vector2 desiredVelocity = (target.Center + target.velocity * 8f - Projectile.Center).SafeNormalize(Vector2.UnitY) * 26f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.07f);
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = MathHelper.Lerp(Projectile.Opacity, 1f, 0.24f);

            if (!Main.dedServ)
                SpawnDiveEffects(firstSubstep);
        }

        private void SpawnDiveEffects(bool firstSubstep)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
            HelixAngle += 0.28f;

            float spiralOffsetAmount = 12f + (float)Math.Sin(HelixAngle) * 6f;
            Vector2 spiralOffset = side * spiralOffsetAmount;

            GeneralParticleHandler.SpawnParticle(new CustomSpark(
                Projectile.Center + spiralOffset,
                -Projectile.velocity * 0.28f + side * 0.4f,
                "CalamityMod/Particles/BloomCircle",
                false,
                10,
                0.4f,
                Color.Lerp(BladePurple, Color.White, 0.15f),
                new Vector2(0.35f, 0.9f),
                true,
                false,
                Main.rand.NextFloat(-0.1f, 0.1f),
                false,
                false,
                0.94f));

            if (firstSubstep)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + spiralOffset,
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                    -Projectile.velocity * 0.08f + side * Main.rand.NextFloat(0.2f, 0.6f),
                    0,
                    Color.Lerp(BladePurple, BladeDark, Main.rand.NextFloat(0.1f, 0.55f)),
                    Main.rand.NextFloat(1f, 1.4f));
                dust.noGravity = true;

                if (Main.rand.NextBool(2))
                {
                    GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                        Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                        (-direction).RotatedByRandom(0.45f) * Main.rand.NextFloat(1.2f, 3.6f),
                        false,
                        Main.rand.Next(10, 16),
                        Main.rand.NextFloat(0.65f, 1f),
                        Color.Lerp(BladePurple, BladeDark, Main.rand.NextFloat(0.2f, 0.7f))));
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Projectile.owner == Main.myPlayer)
            {
                Vector2 slashVelocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 8f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    slashVelocity,
                    ModContent.ProjectileType<TwistingNether_BlackSLASH>(),
                    (int)(Projectile.damage * 1.1f),
                    Projectile.knockBack,
                    Projectile.owner);
            }

            if (!Main.dedServ)
            {
                for (int i = 0; i < 2; i++)
                {
                    GeneralParticleHandler.SpawnParticle(new CustomPulse(
                        target.Center,
                        Vector2.Zero,
                        i == 0 ? BladePurple : Color.White * 0.45f,
                        "CalamityMod/Particles/BloomCircle",
                        Vector2.One,
                        Main.rand.NextFloat(-0.2f, 0.2f),
                        0.45f - i * 0.12f,
                        0f,
                        12,
                        true));
                }

                for (int i = 0; i < 14; i++)
                {
                    Dust dust = Dust.NewDustPerfect(
                        target.Center,
                        Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.8f, 6.4f),
                        0,
                        Color.Lerp(BladePurple, BladeDark, Main.rand.NextFloat()),
                        Main.rand.NextFloat(1f, 1.45f));
                    dust.noGravity = true;
                }
            }
        }

        private NPC FindNearestTarget(float maxDistance)
        {
            if (Main.npc.IndexInRange((int)TargetIndex))
            {
                NPC cachedTarget = Main.npc[(int)TargetIndex];
                if (cachedTarget.active && cachedTarget.CanBeChasedBy())
                    return cachedTarget;
            }

            NPC nearestTarget = null;
            float nearestDistance = maxDistance;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.CanBeChasedBy())
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = npc;
                }
            }

            TargetIndex = nearestTarget?.whoAmI ?? -1;
            return nearestTarget;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (State == HiddenState || Main.dedServ)
                return false;

            Asset<Texture2D> smearTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearRagged");
            Asset<Texture2D> bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            for (int i = 0; i < 5; i++)
            {
                Vector2 afterimageOffset = -Projectile.velocity.SafeNormalize(Vector2.UnitY) * i * 10f;

                Color drawColor = Color.Lerp(BladePurple, Color.White, i / 5f);
                drawColor.A = 0; // ❗防止黑底混色

                Main.EntitySpriteDraw(
                    smearTexture.Value,
                    drawPosition + afterimageOffset,
                    null,
                    drawColor * (0.55f - i * 0.07f) * Projectile.Opacity,
                    Projectile.rotation,
                    smearTexture.Size() * 0.5f,
                    new Vector2(0.22f + i * 0.025f, 1f + i * 0.08f),
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                bloomTexture.Value,
                drawPosition,
                null,
                Color.White * 0.18f * Projectile.Opacity,
                0f,
                bloomTexture.Size() * 0.5f,
                0.18f,
                SpriteEffects.None);

            return false;
        }
    }
}
