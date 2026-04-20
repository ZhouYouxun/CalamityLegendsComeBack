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
        private int gasTimer;
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
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            stickOffset = Projectile.Center - target.Center;
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

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    new Color(172, 228, 92),
                    Main.rand.NextFloat(0.8f, 1.15f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Main.rand.NextBool(6))
            {
                HeavySmokeParticle smoke = new(
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(0.35f, 0.35f) + new Vector2(0f, -0.12f),
                    Color.Lerp(BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_EPlague), Color.White, 0.12f),
                    14,
                    Main.rand.NextFloat(0.38f, 0.52f),
                    0.52f,
                    Main.rand.NextFloat(-0.04f, 0.04f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }

            if (gasTimer % 14 != 0 || Projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < 2; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(1.4f, 1.4f) + new Vector2(0f, -0.5f),
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    (int)(Projectile.damage * 0.35f),
                    0f,
                    Projectile.owner,
                    Main.rand.Next(3),
                    Main.rand.NextFloat(0.9f, 1.2f));
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
        }
    }
}
