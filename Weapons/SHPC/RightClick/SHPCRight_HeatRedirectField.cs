using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.RightClick
{
    internal sealed class SHPCRight_HeatRedirectField : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int HeatStage => Utils.Clamp((int)Projectile.ai[0], 1, 5);
        private int FieldSize => (int)Projectile.ai[1];

        public override void SetDefaults()
        {
            Projectile.width = 200;
            Projectile.height = 200;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 45;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 20;
            Projectile.alpha = 255;
        }

        public override void AI()
        {
            ResizeField();
            if (Main.dedServ || Projectile.numUpdates != 0 || Main.GameUpdateCount % 3 != 0)
                return;

            Vector2 smokePosition = Projectile.Center + Main.rand.NextVector2Circular(Projectile.width * 0.42f, Projectile.height * 0.42f);
            Vector2 smokeVelocity = new(Main.rand.NextFloat(-0.8f, 0.8f), Main.rand.NextFloat(-2.6f, -0.8f));
            GeneralParticleHandler.SpawnParticle(new MediumMistParticle(
                smokePosition,
                smokeVelocity,
                Color.White,
                Color.Transparent,
                Main.rand.NextFloat(0.75f, 1.25f),
                Main.rand.NextFloat(90f, 140f)));
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 0.7f + HeatStage * 0.07f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public static void SpawnOrRefresh(Player player, Vector2 center, int damage, int heatStage, int size, int duration)
        {
            if (player.whoAmI != Main.myPlayer)
                return;

            int fieldType = ModContent.ProjectileType<SHPCRight_HeatRedirectField>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != player.whoAmI || projectile.type != fieldType)
                    continue;

                if (Vector2.DistanceSquared(projectile.Center, center) > 120f * 120f)
                    continue;

                projectile.timeLeft = System.Math.Max(projectile.timeLeft, duration);
                projectile.damage = System.Math.Max(projectile.damage, damage);
                projectile.ai[0] = System.Math.Max(projectile.ai[0], heatStage);
                projectile.ai[1] = System.Math.Max(projectile.ai[1], size);
                projectile.netUpdate = true;
                return;
            }

            Projectile.NewProjectile(
                player.GetSource_FromThis(),
                center,
                Vector2.Zero,
                fieldType,
                damage,
                0f,
                player.whoAmI,
                heatStage,
                size);
        }

        private void ResizeField()
        {
            int size = FieldSize > 0 ? FieldSize : 200;
            if (Projectile.width == size)
                return;

            Vector2 center = Projectile.Center;
            Projectile.width = size;
            Projectile.height = size;
            Projectile.Center = center;
        }
    }
}
