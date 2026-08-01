using System;
using CalamityMod;
using CalamityMod.Buffs.StatDebuffs;
using CalamityMod.Dusts;
using CalamityMod.Enums;
using CalamityMod.Graphics.Primitives;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.Graphics.Shaders;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.AntiMaterielRifle.Proj
{
    internal sealed class AMRRound : ModProjectile, ILocalizedModType
    {
        private const float GoldenAngle = 2.39996323f;

        private bool hitAnyTarget;
        private bool configured;
        private int visualAgeFrames;

        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/Ranged/AMRShot";

        private bool IsAimedShot => Projectile.ai[0] > 0.001f;
        private bool IsMarkerRound => Projectile.ai[1] >= 0.5f;

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 20;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 14;
            Projectile.height = 14;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.extraUpdates = 15;
            Projectile.timeLeft = 240;
            Projectile.scale = 1.55f;
            Projectile.light = 0.75f;
            Projectile.ArmorPenetration = 25;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override void AI()
        {
            if (!configured)
            {
                configured = true;
                Projectile.penetrate = IsAimedShot ? -1 : 1;
                Projectile.ArmorPenetration = IsAimedShot ? 45 : 25;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, new Color(255, 196, 64).ToVector3() * 0.7f);

            // 星流/终灾阶段：引爆弹 (第二发) 穿过物块上的玛瑙标记判定圆时，触发引爆
            if (AMRBalance.OnyxSequenceUnlocked && !IsMarkerRound && Projectile.owner == Main.myPlayer)
            {
                CheckTileMarkerDetonation();
            }

            if (!CalamityUtils.FinalExtraUpdate(Projectile))
                return;

            visualAgeFrames++;
            SpawnFlightEffects();
        }

        private void CheckTileMarkerDetonation()
        {
            int markerType = ModContent.ProjectileType<AMROnyxTileMarker>();
            foreach (Projectile proj in Main.ActiveProjectiles)
            {
                if (proj.type == markerType && proj.owner == Projectile.owner)
                {
                    if (Vector2.Distance(Projectile.Center, proj.Center) <= AMROnyxTileMarker.TriggerRadius)
                    {
                        if (proj.ModProjectile is AMROnyxTileMarker tileMarker && tileMarker.IsTileMarker)
                        {
                            int weaponDamage = Projectile.owner >= 0 ? Main.player[Projectile.owner].GetWeaponDamage(Main.player[Projectile.owner].HeldItem) : Projectile.damage;
                            tileMarker.Detonate((int)(weaponDamage * 2.0f)); // 200% 攻击力伤害
                        }
                    }
                }
            }
        }

        // 主弹飞行期生成沿弹道反向的粉尘/粒子
        private void SpawnFlightEffects()
        {
            if (Main.dedServ)
                return;

            int steps = Projectile.extraUpdates + 1;
            Vector2 oldCenter = Projectile.Center - Projectile.velocity * steps;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            float frameTravel = Projectile.velocity.Length() * steps;

            float cullingMargin = frameTravel + 120f;
            Rectangle expandedView = new(
                (int)(Main.screenPosition.X - cullingMargin),
                (int)(Main.screenPosition.Y - cullingMargin),
                (int)(Main.screenWidth + cullingMargin * 2f),
                (int)(Main.screenHeight + cullingMargin * 2f));
            if (!expandedView.Contains(Projectile.Center.ToPoint()))
                return;

            int orbCount = Math.Clamp((int)(frameTravel / 58f), 1, 4);
            for (int i = 0; i < orbCount; i++)
            {
                float completion = (i + 1f) / (orbCount + 1f);
                Vector2 spawnPosition = Vector2.Lerp(oldCenter, Projectile.Center, completion);
                Color orbColor = i % 2 == 0 ? new Color(255, 211, 102) : new Color(30, 26, 22);
                float scale = i % 2 == 0 ? 0.34f : 0.27f;
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    spawnPosition,
                    -forward * 0.55f,
                    false,
                    11,
                    scale,
                    orbColor,
                    true,
                    false,
                    true));
            }

            if (visualAgeFrames % 2 == 0)
            {
                Vector2 sparkPosition = Projectile.Center - forward * Math.Min(frameTravel * 0.42f, 150f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    sparkPosition,
                    -forward * 1.35f,
                    new Color(255, 250, 222),
                    new Color(255, 184, 54),
                    0.34f,
                    10,
                    0.08f,
                    2.35f));
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.ScalingArmorPenetration += IsAimedShot ? 0.55f : 0.35f;

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
            {
                AMRPlayer player = Main.player[Projectile.owner].GetModPlayer<AMRPlayer>();
                modifiers.SourceDamage *= player.GetCalibrationMultiplier(target.whoAmI);
            }

            // 困难模式边缘准星判定：主子弹命中准星造成 1.25 倍伤害并触发涡旋爆发
            if (AMRBalance.BullseyeUnlocked)
            {
                int bullseyeType = ModContent.ProjectileType<AMRBullseye>();
                foreach (Projectile proj in Main.ActiveProjectiles)
                {
                    if (proj.type == bullseyeType && proj.owner == Projectile.owner && (int)proj.ai[0] == target.whoAmI)
                    {
                        if (proj.ModProjectile is AMRBullseye bullseye && proj.Opacity > 0.2f)
                        {
                            int steps = Projectile.extraUpdates + 1;
                            Vector2 oldCenter = Projectile.Center - Projectile.velocity * steps;
                            Vector2 seg = Projectile.Center - oldCenter;
                            float segLenSq = seg.LengthSquared();
                            float t = segLenSq > 0.0001f ? MathHelper.Clamp(Vector2.Dot(bullseye.Projectile.Center - oldCenter, seg) / segLenSq, 0f, 1f) : 0f;
                            Vector2 closestPoint = oldCenter + seg * t;
                            float distToBullseye = Vector2.Distance(closestPoint, bullseye.Projectile.Center);

                            if (distToBullseye <= 45f)
                            {
                                modifiers.SourceDamage *= 1.25f;
                                bullseye.TriggerVortexImpactExplosion(target);
                                break;
                            }
                        }
                    }
                }
            }

            if (AMRBalance.CriticalOverflowUnlocked && Projectile.CritChance > 100)
                modifiers.CritDamage += (Projectile.CritChance - 100) * 2 / 100f;

            if (AMRBalance.CoreRuptureUnlocked)
                modifiers.CritDamage += IsAimedShot ? 0.75f : 0.5f;

            if (!Main.dedServ)
            {
                // Rubico 风格：主弹幕命中时两侧飞溅高能量线状火花
                Vector2 backward = -Projectile.velocity.SafeNormalize(Vector2.UnitX);
                for (int i = 0; i < 3; i++)
                {
                    LineParticle sparkLeft = new LineParticle(
                        Projectile.Center,
                        backward.RotatedBy(Main.rand.NextFloat(0.18f, 0.44f)) * Main.rand.NextFloat(1.5f, 4f),
                        false,
                        10,
                        1f,
                        Main.rand.NextBool() ? new Color(25, 22, 18) : new Color(255, 195, 40));
                    GeneralParticleHandler.SpawnParticle(sparkLeft);

                    LineParticle sparkRight = new LineParticle(
                        Projectile.Center,
                        backward.RotatedBy(Main.rand.NextFloat(-0.18f, -0.44f)) * Main.rand.NextFloat(1.5f, 4f),
                        false,
                        10,
                        1f,
                        Main.rand.NextBool() ? new Color(218, 165, 32) : new Color(45, 38, 18));
                    GeneralParticleHandler.SpawnParticle(sparkRight);
                }
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            hitAnyTarget = true;

            if (AMRBalance.TryGetMaxLifeTrueDamage(target, out int finalTrueDamage))
            {
                target.life -= finalTrueDamage;
                CombatText.NewText(target.getRect(), new Color(255, 140, 40), finalTrueDamage, true);

                if (target.life <= 0)
                    target.checkDead();
            }

            if (AMRBalance.DeathMarkUnlocked)
            {
                target.AddBuff(ModContent.BuffType<MarkedforDeath>(), 5 * 60);
                int defenseLoss = Math.Max(25, (int)(target.defense * 0.6f));
                target.Calamity().miscDefenseLoss = Math.Max(target.Calamity().miscDefenseLoss, defenseLoss);
            }

            if (AMRBalance.MetalJetUnlocked && Projectile.owner == Main.myPlayer)
            {
                SpawnMetalJetProjectiles(target.Center);
                SpawnMetalJet(target.Center);
            }

            if (AMRBalance.CoreRuptureUnlocked && Projectile.owner == Main.myPlayer)
            {
                SpawnSubBullets(target, hit.Crit);
            }

            if (AMRBalance.Stage >= AMRProgressionStage.DevourerOfGods && target.active)
            {
                bool isBossOrSpecial = target.boss || target.type == NPCID.TargetDummy || target.realLife >= 0;
                if (!isBossOrSpecial && target.lifeMax < 1000000)
                {
                    target.life = 0;
                    target.checkDead();
                }
            }

            if (Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<AMRPlayer>().RegisterCalibrationHit(target.whoAmI);

            if (AMRBalance.OnyxSequenceUnlocked)
                ResolveOnyxSequence(target, damageDone);

            SpawnImpact(target.Center, hit.Crit);
        }

        private void SpawnMetalJetProjectiles(Vector2 impactCenter)
        {
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            int count = 5;
            int shardDamage = (int)(Projectile.damage * 0.5f);

            for (int i = 0; i < count; i++)
            {
                float angle = MathHelper.ToRadians(Main.rand.NextFloat(-45f, 45f));
                Vector2 shardVel = forward.RotatedBy(angle) * Main.rand.NextFloat(7f, 18f);

                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    impactCenter + forward * 10f,
                    shardVel,
                    ModContent.ProjectileType<AMRMetalJetShard>(),
                    Math.Max(1, shardDamage),
                    Projectile.knockBack * 0.3f,
                    Projectile.owner,
                    15f);
            }
        }

        private void SpawnSubBullets(NPC target, bool critical)
        {
            int bulletCount;
            float damageRatio;
            float knockbackRatio;
            float speed;

            if (IsAimedShot)
            {
                if (!critical)
                    return;

                bulletCount = 6;
                damageRatio = 0.175f;
                knockbackRatio = 1f;
                speed = 12f;
            }
            else
            {
                bulletCount = critical ? 4 : 2;
                damageRatio = 0.15f;
                knockbackRatio = 0.1f;
                speed = 10f;
            }

            int subDamage = Math.Max(1, (int)(Projectile.damage * damageRatio));

            for (int i = 0; i < bulletCount; i++)
            {
                bool fromRight = IsAimedShot ? i < bulletCount / 2 : i >= bulletCount / 2;
                CalamityUtils.ProjectileBarrage(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center,
                    target.Center,
                    fromRight,
                    500f,
                    500f,
                    0f,
                    500f,
                    speed,
                    ModContent.ProjectileType<AMRSubBullet>(),
                    subDamage,
                    Projectile.knockBack * knockbackRatio,
                    Projectile.owner);
            }
        }

        private void ResolveOnyxSequence(NPC target, int damageDone)
        {
            AMRMarkerGlobalNPC marker = target.GetGlobalNPC<AMRMarkerGlobalNPC>();
            if (IsMarkerRound)
            {
                marker.SetMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone));
                SpawnOnyxTargetMarker(target);
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.35f, Pitch = -0.45f }, target.Center);
                return;
            }

            if (!marker.TryConsumeMarker(Projectile.owner, Math.Max(Projectile.damage, damageDone), out int detonationDamage))
                return;

            if (Projectile.owner != Main.myPlayer)
                return;

            RemoveOnyxTargetMarker(target.whoAmI);

            int detonation = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                target.Center,
                Vector2.Zero,
                ModContent.ProjectileType<AMROnyxDetonation>(),
                detonationDamage,
                Projectile.knockBack * 1.5f,
                Projectile.owner,
                target.whoAmI);
            if (Main.projectile.IndexInRange(detonation))
                Main.projectile[detonation].CritChance = Projectile.CritChance;
        }

        private void SpawnOnyxTargetMarker(NPC target)
        {
            if (Projectile.owner != Main.myPlayer)
                return;

            RemoveOnyxTargetMarker(target.whoAmI);

            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 markerPosition = Projectile.Center - forward * 9f;
            Vector2 targetOffset = markerPosition - target.Center;
            Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                markerPosition,
                targetOffset,
                ModContent.ProjectileType<AMROnyxTileMarker>(),
                0,
                0f,
                Projectile.owner,
                forward.ToRotation() + MathHelper.PiOver2,
                target.whoAmI + 1f);
        }

        private void RemoveOnyxTargetMarker(int targetIndex)
        {
            int markerType = ModContent.ProjectileType<AMROnyxTileMarker>();
            foreach (Projectile markerProjectile in Main.ActiveProjectiles)
            {
                if (markerProjectile.type == markerType && markerProjectile.owner == Projectile.owner &&
                    markerProjectile.ModProjectile is AMROnyxTileMarker marker &&
                    marker.IsAttachedTo(targetIndex))
                {
                    markerProjectile.Kill();
                }
            }
        }

        private void SpawnMetalJet(Vector2 impactCenter)
        {
            Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-direction.Y, direction.X);
            for (int i = 0; i < 12; i++)
            {
                float distance = MathHelper.Lerp(-10f, 46f, i / 11f);
                float sineOffset = MathF.Sin(i * GoldenAngle) * 2.2f;
                Dust jet = Dust.NewDustPerfect(
                    impactCenter + direction * distance + normal * sineOffset,
                    i % 3 == 0 ? DustID.Torch : DustID.GoldFlame,
                    direction * MathHelper.Lerp(1.5f, 5f, i / 11f),
                    55,
                    new Color(255, 202, 81),
                    MathHelper.Lerp(0.6f, 1.05f, i / 11f));
                jet.noGravity = true;
            }
        }

        private void SpawnImpact(Vector2 center, bool critical)
        {
            if (Main.dedServ)
                return;

            float strength = critical ? 1.3f : 1f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 normal = new(-forward.Y, forward.X);
            float rotation = forward.ToRotation();

            if (Main.LocalPlayer.active && !Main.LocalPlayer.dead)
            {
                Main.LocalPlayer.Calamity().GeneralScreenShakePower = Math.Max(
                    Main.LocalPlayer.Calamity().GeneralScreenShakePower,
                    critical ? 7.5f : 4f);
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center,
                forward * 0.45f,
                new Color(255, 215, 116),
                new Vector2(0.24f, 0.82f),
                rotation,
                0.06f,
                1.05f * strength,
                21));
            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(
                center + forward * 6f,
                forward * 0.8f,
                new Color(125, 151, 184),
                new Vector2(0.38f, 1.18f),
                rotation,
                0.03f,
                1.42f * strength,
                25));

            int orbCount = critical ? 22 : 15;
            for (int i = 0; i < orbCount; i++)
            {
                float spread = i / (orbCount - 1f) - 0.5f;
                float angle = MathHelper.ToRadians(70f) * spread + MathHelper.ToRadians(Main.rand.NextFloat(-2.5f, 2.5f));
                float centerWeight = 1f - MathF.Abs(spread * 2f);
                float speed = MathHelper.Lerp(4.5f, 15.5f, centerWeight) * Main.rand.NextFloat(0.88f, 1.12f) * strength;
                Color color = Color.Lerp(new Color(30, 25, 20), new Color(255, 220, 119), centerWeight);
                Vector2 spawnPosition = center + forward * 5f + normal * spread * 8f;
                GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
                    spawnPosition,
                    forward.RotatedBy(angle) * speed,
                    false,
                    Main.rand.Next(14, 22),
                    Main.rand.NextFloat(0.32f, 0.58f) * strength,
                    color,
                    true,
                    false,
                    true));
            }

            int sparkCount = critical ? 10 : 6;
            for (int i = 0; i < sparkCount; i++)
            {
                float spread = i / (sparkCount - 1f) - 0.5f;
                Vector2 velocity = forward.RotatedBy(MathHelper.ToRadians(54f) * spread) *
                    Main.rand.NextFloat(5.5f, 11f) * strength;
                GeneralParticleHandler.SpawnParticle(new CritSpark(
                    center + forward * 7f,
                    velocity,
                    new Color(255, 252, 228),
                    new Color(255, 171, 46),
                    Main.rand.NextFloat(0.35f, 0.55f) * strength,
                    Main.rand.Next(12, 19),
                    Main.rand.NextFloat(-0.18f, 0.18f),
                    2.4f));
            }

            Vector2 splatterDirection = -forward;
            int gravitySparkCount = critical ? 16 : 10;
            for (int i = 0; i < gravitySparkCount; i++)
            {
                int sparkLifetime = Main.rand.Next(22, 38);
                float sparkScale = Main.rand.NextFloat(0.85f, 1.25f) * (critical ? 1.25f : 1f);
                if (Main.rand.NextBool(10))
                    sparkScale *= 2f;

                Color sparkColor = Color.Lerp(new Color(25, 22, 18), Color.Gold, Main.rand.NextFloat(0.7f));

                Vector2 sparkVelocity = splatterDirection.RotatedByRandom(0.75f) * Main.rand.NextFloat(11f, 24f) * strength;
                sparkVelocity.Y -= Main.rand.NextFloat(4f, 8f);

                GeneralParticleHandler.SpawnParticle(new SparkParticle(
                    center,
                    sparkVelocity,
                    true,
                    sparkLifetime,
                    sparkScale,
                    sparkColor));
            }

            GeneralParticleHandler.SpawnParticle(new ImpactParticle(
                center,
                Main.rand.NextFloat(-0.2f, 0.2f),
                20,
                critical ? 0.95f : 0.7f,
                Color.Lerp(Color.Silver, Color.Gold, 0.55f)));

            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                center + forward * 4f,
                forward * 0.3f,
                new Color(255, 247, 220),
                0.42f * strength,
                10,
                false));

            for (int sign = -1; sign <= 1; sign += 2)
            {
                GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(
                    center + forward * 10f,
                    forward.RotatedBy(MathHelper.ToRadians(24f) * sign) * 2.2f,
                    new Color(30, 25, 20),
                    22,
                    0.42f * strength,
                    0.62f,
                    sign * 0.025f,
                    false));
            }
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            Collision.HitTiles(Projectile.position, Projectile.velocity, Projectile.width, Projectile.height);

            if (AMRBalance.MetalJetUnlocked && Projectile.owner == Main.myPlayer)
            {
                SpawnMetalJetProjectiles(Projectile.Center);
            }

            if (AMRBalance.OnyxSequenceUnlocked && IsMarkerRound && Projectile.owner == Main.myPlayer)
            {
                Vector2 forward = oldVelocity.SafeNormalize(Vector2.UnitX);
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center - forward * 9f,
                    Vector2.Zero,
                    ModContent.ProjectileType<AMROnyxTileMarker>(),
                    0,
                    0f,
                    Projectile.owner,
                    forward.ToRotation() + MathHelper.PiOver2,
                    0f);
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.4f, Pitch = -0.3f }, Projectile.Center);
            }

            SpawnImpact(Projectile.Center, false);
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.7f }, Projectile.Center);
            return true;
        }

        public override void OnKill(int timeLeft)
        {
            if (!hitAnyTarget && Projectile.owner >= 0 && Projectile.owner < Main.maxPlayers)
                Main.player[Projectile.owner].GetModPlayer<AMRPlayer>().ResetCalibration();
        }

        public override Color? GetAlpha(Color lightColor)
        {
            return new Color(255, 248, 205) * Projectile.Opacity;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            DrawShaderTrail();

            Texture2D texture = TextureAssets.Projectile[Type].Value;
            Vector2 origin = texture.Size() * 0.5f;
            Vector2 forward = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 drawCenter = Projectile.Center - Main.screenPosition;

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 bloomOrigin = bloom.Size() * 0.5f;

            Main.EntitySpriteDraw(bloom, drawCenter, null, new Color(255, 168, 28, 0), 0f,
                bloomOrigin, 0.19f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, drawCenter + forward * 2f, null, new Color(255, 252, 232, 0), 0f,
                bloomOrigin, 0.075f, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(texture, drawCenter, null, new Color(255, 244, 198, 0),
                Projectile.rotation, origin,
                new Vector2(Projectile.scale * 1.9f, Projectile.scale * 0.92f),
                SpriteEffects.None, 0f);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }

        private void DrawShaderTrail()
        {
            Vector2[] trailPoints = BuildFlightTrailPoints();
            if (trailPoints.Length < 3)
                return;

            MiscShaderData trailShader = GameShaders.Misc["CalamityMod:TrailStreak"];

            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"));
            PrimitiveRenderer.RenderTrail(
                trailPoints,
                new PrimitiveSettings(
                    ShockTrailWidth,
                    ShockTrailColor,
                    (_, _) => Vector2.Zero,
                    smoothen: true,
                    pixelate: false,
                    shader: trailShader),
                trailPoints.Length * 2);

            int corePointCount = Math.Min(10, trailPoints.Length);
            if (corePointCount < 3)
                return;

            Vector2[] corePoints = new Vector2[corePointCount];
            Array.Copy(trailPoints, corePoints, corePointCount);
            
            trailShader.SetShaderTexture(
                ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/BasicTrail"));
            PrimitiveRenderer.RenderTrail(
                corePoints,
                new PrimitiveSettings(
                    PenetratorCoreWidth,
                    PenetratorCoreColor,
                    (_, _) => Vector2.Zero,
                    smoothen: true,
                    pixelate: false,
                    shader: trailShader),
                corePoints.Length * 2);
        }

        private Vector2[] BuildFlightTrailPoints()
        {
            Vector2[] points = new Vector2[Projectile.oldPos.Length + 3];
            int count = 0;
            points[count++] = Projectile.Center;

            Vector2 lastPoint = Projectile.Center;
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                if (Projectile.oldPos[i] == Vector2.Zero)
                    continue;

                Vector2 point = Projectile.oldPos[i] + Projectile.Size * 0.5f;
                if (Vector2.DistanceSquared(point, lastPoint) < 4f)
                    continue;

                points[count++] = point;
                lastPoint = point;
            }

            while (count < 3)
            {
                points[count] = Projectile.Center - Projectile.velocity * count;
                count++;
            }

            Array.Resize(ref points, count);
            return points;
        }

        private float ShockTrailWidth(float completion, Vector2 _)
        {
            return TaperedTrailWidth(completion, 18f * Projectile.scale / 1.55f, 0.06f, 0.55f);
        }

        private Color ShockTrailColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.42f, 1f, completion, true);
            Color shock = Color.Lerp(new Color(150, 176, 205), new Color(30, 26, 22), completion);
            return shock * (tailFade * 0.34f);
        }

        private float PenetratorCoreWidth(float completion, Vector2 _)
        {
            return TaperedTrailWidth(completion, 2.6f * Projectile.scale / 1.55f, 0.05f, 0.85f);
        }

        private Color PenetratorCoreColor(float completion, Vector2 _)
        {
            float tailFade = 1f - Utils.GetLerpValue(0.5f, 1f, completion, true);
            Color core = Color.Lerp(new Color(255, 255, 248), new Color(255, 186, 46), completion * 0.8f);
            return core * tailFade;
        }

        private static float TaperedTrailWidth(float completion, float maximumWidth, float headFraction, float tailPower)
        {
            if (completion < headFraction)
            {
                float headCompletion = MathHelper.Clamp(completion / headFraction, 0f, 1f);
                return MathHelper.SmoothStep(0.4f, maximumWidth, headCompletion);
            }

            float tailCompletion = MathHelper.Clamp((completion - headFraction) / (1f - headFraction), 0f, 1f);
            return maximumWidth * MathF.Pow(1f - tailCompletion, tailPower);
        }
    }

    internal sealed class AMRMarkerGlobalNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private readonly int[] markerTimers = new int[Main.maxPlayers];
        private readonly int[] markerDamages = new int[Main.maxPlayers];

        public override void PostAI(NPC npc)
        {
            for (int i = 0; i < markerTimers.Length; i++)
            {
                if (markerTimers[i] > 0)
                    markerTimers[i]--;
                else
                    markerDamages[i] = 0;
            }
        }

        internal void SetMarker(int owner, int damage)
        {
            if (owner < 0 || owner >= markerTimers.Length)
                return;

            markerTimers[owner] = 5 * 60;
            markerDamages[owner] = Math.Max(1, damage);
        }

        internal bool TryConsumeMarker(int owner, int detonatorDamage, out int damage)
        {
            damage = 0;
            if (owner < 0 || owner >= markerTimers.Length || markerTimers[owner] <= 0)
                return false;

            damage = Math.Max((int)(detonatorDamage * 1.8f), markerDamages[owner] * 2);
            markerTimers[owner] = 0;
            markerDamages[owner] = 0;
            return true;
        }
    }

    internal sealed class AMROnyxDetonation : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "Terraria/Images/Projectile_661";

        public override void SetDefaults()
        {
            Projectile.width = 96;
            Projectile.height = 96;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex >= 0 && targetIndex < Main.maxNPCs && Main.npc[targetIndex].active)
                Projectile.Center = Main.npc[targetIndex].Center;

            if (Projectile.timeLeft == 3)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.75f, Pitch = -0.35f }, Projectile.Center);
                for (int i = 0; i < 28; i++)
                {
                    Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.PurpleTorch,
                        Main.rand.NextVector2Unit() * Main.rand.NextFloat(3f, 12f), 50,
                        Main.rand.NextBool() ? new Color(104, 44, 176) : new Color(52, 128, 194),
                        Main.rand.NextFloat(0.9f, 1.7f));
                    dust.noGravity = true;
                }

                SpawnOnyxVoidBurst();
            }
        }

        private void SpawnOnyxVoidBurst()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 3; i++)
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    Projectile.Center,
                    Vector2.Zero,
                    new Color(42, 16, 88),
                    "CalamityMod/Particles/SmallBloom",
                    Vector2.One,
                    Main.rand.NextFloat(-10f, 10f),
                    1.9f + i * 0.55f,
                    0f,
                    46,
                    true));
            }

            GeneralParticleHandler.SpawnParticle(new GenericBloom(
                Projectile.Center,
                Vector2.Zero,
                new Color(42, 16, 88),
                1.1f,
                26,
                false,
                true));

            Particle violetRing = new CustomPulse(
                Projectile.Center,
                Vector2.Zero,
                new Color(96, 40, 172),
                "CalamityMod/Particles/BloomRing",
                Vector2.One,
                Main.rand.NextFloat(-10f, 10f),
                0f,
                2.35f,
                20);
            GeneralParticleHandler.SpawnParticle(violetRing);
            violetRing.DrawLayer = GeneralDrawLayer.AfterEverything;

            for (int i = 0; i < 24; i++)
            {
                float variance = Main.rand.NextFloat(-0.8f, 0.8f);
                Dust voidDust = Dust.NewDustPerfect(Projectile.Center, ModContent.DustType<VoidDustInverted>());
                voidDust.noGravity = true;
                voidDust.velocity = new Vector2(14f, 14f).RotatedByRandom(MathHelper.TwoPi) *
                    Main.rand.NextFloat(0.15f, 1f) * (1f - MathF.Abs(variance) * 0.4f);
                voidDust.scale = Main.rand.NextFloat(1.2f, 2.1f);
                voidDust.color = Main.rand.NextBool(3) ? new Color(52, 22, 104) : new Color(44, 108, 170);
            }

            for (int i = 0; i < 9; i++)
            {
                Particle blackShard = new CustomSpark(
                    Projectile.Center,
                    Main.rand.NextVector2CircularEdge(6.5f, 6.5f) * Main.rand.NextFloat(0.45f, 1f),
                    Main.rand.NextBool() ? "CalamityMod/Particles/GlowSpark2" : "CalamityMod/Particles/GlowSpark",
                    false,
                    Main.rand.Next(16, 24),
                    Main.rand.NextFloat(0.04f, 0.085f),
                    new Color(42, 16, 88),
                    new Vector2(0.55f, 1.45f),
                    true);
                GeneralParticleHandler.SpawnParticle(blackShard);
                blackShard.DrawLayer = GeneralDrawLayer.AfterEverything;
            }
        }

        public override bool? CanHitNPC(NPC target)
        {
            return target.whoAmI == (int)Projectile.ai[0] ? null : false;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            float completion = 1f - Projectile.timeLeft / 3f;
            float scale = MathHelper.Lerp(0.15f, 1.25f, completion);

            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.Additive, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                new Color(104, 44, 176) * (1f - completion) * 0.85f, 0f, bloom.Size() * 0.5f, scale,
                SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null,
                new Color(126, 186, 232) * (0.55f - completion * 0.4f), 0f, bloom.Size() * 0.5f,
                scale * 0.45f, SpriteEffects.None, 0f);
            Main.spriteBatch.End();
            Main.spriteBatch.Begin(SpriteSortMode.Deferred, BlendState.AlphaBlend, Main.DefaultSamplerState,
                DepthStencilState.None, Main.Rasterizer, null, Main.GameViewMatrix.TransformationMatrix);

            return false;
        }
    }

    internal sealed class AMRSlideExplosion : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.AntiMaterielRifle";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 160;
            Projectile.height = 160;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 3;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            if (Projectile.timeLeft == 3)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.85f, Pitch = -0.25f }, Projectile.Center);
                if (!Main.dedServ)
                {
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                        new Color(255, 195, 58), new Vector2(1f, 1f), 0f, 0.08f, 1.4f, 18));
                    GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                        new Color(150, 70, 20), new Vector2(1.5f, 1.5f), 0f, 0.05f, 2.0f, 22));

                    for (int i = 0; i < 20; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(12f, 12f);
                        GeneralParticleHandler.SpawnParticle(new LineParticle(Projectile.Center, vel, false, 15, 0.5f,
                            i % 2 == 0 ? new Color(255, 210, 82) : new Color(200, 100, 30)));
                    }

                    for (int i = 0; i < 8; i++)
                    {
                        Vector2 vel = Main.rand.NextVector2Circular(5f, 5f);
                        GeneralParticleHandler.SpawnParticle(new HeavySmokeParticle(Projectile.Center, vel,
                            new Color(25, 20, 15), 24, 0.85f, 0.8f, 0.03f, false, required: true));
                    }
                }
            }
        }
    }
}
