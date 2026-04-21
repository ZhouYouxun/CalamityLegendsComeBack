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
        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/SpecialArrow/BRecov/BFArrow_BRecov";
        private int hitCounter;
        private int storedHeal = 3;

        private ref float State => ref Projectile.ai[0];
        private ref float FlightTimer => ref Projectile.localAI[0];
        private ref float StateTimer => ref Projectile.localAI[1];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];

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
            Lighting.AddLight(Projectile.Center, BFArrowCommon.GetPresetColor(BlossomFluxChloroplastPresetType.Chlo_BRecov).ToVector3() * 0.42f);

            if (State == 0f)
            {
                FlightTimer++;
                BFArrowCommon.FaceForward(Projectile);
                Projectile.velocity *= 0.998f;
                BFArrowCommon.EmitPresetTrail(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 1.08f);

                if (FlightTimer >= 34f || Projectile.timeLeft < 120)
                    Projectile.Kill();

                return;
            }

            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.velocity = Vector2.Zero;
            StateTimer++;

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

            Projectile.Center = attachedNpc.Center;
            Projectile.gfxOffY = attachedNpc.gfxOffY;

            if (StateTimer >= 20f)
                Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            hitCounter++;
            storedHeal = Utils.Clamp(storedHeal + 2 + damageDone / 50, 3, 10);

            BFArrowCommon.EmitPresetBurst(Projectile, BlossomFluxChloroplastPresetType.Chlo_BRecov, 10, 0.9f, 2.8f, 0.9f, 1.25f);
            BeginTransfer(target);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.22f, Pitch = 0.18f }, Projectile.Center);
            return true;
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

        private void BeginTransfer(NPC target)
        {
            if (State != 0f)
                return;

            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            State = 1f;
            AttachedNpcIndex = target.whoAmI;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.damage = 0;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 24;
            Projectile.netUpdate = true;

            int healAmount = Utils.Clamp(storedHeal + hitCounter * 2, 3, 12);
            SpawnRecoveryPulse(target.Center, 1.15f);
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.38f, Pitch = 0.32f }, target.Center);

            if (Projectile.owner == Main.myPlayer)
            {
                Player healTarget = BFArrowCommon.FindLowestHealthPlayer(owner);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<BFArrow_BRecovTransfer>(),
                    0,
                    0f,
                    Projectile.owner,
                    healTarget.whoAmI,
                    healAmount);
            }
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
