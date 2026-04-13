using System;
using CalamityMod.Graphics.Metaballs;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog.SZPC
{
    public class TwistingNether_Shadow : ModProjectile, ILocalizedModType
    {
        private static readonly Color VoidPurple = new(120, 50, 180);
        private static readonly Color DarkPurple = new(55, 15, 85);
        private static readonly Color DeepBlack = new(12, 4, 20);

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public ref float TargetIndex => ref Projectile.ai[0];
        public ref float BaseTimer => ref Projectile.ai[1];
        public ref float OrbitAngle => ref Projectile.ai[2];

        public ref float OrbitRadius => ref Projectile.localAI[0];
        public ref float BladeCounter => ref Projectile.localAI[1];

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 15;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            bool firstSubstep = Projectile.numUpdates == 0;

            if (firstSubstep)
                BaseTimer++;

            NPC target = ResolveTarget();
            if (target is null)
            {
                DoIdleFlight(firstSubstep);
                return;
            }

            if (OrbitRadius <= 0f)
            {
                OrbitRadius = MathHelper.Clamp(Vector2.Distance(Projectile.Center, target.Center), 90f, 260f);
                OrbitAngle = (Projectile.Center - target.Center).ToRotation();
            }

            OrbitAngle += 0.028f;
            OrbitRadius = MathHelper.Lerp(OrbitRadius, 34f, 0.0035f);

            Vector2 desiredCenter = target.Center + OrbitAngle.ToRotationVector2() * OrbitRadius;
            Vector2 oldCenter = Projectile.Center;
            Projectile.Center = Vector2.Lerp(Projectile.Center, desiredCenter, 0.3f);
            Projectile.velocity = Projectile.Center - oldCenter;

            SpawnShadowEffects(target, firstSubstep);

            if (firstSubstep)
            {
                BladeCounter++;
                if (BladeCounter >= 10f && Projectile.owner == Main.myPlayer)
                {
                    BladeCounter = 0f;
                    Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        Projectile.Center,
                        Vector2.Zero,
                        ModContent.ProjectileType<TwistingNether_Blade>(),
                        (int)(Projectile.damage * 0.9f),
                        Projectile.knockBack,
                        Projectile.owner,
                        target.whoAmI,
                        0f,
                        BaseTimer
                    );
                }
            }

            Lighting.AddLight(Projectile.Center, Color.Lerp(VoidPurple, DarkPurple, 0.45f).ToVector3() * 0.5f);
        }

        private NPC ResolveTarget()
        {
            if (Main.npc.IndexInRange((int)TargetIndex))
            {
                NPC cachedTarget = Main.npc[(int)TargetIndex];
                if (cachedTarget.active && cachedTarget.CanBeChasedBy())
                    return cachedTarget;
            }

            NPC nearestTarget = null;
            float nearestDistance = 1600f;

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

        private void DoIdleFlight(bool firstSubstep)
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.velocity *= 1.01f;

            if (firstSubstep && BaseTimer > 24f)
                Projectile.Kill();

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 offset = Main.rand.NextVector2Circular(10f, 10f);
                SpawnBloom(Projectile.Center + offset, Main.rand.NextBool() ? VoidPurple : DarkPurple, 0.26f, 10);
            }
        }

        private void SpawnShadowEffects(NPC target, bool firstSubstep)
        {
            Vector2 movement = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = movement.RotatedBy(MathHelper.PiOver2);
            Projectile.rotation = movement.ToRotation();

            float pulse = 0.5f + 0.5f * (float)Math.Sin(BaseTimer * 0.17f);
            float metaScale = MathHelper.Lerp(24f, 46f, pulse);

            StreamGougeMetaball.SpawnParticle(Projectile.Center + side * (12f + pulse * 7f), Vector2.Zero, metaScale);
            StreamGougeMetaball.SpawnParticle(Projectile.Center - side * (12f + pulse * 7f), Vector2.Zero, metaScale * 0.9f);

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Vector2 smokeVelocity = (-movement).RotatedByRandom(0.7f) * Main.rand.NextFloat(0.2f, 1.4f);
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    smokeVelocity,
                    Main.rand.NextBool(2) ? DeepBlack : DarkPurple,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.5f, 0.8f),
                    0.45f,
                    Main.rand.NextFloat(-0.05f, 0.05f),
                    false));
            }

            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new AltSparkParticle(
                    Projectile.Center + Main.rand.NextVector2Circular(6f, 6f),
                    (-movement).RotatedByRandom(0.8f) * Main.rand.NextFloat(0.8f, 3.8f),
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.5f, 1f),
                    Color.Lerp(VoidPurple, DeepBlack, Main.rand.NextFloat(0.25f, 0.8f))));
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    side * Main.rand.NextFloat(-1.8f, 1.8f) - movement * Main.rand.NextFloat(0.4f, 1.4f),
                    "CalamityMod/Particles/ProvidenceMarkParticle",
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.55f, 0.85f),
                    Color.Lerp(VoidPurple, Color.Black, Main.rand.NextFloat(0.35f, 0.75f)),
                    new Vector2(Main.rand.NextFloat(0.8f, 1.15f), Main.rand.NextFloat(0.2f, 0.45f)),
                    true,
                    false,
                    Main.rand.NextFloat(-0.06f, 0.06f),
                    false,
                    false,
                    0.08f));
            }

            if (firstSubstep)
            {
                for (int i = 0; i < 2; i++)
                {
                    float phase = BaseTimer * 0.23f + i * 1.3f;
                    Vector2 helixOffset = side * (float)Math.Sin(phase) * 14f;
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center + helixOffset,
                        i == 0 ? DustID.Shadowflame : DustID.PurpleTorch,
                        -movement * Main.rand.NextFloat(0.4f, 1.2f) + helixOffset.SafeNormalize(Vector2.UnitY) * 0.5f,
                        0,
                        Color.Lerp(VoidPurple, DarkPurple, Main.rand.NextFloat()),
                        Main.rand.NextFloat(1f, 1.4f));
                    dust.noGravity = true;
                }

                if (!Main.dedServ && Main.rand.NextBool(3))
                {
                    Vector2 offset = Main.rand.NextVector2Circular(16f, 16f);
                    SpawnBloom(Projectile.Center + offset * 0.2f, Color.Lerp(VoidPurple, DeepBlack, 0.5f), 0.34f, 12);
                }
            }

            if (BaseTimer > 150f || OrbitRadius <= 38f)
            {
                SpawnArrivalBurst(target.Center);
                Projectile.Kill();
            }
        }

        private void SpawnArrivalBurst(Vector2 center)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 3; i++)
                SpawnBloom(center, i == 0 ? VoidPurple : DarkPurple, 0.48f - i * 0.1f, 18 - i * 3);

            for (int i = 0; i < 16; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 5.6f);
                Dust dust = Dust.NewDustPerfect(center, Main.rand.NextBool() ? DustID.Shadowflame : DustID.PurpleTorch, velocity, 0, Color.Lerp(VoidPurple, Color.Black, 0.3f), Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }
        }

        private static void SpawnBloom(Vector2 center, Color color, float scale, int lifetime)
        {
            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                center,
                Vector2.Zero,
                color,
                "CalamityMod/Particles/LargeBloom",
                Vector2.One,
                Main.rand.NextFloat(-0.15f, 0.15f),
                scale,
                0f,
                lifetime,
                false));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Asset<Texture2D> bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
            Asset<Texture2D> smearTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearRagged");
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float pulse = 0.85f + 0.15f * (float)Math.Sin(BaseTimer * 0.22f);

            Main.EntitySpriteDraw(
                smearTexture.Value,
                drawPosition,
                null,
                Color.Lerp(VoidPurple, DarkPurple, 0.4f) * 0.45f,
                Projectile.rotation + MathHelper.PiOver2,
                smearTexture.Size() * 0.5f,
                new Vector2(0.36f, 1.2f) * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloomTexture.Value,
                drawPosition,
                null,
                Color.Lerp(VoidPurple, DeepBlack, 0.25f) * 0.55f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.46f * pulse,
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                bloomTexture.Value,
                drawPosition,
                null,
                Color.White * 0.18f,
                0f,
                bloomTexture.Size() * 0.5f,
                0.18f * pulse,
                SpriteEffects.None);

            return false;
        }
    }
}
