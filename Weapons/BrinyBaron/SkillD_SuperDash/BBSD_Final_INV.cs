using System;
using CalamityLegendsComeBack.Weapons.BrinyBaron.CommonAttack;
using CalamityLegendsComeBack.Weapons.BrinyBaron.TideValue;
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
        // The mark gives the target a brief warning before the real execution lands.
        private const int Lifetime = 36;
        private const int ImpactDelay = 24;
        private const float SlashLength = 560f;
        private const float SlashWidth = 190f;

        private int TargetNpcIndex => (int)Projectile.ai[0];
        private float BaseRotation => Projectile.ai[1];
        private NPC LockedTarget => BBSuperDashTargeting.IsTargetValid(TargetNpcIndex) ? Main.npc[TargetNpcIndex] : null;
        private bool executionLanded;

        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 460;
            Projectile.height = 460;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.netImportant = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => executionLanded ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!executionLanded)
                return false;

            Vector2 direction = BaseRotation.ToRotationVector2();
            Vector2 start = Projectile.Center - direction * SlashLength * 0.5f;
            Vector2 end = Projectile.Center + direction * SlashLength * 0.5f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, SlashWidth, ref collisionPoint);
        }

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
            Projectile.rotation = BaseRotation;

            SpawnOrbitEffects(target);

            int elapsed = Lifetime - Projectile.timeLeft + 1;
            if (elapsed == ImpactDelay)
                ReleaseExecutionSlash(target);

            if (elapsed > ImpactDelay + 3)
                Projectile.Kill();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!executionLanded || target.whoAmI != TargetNpcIndex || !Main.player.IndexInRange(Projectile.owner))
                return;

            Main.player[Projectile.owner].GetModPlayer<BBTideValuePlayer>().ResetTide();
        }

        private void ReleaseExecutionSlash(NPC target)
        {
            executionLanded = true;
            Projectile.friendly = true;
            Vector2 direction = BaseRotation.ToRotationVector2();
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2);

            for (int i = 0; i < 52; i++)
            {
                float along = Main.rand.NextFloat(-SlashLength * 0.5f, SlashLength * 0.5f);
                float across = Main.rand.NextFloat(-SlashWidth * 0.5f, SlashWidth * 0.5f);
                Vector2 position = target.Center + direction * along + perpendicular * across;
                Vector2 velocity = perpendicular * Main.rand.NextFloat(-4f, 4f) + direction * Main.rand.NextFloat(-2f, 2f);
                Dust water = Dust.NewDustPerfect(position, DustID.Water, velocity, 80, new Color(90, 215, 255), Main.rand.NextFloat(1.2f, 2.1f));
                water.noGravity = true;
                Dust bubble = Dust.NewDustPerfect(position, DustID.Water, velocity * 0.45f, 80, Color.White, Main.rand.NextFloat(0.9f, 1.55f));
                bubble.noGravity = true;
            }

            SoundEngine.PlaySound(SoundID.Item84 with { Volume = 1.25f, Pitch = -0.32f }, target.Center);
            SoundEngine.PlaySound(SoundID.Splash with { Volume = 1.05f, Pitch = -0.18f }, target.Center);
        }

        private void SpawnOrbitEffects(NPC target)
        {
            if (Main.dedServ)
                return;

            float progress = 1f - Projectile.timeLeft / (float)Lifetime;

            if (!executionLanded && Projectile.timeLeft % 3 == 0)
            {
                Vector2 orbit = (Projectile.rotation + progress * MathHelper.TwoPi * 2f).ToRotationVector2() * (38f + progress * 26f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(
                    target.Center + orbit,
                    orbit.SafeNormalize(Vector2.UnitX).RotatedBy(MathHelper.PiOver2) * 1.2f,
                    false,
                    10,
                    0.28f,
                    Color.Lerp(new Color(130, 225, 255), Color.White, 0.34f)));
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
            Color inner = new Color(210, 248, 255, 0) * 0.28f * fade;

            Main.EntitySpriteDraw(glowTex, drawPos, null, outer, Projectile.rotation, glowTex.Size() * 0.5f, 0.38f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glowTex, drawPos, null, inner, -Projectile.rotation * 1.3f, glowTex.Size() * 0.5f, 0.2f, SpriteEffects.None, 0f);
            return false;
        }
    }
}
