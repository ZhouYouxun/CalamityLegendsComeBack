using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 三层复仇印记的周期性小型战术爆破。生成即为一小片范围伤害，短暂存在。
    /// </summary>
    public class M4A1MarkDetonation : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Radius = 88;

        public override void SetDefaults()
        {
            Projectile.width = Radius;
            Projectile.height = Radius;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 5;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => true;
        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.45f, Pitch = 0.35f }, Projectile.Center);

            if (Main.dedServ)
                return;

            Color mark = M4A1Visuals.MarkColor;
            for (int i = 0; i < 16; i++)
            {
                Dust d = Dust.NewDustPerfect(Projectile.Center, DustID.Torch, Main.rand.NextVector2Circular(5f, 5f), 90, mark, Main.rand.NextFloat(1.1f, 1.9f));
                d.noGravity = true;
            }
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, mark, 0.85f, 16, true));
            GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, Vector2.Zero, new Color(255, 220, 190), 0.5f, 12, true));
        }
    }
}
