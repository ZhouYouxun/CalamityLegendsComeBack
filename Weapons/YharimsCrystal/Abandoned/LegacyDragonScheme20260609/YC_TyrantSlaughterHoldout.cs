// Abandoned by 2026-06-09 dragon-scheme Yharim's Crystal refactor.
// Original file is line-commented so tModLoader in-game compilation cannot load it accidentally.
//
// /*
// Abandoned by 2026-06-09 dragon-scheme Yharim's Crystal refactor.
// Original file was fully commented before moving so tModLoader in-game compilation cannot load it accidentally.
// 
// using System;
// using System.Collections.Generic;
// using System.IO;
// using CalamityLegendsComeBack.Weapons.YharimsCrystal;
// using CalamityMod;
// using CalamityMod.Buffs.DamageOverTime;
// using CalamityMod.Particles;
// using CalamityMod.Projectiles.BaseProjectiles;
// using CalamityMod.Projectiles.Melee;
// using CalamityMod.Projectiles.Typeless;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;
// using ReLogic.Content;
// using Terraria;
// using Terraria.Audio;
// using Terraria.DataStructures;
// using Terraria.GameContent;
// using Terraria.ID;
// using Terraria.ModLoader;
// 
// namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.YCRightSlaughter
// {
//     public class YC_TyrantSlaughterHoldout : BaseCustomUseStyleProjectile, ILocalizedModType
//     {
//         public new string LocalizationCategory => "Projectiles.YharimsCrystal";
//         public override int AssignedItemID => ModContent.ItemType<NewLegendYharimsCrystal>();
//         public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/YharimsCrystalPrism";
//         public override int FrameCount => 6;
//         public override float HitboxOutset => 235f;
//         public override Vector2 HitboxSize => new(320f, 320f);
//         public override float HitboxRotationOffset => MathHelper.ToRadians(-45f);
//         public override Vector2 SpriteOrigin => new(ModContent.Request<Texture2D>(Texture).Value.Width * 0.5f, ModContent.Request<Texture2D>(Texture).Value.Height / FrameCount * 0.5f);
// 
//         private static readonly Color TyrantGold = new(255, 214, 92);
//         private static readonly Color TyrantOrange = new(255, 104, 34);
//         private static readonly Color TyrantWhite = new(255, 245, 190);
// 
//         public float hitboxMult = 1f;
//         public Vector2 mousePos;
//         public Vector2 aimVel;
//         public bool doSwing = true;
//         public bool postSwing;
//         public float fadeIn;
//         public int useAnim;
//         public int swingCount;
//         public bool finalFlip;
//         public bool playSwingSound = true;
//         public bool holding = true;
//         public int postSwingCooldown;
//         public bool willDie;
//         public bool swooshFade;
// 
//         private int flameTimer;
//         private int frameTimer;
//         private bool leftWasDown;
//         private readonly List<Particle> heldFlameParticles = new();
//         private NPC lastHitTarget;
//         private int lastHitTargetIndex = -1;
//         private int postSwingCooldownMax => Math.Max(1, (int)(useAnim * 0.65f));
// 
//         private Vector2 ForwardDirection => (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX * Owner.direction) * -1f;
//         private Vector2 MuzzlePosition => Projectile.Center + ForwardDirection * 54f;
// 
//         public override void SetDefaults()
//         {
//             base.SetDefaults();
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = -1;
//             Projectile.DamageType = DamageClass.Magic;
//         }
// 
//         public override void OnSpawn(IEntitySource source)
//         {
//             base.OnSpawn(source);
//             Projectile.originalDamage *= 8;
//         }
// 
//         public override void WhenSpawned()
//         {
//             IgnoreActiveAnimation = true;
//             DrawUnconditionally = true;
//             CanHit = false;
//             Projectile.knockBack = 0f;
//             Projectile.scale = 1.1f;
//             Projectile.ai[1] = -1f;
// 
//             bool isOwner = Main.myPlayer == Projectile.owner;
//             if (isOwner)
//             {
//                 mousePos = GetMouseWorld();
//                 aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 65f;
//                 Projectile.netUpdate = true;
//             }
//             else
//             {
//                 Vector2 syncedDelta = Owner.Calamity().mouseWorldDeltaFromPlayer;
//                 aimVel = syncedDelta.LengthSquared() > 0.001f
//                     ? (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65f
//                     : Vector2.UnitX * Owner.direction * 65f;
//                 mousePos = Owner.Center - aimVel;
//             }
// 
//             useAnim = Math.Max(36, (int)(Owner.HeldItem.useAnimation / Math.Max(0.15f, Owner.GetTotalAttackSpeed<MagicDamageClass>())));
//             postSwingCooldown = postSwingCooldownMax / 2;
// 
//             Owner.direction = mousePos.X < Owner.Center.X ? -1 : 1;
//             FlipAsSword = Owner.direction == -1;
//         }
// 
//         public override void OnKill(int timeLeft)
//         {
//             Owner.Calamity().demonSwordKillMode = false;
// 
//             if (lastHitTargetIndex >= 0)
//             {
//                 NPC possibleTarget = Main.npc[lastHitTargetIndex];
//                 if (possibleTarget != null && possibleTarget.active && possibleTarget.life > 0)
//                     lastHitTarget = possibleTarget;
//             }
// 
//             if (lastHitTarget == null ||
//                 lastHitTarget.life <= 0 ||
//                 !lastHitTarget.active ||
//                 Owner.HeldItem.type != AssignedItemID ||
//                 Main.myPlayer != Projectile.owner)
//             {
//                 return;
//             }
// 
//             Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 12.5f);
// 
//             if (!Main.dedServ)
//             {
//                 GeneralParticleHandler.SpawnParticle(new CustomPulse(
//                     lastHitTarget.Center,
//                     Vector2.Zero,
//                     Color.Lerp(Color.DeepPink, TyrantGold, 0.45f),
//                     "CalamityMod/Particles/HighResHollowCircleHardEdge",
//                     Vector2.One,
//                     Main.rand.NextFloat(-10f, 10f),
//                     0.05f,
//                     0.6f,
//                     20,
//                     true));
//             }
// 
//             for (int i = 0; i < 4; i++)
//             {
//                 Vector2 velocity = (MathHelper.TwoPi * i / 4f).ToRotationVector2() * 2f;
//                 Projectile.NewProjectileDirect(
//                     Projectile.GetSource_FromThis(),
//                     lastHitTarget.Center,
//                     velocity.RotatedBy(MathHelper.ToRadians(45f)),
//                     ModContent.ProjectileType<DevilsStrike>(),
//                     0,
//                     0f,
//                     Owner.whoAmI,
//                     1f);
//             }
// 
//             Projectile strike = Projectile.NewProjectileDirect(
//                 Projectile.GetSource_FromThis(),
//                 lastHitTarget.Center,
//                 Vector2.Zero,
//                 ModContent.ProjectileType<DirectStrike>(),
//                 (int)(Projectile.damage * 1.666f),
//                 0f,
//                 Owner.whoAmI,
//                 lastHitTarget.whoAmI);
//             strike.DamageType = DamageClass.Magic;
// 
//             SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/LanceofDestinyStrong") { Volume = 0.9f, Pitch = 0.3f }, lastHitTarget.Center);
//             lastHitTarget.AddBuff(ModContent.BuffType<DemonicFlames>(), 210);
//         }
// 
//         public override void UseStyle()
//         {
//             bool isOwner = Main.myPlayer == Projectile.owner;
//             bool rightHeld = Main.mouseRight || Owner.Calamity().mouseRight;
//             bool validMouse = !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
//             bool leftDown = isOwner && Main.mouseLeft;
//             bool leftPressed = leftDown && !leftWasDown;
//             if (isOwner)
//                 leftWasDown = leftDown;
// 
//             Vector2 playerMovement = Owner.position - Owner.oldPosition;
//             UpdateOwnedFlameParticles(playerMovement);
//             AnimatePrism();
// 
//             if (!Owner.active || Owner.dead || Owner.HeldItem.type != AssignedItemID)
//             {
//                 Projectile.Kill();
//                 return;
//             }
// 
//             if (!isOwner)
//             {
//                 Vector2 syncedDelta = Owner.Calamity().mouseWorldDeltaFromPlayer;
//                 if (syncedDelta.LengthSquared() > 0.001f)
//                     aimVel = (Owner.Center - Owner.Calamity().mouseWorld).SafeNormalize(Vector2.UnitX) * 65f;
//             }
// 
//             if (postSwingCooldown > 0)
//                 postSwingCooldown--;
//             else if (willDie && holding)
//             {
//                 Projectile.Kill();
//                 return;
//             }
// 
//             if (holding)
//             {
//                 Animation--;
//                 CanHit = false;
//                 postSwing = false;
// 
//                 if (isOwner)
//                 {
//                     mousePos = GetMouseWorld();
//                     aimVel = (Owner.Center - mousePos).SafeNormalize(Vector2.UnitX) * 65f;
//                 }
//                 else
//                     mousePos = Owner.Center - aimVel;
// 
//                 if (!rightHeld)
//                     willDie = true;
// 
//                 if (isOwner &&
//                     rightHeld &&
//                     validMouse &&
//                     leftPressed &&
//                     swingCount < 2 &&
//                     postSwingCooldown == 0)
//                 {
//                     Animation = (int)(useAnim * 0.7f);
//                     holding = false;
//                     swingCount++;
//                     Projectile.netUpdate = true;
//                 }
// 
//                 RotationOffset = MathHelper.Lerp(
//                     RotationOffset,
//                     MathHelper.ToRadians(120f * Projectile.ai[1] * Owner.direction * (1f + Utils.GetLerpValue(useAnim * 0.7f, useAnim, Animation, true) * 0.35f)),
//                     0.2f);
//             }
//             else if (!willDie)
//             {
//                 if (!finalFlip)
//                     FlipAsSword = Owner.direction < 0;
// 
//                 float time = AnimationProgress - useAnim / 3f;
//                 float timeMax = useAnim - useAnim / 3f;
//                 float swingProgress = MathHelper.Clamp(time / Math.Max(1f, timeMax), 0f, 1f);
// 
//                 if (time >= (int)(timeMax * 0.4f) && playSwingSound)
//                 {
//                     SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordSwing", 2) { Volume = 0.68f, Pitch = Main.rand.NextFloat(-0.35f, -0.18f) }, Projectile.Center);
//                     SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/HeavySwing") { Volume = 0.46f, Pitch = Main.rand.NextFloat(0.18f, 0.28f) }, Projectile.Center);
//                     Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 2.8f);
//                     playSwingSound = false;
//                 }
// 
//                 CanHit = time > timeMax * 0.2f && time < timeMax * 0.9f;
//                 swooshFade = time > timeMax * 0.7f;
//                 postSwing = time < timeMax * 0.75f;
//                 fadeIn = MathHelper.Lerp(fadeIn, CanHit && !swooshFade ? 1f : 0f, CanHit && !swooshFade ? 0.5f : 0.2f);
// 
//                 RotationOffset = MathHelper.Lerp(
//                     RotationOffset,
//                     MathHelper.ToRadians(MathHelper.Lerp(150f * Projectile.ai[1] * Owner.direction, 120f * -Projectile.ai[1] * Owner.direction, CalamityUtils.ExpInOutEasing(swingProgress * 0.9f, 1))),
//                     0.2f);
// 
//                 if (CanHit)
//                     EmitSlashEdgeFX();
// 
//                 if (time >= timeMax * 0.9f)
//                     FinishSwing(rightHeld && swingCount < 2);
//             }
// 
//             if (!CanHit && !postSwing)
//                 Owner.direction = mousePos.X < Owner.Center.X ? -1 : 1;
//             else
//                 Owner.direction = (Owner.Center - aimVel).X < Owner.Center.X ? -1 : 1;
// 
//             Projectile.rotation = Projectile.rotation.AngleLerp(Owner.AngleTo(mousePos) + MathHelper.ToRadians(45f), 0.1f);
//             Projectile.direction = Owner.direction;
//             Projectile.spriteDirection = Owner.direction;
//             ArmRotationOffset = MathHelper.ToRadians(-140f);
//             ArmRotationOffsetBack = MathHelper.ToRadians(-140f);
// 
//             if (holding || willDie)
//                 EmitHoldFlameFX();
//         }
// 
//         private void FinishSwing(bool continueHolding)
//         {
//             Projectile.ai[1] = -Projectile.ai[1];
//             holding = true;
// 
//             for (int i = 0; i < Main.maxNPCs; i++)
//                 Projectile.localNPCImmunity[i] = 0;
// 
//             Projectile.numHits = 0;
//             CanHit = false;
//             mousePos = Main.myPlayer == Projectile.owner ? GetMouseWorld() : Owner.Center - aimVel;
//             Owner.direction = mousePos.X < Owner.Center.X ? -1 : 1;
//             FlipAsSword = Owner.direction == -1;
//             doSwing = true;
//             finalFlip = false;
//             playSwingSound = true;
//             swooshFade = false;
//             postSwingCooldown = postSwingCooldownMax;
// 
//             if (!continueHolding)
//                 willDie = true;
//         }
// 
//         private void AnimatePrism()
//         {
//             frameTimer++;
//             if (frameTimer >= 4)
//             {
//                 frameTimer = 0;
//                 Frame = (Frame + 1) % FrameCount;
//             }
//         }
// 
//         private void EmitHoldFlameFX()
//         {
//             if (Main.dedServ)
//                 return;
// 
//             flameTimer++;
//             float deathFade = willDie ? Utils.GetLerpValue(0f, postSwingCooldownMax, postSwingCooldown, true) : 1f;
//             Vector2 direction = ForwardDirection.SafeNormalize(Vector2.UnitX * Owner.direction);
//             Vector2 muzzle = MuzzlePosition;
//             Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
// 
//             for (int i = 0; i < 4; i++)
//             {
//                 Vector2 spawn = muzzle - direction * Main.rand.NextFloat(4f, 28f) + normal * Main.rand.NextFloat(-9f, 9f);
//                 Vector2 velocity = -direction.RotatedByRandom(0.28f) * Main.rand.NextFloat(4f, 13f) - Owner.velocity * 0.8f;
//                 Color color = Main.rand.NextBool(4) ? TyrantWhite : Color.Lerp(TyrantOrange, TyrantGold, Main.rand.NextFloat(0.25f, 0.8f));
//                 Particle flame = new CustomSpark(
//                     spawn,
//                     velocity,
//                     "CalamityMod/Particles/BloomCircle",
//                     false,
//                     Main.rand.Next(10, 15),
//                     Main.rand.NextFloat(0.28f, 0.52f) * deathFade,
//                     color * deathFade,
//                     new Vector2(0.72f, 1.15f),
//                     shrinkSpeed: 0.9f);
//                 GeneralParticleHandler.SpawnParticle(flame);
//                 heldFlameParticles.Add(flame);
//             }
// 
//             if (flameTimer % 9 == 0)
//                 SoundEngine.PlaySound(SoundID.Item34 with { Volume = 0.18f * deathFade, Pitch = -0.22f, PitchVariance = 0.14f, MaxInstances = 4 }, muzzle);
//         }
// 
//         private void EmitSlashEdgeFX()
//         {
//             if (Main.dedServ)
//                 return;
// 
//             for (int i = 0; i < 4; i++)
//             {
//                 float randRot = Main.rand.NextFloat(-20f, -100f);
//                 Vector2 dustVel = new Vector2(0f, 11f * -Projectile.ai[1] * Owner.direction).RotatedBy(FinalRotation + MathHelper.ToRadians(randRot));
//                 Vector2 placement = Owner.Center + new Vector2(430f, 0f).RotatedBy(FinalRotation + MathHelper.ToRadians(-45f)).RotatedByRandom(0.3f);
//                 GeneralParticleHandler.SpawnParticle(new CustomSpark(
//                     placement,
//                     dustVel,
//                     "CalamityMod/Particles/DemonSigilParticle",
//                     false,
//                     28,
//                     Main.rand.NextFloat(0.34f, 0.5f),
//                     Main.rand.NextBool() ? TyrantGold : TyrantOrange,
//                     Vector2.One,
//                     shrinkSpeed: 0.12f));
//             }
//         }
// 
//         private void UpdateOwnedFlameParticles(Vector2 playerMovement)
//         {
//             for (int i = heldFlameParticles.Count - 1; i >= 0; i--)
//             {
//                 Particle particle = heldFlameParticles[i];
//                 if (particle.Time >= particle.Lifetime)
//                 {
//                     heldFlameParticles.RemoveAt(i);
//                     continue;
//                 }
// 
//                 particle.Position += playerMovement;
//             }
//         }
// 
//         private Vector2 GetMouseWorld()
//         {
//             Vector2 mouseWorld = Owner.Calamity().mouseWorld;
//             return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
//         }
// 
//         public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
//         {
//             lastHitTarget = target;
//             lastHitTargetIndex = target.whoAmI;
// 
//             target.AddBuff(ModContent.BuffType<Dragonfire>(), 240);
//             target.AddBuff(ModContent.BuffType<DemonicFlames>(), 180);
//             SpawnHitEffects(target);
//             Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 5.8f);
//             SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DemonSwordInsaneImpact") { Volume = 0.58f, Pitch = MathHelper.Clamp(swingCount * 0.04f, -0.18f, 0.32f) }, Projectile.Center);
//         }
// 
//         public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
//         {
//             modifiers.SourceDamage *= Utils.Remap(Projectile.numHits, 0f, 6f, 1f, 0.5f, true);
//         }
// 
//         private void SpawnHitEffects(NPC target)
//         {
//             if (Main.dedServ)
//                 return;
// 
//             Vector2 launchVel = Utils.DirectionTo(Owner.Center, GetMouseWorld());
//             for (int i = 0; i < 18; i++)
//             {
//                 float variance = Main.rand.NextFloat(-0.5f, 0.5f);
//                 Vector2 velocity = (launchVel * 42f).RotatedBy(variance) * Main.rand.NextFloat(0.2f, 1f) * (1f - Math.Abs(variance));
//                 Dust dust = Dust.NewDustPerfect(target.Center, DustID.GoldFlame);
//                 dust.scale = Main.rand.NextFloat(1.1f, 1.45f) - Math.Abs(variance);
//                 dust.velocity = velocity;
//                 dust.noGravity = true;
//                 dust.color = Main.rand.NextBool() ? TyrantGold : TyrantOrange;
// 
//                 GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, velocity, true, 42, 1.15f, Main.rand.NextBool() ? TyrantGold : TyrantOrange));
//             }
// 
//             GeneralParticleHandler.SpawnParticle(new CustomPulse(target.Center, Vector2.Zero, TyrantGold, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.6f, 1.1f, 18, true));
//             GeneralParticleHandler.SpawnParticle(new CustomPulse(target.Center, Vector2.Zero, TyrantWhite, "CalamityMod/Particles/BloomCircle", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.25f, 0.68f, 18, true));
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             Asset<Texture2D> texture = ModContent.Request<Texture2D>(Texture);
//             Asset<Texture2D> swoosh = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge");
//             Asset<Texture2D> bloomLine = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineAngled");
//             Asset<Texture2D> bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle");
//             Rectangle frame = texture.Value.Frame(1, FrameCount, 0, Frame);
//             Vector2 frameOrigin = frame.Size() * 0.5f;
//             float swordFlipRotation = FlipAsSword ? MathHelper.ToRadians(90f) : 0f;
//             float deathFade = willDie ? Utils.GetLerpValue(0f, postSwingCooldownMax, postSwingCooldown, true) : 1f;
// 
//             for (int i = 0; i < 18; i++)
//             {
//                 Vector2 offsetDir = Vector2.One.RotatedBy(Projectile.rotation + RotationOffset + MathHelper.ToRadians(90f));
//                 Color auraColor = Color.Lerp(TyrantOrange, TyrantGold, Utils.GetLerpValue(0f, 17f, i)) with { A = 0 } * 0.28f * deathFade;
//                 Vector2 drawOffset = -offsetDir * 8f * deathFade * i;
//                 Main.EntitySpriteDraw(
//                     bloomLine.Value,
//                     Projectile.Center - offsetDir * 38f - Main.screenPosition + drawOffset + new Vector2(0f, Owner.gfxOffY) + Main.rand.NextVector2Circular(10f, 10f),
//                     null,
//                     auraColor,
//                     Projectile.rotation + RotationOffset + swordFlipRotation,
//                     bloomLine.Value.Size() * 0.5f,
//                     Vector2.One * (1f - i * 0.025f) * 0.045f,
//                     FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
//                     0f);
//             }
// 
//             if (fadeIn > 0.02f)
//             {
//                 Main.EntitySpriteDraw(
//                     swoosh.Value,
//                     Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY),
//                     null,
//                     TyrantOrange with { A = 0 } * fadeIn * 0.62f,
//                     FinalRotation + MathHelper.ToRadians(45f) + MathHelper.ToRadians(swingCount % 2 == 0 ? -65f : 65f) * -Owner.direction,
//                     swoosh.Value.Size() * 0.5f,
//                     Projectile.scale * 0.95f * hitboxMult,
//                     SpriteEffects.None);
// 
//                 Main.EntitySpriteDraw(
//                     swoosh.Value,
//                     Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY),
//                     null,
//                     TyrantGold with { A = 0 } * fadeIn * 0.8f,
//                     FinalRotation + MathHelper.ToRadians(45f) + MathHelper.ToRadians(swingCount % 2 == 0 ? -65f : 65f) * -Owner.direction,
//                     swoosh.Value.Size() * 0.5f,
//                     Projectile.scale * 1.25f * hitboxMult,
//                     SpriteEffects.None);
//             }
// 
//             for (int i = 0; i < 8; i++)
//             {
//                 Vector2 drawOffset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 4f * (willDie ? (1f - deathFade) * 2f : 1f);
//                 Main.EntitySpriteDraw(
//                     texture.Value,
//                     Projectile.Center - Main.screenPosition + drawOffset + new Vector2(0f, Owner.gfxOffY),
//                     frame,
//                     TyrantGold with { A = 0 } * 0.12f * deathFade,
//                     Projectile.rotation + RotationOffset + swordFlipRotation,
//                     FlipAsSword ? new Vector2(frame.Width - frameOrigin.X, frameOrigin.Y) : frameOrigin,
//                     Projectile.scale,
//                     FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
//             }
// 
//             Main.EntitySpriteDraw(
//                 texture.Value,
//                 Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY),
//                 frame,
//                 Color.Lerp(TyrantGold with { A = 0 }, lightColor, deathFade) * deathFade,
//                 Projectile.rotation + RotationOffset + swordFlipRotation,
//                 FlipAsSword ? new Vector2(frame.Width - frameOrigin.X, frameOrigin.Y) : frameOrigin,
//                 Projectile.scale,
//                 FlipAsSword ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
// 
//             Main.EntitySpriteDraw(
//                 bloom.Value,
//                 MuzzlePosition - Main.screenPosition,
//                 null,
//                 TyrantGold with { A = 0 } * (0.24f + fadeIn * 0.3f) * deathFade,
//                 Projectile.rotation,
//                 bloom.Value.Size() * 0.5f,
//                 0.16f + fadeIn * 0.08f,
//                 SpriteEffects.None);
// 
//             return false;
//         }
// 
//         public override void SendExtraAI(BinaryWriter writer)
//         {
//             writer.Write(willDie);
//             writer.Write7BitEncodedInt(useAnim);
//             writer.Write7BitEncodedInt(postSwingCooldown);
//             writer.Write7BitEncodedInt(swingCount);
//             writer.Write(holding);
//             writer.WriteVector2(aimVel);
//             writer.Write(Animation);
//             writer.Write7BitEncodedInt(lastHitTargetIndex);
//         }
// 
//         public override void ReceiveExtraAI(BinaryReader reader)
//         {
//             willDie = reader.ReadBoolean();
//             useAnim = reader.Read7BitEncodedInt();
//             postSwingCooldown = reader.Read7BitEncodedInt();
//             swingCount = reader.Read7BitEncodedInt();
//             holding = reader.ReadBoolean();
//             aimVel = reader.ReadVector2();
//             Animation = reader.ReadSingle();
//             lastHitTargetIndex = reader.Read7BitEncodedInt();
//             mousePos = Owner.Center - aimVel;
//         }
//     }
// 
// }
// 
// */
// 