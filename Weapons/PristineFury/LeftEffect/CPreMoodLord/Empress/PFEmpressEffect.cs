using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFEmpressEffect
    {
        private const float SearchRange = 1500f;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                KillExistingStream(holdout);
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            NPC target = FindTarget(holdout);
            if (target == null)
            {
                KillExistingStream(holdout);
                return;
            }

            Vector2 muzzle = holdout.GunTipPosition + holdout.AimDirection * 16f;
            int streamType = ModContent.ProjectileType<PFEmpressRainbowFireStream>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner != holdout.Projectile.owner || projectile.type != streamType || (int)projectile.ai[0] != holdout.Projectile.whoAmI)
                    continue;

                projectile.Center = muzzle;
                projectile.ai[1] = target.whoAmI;
                projectile.timeLeft = 2;
                projectile.netUpdate = true;
                PFLeftEffectRules.ApplyTheme(projectile.whoAmI, holdout.CurrentMark);
                EmitHoldEffects(holdout, muzzle, false);
                return;
            }

            int stream = Projectile.NewProjectile(
                holdout.Projectile.GetSource_FromThis(),
                muzzle,
                (target.Center - muzzle).SafeNormalize(holdout.AimDirection),
                streamType,
                holdout.GetScaledDamage(0.58f),
                holdout.Projectile.knockBack * 0.45f,
                holdout.Projectile.owner,
                holdout.Projectile.whoAmI,
                target.whoAmI);
            PFLeftEffectRules.ApplyTheme(stream, holdout.CurrentMark);
            EmitHoldEffects(holdout, muzzle, true);
        }

        private static NPC FindTarget(NewLegendPristineFuryHoldOut holdout)
        {
            Vector2 origin = holdout.GunTipPosition;
            Vector2 forward = holdout.AimDirection;
            NPC best = null;
            float bestScore = float.MaxValue;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                Vector2 toTarget = npc.Center - origin;
                float distance = toTarget.Length();
                if (distance > SearchRange || distance < 1f)
                    continue;

                float dot = Vector2.Dot(toTarget / distance, forward);
                if (dot < -0.15f)
                    continue;

                float score = distance + (1f - dot) * 680f;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                best = npc;
            }

            return best;
        }

        private static void EmitHoldEffects(NewLegendPristineFuryHoldOut holdout, Vector2 muzzle, bool justStarted)
        {
            holdout.ApplyRecoil(justStarted ? 6.8f : 0.5f);
            holdout.TriggerMuzzleFlash(5);
            if (holdout.LeftTimer++ % 7 == 0)
                holdout.SpawnMuzzleBurst(Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.35f) % 1f, 0.88f, 0.58f), 0.58f);

            if (justStarted)
                SoundEngine.PlaySound(SoundID.Item72 with { Volume = 0.64f, Pitch = 0.18f }, muzzle);
        }

        private static void KillExistingStream(NewLegendPristineFuryHoldOut holdout)
        {
            int streamType = ModContent.ProjectileType<PFEmpressRainbowFireStream>();
            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == holdout.Projectile.owner && projectile.type == streamType && (int)projectile.ai[0] == holdout.Projectile.whoAmI)
                    projectile.Kill();
            }
        }
    }

    internal sealed class PFEmpressRainbowFireStream : ModProjectile, ILocalizedModType
    {
        private const int RampFrames = 180;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int HoldoutIndex => (int)Projectile.ai[0];
        private int TargetIndex => (int)Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private float Ramp => MathHelper.Clamp(Timer / RampFrames, 0f, 1f);
        private Vector2 BeamEnd => Main.npc.IndexInRange(TargetIndex) ? Main.npc[TargetIndex].Center : Projectile.Center + Projectile.velocity * 900f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 34;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (!ValidateOwnerAndTarget(out NewLegendPristineFuryHoldOut holdout, out NPC target))
            {
                Projectile.Kill();
                return;
            }

            Timer++;
            Vector2 muzzle = holdout.GunTipPosition + holdout.AimDirection * 16f;
            Projectile.Center = muzzle;
            Vector2 desired = (target.Center - muzzle).SafeNormalize(holdout.AimDirection);
            Projectile.velocity = Vector2.Lerp(Projectile.velocity.SafeNormalize(desired), desired, 0.22f).SafeNormalize(desired);
            Projectile.timeLeft = 2;
            Projectile.rotation = Projectile.velocity.ToRotation();

            float width = MathHelper.Lerp(22f, 38f, Ramp);
            DelegateMethods.v3_1 = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.24f) % 1f, 0.9f, 0.6f).ToVector3() * MathHelper.Lerp(0.35f, 0.82f, Ramp);
            Utils.PlotTileLine(Projectile.Center, target.Center, width, DelegateMethods.CastLight);
            EmitStreamParticles(target.Center);
        }

        private bool ValidateOwnerAndTarget(out NewLegendPristineFuryHoldOut holdout, out NPC target)
        {
            holdout = null;
            target = null;

            if (!Main.projectile.IndexInRange(HoldoutIndex) || !Main.projectile[HoldoutIndex].active || Main.projectile[HoldoutIndex].ModProjectile is not NewLegendPristineFuryHoldOut foundHoldout || foundHoldout.CurrentMark != PristineFuryMark.Empress)
                return false;

            if (!Main.npc.IndexInRange(TargetIndex) || !Main.npc[TargetIndex].active || !Main.npc[TargetIndex].CanBeChasedBy())
                return false;

            holdout = foundHoldout;
            target = Main.npc[TargetIndex];
            return true;
        }

        private void EmitStreamParticles(Vector2 end)
        {
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 3; i++)
            {
                float completion = Main.rand.NextFloat();
                Vector2 point = Vector2.Lerp(Projectile.Center, end, completion) + Main.rand.NextVector2Circular(8f, 8f);
                Color color = GetStreamColor(completion);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    point,
                    direction * Main.rand.NextFloat(0.2f, 1.2f),
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    Main.rand.Next(8, 14),
                    Main.rand.NextFloat(0.1f, 0.18f),
                    color,
                    new Vector2(0.35f, MathHelper.Lerp(1.5f, 2.4f, Ramp)),
                    true,
                    true,
                    glowOpacity: MathHelper.Lerp(0.42f, 0.78f, Ramp)));
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    end + Main.rand.NextVector2Circular(18f, 18f),
                    Main.rand.NextVector2Circular(2.4f, 2.4f),
                    false,
                    Main.rand.Next(12, 18),
                    Main.rand.NextFloat(0.36f, 0.62f),
                    Color.Lerp(GetStreamColor(0.92f), Color.White, 0.25f + Ramp * 0.35f)));
            }
        }

        private Color GetStreamColor(float completion)
        {
            float hue = (Main.GlobalTimeWrappedHourly * 0.28f + completion * 0.62f + Ramp * 0.08f) % 1f;
            Color rainbow = Main.hslToRgb(hue, 0.92f, MathHelper.Lerp(0.46f, 0.62f, Ramp));
            return Color.Lerp(rainbow, Color.White, Ramp * 0.32f);
        }

        public override bool? CanHitNPC(NPC target) => target.whoAmI == TargetIndex ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float width = MathHelper.Lerp(22f, 38f, Ramp);
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center, BeamEnd, width, ref collisionPoint);
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 1.5f, Ramp);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!Main.npc.IndexInRange(TargetIndex) || !Main.npc[TargetIndex].active)
                return false;

            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/ThinEndedLine").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 start = Projectile.Center - Main.screenPosition;
            Vector2 end = BeamEnd - Main.screenPosition;
            Vector2 direction = (end - start).SafeNormalize(Vector2.UnitX);
            float length = Vector2.Distance(start, end);
            float rotation = direction.ToRotation() - MathHelper.PiOver2;
            float width = MathHelper.Lerp(1.0f, 1.75f, Ramp);

            PFLeftEffectRules.BeginAdditive();
            for (int i = 0; i < 7; i++)
            {
                float completion = i / 6f;
                Color color = (GetStreamColor(completion) with { A = 0 }) * MathHelper.Lerp(0.58f, 0.92f, Ramp);
                Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * MathHelper.Lerp(-10f, 10f, (i % 3) / 2f) * (1f - Ramp * 0.35f);
                Main.EntitySpriteDraw(line, start + direction * (length * 0.5f) + offset, null, color, rotation, new Vector2(line.Width * 0.5f, line.Height * 0.5f), new Vector2(width, length / line.Height), SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, start, null, (GetStreamColor(0f) with { A = 0 }) * 0.64f, 0f, bloom.Size() * 0.5f, 0.22f + Ramp * 0.1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (GetStreamColor(1f) with { A = 0 }) * 0.86f, 0f, bloom.Size() * 0.5f, 0.32f + Ramp * 0.16f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
