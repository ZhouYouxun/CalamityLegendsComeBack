using CalamityLegendsComeBack.Weapons.SHPC;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord.MoonEvent
{
    internal class FragmentEntropy_CosmicFire : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color EntropyCyan = new(58, 255, 214);
        private static readonly Color EntropyViolet = new(146, 76, 255);
        private static readonly Color EntropyAsh = new(18, 22, 22);

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 22;
            Projectile.height = 22;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 118;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 8;
        }

        public override void AI()
        {
            Timer++;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 side = forward.RotatedBy(MathHelper.PiOver2);

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;
            Projectile.velocity *= 1.002f;

            float wave = (float)System.Math.Sin(Timer * 0.19f + Projectile.identity * 0.4f);
            if (Timer < 26f)
                Projectile.position += side * wave * 0.22f;

            Lighting.AddLight(Projectile.Center, Color.Lerp(EntropyCyan, EntropyViolet, 0.45f).ToVector3() * 0.42f);

            if (Main.rand.NextBool(2))
            {
                Particle spark = new SparkParticle(
                    Projectile.Center - forward * Main.rand.NextFloat(4f, 14f) + side * Main.rand.NextFloat(-5f, 5f),
                    -forward * Main.rand.NextFloat(0.6f, 2.4f) + side * Main.rand.NextFloat(-0.7f, 0.7f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.48f, 0.9f),
                    Color.Lerp(EntropyCyan, EntropyViolet, Main.rand.NextFloat(0.2f, 0.8f)));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.BlueTorch,
                    -forward * Main.rand.NextFloat(0.7f, 2.2f) + side * Main.rand.NextFloat(-0.45f, 0.45f),
                    120,
                    Color.Lerp(EntropyAsh, EntropyCyan, Main.rand.NextFloat(0.2f, 0.8f)),
                    Main.rand.NextFloat(0.72f, 1.15f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Projectile.Kill();
        }

        public override void OnKill(int timeLeft)
        {
            SpawnEntropyCollapse(Projectile.Center);

            if (Projectile.owner != Main.myPlayer)
                return;

            int explosionIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner);

            if (Main.projectile.IndexInRange(explosionIndex))
            {
                Projectile explosion = Main.projectile[explosionIndex];
                explosion.width = 170;
                explosion.height = 170;
                explosion.Center = Projectile.Center;
                explosion.netUpdate = true;
            }
        }

        private static void SpawnEntropyCollapse(Vector2 center)
        {
            Particle core = new CustomPulse(
                center,
                Vector2.Zero,
                Color.Black,
                "CalamityMod/Particles/SmallBloom",
                Vector2.One,
                Main.rand.NextFloat(-0.2f, 0.2f),
                0.34f,
                0f,
                20,
                false);
            GeneralParticleHandler.SpawnParticle(core);

            Particle ring = new DirectionalPulseRing(
                center,
                Vector2.Zero,
                Color.Lerp(EntropyCyan, EntropyViolet, 0.48f),
                new Vector2(0.72f, 1.35f),
                Main.rand.NextFloat(MathHelper.TwoPi),
                0.08f,
                0.014f,
                18);
            GeneralParticleHandler.SpawnParticle(ring);

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(4f, 12f);
                Particle shard = new AltSparkParticle(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    velocity,
                    false,
                    Main.rand.Next(10, 18),
                    Main.rand.NextFloat(0.55f, 1f),
                    i % 3 == 0 ? Color.Black : Color.Lerp(EntropyCyan, EntropyViolet, Main.rand.NextFloat()));
                GeneralParticleHandler.SpawnParticle(shard);
            }

            for (int i = 0; i < 10; i++)
            {
                Particle smoke = new HeavySmokeParticle(
                    center + Main.rand.NextVector2Circular(14f, 14f),
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    Main.rand.NextBool() ? Color.Black : EntropyAsh,
                    Main.rand.Next(18, 30),
                    Main.rand.NextFloat(0.52f, 0.95f),
                    0.34f,
                    Main.rand.NextFloat(-0.08f, 0.08f),
                    false);
                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D magicPoint = TextureAssets.Extra[89].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + Vector2.UnitY * Projectile.gfxOffY;
            float pulse = 1f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f + Projectile.identity);

            for (int i = 0; i < 4; i++)
            {
                float angle = Main.GlobalTimeWrappedHourly * MathHelper.TwoPi * 0.58f + MathHelper.TwoPi * i / 4f;
                Color color = Color.Lerp(EntropyCyan, EntropyViolet, i / 3f);
                color.A = 0;

                Main.EntitySpriteDraw(
                    magicPoint,
                    drawPosition,
                    null,
                    color * 1.18f,
                    angle,
                    magicPoint.Size() * 0.5f,
                    (0.22f + i * 0.045f) * pulse,
                    SpriteEffects.None,
                    0);
            }

            return false;
        }
    }
}
