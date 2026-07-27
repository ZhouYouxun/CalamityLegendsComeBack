using System;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.RightClick
{
    /// <summary>
    /// 第三发投矛命中时的终爆：黑核收缩一拍后炸开。
    /// 前 2 帧造成一次范围伤害（半径 132），随后只剩视觉。
    /// 只由第三发命中 NPC 时生成；撞墙 / 超时绝不生成它。不产生灵魂 / 触手 / 裂隙 / 持续伤害。
    /// </summary>
    public class UmbralNadirFinalExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color MeldGreen = Color.LightGreen;

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 40;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 18;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 16 ? null : false; // 仅前 2 帧

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, UmbralNadirBalance.FinalExplosionRadius, targetHitbox);

        public override void OnSpawn(IEntitySource source)
        {
            Vector2 c = Projectile.Center;
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldExplosion") with { Volume = 0.85f, Pitch = -0.1f }, c);

            // 黑核（AlphaBlend，透明底 SmallBloom）：先小后大炸开
            GeneralParticleHandler.SpawnParticle(new CustomPulse(c, Vector2.Zero, Color.Black,
                "CalamityMod/Particles/SmallBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.14f, 0.62f, 18, false));
            // 冥思招牌"外绿内黑"坍缩核
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(c, Vector2.Zero, MeldGreen with { A = 0 },
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.18f, 0.85f, 24), false, GeneralDrawLayer.AfterEverything);
            GeneralParticleHandler.SpawnParticle(new DetailedExplosion(c, Vector2.Zero, Color.Black,
                Vector2.One, Main.rand.NextFloat(MathHelper.TwoPi), 0.1f, 0.45f, 22, false));
            // 一圈荧绿外环
            GeneralParticleHandler.SpawnParticle(new CustomPulse(c, Vector2.Zero, MeldGreen,
                "CalamityMod/Particles/BloomRing", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 1.2f, 18),
                false, GeneralDrawLayer.AfterEverything);

            // 16~24 粒黑色颗粒 + 少量深渊尘
            int grit = Main.rand.Next(16, 25);
            for (int i = 0; i < grit; i++)
                GeneralParticleHandler.SpawnParticle(new GenericBloom(c,
                    Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 9f), Color.Black,
                    Main.rand.NextFloat(0.2f, 0.42f), Main.rand.Next(10, 16), true, false));
            for (int i = 0; i < 10; i++)
            {
                Dust vd = Dust.NewDustPerfect(c, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 8f);
                vd.scale = Main.rand.NextFloat(1.2f, 1.9f);
                vd.color = MeldGreen;
            }

            float f = Utils.GetLerpValue(1400f, 0f, Vector2.Distance(c, Main.LocalPlayer.Center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower =
                Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, UmbralNadirBalance.FinalExplosionScreenShake * f);
        }

        public override void AI() => Projectile.velocity = Vector2.Zero;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(ModContent.BuffType<Voidfrost>(), 180);
    }
}
