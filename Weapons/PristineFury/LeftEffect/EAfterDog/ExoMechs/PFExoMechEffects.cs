// using CalamityMod;
// using CalamityMod.Dusts;
// using CalamityMod.Particles;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;
// using ReLogic.Content;
// using System;
// using Terraria;
// using Terraria.Audio;
// using Terraria.Enums;
// using Terraria.ID;
// using Terraria.ModLoader;
// 
// namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
// {
// #if false
//     // Temporarily disabled: Pristine Fury currently retains only Thanatos among the Exo Mechs.
//     internal static class PFExoTwinsEffect
//     {
//         private const int RocketCount = 3;
//         private const int RocketInterval = 6;
//         private const int CycleCooldown = 36;
// 
//         internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
//         {
//             if (!held)
//             {
//                 PristineFuryLeftEffectRegistry.Reset(holdout);
//                 return;
//             }
// 
//             if (holdout.LeftChargeTimer > 0)
//             {
//                 holdout.LeftChargeTimer--;
//                 return;
//             }
// 
//             if (holdout.LeftAuxTimer > 0)
//             {
//                 holdout.LeftTimer--;
//                 if (holdout.LeftTimer <= 0)
//                 {
//                     FireRocket(holdout, RocketCount - holdout.LeftAuxTimer);
//                     holdout.LeftAuxTimer--;
//                     holdout.LeftTimer = RocketInterval;
// 
//                     if (holdout.LeftAuxTimer <= 0)
//                         holdout.LeftChargeTimer = CycleCooldown;
//                 }
// 
//                 return;
//             }
// 
//             FireArtemisLaser(holdout);
//             holdout.LeftAuxTimer = RocketCount;
//             holdout.LeftTimer = 5;
//         }
// 
//         private static void FireArtemisLaser(NewLegendPristineFuryHoldOut holdout)
//         {
//             Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
//             Vector2 muzzle = holdout.GunTipPosition + direction * 16f;
//             int projectileIndex = Projectile.NewProjectile(
//                 holdout.Projectile.GetSource_FromThis(),
//                 muzzle,
//                 direction,
//                 ModContent.ProjectileType<PFExoTwins_ArtemisLaser>(),
//                 holdout.GetScaledDamage(1.05f),
//                 holdout.Projectile.knockBack * 0.8f,
//                 holdout.Projectile.owner,
//                 46f);
// 
//             PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
//             holdout.ApplyRecoil(8.5f);
//             holdout.TriggerMuzzleFlash(16);
//             holdout.SpawnMuzzleBurst(new Color(74, 255, 180), 1.08f);
//             SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.6f, Pitch = 0.18f, MaxInstances = 4 }, muzzle);
//         }
// 
//         private static void FireRocket(NewLegendPristineFuryHoldOut holdout, int shotIndex)
//         {
//             Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
//             float spread = (shotIndex - 1) * 0.12f + Main.rand.NextFloat(-0.035f, 0.035f);
//             Vector2 velocity = direction.RotatedBy(spread) * Main.rand.NextFloat(14.8f, 16.6f);
//             Vector2 muzzle = holdout.GunTipPosition + direction * 20f + direction.RotatedBy(MathHelper.PiOver2) * (shotIndex - 1) * 4f;
// 
//             int projectileIndex = Projectile.NewProjectile(
//                 holdout.Projectile.GetSource_FromThis(),
//                 muzzle,
//                 velocity,
//                 ModContent.ProjectileType<PFExoTwins_ApolloRocket>(),
//                 holdout.GetScaledDamage(0.74f),
//                 holdout.Projectile.knockBack * 0.9f,
//                 holdout.Projectile.owner,
//                 0f,
//                 shotIndex);
// 
//             PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
//             holdout.ApplyRecoil(3.2f);
//             holdout.TriggerMuzzleFlash(10);
//             SoundEngine.PlaySound(SoundID.Item11 with { Volume = 0.45f, Pitch = 0.28f, MaxInstances = 6 }, muzzle);
//         }
//     }
// 
//     internal sealed class PFExoTwins_ArtemisLaser : ModProjectile, ILocalizedModType
//     {
//         private const float TelegraphTotalTime = 14f;
//         private const float TelegraphFadeTime = 7f;
//         private const float TelegraphWidth = 3600f;
// 
//         public new string LocalizationCategory => "Projectiles.PristineFury";
//         public override string Texture => "CalamityMod/Projectiles/Boss/ArtemisLaser";
// 
//         private ref float LaserVelocity => ref Projectile.ai[0];
//         private ref float DirectionRotation => ref Projectile.ai[1];
//         private ref float TelegraphDelay => ref Projectile.localAI[1];
// 
//         public override void SetStaticDefaults()
//         {
//             Main.projFrames[Type] = 4;
//             ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
//             ProjectileID.Sets.TrailCacheLength[Type] = 8;
//             ProjectileID.Sets.TrailingMode[Type] = 0;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 22;
//             Projectile.height = 22;
//             Projectile.friendly = true;
//             Projectile.hostile = false;
//             Projectile.DamageType = DamageClass.Ranged;
//             Projectile.ignoreWater = true;
//             Projectile.tileCollide = false;
//             Projectile.alpha = 255;
//             Projectile.penetrate = -1;
//             Projectile.extraUpdates = 1;
//             Projectile.timeLeft = 46;
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = 7;
//         }
// 
//         public override void AI()
//         {
//             if (Projectile.localAI[0] == 0f)
//             {
//                 Projectile.localAI[0] = 1f;
//                 DirectionRotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
//                 Projectile.velocity = Vector2.Zero;
//                 Projectile.netUpdate = true;
//             }
// 
//             Projectile.frameCounter++;
//             if (Projectile.frameCounter > 12)
//             {
//                 Projectile.frame++;
//                 Projectile.frameCounter = 0;
//             }
//             if (Projectile.frame > 3)
//                 Projectile.frame = 0;
// 
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(74, 255, 180));
//             Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.65f);
// 
//             if (TelegraphDelay > TelegraphTotalTime)
//             {
//                 if (Projectile.alpha > 0)
//                     Projectile.alpha = Math.Max(0, Projectile.alpha - 35);
// 
//                 if (Projectile.velocity == Vector2.Zero)
//                 {
//                     Projectile.extraUpdates = Main.getGoodWorld ? 4 : 3;
//                     Projectile.velocity = DirectionRotation.ToRotationVector2() * Math.Max(1f, LaserVelocity);
//                     Projectile.netUpdate = true;
//                 }
// 
//                 if (Projectile.velocity.Length() < LaserVelocity)
//                 {
//                     Projectile.velocity *= 1f + LaserVelocity * 0.0016667f;
//                     if (Projectile.velocity.Length() > LaserVelocity)
//                         Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX) * LaserVelocity;
//                 }
// 
//                 Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
//                 Projectile.rotation = Projectile.spriteDirection == -1
//                     ? (float)Math.Atan2(-Projectile.velocity.Y, -Projectile.velocity.X)
//                     : Projectile.velocity.ToRotation();
//             }
//             else
//             {
//                 Projectile.spriteDirection = DirectionRotation.ToRotationVector2().X < 0f ? -1 : 1;
//                 Projectile.rotation = DirectionRotation;
//             }
// 
//             TelegraphDelay++;
//         }
// 
//         public override bool? CanDamage() => TelegraphDelay > TelegraphTotalTime ? null : false;
// 
//         public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
//             CalamityUtils.CircularHitboxCollision(projHitbox.Center(), Projectile.Size.Length() * 0.5f, targetHitbox);
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             if (TelegraphDelay >= TelegraphTotalTime)
//             {
//                 lightColor = Color.White * Projectile.Opacity;
//                 Vector2 drawOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * -30f;
//                 Projectile.Center += drawOffset;
//                 CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
//                 Projectile.Center -= drawOffset;
//                 return false;
//             }
// 
//             Texture2D laserTelegraph = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam").Value;
//             float yScale = 2f;
//             if (TelegraphDelay < TelegraphFadeTime)
//                 yScale = MathHelper.Lerp(0f, 2f, TelegraphDelay / TelegraphFadeTime);
//             if (TelegraphDelay > TelegraphTotalTime - TelegraphFadeTime)
//                 yScale = MathHelper.Lerp(2f, 0f, (TelegraphDelay - (TelegraphTotalTime - TelegraphFadeTime)) / TelegraphFadeTime);
// 
//             Vector2 scaleInner = new(TelegraphWidth / laserTelegraph.Width, yScale);
//             Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
//             Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.2f);
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(74, 255, 180));
//             Color colorOuter = Color.Lerp(theme, Color.OrangeRed, TelegraphDelay / TelegraphTotalTime * 2f % 1f) * 0.6f;
//             Color colorInner = Color.Lerp(colorOuter, Color.White, 0.75f) * 0.75f;
//             float rotation = DirectionRotation;
// 
//             Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, rotation, origin, scaleInner, SpriteEffects.None, 0);
//             Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, rotation, origin, scaleOuter, SpriteEffects.None, 0);
//             return false;
//         }
//     }
// 
//     internal sealed class PFExoTwins_ApolloRocket : ModProjectile, ILocalizedModType
//     {
//         public new string LocalizationCategory => "Projectiles.PristineFury";
//         public override string Texture => "CalamityMod/Projectiles/Boss/ApolloRocket";
// 
//         public override void SetStaticDefaults()
//         {
//             Main.projFrames[Type] = 5;
//             ProjectileID.Sets.TrailCacheLength[Type] = 4;
//             ProjectileID.Sets.TrailingMode[Type] = 0;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 28;
//             Projectile.height = 28;
//             Projectile.friendly = true;
//             Projectile.hostile = false;
//             Projectile.DamageType = DamageClass.Ranged;
//             Projectile.ignoreWater = true;
//             Projectile.tileCollide = false;
//             Projectile.penetrate = -1;
//             Projectile.timeLeft = 240;
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = 10;
//         }
// 
//         public override void AI()
//         {
//             Projectile.frameCounter++;
//             if (Projectile.frameCounter > 4)
//             {
//                 Projectile.frame++;
//                 Projectile.frameCounter = 0;
//             }
//             if (Projectile.frame > 4)
//                 Projectile.frame = 0;
// 
//             Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, Color.Green);
// 
//             if (Projectile.localAI[0] == 0f)
//             {
//                 Projectile.localAI[0] = 1f;
//                 SpawnLaunchDust(theme);
//             }
// 
//             Lighting.AddLight(Projectile.Center, theme.ToVector3() * 0.85f);
//             Projectile.localAI[1]++;
// 
//             NPC target = PFExoMechTargeting.FindTarget(Projectile.Center, 1400f);
//             if (target is null || Projectile.ai[0] == 1f || Projectile.timeLeft < 84)
//             {
//                 Projectile.ai[0] = 1f;
//                 if (Projectile.velocity.Length() < 27f)
//                     Projectile.velocity *= 1.045f;
//             }
//             else
//             {
//                 Vector2 distanceFromTarget = target.Center - Projectile.Center;
//                 if (distanceFromTarget.Length() < 120f || Projectile.localAI[1] > 74f)
//                 {
//                     Projectile.ai[0] = 1f;
//                     return;
//                 }
// 
//                 float speed = Projectile.velocity.Length();
//                 Vector2 desiredVelocity = distanceFromTarget.SafeNormalize(Projectile.velocity) * speed;
//                 Projectile.velocity = (Projectile.velocity * 10f + desiredVelocity) / 11f;
//                 Projectile.velocity = Projectile.velocity.SafeNormalize(desiredVelocity) * speed;
//             }
// 
//             PushAwayFromOtherRockets();
//         }
// 
//         private void SpawnLaunchDust(Color theme)
//         {
//             float randDustSpeed1 = 1.8f;
//             float randDustSpeed2 = 2.8f;
//             float angleRandom = 0.35f;
// 
//             for (int i = 0; i < 20; i++)
//             {
//                 float dustSpeed = Main.rand.NextFloat(randDustSpeed1, randDustSpeed2);
//                 Vector2 dustVel = new Vector2(dustSpeed, 0f).RotatedBy(Projectile.velocity.ToRotation() - angleRandom).RotatedByRandom(2f * angleRandom);
//                 int randomDustType = Main.rand.NextBool() ? 107 : 110;
// 
//                 int plasmaDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVel.X, dustVel.Y, 200, default, 1.7f);
//                 Main.dust[plasmaDust].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(Projectile.width * 0.5f);
//                 Main.dust[plasmaDust].noGravity = true;
//                 Main.dust[plasmaDust].velocity *= 3f;
//                 Main.dust[plasmaDust].color = theme * 0.6f;
// 
//                 plasmaDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVel.X, dustVel.Y, 100, default, 0.8f);
//                 Main.dust[plasmaDust].position = Projectile.Center + Vector2.UnitY.RotatedByRandom(MathHelper.Pi) * Main.rand.NextFloat(Projectile.width * 0.5f);
//                 Main.dust[plasmaDust].velocity *= 2f;
//                 Main.dust[plasmaDust].noGravity = true;
//                 Main.dust[plasmaDust].fadeIn = 1f;
//                 Main.dust[plasmaDust].color = theme * 0.5f;
//             }
// 
//             for (int j = 0; j < 10; j++)
//             {
//                 float dustSpeed = Main.rand.NextFloat(randDustSpeed1, randDustSpeed2);
//                 Vector2 dustVel = new Vector2(dustSpeed, 0f).RotatedBy(Projectile.velocity.ToRotation() - angleRandom).RotatedByRandom(2f * angleRandom);
//                 int randomDustType = Main.rand.NextBool() ? 107 : 110;
//                 int plasmaDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, randomDustType, dustVel.X, dustVel.Y, 0, default, 2f);
//                 Main.dust[plasmaDust].position = Projectile.Center + Vector2.UnitX.RotatedByRandom(MathHelper.Pi).RotatedBy(Projectile.velocity.ToRotation()) * Projectile.width / 3f;
//                 Main.dust[plasmaDust].noGravity = true;
//                 Main.dust[plasmaDust].velocity *= 0.5f;
//                 Main.dust[plasmaDust].color = theme;
//             }
//         }
// 
//         private void PushAwayFromOtherRockets()
//         {
//             for (int k = 0; k < Main.maxProjectiles; k++)
//             {
//                 Projectile otherProj = Main.projectile[k];
//                 if (!otherProj.active || k == Projectile.whoAmI || otherProj.type != Projectile.type)
//                     continue;
// 
//                 float distance = Vector2.Distance(Projectile.Center, otherProj.Center);
//                 if (distance >= 70f)
//                     continue;
// 
//                 Projectile.velocity += (Projectile.Center - otherProj.Center).SafeNormalize(Vector2.Zero) * 0.05f;
//             }
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
//             return false;
//         }
// 
//         public override void PostDraw(Color lightColor)
//         {
//             Texture2D texture = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
//             int height = texture.Height / Main.projFrames[Type];
//             int drawStart = height * Projectile.frame;
//             Vector2 origin = Projectile.Size / 2f;
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, Color.Green);
//             Main.EntitySpriteDraw(
//                 ModContent.Request<Texture2D>("CalamityMod/Projectiles/Boss/ApolloRocketGlow").Value,
//                 Projectile.Center - Main.screenPosition,
//                 new Rectangle(0, drawStart, texture.Width, height),
//                 Color.Lerp(theme, Color.White, 0.34f),
//                 Projectile.rotation,
//                 origin,
//                 Projectile.scale,
//                 SpriteEffects.None,
//                 0);
//         }
// 
//         public override void OnKill(int timeLeft)
//         {
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, Color.Green);
//             Projectile.position = Projectile.Center;
//             Projectile.width = Projectile.height = 90;
//             Projectile.Center = Projectile.position;
//             Projectile.Damage();
// 
//             SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.55f, Pitch = 0.12f }, Projectile.Center);
// 
//             for (int i = 0; i < 12; i++)
//             {
//                 int randomDustType = Main.rand.NextBool() ? 107 : 110;
//                 int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 2f);
//                 Main.dust[dust].velocity *= 3f;
//                 Main.dust[dust].color = theme;
//                 if (Main.rand.NextBool())
//                 {
//                     Main.dust[dust].scale = 0.5f;
//                     Main.dust[dust].fadeIn = 1f + Main.rand.Next(10) * 0.1f;
//                 }
//             }
// 
//             for (int j = 0; j < 15; j++)
//             {
//                 int randomDustType = Main.rand.NextBool() ? 107 : 110;
//                 int dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 3f);
//                 Main.dust[dust].noGravity = true;
//                 Main.dust[dust].velocity *= 5f;
//                 Main.dust[dust].color = theme;
// 
//                 dust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, randomDustType, 0f, 0f, 100, default, 2f);
//                 Main.dust[dust].velocity *= 2f;
//                 Main.dust[dust].color = theme * 0.8f;
//             }
// 
//             if (Main.dedServ)
//                 return;
// 
//             Vector2 source = Projectile.Center - new Vector2(24f);
//             for (int goreIndex = 0; goreIndex < 3; goreIndex++)
//             {
//                 float velocityMult = goreIndex == 0 ? 0.66f : goreIndex == 2 ? 1f : 0.33f;
//                 for (int i = 0; i < 4; i++)
//                 {
//                     int gore = Gore.NewGore(Projectile.GetSource_Death(), source, default, Main.rand.Next(61, 64), 1f);
//                     Main.gore[gore].velocity *= velocityMult;
//                     Main.gore[gore].velocity.X += i < 2 ? 1f : -1f;
//                     Main.gore[gore].velocity.Y += i % 2 == 0 ? 1f : -1f;
//                 }
//             }
//         }
//     }
// 
//     internal static class PFExoAresEffect
//     {
//         private const int FireInterval = 8;
// 
//         internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
//         {
//             if (!held)
//             {
//                 PristineFuryLeftEffectRegistry.Reset(holdout);
//                 return;
//             }
// 
//             if (holdout.LeftTimer > 0)
//             {
//                 holdout.LeftTimer--;
//                 return;
//             }
// 
//             Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
//             Vector2 muzzle = holdout.GunTipPosition + direction * 14f;
//             int projectileIndex = Projectile.NewProjectile(
//                 holdout.Projectile.GetSource_FromThis(),
//                 muzzle,
//                 direction,
//                 ModContent.ProjectileType<PFExoAresLaserBeamStart>(),
//                 holdout.GetScaledDamage(0.68f),
//                 holdout.Projectile.knockBack * 0.55f,
//                 holdout.Projectile.owner);
// 
//             PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
//             holdout.LeftTimer = FireInterval;
//             holdout.ApplyRecoil(4.5f);
//             holdout.TriggerMuzzleFlash(10);
//             holdout.SpawnMuzzleBurst(new Color(255, 76, 64), 0.9f);
//             SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.42f, Pitch = 0.48f, MaxInstances = 5 }, muzzle);
//         }
//     }
// 
//     internal sealed class PFExoAresLaserBeamStart : ModProjectile, ILocalizedModType
//     {
//         private const int MaxFrames = 5;
//         private int frameDrawn;
// 
//         public new string LocalizationCategory => "Projectiles.PristineFury";
//         public override string Texture => "CalamityMod/Projectiles/Boss/AresLaserBeamStart";
// 
//         public override void SetStaticDefaults()
//         {
//             Main.projFrames[Type] = MaxFrames;
//             ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 30;
//             Projectile.height = 30;
//             Projectile.friendly = true;
//             Projectile.hostile = false;
//             Projectile.DamageType = DamageClass.Ranged;
//             Projectile.alpha = 255;
//             Projectile.penetrate = -1;
//             Projectile.tileCollide = false;
//             Projectile.ignoreWater = true;
//             Projectile.timeLeft = 60;
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = 6;
//         }
// 
//         public override bool ShouldUpdatePosition() => false;
// 
//         public override void AI()
//         {
//             if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
//                 Projectile.velocity = Vector2.UnitX;
// 
//             Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
//             Projectile.localAI[0]++;
//             if (Projectile.localAI[0] >= 60f)
//             {
//                 Projectile.Kill();
//                 return;
//             }
// 
//             Projectile.scale = MathF.Min(1f, (float)Math.Sin(Projectile.localAI[0] * Math.PI / 60f) * 10f);
//             Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
// 
//             float[] samples = new float[3];
//             Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.width * Projectile.scale, 2400f, samples);
//             float laserLength = (samples[0] + samples[1] + samples[2]) / 3f;
//             Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], laserLength, 0.5f);
// 
//             SpawnEndDust();
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 76, 64));
//             DelegateMethods.v3_1 = theme.ToVector3() * 0.9f;
//             Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], Projectile.width * Projectile.scale, DelegateMethods.CastLight);
//         }
// 
//         private void SpawnEndDust()
//         {
//             if (Main.dedServ || Projectile.localAI[1] <= 12f)
//                 return;
// 
//             int dustType = (int)CalamityDusts.Brimstone;
//             Vector2 dustPos = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 14f);
//             for (int i = 0; i < 2; i++)
//             {
//                 float dustRot = Projectile.velocity.ToRotation() + (Main.rand.NextBool() ? -1f : 1f) * MathHelper.PiOver2;
//                 Vector2 dustVel = dustRot.ToRotationVector2() * Main.rand.NextFloat(2f, 4f);
//                 int dust = Dust.NewDust(dustPos, 0, 0, dustType, dustVel.X, dustVel.Y, 0, default, 1f);
//                 Main.dust[dust].noGravity = true;
//                 Main.dust[dust].scale = 1.7f;
//             }
//         }
// 
//         public override bool? CanDamage() => Projectile.scale >= 0.5f ? null : false;
// 
//         public override void CutTiles()
//         {
//             DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
//             Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
//         }
// 
//         public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
//         {
//             if (projHitbox.Intersects(targetHitbox))
//                 return true;
// 
//             float collisionPoint = 0f;
//             return Collision.CheckAABBvLineCollision(
//                 targetHitbox.TopLeft(),
//                 targetHitbox.Size(),
//                 Projectile.Center,
//                 Projectile.Center + Projectile.velocity * Projectile.localAI[1],
//                 30f * Projectile.scale,
//                 ref collisionPoint);
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             Texture2D beamStart = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
//             Texture2D beamMiddle = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
//             Texture2D beamEnd = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/AresLaserBeamEnd", AssetRequestMode.ImmediateLoad).Value;
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(255, 76, 64));
//             Color drawColor = Color.Lerp(theme, Color.White, 0.72f) * 0.86f;
//             drawColor.A = 100;
// 
//             if (Projectile.localAI[0] % 5f == 0f)
//             {
//                 frameDrawn++;
//                 if (frameDrawn >= MaxFrames)
//                     frameDrawn = 0;
//             }
// 
//             PFExoMechDrawing.DrawSegmentedBeam(beamStart, beamMiddle, beamEnd, MaxFrames, frameDrawn, Projectile.Center, Projectile.velocity, Projectile.rotation, Projectile.scale, Projectile.localAI[1], drawColor);
//             return false;
//         }
//     }
// 
// #endif
// 
//     internal static class PFExoThanatosEffect
//     {
//         private const int ChargeFrames = 150;
// 
//         internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
//         {
//             if (!held)
//             {
//                 PristineFuryLeftEffectRegistry.Reset(holdout);
//                 return;
//             }
// 
//             if (holdout.LeftAuxTimer == 1)
//             {
//                 holdout.LeftChargeTimer = ChargeFrames;
//                 EnsureExtinctionBeam(holdout);
//                 SpawnChargeEffects(holdout, 1f);
//                 return;
//             }
// 
//             if (holdout.LeftChargeTimer == 0)
//                 StartTelegraphs(holdout);
// 
//             holdout.LeftChargeTimer++;
//             SpawnChargeEffects(holdout, holdout.LeftChargeTimer / (float)ChargeFrames);
// 
//             if (holdout.LeftChargeTimer < ChargeFrames)
//                 return;
// 
//             FireExtinctionBeam(holdout);
//             holdout.LeftAuxTimer = 1;
//         }
// 
//         private static void StartTelegraphs(NewLegendPristineFuryHoldOut holdout)
//         {
//             Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
//             Vector2 muzzle = holdout.GunTipPosition + direction * 14f;
//             float[] offsets = { -0.48f, 0f, 0.48f };
// 
//             for (int i = 0; i < offsets.Length; i++)
//             {
//                 int projectileIndex = Projectile.NewProjectile(
//                     holdout.Projectile.GetSource_FromThis(),
//                     muzzle,
//                     direction,
//                     ModContent.ProjectileType<PFExoThanatosBeamTelegraph>(),
//                     0,
//                     0f,
//                     holdout.Projectile.owner,
//                     holdout.Projectile.whoAmI,
//                     offsets[i]);
// 
//                 PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
//             }
// 
//             SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.5f, Pitch = -0.32f, MaxInstances = 2 }, muzzle);
//         }
// 
//         private static void FireExtinctionBeam(NewLegendPristineFuryHoldOut holdout)
//         {
//             Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
//             Vector2 muzzle = holdout.GunTipPosition + direction * 16f;
//             int projectileIndex = Projectile.NewProjectile(
//                 holdout.Projectile.GetSource_FromThis(),
//                 muzzle,
//                 direction,
//                 ModContent.ProjectileType<PFExoThanatosBeamStart>(),
//                 holdout.GetScaledDamage(1.72f),
//                 holdout.Projectile.knockBack * 1.45f,
//                 holdout.Projectile.owner,
//                 holdout.Projectile.whoAmI);
// 
//             PFLeftEffectRules.ApplyTheme(projectileIndex, holdout.CurrentMark);
//             holdout.ApplyRecoil(34f);
//             holdout.Owner.velocity -= direction * 3.2f;
//             holdout.Owner.Calamity().GeneralScreenShakePower = Math.Max(holdout.Owner.Calamity().GeneralScreenShakePower, 6f);
//             holdout.TriggerMuzzleFlash(28);
//             holdout.SpawnMuzzleBurst(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), 1.5f);
//             SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Custom/DoGLaserWallBigAttack") { Volume = 0.78f, Pitch = 0.08f, MaxInstances = 2 }, muzzle);
//         }
// 
//         private static void EnsureExtinctionBeam(NewLegendPristineFuryHoldOut holdout)
//         {
//             int beamType = ModContent.ProjectileType<PFExoThanatosBeamStart>();
//             for (int i = 0; i < Main.maxProjectiles; i++)
//             {
//                 Projectile projectile = Main.projectile[i];
//                 if (!projectile.active || projectile.owner != holdout.Projectile.owner || projectile.type != beamType || (int)projectile.ai[0] != holdout.Projectile.whoAmI)
//                     continue;
// 
//                 projectile.timeLeft = 2;
//                 return;
//             }
// 
//             FireExtinctionBeam(holdout);
//         }
// 
//         private static void SpawnChargeEffects(NewLegendPristineFuryHoldOut holdout, float charge)
//         {
//             if (Main.dedServ)
//                 return;
// 
//             Vector2 direction = holdout.AimDirection.SafeNormalize(Vector2.UnitX * holdout.Owner.direction);
//             Vector2 muzzle = holdout.GunTipPosition + direction * 12f;
//             Color theme = Color.Lerp(PristineFuryMarkHelper.GetColor(holdout.CurrentMark), Color.White, charge * 0.46f);
//             Lighting.AddLight(muzzle, theme.ToVector3() * (0.35f + charge * 0.9f));
// 
//             if (Main.rand.NextFloat() < 0.25f + charge * 0.45f)
//             {
//                 Vector2 offset = Main.rand.NextVector2CircularEdge(46f + charge * 92f, 46f + charge * 92f);
//                 Dust dust = Dust.NewDustPerfect(muzzle + offset, ModContent.DustType<SquashDust>());
//                 dust.velocity = -offset.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(2.4f, 6.8f) * (0.6f + charge);
//                 dust.color = theme;
//                 dust.scale = Main.rand.NextFloat(0.9f, 1.5f) * (0.85f + charge * 0.75f);
//                 dust.noGravity = true;
//                 dust.fadeIn = 1.8f + charge * 2f;
//             }
// 
//             if (holdout.LeftChargeTimer % 10 == 0)
//             {
//                 Particle line = new PointParticle(
//                     muzzle + Main.rand.NextVector2CircularEdge(56f + charge * 76f, 56f + charge * 76f),
//                     -direction.RotatedByRandom(0.42f) * Main.rand.NextFloat(3.2f, 7.5f),
//                     false,
//                     Main.rand.Next(18, 30),
//                     0.76f + charge * 0.58f,
//                     theme);
// 
//                 GeneralParticleHandler.SpawnParticle(line);
//             }
//         }
//     }
// 
//     internal sealed class PFExoThanatosBeamTelegraph : ModProjectile, ILocalizedModType
//     {
//         private const int Lifetime = 150;
//         private const float TelegraphWidth = 3600f;
// 
//         public new string LocalizationCategory => "Projectiles.PristineFury";
//         public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
// 
//         private ref float HoldoutIndex => ref Projectile.ai[0];
//         private ref float StartingRotationalOffset => ref Projectile.ai[1];
//         private ref float Time => ref Projectile.localAI[0];
// 
//         public override void SetStaticDefaults()
//         {
//             ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = Projectile.height = 2;
//             Projectile.tileCollide = false;
//             Projectile.ignoreWater = true;
//             Projectile.penetrate = -1;
//             Projectile.timeLeft = Lifetime + 10;
//         }
// 
//         public override bool ShouldUpdatePosition() => false;
//         public override bool? CanDamage() => false;
// 
//         public override void AI()
//         {
//             Time++;
//             if (!TryGetBoundHoldout(out NewLegendPristineFuryHoldOut holdout) || holdout.CurrentMark != PristineFuryMark.ExoThanatos || (holdout.LeftChargeTimer <= 0 && Time > 2f))
//             {
//                 Projectile.Kill();
//                 return;
//             }
// 
//             Vector2 direction = holdout.AimDirection.SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
//             Projectile.Center = holdout.GunTipPosition + direction * 14f;
//             Projectile.velocity = direction;
//             float convergence = Utils.GetLerpValue(25f, 122f, Time, true);
//             convergence *= convergence;
//             Projectile.rotation = direction.ToRotation() + MathHelper.Lerp(StartingRotationalOffset, 0f, convergence);
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(102, 162, 255));
//             Lighting.AddLight(Projectile.Center, theme.ToVector3() * Utils.GetLerpValue(0f, 42f, Time, true) * 0.65f);
//         }
// 
//         private bool TryGetBoundHoldout(out NewLegendPristineFuryHoldOut holdout)
//         {
//             holdout = null;
//             int index = (int)HoldoutIndex;
//             if (!Main.projectile.IndexInRange(index))
//                 return false;
// 
//             Projectile boundProjectile = Main.projectile[index];
//             if (!boundProjectile.active || boundProjectile.owner != Projectile.owner || boundProjectile.ModProjectile is not NewLegendPristineFuryHoldOut pristineHoldout)
//                 return false;
// 
//             holdout = pristineHoldout;
//             return true;
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             Texture2D laserTelegraph = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam").Value;
//             float fadeIn = Utils.GetLerpValue(0f, 24f, Time, true);
//             float fadeOutProgress = Utils.GetLerpValue(Lifetime - 18f, Lifetime + 10f, Time, true);
//             float fadeOut = 1f - fadeOutProgress * fadeOutProgress;
//             float yScale = 2f * fadeIn * fadeOut;
//             if (yScale <= 0.01f)
//                 return false;
// 
//             Vector2 scaleInner = new(TelegraphWidth / laserTelegraph.Width, yScale);
//             Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
//             Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.6f);
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(102, 162, 255));
//             Color colorOuter = Color.Lerp(theme, Color.White, Time / Lifetime * 1.8f % 1f * 0.3f) * 0.62f;
//             Color colorInner = Color.Lerp(colorOuter, Color.White, 0.78f) * 0.76f;
// 
//             Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, Projectile.rotation, origin, scaleInner, SpriteEffects.None, 0);
//             Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, Projectile.rotation, origin, scaleOuter, SpriteEffects.None, 0);
//             return false;
//         }
//     }
// 
//     internal sealed class PFExoThanatosBeamStart : ModProjectile, ILocalizedModType
//     {
//         private const int MaxFrames = 5;
//         private const int Lifetime = 96;
//         private const float MaxLaserLength = 3600f;
//         private int frameDrawn;
//         private ref float HoldoutIndex => ref Projectile.ai[0];
// 
//         public new string LocalizationCategory => "Projectiles.PristineFury";
//         public override string Texture => "CalamityMod/Projectiles/Boss/ThanatosBeamStart";
// 
//         public override void SetStaticDefaults()
//         {
//             Main.projFrames[Type] = MaxFrames;
//             ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 42;
//             Projectile.height = 42;
//             Projectile.friendly = true;
//             Projectile.hostile = false;
//             Projectile.DamageType = DamageClass.Ranged;
//             Projectile.alpha = 255;
//             Projectile.penetrate = -1;
//             Projectile.tileCollide = false;
//             Projectile.ignoreWater = true;
//             Projectile.timeLeft = Lifetime;
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = 5;
//         }
// 
//         public override bool ShouldUpdatePosition() => false;
// 
//         public override void AI()
//         {
//             if (Projectile.velocity.HasNaNs() || Projectile.velocity == Vector2.Zero)
//                 Projectile.velocity = Vector2.UnitX;
// 
//             if (!TryGetBoundHoldout(out NewLegendPristineFuryHoldOut holdout) || holdout.CurrentMark != PristineFuryMark.ExoThanatos)
//             {
//                 Projectile.Kill();
//                 return;
//             }
//             bool sustainedBeam = holdout.LeftAuxTimer == 1;
//             if (sustainedBeam)
//                 Projectile.timeLeft = 2;
// 
//             Projectile.velocity = holdout.AimDirection.SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX));
//             Projectile.Center = holdout.GunTipPosition + Projectile.velocity * 16f;
//             Projectile.localAI[0]++;
//             Projectile.rotation = Projectile.velocity.ToRotation() - MathHelper.PiOver2;
// 
//             float fadeIn = Utils.GetLerpValue(0f, 12f, Projectile.localAI[0], true);
//             float fadeOutProgress = Utils.GetLerpValue(Lifetime - 20f, Lifetime, Projectile.localAI[0], true);
//             float fadeOut = sustainedBeam ? 1f : 1f - fadeOutProgress * fadeOutProgress;
//             Projectile.scale = 1.28f * fadeIn * fadeOut;
// 
//             float[] samples = new float[3];
//             Collision.LaserScan(Projectile.Center, Projectile.velocity, Projectile.width * Math.Max(Projectile.scale, 0.2f), MaxLaserLength, samples);
//             float laserLength = MathHelper.Clamp((samples[0] + samples[1] + samples[2]) / 3f, 400f, MaxLaserLength);
//             Projectile.localAI[1] = MathHelper.Lerp(Projectile.localAI[1], laserLength, 0.42f);
// 
//             SpawnEndEffects();
//             SpawnSideLasers();
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(102, 162, 255));
//             DelegateMethods.v3_1 = theme.ToVector3() * 1.1f;
//             Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], Projectile.width * Projectile.scale, DelegateMethods.CastLight);
//         }
// 
//         private bool TryGetBoundHoldout(out NewLegendPristineFuryHoldOut holdout)
//         {
//             holdout = null;
//             int index = (int)HoldoutIndex;
//             if (!Main.projectile.IndexInRange(index))
//                 return false;
// 
//             Projectile boundProjectile = Main.projectile[index];
//             if (!boundProjectile.active || boundProjectile.owner != Projectile.owner || boundProjectile.ModProjectile is not NewLegendPristineFuryHoldOut pristineHoldout)
//                 return false;
// 
//             holdout = pristineHoldout;
//             return true;
//         }
// 
//         private void SpawnEndEffects()
//         {
//             if (Main.dedServ || Projectile.localAI[1] <= 12f || Projectile.scale <= 0.05f)
//                 return;
// 
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(102, 162, 255));
//             Vector2 end = Projectile.Center + Projectile.velocity * (Projectile.localAI[1] - 18f);
//             if (Projectile.localAI[0] % 3f == 0f)
//             {
//                 Dust dust = Dust.NewDustPerfect(end + Main.rand.NextVector2Circular(12f, 12f), ModContent.DustType<BrimstoneFlame>());
//                 dust.velocity = Projectile.velocity.RotatedByRandom(0.9f) * Main.rand.NextFloat(1.6f, 4.8f);
//                 dust.color = theme;
//                 dust.scale = Main.rand.NextFloat(0.8f, 1.35f);
//                 dust.noGravity = true;
//             }
//         }
// 
//         private void SpawnSideLasers()
//         {
//             if (Main.myPlayer != Projectile.owner || Projectile.localAI[0] < 18f || Projectile.localAI[0] % 8f != 1f)
//                 return;
// 
//             float usableLength = Math.Min(Projectile.localAI[1] - 320f, 2600f);
//             if (usableLength <= 420f)
//                 return;
// 
//             for (int i = 0; i < 2; i++)
//             {
//                 float progress = 0.28f + i * 0.34f + Main.rand.NextFloat(-0.04f, 0.04f);
//                 Vector2 spawnPosition = Projectile.Center + Projectile.velocity * (usableLength * progress + 260f);
//                 float sideSign = ((Projectile.identity + i + (int)(Projectile.localAI[0] / 8f)) & 1) == 0 ? 1f : -1f;
//                 Vector2 sideVelocity = Projectile.velocity.RotatedBy(MathHelper.PiOver2 * sideSign) * 45f;
//                 int projectileIndex = Projectile.NewProjectile(
//                     Projectile.GetSource_FromThis(),
//                     spawnPosition,
//                     sideVelocity,
//                     ModContent.ProjectileType<PFExoThanatosSideLaser>(),
//                     Math.Max(1, (int)(Projectile.damage * 0.42f)),
//                     Projectile.knockBack * 0.45f,
//                     Projectile.owner,
//                     0f,
//                     sideVelocity.ToRotation());
// 
//                 PFLeftEffectRules.ApplyTheme(projectileIndex, (PristineFuryMark)(int)Projectile.ai[2]);
//             }
//         }
// 
//         public override bool? CanDamage() => Projectile.scale >= 0.35f ? null : false;
// 
//         public override void CutTiles()
//         {
//             DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
//             Utils.PlotTileLine(Projectile.Center, Projectile.Center + Projectile.velocity * Projectile.localAI[1], Projectile.width * Projectile.scale, DelegateMethods.CutTiles);
//         }
// 
//         public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
//         {
//             float collisionPoint = 0f;
//             return Collision.CheckAABBvLineCollision(
//                 targetHitbox.TopLeft(),
//                 targetHitbox.Size(),
//                 Projectile.Center,
//                 Projectile.Center + Projectile.velocity * Projectile.localAI[1],
//                 44f * Projectile.scale,
//                 ref collisionPoint);
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             Texture2D beamStart = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
//             Texture2D beamMiddle = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ThanatosBeamMiddle", AssetRequestMode.ImmediateLoad).Value;
//             Texture2D beamEnd = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Lasers/ThanatosBeamEnd", AssetRequestMode.ImmediateLoad).Value;
//             Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
//             Texture2D bloomRing = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
// 
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(102, 162, 255));
//             Color drawColor = Color.Lerp(theme, Color.White, 0.82f) * 0.95f;
//             drawColor.A = 110;
// 
//             if (Projectile.localAI[0] % 5f == 0f)
//             {
//                 frameDrawn++;
//                 if (frameDrawn >= MaxFrames)
//                     frameDrawn = 0;
//             }
// 
//             PFLeftEffectRules.BeginAdditive();
// 
//             // Draw segmented beam
//             PFExoMechDrawing.DrawSegmentedBeam(beamStart, beamMiddle, beamEnd, MaxFrames, frameDrawn, Projectile.Center, Projectile.velocity, Projectile.rotation, Projectile.scale, Projectile.localAI[1], drawColor);
// 
//             // Add mechanical red/green/yellow glow overlays at the starting point
//             Vector2 startPos = Projectile.Center - Main.screenPosition;
//             float glowScale = Projectile.scale * 0.85f;
//             Main.EntitySpriteDraw(bloom, startPos, null, drawColor * 0.82f, 0f, bloom.Size() * 0.5f, 0.5f * glowScale, SpriteEffects.None, 0);
//             Main.EntitySpriteDraw(bloomRing, startPos, null, drawColor * 0.65f, 0f, bloomRing.Size() * 0.5f, 0.8f * glowScale, SpriteEffects.None, 0);
// 
//             PFLeftEffectRules.EndAdditive();
//             return false;
//         }
//     }
// 
//     internal sealed class PFExoThanatosSideLaser : ModProjectile, ILocalizedModType
//     {
//         private const float TelegraphTotalTime = 28f;
//         private const float TelegraphFadeTime = 14f;
//         private const float TelegraphWidth = 2200f;
// 
//         public new string LocalizationCategory => "Projectiles.PristineFury";
//         public override string Texture => "CalamityMod/Projectiles/Boss/THanosSideLaser";
// 
//         private ref float TelegraphDelay => ref Projectile.localAI[1];
//         private ref float DirectionRotation => ref Projectile.ai[1];
// 
//         public override void SetStaticDefaults()
//         {
//             Main.projFrames[Type] = 4;
//             ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
//             ProjectileID.Sets.TrailCacheLength[Type] = 6;
//             ProjectileID.Sets.TrailingMode[Type] = 0;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 22;
//             Projectile.height = 22;
//             Projectile.friendly = true;
//             Projectile.hostile = false;
//             Projectile.DamageType = DamageClass.Ranged;
//             Projectile.ignoreWater = true;
//             Projectile.tileCollide = false;
//             Projectile.alpha = 255;
//             Projectile.penetrate = -1;
//             Projectile.extraUpdates = 1;
//             Projectile.timeLeft = 84;
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = 10;
//         }
// 
//         public override void AI()
//         {
//             if (Projectile.localAI[0] == 0f)
//             {
//                 Projectile.localAI[0] = 1f;
//                 DirectionRotation = Projectile.velocity.SafeNormalize(Vector2.UnitX).ToRotation();
//                 Projectile.velocity = Vector2.Zero;
//                 Projectile.netUpdate = true;
//             }
// 
//             Projectile.frameCounter++;
//             if (Projectile.frameCounter > 12)
//             {
//                 Projectile.frame++;
//                 Projectile.frameCounter = 0;
//             }
//             if (Projectile.frame > 3)
//                 Projectile.frame = 0;
// 
//             if (TelegraphDelay > TelegraphTotalTime)
//             {
//                 Projectile.alpha = Math.Max(0, Projectile.alpha - 25);
//                 if (Projectile.velocity == Vector2.Zero)
//                 {
//                     Projectile.extraUpdates = Main.getGoodWorld ? 4 : 3;
//                     Projectile.velocity = DirectionRotation.ToRotationVector2() * 15f;
//                     Projectile.netUpdate = true;
//                 }
// 
//                 Projectile.spriteDirection = Projectile.velocity.X < 0f ? -1 : 1;
//                 Projectile.rotation = Projectile.spriteDirection == -1
//                     ? (float)Math.Atan2(-Projectile.velocity.Y, -Projectile.velocity.X)
//                     : Projectile.velocity.ToRotation();
//             }
//             else
//             {
//                 Projectile.rotation = DirectionRotation;
//             }
// 
//             TelegraphDelay++;
//         }
// 
//         public override bool? CanDamage() => TelegraphDelay > TelegraphTotalTime ? null : false;
// 
//         public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox) =>
//             CalamityUtils.CircularHitboxCollision(projHitbox.Center(), Projectile.Size.Length() * 0.5f, targetHitbox);
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             if (TelegraphDelay >= TelegraphTotalTime)
//             {
//                 lightColor = Color.White * Projectile.Opacity;
//                 Vector2 drawOffset = Projectile.velocity.SafeNormalize(Vector2.Zero) * -30f;
//                 Projectile.Center += drawOffset;
//                 CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
//                 Projectile.Center -= drawOffset;
//                 return false;
//             }
// 
//             Texture2D laserTelegraph = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/LaserWallTelegraphBeam").Value;
//             float yScale = 2f;
//             if (TelegraphDelay < TelegraphFadeTime)
//                 yScale = MathHelper.Lerp(0f, 2f, TelegraphDelay / TelegraphFadeTime);
//             if (TelegraphDelay > TelegraphTotalTime - TelegraphFadeTime)
//                 yScale = MathHelper.Lerp(2f, 0f, (TelegraphDelay - (TelegraphTotalTime - TelegraphFadeTime)) / TelegraphFadeTime);
// 
//             Vector2 scaleInner = new(TelegraphWidth / laserTelegraph.Width, yScale);
//             Vector2 origin = laserTelegraph.Size() * new Vector2(0f, 0.5f);
//             Vector2 scaleOuter = scaleInner * new Vector2(1f, 2.2f);
//             Color theme = PFLeftEffectRules.GetThemeColor(Projectile, new Color(102, 162, 255));
//             Color colorOuter = Color.Lerp(theme, Color.White, TelegraphDelay / TelegraphTotalTime * 2f % 1f * 0.3f) * 0.62f;
//             Color colorInner = Color.Lerp(colorOuter, Color.White, 0.75f) * 0.72f;
// 
//             Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorInner, DirectionRotation, origin, scaleInner, SpriteEffects.None, 0);
//             Main.EntitySpriteDraw(laserTelegraph, Projectile.Center - Main.screenPosition, null, colorOuter, DirectionRotation, origin, scaleOuter, SpriteEffects.None, 0);
//             return false;
//         }
//     }
// 
//     internal static class PFExoMechTargeting
//     {
//         internal static NPC FindTarget(Vector2 center, float range)
//         {
//             NPC bestTarget = null;
//             float bestDistance = range;
//             for (int i = 0; i < Main.maxNPCs; i++)
//             {
//                 NPC npc = Main.npc[i];
//                 if (!npc.active || !npc.CanBeChasedBy())
//                     continue;
// 
//                 float distance = Vector2.Distance(center, npc.Center);
//                 if (distance >= bestDistance)
//                     continue;
// 
//                 bestDistance = distance;
//                 bestTarget = npc;
//             }
// 
//             return bestTarget;
//         }
//     }
// 
//     internal static class PFExoMechDrawing
//     {
//         internal static void DrawSegmentedBeam(Texture2D beamStart, Texture2D beamMiddle, Texture2D beamEnd, int maxFrames, int frameDrawn, Vector2 center, Vector2 direction, float rotation, float scale, float drawLength, Color color)
//         {
//             if (direction == Vector2.Zero || scale <= 0.01f || drawLength <= 0f)
//                 return;
// 
//             direction = direction.SafeNormalize(Vector2.UnitX);
//             Vector2 drawPosition = center - Main.screenPosition;
//             int startFrameHeight = beamStart.Height / maxFrames;
//             int middleFrameHeight = beamMiddle.Height / maxFrames;
//             int endFrameHeight = beamEnd.Height / maxFrames;
//             Rectangle sourceRectangle = new(0, startFrameHeight * frameDrawn, beamStart.Width, startFrameHeight);
// 
//             Main.EntitySpriteDraw(beamStart, drawPosition, sourceRectangle, color, rotation, new Vector2(beamStart.Width, startFrameHeight) / 2f, scale, SpriteEffects.None, 0);
// 
//             drawLength -= (startFrameHeight / 2f + endFrameHeight) * scale;
//             Vector2 segmentCenter = center + direction * scale * startFrameHeight / 2f;
//             if (drawLength > 0f)
//             {
//                 float distanceDrawn = 0f;
//                 int middleFrameDrawn = frameDrawn;
//                 while (distanceDrawn + 1f < drawLength)
//                 {
//                     Rectangle middleFrame = new(0, middleFrameHeight * middleFrameDrawn, beamMiddle.Width, middleFrameHeight);
//                     if (drawLength - distanceDrawn < middleFrame.Height)
//                         middleFrame.Height = (int)(drawLength - distanceDrawn);
// 
//                     Main.EntitySpriteDraw(beamMiddle, segmentCenter - Main.screenPosition, middleFrame, color, rotation, new Vector2(middleFrame.Width / 2f, 0f), scale, SpriteEffects.None, 0);
//                     middleFrameDrawn++;
//                     if (middleFrameDrawn >= maxFrames)
//                         middleFrameDrawn = 0;
// 
//                     distanceDrawn += middleFrame.Height * scale;
//                     segmentCenter += direction * middleFrame.Height * scale;
//                 }
//             }
// 
//             sourceRectangle = new Rectangle(0, endFrameHeight * frameDrawn, beamEnd.Width, endFrameHeight);
//             Main.EntitySpriteDraw(beamEnd, segmentCenter - Main.screenPosition, sourceRectangle, color, rotation, new Vector2(beamEnd.Width, endFrameHeight) / 2f, scale, SpriteEffects.None, 0);
//         }
//     }
// }
