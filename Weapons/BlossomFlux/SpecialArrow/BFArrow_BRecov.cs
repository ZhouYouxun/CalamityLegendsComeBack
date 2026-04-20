using CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // B 战术右键箭：先输出，再折返寻找血线最低的队友完成治疗。
    internal class BFArrow_BRecov : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        private int hitCounter;
        private int storedHeal = 3;

        private ref float State => ref Projectile.ai[0];
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 210, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => State == 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State == 0f ? null : false;

        public override void AI()
        {
            Timer++;
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov).ToVector3() * 0.42f);

            if (State == 0f)
            {
                BFArrowCommon.FaceForward(Projectile);
                Projectile.velocity *= 0.998f;
                BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 1.08f);

                if (Timer >= 34f || Projectile.timeLeft < 120)
                    BeginReturn();

                return;
            }

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Player healTarget = BFArrowCommon.FindLowestHealthPlayer(owner);
            Vector2 desiredVelocity = (healTarget.Center - Projectile.Center).SafeNormalize(Vector2.UnitY) * 19.5f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.16f);
            BFArrowCommon.FaceForward(Projectile);
            BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 0.92f);

            if (Projectile.Hitbox.Intersects(healTarget.Hitbox))
            {
                int healAmount = Utils.Clamp(storedHeal + hitCounter * 2, 3, 12);
                healTarget.statLife += healAmount;
                if (healTarget.statLife > healTarget.statLifeMax2)
                    healTarget.statLife = healTarget.statLifeMax2;

                healTarget.HealEffect(healAmount, true);
                SpawnRecoveryPulse(healTarget.Center, 1.25f);
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.55f, Pitch = 0.25f }, healTarget.Center);
                Projectile.Kill();
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            hitCounter++;
            storedHeal = Utils.Clamp(storedHeal + 2 + damageDone / 50, 3, 10);

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 10, 0.9f, 2.8f, 0.9f, 1.25f);

            BeginReturn();
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            BeginReturn();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 10, 0.9f, 2.6f, 0.9f, 1.35f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            BFArrowCommon.DrawPresetArrow(Projectile, lightColor, BlossomFluxChloroplastPresetType.Chlo_BRecov);
            return false;
        }

        private void BeginReturn()
        {
            if (State != 0f)
                return;

            State = 1f;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.timeLeft = Utils.Clamp(Projectile.timeLeft, 120, 210);
            Projectile.netUpdate = true;
            SpawnRecoveryPulse(Projectile.Center, 1f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.35f, Pitch = 0.25f }, Projectile.Center);
        }

        private void SpawnRecoveryPulse(Vector2 center, float intensity)
        {
            if (Main.dedServ)
                return;

            Color mainColor = BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov);
            DirectionalPulseRing pulse = new(
                center,
                Vector2.Zero,
                Color.Lerp(mainColor, Color.White, 0.28f),
                Vector2.One,
                0f,
                0.18f * intensity,
                0.038f,
                16);
            GeneralParticleHandler.SpawnParticle(pulse);

            GenericSparkle sparkle = new(
                center,
                Vector2.Zero,
                Color.White,
                Color.Lerp(mainColor, Color.White, 0.35f),
                1.15f * intensity,
                8,
                0f,
                1.3f);
            GeneralParticleHandler.SpawnParticle(sparkle);
        }
    }
}
