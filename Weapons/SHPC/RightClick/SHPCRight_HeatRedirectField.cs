using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using CalamityLegendsComeBack.Accssory.SHPC.Skill.HeatModule;
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
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || !owner.GetModPlayer<HeatModulePlayer>().HeatModuleEquipped ||
                !owner.GetModPlayer<SHPCRight_Player>().IsMaximumHeatFilled())
            {
                Projectile.Kill();
                return;
            }
            Projectile.Center = owner.Center;
            ResizeField();
            if (Main.dedServ || Projectile.numUpdates != 0 || Main.GameUpdateCount % 30 != 0)
                return;
            for (int i = 0; i < 12; i++)
            {
                Vector2 direction = (MathHelper.TwoPi * i / 12f + Main.GlobalTimeWrappedHourly * 0.25f).ToRotationVector2();
                Dust dust = Dust.NewDustPerfect(Projectile.Center + direction * (Projectile.width * 0.5f),
                    DustID.GemSapphire, -direction * 0.6f, 170, new Color(60, 180, 255), 0.7f);
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Main.player[Projectile.owner].GetModPlayer<HeatModulePlayer>().HeatModuleEquipped &&
            Main.player[Projectile.owner].GetModPlayer<SHPCRight_Player>().IsMaximumHeatFilled();

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 nearest = Vector2.Clamp(Projectile.Center, targetHitbox.TopLeft(), targetHitbox.BottomRight());
            float radius = Projectile.width * 0.5f;
            return Vector2.DistanceSquared(Projectile.Center, nearest) <= radius * radius;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) => target.AddBuff(BuffID.OnFire3, 120);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= 0.7f + HeatStage * 0.07f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color blue = new Color(50, 185, 255, 0) * 0.17f;
            Color deep = new Color(10, 75, 210, 0) * 0.14f;
            float scale = Projectile.width / (float)ring.Width;
            Main.EntitySpriteDraw(bloom, center, null, deep * 0.55f, 0f, bloom.Size() * 0.5f,
                new Vector2(1.9f, 1.25f) * (Projectile.width / (float)bloom.Width) * 0.72f, SpriteEffects.None, 0);
            for (int i = 0; i < 3; i++)
            {
                float local = i / 3f;
                float rotation = Main.GlobalTimeWrappedHourly * (0.28f + local * 0.14f);
                Main.EntitySpriteDraw(ring, center, null, (i == 0 ? blue : deep) * (1f - local * 0.22f),
                    rotation, ring.Size() * 0.5f, scale * (0.7f + local * 0.45f), SpriteEffects.None, 0);
            }
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
