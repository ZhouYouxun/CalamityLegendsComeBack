using CalamityLegendsComeBack.Accssory.MC.PeacockScroll;
using CalamityLegendsComeBack.Accssory.MC.PrecisionEmblem;
using CalamityLegendsComeBack.Accssory.MC.MalachiteFeather;
using CalamityLegendsComeBack.Accssory.MC.GaleAce;
using CalamityLegendsComeBack.Weapons.Malachite;
using CalamityMod;
using CalamityMod.Buffs.DamageOverTime;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.Malachite.EXSkill
{
    public class MalachiteFinaleController : ModProjectile, ILocalizedModType
    {
        private const int DetonateTime = 120;
        private const int TotalTime = 170;
        private const int FinalePetalCount = 23;
        private const float TargetSearchRange = 500f * 16f;
        private const int TargetSpotlightCount = 5;

        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.timeLeft = TotalTime;
            Projectile.penetrate = -1;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override bool? CanDamage() => false;

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Center;
            Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            Projectile.localAI[0]++;
            if (Projectile.localAI[1] == 0f && Projectile.owner == Main.myPlayer)
            {
                Projectile.localAI[1] = 1f;
                SpawnReadyPetals(owner);
            }

            owner.itemTime = Math.Max(owner.itemTime, 2);
            owner.itemAnimation = Math.Max(owner.itemAnimation, 2);
            owner.ChangeDir(Projectile.velocity.X >= 0f ? 1 : -1);

            if (Projectile.localAI[0] < DetonateTime)
            {
                owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, Projectile.velocity.ToRotation() - MathHelper.PiOver2);
                owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.ThreeQuarters, Projectile.velocity.ToRotation() - MathHelper.PiOver2);
                SpawnChargeDust(owner);
                if (Projectile.owner == Main.myPlayer && Projectile.localAI[0] % 4f == 0f)
                {
                    SpawnVisualPetal(owner);
                }

                return;
            }

            if (Projectile.localAI[0] == DetonateTime && Projectile.owner == Main.myPlayer)
                ReleaseFinale(owner);
        }

        private void SpawnReadyPetals(Player owner)
        {
            for (int i = 0; i < FinalePetalCount; i++)
            {
                float progress = FinalePetalCount <= 1 ? 0.5f : i / (float)(FinalePetalCount - 1);
                float centered = progress - 0.5f;
                Vector2 spawnOffset = new(
                    centered * 1180f + Main.rand.NextFloat(-120f, 120f),
                    Main.rand.NextFloat(-880f, -520f) - MathF.Sin(progress * MathHelper.Pi) * 120f);
                Vector2 velocity = new(
                    Main.rand.NextFloat(-0.9f, 0.9f),
                    Main.rand.NextFloat(1.2f, 2.45f));

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    owner.Center + spawnOffset,
                    velocity,
                    ModContent.ProjectileType<MalachiteFinalePetal>(),
                    0,
                    0f,
                    Projectile.owner,
                    i,
                    i % 3,
                    Main.rand.NextFloatDirection());
            }
        }

        private void ReleaseFinale(Player owner)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX * owner.direction);
            List<NPC> targets = FindFinaleTargets(owner, TargetSearchRange);
            List<Projectile> petals = FindReadyPetals(owner);
            int cumulativeDelay = 0;

            for (int i = 0; i < FinalePetalCount; i++)
            {
                Vector2 spawnPosition = i < petals.Count
                    ? petals[i].Center
                    : owner.Center - direction * Main.rand.NextFloat(260f, 520f) + Main.rand.NextVector2Circular(220f, 180f);

                NPC target = PickTargetForPetal(targets, i);
                Vector2 targetPoint = target?.Center ?? owner.Center + direction * 900f + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-180f, 180f);
                Vector2 dartDirection = (targetPoint - spawnPosition).SafeNormalize(direction);
                int delay = cumulativeDelay;
                cumulativeDelay += Main.rand.Next(1, 3);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    spawnPosition,
                    dartDirection * Main.rand.NextFloat(45f, 54f),
                    ModContent.ProjectileType<MalachiteFinaleDart>(),
                    Math.Max(1, (int)(Projectile.damage * 0.74f)),
                    Projectile.knockBack,
                    Projectile.owner,
                    delay,
                    i % 3);

                if (i < petals.Count)
                {
                    petals[i].localAI[2] = 1f;
                    petals[i].timeLeft = Math.Min(petals[i].timeLeft, 14);
                }
            }

            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/AbilitySounds/PlagueReaperRecharge") { Volume = 0.52f, Pitch = 0.24f }, owner.Center);
            SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort") { Volume = 0.62f, Pitch = 0.12f }, owner.Center);
            ApplyScreenShake(owner.Center, 4.6f);
            owner.SetImmuneTimeForAllTypes(32);
        }

        private static List<Projectile> FindReadyPetals(Player owner)
        {
            int petalType = ModContent.ProjectileType<MalachiteFinalePetal>();
            List<Projectile> petals = new(FinalePetalCount);

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (projectile.owner == owner.whoAmI &&
                    projectile.type == petalType &&
                    projectile.ai[0] >= 0f)
                {
                    petals.Add(projectile);
                }
            }

            petals.Sort((left, right) => left.ai[0].CompareTo(right.ai[0]));
            return petals;
        }

        private static List<NPC> FindFinaleTargets(Player owner, float range)
        {
            List<NPC> targets = new();

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy() || Vector2.Distance(owner.Center, npc.Center) > range)
                    continue;

                targets.Add(npc);
            }

            targets.Sort((left, right) =>
                Vector2.DistanceSquared(owner.Center, left.Center).CompareTo(Vector2.DistanceSquared(owner.Center, right.Center)));

            if (targets.Count > FinalePetalCount)
                targets.RemoveRange(FinalePetalCount, targets.Count - FinalePetalCount);

            return targets;
        }

        private static NPC PickTargetForPetal(List<NPC> targets, int petalIndex)
        {
            if (targets.Count <= 0)
                return null;

            if (targets.Count >= FinalePetalCount)
                return targets[petalIndex];

            return targets[petalIndex % targets.Count];
        }

        private void SpawnVisualPetal(Player owner)
        {
            Vector2 screenSizedOffset = new(
                Main.rand.NextFloat(-Main.screenWidth * 0.55f, Main.screenWidth * 0.55f),
                -Main.screenHeight * 0.55f - Main.rand.NextFloat(30f, 160f));
            Vector2 velocity = new(Main.rand.NextFloat(-1.8f, 1.8f), Main.rand.NextFloat(1.9f, 4.2f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                owner.Center + screenSizedOffset,
                velocity,
                ModContent.ProjectileType<MalachiteFinalePetal>(),
                0,
                0f,
                Projectile.owner,
                -1f,
                Main.rand.Next(3),
                Main.rand.NextFloatDirection());
        }

        private static void ApplyScreenShake(Vector2 center, float power)
        {
            if (Main.dedServ)
                return;

            float distanceFactor = Utils.GetLerpValue(1600f, 120f, Vector2.Distance(Main.LocalPlayer.Center, center), true);
            Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(Main.LocalPlayer.Calamity().GeneralScreenShakePower, power * distanceFactor);
        }

        private void SpawnChargeDust(Player owner)
        {
            float charge = Utils.GetLerpValue(0f, DetonateTime, Projectile.localAI[0], true);

            if (Projectile.localAI[0] > 10f && Projectile.localAI[0] < DetonateTime)
            {
                Vector2 center = owner.Center;
                Color pulseColor = Color.Lerp(new Color(25, 25, 25, 0), new Color(115, 255, 150, 0), charge);

                if (Projectile.localAI[0] % 10f == 0f)
                {
                    Particle pulse = new CustomPulse(
                        center,
                        Vector2.Zero,
                        pulseColor,
                        "CalamityMod/Particles/SoftRoundExplosion",
                        new Vector2(1.5f, 1f),
                        Main.rand.NextBool() ? 0f : MathHelper.Pi,
                        charge * 0.5f,
                        charge * 0.1f,
                        20,
                        true);
                    GeneralParticleHandler.SpawnParticle(pulse);
                }

                Vector2 sparkPosition = center + Main.rand.NextVector2Unit() * Main.rand.NextFloat(80f, 300f) * charge * 1.6f * new Vector2(1.5f, 1f);
                Vector2 sparkVelocity = (center - sparkPosition).SafeNormalize(Vector2.Zero) * (Vector2.Distance(sparkPosition, center) / 10f);
                Particle spark = new SparkParticle(
                    sparkPosition,
                    sparkVelocity,
                    affectedByGravity: false,
                    10,
                    Main.rand.NextFloat(0.2f, 0.5f) * charge * 2f,
                    new Color(115, 255, 150));
                GeneralParticleHandler.SpawnParticle(spark);
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 spawnPosition = owner.Center + Main.rand.NextVector2Circular(180f, 130f);
                Vector2 velocity = (owner.Center - spawnPosition).SafeNormalize(Vector2.Zero) * Main.rand.NextFloat(1.8f, 4.2f);
                Dust dust = Dust.NewDustPerfect(spawnPosition, DustID.Terra, velocity, 80, new Color(120, 255, 135), Main.rand.NextFloat(0.75f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            float timer = Projectile.localAI[0];
            float charge = Utils.GetLerpValue(0f, DetonateTime, timer, true);
            float flash = timer >= DetonateTime ? Utils.GetLerpValue(TotalTime, DetonateTime, timer, true) : charge;
            Vector2 playerScreen = Projectile.Center - Main.screenPosition;
            Player owner = Main.player[Projectile.owner];
            bool galeAce = owner.active && owner.GetModPlayer<GaleAcePlayer>().GaleAceEquipped;

            DrawSpotlight(texture, origin, playerScreen, charge, flash);
            if (owner.active)
            {
                List<NPC> targets = FindFinaleTargets(owner, TargetSearchRange);
                int spotlightCount = Math.Min(TargetSpotlightCount, targets.Count);
                for (int i = 0; i < spotlightCount; i++)
                {
                    float stagger = Utils.GetLerpValue(14f + i * 6f, 42f + i * 6f, timer, true);
                    DrawEnemySpotlight(targets[i].Center - Main.screenPosition, charge * stagger, flash * 0.72f, i);
                }
            }

            DrawPetals(texture, origin, charge, galeAce, owner.direction);
            return false;
        }

        private static void DrawSpotlight(Texture2D texture, Vector2 origin, Vector2 playerScreen, float charge, float flash)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 starOrigin = star.Size() * 0.5f;
            float height = Main.screenHeight + 220f;
            float width = MathHelper.Lerp(180f, 520f, charge);
            Vector2 start = new(playerScreen.X, -100f);
            Vector2 center = (start + playerScreen) * 0.5f;
            float rotation = (playerScreen - start).ToRotation() + MathHelper.PiOver2;
            Vector2 scale = new(width / bloom.Width, height / bloom.Height);

            DrawSoftCone(start, playerScreen, charge, flash);

            for (int i = 0; i < 3; i++)
            {
                float wave = MathF.Sin(Main.GlobalTimeWrappedHourly * 2.8f + i) * 18f;
                Color color = Color.Lerp(new Color(20, 95, 45, 0), new Color(145, 255, 155, 0), charge);
                Main.EntitySpriteDraw(
                    bloom,
                    center + Vector2.UnitX * wave,
                    null,
                    color * (0.065f + flash * 0.09f),
                    rotation,
                    bloomOrigin,
                    scale * (0.68f + i * 0.12f),
                    SpriteEffects.None);
            }

            float aura = MathF.Sin(MathHelper.Pi * charge);
            for (int i = 0; i < 3; i++)
            {
                float pulse = charge * 4.4f + MathF.Cos(Main.GlobalTimeWrappedHourly * 2f + i) * charge * 0.22f;
                Color ringColor = new Color(75, 255, 135, 0) * (0.10f + flash * 0.12f);
                Main.EntitySpriteDraw(
                    bloom,
                    playerScreen + (Main.GlobalTimeWrappedHourly * (0.8f + i * 0.17f)).ToRotationVector2() * (i * 5f + aura * 8f),
                    null,
                    ringColor,
                    0f,
                    bloomOrigin,
                    pulse + i * 0.46f,
                    SpriteEffects.None);
            }

            Color starColor = new Color(105, 255, 150, 0) * (0.18f + flash * 0.18f);
            float starPulse = MathHelper.Lerp(0.2f, 1.25f, charge) * (1f + MathF.Sin(Main.GlobalTimeWrappedHourly * MathHelper.TwoPi) * 0.08f);
            Vector2 starScale = new(1.5f + charge * 1.3f, 2.5f + charge * 1.7f);
            Main.EntitySpriteDraw(star, playerScreen, null, starColor, MathHelper.PiOver4, starOrigin, starScale * starPulse, SpriteEffects.None);
            Main.EntitySpriteDraw(star, playerScreen, null, starColor * 0.65f, -MathHelper.PiOver4, starOrigin, starScale * starPulse * 0.68f, SpriteEffects.None);
            Main.EntitySpriteDraw(texture, playerScreen, null, new Color(150, 255, 160, 0) * flash * 0.32f, Main.GlobalTimeWrappedHourly * 0.5f, origin, 1.8f + charge * 2.1f, SpriteEffects.None);
        }

        private static void DrawEnemySpotlight(Vector2 targetScreen, float charge, float flash, int index)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Projectiles/StarProj").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 starOrigin = star.Size() * 0.5f;
            Vector2 top = new(targetScreen.X + MathF.Sin(Main.GlobalTimeWrappedHourly * 1.8f + index) * 28f, -90f);

            DrawSoftCone(top, targetScreen, charge * 0.82f, flash * 0.55f, 0.48f);

            Color lockColor = new Color(120, 255, 150, 0) * (0.12f + charge * 0.16f);
            float pulse = 0.48f + MathF.Sin(Main.GlobalTimeWrappedHourly * 3.2f + index) * 0.05f;
            Main.EntitySpriteDraw(
                bloom,
                targetScreen,
                null,
                lockColor * 0.5f,
                0f,
                bloomOrigin,
                new Vector2(0.72f + charge * 0.42f, 0.16f + charge * 0.08f),
                SpriteEffects.None);

            Main.EntitySpriteDraw(
                star,
                targetScreen,
                null,
                lockColor,
                MathHelper.PiOver4,
                starOrigin,
                new Vector2(0.9f, 1.45f) * pulse * charge,
                SpriteEffects.None);
        }

        private static void DrawSoftCone(Vector2 top, Vector2 bottom, float charge, float flash, float widthMultiplier = 1f)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            int layers = 5;
            float topWidth = MathHelper.Lerp(76f, 128f, charge) * widthMultiplier;
            float bottomWidth = MathHelper.Lerp(260f, 520f, charge) * widthMultiplier;

            for (int i = 0; i < layers; i++)
            {
                float progress = i / (float)(layers - 1);
                Vector2 position = Vector2.Lerp(top, bottom, progress);
                float softness = MathF.Sin(progress * MathHelper.Pi);
                float width = MathHelper.Lerp(topWidth, bottomWidth, progress) * (0.88f + softness * 0.12f);
                float height = MathHelper.Lerp(180f, 310f, progress);
                Color color = Color.Lerp(new Color(40, 125, 65, 0), new Color(150, 255, 165, 0), charge);
                color *= (0.035f + softness * 0.055f + flash * 0.04f);

                Main.EntitySpriteDraw(
                    bloom,
                    position,
                    null,
                    color,
                    0f,
                    bloomOrigin,
                    new Vector2(width / bloom.Width, height / bloom.Height),
                    SpriteEffects.None);
            }
        }

        private static void DrawPetals(Texture2D texture, Vector2 origin, float charge, bool winded, int direction)
        {
            Texture2D petal1 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT").Value;
            Texture2D petal2 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT2").Value;
            Texture2D petal3 = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT3").Value;
            int petalCount = 38;
            for (int i = 0; i < petalCount; i++)
            {
                Texture2D petalTexture = i % 3 == 0 ? petal1 : i % 3 == 1 ? petal2 : petal3;
                Vector2 petalOrigin = petalTexture.Size() * 0.5f;
                float seed = i * 37.719f;
                float fall = (Main.GlobalTimeWrappedHourly * 74f + seed) % (Main.screenHeight + 160f) - 80f;
                float x = (seed * 19f + MathF.Sin(Main.GlobalTimeWrappedHourly * 1.7f + i) * 42f) % (Main.screenWidth + 120f) - 60f;
                if (winded)
                    x = (x + fall * 0.33f * direction + Main.screenWidth + 120f) % (Main.screenWidth + 120f) - 60f;

                float rotation = Main.GlobalTimeWrappedHourly * (0.7f + i % 5 * 0.11f) + i;
                float scale = 0.13f + (i % 7) * 0.012f;
                Color color = Color.Lerp(new Color(255, 188, 220, 0), new Color(170, 255, 150, 0), i % 3 / 2f);

                Main.EntitySpriteDraw(
                    petalTexture,
                    new Vector2(x, fall),
                    null,
                    color * (0.35f + charge * 0.35f),
                    rotation,
                    petalOrigin,
                    scale,
                    SpriteEffects.None);
            }
        }
    }

    public class MalachiteFinaleSlash : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 76;
            Projectile.height = 76;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 34;
            Projectile.extraUpdates = 2;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override void AI()
        {
            Timer++;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = direction.ToRotation();
            Projectile.velocity *= 0.996f;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.8f, 0.25f);

            if (Timer == 1f)
                SoundEngine.PlaySound(SoundID.Item60 with { Volume = 0.72f, Pitch = 0.25f }, Projectile.Center);

            if (!Main.dedServ && Main.rand.NextBool())
            {
                Vector2 side = direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-32f, 32f);
                Particle line = new LineParticle(
                    Projectile.Center - direction * Main.rand.NextFloat(16f, 54f) + side,
                    -direction * Main.rand.NextFloat(4f, 10f),
                    false,
                    Main.rand.Next(10, 17),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.NextBool() ? new Color(120, 255, 145) : Color.White);
                GeneralParticleHandler.SpawnParticle(line);
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 10 * 60);
            if (Projectile.owner != Main.myPlayer)
                return;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                direction,
                ModContent.ProjectileType<MalachiteFinaleHitSlash>(),
                Math.Max(1, (int)(Projectile.damage * 0.48f)),
                0f,
                Projectile.owner,
                direction.ToRotation(),
                Main.rand.NextFloat(-0.35f, 0.35f));

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center + Main.rand.NextVector2Circular(24f, 24f),
                Vector2.Zero,
                ModContent.ProjectileType<MalachiteGreenExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 0.58f)),
                0f,
                Projectile.owner,
                1f);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 start = Projectile.Center - direction * 90f;
            Vector2 end = Projectile.Center + direction * 260f;
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), start, end, 48f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/ForwardSmear").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float fade = Utils.GetLerpValue(0f, 5f, Timer, true) * Utils.GetLerpValue(0f, 10f, Projectile.timeLeft, true);
            Color green = new Color(90, 255, 125, 0);
            Color white = Color.White with { A = 0 };

            for (int i = 0; i < 5; i++)
            {
                Vector2 offset = direction.RotatedBy(MathHelper.PiOver2) * ((i - 2) * 18f);
                Main.EntitySpriteDraw(
                    smear,
                    drawPosition + offset,
                    null,
                    Color.Lerp(green, white, i == 2 ? 0.38f : 0.08f) * fade * (0.5f - i * 0.035f),
                    direction.ToRotation() - MathHelper.PiOver2,
                    new Vector2(smear.Width * 0.5f, smear.Height),
                    new Vector2(0.052f, 1.75f + i * 0.08f),
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, drawPosition, null, green * 0.28f * fade, 0f, bloom.Size() * 0.5f, new Vector2(1.4f, 0.22f), SpriteEffects.None);

            return false;
        }
    }

    public class MalachiteFinaleHitSlash : ModProjectile, ILocalizedModType
    {
        public override string Texture => "Terraria/Images/Extra_98";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 24;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Timer++;
            Projectile.velocity *= 0f;
            Projectile.rotation = Projectile.ai[0] + Projectile.ai[1];
            Projectile.Opacity = Utils.GetLerpValue(24f, 4f, Timer, true);
            if (Timer == 1f)
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.35f, Pitch = 0.45f, MaxInstances = 6 }, Projectile.Center);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - direction * 120f, Projectile.Center + direction * 120f, 36f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Extra[ExtrasID.SharpTears].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Color color = new Color(100, 255, 130, 0) * Projectile.Opacity;

            Main.EntitySpriteDraw(texture, drawPosition, null, color, Projectile.rotation, origin, new Vector2(4.1f, 0.55f), SpriteEffects.None);
            Main.EntitySpriteDraw(texture, drawPosition, null, Color.White * 0.42f * Projectile.Opacity, Projectile.rotation + MathHelper.PiOver2, origin, new Vector2(1.3f, 0.16f), SpriteEffects.None);
            return false;
        }
    }

    public class MalachiteFinaleDart : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];
        private int Delay => Math.Max(0, (int)Projectile.ai[0]);
        private int Variant => Utils.Clamp((int)Projectile.ai[1], 0, 2);
        private bool Launched => Timer > Delay;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 24;
            Projectile.height = 24;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 0;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 6;
        }

        public override bool ShouldUpdatePosition() => Launched;

        public override bool? CanDamage() => Launched;

        public override void AI()
        {
            Timer++;
            Projectile.friendly = Launched;

            if (!Launched)
            {
                Projectile.rotation += 0.08f + Variant * 0.02f;
                Projectile.scale = 0.76f + MathF.Sin((Timer + Projectile.identity) * 0.18f) * 0.045f;
                Projectile.alpha = (int)MathHelper.Lerp(30f, 0f, Utils.GetLerpValue(0f, Delay, Timer, true));
                Lighting.AddLight(Projectile.Center, 0.1f, 0.25f, 0.07f);
                return;
            }

            if (Timer == Delay + 1f)
            {
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SwooshMid") { Volume = 0.28f, Pitch = 0.28f, MaxInstances = 8 }, Projectile.Center);
                SoundEngine.PlaySound(SoundID.Item1 with { Volume = 0.25f, Pitch = 0.46f, MaxInstances = 8 }, Projectile.Center);
                Projectile.netUpdate = true;
            }

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Projectile.rotation = direction.ToRotation() + MathHelper.PiOver2;
            Projectile.velocity *= 1.002f;
            Projectile.scale = MathHelper.Lerp(Projectile.scale, 1.08f, 0.08f);
            Lighting.AddLight(Projectile.Center, 0.12f, 0.36f, 0.08f);
            SpawnFlightVisuals(direction);
        }

        private void SpawnFlightVisuals(Vector2 direction)
        {
            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center - direction * Main.rand.NextFloat(6f, 16f) + Main.rand.NextVector2Circular(5f, 5f),
                    Main.rand.NextBool(30) ? DustID.Terra : DustID.GreenTorch,
                    -direction * Main.rand.NextFloat(0.5f, 1.8f) + Main.rand.NextVector2Circular(0.45f, 0.45f),
                    120,
                    default,
                    Main.rand.NextBool(30) ? Main.rand.NextFloat(0.85f, 1.05f) : Main.rand.NextFloat(0.32f, 0.48f));
                dust.noGravity = true;
            }

            if (Timer % 8f != 0f)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                (Main.rand.NextBool(3) ? Color.LimeGreen : Color.Green) * 0.35f,
                Vector2.One,
                Projectile.rotation,
                Main.rand.NextFloat(0.04f, 0.08f),
                0f,
                15));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 8 * 60);
            target.AddBuff(ModContent.BuffType<Plague>(), 5 * 60);

            if (Projectile.owner != Main.myPlayer)
                return;

            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<MalachiteFinaleImpactExplosion>(),
                Math.Max(1, (int)(Projectile.damage * 0.42f)),
                0f,
                Projectile.owner,
                Projectile.rotation);
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (!Launched)
                return false;

            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(), Projectile.Center - direction * 18f, Projectile.Center + direction * 42f, 18f, ref collisionPoint);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D petal = GetPetalTexture();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;

            if (!Launched)
            {
                Vector2 petalOrigin = petal.Size() * 0.5f;
                Color petalColor = Color.Lerp(new Color(255, 206, 226, 0), new Color(170, 255, 150, 0), 0.6f) * Projectile.Opacity;
                Main.EntitySpriteDraw(petal, drawPosition, null, petalColor, Projectile.rotation, petalOrigin, Projectile.scale * 0.64f, SpriteEffects.None);
                return false;
            }

            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Projectile.type], lightColor, 1);

            Vector2 origin = texture.Size() * 0.5f;
            Color glowColor = new Color(95, 255, 125, 0) * 0.46f;
            for (int i = 0; i < 6; i++)
            {
                Vector2 offset = (MathHelper.TwoPi * i / 6f).ToRotationVector2() * 3.4f;
                Main.EntitySpriteDraw(texture, drawPosition + offset, null, glowColor, Projectile.rotation, origin, Projectile.scale * 0.82f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(texture, drawPosition, null, Color.Lerp(lightColor, Color.White, 0.54f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None);
            return false;
        }

        private Texture2D GetPetalTexture()
        {
            string suffix = Variant == 0 ? string.Empty : (Variant + 1).ToString();
            return ModContent.Request<Texture2D>($"CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT{suffix}").Value;
        }
    }

    public class MalachiteFinaleImpactExplosion : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/Malachite";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private ref float Timer => ref Projectile.localAI[0];

        public override void SetDefaults()
        {
            Projectile.width = 154;
            Projectile.height = 154;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 24;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override bool ShouldUpdatePosition() => false;

        public override bool? CanDamage() => Timer <= 3f;

        public override void AI()
        {
            Timer++;

            if (Timer == 1f)
            {
                Projectile.Resize(154, 154);
                Projectile.Damage();
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/SporeKnifeImpact") { Volume = 0.34f, Pitch = 0.25f, MaxInstances = 5 }, Projectile.Center);
                SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/PlagueSounds/PlagueBoom" + Main.rand.Next(1, 5)) { Volume = 0.28f, Pitch = 0.35f, MaxInstances = 4 }, Projectile.Center);
                SpawnImpactBurst();
            }

            Lighting.AddLight(Projectile.Center, 0.16f, 0.46f, 0.08f);
            SpawnLingeringDust();
        }

        private void SpawnImpactBurst()
        {
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                Projectile.Center,
                Vector2.Zero,
                Color.GreenYellow * 0.55f,
                Vector2.One,
                Projectile.ai[0],
                0.13f,
                0.42f,
                18));

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2Unit() * Main.rand.NextFloat(1.2f, 5.2f);
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(42f, 42f),
                    Main.rand.NextBool(4) ? DustID.GreenTorch : DustID.Terra,
                    velocity,
                    80,
                    Main.rand.NextBool() ? new Color(100, 255, 120) : new Color(185, 255, 100),
                    Main.rand.NextFloat(0.74f, 1.22f));
                dust.noGravity = true;
            }
        }

        private void SpawnLingeringDust()
        {
            if (!Main.rand.NextBool(2))
                return;

            Vector2 radial = Main.rand.NextVector2Unit();
            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + radial * Main.rand.NextFloat(12f, 66f),
                Main.rand.NextBool(30) ? DustID.Terra : DustID.GreenTorch,
                radial.RotatedBy(MathHelper.PiOver2 * Main.rand.NextFloatDirection()) * Main.rand.NextFloat(0.5f, 1.8f),
                115,
                default,
                Main.rand.NextBool(30) ? Main.rand.NextFloat(0.85f, 1.05f) : Main.rand.NextFloat(0.28f, 0.42f));
            dust.noGravity = true;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(BuffID.Poisoned, 8 * 60);
            target.AddBuff(ModContent.BuffType<Plague>(), 5 * 60);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            float completion = Timer / 24f;
            float pulse = MathF.Sin(MathHelper.Clamp(completion, 0f, 1f) * MathHelper.Pi);
            float fade = Utils.GetLerpValue(1f, 0.62f, completion, true);

            Main.EntitySpriteDraw(bloom, drawPosition, null, new Color(60, 255, 110, 0) * (0.18f * fade), 0f, bloomOrigin, 0.92f + pulse * 0.42f, SpriteEffects.None);

            for (int i = 0; i < 5; i++)
            {
                float rotation = MathHelper.TwoPi * i / 5f + Projectile.ai[0] + Timer * 0.035f;
                Main.EntitySpriteDraw(
                    texture,
                    drawPosition,
                    null,
                    new Color(105, 255, 125, 0) * (0.12f + pulse * 0.19f) * fade,
                    rotation,
                    origin,
                    1.05f + pulse * 0.35f,
                    SpriteEffects.None);
            }

            return false;
        }
    }

    public class MalachiteFinalePetal : ModProjectile, ILocalizedModType
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT";

        public override string LocalizationCategory => "Projectiles.Malachite";

        private bool ReadyPetal => Projectile.ai[0] >= 0f;
        private bool Fading => Projectile.localAI[2] >= 1f;
        private int Variant => Utils.Clamp((int)Projectile.ai[1], 0, 2);

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = false;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 140;
            Projectile.DamageType = ModContent.GetInstance<RogueDamageClass>();
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            Projectile.localAI[0]++;
            float direction = Projectile.ai[2] == 0f ? Math.Sign(Projectile.velocity.X) : Projectile.ai[2];
            if (direction == 0f)
                direction = 1f;

            float sway = MathF.Sin(Projectile.localAI[0] * 0.055f + Projectile.identity * 0.37f);
            Projectile.velocity.X += direction * (ReadyPetal ? 0.005f : 0.008f) + sway * 0.035f;
            Projectile.velocity.Y += ReadyPetal ? 0.018f : 0.026f;

            float maxSpeed = ReadyPetal ? 5.2f : 7.2f;
            if (Projectile.velocity.Length() > maxSpeed)
                Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitY) * maxSpeed;

            float glow = Utils.GetLerpValue(64f, 112f, Projectile.localAI[0], true);
            float fade = Fading || Projectile.timeLeft < 20
                ? Utils.GetLerpValue(18f, 0f, Projectile.timeLeft, true)
                : 0f;

            Projectile.rotation += direction * 0.055f + Projectile.velocity.X * 0.014f;
            Projectile.scale = (ReadyPetal ? 0.84f : 0.7f) + sway * 0.04f;
            Projectile.alpha = (int)MathHelper.Lerp(MathHelper.Lerp(58f, 0f, Utils.GetLerpValue(0f, 24f, Projectile.localAI[0], true)), 238f, fade);
            Lighting.AddLight(Projectile.Center, 0.06f + glow * 0.08f, 0.18f + glow * 0.2f, 0.05f);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = GetPetalTexture();
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float glow = Utils.GetLerpValue(64f, 112f, Projectile.localAI[0], true);
            float fade = Fading || Projectile.timeLeft < 20
                ? Utils.GetLerpValue(18f, 0f, Projectile.timeLeft, true)
                : 0f;
            Color baseColor = Color.Lerp(new Color(255, 206, 226, 0), new Color(170, 255, 150, 0), 0.45f + glow * 0.35f) * Projectile.Opacity;
            Vector2 scale = Vector2.One * Projectile.scale * 0.72f;

            if (glow > 0f && fade < 0.9f)
            {
                Main.EntitySpriteDraw(
                    bloom,
                    drawPosition,
                    null,
                    new Color(105, 255, 135, 0) * glow * 0.1f * Projectile.Opacity,
                    0f,
                    bloomOrigin,
                    0.12f + glow * 0.16f,
                    SpriteEffects.None);
            }

            Main.EntitySpriteDraw(
                texture,
                drawPosition,
                null,
                baseColor,
                Projectile.rotation,
                origin,
                scale,
                SpriteEffects.None);

            return false;
        }

        private Texture2D GetPetalTexture()
        {
            string suffix = Variant == 0 ? string.Empty : (Variant + 1).ToString();
            return ModContent.Request<Texture2D>($"CalamityLegendsComeBack/Weapons/Malachite/EXSkill/MalachiteSPIT{suffix}").Value;
        }
    }
}
