using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.APreHardMode
{
    public class AnodizedWulfrumMetalEffect : DefaultEffect
    {
        public override int EffectID => 45;
        public override int AmmoType => ModContent.Find<ModItem>("CalamityMod/AnodizedWulfrumMetal").Type;

        public override bool EnableDefaultSlowdown => false;
        public override bool EnableProximityExplosion => false;
        public override bool SuppressDefaultOnKillEffects => true;
        public override float ExplosionPulseFactor => 0f;
        public override float SquishyLightParticleFactor => 0f;
        public override float GlowScaleFactor => 0f;
        public override float GlowIntensityFactor => 0f;

        private static int _blunderbussType = -1;
        private static int BlunderbussType
        {
            get
            {
                if (_blunderbussType < 0)
                    _blunderbussType = ModContent.Find<ModItem>("CalamityMod/WulfrumBlunderbuss").Item.shoot;
                return _blunderbussType;
            }
        }

        // 夺舍：取消即死，让 SHPC 弹幕正常飞行
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.penetrate = 1;
            projectile.timeLeft = 90;
            projectile.tileCollide = true;
        }

        // 特技：飞行时拖曳 Wulfrum 电弧粒子
        public override void AI(Projectile projectile, Player owner)
        {
            if (Main.rand.NextBool(3))
            {
                Dust spark = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.Electric,
                    -projectile.velocity * 0.15f + Main.rand.NextVector2Circular(1f, 1f),
                    0,
                    new Color(190, 255, 60),
                    Main.rand.NextFloat(0.4f, 0.8f)
                );
                spark.noGravity = true;
            }
        }

        // 夺舍：绘制猎枪霰弹纹理，抹掉 SHPC 球体外观
        public override void PostDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            int type = BlunderbussType;
            if (type <= 0)
                return;

            ModProjectile modProj = ProjectileLoader.GetProjectile(type);
            if (modProj == null)
                return;

            Texture2D tex = ModContent.Request<Texture2D>(modProj.Texture).Value;
            spriteBatch.Draw(
                tex,
                projectile.Center - Main.screenPosition,
                null,
                Color.White,
                projectile.velocity.ToRotation(),
                tex.Size() / 2f,
                1f,
                SpriteEffects.None,
                0f
            );
        }

        // 夺舍：收缩碰撞箱到霰弹大小
        public override void ModifyDamageHitbox(Projectile projectile, Player owner, ref Rectangle hitbox)
        {
            int size = 8;
            hitbox = new Rectangle(
                (int)(projectile.Center.X - size / 2f),
                (int)(projectile.Center.Y - size / 2f),
                size,
                size
            );
        }

        // 随机散射 7-10 发，每发独立随机角度
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            if (projectile.owner != Main.myPlayer)
                return;

            int pelletCount = Main.rand.Next(7, 11);
            Vector2 direction = projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            float speed = projectile.velocity.Length();
            float halfSpread = MathHelper.ToRadians(2.5f);

            for (int i = 0; i < pelletCount; i++)
            {
                float angle = Main.rand.NextFloat(-halfSpread, halfSpread);
                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center,
                    direction.RotatedBy(angle) * speed,
                    BlunderbussType,
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner
                );
            }

            // 特技：散射时电光爆发
            for (int i = 0; i < 14; i++)
            {
                Dust spark = Dust.NewDustPerfect(
                    projectile.Center,
                    DustID.Electric,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 6f),
                    0,
                    new Color(190, 255, 60),
                    Main.rand.NextFloat(0.9f, 1.5f)
                );
                spark.noGravity = true;
            }

            Particle flash = new CustomPulse(
                projectile.Center,
                Vector2.Zero,
                new Color(200, 255, 70) * 0.75f,
                "CalamityMod/Particles/BloomCircle",
                Vector2.One * 0.1f,
                0f,
                0.06f,
                0.28f,
                10,
                true
            );
            GeneralParticleHandler.SpawnParticle(flash);
        }
    }
}
