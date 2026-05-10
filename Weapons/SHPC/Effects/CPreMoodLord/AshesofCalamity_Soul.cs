using CalamityMod.Dusts;
using CalamityMod.Graphics.Metaballs;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    internal class AshesofCalamity_Soul : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private bool CanCreatePortal => Projectile.ai[0] == 1f;

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 6;
            ProjectileID.Sets.TrailCacheLength[Type] = 3;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 80;
            Projectile.alpha = 255;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 50;
            Projectile.DamageType = DamageClass.Magic;
        }

        public override void OnSpawn(IEntitySource source)
        {
        }

        public override void AI()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter > 9)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }

            if (Projectile.frame >= 6)
                Projectile.frame = 0;

            if (Projectile.alpha > 5)
                Projectile.alpha -= 15;
            if (Projectile.alpha < 5)
                Projectile.alpha = 5;

            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() +
                                  (Projectile.spriteDirection == 1 ? 0f : MathHelper.Pi) -
                                  MathHelper.ToRadians(90f) * Projectile.direction;

            Projectile.velocity *= 1.03f;
            Lighting.AddLight(Projectile.Center, 0.65f, 0f, 0f);

            Dust brimDust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(32f, 32f),
                (int)CalamityDusts.Brimstone,
                null,
                170,
                default,
                1.1f
            );
            brimDust.noGravity = true;
            brimDust.velocity *= 0.5f;
            brimDust.velocity += Projectile.velocity * 0.1f;

            CalamitasMetaball.SpawnParticle(
                Projectile.Center + Projectile.velocity,
                Main.rand.NextVector2Circular(2f, 2f),
                64f
            );
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            for (int i = 0; i < 2; i++)
            {
                CalamitasMetaball.SpawnParticle(
                    target.Center + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(2f, 2f),
                    58f
                );
            }

            if (!CanCreatePortal || Projectile.owner != Main.myPlayer)
                return;

            Projectile.ai[0] = 0f;
            Projectile.netUpdate = true;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AshesofCalamity_Portal>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner
            );
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(250, 50, 50, Projectile.alpha);
        }

        public override void OnKill(int timeLeft)
        {
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }
    }
}
