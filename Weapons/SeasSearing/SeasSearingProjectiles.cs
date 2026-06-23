using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Buffs.StatDebuffs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SeasSearing
{
    internal sealed class SeasSearingPollutionRound : ModProjectile, ILocalizedModType
    {
        private static readonly Color CoreColor = new(170, 255, 238);
        private static readonly Color TrailColor = new(26, 128, 190);

        private int Stage => (int)Projectile.ai[1];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 18;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 540;
            Projectile.extraUpdates = 6;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 18;
        }

        public override void AI()
        {
            // Initialize penetrate from stage on first tick
            if (Projectile.localAI[0] == 0f)
            {
                int stage = Stage;
                Projectile.penetrate = stage >= 3 ? 4 : (stage >= 1 ? 3 : 2);
                Projectile.localAI[1] = Main.rand.NextFloat(0.82f, 1.38f);
                Projectile.netUpdate = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.direction;

            int stage2 = Stage;
            Lighting.AddLight(Projectile.Center, stage2 >= 3
                ? new Vector3(0.09f, 0.28f, 0.14f)
                : new Vector3(0.05f, 0.22f, 0.24f));

            if (Projectile.localAI[0]++ < 2f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            bool emitDust = stage2 >= 2 ? Main.rand.NextBool() : Main.rand.NextBool(2);
            if (emitDust)
            {
                Color dustColor;
                int dustType;
                if (stage2 >= 3)
                {
                    dustColor = Color.Lerp(SeasSearingPalette.BiohazardLime, SeasSearingPalette.ToxicGreen, Main.rand.NextFloat());
                    dustType = Main.rand.NextBool(3) ? DustID.Vortex : 89;
                }
                else if (stage2 == 2)
                {
                    dustColor = Main.rand.NextBool() ? CoreColor : SeasSearingPalette.BiohazardLime;
                    dustType = Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald;
                }
                else
                {
                    dustColor = Main.rand.NextBool() ? CoreColor : SeasSearingPalette.ToxicGreen;
                    dustType = Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald;
                }

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    dustType,
                    -direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(0.6f, 1.9f),
                    110,
                    dustColor,
                    Main.rand.NextFloat(0.45f, 0.78f + stage2 * 0.06f));
                dust.noGravity = true;
            }

            if (!Main.dedServ && Projectile.localAI[1] > 1.18f && Main.rand.NextBool(5))
            {
                Color sparkColor = stage2 >= 3 ? SeasSearingPalette.BiohazardLime : CoreColor;
                Dust spark = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(3.5f, 3.5f),
                    DustID.GemDiamond,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    90,
                    sparkColor,
                    Main.rand.NextFloat(0.5f, 0.88f));
                spark.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float distance = Vector2.Distance(Main.player[Projectile.owner].Center, target.Center);
            float precisionBonus = 1f + Utils.GetLerpValue(380f, 1050f, distance, true) * 0.28f;
            modifiers.FinalDamage *= precisionBonus;
            modifiers.ScalingArmorPenetration += 0.08f;
            if (Projectile.localAI[1] > 1.2f)
                modifiers.FinalDamage *= 1f + (Projectile.localAI[1] - 1.2f) * 0.55f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int stackAmount = hit.Crit ? 6 : 4;
            if (target.boss || target.realLife >= 0)
                stackAmount += 1;
            if (Stage >= 2)
                stackAmount += 1;
            if (Stage >= 3)
                stackAmount += 1;

            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, stackAmount);
            target.AddBuff(BuffID.Venom, 240);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
            SpawnImpact(target.Center, true);
        }

        public override void OnKill(int timeLeft)
        {
            if (Projectile.numHits <= 0)
                SpawnImpact(Projectile.Center, false);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            float velRot = Projectile.velocity.ToRotation();
            float rawBoost = Projectile.localAI[1];
            float boost = rawBoost > 0f ? MathHelper.Clamp(rawBoost, 0.82f, 1.38f) : 1f;

            int stage = Stage;
            Color headColor = stage >= 3 ? SeasSearingPalette.BiohazardLime : CoreColor;
            Color tailColor = stage >= 2
                ? Color.Lerp(TrailColor, SeasSearingPalette.ToxicGreen, 0.55f)
                : TrailColor;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = Color.Lerp(tailColor, headColor, completion) * (0.08f + completion * 0.42f * boost);
                color.A = 0;

                Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.72f, velRot, bloomOrigin, new Vector2(0.12f, 0.038f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, (headColor with { A = 0 }) * (0.6f * boost), velRot, bloomOrigin, new Vector2(0.15f, 0.045f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        private static void SpawnImpact(Vector2 center, bool hitTarget)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(center, hitTarget ? 18 : 10, hitTarget ? 5.2f : 2.8f, 5f, hitTarget ? 1.05f : 0.75f);
            SoundEngine.PlaySound(SoundID.Item10 with { Volume = hitTarget ? 0.24f : 0.14f, Pitch = -0.15f }, center);
        }
    }

    internal sealed class SeasSearingResonancePulse : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 34;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.velocity *= 0.98f;
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.RadioactiveCyan.ToVector3() * 0.3f);

            if (Projectile.timeLeft % 4 == 0)
            {
                float completion = 1f - Projectile.timeLeft / (float)Lifetime;
                SeasSearingVisualUtility.SpawnPressureRing(
                    Projectile.Center,
                    3.4f + completion * 3.8f,
                    12f + completion * 75f,
                    28,
                    Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, completion));
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float completion = 1f - Projectile.timeLeft / (float)Lifetime;
            float opacity = (float)Math.Sin(completion * MathHelper.Pi);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color color = (Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.RadioactiveCyan, completion) with { A = 0 }) * opacity;

            Main.EntitySpriteDraw(bloom, center, null, color * 0.28f, 0f, bloom.Size() * 0.5f, new Vector2(0.35f + completion * 1.2f, 0.18f + completion * 0.35f), SpriteEffects.None, 0);
            for (int i = 0; i < 3; i++)
            {
                float scale = 0.22f + completion * (0.72f + i * 0.32f);
                Main.EntitySpriteDraw(ring, center, null, color * (0.72f - i * 0.16f), Projectile.rotation + Main.GlobalTimeWrappedHourly * (0.6f + i * 0.2f), ring.Size() * 0.5f, scale, SpriteEffects.None, 0);
            }

            return false;
        }
    }

    internal sealed class SeasSearingDetonationBlast : ModProjectile, ILocalizedModType
    {
        private bool initialized;
        private float blastRadius;

        private int Stacks => Math.Max(1, (int)Projectile.ai[0]);
        // ai[1] = grade * 10 + mode
        private int Grade => (int)Projectile.ai[1] / 10;
        private int Mode => (int)Projectile.ai[1] % 10;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 20;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 30;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => Projectile.timeLeft >= 22;

        public override void AI()
        {
            if (!initialized)
                InitializeBlast();

            Projectile.velocity *= 0f;
            int grade = Grade;
            Color glowColor = grade >= 1 ? SeasSearingPalette.GradeColor(grade) : SeasSearingPalette.RadioactiveCyan;
            float completion = 1f - Projectile.timeLeft / 30f;
            Lighting.AddLight(Projectile.Center, Color.Lerp(glowColor, SeasSearingPalette.ToxicGreen, completion * 0.5f).ToVector3() * (0.35f + completion * 0.5f));

            if (Projectile.timeLeft == 22 && Main.myPlayer == Projectile.owner && Stacks >= 18)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                    Math.Max(1, Projectile.damage / 5),
                    0f,
                    Projectile.owner,
                    Math.Min(Stacks, 80));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            int mode = Mode;
            int grade = Grade;
            modifiers.DefenseEffectiveness *= mode == 0 ? 0.45f : 0.08f;
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
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            float completion = 1f - Projectile.timeLeft / 30f;
            float opacity = (float)Math.Sin(completion * MathHelper.Pi);
            Vector2 center = Projectile.Center - Main.screenPosition;

            int grade = Grade;
            Color gradeColor = grade >= 1 ? SeasSearingPalette.GradeColor(grade) : SeasSearingPalette.RadioactiveCyan;
            int mode = Mode;

            Color outer = (SeasSearingPalette.DeepBlue with { A = 0 }) * opacity;
            Color inner = (gradeColor with { A = 0 }) * opacity;
            Color toxic = (SeasSearingPalette.ToxicGreen with { A = 0 }) * opacity;
            float scale = blastRadius / bloom.Width * (0.65f + completion * 0.85f);

            Main.EntitySpriteDraw(bloom, center, null, outer * 0.72f, 0f, bloom.Size() * 0.5f, scale * 2.1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, inner * 0.58f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(scale * 1.1f, scale * 0.74f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, toxic * 0.8f, Main.GlobalTimeWrappedHourly * 1.6f, ring.Size() * 0.5f, scale * 1.9f, SpriteEffects.None, 0);

            if (mode > 0)
                Main.EntitySpriteDraw(ring, center, null, (Color.White with { A = 0 }) * opacity * 0.44f, -Main.GlobalTimeWrappedHourly * 2.1f, ring.Size() * 0.5f, scale * 0.78f, SpriteEffects.None, 0);

            if (grade >= 3)
                Main.EntitySpriteDraw(ring, center, null, (gradeColor with { A = 0 }) * opacity * 0.62f, Main.GlobalTimeWrappedHourly * 2.8f, ring.Size() * 0.5f, scale * 2.6f, SpriteEffects.None, 0);

            return false;
        }

        private void InitializeBlast()
        {
            initialized = true;
            int mode = Mode;
            int grade = Grade;
            float gradeBonus = grade >= 1 ? grade * 18f : 0f;
            blastRadius = MathHelper.Clamp(94f + Stacks * 3.4f + gradeBonus, 130f, mode == 0 ? 390f : 520f);
            if (mode == 2)
                blastRadius = MathHelper.Clamp(blastRadius + 120f, 260f, 650f);

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = Math.Max(12, (int)(blastRadius * 2f));
            Projectile.Center = center;
            SeasSearingVisualUtility.SpawnAbyssDust(center, mode == 0 ? 38 : 70, mode == 0 ? 6f : 9f, Math.Min(blastRadius * 0.28f, 80f), mode == 0 ? 1f : 1.35f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = mode == 0 ? 0.56f : 0.86f, Pitch = mode == 0 ? -0.3f : -0.58f }, center);

            if (grade >= 2)
                SeasSearingVisualUtility.SpawnGradeBurst(center, grade, 18 + grade * 6);
        }
    }

    internal sealed class SeasSearingFalloutCloud : ModProjectile, ILocalizedModType
    {
        private float Radius => MathHelper.Clamp(Projectile.ai[0] * 5f + 120f, 140f, 520f);

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 220;
            Projectile.height = 220;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 24;
        }

        public override void AI()
        {
            Vector2 center = Projectile.Center;
            int size = (int)(Radius * 2f);
            Projectile.width = Projectile.height = size;
            Projectile.Center = center;
            Projectile.damage = Math.Max(1, (int)(Projectile.damage * 0.998f));

            if (Main.rand.NextBool(3))
            {
                Vector2 offset = Main.rand.NextVector2Circular(Radius * 0.85f, Radius * 0.55f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset,
                    Main.rand.NextBool(2) ? DustID.Smoke : DustID.GemEmerald,
                    Main.rand.NextVector2Circular(0.8f, 0.8f) - Vector2.UnitY * Main.rand.NextFloat(0.15f, 0.65f),
                    150,
                    Main.rand.NextBool(3) ? SeasSearingPalette.FalloutAsh : SeasSearingPalette.ToxicGreen,
                    Main.rand.NextFloat(0.55f, 1.05f));
                dust.noGravity = true;
            }

            Lighting.AddLight(Projectile.Center, SeasSearingPalette.DeepBlue.ToVector3() * 0.12f);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 2, 8 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 70f, 0f, 1f) * 0.55f;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color deep = (SeasSearingPalette.AbyssBlack with { A = 0 }) * opacity;
            Color toxic = (SeasSearingPalette.ToxicGreen with { A = 0 }) * opacity;

            Main.EntitySpriteDraw(bloom, center, null, deep * 0.8f, 0f, bloom.Size() * 0.5f, new Vector2(Radius / bloom.Width * 2.2f, Radius / bloom.Height * 1.35f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, toxic * 0.18f, Main.GlobalTimeWrappedHourly, bloom.Size() * 0.5f, new Vector2(Radius / bloom.Width * 1.4f, Radius / bloom.Height * 0.9f), SpriteEffects.None, 0);
            return false;
        }
    }

    // Spawned on death of a polluted NPC; floats in place and inflicts pollution on contact
    internal sealed class SSDeathPollutionCloud : ModProjectile, ILocalizedModType
    {
        private const int FrameCount = 10;

        private int Stacks => (int)Projectile.ai[0];
        private int Duration => (int)Projectile.ai[1];

        private int CloudGrade
        {
            get
            {
                int s = Stacks;
                if (s >= 150) return 5;
                if (s >= 95) return 4;
                if (s >= 55) return 3;
                if (s >= 28) return 2;
                if (s >= 10) return 1;
                return 0;
            }
        }

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/Boss/SandPoisonCloudOldDuke";

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = FrameCount;
        }

        public override void SetDefaults()
        {
            Projectile.width = 70;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 360;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 30;
        }

        public override void AI()
        {
            // Set size and duration on first frame
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                int grade = CloudGrade;
                float radius = MathHelper.Clamp(50f + grade * 22f + Stacks * 0.5f, 65f, 180f);
                Vector2 center = Projectile.Center;
                Projectile.width = Projectile.height = (int)(radius * 2f);
                Projectile.Center = center;
                Projectile.timeLeft = Math.Max(Duration, 60);
                Projectile.netUpdate = true;
            }

            // Gentle upward drift
            Projectile.velocity.Y = MathHelper.Lerp(Projectile.velocity.Y, -0.38f, 0.04f);
            Projectile.velocity.X *= 0.98f;

            // Animate
            if (++Projectile.frameCounter >= 4)
            {
                Projectile.frameCounter = 0;
                if (++Projectile.frame >= FrameCount)
                    Projectile.frame = 0;
            }

            float age = Projectile.timeLeft / (float)Math.Max(1, Duration);
            int grade2 = CloudGrade;
            Color gradeColor = grade2 >= 1 ? SeasSearingPalette.GradeColor(grade2) : SeasSearingPalette.ToxicGreen;
            Lighting.AddLight(Projectile.Center, gradeColor.ToVector3() * (0.15f + (1f - age) * 0.1f));

            if (!Main.dedServ && Main.rand.NextBool(4))
            {
                float radius = Projectile.width * 0.4f;
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(radius, radius),
                    grade2 >= 3 ? 89 : DustID.GemEmerald,
                    (-Vector2.UnitY + Main.rand.NextVector2Circular(0.5f, 0.5f)) * Main.rand.NextFloat(0.4f, 1.2f),
                    140,
                    Color.Lerp(gradeColor, SeasSearingPalette.DeepBlue, Main.rand.NextFloat(0.2f, 0.6f)),
                    Main.rand.NextFloat(0.55f, 0.95f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int grade = CloudGrade;
            int amount = Math.Max(1, grade * 2 + Stacks / 20);
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 12 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
            if (grade >= 3)
                target.AddBuff(BuffID.Venom, 180);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            int frameH = texture.Height / FrameCount;
            Rectangle frame = new(0, Projectile.frame * frameH, texture.Width, frameH);
            Vector2 origin = frame.Size() * 0.5f;

            int grade = CloudGrade;
            Color gradeColor = grade >= 1 ? SeasSearingPalette.GradeColor(grade) : SeasSearingPalette.ToxicGreen;
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 60f, 0f, 1f) * MathHelper.Clamp(1f - (Projectile.timeLeft - 30f) / 30f, 0f, 1f);
            opacity = MathHelper.Clamp(opacity, 0f, 0.85f);

            float scale = Projectile.width / (float)(frameH * 2);
            Color drawColor = Color.Lerp(Color.White, gradeColor, 0.55f) * opacity;

            for (int i = 0; i < 3; i++)
            {
                float layerScale = scale * (0.85f + i * 0.12f);
                float layerRot = Main.GlobalTimeWrappedHourly * (0.2f + i * 0.15f) * (i % 2 == 0 ? 1f : -1f);
                Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, drawColor * (1f - i * 0.22f), layerRot, origin, layerScale, SpriteEffects.None, 0);
            }

            return false;
        }
    }

    // Spawned on grade-3+ detonation in radial burst; flies outward and inflicts pollution
    internal sealed class SSPollutionSpike : ModProjectile, ILocalizedModType
    {
        private int SpikeGrade => (int)Projectile.ai[1];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/Boss/OldDukeToothBallSpike";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 26;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 220;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.16f, 14f);

            int grade = SpikeGrade;
            Color spikeColor = SeasSearingPalette.GradeColor(grade);
            Lighting.AddLight(Projectile.Center, spikeColor.ToVector3() * 0.22f);

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    89,
                    -Projectile.velocity * 0.1f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120,
                    spikeColor,
                    Main.rand.NextFloat(0.45f, 0.8f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int grade = SpikeGrade;
            int amount = Math.Clamp(grade * 3, 4, 14);
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 10 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
        }

        public override void OnKill(int timeLeft)
        {
            SeasSearingVisualUtility.SpawnGradeBurst(Projectile.Center, SpikeGrade, 8);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            int grade = SpikeGrade;
            Color gradeColor = SeasSearingPalette.GradeColor(grade);

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, (gradeColor with { A = 0 }) * 0.5f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.08f, 0.22f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.Lerp(Color.White, gradeColor, 0.42f), Projectile.rotation, origin, Projectile.scale * (0.9f + grade * 0.04f), SpriteEffects.None, 0);
            return false;
        }
    }

    // Spawned on grade-4+ detonation; rotates in area and inflicts heavy pollution
    internal sealed class SSPollutionVortex : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 300;
        private bool initialized;

        private int VortexGrade => (int)Projectile.ai[1];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/Boss/OldDukeVortex";

        public override void SetDefaults()
        {
            Projectile.width = 120;
            Projectile.height = 120;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
        }

        public override void AI()
        {
            if (!initialized)
            {
                initialized = true;
                int grade = VortexGrade;
                float radius = MathHelper.Clamp(70f + grade * 20f, 90f, 160f);
                Vector2 center = Projectile.Center;
                Projectile.width = Projectile.height = (int)(radius * 2f);
                Projectile.Center = center;
                Projectile.netUpdate = true;
            }

            Projectile.velocity = Vector2.Zero;
            Projectile.rotation += 0.028f;

            float age = 1f - Projectile.timeLeft / (float)Lifetime;
            int grade2 = VortexGrade;
            Color vortexColor = SeasSearingPalette.GradeColor(grade2);
            Lighting.AddLight(Projectile.Center, vortexColor.ToVector3() * (0.25f + age * 0.15f));

            // Pull nearby enemies toward center
            float pullRadius = Projectile.width * 1.4f;
            float pullRadiusSq = pullRadius * pullRadius;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || !npc.CanBeChasedBy())
                    continue;
                if (Vector2.DistanceSquared(npc.Center, Projectile.Center) > pullRadiusSq)
                    continue;

                Vector2 toCenter = (Projectile.Center - npc.Center).SafeNormalize(Vector2.Zero);
                float strength = 1f - Vector2.Distance(npc.Center, Projectile.Center) / pullRadius;
                float pull = MathHelper.Lerp(0.12f, 0.32f, strength);
                if (npc.boss || npc.knockBackResist <= 0f)
                    pull *= 0.2f;

                npc.velocity += toCenter * pull;
            }

            // Ambient dust
            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                float r = Projectile.width * 0.45f;
                float angle = Main.rand.NextFloat(MathHelper.TwoPi);
                Vector2 pos = Projectile.Center + angle.ToRotationVector2() * r;
                Vector2 vel = (Projectile.Center - pos).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(0.5f, 2.2f);
                vel = vel.RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloat(-0.3f, 0.3f));
                Dust d = Dust.NewDustPerfect(
                    pos, DustID.Vortex, vel, 120,
                    Color.Lerp(vortexColor, SeasSearingPalette.BiohazardLime, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.55f, 0.9f));
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int grade = VortexGrade;
            int amount = Math.Clamp(grade * 4 + (int)Projectile.ai[0] / 12, 8, 22);
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 14 * 60, fromSpread: true);
            target.AddBuff(BuffID.Venom, 360);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 420);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float age = 1f - Projectile.timeLeft / (float)Lifetime;
            float fadeIn = MathHelper.Clamp(Projectile.timeLeft > Lifetime - 20 ? (Lifetime - Projectile.timeLeft) / 20f : 1f, 0f, 1f);
            float fadeOut = MathHelper.Clamp(Projectile.timeLeft / 40f, 0f, 1f);
            float opacity = fadeIn * fadeOut * 0.82f;

            int grade = VortexGrade;
            Color gradeColor = SeasSearingPalette.GradeColor(grade);
            float drawScale = (Projectile.width / (float)texture.Width) * (0.85f + age * 0.15f);

            Color drawColor = Color.Lerp(gradeColor, SeasSearingPalette.BiohazardLime, age * 0.45f) * opacity;
            drawColor.A = (byte)(40 * opacity);

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor, Projectile.rotation, origin, drawScale, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, drawColor * 0.55f, -Projectile.rotation * 0.7f, origin, drawScale * 0.82f, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class SeasSearingDesignatorBeam : ModProjectile, ILocalizedModType
    {
        private const float BeamLength = 2400f;
        private const int MaxTime = 42;
        private bool markerCreated;
        private Vector2 targetPoint;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.penetrate = -1;
            Projectile.timeLeft = MaxTime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.rotation = direction.ToRotation();
            targetPoint = FindDesignationPoint(Projectile.Center, direction, BeamLength);

            if (!markerCreated && Projectile.timeLeft <= MaxTime - 22 && Main.myPlayer == Projectile.owner)
            {
                markerCreated = true;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    targetPoint,
                    Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingNukeMarker>(),
                    Math.Max(1, owner.GetWeaponDamage(owner.HeldItem) * 18),
                    0f,
                    Projectile.owner);

                SoundEngine.PlaySound(SoundID.Item124 with { Volume = 0.65f, Pitch = -0.4f }, targetPoint);
            }

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Vector2.Lerp(Projectile.Center, targetPoint, Main.rand.NextFloat()),
                    DustID.GemDiamond,
                    -direction * Main.rand.NextFloat(0.2f, 1.1f),
                    120,
                    SeasSearingPalette.RadioactiveCyan,
                    Main.rand.NextFloat(0.45f, 0.8f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Vector2 start = Projectile.Center;
            Vector2 end = targetPoint == Vector2.Zero ? Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength : targetPoint;
            Vector2 line = end - start;
            float length = line.Length();
            if (length <= 2f)
                return false;

            Texture2D pixel = TextureAssets.MagicPixel.Value;
            Vector2 origin = new(0f, 0.5f);
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 12f, 0f, 1f);
            float rotation = line.ToRotation();
            Color cyan = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * opacity;
            Color white = (Color.White with { A = 0 }) * opacity;

            Main.EntitySpriteDraw(pixel, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), cyan * 0.46f, rotation, origin, new Vector2(length, 7f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(pixel, start - Main.screenPosition, new Rectangle(0, 0, 1, 1), white * 0.74f, rotation, origin, new Vector2(length, 2f), SpriteEffects.None, 0);
            return false;
        }

        private Vector2 FindDesignationPoint(Vector2 start, Vector2 direction, float length)
        {
            Vector2 end = start + direction * length;
            NPC bestTarget = null;
            float bestDistance = length;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy())
                    continue;

                float collisionPoint = 0f;
                if (!Collision.CheckAABBvLineCollision(npc.Hitbox.TopLeft(), npc.Hitbox.Size(), start, end, 12f, ref collisionPoint))
                    continue;

                if (collisionPoint < bestDistance)
                {
                    bestDistance = collisionPoint;
                    bestTarget = npc;
                }
            }

            if (bestTarget != null)
                return bestTarget.Center;

            return end;
        }
    }

    internal sealed class SeasSearingNukeMarker : ModProjectile, ILocalizedModType
    {
        private const int AlarmFrames = 300;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.penetrate = -1;
            Projectile.timeLeft = AlarmFrames;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            float completion = 1f - Projectile.timeLeft / (float)AlarmFrames;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.WarningOrange, completion).ToVector3() * 0.42f);

            if (Projectile.timeLeft % 60 == 0)
            {
                SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.65f, Pitch = MathHelper.Lerp(-0.55f, 0.18f, completion) }, Projectile.Center);
                SeasSearingVisualUtility.ShakeAt(Projectile.Center, 1.4f + completion * 2.2f, 2200f);
            }

            if (Projectile.timeLeft % 9 == 0)
                SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 2f + completion * 3f, 24f + completion * 120f, 20, Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.WarningOrange, completion));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.myPlayer != Projectile.owner)
                return;

            Vector2 spawnPosition = Projectile.Center + new Vector2(Main.rand.NextFloat(-160f, 160f), -1450f);
            Vector2 velocity = (Projectile.Center - spawnPosition).SafeNormalize(Vector2.UnitY) * 24f;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPosition,
                velocity,
                ModContent.ProjectileType<SeasSearingThermonuclearWarhead>(),
                Projectile.damage,
                14f,
                Projectile.owner);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float completion = 1f - Projectile.timeLeft / (float)AlarmFrames;
            float pulse = 0.82f + 0.18f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 12f);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Color warning = (Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.WarningOrange, completion) with { A = 0 }) * (0.65f + completion * 0.35f);

            Main.EntitySpriteDraw(bloom, center, null, warning * 0.38f, 0f, bloom.Size() * 0.5f, 0.22f + completion * 0.38f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, warning * 0.86f, Main.GlobalTimeWrappedHourly * 1.8f, ring.Size() * 0.5f, (0.2f + completion * 1.15f) * pulse, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, (Color.White with { A = 0 }) * 0.34f, -Main.GlobalTimeWrappedHourly * 2.4f, ring.Size() * 0.5f, 0.12f + completion * 0.32f, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class SeasSearingThermonuclearWarhead : ModProjectile, ILocalizedModType
    {
        private bool detonated;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_134";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 34;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 240;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.18f, 34f);
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.WarningOrange.ToVector3() * 0.48f);

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitY);
            for (int i = 0; i < 2; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(12f, 34f) + Main.rand.NextVector2Circular(6f, 6f),
                    Main.rand.NextBool() ? DustID.Torch : DustID.GemEmerald,
                    -direction * Main.rand.NextFloat(1.2f, 4.6f) + Main.rand.NextVector2Circular(0.8f, 0.8f),
                    100,
                    Main.rand.NextBool() ? SeasSearingPalette.WarningOrange : SeasSearingPalette.RadioactiveCyan,
                    Main.rand.NextFloat(0.7f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            TriggerDetonation();
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            if (!detonated)
                TriggerDetonation();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.RadioactiveCyan, completion * 0.55f) * (completion * 0.5f);
                color.A = 0;
                Main.EntitySpriteDraw(bloom, drawPosition, null, color, Projectile.rotation, bloom.Size() * 0.5f, 0.18f + completion * 0.16f, SpriteEffects.None, 0);
            }

            Color armorColor = Color.Lerp(new Color(18, 26, 42), SeasSearingPalette.DeepBlue, 0.35f);
            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, armorColor, Projectile.rotation, origin, Projectile.scale * 1.45f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * 0.45f, Projectile.rotation, bloom.Size() * 0.5f, 0.2f, SpriteEffects.None, 0);
            return false;
        }

        private void TriggerDetonation()
        {
            if (detonated)
                return;

            detonated = true;
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<SeasSearingNuclearDetonation>(),
                    Projectile.damage,
                    Projectile.knockBack,
                    Projectile.owner);
            }

            Projectile.Kill();
        }
    }

    internal sealed class SeasSearingNuclearDetonation : ModProjectile, ILocalizedModType
    {
        private const int Lifetime = 150;
        private bool initialized;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 1200;
            Projectile.height = 1200;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 18;
            Projectile.netImportant = true;
        }

        public override bool? CanDamage() => Projectile.timeLeft > 45;

        public override void AI()
        {
            if (!initialized)
                InitializeNuke();

            int age = Lifetime - Projectile.timeLeft;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.RadioactiveCyan, MathHelper.Clamp(age / 80f, 0f, 1f)).ToVector3() * 1.4f);

            if (age % 7 == 0)
                SpawnNuclearDust(age);

            if (Main.myPlayer == Projectile.owner && age > 45 && age % 10 == 0)
                SpawnFalloutRain();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int amount = target.boss || target.realLife >= 0 ? 92 : 55;
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, amount, 24 * 60);
            target.AddBuff(BuffID.Venom, 600);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 720);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            float age = Lifetime - Projectile.timeLeft;
            float shock = MathHelper.Clamp(age / 42f, 0f, 1f);
            float collapse = MathHelper.Clamp((age - 42f) / 38f, 0f, 1f);
            float fallout = MathHelper.Clamp((age - 82f) / 60f, 0f, 1f);
            float fade = MathHelper.Clamp(Projectile.timeLeft / 34f, 0f, 1f);
            Vector2 center = Projectile.Center - Main.screenPosition;

            Color white = (Color.White with { A = 0 }) * fade;
            Color cyan = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * fade;
            Color deep = (SeasSearingPalette.AbyssBlack with { A = 0 }) * fade;
            Color orange = (SeasSearingPalette.WarningOrange with { A = 0 }) * fade;

            float baseScale = 1200f / bloom.Width;
            Main.EntitySpriteDraw(bloom, center, null, white * (1f - shock) * 1.8f, 0f, bloom.Size() * 0.5f, baseScale * (0.8f + shock * 1.3f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, cyan * (0.8f + shock * 0.6f), Main.GlobalTimeWrappedHourly * 1.2f, ring.Size() * 0.5f, 1.1f + shock * 7.5f, SpriteEffects.None, 0);

            if (collapse > 0f)
            {
                Main.EntitySpriteDraw(bloom, center, null, deep * collapse * 1.25f, 0f, bloom.Size() * 0.5f, baseScale * (0.32f + collapse * 0.28f), SpriteEffects.None, 0);
                Main.EntitySpriteDraw(ring, center, null, cyan * collapse * 0.9f, -Main.GlobalTimeWrappedHourly * 3.6f, ring.Size() * 0.5f, 0.9f + collapse * 1.7f, SpriteEffects.None, 0);
            }

            if (fallout > 0f)
                Main.EntitySpriteDraw(bloom, center, null, Color.Lerp(orange, deep, 0.75f) * fallout * 0.7f, 0f, bloom.Size() * 0.5f, baseScale * (1.4f + fallout * 0.75f), SpriteEffects.None, 0);

            return false;
        }

        private void InitializeNuke()
        {
            initialized = true;
            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = 1200;
            Projectile.Center = center;

            SeasSearingVisualUtility.ShakeAt(center, 22f, 3200f);
            SeasSearingVisualUtility.SpawnAbyssDust(center, 150, 18f, 90f, 1.8f);
            SeasSearingVisualUtility.SpawnPressureRing(center, 12f, 24f, 90, Color.White);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = 1f, Pitch = -0.65f }, center);
            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.95f, Pitch = -0.35f }, center);
        }

        private void SpawnNuclearDust(int age)
        {
            int count = age < 45 ? 18 : 11;
            float radius = age < 45 ? 620f : 760f;

            for (int i = 0; i < count; i++)
            {
                Vector2 offset = Main.rand.NextVector2CircularEdge(radius, radius * Main.rand.NextFloat(0.45f, 1f));
                Vector2 velocity = offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2.2f, 7.5f);
                if (age > 50)
                    velocity = -Vector2.UnitY.RotatedByRandom(0.65f) * Main.rand.NextFloat(0.8f, 3.6f);

                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + offset * Main.rand.NextFloat(0.12f, 0.9f),
                    Main.rand.NextBool(3) ? DustID.Smoke : DustID.GemEmerald,
                    velocity,
                    140,
                    Main.rand.NextBool(4) ? SeasSearingPalette.WarningOrange : SeasSearingPalette.PollutionColor(Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.8f, 1.8f));
                dust.noGravity = true;
            }
        }

        private void SpawnFalloutRain()
        {
            Vector2 spawn = Projectile.Center + new Vector2(Main.rand.NextFloat(-760f, 760f), -720f);
            Vector2 velocity = new(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(9f, 16f));
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawn,
                velocity,
                ModContent.ProjectileType<SeasSearingFalloutRain>(),
                Math.Max(1, Projectile.damage / 9),
                1.5f,
                Projectile.owner);
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NEW: SeasSearingPressureBolt
    // Fired on right-click release. Large, fast, pierces 3-5.
    // On hit or timeout: bursts into 8 homing satellite orbs.
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class SeasSearingPressureBolt : ModProjectile, ILocalizedModType
    {
        private static readonly Color HeadColor = new(210, 255, 252);
        private static readonly Color TrailColor = new(30, 100, 200);

        private bool Strong => Projectile.ai[0] > 0f;
        private bool satellitesSpawned;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Terraria.ModLoader.ModContent.ProjectileType<SeasSearingPressureBolt>()] = 22;
            ProjectileID.Sets.TrailingMode[Terraria.ModLoader.ModContent.ProjectileType<SeasSearingPressureBolt>()] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width  = 18;
            Projectile.height = 8;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;   // corrected to 5 on first AI tick when Strong
            Projectile.timeLeft  = 420;
            Projectile.extraUpdates = 2;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown  = 12;
            Projectile.ArmorPenetration = 24;
        }

        public override void AI()
        {
            // Set correct penetrate from Strong flag on first tick
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                Projectile.penetrate  = Projectile.ai[0] > 0f ? 5 : 3;
                Projectile.netUpdate  = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();

            Color glow = Strong ? SeasSearingPalette.PressureBlue : SeasSearingPalette.RadioactiveCyan;
            Lighting.AddLight(Projectile.Center, glow.ToVector3() * 0.42f);

            // Drip acid trail every 3 physics ticks
            if (!Main.dedServ && (int)Projectile.localAI[1]++ % 3 == 0)
            {
                Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - dir * Main.rand.NextFloat(4f, 16f) + Main.rand.NextVector2Circular(2f, 2f),
                    Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                    -dir.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.5f, 2.2f),
                    110,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.RadioactiveCyan, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.55f, 0.95f + (Projectile.ai[0] > 0 ? 0.15f : 0f)));
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 0.12f;
            if (Strong)
                modifiers.FinalDamage *= 1.18f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, Strong ? 9 : 5);
            target.AddBuff(BuffID.Venom, 240);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
            SpawnSatellites(target.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 18, 4.5f, 8f, 1.1f);
        }

        public override void OnKill(int timeLeft)
        {
            if (!satellitesSpawned)
                SpawnSatellites(Projectile.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 12, 3.2f, 6f, 0.9f);
            SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 3.5f, 10f, 18, SeasSearingPalette.PressureBlue);
        }

        private void SpawnSatellites(Vector2 center)
        {
            if (satellitesSpawned || Main.myPlayer != Projectile.owner) return;
            satellitesSpawned = true;

            int count    = Strong ? 8 : 5;
            int satDmg   = Math.Max(1, Projectile.damage / 3);
            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.TwoPi * i / count + Main.rand.NextFloat(0.2f);
                Vector2 vel = angle.ToRotationVector2() * Main.rand.NextFloat(3.5f, 6.5f);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    center + angle.ToRotationVector2() * 18f,
                    vel,
                    Terraria.ModLoader.ModContent.ProjectileType<SeasSearingSatellite>(),
                    satDmg, 1.5f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex   = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = Terraria.ModLoader.ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin  = tex.Size() * 0.5f;
            Vector2 bOrigin = bloom.Size() * 0.5f;
            Color head      = Strong ? SeasSearingPalette.PressureBlue : HeadColor;
            Color tail      = TrailColor;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color c = Color.Lerp(tail, head, t) * (0.06f + t * 0.55f);
                c.A = 0;
                Main.EntitySpriteDraw(bloom, pos, null, c * 0.78f, Projectile.rotation, bOrigin, new Vector2(0.18f, 0.055f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex,   pos, null, c,          Projectile.rotation, origin,  Projectile.scale * (0.9f + t * 0.25f), SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, (head with { A = 0 }) * 0.7f, Projectile.rotation, bOrigin, new Vector2(0.22f, 0.06f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex,   Projectile.Center - Main.screenPosition, null, Color.White,                 Projectile.rotation, origin,  Projectile.scale * 1.15f, SpriteEffects.None, 0);
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NEW: SeasSearingSatellite
    // Small homing bubble spawned in a ring by SeasSearingPressureBolt.
    // Spirals toward nearest enemy and deals continuous damage.
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class SeasSearingSatellite : ModProjectile, ILocalizedModType
    {
        private float orbitPhase;
        private bool  hasTarget;
        private int   targetWho = -1;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 12;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width  = 10;
            Projectile.height = 10;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate  = -1;
            Projectile.timeLeft   = 150;
            Projectile.usesLocalNPCImmunity  = true;
            Projectile.localNPCHitCooldown   = 18;
        }

        public override void AI()
        {
            orbitPhase += 0.12f;

            // Find or maintain target
            if (Main.myPlayer == Projectile.owner)
            {
                if (!hasTarget || targetWho < 0 || !Main.npc.IndexInRange(targetWho) || !Main.npc[targetWho].CanBeChasedBy())
                    FindTarget();
            }

            if (hasTarget && Main.npc.IndexInRange(targetWho) && Main.npc[targetWho].CanBeChasedBy())
            {
                NPC target = Main.npc[targetWho];
                Vector2 toTarget = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                // Perpendicular offset creates spiral orbit approach
                Vector2 perpendicular = new Vector2(-toTarget.Y, toTarget.X) * (float)Math.Sin(orbitPhase) * 3.5f;
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, toTarget * 10f + perpendicular, 0.14f);
            }
            else
            {
                // Drift and slow down
                Projectile.velocity *= 0.97f;
            }

            // Speed cap
            if (Projectile.velocity.LengthSquared() > 12f * 12f)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * 12f;

            // Fade out in last 30 frames
            float age = 1f - Projectile.timeLeft / 150f;
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.PressureBlue.ToVector3() * (0.25f - age * 0.1f));

            // Trail dust
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemDiamond,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    110,
                    Color.Lerp(SeasSearingPalette.PressureBlue, SeasSearingPalette.RadioactiveCyan, age),
                    Main.rand.NextFloat(0.5f, 0.85f));
                d.noGravity = true;
            }
        }

        private void FindTarget()
        {
            float bestDist = 400f * 400f;
            int best = -1;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC n = Main.npc[i];
                if (!n.CanBeChasedBy()) continue;
                float d = Vector2.DistanceSquared(Projectile.Center, n.Center);
                if (d < bestDist) { bestDist = d; best = i; }
            }
            targetWho = best;
            hasTarget = best >= 0;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 2, 6 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 120);
        }

        public override void OnKill(int timeLeft)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 8, 2.8f, 5f, 0.8f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = Terraria.ModLoader.ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float opacity = MathHelper.Clamp(Projectile.timeLeft / 30f, 0f, 1f);
            Color c = (SeasSearingPalette.PressureBlue with { A = 0 }) * opacity;
            Color c2 = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * opacity * 0.55f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(bloom, pos, null, c2 * (t * 0.6f), 0f, bloom.Size() * 0.5f, 0.07f + t * 0.06f, SpriteEffects.None, 0);
            }
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, c,  0f, bloom.Size() * 0.5f, 0.14f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, c2, 0f, bloom.Size() * 0.5f, 0.08f, SpriteEffects.None, 0);
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NEW: SeasSearingAbyssalGeyser
    // Delayed eruptive column at cursor position (AbyssalRupture).
    // ai[0] = delay frames before becoming active.
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class SeasSearingAbyssalGeyser : ModProjectile, ILocalizedModType
    {
        private const int ActiveFrames = 28;
        private bool activated;
        private int  activeTimer;

        private int DelayFrames => (int)Projectile.ai[0];

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width      = 28;
            Projectile.height     = 220;
            Projectile.friendly   = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate  = -1;
            Projectile.timeLeft   = 200;   // generous lifetime; delay+active always fits
            Projectile.usesLocalNPCImmunity    = true;
            Projectile.localNPCHitCooldown     = -1;
        }

        public override bool? CanDamage() => activated && activeTimer >= 2;

        public override void AI()
        {
            if (!activated)
            {
                // localAI[1] counts UP; compare against delay threshold from ai[0]
                float delayThresh = Projectile.ai[0];
                float progress = delayThresh > 0f ? Projectile.localAI[1] / delayThresh : 1f;
                Projectile.localAI[1]++;

                // Warning phase: pulsing ring at base
                Projectile.friendly = false;
                if (!Main.dedServ && Main.GameUpdateCount % 6 == 0)
                {
                    SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 1.5f, 10f, 12,
                        Color.Lerp(SeasSearingPalette.DeepBlue, SeasSearingPalette.WarningOrange, progress));
                }

                if (Projectile.localAI[1] >= Projectile.ai[0])
                {
                    activated = true;
                    Projectile.friendly = true;
                    SeasSearingVisualUtility.SpawnPressureRing(Projectile.Center, 8f, 14f, 28, SeasSearingPalette.WarningOrange);
                    SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 45, 9f, 16f, 1.5f);
                    SeasSearingVisualUtility.ShakeAt(Projectile.Center, 5.5f, 1400f);
                    SoundEngine.PlaySound(SoundID.Item62 with { Volume = 0.76f, Pitch = -0.42f }, Projectile.Center);
                    SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.58f, Pitch = -0.58f }, Projectile.Center);
                }
                return;
            }

            activeTimer++;
            if (activeTimer > ActiveFrames)
            {
                Projectile.Kill();
                return;
            }

            float t = activeTimer / (float)ActiveFrames;
            Lighting.AddLight(Projectile.Center, Color.Lerp(SeasSearingPalette.WarningOrange, SeasSearingPalette.RadioactiveCyan, t).ToVector3() * 0.65f);

            // Geyser column particle eruption
            if (!Main.dedServ && Main.rand.NextBool(2))
            {
                float rx = Main.rand.NextFloat(-12f, 12f);
                float ry = Main.rand.NextFloat(-100f, 10f);
                Vector2 pos = Projectile.Center + new Vector2(rx, ry);
                Vector2 vel = new(Main.rand.NextFloat(-1.5f, 1.5f), Main.rand.NextFloat(-8f, -3f));
                Color col   = Main.rand.NextBool(3)
                    ? SeasSearingPalette.WarningOrange
                    : Color.Lerp(SeasSearingPalette.RadioactiveCyan, SeasSearingPalette.ToxicGreen, Main.rand.NextFloat());
                Dust d = Dust.NewDustPerfect(pos, Main.rand.NextBool(2) ? DustID.Water : DustID.GemEmerald, vel, 110, col, Main.rand.NextFloat(0.65f, 1.25f));
                d.noGravity = true;
            }

            // Spawn a persistent cloud at mid-life
            if (activeTimer == ActiveFrames / 2 && Main.myPlayer == Projectile.owner)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    Vector2.Zero,
                    Terraria.ModLoader.ModContent.ProjectileType<SeasSearingFalloutCloud>(),
                    Math.Max(1, Projectile.damage / 4),
                    0f, Projectile.owner, 20f);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 12);
            target.AddBuff(BuffID.Venom, 360);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 420);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (!activated) return false;

            Texture2D bloom = Terraria.ModLoader.ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring  = Terraria.ModLoader.ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
            float t         = activeTimer / (float)ActiveFrames;
            float fade      = (float)Math.Sin(t * MathHelper.Pi);
            Vector2 center  = Projectile.Center - Main.screenPosition;

            Color orange = (SeasSearingPalette.WarningOrange with { A = 0 }) * fade;
            Color cyan   = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * fade;

            // Wide horizontal burst at base
            Main.EntitySpriteDraw(bloom, center, null, orange * 0.7f, 0f, bloom.Size() * 0.5f,
                new Vector2(1.8f * (0.6f + t * 0.8f), 0.22f), SpriteEffects.None, 0);
            // Tall vertical column
            Main.EntitySpriteDraw(bloom, center, null, cyan * 0.55f, 0f, bloom.Size() * 0.5f,
                new Vector2(0.18f, 3.8f * fade), SpriteEffects.None, 0);
            // Rotating ring at base
            Main.EntitySpriteDraw(ring, center, null, orange * 0.8f,
                Main.GlobalTimeWrappedHourly * 4f, ring.Size() * 0.5f, 0.22f + t * 0.55f, SpriteEffects.None, 0);

            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NEW: SeasSearingVentShot
    // Enhanced left-click during VentCooldown.
    // Brighter trail; on hit spawns 2 diverging pressure orbs.
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class SeasSearingVentShot : ModProjectile, ILocalizedModType
    {
        private static readonly Color VentHead = new(200, 240, 255);
        private static readonly Color VentTail = new(40, 90, 200);

        private int Stage => (int)Projectile.ai[1];
        private bool orbsSpawned;

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width   = 12;
            Projectile.height  = 6;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate   = 4;
            Projectile.timeLeft    = 480;
            Projectile.extraUpdates = 6;
            Projectile.usesLocalNPCImmunity    = true;
            Projectile.localNPCHitCooldown     = 10;
            Projectile.ArmorPenetration = 28;
        }

        public override void AI()
        {
            if (Projectile.localAI[0] == 0f)
            {
                Projectile.localAI[0] = 1f;
                int stage = Stage;
                Projectile.penetrate = stage >= 3 ? 6 : (stage >= 1 ? 5 : 4);
                Projectile.netUpdate = true;
            }

            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.08f, 0.20f, 0.32f));

            if (Projectile.localAI[1]++ < 2f) return;

            if (!Main.dedServ && Main.rand.NextBool())
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(4f, 20f),
                    DustID.GemEmerald,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitX).RotatedByRandom(0.18f) * Main.rand.NextFloat(0.6f, 2.2f),
                    100,
                    Color.Lerp(VentTail, VentHead, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float dist = Vector2.Distance(Main.player[Projectile.owner].Center, target.Center);
            modifiers.FinalDamage *= 1f + Utils.GetLerpValue(380f, 1050f, dist, true) * 0.30f;
            modifiers.ScalingArmorPenetration += 0.10f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int stacks = hit.Crit ? 7 : 5;
            if (Stage >= 2) stacks++;
            if (Stage >= 3) stacks++;
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, stacks);
            target.AddBuff(BuffID.Venom, 260);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 260);

            SpawnPressureOrbs(target.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 20, 5f, 6f, 1.1f);
        }

        public override void OnKill(int timeLeft)
        {
            if (!orbsSpawned) SpawnPressureOrbs(Projectile.Center);
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 12, 3f, 5f, 0.8f);
        }

        private void SpawnPressureOrbs(Vector2 center)
        {
            if (orbsSpawned || Main.myPlayer != Projectile.owner) return;
            orbsSpawned = true;

            Vector2 dir = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int orbDmg  = Math.Max(1, (int)(Projectile.damage * 0.45f));
            for (int i = -1; i <= 1; i += 2)
            {
                Vector2 orbVel = dir.RotatedBy(MathHelper.ToRadians(35f * i)) * 10f;
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(), center, orbVel,
                    Terraria.ModLoader.ModContent.ProjectileType<SeasSearingSatellite>(),
                    orbDmg, 1f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex   = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = Terraria.ModLoader.ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin  = tex.Size() * 0.5f;
            Vector2 bOrigin = bloom.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color c = Color.Lerp(VentTail, VentHead, t) * (0.06f + t * 0.52f);
                c.A = 0;
                Main.EntitySpriteDraw(bloom, pos, null, c * 0.80f, Projectile.rotation, bOrigin, new Vector2(0.14f, 0.042f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex,   pos, null, c,          Projectile.rotation, origin,  Projectile.scale * (0.85f + t * 0.3f), SpriteEffects.None, 0);
            }

            Color hc = (VentHead with { A = 0 }) * 0.75f;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, hc, Projectile.rotation, bOrigin, new Vector2(0.18f, 0.052f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex,   Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale * 1.12f, SpriteEffects.None, 0);
            return false;
        }
    }

    // ──────────────────────────────────────────────────────────────────────────
    // NEW: SeasSearingTorrentShot
    // Rapid-fire pierce shot during AbyssalRupture.
    // Orange-tinted; pierces all; drops acid puddles as it travels.
    // ──────────────────────────────────────────────────────────────────────────
    internal sealed class SeasSearingTorrentShot : ModProjectile, ILocalizedModType
    {
        private static readonly Color TorrentHead = new(255, 180, 60);
        private static readonly Color TorrentTail = new(60, 160, 200);

        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_14";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width   = 10;
            Projectile.height  = 4;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.penetrate   = -1;   // pierce all
            Projectile.timeLeft    = 380;
            Projectile.extraUpdates = 6;
            Projectile.usesLocalNPCImmunity    = true;
            Projectile.localNPCHitCooldown     = 8;
            Projectile.ArmorPenetration = 22;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Lighting.AddLight(Projectile.Center, new Vector3(0.22f, 0.12f, 0.04f));

            // Drop small acid puddle every 8 physics ticks
            if (!Main.dedServ && (int)Projectile.localAI[0]++ % 8 == 0)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(2f, 2f),
                    DustID.GemEmerald,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.3f, 1.5f),
                    115,
                    Color.Lerp(TorrentTail, TorrentHead, Main.rand.NextFloat()),
                    Main.rand.NextFloat(0.5f, 0.9f));
                d.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += 0.18f;
            modifiers.DefenseEffectiveness   *= 0.55f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 4);
            target.AddBuff(BuffID.Venom, 200);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 200);
            SeasSearingVisualUtility.SpawnAbyssDust(target.Center, 10, 3.5f, 5f, 0.9f);
        }

        public override void OnKill(int timeLeft)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 8, 2.5f, 4f, 0.75f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex   = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = Terraria.ModLoader.ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin  = tex.Size() * 0.5f;
            Vector2 bOrigin = bloom.Size() * 0.5f;

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero) continue;
                float t = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 pos = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color c = Color.Lerp(TorrentTail, TorrentHead, t) * (0.05f + t * 0.48f);
                c.A = 0;
                Main.EntitySpriteDraw(bloom, pos, null, c * 0.70f, Projectile.rotation, bOrigin, new Vector2(0.10f, 0.032f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(tex,   pos, null, c,          Projectile.rotation, origin,  Projectile.scale, SpriteEffects.None, 0);
            }

            Color hc = (TorrentHead with { A = 0 }) * 0.65f;
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, hc, Projectile.rotation, bOrigin, new Vector2(0.14f, 0.04f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(tex,   Projectile.Center - Main.screenPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class SeasSearingFalloutRain : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.SeasSearing";
        public override string Texture => "Terraria/Images/Projectile_618";

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 12;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 160;
            Projectile.tileCollide = true;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y = Math.Min(Projectile.velocity.Y + 0.08f, 18f);
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.ToxicGreen.ToVector3() * 0.22f);

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GemEmerald,
                    -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.5f, 0.5f),
                    120,
                    SeasSearingPalette.ToxicGreen,
                    Main.rand.NextFloat(0.5f, 0.85f));
                dust.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.GetGlobalNPC<SeasSearingPollutionNPC>().ApplyPollution(target, Projectile.owner, 5, 10 * 60, fromSpread: true);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 240);
        }

        public override void OnKill(int timeLeft)
        {
            SeasSearingVisualUtility.SpawnAbyssDust(Projectile.Center, 8, 2.8f, 5f, 0.75f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, center, null, (SeasSearingPalette.ToxicGreen with { A = 0 }) * 0.55f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.08f, 0.16f), SpriteEffects.None, 0);
            return true;
        }
    }
}
