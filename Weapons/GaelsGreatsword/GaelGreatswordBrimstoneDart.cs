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
    internal sealed class GaelGreatswordBrimstoneDart : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/BrimstoneBarrage";

        private static readonly Color BloodRed = new(185, 14, 38);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.55f, 0.02f, 0.06f);

            if (Projectile.ai[0] > 10f)
                CalamityUtils.HomeInOnNPC(Projectile, true, 760f, 16f, 18f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 12f,
                    DustID.Blood, -Projectile.velocity * 0.14f, 100, BloodRed, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }

            // 灾厄原版 BrimstoneBarrage 的熔火拖尾：细碎的炽热尘埃向后散逸。
            Dust trailDust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(4f, 4f), DustID.TheDestroyer);
            trailDust.noGravity = true;
            trailDust.velocity = -Projectile.velocity * 0.5f * Main.rand.NextFloat(0.1f, 0.9f);
            trailDust.scale = Main.rand.NextFloat(0.2f, 0.6f);

            // 硫火速度线：偶发一缕沿飞行方向拉长的血色火线，强化贯穿感。
            if (!Main.dedServ && Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 16f,
                    -Projectile.velocity * Main.rand.NextFloat(0.12f, 0.3f), false,
                    Main.rand.Next(8, 14), Main.rand.NextFloat(0.3f, 0.55f), BloodRed));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.58f, MathHelper.Clamp(Projectile.numHits / 2f, 0f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }
}
