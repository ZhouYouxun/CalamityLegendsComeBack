using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // E 战术右键箭：扎附在目标或地形后，持续朝周围排出毒云。
    internal class BFArrow_EPlague : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/EPlague/BFArrow_EPlague";
        private int gasTimer;
        private int storedGasDamage = 1;
        private Vector2 stickOffset;

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];

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
        }

        public override bool? CanDamage() => State == 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State == 0f ? null : false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague).ToVector3() * 0.42f);

            if (State == 0f)
            {
                Projectile.velocity = Projectile.velocity.RotatedBy(System.Math.Sin(Projectile.identity * 0.41f + Projectile.timeLeft * 0.08f) * 0.0022f);
                Projectile.velocity *= 0.9985f;
                BFArrowCommon.FaceForward(Projectile);
                BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_EPlague, 1.02f);
                EmitPlagueFlightFX();

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
            if (State == 0f)
                BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_EPlague);
            else
                BFArrowCommon.DrawProjectile(Projectile, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value, Projectile.GetAlpha(lightColor), Projectile.rotation, Projectile.scale);

            return false;
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

        private void EmitPlagueFlightFX()
        {
            if (Main.dedServ || Main.rand.NextBool(3))
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague);
            Vector2 smokeVelocity = -Projectile.velocity * 0.03f + Main.rand.NextVector2Circular(0.45f, 0.45f);
            HeavySmokeParticle smoke = new(
                Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                smokeVelocity,
                Color.Lerp(mainColor, Color.Black, 0.12f),
                15,
                Main.rand.NextFloat(0.34f, 0.56f),
                0.52f,
                Main.rand.NextFloat(-0.04f, 0.04f),
                false);
            GeneralParticleHandler.SpawnParticle(smoke);
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
