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
            Projectile.penetrate = 1;
            Projectile.timeLeft = 540;
            Projectile.extraUpdates = 4;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
            Projectile.ArmorPenetration = 18;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation();
            Projectile.spriteDirection = Projectile.direction;
            Lighting.AddLight(Projectile.Center, new Vector3(0.05f, 0.22f, 0.24f));

            if (Projectile.localAI[0]++ < 2f)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(4f, 18f) + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    Main.rand.NextBool(3) ? DustID.Water : DustID.GemEmerald,
                    -direction.RotatedByRandom(0.22f) * Main.rand.NextFloat(0.6f, 1.9f),
                    110,
                    Main.rand.NextBool() ? CoreColor : SeasSearingPalette.ToxicGreen,
                    Main.rand.NextFloat(0.45f, 0.78f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float distance = Vector2.Distance(Main.player[Projectile.owner].Center, target.Center);
            float precisionBonus = 1f + Utils.GetLerpValue(380f, 1050f, distance, true) * 0.28f;
            modifiers.FinalDamage *= precisionBonus;
            modifiers.ScalingArmorPenetration += 0.08f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            int stackAmount = hit.Crit ? 6 : 4;
            if (target.boss || target.realLife >= 0)
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

            for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Color color = Color.Lerp(TrailColor, CoreColor, completion) * (0.08f + completion * 0.42f);
                color.A = 0;

                Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.72f, Projectile.rotation, bloomOrigin, new Vector2(0.12f, 0.038f) * Projectile.scale, SpriteEffects.None, 0);
                Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, (CoreColor with { A = 0 }) * 0.6f, Projectile.rotation, bloomOrigin, new Vector2(0.15f, 0.045f), SpriteEffects.None, 0);
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
        private int Mode => (int)Projectile.ai[1];

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
            float completion = 1f - Projectile.timeLeft / 30f;
            Lighting.AddLight(Projectile.Center, SeasSearingPalette.PollutionColor(1f - completion).ToVector3() * (0.35f + completion * 0.5f));

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
            modifiers.DefenseEffectiveness *= Mode == 0 ? 0.45f : 0.08f;
            modifiers.ScalingArmorPenetration += Mode == 0 ? 0.12f : 0.38f;

            if (Mode == 1)
                modifiers.FinalDamage *= 1f + MathHelper.Clamp(Stacks / 140f, 0f, 0.8f);
            else if (Mode == 2)
                modifiers.FinalDamage *= 1.22f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Venom, 360);
            target.AddBuff(ModContent.BuffType<Irradiated>(), 420);
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

            Color outer = (SeasSearingPalette.DeepBlue with { A = 0 }) * opacity;
            Color inner = (SeasSearingPalette.RadioactiveCyan with { A = 0 }) * opacity;
            Color toxic = (SeasSearingPalette.ToxicGreen with { A = 0 }) * opacity;
            float scale = blastRadius / bloom.Width * (0.65f + completion * 0.85f);

            Main.EntitySpriteDraw(bloom, center, null, outer * 0.72f, 0f, bloom.Size() * 0.5f, scale * 2.1f, SpriteEffects.None, 0);
            Main.EntitySpriteDraw(bloom, center, null, inner * 0.58f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(scale * 1.1f, scale * 0.74f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(ring, center, null, toxic * 0.8f, Main.GlobalTimeWrappedHourly * 1.6f, ring.Size() * 0.5f, scale * 1.9f, SpriteEffects.None, 0);

            if (Mode > 0)
                Main.EntitySpriteDraw(ring, center, null, (Color.White with { A = 0 }) * opacity * 0.44f, -Main.GlobalTimeWrappedHourly * 2.1f, ring.Size() * 0.5f, scale * 0.78f, SpriteEffects.None, 0);

            return false;
        }

        private void InitializeBlast()
        {
            initialized = true;
            blastRadius = MathHelper.Clamp(94f + Stacks * 3.4f, 130f, Mode == 0 ? 390f : 520f);
            if (Mode == 2)
                blastRadius = MathHelper.Clamp(blastRadius + 120f, 260f, 650f);

            Vector2 center = Projectile.Center;
            Projectile.width = Projectile.height = Math.Max(12, (int)(blastRadius * 2f));
            Projectile.Center = center;
            SeasSearingVisualUtility.SpawnAbyssDust(center, Mode == 0 ? 38 : 70, Mode == 0 ? 6f : 9f, Math.Min(blastRadius * 0.28f, 80f), Mode == 0 ? 1f : 1.35f);
            SoundEngine.PlaySound(SoundID.Item14 with { Volume = Mode == 0 ? 0.56f : 0.86f, Pitch = Mode == 0 ? -0.3f : -0.58f }, center);
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
