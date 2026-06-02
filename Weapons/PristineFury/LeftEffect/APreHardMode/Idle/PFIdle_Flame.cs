using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFIdle_Flame : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Particles/MediumMist";

        private const int Lifetime = 84;
        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = Lifetime;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Timer++;
            Projectile.rotation = Projectile.velocity.ToRotation();
            Color color = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62));
            Projectile.velocity.Y = MathHelper.Clamp(Projectile.velocity.Y + 0.08f, -8f, 8f);
            Projectile.velocity.X *= 0.992f;
            Lighting.AddLight(Projectile.Center, color.ToVector3() * 0.34f);

            if (Main.dedServ)
                return;

            if (Timer == 7f && !Main.rand.NextBool(4))
                SpawnSparks(3, 0.72f);

            if (Timer < 8f)
                return;

            Dust cinder = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                Projectile.velocity * 0.24f + Main.rand.NextVector2Circular(0.45f, 0.45f),
                40,
                Color.Lerp(color, Color.White, Main.rand.NextFloat(0.04f, 0.26f)),
                Main.rand.NextFloat(0.55f, 1.15f));
            cinder.noGravity = true;
            cinder.fadeIn = 0.9f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(ModContent.BuffType<HolyFlames>(), 120);

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SpawnSparks(8, 1f);
            return true;
        }

        public override void OnKill(int timeLeft) => SpawnSparks(5, 0.9f);

        private void SpawnSparks(int count, float scale)
        {
            if (Main.dedServ)
                return;

            Color color = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 146, 62));
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Projectile.velocity.RotatedByRandom(MathHelper.ToRadians(34f)) * Main.rand.NextFloat(0.7f, 1.8f);
                velocity.Y -= Main.rand.NextFloat(2.5f, 5.8f);
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    Main.rand.NextBool(3) ? DustID.GoldFlame : DustID.YellowTorch,
                    velocity,
                    30,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.08f, 0.38f)),
                    Main.rand.NextFloat(0.65f, 1.2f) * scale);
                spark.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
