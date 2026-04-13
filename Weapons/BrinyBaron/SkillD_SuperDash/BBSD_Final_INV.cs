using System;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BrinyBaron.SkillD_SuperDash
{
    internal class BBSD_Final_INV : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 60;
        private const int SlashInterval = 5;
        private const float SlashScale = 3.1f;
        private const float OrbitRadius = 42f;

        private int TargetNpcIndex => (int)Projectile.ai[0];
        private float BaseRotation => Projectile.ai[1];
        private NPC LockedTarget => BBSuperDashTargeting.IsTargetValid(TargetNpcIndex) ? Main.npc[TargetNpcIndex] : null;

        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 140;
            Projectile.height = 140;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.netImportant = true;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            NPC target = LockedTarget;
            if (target is null)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = target.Center;
            Projectile.velocity = target.velocity;
            Projectile.rotation = BaseRotation + (Lifetime - Projectile.timeLeft) * 0.23f;

            SpawnOrbitEffects(target);

            int elapsed = Lifetime - Projectile.timeLeft + 1;
            if (Main.myPlayer == Projectile.owner && elapsed % SlashInterval == 0)
                ReleaseFinalSlash(target, elapsed / SlashInterval);
        }

        private void ReleaseFinalSlash(NPC target, int sequenceIndex)
        {
            float wave = (sequenceIndex - 1) * 0.58f;
            float slashRotation = BaseRotation + wave;
            Vector2 slashDirection = slashRotation.ToRotationVector2();

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                slashDirection * 8f,
                ModContent.ProjectileType<BBSwing_Slash>(),
                Projectile.damage,
                Projectile.knockBack,
                Projectile.owner,
                SlashScale,
                Main.rand.NextFloat(-0.08f, 0.08f));

            SoundEngine.PlaySound(SoundID.Item71 with
            {
                Volume = 1f,
                Pitch = -0.22f + Main.rand.NextFloat(-0.06f, 0.06f)
            }, target.Center);
        }

        private void SpawnOrbitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            float progress = 1f - Projectile.timeLeft / (float)Lifetime;
            float pulse = 0.7f + 0.3f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 18f);

            for (int i = 0; i < 4; i++)
            {
                float angle = Projectile.rotation + MathHelper.PiOver2 * i;
                Vector2 offset = angle.ToRotationVector2() * OrbitRadius * (0.85f + pulse * 0.18f);

                Particle marker = new CustomSpark(
                    target.Center + offset,
                    target.velocity * 0.04f,
                    "CalamityLegendsComeBack/Weapons/BrinyBaron/SkillA_ShortDash/GlowBlade",
                    false,
                    6,
                    0.16f,
                    new Color(180, 244, 255) * 1.05f,
                    new Vector2(0.56f, 2.6f),
                    glowCenter: true,
                    shrinkSpeed: 0.95f,
                    glowCenterScale: 0.92f,
                    glowOpacity: 0.74f);
                GeneralParticleHandler.SpawnParticle(marker);
            }

            if (Projectile.timeLeft % 2 == 0)
            {
                DirectionalPulseRing ring = new DirectionalPulseRing(
                    target.Center,
                    target.velocity * 0.04f,
                    Color.Lerp(new Color(110, 220, 255), new Color(255, 236, 170), progress * 0.5f),
                    new Vector2(0.52f, 1.28f),
                    Projectile.rotation,
                    0.11f,
                    0.018f,
                    12);
                GeneralParticleHandler.SpawnParticle(ring);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    target.Center + Main.rand.NextVector2Circular(32f, 32f),
                    Main.rand.NextBool() ? DustID.Water : DustID.YellowTorch,
                    target.velocity * 0.06f + Main.rand.NextVector2Circular(1.1f, 1.1f),
                    100,
                    new Color(210, 246, 255),
                    Main.rand.NextFloat(0.95f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            NPC target = LockedTarget;
            if (target is null)
                return false;

            Texture2D glowTex = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPos = target.Center - Main.screenPosition;
            float fade = Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true) * Utils.GetLerpValue(Lifetime, Lifetime - 8f, Projectile.timeLeft, true);
            Color outer = new Color(130, 225, 255, 0) * 0.42f * fade;
            Color inner = new Color(255, 238, 180, 0) * 0.28f * fade;

            Main.EntitySpriteDraw(glowTex, drawPos, null, outer, Projectile.rotation, glowTex.Size() * 0.5f, 0.38f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glowTex, drawPos, null, inner, -Projectile.rotation * 1.3f, glowTex.Size() * 0.5f, 0.2f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
