// Abandoned by 2026-06-09 dragon-scheme Yharim's Crystal refactor.
// Original file is line-commented so tModLoader in-game compilation cannot load it accidentally.
//
// /*
// Abandoned by 2026-06-09 dragon-scheme Yharim's Crystal refactor.
// Original file was fully commented before moving so tModLoader in-game compilation cannot load it accidentally.
// 
// using CalamityLegendsComeBack.Accssory.YC;
// using CalamityLegendsComeBack.Weapons.YharimsCrystal;
// using CalamityLegendsComeBack.Weapons.YharimsCrystal.EXSkill;
// using CalamityMod;
// using CalamityMod.Buffs.DamageOverTime;
// using CalamityMod.Particles;
// using CalamityMod.Projectiles.Magic;
// using Microsoft.Xna.Framework;
// using Microsoft.Xna.Framework.Graphics;
// using Terraria;
// using Terraria.Audio;
// using Terraria.DataStructures;
// using Terraria.Enums;
// using Terraria.GameContent;
// using Terraria.ID;
// using Terraria.ModLoader;
// 
// namespace CalamityLegendsComeBack.Weapons.YharimsCrystal.MainAttack.E_TyrantPrism
// {
//     internal static class YC_TyrantPrismDroneCoordinator
//     {
//         public const int IdleParentIndex = -1;
// 
//         public static bool ShouldKeepDrones(Player player)
//         {
//             if (YC_EXHelper.TryGetActiveVip(player.whoAmI, out _, out _))
//                 return player.active && !player.dead;
// 
//             return player.active &&
//                 !player.dead &&
//                 player.HeldItem != null &&
//                 !player.HeldItem.IsAir &&
//                 player.HeldItem.type == ModContent.ItemType<NewLegendYharimsCrystal>();
//         }
// 
//         public static void EnsureIdleDrones(Player player, IEntitySource source, int damage, float knockback)
//         {
//             if (Main.myPlayer != player.whoAmI || !ShouldKeepDrones(player))
//                 return;
// 
//             for (int slot = 0; slot < YC_TyrantPrismHoldout.DroneCount; slot++)
//             {
//                 if (TryFindDrone(player.whoAmI, slot, out _, out _))
//                     continue;
// 
//                 Projectile.NewProjectile(
//                     source,
//                     player.MountedCenter,
//                     Vector2.Zero,
//                     ModContent.ProjectileType<YC_TyrantPrismDrone>(),
//                     damage,
//                     knockback,
//                     player.whoAmI,
//                     slot,
//                     IdleParentIndex);
//             }
//         }
// 
//         public static bool TryFindDrone(int owner, int slot, out Projectile droneProjectile, out YC_TyrantPrismDrone drone)
//         {
//             droneProjectile = null;
//             drone = null;
// 
//             for (int i = 0; i < Main.maxProjectiles; i++)
//             {
//                 Projectile other = Main.projectile[i];
//                 if (!other.active ||
//                     other.owner != owner ||
//                     other.type != ModContent.ProjectileType<YC_TyrantPrismDrone>() ||
//                     (int)other.ai[0] != slot ||
//                     other.ModProjectile is not YC_TyrantPrismDrone droneMod)
//                 {
//                     continue;
//                 }
// 
//                 droneProjectile = other;
//                 drone = droneMod;
//                 return true;
//             }
// 
//             return false;
//         }
// 
//         public static int CountOwnedDrones(int owner)
//         {
//             int count = 0;
//             for (int slot = 0; slot < YC_TyrantPrismHoldout.DroneCount; slot++)
//             {
//                 if (TryFindDrone(owner, slot, out _, out _))
//                     count++;
//             }
// 
//             return count;
//         }
//     }
// 
//     public class YC_TyrantPrismHoldout : YC_BaseHoldout
//     {
//         public enum TyrantPrismState
//         {
//             // 蓄力阶段：僚机围绕水晶螺旋旋转，每个僚机各自发射一道汇聚激光。
//             Converging,
//             // 攻击形态：蓄力完成后持续发射机枪弹幕与僚机激光，方向跟随鼠标附近。
//             Combat,
//             HeavyRest
//         }
// 
//         public const int DroneCount = 6;
//         public const int ConvergenceFrames = 156;
//         public const int SpawnInterval = 13;
//         public const int MainBeamFadeFrames = 46;
//         public const int HeavyRestFrames = 96;
//         private const int ManaDrainInterval = 10;
// 
//         private int manaDrainTimer;
//         private bool convergenceReadySoundPlayed;
// 
//         private ref float StateRaw => ref Projectile.ai[0];
//         private ref float StateTimerRaw => ref Projectile.ai[1];
//         private ref float CommandSerialRaw => ref Projectile.ai[2];
// 
//         public TyrantPrismState CurrentState => (TyrantPrismState)(int)StateRaw;
//         public int StateTimer => (int)StateTimerRaw;
//         public int CommandSerial => (int)CommandSerialRaw;
//         public float HoldFrames => HoldFrameCounter;
//         public float ConvergenceRatio => MathHelper.Clamp(HoldFrameCounter / ConvergenceFrames, 0f, 1f);
//         public float MainBeamStrength => CurrentState == TyrantPrismState.Converging
//             ? Utils.GetLerpValue(ConvergenceFrames - MainBeamFadeFrames, ConvergenceFrames, HoldFrameCounter, true)
//             : 1f;
//         public bool MainBeamCanDamage => CurrentState != TyrantPrismState.Converging || MainBeamStrength > 0.34f;
//         public bool DroneCombatOnline => CurrentState != TyrantPrismState.Converging;
//         public Vector2 MainMuzzle => Projectile.Center + ForwardDirection * 24f;
//         public Vector2 BeamFocusPoint => Projectile.Center + ForwardDirection * MathHelper.Lerp(720f, 1320f, MainBeamStrength);
// 
//         protected override float HoldoutDistance => 4f;
//         protected override float SoundPitch => 0.14f;
// 
//         protected override void OnHoldoutAI()
//         {
//             Projectile.damage = Owner.GetWeaponDamage(Owner.HeldItem);
// 
//             if (Main.myPlayer == Projectile.owner)
//                 Owner.Calamity().rightClickListener = true;
// 
//             EnsureMainBeams();
//             EnsureDrones();
//             DrainManaOrReset();
// 
//             StateTimerRaw++;
// 
//             if (CurrentState == TyrantPrismState.Converging)
//             {
//                 if (HoldFrameCounter >= ConvergenceFrames && CountOwnedDrones() >= DroneCount)
//                     EnterCombatState();
// 
//                 EmitConvergenceFX();
//                 return;
//             }
// 
//             if (CurrentState == TyrantPrismState.Combat)
//             {
//                 TryIssueHeavyCommand();
//                 EmitCombatFX();
//                 return;
//             }
// 
//             if (StateTimer >= HeavyRestFrames)
//                 SetState(TyrantPrismState.Combat);
// 
//             EmitRestFX();
//         }
// 
//         public override void OnKill(int timeLeft)
//         {
//             KillOwnedPrismProjectiles();
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             base.PreDraw(ref lightColor);
// 
//             if (Main.dedServ)
//                 return false;
// 
//             Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
//             Vector2 muzzle = MainMuzzle - Main.screenPosition;
//             float strength = MathHelper.Clamp(0.18f + MainBeamStrength, 0f, 1f);
//             float pulse = 1f + 0.08f * (float)System.Math.Sin(HoldFrameCounter * 0.18f);
//             Color gold = new Color(255, 216, 104, 0);
// 
//             Main.EntitySpriteDraw(glow, muzzle, null, gold * (0.38f * strength), Projectile.rotation, glow.Size() * 0.5f, (0.13f + MainBeamStrength * 0.2f) * pulse, SpriteEffects.None, 0f);
//             Main.EntitySpriteDraw(glow, muzzle, null, (Color.White with { A = 0 }) * (0.18f + MainBeamStrength * 0.32f), Projectile.rotation, glow.Size() * 0.5f, (0.055f + MainBeamStrength * 0.08f) * pulse, SpriteEffects.None, 0f);
// 
//             return false;
//         }
// 
//         public bool TryGetDrone(int slot, out Projectile droneProjectile, out YC_TyrantPrismDrone drone)
//         {
//             if (!YC_TyrantPrismDroneCoordinator.TryFindDrone(Projectile.owner, slot, out droneProjectile, out drone))
//                 return false;
// 
//             if ((int)droneProjectile.ai[1] != Projectile.whoAmI)
//             {
//                 droneProjectile.ai[1] = Projectile.whoAmI;
//                 droneProjectile.netUpdate = true;
//             }
// 
//             return true;
//         }
// 
//         private void EnsureMainBeams()
//         {
//             if (Projectile.owner != Main.myPlayer)
//                 return;
// 
//             for (int beamID = 0; beamID < YC_YharimsCrystalBeam.NumBeams; beamID++)
//             {
//                 if (HasMainBeam(beamID))
//                     continue;
// 
//                 Projectile.NewProjectile(
//                     Projectile.GetSource_FromThis(),
//                     MainMuzzle,
//                     ForwardDirection,
//                     ModContent.ProjectileType<YC_YharimsCrystalBeam>(),
//                     Projectile.damage,
//                     Projectile.knockBack,
//                     Projectile.owner,
//                     beamID,
//                     Projectile.whoAmI,
//                     (float)YC_YharimsCrystalBeam.BeamHostKind.MainHoldout);
//             }
//         }
// 
//         private bool HasMainBeam(int beamID)
//         {
//             int beamType = ModContent.ProjectileType<YC_YharimsCrystalBeam>();
//             for (int i = 0; i < Main.maxProjectiles; i++)
//             {
//                 Projectile other = Main.projectile[i];
//                 if (other.active &&
//                     other.owner == Projectile.owner &&
//                     other.type == beamType &&
//                     (int)other.ai[0] == beamID &&
//                     (int)other.ai[1] == Projectile.whoAmI &&
//                     (YC_YharimsCrystalBeam.BeamHostKind)(int)other.ai[2] == YC_YharimsCrystalBeam.BeamHostKind.MainHoldout)
//                 {
//                     return true;
//                 }
//             }
// 
//             return false;
//         }
// 
//         private void EnsureDrones()
//         {
//             if (Projectile.owner != Main.myPlayer)
//                 return;
// 
//             for (int slot = 0; slot < DroneCount; slot++)
//             {
//                 if (HoldFrameCounter < slot * SpawnInterval)
//                     break;
// 
//                 if (TryGetDrone(slot, out _, out _))
//                     continue;
// 
//                 Projectile.NewProjectile(
//                     Projectile.GetSource_FromThis(),
//                     Projectile.Center,
//                     ForwardDirection,
//                     ModContent.ProjectileType<YC_TyrantPrismDrone>(),
//                     Projectile.damage,
//                     Projectile.knockBack,
//                     Projectile.owner,
//                     slot,
//                     Projectile.whoAmI);
// 
//                 SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.16f, Pitch = 0.08f + slot * 0.025f }, Projectile.Center);
//             }
//         }
// 
//         private int CountOwnedDrones()
//         {
//             return YC_TyrantPrismDroneCoordinator.CountOwnedDrones(Projectile.owner);
//         }
// 
//         private void DrainManaOrReset()
//         {
//             if (Projectile.owner != Main.myPlayer)
//                 return;
// 
//             manaDrainTimer++;
//             if (manaDrainTimer < ManaDrainInterval)
//                 return;
// 
//             manaDrainTimer = 0;
//             if (Owner.CheckMana(Owner.HeldItem, -1, true))
//                 return;
// 
//             SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.45f }, Owner.Center);
//             Projectile.Kill();
//         }
// 
//         private void EnterCombatState()
//         {
//             SetState(TyrantPrismState.Combat);
// 
//             if (!convergenceReadySoundPlayed)
//             {
//                 convergenceReadySoundPlayed = true;
//                 SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.48f, Pitch = -0.12f }, Projectile.Center);
//                 Owner.Calamity().GeneralScreenShakePower = System.Math.Max(Owner.Calamity().GeneralScreenShakePower, 4.5f);
//             }
//         }
// 
//         private void TryIssueHeavyCommand()
//         {
//             if (Projectile.owner != Main.myPlayer)
//                 return;
// 
//             if (!Owner.Calamity().mouseRight || !Main.mouseRightRelease || Main.mapFullscreen || Main.blockMouse)
//                 return;
// 
//             int heavyManaCost = System.Math.Max(1, (int)(Owner.HeldItem.mana * Owner.manaCost * 8f));
//             if (!Owner.CheckMana(Owner.HeldItem, heavyManaCost, true))
//             {
//                 SoundEngine.PlaySound(SoundID.MaxMana with { Pitch = -0.45f }, Owner.Center);
//                 Projectile.Kill();
//                 return;
//             }
// 
//             CommandSerialRaw++;
//             SetState(TyrantPrismState.HeavyRest);
//             SoundEngine.PlaySound(SoundID.Item92 with { Volume = 0.5f, Pitch = -0.2f }, Projectile.Center);
//             Owner.Calamity().GeneralScreenShakePower = System.Math.Max(Owner.Calamity().GeneralScreenShakePower, 5.8f);
//         }
// 
//         private void SetState(TyrantPrismState state)
//         {
//             StateRaw = (int)state;
//             StateTimerRaw = 0f;
//             Projectile.netUpdate = true;
//         }
// 
//         private void KillOwnedPrismProjectiles()
//         {
//             for (int i = 0; i < Main.maxProjectiles; i++)
//             {
//                 Projectile other = Main.projectile[i];
//                 if (!other.active || other.owner != Projectile.owner)
//                     continue;
// 
//                 if (other.type == ModContent.ProjectileType<YC_TyrantPrismDrone>() && (int)other.ai[1] == Projectile.whoAmI)
//                 {
//                     other.ai[1] = YC_TyrantPrismDroneCoordinator.IdleParentIndex;
//                     other.netUpdate = true;
//                 }
//                 else if (other.type == ModContent.ProjectileType<YC_TyrantPrismMainBeam>() && (int)other.ai[0] == Projectile.whoAmI)
//                     other.Kill();
//                 else if (other.type == ModContent.ProjectileType<YC_TyrantPrismConvergeBeam>() && (int)other.ai[1] == Projectile.whoAmI)
//                     other.Kill();
//                 else if (other.type == ModContent.ProjectileType<YC_YharimsCrystalBeam>() &&
//                     (int)other.ai[1] == Projectile.whoAmI &&
//                     (YC_YharimsCrystalBeam.BeamHostKind)(int)other.ai[2] == YC_YharimsCrystalBeam.BeamHostKind.MainHoldout)
//                 {
//                     other.Kill();
//                 }
//             }
//         }
// 
//         private void EmitConvergenceFX()
//         {
//             if (Main.dedServ || Main.GameUpdateCount % 4 != 0)
//                 return;
// 
//             Vector2 normal = ForwardDirection.RotatedBy(MathHelper.PiOver2);
//             float radius = MathHelper.Lerp(42f, 8f, ConvergenceRatio);
//             Vector2 position = MainMuzzle + normal * Main.rand.NextFloat(-radius, radius) - ForwardDirection * Main.rand.NextFloat(4f, 30f);
//             EmitDust(
//                 position,
//                 ForwardDirection.RotatedByRandom(0.28f) * Main.rand.NextFloat(0.8f, 2.4f),
//                 Color.Lerp(new Color(255, 188, 86), Color.White, Main.rand.NextFloat(0.18f, 0.58f)),
//                 Main.rand.NextFloat(0.7f, 1.1f),
//                 DustID.GoldFlame);
//         }
// 
//         private void EmitCombatFX()
//         {
//             if (Main.dedServ || Main.GameUpdateCount % 8 != 0)
//                 return;
// 
//             GlowOrbParticle glow = new(
//                 MainMuzzle + Main.rand.NextVector2Circular(5f, 5f),
//                 ForwardDirection * Main.rand.NextFloat(0.4f, 1.3f),
//                 false,
//                 Main.rand.Next(8, 12),
//                 Main.rand.NextFloat(0.22f, 0.34f),
//                 Color.Lerp(new Color(255, 214, 108), Color.White, Main.rand.NextFloat(0.25f, 0.65f)),
//                 true,
//                 false,
//                 true);
//             GeneralParticleHandler.SpawnParticle(glow);
//         }
// 
//         private void EmitRestFX()
//         {
//             if (Main.dedServ || Main.GameUpdateCount % 10 != 0)
//                 return;
// 
//             Dust dust = Dust.NewDustPerfect(
//                 MainMuzzle + Main.rand.NextVector2Circular(8f, 8f),
//                 DustID.SteampunkSteam,
//                 -Vector2.UnitY * Main.rand.NextFloat(0.4f, 1.2f),
//                 80,
//                 default,
//                 Main.rand.NextFloat(0.45f, 0.85f));
//             dust.noGravity = false;
//             dust.color = new Color(255, 210, 110);
//         }
//     }
// 
// public class YC_TyrantPrismDrone : ModProjectile, ILocalizedModType
//     {
//         private static readonly Vector2[] FleetOffsets =
//         {
//             new(-72f, -18f),
//             new(-118f, -54f),
//             new(-52f, -96f),
//             new(72f, -18f),
//             new(118f, -54f),
//             new(52f, -96f)
//         };
// 
//         private bool positionInitialized;
//         private int attackTimer;
//         private int lastCommandSerial = -1;
//         private int lastRhythmStep = -1;
// 
//         public new string LocalizationCategory => "Projectiles.YharimsCrystal";
//         public override string Texture => "CalamityLegendsComeBack/Weapons/YharimsCrystal/\u56FE\u7247/\u9ED8\u8BA4\u6218\u673A";
// 
//         public int SlotIndex => (int)Projectile.ai[0];
//         public int ParentHoldoutIndex => (int)Projectile.ai[1];
//         public Vector2 CurrentForwardDirection { get; private set; } = Vector2.UnitX;
// 
//         public override void SetStaticDefaults()
//         {
//             ProjectileID.Sets.TrailCacheLength[Type] = 10;
//             ProjectileID.Sets.TrailingMode[Type] = 2;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 34;
//             Projectile.height = 34;
//             Projectile.penetrate = -1;
//             Projectile.tileCollide = false;
//             Projectile.ignoreWater = true;
//             Projectile.friendly = false;
//             Projectile.hostile = false;
//             Projectile.netImportant = true;
//             Projectile.DamageType = DamageClass.Magic;
//             Projectile.timeLeft = 2;
//         }
// 
//         public override bool ShouldUpdatePosition() => false;
//         public override bool? CanDamage() => false;
// 
//         public override void AI()
//         {
//             Player owner = Main.player[Projectile.owner];
//             if (!YC_TyrantPrismDroneCoordinator.ShouldKeepDrones(owner))
//             {
//                 Projectile.Kill();
//                 return;
//             }
// 
//             Projectile.timeLeft = 2;
//             Projectile.damage = owner.GetWeaponDamage(owner.HeldItem);
// 
//             if (YC_EXHelper.TryGetActiveVip(Projectile.owner, out _, out YC_EX_VIP vip))
//             {
//                 Projectile.ai[1] = YC_TyrantPrismDroneCoordinator.IdleParentIndex;
//                 KillConvergenceBeam();
//                 UpdateUltimateMovement(owner, vip);
//                 UpdateUltimateFacing(owner);
//                 EnsurePersistentYharimBeam();
//                 UpdateUltimateAttacks(owner, vip);
//                 EmitUltimateFX(vip);
//                 Lighting.AddLight(Projectile.Center, new Color(255, 213, 116).ToVector3() * 0.66f);
//                 return;
//             }
// 
//             if (!TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout))
//             {
//                 Projectile.ai[1] = YC_TyrantPrismDroneCoordinator.IdleParentIndex;
//                 KillConvergenceBeam();
//                 UpdateIdleMovement(owner);
//                 UpdateIdleFacing(owner);
//                 EnsurePersistentYharimBeam();
//                 EmitIdleOrbitFX();
//                 Lighting.AddLight(Projectile.Center, new Color(255, 213, 116).ToVector3() * 0.36f);
//                 return;
//             }
// 
//             Projectile.damage = holdoutProjectile.damage;
// 
//             UpdateMovement(owner, holdoutProjectile, holdout);
//             UpdateFacing(owner, holdout);
//             KillConvergenceBeam();
//             EnsurePersistentYharimBeam();
//             UpdateAttacks(owner, holdout);
//             EmitIdleFX(holdout);
// 
//             Lighting.AddLight(Projectile.Center, new Color(255, 213, 116).ToVector3() * 0.42f);
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             Texture2D texture = TextureAssets.Projectile[Type].Value;
//             Vector2 origin = texture.Size() * 0.5f;
//             SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
// 
//             for (int i = Projectile.oldPos.Length - 1; i >= 0; i--)
//             {
//                 if (Projectile.oldPos[i] == Vector2.Zero)
//                     continue;
// 
//                 float completion = 1f - i / (float)Projectile.oldPos.Length;
//                 Vector2 oldDraw = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
//                 Main.EntitySpriteDraw(texture, oldDraw, null, new Color(255, 212, 104, 0) * (0.06f + completion * 0.12f), Projectile.rotation, origin, Projectile.scale, effects, 0);
//             }
// 
//             Vector2 drawPosition = Projectile.Center - Main.screenPosition;
//             Main.EntitySpriteDraw(texture, drawPosition, null, Color.White, Projectile.rotation, origin, Projectile.scale, effects, 0);
//             Main.EntitySpriteDraw(texture, drawPosition, null, new Color(255, 232, 160, 0) * 0.32f, Projectile.rotation, origin, Projectile.scale * 1.08f, effects, 0);
//             DrawUltimateCrosshair(drawPosition);
//             return false;
//         }
// 
//         private void DrawUltimateCrosshair(Vector2 drawPosition)
//         {
//             if (!YC_EXHelper.TryGetActiveVip(Projectile.owner, out _, out YC_EX_VIP vip) ||
//                 vip.CurrentState != YC_EX_VIP.EXVipState.AwaitingFireCommand)
//             {
//                 return;
//             }
// 
//             Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomRing").Value;
//             Texture2D star = ModContent.Request<Texture2D>("CalamityMod/Particles/HalfStar").Value;
//             Color gold = new Color(255, 220, 120, 0);
//             float pulse = 0.92f + 0.08f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 10f + SlotIndex);
//             Main.EntitySpriteDraw(ring, drawPosition, null, gold * 0.72f, Main.GlobalTimeWrappedHourly * 1.8f + SlotIndex, ring.Size() * 0.5f, 0.28f * pulse, SpriteEffects.None, 0f);
//             for (int i = 0; i < 4; i++)
//             {
//                 float rotation = MathHelper.PiOver2 * i + Main.GlobalTimeWrappedHourly * 2.4f;
//                 Main.EntitySpriteDraw(star, drawPosition, null, Color.White with { A = 0 } * 0.36f, rotation, star.Size() * 0.5f, new Vector2(0.08f, 0.58f) * pulse, SpriteEffects.None, 0f);
//             }
//         }
// 
//         private bool TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout)
//         {
//             holdoutProjectile = null;
//             holdout = null;
// 
//             if (ParentHoldoutIndex < 0 || ParentHoldoutIndex >= Main.maxProjectiles)
//                 return false;
// 
//             Projectile candidate = Main.projectile[ParentHoldoutIndex];
//             if (!candidate.active ||
//                 candidate.owner != Projectile.owner ||
//                 candidate.type != ModContent.ProjectileType<YC_TyrantPrismHoldout>() ||
//                 candidate.ModProjectile is not YC_TyrantPrismHoldout holdoutMod)
//             {
//                 return false;
//             }
// 
//             holdoutProjectile = candidate;
//             holdout = holdoutMod;
//             return true;
//         }
// 
//         public bool TryGetActiveHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout) => TryGetHoldout(out holdoutProjectile, out holdout);
// 
//         private void UpdateMovement(Player owner, Projectile holdoutProjectile, YC_TyrantPrismHoldout holdout)
//         {
//             Vector2 desiredCenter = GetDesiredCenter(owner, holdoutProjectile, holdout);
//             if (!positionInitialized)
//             {
//                 Projectile.Center = desiredCenter;
//                 positionInitialized = true;
//                 return;
//             }
// 
//             float ownerSpeed = owner.velocity.Length();
//             float speedBoost = Utils.GetLerpValue(8f, 42f, ownerSpeed, true);
//             float response = holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging ? 0.16f : 0.105f;
//             response += speedBoost * 0.14f;
//             float maxSpeed = holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging ? 18f : 13f;
//             maxSpeed += ownerSpeed * MathHelper.Lerp(0.75f, 1.35f, speedBoost);
//             Vector2 desiredVelocity = (desiredCenter - Projectile.Center) * response;
//             if (desiredVelocity.Length() > maxSpeed)
//                 desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;
// 
//             Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.34f);
//             Projectile.Center += Projectile.velocity;
//             Projectile.velocity *= 0.92f;
//         }
// 
//         private void UpdateIdleMovement(Player owner)
//         {
//             float phase = Main.GlobalTimeWrappedHourly * 2.7f + SlotIndex * MathHelper.TwoPi / YC_TyrantPrismHoldout.DroneCount;
//             float radius = 86f + (SlotIndex % 2) * 10f;
//             Vector2 desiredCenter = owner.MountedCenter + phase.ToRotationVector2() * radius + Vector2.UnitY * (float)System.Math.Sin(phase * 1.4f) * 6f;
//             float ownerSpeed = owner.velocity.Length();
//             float boost = Utils.GetLerpValue(8f, 42f, ownerSpeed, true);
//             MoveToward(desiredCenter, 0.09f + boost * 0.12f, 10f + ownerSpeed * MathHelper.Lerp(0.75f, 1.25f, boost), 0.26f + boost * 0.18f);
//         }
// 
//         private void UpdateUltimateMovement(Player owner, YC_EX_VIP vip)
//         {
//             float charge = vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge
//                 ? MathHelper.Clamp(vip.CurrentStateTimer / (float)YC_EX_VIP.DroneChargeTime, 0f, 1f)
//                 : vip.CurrentState == YC_EX_VIP.EXVipState.AwaitingFireCommand ? 1f : 0.75f;
// 
//             float baseSpeed = vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge
//                 ? MathHelper.Lerp(4.4f, 18f, charge)
//                 : vip.CurrentState == YC_EX_VIP.EXVipState.AwaitingFireCommand ? 2.8f : 7.2f;
//             float radius = vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge
//                 ? MathHelper.Lerp(96f, 48f, charge)
//                 : vip.CurrentState == YC_EX_VIP.EXVipState.AwaitingFireCommand ? 72f : 82f;
//             float phase = Main.GlobalTimeWrappedHourly * baseSpeed + SlotIndex * MathHelper.TwoPi / YC_TyrantPrismHoldout.DroneCount;
//             Vector2 desiredCenter = owner.MountedCenter + phase.ToRotationVector2() * radius;
//             float ownerSpeed = owner.velocity.Length();
//             float boost = Utils.GetLerpValue(8f, 42f, ownerSpeed, true);
//             MoveToward(
//                 desiredCenter,
//                 MathHelper.Lerp(0.12f, 0.23f, charge) + boost * 0.14f,
//                 MathHelper.Lerp(14f, 26f, charge) + ownerSpeed * MathHelper.Lerp(0.7f, 1.25f, boost),
//                 0.34f + boost * 0.16f);
//         }
// 
//         private void MoveToward(Vector2 desiredCenter, float response, float maxSpeed, float velocityBlend)
//         {
//             if (!positionInitialized)
//             {
//                 Projectile.Center = desiredCenter;
//                 positionInitialized = true;
//                 return;
//             }
// 
//             Vector2 desiredVelocity = (desiredCenter - Projectile.Center) * response;
//             if (desiredVelocity.Length() > maxSpeed)
//                 desiredVelocity = desiredVelocity.SafeNormalize(Vector2.Zero) * maxSpeed;
// 
//             Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, velocityBlend);
//             Projectile.Center += Projectile.velocity;
//             Projectile.velocity *= 0.9f;
//         }
// 
//         private Vector2 GetDesiredCenter(Player owner, Projectile holdoutProjectile, YC_TyrantPrismHoldout holdout)
//         {
//             Vector2 forward = holdout.ForwardDirection;
//             Vector2 right = forward.RotatedBy(MathHelper.PiOver2);
// 
//             if (holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging)
//             {
//                 float phase = holdout.HoldFrames * 0.105f + SlotIndex * MathHelper.TwoPi / YC_TyrantPrismHoldout.DroneCount;
//                 float spiralRadius = MathHelper.Lerp(34f, 14f, holdout.ConvergenceRatio);
//                 Vector2 spiralOffset = right * ((float)System.Math.Cos(phase) * spiralRadius) +
//                     forward * ((float)System.Math.Sin(phase * 1.35f) * spiralRadius * 0.42f);
//                 return holdout.MainMuzzle - forward * 20f + spiralOffset;
//             }
// 
//             Vector2 local = FleetOffsets[Utils.Clamp(SlotIndex, 0, FleetOffsets.Length - 1)];
//             float bob = (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4.1f + SlotIndex * 0.72f) * 4.5f;
//             return owner.MountedCenter + right * (local.X + bob * 0.45f) + forward * (local.Y + bob);
//         }
// 
//         private void UpdateFacing(Player owner, YC_TyrantPrismHoldout holdout)
//         {
//             Vector2 defaultDirection = holdout.ForwardDirection;
//             if (holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.Converging)
//             {
//                 CurrentForwardDirection = (holdout.BeamFocusPoint - Projectile.Center).SafeNormalize(defaultDirection);
//             }
//             else
//             {
//                 Vector2 mouseWorld = owner.Calamity().mouseWorld;
//                 if (mouseWorld == Vector2.Zero && owner.whoAmI == Main.myPlayer)
//                     mouseWorld = Main.MouseWorld;
// 
//                 CurrentForwardDirection = mouseWorld == Vector2.Zero
//                     ? defaultDirection
//                     : (mouseWorld - Projectile.Center).SafeNormalize(defaultDirection);
//             }
// 
//             Projectile.rotation = CurrentForwardDirection.ToRotation() + MathHelper.PiOver2;
//             Projectile.direction = Projectile.spriteDirection = CurrentForwardDirection.X >= 0f ? 1 : -1;
//             Projectile.scale = 0.86f + 0.035f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 5f + SlotIndex * 0.8f);
//         }
// 
//         private void UpdateIdleFacing(Player owner)
//         {
//             Vector2 orbitDirection = (Projectile.Center - owner.MountedCenter).SafeNormalize(Vector2.UnitX * owner.direction);
//             CurrentForwardDirection = orbitDirection;
//             Projectile.rotation = CurrentForwardDirection.ToRotation() + MathHelper.PiOver2;
//             Projectile.direction = Projectile.spriteDirection = CurrentForwardDirection.X >= 0f ? 1 : -1;
//             Projectile.scale = 0.84f + 0.025f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 4.4f + SlotIndex);
//         }
// 
//         private void UpdateUltimateFacing(Player owner)
//         {
//             NPC target = FindTarget(owner, 3600f, false);
//             Vector2 fallback = (Main.MouseWorld - Projectile.Center).SafeNormalize(Vector2.UnitX * owner.direction);
//             CurrentForwardDirection = target != null
//                 ? (target.Center - Projectile.Center).SafeNormalize(fallback)
//                 : fallback;
//             Projectile.rotation = CurrentForwardDirection.ToRotation() + MathHelper.PiOver2;
//             Projectile.direction = Projectile.spriteDirection = CurrentForwardDirection.X >= 0f ? 1 : -1;
//             Projectile.scale = 0.9f + 0.04f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 7f + SlotIndex);
//         }
// 
//         private void EnsurePersistentYharimBeam()
//         {
//             if (Projectile.owner != Main.myPlayer || HasPersistentYharimBeam())
//                 return;
// 
//             Projectile.NewProjectile(
//                 Projectile.GetSource_FromThis(),
//                 Projectile.Center + CurrentForwardDirection * 10f,
//                 CurrentForwardDirection,
//                 ModContent.ProjectileType<YC_YharimsCrystalBeam>(),
//                 System.Math.Max(1, (int)(Projectile.damage * 0.42f)),
//                 Projectile.knockBack * 0.2f,
//                 Projectile.owner,
//                 SlotIndex,
//                 Projectile.whoAmI,
//                 (float)YC_YharimsCrystalBeam.BeamHostKind.TyrantDrone);
// 
//             SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.13f, Pitch = -0.05f + SlotIndex * 0.025f, MaxInstances = 8 }, Projectile.Center);
//         }
// 
//         private bool HasPersistentYharimBeam()
//         {
//             int beamType = ModContent.ProjectileType<YC_YharimsCrystalBeam>();
//             for (int i = 0; i < Main.maxProjectiles; i++)
//             {
//                 Projectile other = Main.projectile[i];
//                 if (other.active &&
//                     other.owner == Projectile.owner &&
//                     other.type == beamType &&
//                     (int)other.ai[1] == Projectile.whoAmI &&
//                     (YC_YharimsCrystalBeam.BeamHostKind)(int)other.ai[2] == YC_YharimsCrystalBeam.BeamHostKind.TyrantDrone)
//                 {
//                     return true;
//                 }
//             }
// 
//             return false;
//         }
// 
//         private void EnsureConvergenceBeam(YC_TyrantPrismHoldout holdout)
//         {
//             if (holdout.CurrentState != YC_TyrantPrismHoldout.TyrantPrismState.Converging || Projectile.owner != Main.myPlayer || HasConvergenceBeam())
//                 return;
// 
//             Projectile.NewProjectile(
//                 Projectile.GetSource_FromThis(),
//                 Projectile.Center,
//                 CurrentForwardDirection,
//                 ModContent.ProjectileType<YC_TyrantPrismConvergeBeam>(),
//                 (int)(Projectile.damage * 0.42f),
//                 Projectile.knockBack * 0.2f,
//                 Projectile.owner,
//                 Projectile.whoAmI,
//                 ParentHoldoutIndex);
//         }
// 
//         private bool HasConvergenceBeam()
//         {
//             int beamType = ModContent.ProjectileType<YC_TyrantPrismConvergeBeam>();
//             for (int i = 0; i < Main.maxProjectiles; i++)
//             {
//                 Projectile other = Main.projectile[i];
//                 if (other.active && other.owner == Projectile.owner && other.type == beamType && (int)other.ai[0] == Projectile.whoAmI)
//                     return true;
//             }
// 
//             return false;
//         }
// 
//         private void KillConvergenceBeam()
//         {
//             int beamType = ModContent.ProjectileType<YC_TyrantPrismConvergeBeam>();
//             for (int i = 0; i < Main.maxProjectiles; i++)
//             {
//                 Projectile other = Main.projectile[i];
//                 if (other.active && other.owner == Projectile.owner && other.type == beamType && (int)other.ai[0] == Projectile.whoAmI)
//                     other.Kill();
//             }
//         }
// 
//         private void UpdateAttacks(Player owner, YC_TyrantPrismHoldout holdout)
//         {
//             if (!holdout.DroneCombatOnline)
//                 return;
// 
//             if (lastCommandSerial < 0)
//                 lastCommandSerial = holdout.CommandSerial;
// 
//             if (holdout.CommandSerial != lastCommandSerial)
//             {
//                 lastCommandSerial = holdout.CommandSerial;
//                 attackTimer = 48 + SlotIndex * 4;
//                 lastRhythmStep = -1;
// 
//                 if (Projectile.owner == Main.myPlayer)
//                     FireHeavySalvo();
//             }
// 
//             if (holdout.CurrentState == YC_TyrantPrismHoldout.TyrantPrismState.HeavyRest)
//                 return;
// 
//             int rhythmStep = holdout.StateTimer / 6;
//             if (rhythmStep == lastRhythmStep)
//                 return;
// 
//             lastRhythmStep = rhythmStep;
//             if (!IsRhythmPair(rhythmStep % 3))
//                 return;
// 
//             if (Projectile.owner == Main.myPlayer)
//                 FireRhythmVolley(rhythmStep);
// 
//             if (Projectile.owner == Main.myPlayer)
//                 TryFireAetherfluxRay(holdout.StateTimer);
//         }
// 
//         private bool IsRhythmPair(int pair) => pair switch
//         {
//             0 => SlotIndex is 0 or 3,
//             1 => SlotIndex is 1 or 4,
//             _ => SlotIndex is 2 or 5
//         };
// 
//         private void FireRhythmVolley(int rhythmStep)
//         {
//             YCAccessoryPlayer accessoryPlayer = Main.player[Projectile.owner].GetModPlayer<YCAccessoryPlayer>();
//             Vector2 direction = CurrentForwardDirection.SafeNormalize(Vector2.UnitX);
//             Vector2 muzzle = Projectile.Center + direction * 18f;
//             Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
// 
//             int bulletCount = 5 + (rhythmStep + SlotIndex) % 2 + accessoryPlayer.PrismVolleyBulletBonus;
//             float speedMultiplier = accessoryPlayer.PrismProjectileSpeedMultiplier;
//             for (int i = 0; i < bulletCount; i++)
//             {
//                 float spread = MathHelper.Lerp(-0.055f, 0.055f, bulletCount == 1 ? 0.5f : i / (float)(bulletCount - 1));
//                 Projectile.NewProjectile(
//                     Projectile.GetSource_FromThis(),
//                     muzzle + side * MathHelper.Lerp(-5f, 5f, bulletCount == 1 ? 0.5f : i / (float)(bulletCount - 1)),
//                     direction.RotatedBy(spread + Main.rand.NextFloat(-0.01f, 0.01f)) * Main.rand.NextFloat(25f, 29f) * speedMultiplier,
//                     ModContent.ProjectileType<YC_TyrantPrismBolt>(),
//                     (int)(Projectile.damage * 0.26f),
//                     Projectile.knockBack * 0.18f,
//                     Projectile.owner,
//                     SlotIndex + Main.rand.NextFloat(),
//                     0.9f);
//             }
// 
//             EmitMuzzleBurst(direction, 3, 4.2f);
//             SoundEngine.PlaySound(SoundID.Item12 with { Volume = 0.055f, Pitch = 0.18f + SlotIndex * 0.02f, PitchVariance = 0.04f, MaxInstances = 8 }, Projectile.Center);
//             SoundEngine.PlaySound(SoundID.Item33 with { Volume = 0.04f, Pitch = -0.18f + SlotIndex * 0.025f, PitchVariance = 0.08f, MaxInstances = 8 }, Projectile.Center);
//         }
// 
//         private void TryFireAetherfluxRay(int stateTimer)
//         {
//             int interval = Main.player[Projectile.owner].GetModPlayer<YCAccessoryPlayer>().AetherfluxInterval;
//             if ((stateTimer + SlotIndex * 19) % interval != 0)
//                 return;
// 
//             Vector2 direction = CurrentForwardDirection.SafeNormalize(Vector2.UnitX);
//             NPC target = FindTargetNearMouse(1500f, 620f);
//             if (target != null)
//                 direction = (target.Center - Projectile.Center).SafeNormalize(direction);
// 
//             Vector2 side = direction.RotatedBy(MathHelper.PiOver2);
//             for (int i = -1; i <= 1; i += 2)
//             {
//                 Projectile.NewProjectile(
//                     Projectile.GetSource_FromThis(),
//                     Projectile.Center + direction * 18f + side * (i * 20f),
//                     direction * 24f,
//                     ModContent.ProjectileType<PhasedGodRay>(),
//                     (int)(Projectile.damage * 0.72f),
//                     Projectile.knockBack * 0.18f,
//                     Projectile.owner,
//                     i * 0.5f);
//             }
// 
//             SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/MagnaCannonShot") { Volume = 0.16f, Pitch = 0.18f + SlotIndex * 0.025f, PitchVariance = 0.08f, MaxInstances = 6 }, Projectile.Center);
//         }
// 
//         private void FireHeavySalvo()
//         {
//             YCAccessoryPlayer accessoryPlayer = Main.player[Projectile.owner].GetModPlayer<YCAccessoryPlayer>();
//             Vector2 direction = CurrentForwardDirection.SafeNormalize(Vector2.UnitX);
//             Vector2 muzzle = Projectile.Center + direction * 18f;
//             Vector2 missileVelocity = direction.RotatedBy(Main.rand.NextFloat(-0.06f, 0.06f)) * 37.5f;
// 
//             Projectile.NewProjectile(
//                 Projectile.GetSource_FromThis(),
//                 muzzle,
//                 missileVelocity,
//                 ModContent.ProjectileType<YC_TyrantPrismMissile>(),
//                 (int)(Projectile.damage * 2.65f * accessoryPlayer.HeavySalvoDamageMultiplier),
//                 Projectile.knockBack * 2.2f,
//                 Projectile.owner,
//                 SlotIndex);
// 
//             EmitHeavyMuzzleFX(muzzle, direction);
// 
//             SoundStyle missileSound = new("CalamityMod/Sounds/Item/MagnaCannonShot");
//             SoundEngine.PlaySound(missileSound with { Volume = 0.22f, Pitch = -0.18f + SlotIndex * 0.02f, PitchVariance = 0.12f }, Projectile.Center);
//         }
// 
//         private void UpdateUltimateAttacks(Player owner, YC_EX_VIP vip)
//         {
//             if (vip.CurrentState != YC_EX_VIP.EXVipState.Firing || Projectile.owner != Main.myPlayer)
//             {
//                 attackTimer = 0;
//                 return;
//             }
// 
//             attackTimer++;
//             if ((attackTimer + SlotIndex * 5) % 28 == 0)
//                 SoundEngine.PlaySound(SoundID.Item15 with { Volume = 0.08f, Pitch = -0.18f + SlotIndex * 0.02f, PitchVariance = 0.06f, MaxInstances = 8 }, Projectile.Center);
//         }
// 
//         private static void EmitHeavyMuzzleFX(Vector2 muzzle, Vector2 direction)
//         {
//             if (Main.dedServ)
//                 return;
// 
//             Color effectColor = new(255, 214, 92);
//             for (int i = 0; i < 7; i++)
//             {
//                 Vector2 dustVelocity = (direction * 10f).RotatedByRandom(0.5f) * Main.rand.NextFloat(0.1f, 1.6f);
//                 Dust dust = Dust.NewDustPerfect(muzzle, Main.rand.NextBool(4) ? DustID.YellowTorch : DustID.GoldFlame, dustVelocity);
//                 dust.scale = Main.rand.NextFloat(1.05f, 1.35f);
//                 dust.noGravity = true;
//                 dust.color = Main.rand.NextBool() ? Color.Lerp(effectColor, Color.White, 0.5f) : effectColor;
//             }
// 
//             GlowSparkParticle pulse = new(
//                 muzzle - direction * 10f,
//                 direction * 20f,
//                 false,
//                 Main.rand.Next(7, 12),
//                 0.045f,
//                 effectColor,
//                 new Vector2(1.5f, 0.9f),
//                 true);
//             GeneralParticleHandler.SpawnParticle(pulse);
//         }
// 
//         private NPC FindTarget(Player owner, float range, bool requireLineOfSight = true)
//         {
//             NPC nearest = null;
//             float maxDistanceSquared = range * range;
// 
//             for (int i = 0; i < Main.maxNPCs; i++)
//             {
//                 NPC npc = Main.npc[i];
//                 if (!npc.CanBeChasedBy(Projectile))
//                     continue;
// 
//                 float distanceSquared = Vector2.DistanceSquared(Projectile.Center, npc.Center);
//                 if (distanceSquared > maxDistanceSquared)
//                     continue;
// 
//                 if (requireLineOfSight && !Collision.CanHitLine(Projectile.Center, 1, 1, npc.Center, 1, 1))
//                     continue;
// 
//                 maxDistanceSquared = distanceSquared;
//                 nearest = npc;
//             }
// 
//             return nearest;
//         }
// 
//         private NPC FindTargetNearMouse(float rangeFromDrone, float rangeFromMouse)
//         {
//             Vector2 mouseWorld = Main.MouseWorld;
//             if (Main.player.IndexInRange(Projectile.owner))
//             {
//                 Vector2 syncedMouse = Main.player[Projectile.owner].Calamity().mouseWorld;
//                 if (syncedMouse != Vector2.Zero)
//                     mouseWorld = syncedMouse;
//             }
// 
//             NPC nearest = null;
//             float bestScore = rangeFromMouse * rangeFromMouse;
// 
//             for (int i = 0; i < Main.maxNPCs; i++)
//             {
//                 NPC npc = Main.npc[i];
//                 if (!npc.CanBeChasedBy(Projectile))
//                     continue;
// 
//                 if (Vector2.DistanceSquared(Projectile.Center, npc.Center) > rangeFromDrone * rangeFromDrone)
//                     continue;
// 
//                 float score = Vector2.DistanceSquared(mouseWorld, npc.Center);
//                 if (score >= bestScore)
//                     continue;
// 
//                 bestScore = score;
//                 nearest = npc;
//             }
// 
//             return nearest;
//         }
// 
//         private void EmitMuzzleBurst(Vector2 direction, int dustCount, float speed)
//         {
//             if (Main.dedServ)
//                 return;
// 
//             for (int i = 0; i < dustCount; i++)
//             {
//                 Dust dust = Dust.NewDustPerfect(
//                     Projectile.Center + direction * 12f,
//                     DustID.GoldFlame,
//                     direction.RotatedByRandom(0.3f) * Main.rand.NextFloat(speed * 0.45f, speed),
//                     0,
//                     Color.Lerp(new Color(255, 199, 92), Color.White, Main.rand.NextFloat(0.18f, 0.58f)),
//                     Main.rand.NextFloat(0.75f, 1.15f));
//                 dust.noGravity = true;
//             }
//         }
// 
//         private void EmitIdleFX(YC_TyrantPrismHoldout holdout)
//         {
//             if (Main.dedServ || Main.GameUpdateCount % (holdout.DroneCombatOnline ? 10 : 5) != 0)
//                 return;
// 
//             Dust dust = Dust.NewDustPerfect(
//                 Projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
//                 DustID.GoldFlame,
//                 CurrentForwardDirection.RotatedByRandom(0.25f) * Main.rand.NextFloat(0.25f, 0.9f),
//                 0,
//                 Color.Lerp(new Color(255, 204, 100), Color.White, Main.rand.NextFloat(0.16f, 0.5f)),
//                 Main.rand.NextFloat(0.55f, 0.9f));
//             dust.noGravity = true;
//         }
// 
//         private void EmitIdleOrbitFX()
//         {
//             if (Main.dedServ || Main.GameUpdateCount % 14 != 0)
//                 return;
// 
//             Dust dust = Dust.NewDustPerfect(
//                 Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
//                 DustID.GoldFlame,
//                 -CurrentForwardDirection * Main.rand.NextFloat(0.2f, 0.6f),
//                 0,
//                 Color.Lerp(new Color(255, 204, 100), Color.White, Main.rand.NextFloat(0.1f, 0.35f)),
//                 Main.rand.NextFloat(0.42f, 0.72f));
//             dust.noGravity = true;
//         }
// 
//         private void EmitUltimateFX(YC_EX_VIP vip)
//         {
//             if (Main.dedServ)
//                 return;
// 
//             float charge = vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge
//                 ? MathHelper.Clamp(vip.CurrentStateTimer / (float)YC_EX_VIP.DroneChargeTime, 0f, 1f)
//                 : 1f;
// 
//             int interval = vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge ? System.Math.Max(2, (int)MathHelper.Lerp(8f, 2f, charge)) : 12;
//             if (Main.GameUpdateCount % interval != 0)
//                 return;
// 
//             Color gold = Color.Lerp(new Color(255, 188, 86), Color.White, charge * 0.5f);
//             GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(
//                 Projectile.Center + Main.rand.NextVector2Circular(8f + charge * 10f, 8f + charge * 10f),
//                 -CurrentForwardDirection * Main.rand.NextFloat(0.4f, 1.8f + charge * 2f),
//                 false,
//                 Main.rand.Next(8, 14),
//                 Main.rand.NextFloat(0.18f, 0.38f) * (0.8f + charge),
//                 gold,
//                 true,
//                 false,
//                 true));
// 
//             if (vip.CurrentState == YC_EX_VIP.EXVipState.DroneCharge && Main.GameUpdateCount % 10 == 0)
//                 GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, CurrentForwardDirection, gold * 0.65f, Vector2.One, Projectile.rotation, 0.02f, MathHelper.Lerp(0.12f, 0.48f, charge), 16));
//         }
// }
// 
// public class YC_TyrantPrismBolt : ModProjectile, ILocalizedModType
// {
//     public new string LocalizationCategory => "Projectiles.YharimsCrystal";
// 
//     public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
// 
//     public ref float Time => ref Projectile.ai[0];
// 
//     public override void SetDefaults()
//     {
//         Projectile.width = 4;
//         Projectile.height = 4;
//         Projectile.friendly = true;
//         Projectile.DamageType = DamageClass.Magic;
//         Projectile.penetrate = 1;
//         Projectile.timeLeft = 600;
//         Projectile.MaxUpdates = 7;
//         Projectile.alpha = 255;
//         Projectile.tileCollide = false;
//         Projectile.ignoreWater = true;
//         Projectile.usesLocalNPCImmunity = true;
//         Projectile.localNPCHitCooldown = 14;
//     }
// 
//     public override void SetStaticDefaults()
//     {
//         ProjectileID.Sets.TrailCacheLength[Type] = 8;
//         ProjectileID.Sets.TrailingMode[Type] = 1;
//     }
// 
//     public override void AI()
//     {
//         Time++;
//         Projectile.rotation = Projectile.velocity.ToRotation();
//         Lighting.AddLight(Projectile.Center, Color.Lerp(Color.Blue, Color.AliceBlue, 0.5f).ToVector3() * 0.49f);
// 
//         if (Projectile.timeLeft == 595)
//             Projectile.alpha = 0;
// 
//         if (Projectile.timeLeft <= 595 && !Main.dedServ)
//         {
//             float positionVariation = 5f;
//             LineParticle spark = new(
//                 Projectile.Center + Main.rand.NextVector2Circular(positionVariation, positionVariation),
//                 -Projectile.velocity * Main.rand.NextFloat(0.3f, 1.1f),
//                 false,
//                 4,
//                 1.45f,
//                 Main.rand.NextBool() ? (Projectile.timeLeft < 570 ? Color.Goldenrod : Color.OrangeRed) : (Projectile.timeLeft > 590 ? Color.Red : Color.DarkGoldenrod));
//             GeneralParticleHandler.SpawnParticle(spark);
//         }
//     }
// 
//     public override bool PreDraw(ref Color lightColor)
//     {
//         CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
//         return false;
//     }
// 
//     public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
//     {
//         target.AddBuff(BuffID.Electrified, 300);
//         target.AddBuff(ModContent.BuffType<Dragonfire>(), 90);
// 
//         if (Main.dedServ)
//             return;
// 
//         for (int i = 0; i < 2; i++)
//         {
//             GenericSparkle sparker = new(
//                 Projectile.Center,
//                 Vector2.Zero,
//                 Color.Gold,
//                 Color.Cyan,
//                 Main.rand.NextFloat(1.8f, 2.5f),
//                 5,
//                 Main.rand.NextFloat(-0.01f, 0.01f),
//                 1.68f);
//             GeneralParticleHandler.SpawnParticle(sparker);
//         }
// 
//         for (int i = 0; i < 2; i++)
//         {
//             Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(target.width, target.height) * 0.04f;
//             Vector2 splatterDirection = (Projectile.Center - spawnPosition).SafeNormalize(Vector2.UnitY);
//             Vector2 sparkVelocity = splatterDirection.RotatedByRandom(0.6f) * Main.rand.NextFloat(10f, 30f);
//             sparkVelocity.Y -= 12f;
// 
//             SparkParticle spark = new(target.Center, sparkVelocity, false, Main.rand.Next(9, 12), Main.rand.NextFloat(0.9f, 1.3f) * 0.85f, Color.Lerp(Color.DarkGoldenrod, Color.Gold, Main.rand.NextFloat(0.7f)));
//             GeneralParticleHandler.SpawnParticle(spark);
//         }
//     }
// 
//     public override void OnKill(int timeLeft)
//     {
//         if (!Main.dedServ)
//         {
//             for (int i = 0; i <= 6; i++)
//             {
//                 Dust dust = Dust.NewDustPerfect(Projectile.Center, DustID.Electric, new Vector2(2, 2).RotatedByRandom(100f) * Main.rand.NextFloat(0.1f, 2.9f));
//                 dust.noGravity = false;
//                 dust.scale = Main.rand.NextFloat(0.3f, 0.9f);
//             }
//         }
// 
//         SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/AuricBulletHit") { Volume = 0.4f }, Projectile.position);
//     }
// }
// 
// public class YC_TyrantPrismLaserLance : ModProjectile, ILocalizedModType
// {
//     public new string LocalizationCategory => "Projectiles.YharimsCrystal";
//     public override string Texture => "CalamityMod/Projectiles/InvisibleProj";
// 
//     private ref float BeamLength => ref Projectile.ai[0];
//     private ref float SlotIndex => ref Projectile.ai[1];
//     private ref float Timer => ref Projectile.localAI[0];
// 
//     public override void SetDefaults()
//     {
//         Projectile.width = 14;
//         Projectile.height = 14;
//         Projectile.friendly = true;
//         Projectile.DamageType = DamageClass.Magic;
//         Projectile.penetrate = -1;
//         Projectile.timeLeft = 14;
//         Projectile.tileCollide = false;
//         Projectile.ignoreWater = true;
//         Projectile.usesLocalNPCImmunity = true;
//         Projectile.localNPCHitCooldown = 8;
//     }
// 
//     public override bool ShouldUpdatePosition() => false;
// 
//     public override void AI()
//     {
//         Timer++;
//         Projectile.velocity = Projectile.velocity.SafeNormalize(Vector2.UnitX);
//         Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
//         if (BeamLength <= 0f)
//             BeamLength = 620f;
// 
//         Lighting.AddLight(Projectile.Center, new Color(255, 224, 112).ToVector3() * 0.72f);
//         if (Main.dedServ || Timer % 3f != 0f)
//             return;
// 
//         Vector2 direction = Projectile.velocity;
//         Vector2 position = Projectile.Center + direction * Main.rand.NextFloat(24f, BeamLength) + direction.RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(-5f, 5f);
//         GlowOrbParticle glow = new(
//             position,
//             -direction * Main.rand.NextFloat(0.15f, 0.55f),
//             false,
//             Main.rand.Next(7, 12),
//             Main.rand.NextFloat(0.16f, 0.28f),
//             Color.Lerp(new Color(255, 204, 92), Color.White, Main.rand.NextFloat(0.18f, 0.52f)),
//             true,
//             false,
//             true);
//         GeneralParticleHandler.SpawnParticle(glow);
//     }
// 
//     public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
//     {
//         float collisionPoint = 0f;
//         return Collision.CheckAABBvLineCollision(
//             targetHitbox.TopLeft(),
//             targetHitbox.Size(),
//             Projectile.Center,
//             Projectile.Center + Projectile.velocity.SafeNormalize(Vector2.UnitX) * BeamLength,
//             12f,
//             ref collisionPoint);
//     }
// 
//     public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
//     {
//         target.AddBuff(ModContent.BuffType<Dragonfire>(), 120);
//     }
// 
//     public override bool PreDraw(ref Color lightColor)
//     {
//         float fadeIn = Utils.GetLerpValue(0f, 3f, Timer, true);
//         float fadeOut = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);
//         float opacity = fadeIn * fadeOut;
//         float pulse = 0.9f + 0.1f * (float)System.Math.Sin(Timer * 0.7f + SlotIndex);
//         YC_YharimBeamVisuals.DrawYharimBeam(Projectile, BeamLength, 0.48f * pulse, opacity, new Color(255, 208, 86));
//         return false;
//     }
// }
// 
// public class YC_TyrantPrismMissile : ModProjectile, ILocalizedModType
// {
//     public new string LocalizationCategory => "Projectiles.YharimsCrystal";
//     public override string Texture => "CalamityMod/Projectiles/Ranged/ThePackMissile";
// 
//     private ref float SlotIndex => ref Projectile.ai[0];
//     private ref float Timer => ref Projectile.localAI[0];
// 
//     public override void SetStaticDefaults()
//     {
//         Main.projFrames[Type] = 9;
//         ProjectileID.Sets.TrailCacheLength[Type] = 8;
//         ProjectileID.Sets.TrailingMode[Type] = 0;
//     }
// 
//     public override void SetDefaults()
//     {
//         Projectile.width = 40;
//         Projectile.height = 40;
//         Projectile.friendly = true;
//         Projectile.DamageType = DamageClass.Magic;
//         Projectile.penetrate = 1;
//         Projectile.timeLeft = 260;
//         Projectile.tileCollide = false;
//         Projectile.ignoreWater = true;
//         Projectile.usesLocalNPCImmunity = true;
//         Projectile.localNPCHitCooldown = -1;
//     }
// 
//     public override void AI()
//     {
//         Timer++;
//         Projectile.rotation = Projectile.velocity.ToRotation();
//         Projectile.frameCounter++;
//         Projectile.frame = Projectile.frameCounter / 4 % Main.projFrames[Type];
// 
//         if (Timer > 18f)
//             HomeTowardTarget();
//         else
//             Projectile.velocity *= 0.99f;
// 
//         Projectile.velocity *= 0.99f;
// 
//         Color glow = Color.Lerp(new Color(255, 196, 72), Color.White, 0.28f);
//         Lighting.AddLight(Projectile.Center, glow.ToVector3() * 0.85f);
//         EmitMissileTrail(glow);
//     }
// 
//     private void HomeTowardTarget()
//     {
//         NPC target = Projectile.Center.ClosestNPCAt(1500f);
//         Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
//         float speed = MathHelper.Clamp(Projectile.velocity.Length(), 36f, 72f);
//         if (target is null)
//         {
//             Projectile.velocity = Vector2.Lerp(Projectile.velocity, currentDirection * MathHelper.Min(speed + 0.08f, 66f), 0.08f);
//             return;
//         }
// 
//         Vector2 predicted = target.Center + target.velocity * MathHelper.Clamp(Projectile.Distance(target.Center) / System.Math.Max(speed, 1f), 6f, 26f);
//         Vector2 desiredDirection = (predicted - Projectile.Center).SafeNormalize(currentDirection);
//         float lockIn = Utils.GetLerpValue(18f, 70f, Timer, true);
//         float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(4f), MathHelper.ToRadians(18f), lockIn);
//         Vector2 newDirection = currentDirection.ToRotation().AngleTowards(desiredDirection.ToRotation(), maxTurn).ToRotationVector2();
//         Projectile.velocity = Vector2.Lerp(Projectile.velocity, newDirection * MathHelper.Lerp(speed, 69f, 0.12f), 0.12f + lockIn * 0.08f);
//     }
// 
//     private void EmitMissileTrail(Color glow)
//     {
//         if (Main.dedServ)
//             return;
// 
//         Vector2 direction = Projectile.velocity.SafeNormalize(Vector2.UnitX);
//         Vector2 normal = direction.RotatedBy(MathHelper.PiOver2);
//         if (Main.rand.NextBool(2))
//         {
//             Dust dust = Dust.NewDustPerfect(
//                 Projectile.Center - direction * Main.rand.NextFloat(16f, 34f) + normal * Main.rand.NextFloat(-5f, 5f),
//                 Main.rand.NextBool(3) ? DustID.YellowTorch : DustID.GoldFlame,
//                 -direction * Main.rand.NextFloat(0.9f, 2.8f) + normal * Main.rand.NextFloat(-0.35f, 0.35f),
//                 0,
//                 glow,
//                 Main.rand.NextFloat(0.78f, 1.25f));
//             dust.noGravity = true;
//         }
// 
//         if (Timer % 3f == 0f)
//         {
//             GeneralParticleHandler.SpawnParticle(new CustomSpark(
//                 Projectile.Center - direction * 24f + normal * Main.rand.NextFloat(-7f, 7f),
//                 -direction * Main.rand.NextFloat(0.5f, 1.6f),
//                 "CalamityMod/Particles/BloomCircle",
//                 false,
//                 Main.rand.Next(10, 16),
//                 Main.rand.NextFloat(0.24f, 0.42f),
//                 Main.rand.NextBool(4) ? Color.White : glow,
//                 new Vector2(0.28f, 1.5f),
//                 true,
//                 true,
//                 extraRotation: -MathHelper.PiOver2,
//                 shrinkSpeed: 0.28f,
//                 glowOpacity: 0.78f));
//         }
//     }
// 
//     public override void OnKill(int timeLeft)
//     {
//         if (Projectile.owner == Main.myPlayer)
//         {
//             int oldWidth = Projectile.width;
//             int oldHeight = Projectile.height;
//             Vector2 center = Projectile.Center;
//             Projectile.width = Projectile.height = 92;
//             Projectile.Center = center;
//             Projectile.penetrate = -1;
//             Projectile.Damage();
//             Projectile.width = oldWidth;
//             Projectile.height = oldHeight;
//             Projectile.Center = center;
//         }
// 
//         if (Main.dedServ)
//             return;
// 
//         Color gold = new(255, 205, 78);
//         SoundEngine.PlaySound(new SoundStyle("CalamityMod/Sounds/Item/DeadSunExplosion") { Volume = 0.32f, Pitch = -0.15f, PitchVariance = 0.12f }, Projectile.Center);
//         GeneralParticleHandler.SpawnParticle(new CustomPulse(Projectile.Center, Vector2.Zero, gold * 0.78f, "CalamityMod/Particles/FlameExplosion", Vector2.One, Main.rand.NextFloat(-10f, 10f), 0.05f, 0.28f, 18, true));
//         GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero, Color.Lerp(gold, Color.White, 0.24f), Vector2.One, Projectile.rotation, 0.14f, 2.36f, 22));
// 
//         for (int i = 0; i < 56; i++)
//         {
//             Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.8f, 10.5f);
//             Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(4) ? DustID.YellowTorch : DustID.GoldFlame, velocity, 0, Main.rand.NextBool(4) ? Color.White : gold, Main.rand.NextFloat(0.9f, 1.65f));
//             dust.noGravity = true;
//         }
// 
//         for (int i = 0; i < 24; i++)
//         {
//             Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 7.8f);
//             GeneralParticleHandler.SpawnParticle(new SparkParticle(Projectile.Center, velocity, false, Main.rand.Next(16, 28), Main.rand.NextFloat(0.7f, 1.3f), Color.Lerp(gold, Color.White, Main.rand.NextFloat(0.1f, 0.45f))));
//         }
//     }
// 
//     public override bool PreDraw(ref Color lightColor)
//     {
//         Texture2D texture = TextureAssets.Projectile[Type].Value;
//         Rectangle frame = texture.Frame(verticalFrames: Main.projFrames[Type], frameY: Projectile.frame);
//         Vector2 origin = frame.Size() * 0.5f;
//         Color afterimageColor = new Color(255, 205, 86, 0) * 0.5f;
// 
//         CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], afterimageColor, 1);
//         Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, 0.48f), Projectile.rotation, origin, Projectile.scale, SpriteEffects.None, 0f);
//         return false;
//     }
// }
// 
// public class YC_TyrantPrismMainBeam : ModProjectile, ILocalizedModType
//     {
//         private const float MaxBeamLength = 2600f;
//         private const int SampleCount = 3;
// 
//         public new string LocalizationCategory => "Projectiles.YharimsCrystal";
//         public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";
// 
//         private int HoldoutIndex => (int)Projectile.ai[0];
//         private ref float BeamLength => ref Projectile.localAI[0];
//         private ref float Timer => ref Projectile.localAI[1];
// 
//         public override void SetStaticDefaults()
//         {
//             ProjectileID.Sets.DrawScreenCheckFluff[Type] = 10000;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 18;
//             Projectile.height = 18;
//             Projectile.friendly = true;
//             Projectile.hostile = false;
//             Projectile.penetrate = -1;
//             Projectile.tileCollide = false;
//             Projectile.ignoreWater = true;
//             Projectile.hide = true;
//             Projectile.DamageType = DamageClass.Magic;
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = 6;
//             Projectile.timeLeft = 2;
//         }
// 
//         public override bool ShouldUpdatePosition() => false;
// 
//         public override bool? CanDamage()
//         {
//             return TryGetHoldout(out _, out YC_TyrantPrismHoldout holdout) && holdout.MainBeamCanDamage ? null : false;
//         }
// 
//         public override void DrawBehind(
//             int index,
//             System.Collections.Generic.List<int> behindNPCsAndTiles,
//             System.Collections.Generic.List<int> behindNPCs,
//             System.Collections.Generic.List<int> behindProjectiles,
//             System.Collections.Generic.List<int> overPlayers,
//             System.Collections.Generic.List<int> overWiresUI)
//         {
//             overPlayers.Add(index);
//         }
// 
//         public override void AI()
//         {
//             if (!TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout))
//             {
//                 Projectile.Kill();
//                 return;
//             }
// 
//             Timer++;
//             Projectile.timeLeft = 2;
//             Projectile.Center = holdout.MainMuzzle;
//             Projectile.velocity = holdout.ForwardDirection;
//             Projectile.rotation = Projectile.velocity.ToRotation();
//             Projectile.scale = MathHelper.Lerp(0.42f, 1.65f, holdout.MainBeamStrength);
//             Projectile.damage = (int)(holdoutProjectile.damage * MathHelper.Lerp(0.82f, 2.35f, holdout.MainBeamStrength));
// 
//             UpdateBeamLength();
//             EmitBeamFX(holdout);
//             if (BeamLength > 0f && Projectile.scale > 0f)
//             {
//                 DelegateMethods.v3_1 = new Color(255, 214, 95).ToVector3() * (0.28f + holdout.MainBeamStrength * 0.42f);
//                 YC_BeamWorldSafety.SafePlotTileLine(
//                     Projectile.Center,
//                     Projectile.Center + Projectile.velocity * BeamLength,
//                     24f * Projectile.scale,
//                     DelegateMethods.CastLight);
//             }
//         }
// 
//         public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
//         {
//             if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
//                 return false;
// 
//             float collisionPoint = 0f;
//             float width = MathHelper.Lerp(10f, 42f, Projectile.scale / 1.65f);
//             return Collision.CheckAABBvLineCollision(
//                 targetHitbox.TopLeft(),
//                 targetHitbox.Size(),
//                 Projectile.Center,
//                 Projectile.Center + Projectile.velocity * BeamLength,
//                 width,
//                 ref collisionPoint);
//         }
// 
//         public override void CutTiles()
//         {
//             if (Projectile.velocity == Vector2.Zero)
//                 return;
// 
//             DelegateMethods.tilecut_0 = TileCuttingContext.AttackProjectile;
//             YC_BeamWorldSafety.SafePlotTileLine(
//                 Projectile.Center,
//                 Projectile.Center + Projectile.velocity * BeamLength,
//                 30f * Projectile.scale,
//                 DelegateMethods.CutTiles);
//         }
// 
//         public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
//         {
//             target.AddBuff(ModContent.BuffType<Dragonfire>(), 180);
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             if (!TryGetHoldout(out _, out YC_TyrantPrismHoldout holdout) || Projectile.velocity == Vector2.Zero || BeamLength <= 0f)
//                 return false;
// 
//             float strength = holdout.MainBeamStrength;
//             float opacity = Utils.GetLerpValue(0f, 0.08f, strength, true);
//             float pulse = 1f + 0.055f * (float)System.Math.Sin(Timer * 0.21f);
//             Color gold = Color.Lerp(new Color(255, 182, 76), new Color(255, 234, 150), strength);
// 
//             YC_YharimBeamVisuals.DrawYharimBeam(Projectile, BeamLength, Projectile.scale * 0.42f * pulse, opacity, gold);
//             return false;
//         }
// 
//         private bool TryGetHoldout(out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout)
//         {
//             holdoutProjectile = null;
//             holdout = null;
// 
//             if (HoldoutIndex < 0 || HoldoutIndex >= Main.maxProjectiles)
//                 return false;
// 
//             Projectile candidate = Main.projectile[HoldoutIndex];
//             if (!candidate.active ||
//                 candidate.owner != Projectile.owner ||
//                 candidate.type != ModContent.ProjectileType<YC_TyrantPrismHoldout>() ||
//                 candidate.ModProjectile is not YC_TyrantPrismHoldout holdoutMod)
//             {
//                 return false;
//             }
// 
//             holdoutProjectile = candidate;
//             holdout = holdoutMod;
//             return true;
//         }
// 
//         private void UpdateBeamLength()
//         {
//             float[] samples = new float[SampleCount];
//             if (!YC_BeamWorldSafety.TryLaserScan(Projectile.Center, Projectile.velocity, 3f * Projectile.scale, MaxBeamLength, samples))
//             {
//                 BeamLength = 0f;
//                 return;
//             }
// 
//             float average = 0f;
//             for (int i = 0; i < samples.Length; i++)
//                 average += samples[i];
// 
//             average /= samples.Length;
//             if (average <= 0f)
//                 average = MaxBeamLength;
// 
//             BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.66f);
//         }
// 
//         private void EmitBeamFX(YC_TyrantPrismHoldout holdout)
//         {
//             if (Main.dedServ || holdout.MainBeamStrength < 0.12f || Main.GameUpdateCount % 5 != 0)
//                 return;
// 
//             YC_YharimBeamVisuals.EmitYharimBeamDust(Projectile, BeamLength, Projectile.scale * 0.42f, Color.Lerp(new Color(255, 200, 88), Color.White, Main.rand.NextFloat(0.2f, 0.58f)));
//         }
//     }
// 
//     public class YC_TyrantPrismConvergeBeam : ModProjectile, ILocalizedModType
//     {
//         private const float MaxBeamLength = 1850f;
// 
//         public new string LocalizationCategory => "Projectiles.YharimsCrystal";
//         public override string Texture => "CalamityMod/Projectiles/Magic/YharimsCrystalBeam";
// 
//         private int DroneIndex => (int)Projectile.ai[0];
//         private int HoldoutIndex => (int)Projectile.ai[1];
//         private ref float BeamLength => ref Projectile.localAI[0];
//         private ref float Timer => ref Projectile.localAI[1];
// 
//         public override void SetStaticDefaults()
//         {
//             ProjectileID.Sets.DrawScreenCheckFluff[Type] = 8000;
//         }
// 
//         public override void SetDefaults()
//         {
//             Projectile.width = 10;
//             Projectile.height = 10;
//             Projectile.friendly = true;
//             Projectile.hostile = false;
//             Projectile.penetrate = -1;
//             Projectile.tileCollide = false;
//             Projectile.ignoreWater = true;
//             Projectile.hide = true;
//             Projectile.DamageType = DamageClass.Magic;
//             Projectile.usesLocalNPCImmunity = true;
//             Projectile.localNPCHitCooldown = 8;
//             Projectile.timeLeft = 2;
//         }
// 
//         public override bool ShouldUpdatePosition() => false;
// 
//         public override bool? CanDamage()
//         {
//             if (!TryGetSources(out _, out _, out YC_TyrantPrismHoldout holdout))
//                 return false;
// 
//             return holdout.HoldFrames > 18f && holdout.MainBeamStrength < 0.84f ? null : false;
//         }
// 
//         public override void AI()
//         {
//             if (!TryGetSources(out Projectile droneProjectile, out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout) ||
//                 holdout.CurrentState != YC_TyrantPrismHoldout.TyrantPrismState.Converging)
//             {
//                 Projectile.Kill();
//                 return;
//             }
// 
//             Timer++;
//             Projectile.timeLeft = 2;
//             Vector2 focusOffset = holdout.ForwardDirection.RotatedBy(MathHelper.PiOver2) *
//                 ((DroneIndex - (YC_TyrantPrismHoldout.DroneCount - 1f) * 0.5f) * MathHelper.Lerp(72f, 0f, holdout.ConvergenceRatio));
//             Vector2 targetPoint = holdout.BeamFocusPoint + focusOffset;
//             Vector2 direction = (targetPoint - droneProjectile.Center).SafeNormalize(holdout.ForwardDirection);
// 
//             Projectile.Center = droneProjectile.Center + direction * 14f;
//             Projectile.velocity = direction;
//             Projectile.rotation = direction.ToRotation();
//             Projectile.scale = MathHelper.Lerp(0.72f, 1.2f, holdout.ConvergenceRatio) * Utils.GetLerpValue(0.98f, 0.42f, holdout.MainBeamStrength, true);
//             Projectile.damage = (int)(holdoutProjectile.damage * MathHelper.Lerp(0.35f, 0.52f, holdout.ConvergenceRatio));
// 
//             UpdateBeamLength();
//             Lighting.AddLight(Projectile.Center, new Color(255, 206, 104).ToVector3() * (0.18f + Projectile.scale * 0.2f));
//         }
// 
//         public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
//         {
//             if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f || Projectile.scale <= 0.08f)
//                 return false;
// 
//             float collisionPoint = 0f;
//             return Collision.CheckAABBvLineCollision(
//                 targetHitbox.TopLeft(),
//                 targetHitbox.Size(),
//                 Projectile.Center,
//                 Projectile.Center + Projectile.velocity * BeamLength,
//                 18f * Projectile.scale,
//                 ref collisionPoint);
//         }
// 
//         public override bool PreDraw(ref Color lightColor)
//         {
//             if (Projectile.velocity == Vector2.Zero || BeamLength <= 0f || Projectile.scale <= 0.08f)
//                 return false;
// 
//             float opacity = MathHelper.Clamp(Projectile.scale / 1.2f, 0f, 1f);
//             Color gold = Color.Lerp(new Color(255, 166, 78), new Color(255, 238, 172), 0.35f);
// 
//             YC_YharimBeamVisuals.DrawYharimBeam(Projectile, BeamLength, Projectile.scale * 0.4f, opacity, gold);
//             return false;
//         }
// 
//         private bool TryGetSources(out Projectile droneProjectile, out Projectile holdoutProjectile, out YC_TyrantPrismHoldout holdout)
//         {
//             droneProjectile = null;
//             holdoutProjectile = null;
//             holdout = null;
// 
//             if (DroneIndex < 0 || DroneIndex >= Main.maxProjectiles || HoldoutIndex < 0 || HoldoutIndex >= Main.maxProjectiles)
//                 return false;
// 
//             Projectile drone = Main.projectile[DroneIndex];
//             Projectile candidateHoldout = Main.projectile[HoldoutIndex];
// 
//             if (!drone.active ||
//                 drone.owner != Projectile.owner ||
//                 drone.type != ModContent.ProjectileType<YC_TyrantPrismDrone>() ||
//                 !candidateHoldout.active ||
//                 candidateHoldout.owner != Projectile.owner ||
//                 candidateHoldout.type != ModContent.ProjectileType<YC_TyrantPrismHoldout>() ||
//                 candidateHoldout.ModProjectile is not YC_TyrantPrismHoldout holdoutMod)
//             {
//                 return false;
//             }
// 
//             droneProjectile = drone;
//             holdoutProjectile = candidateHoldout;
//             holdout = holdoutMod;
//             return true;
//         }
// 
//         private void UpdateBeamLength()
//         {
//             float[] samples = new float[3];
//             if (!YC_BeamWorldSafety.TryLaserScan(Projectile.Center, Projectile.velocity, 2f * Projectile.scale, MaxBeamLength, samples))
//             {
//                 BeamLength = 0f;
//                 return;
//             }
// 
//             float average = 0f;
//             for (int i = 0; i < samples.Length; i++)
//                 average += samples[i];
// 
//             average /= samples.Length;
//             if (average <= 0f)
//                 average = MaxBeamLength;
// 
//             BeamLength = MathHelper.Lerp(BeamLength <= 0f ? average : BeamLength, average, 0.72f);
//         }
//     }
// 
// }
// 
// */
// 