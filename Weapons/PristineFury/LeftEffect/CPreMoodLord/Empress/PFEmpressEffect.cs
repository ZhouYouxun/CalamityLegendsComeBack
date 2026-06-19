using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFEmpressEffect
    {
        private const float SearchRange = 1500f;
        private const float MaxTargetLeanAngle = MathHelper.Pi / 36f;

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
                GetLimitedTargetDirection(holdout.AimDirection, muzzle, target.Center),
                streamType,
                holdout.GetScaledDamage(0.58f),
                holdout.Projectile.knockBack * 0.45f,
                holdout.Projectile.owner,
                holdout.Projectile.whoAmI,
                target.whoAmI);
            PFLeftEffectRules.ApplyTheme(stream, holdout.CurrentMark);
            EmitHoldEffects(holdout, muzzle, true);
        }

        internal static Vector2 GetLimitedTargetDirection(Vector2 forward, Vector2 origin, Vector2 targetCenter)
        {
            Vector2 baseDirection = forward.SafeNormalize(Vector2.UnitX);
            Vector2 targetDirection = (targetCenter - origin).SafeNormalize(baseDirection);
            float angleOffset = MathHelper.WrapAngle(targetDirection.ToRotation() - baseDirection.ToRotation());
            angleOffset = MathHelper.Clamp(angleOffset, -MaxTargetLeanAngle, MaxTargetLeanAngle);
            return baseDirection.RotatedBy(angleOffset);
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
        private const float StreamLength = 1100f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private int HoldoutIndex => (int)Projectile.ai[0];
        private int TargetIndex => (int)Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private float Ramp => MathHelper.Clamp(Timer / RampFrames, 0f, 1f);
        private Vector2 BeamEnd => Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * StreamLength;

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
            Projectile.velocity = PFEmpressEffect.GetLimitedTargetDirection(holdout.AimDirection, muzzle, target.Center);
            Projectile.timeLeft = 2;
            Projectile.rotation = Projectile.velocity.ToRotation();

            float width = MathHelper.Lerp(24f, 42f, Ramp);
            DelegateMethods.v3_1 = Main.hslToRgb((Main.GlobalTimeWrappedHourly * 0.24f) % 1f, 0.9f, 0.6f).ToVector3() * MathHelper.Lerp(0.38f, 0.95f, Ramp);
            Utils.PlotTileLine(Projectile.Center, BeamEnd, width, DelegateMethods.CastLight);

            EmitStreamParticles(BeamEnd);
            SpawnScatterBolts();
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

        private void SpawnScatterBolts()
        {
            if (Main.dedServ || Projectile.owner != Main.myPlayer)
                return;

            if (Timer % 4 != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int boltType = ModContent.ProjectileType<PFEmpress_ScatterBolt>();
            int boltDamage = Math.Max(1, (int)(Projectile.damage * 0.22f));

            for (int i = 0; i < 3; i++)
            {
                float boltHue = (Main.GlobalTimeWrappedHourly * 0.24f + i * 0.26f + Main.rand.NextFloat(0.08f)) % 1f;
                float t = Main.rand.NextFloat(0.08f, 0.88f);
                Vector2 boltSpawn = Vector2.Lerp(Projectile.Center, BeamEnd, t);
                Vector2 boltVel = direction.RotatedByRandom(MathHelper.Pi / 3.2f) * Main.rand.NextFloat(11f, 19f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    boltSpawn,
                    boltVel,
                    boltType,
                    boltDamage,
                    Projectile.knockBack * 0.12f,
                    Projectile.owner,
                    boltHue);
            }
        }

        private void EmitStreamParticles(Vector2 end)
        {
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            for (int i = 0; i < 4; i++)
            {
                float completion = Main.rand.NextFloat();
                Vector2 point = Vector2.Lerp(Projectile.Center, end, completion) + Main.rand.NextVector2Circular(10f, 10f);
                Color color = GetStreamColor(completion);
                GeneralParticleHandler.SpawnParticle(new CustomSpark(
                    point,
                    direction * Main.rand.NextFloat(0.3f, 1.5f),
                    "CalamityMod/Particles/ThinEndedLine",
                    false,
                    Main.rand.Next(7, 13),
                    Main.rand.NextFloat(0.1f, 0.22f),
                    color,
                    new Vector2(0.38f, MathHelper.Lerp(1.6f, 2.6f, Ramp)),
                    true,
                    true,
                    glowOpacity: MathHelper.Lerp(0.45f, 0.85f, Ramp)));
            }

            if (Main.rand.NextBool(2))
            {
                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    end + Main.rand.NextVector2Circular(22f, 22f),
                    Main.rand.NextVector2Circular(3f, 3f),
                    false,
                    Main.rand.Next(12, 20),
                    Main.rand.NextFloat(0.42f, 0.72f),
                    Color.Lerp(GetStreamColor(0.92f), Color.White, 0.28f + Ramp * 0.38f)));
            }

            if (Timer % 8f == 0f)
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(end, Vector2.Zero, GetStreamColor(Main.rand.NextFloat()) * 0.72f, Vector2.One, 0f, 0.04f, MathHelper.Lerp(0.18f, 0.45f, Ramp), 10));
        }

        private Color GetStreamColor(float completion)
        {
            float hue = (Main.GlobalTimeWrappedHourly * 0.28f + completion * 0.62f + Ramp * 0.08f) % 1f;
            Color rainbow = Main.hslToRgb(hue, 0.92f, MathHelper.Lerp(0.46f, 0.65f, Ramp));
            return Color.Lerp(rainbow, Color.White, Ramp * 0.32f);
        }

        public override bool? CanHitNPC(NPC target) => null;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            float width = MathHelper.Lerp(24f, 42f, Ramp);
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
            float pulse = 0.92f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 9f);

            float outerW = MathHelper.Lerp(1.5f, 3.0f, Ramp) * pulse;
            float midW = MathHelper.Lerp(1.0f, 1.9f, Ramp) * pulse;
            float coreW = MathHelper.Lerp(0.3f, 0.55f, Ramp) * pulse;

            PFLeftEffectRules.BeginAdditive();

            // 外层宽彩虹光晕：5条横向偏移
            for (int i = 0; i < 5; i++)
            {
                float t = i / 4f;
                Color glow = (GetStreamColor(t) with { A = 0 }) * MathHelper.Lerp(0.28f, 0.52f, Ramp);
                float perpDist = MathHelper.Lerp(-22f, 22f, t) * (1f - Ramp * 0.45f);
                Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * perpDist;
                Main.EntitySpriteDraw(line, start + direction * length * 0.5f + offset, null, glow, rotation,
                    new Vector2(line.Width * 0.5f, line.Height * 0.5f), new Vector2(outerW * 0.75f, length / line.Height), SpriteEffects.None, 0);
            }

            // 中层：3条更亮更窄
            for (int i = 0; i < 3; i++)
            {
                float t = i / 2f;
                Color mid = (GetStreamColor(t) with { A = 0 }) * MathHelper.Lerp(0.55f, 0.92f, Ramp);
                float perpDist = MathHelper.Lerp(-7f, 7f, t);
                Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * perpDist;
                Main.EntitySpriteDraw(line, start + direction * length * 0.5f + offset, null, mid, rotation,
                    new Vector2(line.Width * 0.5f, line.Height * 0.5f), new Vector2(midW, length / line.Height), SpriteEffects.None, 0);
            }

            // 核心亮白线
            Color core = (Color.Lerp(GetStreamColor(0.5f), Color.White, 0.65f) with { A = 0 }) * (0.9f + Ramp * 0.4f) * pulse;
            Main.EntitySpriteDraw(line, start + direction * length * 0.5f, null, core, rotation,
                new Vector2(line.Width * 0.5f, line.Height * 0.5f), new Vector2(coreW, length / line.Height), SpriteEffects.None, 0);

            // 起点发光
            Color startBloom = (GetStreamColor(0f) with { A = 0 }) * (0.88f + Ramp * 0.45f);
            Main.EntitySpriteDraw(bloom, start, null, startBloom, 0f, bloom.Size() * 0.5f, 0.32f + Ramp * 0.20f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, start, null, (Color.White with { A = 0 }) * (0.6f + Ramp * 0.3f), 0f, bloom.Size() * 0.5f, 0.15f + Ramp * 0.08f, SpriteEffects.None, 0);

            // 终点强发光
            Color endBloom = (GetStreamColor(1f) with { A = 0 }) * (1.1f + Ramp * 0.5f);
            Main.EntitySpriteDraw(bloom, end, null, endBloom, 0f, bloom.Size() * 0.5f, 0.5f + Ramp * 0.28f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, end, null, (Color.White with { A = 0 }) * (0.75f + Ramp * 0.35f), 0f, bloom.Size() * 0.5f, 0.22f + Ramp * 0.10f, SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PFEmpress_ScatterBolt : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private Color BoltColor => Main.hslToRgb(Projectile.ai[0] % 1f, 0.92f, 0.62f);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 72;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            Projectile.velocity *= 0.972f;
            Lighting.AddLight(Projectile.Center, BoltColor.ToVector3() * 0.28f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.dedServ)
                return;

            Color c = BoltColor;
            for (int i = 0; i < 6; i++)
            {
                Dust d = Dust.NewDustPerfect(target.Center + Main.rand.NextVector2Circular(8f, 8f), DustID.RainbowMk2,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 5f), 0, c, 0.85f);
                d.noGravity = true;
                d.color = c;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/SmallGreyscaleCircle").Value;
            float opacity = Utils.GetLerpValue(0f, 8f, 72f - Projectile.timeLeft, true) * Utils.GetLerpValue(0f, 14f, Projectile.timeLeft, true);

            PFLeftEffectRules.BeginAdditive();

            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float fade = 1f - i / (float)Projectile.oldPos.Length;
                float hueShift = (Projectile.ai[0] + i * 0.05f) % 1f;
                Color color = (Main.hslToRgb(hueShift, 0.92f, 0.62f) with { A = 0 }) * fade * opacity * 0.82f;
                Vector2 drawPos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(tex, drawPos, null, color, 0f, tex.Size() * 0.5f, fade * 0.36f, SpriteEffects.None, 0);
            }

            Vector2 center = Projectile.Center - Main.screenPosition;
            Color boltCol = (BoltColor with { A = 0 }) * opacity;
            Main.EntitySpriteDraw(tex, center, null, boltCol, 0f, tex.Size() * 0.5f, 0.24f * opacity, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex, center, null, (Color.White with { A = 0 }) * opacity * 0.52f, 0f, tex.Size() * 0.5f, 0.11f * opacity, SpriteEffects.None, 0);

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
