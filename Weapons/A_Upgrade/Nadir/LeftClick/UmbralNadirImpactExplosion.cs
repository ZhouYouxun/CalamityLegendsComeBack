using System;
using CalamityMod;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.Nadir.LeftClick
{
    /// <summary>
    /// 左键命中的一次性冥蚀冲击：短促、猛烈、可读。
    /// 只在前几帧造成一次范围伤害（局部无敌帧，能扫群但不会在大体型上叠成霰弹），
    /// 视觉为"黑色坍缩核 → 一圈克制的荧绿边缘 → 少量黑砂粒外甩"。不生成任何后续弹幕。
    /// ai[0] = 连招段数(0/1/2)，决定半径与是否轻微震屏。
    /// </summary>
    public class UmbralNadirImpactExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Nadir";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public int Stage => (int)Projectile.ai[0];
        private float Radius => UmbralNadirBalance.GetImpactRadius(Stage);

        public override void SetStaticDefaults() => ProjectileID.Sets.CultistIsResistantTo[Type] = true;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.timeLeft = 15;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        // 只在前 3 帧造成范围伤害（timeLeft 从 15 起，前三帧为 15/14/13）
        public override bool? CanDamage() => Projectile.timeLeft >= 13 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
            => CalamityUtils.CircularHitboxCollision(Projectile.Center, Radius, targetHitbox);

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MeldBurn") with { Volume = 0.5f, Pitch = 0.15f + Stage * 0.08f }, Projectile.Center);

            // 黑色坍缩核（AlphaBlend，透明底 SmallBloom → 不出黑框）
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.Black,
                "CalamityMod/Particles/SmallBloom", Vector2.One, Main.rand.NextFloat(-10f, 10f),
                Radius / 320f, Radius / 900f, 14, false));

            // 一圈克制的荧绿边缘（加色，顶层）
            GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, Color.LightGreen with { A = 0 },
                "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f),
                Radius / 260f, Radius / 150f, 15, true), false, GeneralDrawLayer.AfterEverything);

            // 黑色砂粒外甩
            int grit = Main.rand.Next(10, 17);
            for (int i = 0; i < grit; i++)
            {
                Vector2 vel = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2.5f, 6.5f) * (0.7f + Stage * 0.2f);
                GeneralParticleHandler.SpawnParticle(new GenericBloom(Projectile.Center, vel, Color.Black,
                    Main.rand.NextFloat(0.16f, 0.34f), Main.rand.Next(9, 14), true, false));
            }
            // 少量深渊识别点（内黑外绿）
            for (int i = 0; i < 4; i++)
            {
                Dust vd = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                vd.noGravity = true;
                vd.velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(2f, 5f);
                vd.scale = Main.rand.NextFloat(0.9f, 1.4f);
                vd.color = Color.LightGreen;
            }

            float shake = UmbralNadirBalance.GetImpactScreenShake(Stage);
            if (shake > 0f)
            {
                float f = Utils.GetLerpValue(1300f, 0f, Vector2.Distance(Projectile.Center, Main.LocalPlayer.Center), true);
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, shake * f);
            }
        }

        public override void AI() => Projectile.velocity = Vector2.Zero;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
            => target.AddBuff(ModContent.BuffType<CalamityMod.Buffs.DamageOverTime.Voidfrost>(), 90);
    }
}
