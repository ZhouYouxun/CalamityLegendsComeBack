using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingDetonationBlast : ModProjectile, ILocalizedModType
    {
        private bool  initialized;
        private float blastRadius;

        private int Stacks => Math.Max(1, (int)Projectile.ai[0]);
        // ai[1] = grade * 10 + mode
        private int Grade => (int)Projectile.ai[1] / 10;
        private int Mode  => (int)Projectile.ai[1] % 10;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width          = 20;
            Projectile.height         = 20;
            Projectile.friendly       = true;
            Projectile.DamageType     = DamageClass.Ranged;
            Projectile.penetrate      = -1;
            Projectile.timeLeft       = 30;
            Projectile.tileCollide    = false;
            Projectile.ignoreWater    = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = -1;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 22;

        public override void AI()
        {
            if (!initialized)
                InitializeBlast();

            Projectile.velocity = Vector2.Zero;
            int   grade     = Grade;
            Color glowColor = grade >= 1 ? SeasSearingPalette.GradeColor(grade) : SeasSearingPalette.RadioactiveCyan;
            float completion = 1f - Projectile.timeLeft / 30f;
            Lighting.AddLight(Projectile.Center, Color.Lerp(glowColor, SeasSearingPalette.ToxicGreen, completion * 0.5f).ToVector3() * (0.35f + completion * 0.5f));

            if (Projectile.timeLeft == 22 && Main.myPlayer == Projectile.owner && Stacks >= 18)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center, Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                    Math.Max(1, Projectile.damage / 5),
                    0f, Projectile.owner,
                    Math.Min(Stacks, 80));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int mode  = Mode;
            int grade = Grade;
            modifiers.DefenseEffectiveness    *= mode == 0 ? 0.45f : 0.08f;
            modifiers.ScalingArmorPenetration += mode == 0 ? 0.12f : 0.38f;

            if (mode == 1)
                modifiers.FinalDamage *= 1f + MathHelper.Clamp(Stacks / 140f, 0f, 0.8f);
            else if (mode == 2)
                modifiers.FinalDamage *= 1.22f;

            if (grade >= 3)
                modifiers.FinalDamage *= 1f + (grade - 2) * 0.12f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 360);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 420);

            int grade = Grade;
            if (grade >= 2)
                target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, grade * 3, 10 * 60, fromSpread: true);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!initialized)
                InitializeBlast();

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring  = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            float completion = 1f - Projectile.timeLeft / 30f;
            float opacity    = (float)Math.Sin(completion * MathHelper.Pi);
            Vector2 center   = Projectile.Center - Main.screenPosition;

            int   grade      = Grade;
            int   mode       = Mode;
            Color gradeColor = grade >= 1 ? SeasSearingPalette.GradeColor(grade) : SeasSearingPalette.RadioactiveCyan;

            Color outer = (SeasSearingPalette.DeepBlue with { A = 0 }) * opacity;
            Color inner = (gradeColor with { A = 0 }) * opacity;
            Color toxic = (SeasSearingPalette.ToxicGreen with { A = 0 }) * opacity;
            float scale = blastRadius / bloom.Width * (0.65f + completion * 0.85f);

            Main.EntitySpriteDraw(bloom, center, null, outer * 0.72f, 0f, bloom.Size() * 0.5f, scale * 2.1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, inner * 0.58f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(scale * 1.1f, scale * 0.74f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring,  center, null, toxic * 0.8f,  Main.GlobalTimeWrappedHourly * 1.6f, ring.Size() * 0.5f, scale * 1.9f, SpriteEffects.None, 0);

            if (mode > 0)
                Main.EntitySpriteDraw(ring, center, null, (Color.White with { A = 0 }) * opacity * 0.44f, -Main.GlobalTimeWrappedHourly * 2.1f, ring.Size() * 0.5f, scale * 0.78f, SpriteEffects.None, 0);

            if (grade >= 3)
                Main.EntitySpriteDraw(ring, center, null, (gradeColor with { A = 0 }) * opacity * 0.62f, Main.GlobalTimeWrappedHourly * 2.8f, ring.Size() * 0.5f, scale * 2.6f, SpriteEffects.None, 0);

            return false;
        }

        private void InitializeBlast()
        {
            initialized = true;
            int   mode       = Mode;
            int   grade      = Grade;
            float gradeBonus = grade >= 1 ? grade * 18f : 0f;
            blastRadius      = MathHelper.Clamp(94f + Stacks * 3.4f + gradeBonus, 130f, mode == 0 ? 390f : 520f);
            if (mode == 2)
                blastRadius  = MathHelper.Clamp(blastRadius + 120f, 260f, 650f);

            Vector2 center   = Projectile.Center;
            Projectile.width = Projectile.height = Math.Max(12, (int)(blastRadius * 2f));
            Projectile.Center = center;
            SeasSearingVisualUtility.SpawnAbyssDust(center, mode == 0 ? 38 : 70, mode == 0 ? 6f : 9f, Math.Min(blastRadius * 0.28f, 80f), mode == 0 ? 1f : 1.35f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = mode == 0 ? 0.56f : 0.86f, Pitch = mode == 0 ? -0.3f : -0.58f }, center);

            if (grade >= 2)
                SeasSearingVisualUtility.SpawnGradeBurst(center, grade, 18 + grade * 6);
        }
    }
}
