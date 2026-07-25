using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CalamityMod;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using ReLogic.Content;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;


namespace CalamityLegendsComeBack.Weapons.GaelsGreatsword
{
    internal sealed class GaelGreatswordSwingHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const float SwordVisualScale = 1.42f;
        private const float BladeReach = 130f;
        private const int TrailHistoryFrames = 18;

        // 硫火红-虚空黑配色，与至尊灾厄同源。BrimBody = 常规挥砍的深硫红，
        // BloodRed = 黑血强化后的炽硫红，PaleCore = 命中最内层的灼白心。
        private static readonly Color BrimBody = GaelGreatswordVisuals.BrimstoneRed;
        private static readonly Color BloodRed = GaelGreatswordVisuals.BrimstoneHot;
        private static readonly Color PaleCore = GaelGreatswordVisuals.WhiteHot;

        private readonly Vector2[] bladeTipHistory = new Vector2[TrailHistoryFrames];
        private Player Owner => Main.player[Projectile.owner];
        private bool FollowupSlash => Projectile.ai[0] == 1f;

        private int bladeTipHistoryLength;
        private int swingTimer;
        private int swingCount;
        private int comboIndex;
        private int swingDirection = 1;
        private bool bloodCostChecked;
        private bool comboPayloadReleased;
        private float bloodDamageMultiplier = 1f;
        private float scale = 1f;
        private float currentAngle;
        private float startAngle;
        private float windupAngle;
        private float endAngle;
        private float slashOpacity;
        private float outlinePulse;
        private Vector2 lockedDirection = Vector2.UnitX;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 62;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = GaelGreatswordProgression.GetRepeatHitCooldown();
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendGaelsGreatsword>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.localNPCHitCooldown = GaelGreatswordProgression.GetRepeatHitCooldown();
            scale = Owner.GetMeleeScale() * SwordVisualScale;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 4;

            DoSwing();

            float armAngle = currentAngle - MathHelper.ToRadians(130f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = currentAngle;
        }

        public override bool? CanDamage()
        {
            float progress = GetProgress();
            return progress >= 0.24f && progress <= 0.9f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 bladeTip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Owner.MountedCenter, bladeTip, (FollowupSlash ? 30f : 25f) * scale, ref collisionPoint) ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            float groupReduction = MathHelper.Lerp(1f, 0.38f, MathHelper.Clamp(Projectile.numHits / 6f, 0f, 1f));
            modifiers.SourceDamage *= groupReduction * bloodDamageMultiplier;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            GaelGreatswordPlayer gaelPlayer = Owner.GetModPlayer<GaelGreatswordPlayer>();
            gaelPlayer.RegisterGreatswordHit(target, FollowupSlash ? 11 : 7, bloodDamageMultiplier > 1f);

