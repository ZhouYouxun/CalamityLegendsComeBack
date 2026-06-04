using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidCometStealthMark : ModProjectile, ILocalizedModType
    {
        public override string Texture => "Terraria/Images/Projectile_533";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        private int SpawnedMeteors
        {
            get => (int)Projectile.localAI[0];
            set => Projectile.localAI[0] = value;
        }

        private int SpawnTimer
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        private int TargetIndex => (int)Projectile.ai[0];
        private int PrimaryEffectID => (int)Projectile.ai[1];
        private int SecondaryEffectID => (int)Projectile.ai[2];

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 12;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.friendly = false;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (TargetIndex < 0 || TargetIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC target = Main.npc[TargetIndex];
            if (!target.active || target.dontTakeDamage || target.life <= 0)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            SpawnTimer++;

            Color markColor = LeonidVisualUtils.GetMeteorColor(PrimaryEffectID, SecondaryEffectID);
            Lighting.AddLight(Projectile.Center, markColor.ToVector3() * 0.55f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(target.width * 0.45f, target.height * 0.45f),
                    DustID.TintableDustLighted,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    markColor,
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
            }

            if (SpawnTimer >= 12 && SpawnedMeteors < 6 && Main.myPlayer == Projectile.owner)
            {
                SpawnTimer = 0;
                SpawnedMeteors++;

                Vector2 spawnPosition = new(target.Center.X + Main.rand.Next(-120, 121), target.Center.Y - 600f - Main.rand.Next(30, 120));
                Vector2 velocity = (target.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 20.5f;

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<LeonidCometSmall>(),
                    Projectile.damage / 2,
                    Projectile.knockBack,
                    Projectile.owner,
                    PrimaryEffectID,
                    SecondaryEffectID,
                    1f);
            }
        }
    }
}
