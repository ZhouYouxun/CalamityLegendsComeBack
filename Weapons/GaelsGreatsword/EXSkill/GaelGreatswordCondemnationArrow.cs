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
    internal sealed class GaelGreatswordCondemnationArrow : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/CondemnationArrow";

        private static readonly Color BloodRed = GaelGreatswordVisuals.BrimstoneRed;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 54;
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
            Lighting.AddLight(Projectile.Center, 0.28f, 0.04f, 0.3f);

            if (Projectile.ai[0] > 18f)
                CalamityUtils.HomeInOnNPC(Projectile, true, 860f, 18f, 14f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 10f,
                    DustID.Blood, -Projectile.velocity * 0.12f, 100, BloodRed, Main.rand.NextFloat(0.8f, 1.1f));
                dust.noGravity = true;
            }

            // 箭尾星闪：裁决的天罚箭矢曳出细碎星光。
            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                Vector2 tail = Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 20f;
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(tail + Main.rand.NextVector2Circular(5f, 5f),
                    -Projectile.velocity * Main.rand.NextFloat(0.04f, 0.12f), Color.White,
                    Main.rand.NextBool() ? GaelGreatswordVisuals.EmberGold : GaelGreatswordVisuals.CrimsonViolet,
                    Main.rand.NextFloat(0.35f, 0.6f), Main.rand.Next(9, 15), Main.rand.NextFloat(-0.08f, 0.08f), 2f));
            }
        }

        public override Color? GetAlpha(Color lightColor)
        {
            // 硫红↔烬金脉动染色：每支箭以自身 identity 为相位错开闪烁，
            // 箭体像烧着裁决硫火一样明灭。
            Color bloodGlow = GaelGreatswordVisuals.BrimstoneHot with { A = 0 };
            Color emberGlow = GaelGreatswordVisuals.EmberGold with { A = 0 };
            Color fadeColor = Color.Lerp(bloodGlow, emberGlow,
                MathF.Cos(Projectile.identity * 1.41f + Main.GlobalTimeWrappedHourly * 8f) * 0.5f + 0.5f);
            return Color.Lerp(lightColor, fadeColor, 0.5f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Projectile.GetAlpha(lightColor), 1);
            return false;
        }
    }
}
