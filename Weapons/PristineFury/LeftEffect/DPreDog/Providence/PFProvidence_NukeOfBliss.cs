using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Dusts;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal sealed class PFProvidence_NukeOfBliss : ModProjectile, ILocalizedModType
    {
        private const int ReachedPeakTime = 86;
        private const int RainDownStartTime = 112;
        private const float FinalDiveSpeed = 18.5f;
        private const float TargetLockRange = 460f;

        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityLegendsComeBack/Weapons/PristineFury/LeftEffect/DPreDog/Providence/NukeOfBliss";

        private ref float SavedTargetX => ref Projectile.ai[0];
        private ref float SavedTargetY => ref Projectile.ai[1];
        private int time;
        private int rainDownTimer = RainDownStartTime;
        private int lockedTargetIndex = -1;
        private float fade = 1f;

        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 220, 110));
        private NPC LockedTarget =>
            Main.npc.IndexInRange(lockedTargetIndex) && Main.npc[lockedTargetIndex].CanBeChasedBy(Projectile, false)
                ? Main.npc[lockedTargetIndex]
                : null;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 16;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 64;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.extraUpdates = 1;
            Projectile.timeLeft = 640;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
        }

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (SavedTargetX != 0f || SavedTargetY != 0f)
                return;

            Player owner = Main.player[Projectile.owner];
            Vector2 mouse = owner.Calamity().mouseWorld;
            if (mouse == Vector2.Zero)
                mouse = Main.MouseWorld;

            SavedTargetX = mouse.X;
            SavedTargetY = mouse.Y;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Vector2 targetPoint = GetTargetPoint(owner);
            if (time > ReachedPeakTime)
                UpdateRainDownPhase(owner, targetPoint);
            else
                Projectile.velocity *= 0.995f;

            Projectile.spriteDirection = Projectile.direction = (Projectile.velocity.X > 0f).ToDirectionInt();
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            fade = rainDownTimer > 0 ? Utils.GetLerpValue(ReachedPeakTime, ReachedPeakTime * 0.7f, time, true) : 1f;

            SpawnFlightEffects();
            time++;
        }

        private void UpdateRainDownPhase(Player owner, Vector2 targetPoint)
        {
            EnsureTargetLocked(targetPoint);
            Vector2 strikePoint = LockedTarget?.Center ?? targetPoint;

            if (rainDownTimer > 1)
                Projectile.Center = new Vector2(strikePoint.X, owner.Center.Y - 640f * owner.gravDir);

            if (rainDownTimer > 0)
            {
                if (Projectile.owner == Main.myPlayer && Projectile.numUpdates == 0)
                    SpawnAirBurstShards(strikePoint);

                rainDownTimer--;
            }

            if (rainDownTimer == 64 && Projectile.numUpdates == 0)
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MissileNearing") { Volume = 0.6f, Pitch = 0.42f, MaxInstances = 3 }, Projectile.Center);

            if (rainDownTimer == 1)
            {
                Projectile.extraUpdates = 6;
                Projectile.penetrate = 1;
                Projectile.velocity = (strikePoint - Projectile.Center).SafeNormalize(Vector2.UnitY) * FinalDiveSpeed;
                Projectile.netUpdate = true;
            }

            if (rainDownTimer != 0)
                return;

            if (Projectile.Center.Y > strikePoint.Y)
                Projectile.tileCollide = true;

            Vector2 desiredDirection = (strikePoint - Projectile.Center).SafeNormalize(Vector2.UnitY);
            if (Projectile.velocity.Length() < FinalDiveSpeed)
                Projectile.velocity = Projectile.velocity * 0.96f + desiredDirection * 3f;
            else
                Projectile.velocity = Projectile.velocity.ToRotation().AngleTowards(desiredDirection.ToRotation(), 0.042f).ToRotationVector2() * Projectile.velocity.Length() * 0.995f;

            if (Vector2.Distance(Projectile.Center, strikePoint) < 34f)
                Projectile.Kill();
        }

        private Vector2 GetTargetPoint(Player owner)
        {
            Vector2 savedTarget = new(SavedTargetX, SavedTargetY);
            if (savedTarget != Vector2.Zero)
                return savedTarget;

            Vector2 mouse = owner.Calamity().mouseWorld;
            return mouse == Vector2.Zero ? Main.MouseWorld : mouse;
        }

        private void EnsureTargetLocked(Vector2 mouse)
        {
            if (LockedTarget != null)
                return;

            NPC closest = null;
            float bestDistance = TargetLockRange;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile, false))
                    continue;

                float distance = Vector2.Distance(mouse, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            if (closest == null)
                return;

            lockedTargetIndex = closest.whoAmI;
            Projectile.netUpdate = true;
        }

        private void SpawnAirBurstShards(Vector2 strikePoint)
        {
            if (rainDownTimer < 34 || rainDownTimer > 98 || rainDownTimer % 8 != 0)
                return;

            int shardCount = Main.rand.Next(2, 4);
            for (int i = 0; i < shardCount; i++)
            {
                Vector2 spawnPosition = Projectile.Center + new Vector2(Main.rand.NextFloat(-190f, 190f), Main.rand.NextFloat(-20f, 70f));
                Vector2 target = strikePoint + new Vector2(Main.rand.NextFloat(-300f, 300f), Main.rand.NextFloat(-20f, 90f));
                Vector2 velocity = (target - spawnPosition).SafeNormalize(Vector2.UnitY).RotatedByRandom(0.08f) * Main.rand.NextFloat(12f, 17f);

                int projectileIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    velocity,
                    ModContent.ProjectileType<PFProvidence_HolyShrapnel>(),
                    Math.Max(1, (int)(Projectile.damage * 0.33f)),
                    Projectile.knockBack * 0.35f,
                    Projectile.owner,
                    Main.rand.NextFloat(0.75f, 1.25f));

                PFLeftEffectRules.ApplyTheme(projectileIndex, (PristineFuryMark)(int)Projectile.ai[2]);
            }
        }

        private void SpawnFlightEffects()
        {
            if (Main.dedServ)
                return;

            Color theme = ThemeColor;
            Vector2 back = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.42f);

            if (fade <= 0.18f)
                return;

            if (Projectile.numUpdates == 0)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + back * 10f + Main.rand.NextVector2Circular(4f, 4f), ModContent.DustType<LightDust>());
                dust.velocity = back.RotatedByRandom(0.25f) * Main.rand.NextFloat(1.6f, 4.6f);
                dust.color = Color.Lerp(theme, Color.White, Main.rand.NextFloat(0.15f, 0.45f));
                dust.scale = Main.rand.NextFloat(0.72f, 1.05f);
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            if (Projectile.timeLeft % 3 == 0)
            {
                Particle smoke = new HeavySmokeParticle(
                    Projectile.Center,
                    -Projectile.velocity * Main.rand.NextFloat(0.18f, 0.52f),
                    Color.Lerp(theme, Color.Goldenrod, 0.35f),
                    Main.rand.Next(36, 55),
                    Main.rand.NextFloat(0.24f, 0.5f),
                    0.5f,
                    Main.rand.NextFloat(-0.18f, 0.18f),
                    Main.rand.NextBool(),
                    required: true);

                GeneralParticleHandler.SpawnParticle(smoke);
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float damageMultiplier = Utils.Remap(Projectile.numHits, 0f, 8f, 1f, 0.45f, true);
            modifiers.SourceDamage *= damageMultiplier * (rainDownTimer <= 0 ? 1f : 0.25f);
        }

        public override bool? CanDamage() => fade <= 0.2f ? false : null;

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 360);

        public override void OnKill(int timeLeft)
        {
            Vector2 center = Projectile.Center;
            int blastDamage = Math.Max(1, (int)(Projectile.damage * 0.62f));
            DamageCircle(center, 178, blastDamage);

            if (Main.myPlayer == Projectile.owner)
                SpawnFlameFields(center, blastDamage);

            SpawnExplosionEffects(center, 1.25f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MineralMortarExplode") { Volume = 0.9f, Pitch = 0.32f }, center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastImpact") { Volume = 0.72f, PitchVariance = 0.18f }, center);
        }

        private void SpawnFlameFields(Vector2 center, int damage)
        {
            for (int i = 0; i < 7; i++)
            {
                float offset = MathHelper.Lerp(-270f, 270f, i / 6f) + Main.rand.NextFloat(-26f, 26f);
                Vector2 spawnPosition = center + new Vector2(offset, Main.rand.NextFloat(-10f, 34f));

                int projectileIndex = Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    Vector2.Zero,
                    ModContent.ProjectileType<PFProvidence_HolyFireField>(),
                    Math.Max(1, (int)(damage * 0.24f)),
                    Projectile.knockBack * 0.18f,
                    Projectile.owner,
                    Main.rand.NextFloat(0.82f, 1.25f),
                    Main.rand.NextFloat(MathHelper.TwoPi));

                PFLeftEffectRules.ApplyTheme(projectileIndex, (PristineFuryMark)(int)Projectile.ai[2]);
            }
        }

        private void DamageCircle(Vector2 center, int radius, int damage)
        {
            int oldWidth = Projectile.width;
            int oldHeight = Projectile.height;
            int oldDamage = Projectile.damage;
            int oldPenetrate = Projectile.penetrate;
            Vector2 oldCenter = Projectile.Center;

            Projectile.position = center;
            Projectile.width = Projectile.height = radius * 2;
            Projectile.Center = center;
            Projectile.damage = damage;
            Projectile.penetrate = -1;
            Projectile.Damage();

            Projectile.width = oldWidth;
            Projectile.height = oldHeight;
            Projectile.damage = oldDamage;
            Projectile.penetrate = oldPenetrate;
            Projectile.Center = oldCenter;
        }

        internal static void SpawnExplosionEffects(Vector2 center, float scale)
        {
            if (Main.dedServ)
                return;

            Color gold = new(255, 214, 86);
            Color orange = new(255, 128, 48);
            Color white = Color.White;

            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, gold, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.36f * scale, 19, true));
            GeneralParticleHandler.SpawnParticle(new CustomPulse(center, Vector2.Zero, orange, "CalamityMod/Particles/SoftRoundExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0f, 0.24f * scale, 19, true));

            for (int i = 0; i < 38; i++)
            {
                Dust dust = Dust.NewDustPerfect(center, ModContent.DustType<LightDust>());
                dust.velocity = (MathHelper.TwoPi * i / 38f).ToRotationVector2() * Main.rand.NextFloat(5.5f, 13f) * scale;
                dust.color = Color.Lerp(orange, white, Main.rand.NextFloat(0.15f, 0.78f));
                dust.scale = Main.rand.NextFloat(1.05f, 1.7f) * scale;
                dust.noGravity = true;
                dust.noLightEmittence = true;
            }

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = new Vector2(4f, 4f).RotatedByRandom(100f) * Main.rand.NextFloat(0.45f, 1.2f) * scale;
                Particle spark = new CustomSpark(
                    center,
                    velocity,
                    "CalamityMod/Particles/ProvidenceMarkParticle",
                    false,
                    Main.rand.Next(18, 28),
                    Main.rand.NextFloat(1.45f, 1.9f) * scale,
                    Main.rand.NextBool(3) ? Color.Khaki : gold,
                    new Vector2(1.3f, 0.5f),
                    true,
                    false,
                    glowOpacity: 0.48f);

                GeneralParticleHandler.SpawnParticle(spark);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Color theme = ThemeColor with { A = 0 };
            float backglow = 4f + Utils.GetLerpValue(0f, ReachedPeakTime, time, true) * 4f;

            for (int i = 0; i < 8; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * backglow;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, theme * 0.58f * fade, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Projectile.GetAlpha(lightColor) * fade, Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0);
            return false;
        }
    }

    internal sealed class PFProvidence_HolyShrapnel : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float FlameScale => ref Projectile.ai[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 220, 110));

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 2;
            Projectile.timeLeft = 180;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity.Y += 0.08f;
            Projectile.velocity *= 1.003f;
            SpawnTrail();
        }

        private void SpawnTrail()
        {
            if (Main.dedServ || Projectile.numUpdates != 0)
                return;

            Color theme = ThemeColor;
            Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.3f);

            Particle spark = new CustomSpark(
                Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 10f,
                -Projectile.velocity * 0.12f,
                "CalamityMod/Particles/ProvidenceMarkParticle",
                false,
                12,
                Main.rand.NextFloat(0.78f, 1.05f),
                Color.Lerp(theme, Color.White, 0.16f),
                new Vector2(1.3f, 0.5f),
                true,
                false,
                glowOpacity: 0.42f);

            GeneralParticleHandler.SpawnParticle(spark);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Projectile.Kill();
            return false;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 180);

        public override void OnKill(int timeLeft)
        {
            int fieldDamage = Math.Max(1, (int)(Projectile.damage * 0.62f));
            int projectileIndex = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                Projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<PFProvidence_HolyFireField>(),
                fieldDamage,
                Projectile.knockBack * 0.2f,
                Projectile.owner,
                MathHelper.Clamp(FlameScale, 0.7f, 1.3f),
                Main.rand.NextFloat(MathHelper.TwoPi));

            PFLeftEffectRules.ApplyTheme(projectileIndex, (PristineFuryMark)(int)Projectile.ai[2]);
            PFProvidence_NukeOfBliss.SpawnExplosionEffects(Projectile.Center, 0.52f);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/Providence/ProvidenceHolyBlastImpact") { Volume = 0.34f, PitchVariance = 0.24f, MaxInstances = 10 }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = (Color.Lerp(ThemeColor, Color.White, 0.25f) with { A = 0 }) * 0.78f;

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.42f, Projectile.rotation, bloom.Size() * 0.5f, new Vector2(0.12f, 0.26f), SpriteEffects.None, 0);
            Main.EntitySpriteDraw(star, drawPosition, null, color * 0.72f, Projectile.rotation, star.Size() * 0.5f, new Vector2(0.12f, 0.78f), SpriteEffects.None, 0);
            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }

    internal sealed class PFProvidence_HolyFireField : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float FieldScale => ref Projectile.ai[0];
        private ref float RotationSeed => ref Projectile.ai[1];
        private ref float Timer => ref Projectile.localAI[0];
        private Color ThemeColor => PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 220, 110));

        public override void SetDefaults()
        {
            Projectile.width = 190;
            Projectile.height = 86;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0f;
            Projectile.rotation = RotationSeed;
            Lighting.AddLight(Projectile.Center, ThemeColor.ToVector3() * (0.38f * Projectile.Opacity));

            if (Timer == 1f)
            {
                Projectile.width = (int)(190f * MathHelper.Clamp(FieldScale, 0.72f, 1.35f));
                Projectile.height = (int)(86f * MathHelper.Clamp(FieldScale, 0.72f, 1.35f));
            }

            if (Main.dedServ || Main.rand.NextBool(3))
                return;

            Vector2 flamePosition = Projectile.Center + new Vector2(Main.rand.NextFloat(-Projectile.width * 0.46f, Projectile.width * 0.46f), Main.rand.NextFloat(-Projectile.height * 0.45f, Projectile.height * 0.24f));
            Vector2 velocity = -Vector2.UnitY.RotatedByRandom(0.48f) * Main.rand.NextFloat(0.7f, 2.6f);
            Particle flame = new CustomSpark(
                flamePosition,
                velocity,
                "CalamityMod/Particles/ProvidenceMarkParticle",
                false,
                Main.rand.Next(16, 25),
                Main.rand.NextFloat(1.15f, 1.55f) * FieldScale,
                Main.rand.NextBool(4) ? Color.Khaki : ThemeColor,
                new Vector2(1.3f, 0.5f),
                true,
                false,
                glowOpacity: 0.45f);

            GeneralParticleHandler.SpawnParticle(flame);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 closest = Vector2.Clamp(targetHitbox.Center.ToVector2(), projHitbox.TopLeft(), projHitbox.BottomRight());
            Vector2 delta = closest - Projectile.Center;
            float radiusX = Projectile.width * 0.5f;
            float radiusY = Projectile.height * 0.5f;
            return delta.X * delta.X / (radiusX * radiusX) + delta.Y * delta.Y / (radiusY * radiusY) <= 1f;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone) =>
            target.AddBuff(ModContent.BuffType<HolyFlames>(), 240);

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            if (Projectile.numHits > 0)
                modifiers.SourceDamage *= 0.72f;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/SoftRoundExplosion").Value;
            Texture2D mark = ModContent.Request<Texture2D>("CalamityMod/Particles/ProvidenceMarkParticle").Value;
            float fadeIn = Utils.GetLerpValue(0f, 16f, Timer, true);
            float fadeOut = Utils.GetLerpValue(0f, 42f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = ThemeColor with { A = 0 };

            PFLeftEffectRules.BeginAdditive();
            Main.EntitySpriteDraw(bloom, drawPosition, null, color * 0.16f * opacity, RotationSeed, bloom.Size() * 0.5f, new Vector2(0.72f, 0.22f) * FieldScale, SpriteEffects.None, 0);

            for (int i = 0; i < 8; i++)
            {
                float ratio = i / 7f;
                Vector2 offset = new(MathHelper.Lerp(-Projectile.width * 0.42f, Projectile.width * 0.42f, ratio), (float)Math.Sin(Timer * 0.08f + i) * 8f);
                Main.EntitySpriteDraw(mark, drawPosition + offset, null, color * 0.28f * opacity, -0.45f + ratio * 0.9f, mark.Size() * 0.5f, new Vector2(0.62f, 0.22f) * FieldScale, SpriteEffects.None, 0);
            }

            PFLeftEffectRules.EndAdditive();
            return false;
        }
    }
}
