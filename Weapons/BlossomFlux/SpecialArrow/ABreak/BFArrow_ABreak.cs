using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    internal class BFArrow_ABreak : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/ABreak/BFArrow_ABreak";
        // A 战术右键箭：先弹射开路，再在飞行后段自动追猎。
        private ref float BounceCounter => ref Projectile.ai[0];
        private ref float FlightTimer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 240, penetrate: 2, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            FlightTimer++;
            BFArrowCommon.FaceForward(Projectile);
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak).ToVector3() * 0.45f);
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 1.05f);
            EmitBreakthroughFlightFX();

            if (FlightTimer >= 18f)
            {
                NPC target = Projectile.Center.ClosestNPCAt(1020f);
                if (target != null)
                {
                    BFArrowCommon.DirectHomeTowards(Projectile, target, 0.22f, 21f);
                    BFArrowCommon.MaintainSpeed(Projectile, 21f, 0.14f);
                }
            }
            else
            {
                Projectile.velocity *= 1.006f;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.35f, Pitch = 0.2f }, Projectile.Center);
            if (BFArrowCommon.Bounce(Projectile, oldVelocity, ref BounceCounter, 3, 0.98f))
                return true;

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 10, 0.9f, 2.8f, 0.8f, 1.25f);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 14, 1.2f, 4.8f, 1f, 1.35f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_ABreak, 8, 0.9f, 2.6f, 0.8f, 1.15f);
            SpawnBreakthroughImpactFX(target.Center, 1.1f);
            SoundEngine.PlaySound(SoundID.Item17 with { Volume = 0.22f, Pitch = 0.25f }, target.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_ABreak);
            return false;
        }

        private void EmitBreakthroughFlightFX()
        {
            if (Main.dedServ || (int)FlightTimer % 4 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
            DirectionalPulseRing pulse = new(
                Projectile.Center + direction * 8f,
                Projectile.velocity * 0.05f,
                Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), Color.White, 0.2f),
                new Vector2(0.72f, 1.85f),
                direction.ToRotation(),
                0.14f,
                0.04f,
                10);
            GeneralParticleHandler.SpawnParticle(pulse);

            BloomLineVFX line = new(
                Projectile.Center - direction * 18f,
                direction * 36f,
                0.95f,
                Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak), Color.White, 0.35f),
                10);
            GeneralParticleHandler.SpawnParticle(line);

            GenericSparkle edgeSpark = new(
                Projectile.Center + normal * Main.rand.NextFloat(-5f, 5f),
                Projectile.velocity * 0.04f,
                Color.White,
                BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak),
                0.95f,
                7,
                0f,
                1.2f);
            GeneralParticleHandler.SpawnParticle(edgeSpark);
        }

        private void SpawnBreakthroughImpactFX(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Color accentColor = BFArrowCommon.GetPresetAccentColor(BlossomFluxChloroplastPresetType.Chlo_ABreak);
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 2; i++)
            {
                BloomLineVFX line = new(
                    center - direction * 20f + normal * (i == 0 ? -6f : 6f),
                    direction * 40f,
                    0.95f * intensity,
                    i == 0 ? mainColor : accentColor,
                    12);
                GeneralParticleHandler.SpawnParticle(line);
            }
        }
    }
}
