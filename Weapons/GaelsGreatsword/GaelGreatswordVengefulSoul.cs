using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordVengefulSoul : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/RedirectingVengefulSoul";

        private static readonly Color SoulPurple = new(95, 36, 150);
        private static readonly Color BloodRed = new(170, 12, 42);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 13;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            // 贴图为水平朝向：与灾厄原版 RedirectingVengefulSoul 一致，
            // 面朝速度方向，向左飞时旋转半圈并交由 DrawAfterimagesCentered 水平翻转。
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.rotation = Projectile.velocity.ToRotation();
            if (Projectile.spriteDirection == -1)
                Projectile.rotation += MathHelper.Pi;
            Lighting.AddLight(Projectile.Center, 0.18f, 0.04f, 0.32f);

            if (Projectile.ai[0] > 14f)
                CalamityUtils.HomeInOnNPC(Projectile, true, 920f, 14f, 22f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 6 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 12f),
                    Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.4f, 1.6f), 120,
                    Main.rand.NextBool() ? SoulPurple : BloodRed, Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }

            // 灾厄原版 RedirectingVengefulSoul 的灵质拖尾：发光的传送门尘埃，
            // 染成灵魂紫，不发光照（noLight）以免整屏泛紫。
            if (Main.rand.NextBool(2))
            {
                Dust ectoplasm = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(7f, 10f), DustID.PortalBoltTrail);
                ectoplasm.velocity = -Projectile.velocity * Main.rand.NextFloat(0.05f, 0.2f);
                ectoplasm.color = SoulPurple;
                ectoplasm.scale = Main.rand.NextFloat(0.9f, 1.3f);
                ectoplasm.noGravity = true;
                ectoplasm.noLight = true;
            }

            // 灵珠残响：偶尔在身后滞留一颗缓缓熄灭的灵魂光珠。
            if (!Main.dedServ && Main.rand.NextBool(6))
            {
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 18f + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextVector2Circular(0.6f, 0.6f), false,
                    Main.rand.Next(16, 26), Main.rand.NextFloat(0.12f, 0.2f), SoulPurple));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.52f, MathHelper.Clamp(Projectile.numHits / 3f, 0f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(lightColor, SoulPurple, 0.35f), 1);
            return false;
        }
    }
}
