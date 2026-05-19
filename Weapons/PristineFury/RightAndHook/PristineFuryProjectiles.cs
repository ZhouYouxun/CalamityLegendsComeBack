using CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury
{
    internal sealed class PristineFuryRightPellet : ModProjectile, ILocalizedModType
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
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 0.992f;
            Lighting.AddLight(Projectile.Center, new Vector3(0.72f, 0.22f, 0.08f));
            if (Main.rand.NextBool())
            {
                Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust ember = Dust.NewDustPerfect(
                    Projectile.Center - direction * 6f + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Torch,
                    -direction.RotatedByRandom(0.36f) * Main.rand.NextFloat(0.35f, 1.35f),
                    120,
                    Color.Lerp(Color.OrangeRed, Color.Gold, Main.rand.NextFloat(0.2f, 0.65f)),
                    Main.rand.NextFloat(0.65f, 1.05f));
                ember.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero, ModContent.ProjectileType<PristineFuryGroundFlame>(), Projectile.damage, 0f, Projectile.owner, 1f);
            return true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 240);
    }

    internal sealed class PristineFuryGroundFlame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 80;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 180;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            float scale = Projectile.ai[0] <= 0f ? 1f : Projectile.ai[0];
            Projectile.width = (int)(80f * scale);
            Projectile.height = (int)(36f * scale);
            Lighting.AddLight(Projectile.Center, new Vector3(0.9f, 0.42f, 0.05f) * scale);
            if (!Main.dedServ)
            {
                Vector2 position = Projectile.Bottom + new Vector2(Main.rand.NextFloat(-Projectile.width * 0.45f, Projectile.width * 0.45f), Main.rand.NextFloat(-8f, 6f));
                Vector2 velocity = new(Main.rand.NextFloat(-0.7f, 0.7f), Main.rand.NextFloat(-2.6f, -0.8f));
                Particle flame = new MediumMistParticle(
                    position,
                    velocity,
                    Color.Lerp(Color.OrangeRed, Color.Gold, Main.rand.NextFloat(0.2f, 0.55f)),
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
                        Color.Orange);
                    GeneralParticleHandler.SpawnParticle(ember);
                }
            }
        }
    }

    internal sealed class PristineFuryImpactExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;
            Projectile.localAI[0] = 1f;
            int radius = (int)MathHelper.Clamp(Projectile.ai[0] <= 0f ? 55f : Projectile.ai[0], 30f, 240f);
            Projectile.Resize(radius, radius);
            Projectile.Damage();
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                Color.OrangeRed * 0.75f,
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.08f,
                radius / 120f,
                18));

            GeneralParticleHandler.SpawnParticle(new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                Color.Gold,
                "CalamityMod/Particles/SoftRoundExplosion",
                Vector2.One,
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.05f,
                radius / 95f,
                16,
                false));

            for (int i = 0; i < 18; i++)
            {
                Particle spark = new SparkParticle(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f),
                    false,
                    Main.rand.Next(12, 22),
                    Main.rand.NextFloat(0.55f, 1.15f),
                    Color.Lerp(Color.Orange, Color.White, Main.rand.NextFloat(0.1f, 0.35f)));
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }

    internal sealed class PristineFuryGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;
        internal int PlagueRelease;

        public override void ResetEffects(NPC npc)
        {
            if (PlagueRelease > 0)
                PlagueRelease--;
        }

        public override void OnKill(NPC npc)
        {
            if (PlagueRelease <= 0)
                return;
            Player owner = Main.LocalPlayer;
            for (int i = 0; i < 5; i++)
                Projectile.NewProjectile(npc.GetSource_Death(), npc.Center, Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 8f), ModContent.ProjectileType<PFGoliath_Flame>(), 30, 0f, owner.whoAmI, 1f, i);
        }
    }
}