            outlinePulse = 1f;
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, FollowupSlash ? 3f : 2.2f);
            SpawnHitImpactEffects(target, hit.Crit);

            if (Main.myPlayer == Projectile.owner && Main.rand.NextBool(FollowupSlash ? 1 : 2))
            {
                Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(70f, 70f);
                Vector2 velocity = spawnPosition.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(6.5f, 9.5f);
                int soulDamage = Math.Max(1, (int)(Projectile.damage * (FollowupSlash ? 0.28f : 0.18f)));
                Projectile.NewProjectile(Projectile.GetSource_OnHit(target), spawnPosition, velocity,
                    ModContent.ProjectileType<GaelGreatswordDarkSoul>(), soulDamage, 1f, Projectile.owner, target.whoAmI);
            }

            SoundEngine.PlaySound(SoundID.NPCHit4 with { Volume = 0.45f, Pitch = -0.25f }, target.Center);
        }

        private void SpawnHitImpactEffects(NPC target, bool crit)
        {
            if (Main.dedServ)
                return;

            Vector2 direction = currentAngle.ToRotationVector2();
            bool empowered = bloodDamageMultiplier > 1f;
            Color bodyColor = empowered ? BloodRed : BrimBody;
            Color accent = empowered ? GaelGreatswordVisuals.EmberGold : GaelGreatswordVisuals.CrimsonViolet;
            float power = crit ? 1f : 0.72f;

            // 双脉冲环：外圈沿刃向拉扁的硫红冲击环，内圈近圆的灼白心。
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero,
                bodyColor, new Vector2(1.05f, 0.6f), direction.ToRotation(), 0.08f, crit ? 0.9f : 0.7f, 14));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero,
                PaleCore, new Vector2(0.7f, 0.7f), direction.ToRotation(), 0.04f, crit ? 0.5f : 0.38f, 11));

            // 硫火辉光核心 + 灼白强光：加算发光团块撑起命中的"重量"。
            GeneralParticleHandler.SpawnParticle(new BloomParticle(target.Center, Vector2.Zero,
                bodyColor * 0.5f, 0.05f, 0.62f * power + 0.2f, 16, false));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(target.Center, Vector2.Zero,
                PaleCore * (crit ? 0.6f : 0.42f), crit ? 0.6f : 0.42f, 10));

            // 熔火元球：一两枚随机崩飞的硫火团块，是"和 SCal 一个妈生的"关键笔触。
            int blobCount = crit ? 3 : 2;
            for (int i = 0; i < blobCount; i++)
            {
                Vector2 blobVel = direction.RotatedByRandom(0.7f) * Main.rand.NextFloat(1.5f, 4.5f);
                GaelGreatswordVisuals.SpawnBrimstoneMetaball(target.Center + blobVel, blobVel,
                    Main.rand.NextFloat(18f, 30f) * (empowered ? 1.15f : 1f), 0.8f);
            }

            // 烬火光珠 + 硫火火星：沿刃向法线崩出的碎火。
            int orbCount = crit ? 4 : 2;
            for (int i = 0; i < orbCount; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.85f) * Main.rand.NextFloat(1.6f, 4.5f);
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(target.Center + velocity * 0.4f, velocity, false,
                    Main.rand.Next(11, 17), Main.rand.NextFloat(0.24f, 0.4f),
                    Main.rand.NextBool() ? bodyColor : accent, true, false));
            }

            int sparkCount = crit ? 10 : 6;
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.75f) * Main.rand.NextFloat(3f, crit ? 11f : 8.5f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(target.Center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity, Color.White, Main.rand.NextBool() ? accent : PaleCore,
                    Main.rand.NextFloat(0.35f, 0.75f), Main.rand.Next(9, 15)));
            }

            // 硫火尘埃碎溅 + 一口火焰喷进流体场，命中点像被烧穿一样冒火。
            for (int i = 0; i < (crit ? 8 : 5); i++)
                GaelGreatswordVisuals.SpawnBrimstoneDust(target.Center,
                    direction.RotatedByRandom(1f) * Main.rand.NextFloat(2f, 6.5f), Main.rand.NextFloat(1f, 1.6f));
            GaelGreatswordVisuals.RegisterBrimstoneFire(target.Center, direction * 1.2f,
                0.6f * power + 0.3f, empowered ? 0.45f : 0.25f);
        }

        private void DoSwing()
        {
            if (swingTimer == 0)
                StartSwing();

            swingTimer++;
            if (outlinePulse > 0f)
                outlinePulse = Math.Max(0f, outlinePulse - 0.09f);

            float progress = GetProgress();
            if (progress <= 0.2f)
            {
                float windupProgress = MathHelper.Clamp(progress / 0.2f, 0f, 1f);
                currentAngle = MathHelper.Lerp(startAngle, windupAngle, CalamityUtils.EaseInOutExp(windupProgress, 4f, 4f));
            }
            else
            {
                float strikeProgress = MathHelper.Clamp((progress - 0.2f) / 0.8f, 0f, 1f);
                currentAngle = MathHelper.Lerp(windupAngle, endAngle, CalamityUtils.EaseInOutExp(strikeProgress, 6f, 2f));

                // 斩击正中的正弦膨胀：大剑在挥击高潮瞬间胀大，剑更重、更狠。
                float swellBell = MathF.Sin(strikeProgress * MathHelper.Pi);
                scale *= 1f + swellBell * (FollowupSlash ? 0.19f : 0.13f);
            }

            bool hitWindow = progress >= 0.24f && progress <= 0.9f;
            if (hitWindow && !bloodCostChecked)
            {
                bloodDamageMultiplier = Owner.GetModPlayer<GaelGreatswordPlayer>().TryPayBlackBloodCost();
                bloodCostChecked = true;
                if (bloodDamageMultiplier > 1f)
                    SoundEngine.PlaySound(SoundID.Item171 with { Volume = 0.55f, Pitch = -0.22f }, Owner.Center);
            }

            TrackBladeTrail(hitWindow);
            EmitSwingEffects(hitWindow, progress);
            ReleaseComboPayload(hitWindow, progress);

            if (swingTimer < GaelGreatswordProgression.GetSwingDuration(Owner, FollowupSlash))
                return;

            if (!FollowupSlash && IsLeftHeld())
            {
                swingCount++;
                Projectile.ResetLocalNPCHitImmunity();
                swingTimer = 0;
                bloodCostChecked = false;
                comboPayloadReleased = false;
                bloodDamageMultiplier = 1f;
                return;
            }

            Projectile.Kill();
        }

        private void StartSwing()
        {
            swingTimer = 0;
            bloodCostChecked = false;
            comboPayloadReleased = false;
            bloodDamageMultiplier = 1f;
            lockedDirection = GetMouseDirection();
            comboIndex = Owner.GetModPlayer<GaelGreatswordPlayer>().ConsumeLeftComboIndex(FollowupSlash);

            swingDirection = -Math.Sign(Owner.Center.X - NewLegendGaelsGreatsword.GetMouseWorld(Owner).X);
            if (swingDirection == 0)
                swingDirection = Owner.direction;
            Owner.direction = swingDirection;

            float baseAngle = lockedDirection.ToRotation();
            int parity = swingCount % 2 == 0 ? 1 : -1;

            if (FollowupSlash)
            {
                startAngle = baseAngle + MathHelper.ToRadians(-92f * swingDirection);
                windupAngle = baseAngle + MathHelper.ToRadians(-118f * swingDirection);
                endAngle = baseAngle + MathHelper.ToRadians(86f * swingDirection);
            }
            else
            {
                startAngle = baseAngle + MathHelper.ToRadians(-78f * swingDirection * parity);
                windupAngle = baseAngle + MathHelper.ToRadians(-122f * swingDirection * parity);
                endAngle = baseAngle + MathHelper.ToRadians(112f * swingDirection * parity);
            }

            currentAngle = startAngle;
            SoundEngine.PlaySound(SoundID.Item1 with { Volume = FollowupSlash ? 0.74f : 0.58f, Pitch = FollowupSlash ? -0.35f : -0.18f }, Owner.Center);
        }

        private void ReleaseComboPayload(bool hitWindow, float progress)
        {
            if (!hitWindow || comboPayloadReleased || progress < 0.34f || Main.myPlayer != Projectile.owner)
                return;

            comboPayloadReleased = true;
            Vector2 aim = lockedDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
            Vector2 perpendicular = aim.RotatedBy(MathHelper.PiOver2);
            Vector2 basePosition = Owner.MountedCenter + aim * 44f;
            int totalCrit = (int)Math.Round(Owner.GetTotalCritChance(Projectile.DamageType));

            int SpawnComboProjectile(int projectileType, Vector2 position, Vector2 velocity, float damageFactor, float knockbackFactor = 1f, float ai0 = -1f, float ai1 = 0f)
            {
                int damage = Math.Max(1, (int)(Projectile.damage * damageFactor * bloodDamageMultiplier));
                int projectileIndex = Projectile.NewProjectile(Projectile.GetSource_FromThis(), position, velocity, projectileType,
                    damage, Projectile.knockBack * knockbackFactor, Projectile.owner, ai0, ai1);
                if (projectileIndex >= 0 && projectileIndex < Main.maxProjectiles)
                    Main.projectile[projectileIndex].CritChance = totalCrit;
                return projectileIndex;
            }

            switch (comboIndex)
            {
                case 0:
                    for (int i = 0; i < 2; i++)
                    {
                        float offset = MathHelper.Lerp(-0.16f, 0.16f, i);
                        int skull = SpawnComboProjectile(ModContent.ProjectileType<GaelGreatswordDarkSoul>(),
                            basePosition + perpendicular * (i == 0 ? -18f : 18f),
                            aim.RotatedBy(offset) * 13.5f, 0.34f, 0.8f);
                        if (skull.WithinBounds(Main.maxProjectiles))
                            Main.projectile[skull].scale = 0.92f;
                    }
                    SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.28f, Pitch = 0.22f }, basePosition);
                    break;

                case 1:
                    int giantSkull = SpawnComboProjectile(ModContent.ProjectileType<GaelGreatswordDarkSoul>(),
                        basePosition + aim * 14f, aim * 8.2f, 0.72f, 1.25f);
                    if (giantSkull.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[giantSkull].scale = 1.85f;
                        Main.projectile[giantSkull].penetrate = -1;
                        Main.projectile[giantSkull].timeLeft = 110;
                    }
                    SoundEngine.PlaySound(SoundID.NPCDeath52 with { Volume = 0.45f, Pitch = -0.34f }, basePosition);
                    break;

                case 2:
                    for (int i = 0; i < 5; i++)
                    {
                        float spread = MathHelper.Lerp(-0.34f, 0.34f, i / 4f);
                        SpawnComboProjectile(ModContent.ProjectileType<GaelGreatswordBrimstoneDart>(),
                            basePosition - aim * 20f + perpendicular * MathHelper.Lerp(-30f, 30f, i / 4f),
                            aim.RotatedBy(spread) * 15.6f, 0.28f, 0.7f);
                    }
                    SoundEngine.PlaySound(SoundID.Item20 with { Volume = 0.48f, Pitch = -0.15f }, basePosition);
                    break;

                case 3:
                    for (int i = 0; i < 3; i++)
                    {
                        float side = i - 1f;
                        Vector2 spawnPosition = Owner.MountedCenter - aim * (72f + i * 8f) + perpendicular * side * 64f;
                        Vector2 velocity = (aim * 8f + perpendicular * -side * 2.4f).RotatedBy(Main.rand.NextFloat(-0.1f, 0.1f));
                        SpawnComboProjectile(ModContent.ProjectileType<GaelGreatswordVengefulSoul>(), spawnPosition, velocity, 0.4f, 0.85f);
                    }
                    SoundEngine.PlaySound(SoundID.Item103 with { Volume = 0.42f, Pitch = -0.24f }, basePosition);
                    break;

                case 4:
                    // ai1 是灾厄斩击的存活计时器，必须从 0 起跳；朝向完全由 ai0 的锁定角决定。
                    SpawnComboProjectile(ModContent.ProjectileType<GaelGreatswordCatastropheSlash>(),
                        basePosition + aim * 58f, aim * 9.4f, 0.62f, 1.05f, aim.ToRotation(), 0f);
                    SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/ExobladeBeamSlash") { Volume = 0.52f, Pitch = -0.18f }, basePosition);
                    break;

                default:
                    Vector2 targetPoint = NewLegendGaelsGreatsword.GetMouseWorld(Owner);
                    for (int i = 0; i < 4; i++)
                    {
                        float offset = (i - 1.5f) * 48f;
                        Vector2 spawnPosition = targetPoint + new Vector2(offset, -190f - i * 12f);
                        Vector2 velocity = spawnPosition.DirectionTo(targetPoint + perpendicular * offset * 0.24f).SafeNormalize(Vector2.UnitY) * 17f;
                        SpawnComboProjectile(ModContent.ProjectileType<GaelGreatswordCondemnationArrow>(),
                            spawnPosition, velocity, 0.31f, 0.65f);
                    }
                    SoundEngine.PlaySound(SoundID.Item5 with { Volume = 0.5f, Pitch = 0.06f }, targetPoint);
                    break;
            }
        }

        private float GetProgress()
        {
            return MathHelper.Clamp(swingTimer / (float)GaelGreatswordProgression.GetSwingDuration(Owner, FollowupSlash), 0f, 1f);
        }

        private bool IsLeftHeld()
        {
            return Owner.channel && (Main.myPlayer != Projectile.owner || Main.mouseLeft) &&
                !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        private Vector2 GetMouseDirection()
        {
            return Owner.MountedCenter.DirectionTo(NewLegendGaelsGreatsword.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
        }

        private void TrackBladeTrail(bool hitWindow)
        {
            if (!hitWindow)
            {
                slashOpacity = MathHelper.Lerp(slashOpacity, 0f, 0.26f);
                if (slashOpacity < 0.02f)
                    bladeTipHistoryLength = 0;
                return;
            }

            slashOpacity = MathHelper.Lerp(slashOpacity, 1f, FollowupSlash ? 0.55f : 0.42f);
            Vector2 tipOffset = currentAngle.ToRotationVector2() * BladeReach * scale;
            Array.Copy(bladeTipHistory, 0, bladeTipHistory, 1, bladeTipHistory.Length - 1);
            bladeTipHistory[0] = tipOffset;
            if (bladeTipHistoryLength < bladeTipHistory.Length)
                bladeTipHistoryLength++;
        }

        private void EmitSwingEffects(bool hitWindow, float progress)
        {
            if (Main.dedServ || !hitWindow)
                return;

            Vector2 direction = currentAngle.ToRotationVector2();
            Vector2 perpendicular = direction.RotatedBy(MathHelper.PiOver2 * Math.Sign(endAngle - startAngle));
            bool empowered = bloodDamageMultiplier > 1f;
            Color bodyColor = empowered ? BloodRed : BrimBody;

            // 刃缘硫火尘：沿剑身外段撒硫火尘（偶掺黑烟），被挥砍甩向切线法线。
            int dustCount = FollowupSlash ? 3 : 2;
            for (int i = 0; i < dustCount; i++)
            {
                Vector2 position = Owner.MountedCenter + direction * Main.rand.NextFloat(46f, BladeReach) * scale;
                Vector2 velocity = perpendicular * Main.rand.NextFloat(1.2f, FollowupSlash ? 5.5f : 4f) + direction * Main.rand.NextFloat(-1f, 1f);
                GaelGreatswordVisuals.SpawnBrimstoneDust(position, velocity, Main.rand.NextFloat(1f, 1.55f) * scale);
            }

            // 流体火焰尾焰：在刃身中外段向共享硫火场投喂火源，剑锋所过之处烧起真正流动的火。
            Vector2 firePos = Owner.MountedCenter + direction * Main.rand.NextFloat(64f, BladeReach) * scale;
            GaelGreatswordVisuals.RegisterBrimstoneFire(firePos, perpendicular * 1.4f + direction * 0.6f,
                (empowered ? 0.55f : 0.4f) * (FollowupSlash ? 1.2f : 1f), empowered ? 0.4f : 0.24f);

            // 熔火元球顺着剑尖甩出，拖出一道熔岩弧。
            if ((swingTimer & 1) == 0)
            {
                Vector2 tip = Owner.MountedCenter + direction * BladeReach * scale * Main.rand.NextFloat(0.82f, 1f);
                GaelGreatswordVisuals.SpawnBrimstoneMetaball(tip,
                    perpendicular * Main.rand.NextFloat(1.5f, 4f) * Math.Sign(endAngle - startAngle),
                    Main.rand.NextFloat(14f, 22f) * scale * (FollowupSlash ? 1.15f : 1f), 0.75f);
            }

            // 强化时额外掀起一缕黑烟，读出"黑血沸腾"的分量。
            if (empowered && Main.rand.NextBool(3))
            {
                Vector2 smokePos = Owner.MountedCenter + direction * Main.rand.NextFloat(40f, BladeReach) * scale;
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(smokePos,
                    perpendicular * Main.rand.NextFloat(0.6f, 2f), Color.Lerp(GaelGreatswordVisuals.VoidSmoke, BloodRed, 0.35f),
                    Main.rand.Next(22, 34), Main.rand.NextFloat(0.4f, 0.7f), 0.55f, Main.rand.NextFloat(-0.03f, 0.03f), true));
            }

            // 剑尖闪星：斩击弧线最外缘随行一缕烬火星光。
            if (Main.rand.NextBool(3))
            {
                Vector2 tipPosition = Owner.MountedCenter + direction * BladeReach * scale * Main.rand.NextFloat(0.88f, 1f);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(tipPosition,
                    perpendicular * Main.rand.NextFloat(1.5f, 4f), Color.White,
                    empowered ? GaelGreatswordVisuals.EmberGold : bodyColor,
                    Main.rand.NextFloat(0.4f, 0.7f), Main.rand.Next(9, 14), Main.rand.NextFloat(-0.1f, 0.1f), 2.2f));
            }
        }

        private void DrawBladeTrail()
        {
            if (bladeTipHistoryLength < 2 || slashOpacity <= 0.01f || Main.dedServ)
                return;

            Main.spriteBatch.EnterShaderRegion();
            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            // Voronoi 灰度纹理染成硫红-虚空黑后读作"龟裂的熔岩/硫火"，正是 SCal 的质感。
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            slashShader.UseColor(bloodDamageMultiplier > 1f ? BloodRed : BrimBody);
            slashShader.UseSecondaryColor(GaelGreatswordVisuals.VoidSmoke);
            slashShader.Shader.Parameters["fireColor"].SetValue((bloodDamageMultiplier > 1f
                ? GaelGreatswordVisuals.EmberGold : GaelGreatswordVisuals.WhiteHot).ToVector3());
            slashShader.Shader.Parameters["flipped"].SetValue(Owner.direction == 1);
            slashShader.Apply();

            List<Vector2> trailPoints = new(bladeTipHistoryLength);
            for (int i = 0; i < bladeTipHistoryLength; i++)
                trailPoints.Add(bladeTipHistory[i]);

            float WidthFunction(float completionRatio, Vector2 _)
            {
                float width = FollowupSlash ? 34f : 25f;
                return scale * width * Utils.GetLerpValue(1f, 0f, completionRatio, true) * slashOpacity;
            }

            Color ColorFunction(float completionRatio, Vector2 _)
            {
                return Color.White * Utils.GetLerpValue(0.95f, 0.2f, completionRatio, true) * slashOpacity;
            }

            PrimitiveRenderer.RenderTrail(trailPoints,
                new PrimitiveSettings(WidthFunction, ColorFunction, (_, _) => Owner.MountedCenter, shader: slashShader),
                TrailHistoryFrames);
            Main.spriteBatch.ExitShaderRegion();
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D swooshTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D bloomTexture = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = new(0f, swordTexture.Height);
            float drawRotation = currentAngle + MathHelper.PiOver4;
            Vector2 drawPosition = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);

            if (slashOpacity > 0.01f)
            {
                bool empowered = bloodDamageMultiplier > 1f;
                Color slashColor = empowered ? BloodRed : BrimBody;
                Color coreColor = empowered ? GaelGreatswordVisuals.EmberGold : PaleCore;
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                float swipeDirection = Math.Sign(endAngle - startAngle);
                float swooshRotation = drawRotation + MathHelper.PiOver2 * swipeDirection;
                Vector2 swooshOrigin = swooshTexture.Size() * 0.5f;
                // 宽体深硫红弧 + 窄芯烬火/灼白弧：SCal 式"外暗内炽"的双层刀光。
                Main.EntitySpriteDraw(swooshTexture, drawPosition, null, slashColor with { A = 0 } * slashOpacity * (FollowupSlash ? 0.62f : 0.46f),
                    swooshRotation, swooshOrigin, scale * (FollowupSlash ? 0.9f : 0.78f), SpriteEffects.None);
                Main.EntitySpriteDraw(swooshTexture, drawPosition, null, coreColor with { A = 0 } * slashOpacity * 0.34f,
                    swooshRotation, swooshOrigin, scale * (FollowupSlash ? 0.62f : 0.52f), SpriteEffects.None);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 3.1f * slashOpacity;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, slashColor with { A = 0 } * slashOpacity * 0.07f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                Vector2 bladeTip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale - Main.screenPosition;
                Main.EntitySpriteDraw(bloomTexture, bladeTip, null, PaleCore with { A = 0 } * slashOpacity * 0.48f,
                    0f, bloomTexture.Size() * 0.5f, scale * 0.44f, SpriteEffects.None);

                DrawBladeRim(bloomTexture);
                Main.spriteBatch.ExitShaderRegion();
            }

            // 命中脉冲包边：击中目标的一瞬，剑身向外扩散一层苍白轮廓光。
            if (outlinePulse > 0.01f)
            {
                Color pulseColor = bloodDamageMultiplier > 1f ? BloodRed : PaleCore;
                float pulseRadius = MathHelper.Lerp(3.5f, 7.5f, outlinePulse) * scale;
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                for (int i = 0; i < 12; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 12f).ToRotationVector2() * pulseRadius;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null,
                        pulseColor with { A = 0 } * outlinePulse * 0.22f, drawRotation, origin, scale, SpriteEffects.None);
                }
                Main.spriteBatch.ExitShaderRegion();
            }

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            DrawBladeTrail();
            return false;
        }

        private void DrawBladeRim(Texture2D bloomTexture)
        {
            // 沿剑身铺一条由暗紫渐亮到苍白的流光棱线（参考庇护之刃 DrawForwardLightRim）。
            const int rimDraws = 30;
            Color rimBase = bloodDamageMultiplier > 1f ? BloodRed : BrimBody;
            Vector2 forward = currentAngle.ToRotationVector2();
            Vector2 drawBase = Owner.MountedCenter + forward * (BladeReach * 0.1f * scale) - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float step = BladeReach * 0.9f / rimDraws * scale;

            for (int i = 0; i < rimDraws; i++)
            {
                float colorT = i / (float)rimDraws;
                bool nearTip = i > rimDraws * 0.72f;
                float tipTaper = nearTip ? Utils.Remap(i, (int)(rimDraws * 0.72f), rimDraws, 0.9f, 0.35f) : 1f;
                Color rimColor = Color.Lerp(rimBase, PaleCore, 0.2f + 0.55f * colorT) with { A = 0 };
                Vector2 offset = forward * (i * step) + Main.rand.NextVector2Circular(1.4f, 1.4f);
                Vector2 rimScale = new Vector2(0.5f * tipTaper, 0.2f) * 0.6f * tipTaper * scale;

                Main.EntitySpriteDraw(bloomTexture, drawBase + offset, null, rimColor * 0.3f * slashOpacity,
                    currentAngle, bloomTexture.Size() * 0.5f, rimScale, SpriteEffects.None);
            }
        }
    }
}
