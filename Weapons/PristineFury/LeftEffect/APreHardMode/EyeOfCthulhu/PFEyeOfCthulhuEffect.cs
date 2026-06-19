using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFEyeOfCthulhuEffect
    {
        private const int FireInterval = 34;
        private const int BeamCount = 5;
        private const float SearchRange = 1320f;
        private const float BeamLength = 920f;
        private const float MaxTargetLeanAngle = MathHelper.Pi / 36f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            FireDeathstareFan(holdout);
        }

        private static void FireDeathstareFan(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 muzzle = holdout.GunTipPosition + holdout.AimDirection * 18f;
            List<NPC> targets = FindForwardTargets(muzzle, holdout.AimDirection);

            for (int i = 0; i < BeamCount; i++)
            {
                NPC target = targets.Count > 0 ? targets[i % targets.Count] : null;
                Vector2 direction = GetLimitedTargetDirection(holdout.AimDirection, muzzle, target);
                Vector2 vector = direction * BeamLength;

                int beam = Projectile.NewProjectile(
                    holdout.Projectile.GetSource_FromThis(),
                    muzzle,
                    vector,
                    ModContent.ProjectileType<PFEyeOfCthulhuDeathstareBeam>(),
                    holdout.GetScaledDamage(0.42f),
                    holdout.Projectile.knockBack * 0.4f,
                    holdout.Projectile.owner,
                    i,
                    holdout.Projectile.whoAmI,
                    target?.whoAmI ?? -1);
                PFLeftEffectRules.ApplyTheme(beam, holdout.CurrentMark);
            }

            SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.64f, Pitch = 0.24f }, muzzle);
            holdout.ApplyRecoil(6.4f);
            holdout.TriggerMuzzleFlash(10);
            holdout.SpawnMuzzleBurst(new Color(228, 72, 72), 0.64f);
        }

        private static Vector2 GetLimitedTargetDirection(Vector2 forward, Vector2 origin, NPC target)
        {
            Vector2 baseDirection = forward.SafeNormalize(Vector2.UnitX);
            if (target == null)
                return baseDirection;

            Vector2 targetDirection = (target.Center - origin).SafeNormalize(baseDirection);
            float angleOffset = MathHelper.WrapAngle(targetDirection.ToRotation() - baseDirection.ToRotation());
            angleOffset = MathHelper.Clamp(angleOffset, -MaxTargetLeanAngle, MaxTargetLeanAngle);
            return baseDirection.RotatedBy(angleOffset);
        }

        private static List<NPC> FindForwardTargets(Vector2 origin, Vector2 forward)
        {
            List<NPC> targets = new();
            List<(NPC Target, float Score)> candidates = new();

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toTarget = npc.Center - origin;
                float distance = toTarget.Length();
                if (distance > SearchRange || distance < 1f)
                    continue;

                Vector2 direction = toTarget / distance;
                float dot = Vector2.Dot(direction, forward);
                if (dot < -0.1f)
                    continue;

                float anglePenalty = (1f - dot) * 520f;
                candidates.Add((npc, distance + anglePenalty));
            }

            candidates.Sort((left, right) => left.Score.CompareTo(right.Score));
            for (int i = 0; i < candidates.Count && targets.Count < BeamCount; i++)
                targets.Add(candidates[i].Target);

            return targets;
        }
    }

    internal sealed class PFEyeOfCthulhuDeathstareBeam : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/Summon/DeathstareBeam";

        private Vector2 End => Projectile.Center + Projectile.velocity;
        private Color BeamColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(228, 72, 72));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 10;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.Opacity = MathHelper.Clamp(Projectile.timeLeft / 6f, 0f, 1f);

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            DelegateMethods.v3_1 = BeamColor.ToVector3() * 0.62f;
            Utils.PlotTileLine(Projectile.Center, End, 28f, DelegateMethods.CastLight);
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            for (int i = 0; i < 2; i++)
            {
                float completion = Main.rand.NextFloat();
                Vector2 point = Vector2.Lerp(Projectile.Center, End, completion);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    point,
                    direction * Main.rand.NextFloat(0.4f, 1.6f),
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    8,
                    Main.rand.NextFloat(0.08f, 0.13f),
                    Color.Lerp(BeamColor, Color.White, Main.rand.NextFloat(0.18f, 0.52f)),
                    new Vector2(0.28f, 1.55f),
                    true,
                    true));
            }
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, End, 18f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float length = Projectile.velocity.Length();
            Color color = (Color.Lerp(BeamColor, Color.White, 0.18f) with { A = 0 }) * Projectile.Opacity;
            Rectangle source = texture.Frame();

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(texture, start + direction * (length * 0.5f), source, color, direction.ToRotation() + MathHelper.PiOver2, source.Size() * 0.5f, new Vector2(1.25f, length / source.Height), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, start, null, color * 0.5f, 0f, bloom.Size() * 0.5f, 0.18f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, End - Main.screenPosition, null, color * 0.65f, 0f, bloom.Size() * 0.5f, 0.22f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
