using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.PeaShooter
{
    internal sealed class PeaShooterSplash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.A_Dev";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private PeaShooterPeaType PeaType => (PeaShooterPeaType)(int)Projectile.ai[0];
        private int StageIndex => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.hide = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] != 0f)
                return;

            Projectile.localAI[0] = 1f;
            int radius = BalancePeaShooter.GetSplashRadius(StageIndex, PeaType);
            Vector2 center = Projectile.Center;
            Projectile.Resize(radius * 2, radius * 2);
            Projectile.Center = center;

            SpawnSplashDust(radius);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.HitDirectionOverride = (Projectile.Center.X < target.Center.X).ToDirectionInt();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            PeaShooterPea.ApplyDebuffs(target, PeaType, StageIndex);
        }

        public override bool PreDraw(ref Color lightColor) => false;

        private void SpawnSplashDust(int radius)
        {
            Color color = PeaShooterPea.GetPeaColor(PeaType);
            int dustCount = PeaType == PeaShooterPeaType.Rock ? 18 : 11;
            float speedMax = PeaType == PeaShooterPeaType.Rock ? 6.2f : 3.8f;

            for (int i = 0; i < dustCount; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.0f, speedMax);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(radius * 0.18f, radius * 0.18f),
                    PeaShooterPea.GetDustType(PeaType),
                    velocity,
                    100,
                    Color.Lerp(color, Color.White, Main.rand.NextFloat(0.05f, 0.35f)),
                    Main.rand.NextFloat(0.72f, PeaType == PeaShooterPeaType.Rock ? 1.45f : 1.1f));
                dust.noGravity = PeaType != PeaShooterPeaType.Rock;
            }

            if (PeaType == PeaShooterPeaType.Rock)
            {
                for (int i = 0; i < 6; i++)
                {
                    Dust stone = Dust.NewDustPerfect(
                        Projectile.Center,
                        Main.rand.NextBool() ? DustID.Stone : DustID.Iron,
                        Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.4f, 6.4f),
                        120,
                        Color.SandyBrown,
                        Main.rand.NextFloat(0.92f, 1.35f));
                    stone.noGravity = false;
                }
            }
        }
    }
}
