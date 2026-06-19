using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera
{
    public class StarblightSootEffect : DefaultEffect
    {
        public override int EffectID => 8;
        public override int AmmoType => ModContent.ItemType<StarblightSoot>();

        public override Color ThemeColor => new(255, 120, 76);
        public override Color StartColor => new(255, 176, 92);
        public override Color EndColor => new(92, 205, 255);

        public override float SquishyLightParticleFactor => 0f;
        public override float ExplosionPulseFactor => 0f;
        public override bool EnableDefaultSlowdown => false;
        public override bool PlayDefaultLeftClickFireSound => false;

        public override void OnSpawn(Projectile projectile, Player owner)
        {
            projectile.timeLeft = 1;
            projectile.penetrate = -1;
            projectile.tileCollide = false;
            projectile.friendly = false;
            projectile.hide = true;

            if (projectile.owner != Main.myPlayer)
                return;

            Vector2 forward = projectile.velocity.SafeNormalize(owner.direction == 0 ? Vector2.UnitX : new Vector2(owner.direction, 0f));
            float baseSpeed = MathHelper.Clamp(projectile.velocity.Length(), 12f, 28f);
            float[] shotAngles =
            {
                MathHelper.ToRadians(Main.rand.NextFloat(-5f, 5f)),
                MathHelper.ToRadians(Main.rand.NextFloat(15f, 24f)),
                MathHelper.ToRadians(Main.rand.NextFloat(-24f, -15f)),
                MathHelper.ToRadians(Main.rand.NextFloat(-24f, 24f)),
                MathHelper.ToRadians(Main.rand.NextFloat(-24f, 24f)),
                MathHelper.ToRadians(Main.rand.NextFloat(-24f, 24f)),
                MathHelper.ToRadians(Main.rand.NextFloat(-24f, 24f))
            };

            for (int i = 0; i < shotAngles.Length; i++)
            {
                float speedScale = i == 0 ? 1f : Main.rand.NextFloat(0.9f, 1.12f);
                Vector2 velocity = forward.RotatedBy(shotAngles[i]) * baseSpeed * speedScale * 1.5f;

                Projectile.NewProjectile(
                    projectile.GetSource_FromThis(),
                    projectile.Center + forward * 18f,
                    velocity,
                    ModContent.ProjectileType<StarblightSootShard>(),
                    projectile.damage,
                    projectile.knockBack,
                    projectile.owner,
                    speedScale,
                    Main.rand.Next(5));
            }
        }

        public override void AI(Projectile projectile, Player owner)
        {
            projectile.Kill();
        }

        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // Do nothing to prevent DefaultEffect's default OnKill explosion from spawning
        }
    }

 
}
