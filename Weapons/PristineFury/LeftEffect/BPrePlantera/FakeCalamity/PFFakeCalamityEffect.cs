using CalamityLegendsComeBack.Weapons.PristineFury;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFFakeCalamityEffect
    {
        private const int BurstCount = 6;
        private const int BurstInterval = 4;
        private const int BurstCooldown = 34;
        private const float DamageMultiplier = 0.34f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            if (holdout.LeftChargeTimer > 0)
            {
                holdout.LeftChargeTimer--;
                return;
            }

            if (holdout.LeftAuxTimer <= 0)
            {
                holdout.LeftAuxTimer = BurstCount;
                holdout.LeftTimer = 0;
            }

            if (holdout.LeftTimer > 0)
            {
                holdout.LeftTimer--;
                return;
            }

            FireScatter(holdout);
            holdout.LeftAuxTimer--;
            holdout.LeftTimer = BurstInterval;

            if (holdout.LeftAuxTimer <= 0)
                holdout.LeftChargeTimer = BurstCooldown;
        }

        private static void FireScatter(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 muzzleDirection = holdout.AimDirection;
            Vector2 muzzle = holdout.GunTipPosition + muzzleDirection * 8f;
            int pelletCount = Main.rand.Next(4, 7);
            int damage = holdout.GetScaledDamage(DamageMultiplier);
            float knockBack = holdout.Projectile.knockBack * 0.7f;
            float speed = 13.5f;

            for (int i = 0; i < pelletCount; i++)
            {
                Vector2 velocity = muzzleDirection.RotatedBy(MathHelper.ToRadians(Main.rand.NextFloat(-11f, 11f))) * speed * Main.rand.NextFloat(0.86f, 1.18f);
                int projectileIndex = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle,
                    velocity,
                    ModContent.ProjectileType<PFFakeCalamity_Pellet>(),
                    damage,
                    knockBack,
                    holdout.Projectile.owner,
                    0f,
                    holdout.LeftBurstIndex++);

                PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
            }

            holdout.ApplyRecoil(4f);
            holdout.TriggerMuzzleFlash(12);
            holdout.SpawnMuzzleBurst(new Color(255, 74, 48), 0.78f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/FlakKrakenShoot") { Volume = 0.5f, Pitch = 0.45f }, muzzle);
        }
    }

    internal sealed class PFFakeCalamity_Pellet : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/RightAndHook/PristineFuryRightPellet";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 1;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 15;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 110;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.extraUpdates = 2;
        }

        public override void AI()
        {
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 66, 54));
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 0.992f;
            Lighting.AddLight(Projectile.Center, Color.Lerp(theme, Color.Gold, 0.24f).ToVector3() * 0.55f);

            if (Main.rand.NextBool())
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center - direction * 6f + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Torch,
                    -direction.RotatedByRandom(0.36f) * Main.rand.NextFloat(0.35f, 1.35f),
                    120,
                    Color.Lerp(theme, Color.Gold, Main.rand.NextFloat(0.2f, 0.65f)),
                    Main.rand.NextFloat(0.65f, 1.05f));
                ember.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            int flame = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PFFakeCalamity_GroundFlame>(),
                Projectile.damage,
                0f,
                Projectile.owner,
                1f);

            PFLeftEffectRules.ApplyTheme(flame, (PristineFuryMark)(int)Projectile.ai[2]);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 180);
        }
    }

    internal sealed class PFFakeCalamity_GroundFlame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 84;
            Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            float scale = Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
            Projectile.width = (int)(84f * scale);
            Projectile.height = (int)(40f * scale);
            Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 66, 54));
            Lighting.AddLight(Projectile.Center, Color.Lerp(theme, Color.Gold, 0.26f).ToVector3() * scale);

            if (Main.dedServ)
                return;

            Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-Projectile.width * 0.45f, Projectile.width * 0.45f), Main.rand.NextFloat(-8f, 6f));
            Vector2 velocity = new(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-2.6f, -0.8f));
            Particle flame = new MediumMistParticle(
                position,
                velocity,
                Color.Lerp(theme, Color.Gold, Main.rand.NextFloat(0.2f, 0.55f)),
                Color.Black,
                Main.rand.NextFloat(0.45f, 0.9f) * scale,
                Main.rand.Next(24, 42),
                Main.rand.NextFloat(-0.08f, 0.08f));
            GeneralParticleHandler.SpawnParticle(flame);

            if (Main.rand.NextBool(3))
            {
                Particle ember = new SparkParticle(
                    position + Main.rand.NextVector2Circular(8f, 4f),
                    velocity.RotatedByRandom(0.45f) * Main.rand.NextFloat(1.2f, 2.8f),
                    true,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.55f, 0.9f) * scale,
                    Color.Lerp(theme, Color.Orange, 0.3f));
                GeneralParticleHandler.SpawnParticle(ember);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.OnFire3, 240);
            target.AddBuff(ModContent.BuffType<BrimstoneFlames>(), 180);
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
