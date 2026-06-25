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
    public class NewLegendGaelsGreatsword : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private const int FollowupSlashWindow = 45;
        private const float PlungeDamageMultiplier = 1.9f;
        private const int FinisherCooldown = 18 * 60;

        private static int SwingHoldoutType => ModContent.ProjectileType<GaelGreatswordSwingHoldout>();
        private static int PlungeHoldoutType => ModContent.ProjectileType<GaelGreatswordPlungeHoldout>();
        private static int CapeFinisherType => ModContent.ProjectileType<GaelGreatswordCapeFinisher>();

        public override void SetDefaults()
        {
            Item.width = 86;
            Item.height = 86;
            Item.damage = GaelGreatswordProgression.GetBaseDamage();
            Item.DamageType = DamageClass.Melee;
            Item.knockBack = 7f;
            Item.useTime = 23;
            Item.useAnimation = 23;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.shoot = SwingHoldoutType;
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Orange;
            Item.value = Item.sellPrice(gold: 8);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.ownedProjectileCounts[SwingHoldoutType] > 0 ||
                player.ownedProjectileCounts[PlungeHoldoutType] > 0 ||
                player.ownedProjectileCounts[CapeFinisherType] > 0)
            {
                return false;
            }

            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.shootSpeed = 0f;
            Item.UseSound = null;

            if (player.altFunctionUse == 2)
            {
                Item.channel = false;
                Item.autoReuse = false;
                Item.useTime = 31;
                Item.useAnimation = 31;
                Item.shoot = PlungeHoldoutType;
                return base.CanUseItem(player);
            }

            Item.channel = true;
            Item.autoReuse = true;
            Item.useTime = Math.Max(12, GaelGreatswordProgression.GetLeftUseTime(player));
            Item.useAnimation = Item.useTime;
            Item.shoot = SwingHoldoutType;
            return base.CanUseItem(player);
        }

        public override bool CanShoot(Player player)
        {
            return player.ownedProjectileCounts[SwingHoldoutType] <= 0 &&
                player.ownedProjectileCounts[PlungeHoldoutType] <= 0 &&
                player.ownedProjectileCounts[CapeFinisherType] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

            if (player.altFunctionUse == 2)
            {
                int plungeDamage = (int)(damage * PlungeDamageMultiplier);
                Projectile.NewProjectile(source, player.MountedCenter, aimDirection, PlungeHoldoutType, plungeDamage, knockback + 2f, player.whoAmI);
                return false;
            }

            GaelGreatswordPlayer gaelPlayer = player.GetModPlayer<GaelGreatswordPlayer>();
            bool followupSlash = gaelPlayer.ConsumeFollowupSlash();
            Projectile.NewProjectile(source, player.MountedCenter, aimDirection, SwingHoldoutType, damage, knockback, player.whoAmI, followupSlash ? 1f : 0f);
            return false;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            GaelGreatswordPlayer gaelPlayer = player.GetModPlayer<GaelGreatswordPlayer>();
            gaelPlayer.ApplyHeldEffects();

            if (Main.myPlayer != player.whoAmI || !GaelGreatswordRageInterop.RageHotKeyJustPressed())
                return;

            TryActivateFinisher(player, gaelPlayer);
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += GaelGreatswordProgression.GetBaseDamage() - Item.damage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string rageKeyText = GaelGreatswordRageInterop.GetRageKeyText();
            string text = string.Format(this.GetLocalizedValue("FunctionalTooltip"), rageKeyText);
            tooltips.FindAndReplace("[GFB]", text);

            bool shiftPressed = Main.keyState.PressingShift();
            string legendarySection = shiftPressed
                ? this.GetLocalizedValue("LegendaryText")
                : this.GetLocalizedValue("LegendaryHint");
            tooltips.Add(new TooltipLine(Mod, "GaelDarkSoulLegendaryText", legendarySection));
        }

        private void TryActivateFinisher(Player player, GaelGreatswordPlayer gaelPlayer)
        {
            if (!Main.hardMode)
            {
                CombatText.NewText(player.Hitbox, Color.Gray, Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendGaelsGreatsword.FinisherLocked"));
                return;
            }

            if (gaelPlayer.FinisherCooldown > 0 ||
                player.ownedProjectileCounts[CapeFinisherType] > 0 ||
                player.noItems ||
                player.CCed)
            {
                return;
            }

            int damage = (int)(player.GetWeaponDamage(Item) * 0.8f);
            Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, CapeFinisherType, damage, Item.knockBack, player.whoAmI);
            gaelPlayer.FinisherCooldown = FinisherCooldown;

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.55f }, player.Center);
            SoundEngine.PlaySound(SoundID.Roar with { Volume = 0.55f, Pitch = -0.2f }, player.Center);
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }

    internal sealed class GaelGreatswordPlayer : ModPlayer
    {
        public int FollowupSlashWindow;
        public int FinisherCooldown;

        private int holdingTimer;
        private int blackBloodAfterglowTimer;
        private int darkSoulFlightTime;
        private int blackBloodLifeCostCooldown;

        public bool HoldingGael => holdingTimer > 0;
        public bool BlackBloodActive => blackBloodAfterglowTimer > 0;
        public bool BlackHumanityActive => HoldingGael && Player.statLife <= Player.statLifeMax2 / 2;

        public override void PreUpdate()
        {
            if (holdingTimer > 0)
                holdingTimer--;
            if (FollowupSlashWindow > 0)
                FollowupSlashWindow--;
            if (FinisherCooldown > 0)
                FinisherCooldown--;
            if (blackBloodAfterglowTimer > 0)
                blackBloodAfterglowTimer--;
            if (blackBloodLifeCostCooldown > 0)
                blackBloodLifeCostCooldown--;
        }

        public bool ConsumeFollowupSlash()
        {
            if (FollowupSlashWindow <= 0)
                return false;

            FollowupSlashWindow = 0;
            return true;
        }

        public void ApplyHeldEffects()
        {
            holdingTimer = 2;
            ApplyDarkSoulPassive();
            ApplyBloodAndFire();
            ApplyBlackHumanity();
        }

        public float TryPayBlackBloodCost()
        {
            if (!BlackBloodActive || blackBloodLifeCostCooldown > 0 || Player.statLife <= 1)
                return 1f;

            int cost = Math.Max(4, (int)MathF.Ceiling(Player.statLifeMax2 * 0.035f));
            Player.statLife = Math.Max(1, Player.statLife - cost);
            blackBloodLifeCostCooldown = 6;
            blackBloodAfterglowTimer = Math.Max(blackBloodAfterglowTimer, 180);

            CombatText.NewText(Player.Hitbox, new Color(160, 18, 34), cost, true);
            SpawnBlackBloodPaidEffects();
            return 1.32f + GaelGreatswordProgression.GetStage() * 0.03f;
        }

        private void ApplyDarkSoulPassive()
        {
            Player.slowFall = true;
            Player.noFallDmg = true;

            // After the Wall of Flesh, Gael no longer passively feeds Calamity's Rage bar.
            // From that point onward, Black Humanity is the only Rage-feeding route.
            if (!Main.hardMode)
                GaelGreatswordRageInterop.AddRage(Player, GaelGreatswordProgression.GetPassiveRagePerFrame());

            HandleDarkSoulFlight();
        }

        private void ApplyBloodAndFire()
        {
            bool belowHalfLife = Player.statLife <= Player.statLifeMax2 / 2;
            bool rageHigh = GaelGreatswordRageInterop.GetRageRatio(Player) >= 0.75f;

            if (belowHalfLife || rageHigh)
                blackBloodAfterglowTimer = 180;

            if (blackBloodAfterglowTimer > 0)
                Player.AddBuff(ModContent.BuffType<GaelGreatswordBlackBlood>(), 2);
        }

        private void ApplyBlackHumanity()
        {
            if (!BlackHumanityActive)
                return;

            Player.GetAttackSpeed(DamageClass.Melee) += 0.38f;
            GaelGreatswordRageInterop.AddRage(Player, Main.hardMode ? 0.18f : 0.12f);

            if (Main.rand.NextBool(5))
            {
                Vector2 velocity = new Vector2(Main.rand.NextFloat(-1.4f, 1.4f), Main.rand.NextFloat(-2.2f, -0.3f));
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(24f, 34f), DustID.Blood, velocity, 130, new Color(120, 0, 20), Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }
        }

        private void HandleDarkSoulFlight()
        {
            int maxFlightTime = GaelGreatswordProgression.GetFlightFrames();
            if (maxFlightTime <= 0)
                return;

            bool grounded = Player.velocity.Y == 0f || Player.sliding || Player.mount.Active;
            if (grounded)
                darkSoulFlightTime = maxFlightTime;

            if (!Player.controlJump || darkSoulFlightTime <= 0 || Player.mount.Active || Player.grappling[0] >= 0)
                return;

            darkSoulFlightTime--;
            Player.velocity.Y -= GaelGreatswordProgression.GetFlightAcceleration();
            Player.velocity.Y = Math.Max(Player.velocity.Y, -GaelGreatswordProgression.GetFlightTopSpeed());
            Player.wingTime = Math.Max(Player.wingTime, 2f);

            if (Main.rand.NextBool(3))
            {
                Vector2 position = Player.Bottom + Main.rand.NextVector2Circular(18f, 4f);
                Dust dust = Dust.NewDustPerfect(position, DustID.Shadowflame, new Vector2(Main.rand.NextFloat(-1.1f, 1.1f), Main.rand.NextFloat(1.4f, 3.2f)), 120, new Color(80, 40, 120), 1.1f);
                dust.noGravity = true;
            }
        }

        private void SpawnBlackBloodPaidEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 12; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.4f);
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(18f, 28f), DustID.Blood, velocity, 80, new Color(145, 0, 26), Main.rand.NextFloat(1.1f, 1.7f));
                dust.noGravity = Main.rand.NextBool();
            }
        }
    }

    public class GaelGreatswordBlackBlood : ModBuff, ILocalizedModType
    {
        public new string LocalizationCategory => "Buffs";
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/GaelGreatswordBlackBlood";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.buffNoSave[Type] = true;
            Main.buffNoTimeDisplay[Type] = false;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            player.lifeRegen -= 2;
        }
    }

    internal static class GaelGreatswordProgression
    {
        public static int GetStage()
        {
            int stage = 0;
            if (NPC.downedBoss1)
                stage++;
            if (NPC.downedBoss2 || NPC.downedQueenBee)
                stage++;
            if (NPC.downedBoss3)
                stage++;
            if (Main.hardMode)
                stage += 2;
            return Math.Min(stage, 5);
        }

        public static int GetBaseDamage()
        {
            return GetStage() switch
            {
                0 => 28,
                1 => 35,
                2 => 43,
                3 => 52,
                4 => 66,
                _ => 78,
            };
        }

        public static int GetLeftUseTime(Player player)
        {
            float speed = Math.Max(0.5f, player.GetAttackSpeed(DamageClass.Melee));
            int raw = (int)MathF.Round(23f / speed);
            return Math.Max(12, raw);
        }

        public static int GetSwingDuration(Player player, bool followupSlash)
        {
            float baseDuration = followupSlash ? 19f : 23f;
            float speed = Math.Max(0.5f, player.GetAttackSpeed(DamageClass.Melee));
            return Math.Max(followupSlash ? 12 : 14, (int)MathF.Round(baseDuration / speed));
        }

        public static int GetRepeatHitCooldown()
        {
            return GetStage() switch
            {
                0 => 22,
                1 => 18,
                2 => 15,
                3 => 12,
                _ => 9,
            };
        }

        public static float GetPassiveRagePerFrame()
        {
            return GetStage() switch
            {
                0 => 0.035f,
                1 => 0.045f,
                2 => 0.055f,
                3 => 0.065f,
                _ => 0.075f,
            };
        }

        public static int GetFlightFrames()
        {
            if (Main.hardMode)
                return 120;
            if (NPC.downedBoss3)
                return 82;
            if (NPC.downedBoss2 || NPC.downedQueenBee)
                return 56;
            if (NPC.downedBoss1)
                return 32;
            return 0;
        }

        public static float GetFlightAcceleration()
        {
            return Main.hardMode ? 0.42f : NPC.downedBoss3 ? 0.34f : 0.25f;
        }

        public static float GetFlightTopSpeed()
        {
            return Main.hardMode ? 6.4f : NPC.downedBoss3 ? 5.2f : 4.1f;
        }
    }

    internal sealed class GaelGreatswordSwingHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const float SwordVisualScale = 1.42f;
        private const float BladeReach = 130f;
        private const int TrailHistoryFrames = 18;

        private static readonly Color DarkPurple = new(70, 24, 118);
        private static readonly Color BloodRed = new(160, 12, 36);
        private static readonly Color PaleCore = new(222, 205, 245);

        private readonly Vector2[] bladeTipHistory = new Vector2[TrailHistoryFrames];
        private Player Owner => Main.player[Projectile.owner];
        private bool FollowupSlash => Projectile.ai[0] == 1f;

        private int bladeTipHistoryLength;
        private int swingTimer;
        private int swingCount;
        private int swingDirection = 1;
        private bool bloodCostChecked;
        private float bloodDamageMultiplier = 1f;
        private float scale = 1f;
        private float currentAngle;
        private float startAngle;
        private float windupAngle;
        private float endAngle;
        private float slashOpacity;
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

        private void DoSwing()
        {
            if (swingTimer == 0)
                StartSwing();

            swingTimer++;
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

            if (swingTimer < GaelGreatswordProgression.GetSwingDuration(Owner, FollowupSlash))
                return;

            if (!FollowupSlash && IsLeftHeld())
            {
                swingCount++;
                Projectile.ResetLocalNPCHitImmunity();
                swingTimer = 0;
                bloodCostChecked = false;
                bloodDamageMultiplier = 1f;
                return;
            }

            Projectile.Kill();
        }

        private void StartSwing()
        {
            swingTimer = 0;
            bloodCostChecked = false;
            bloodDamageMultiplier = 1f;
            lockedDirection = GetMouseDirection();

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
            int particleCount = FollowupSlash ? 4 : 2;

            for (int i = 0; i < particleCount; i++)
            {
                Vector2 position = Owner.MountedCenter + direction * Main.rand.NextFloat(42f, BladeReach) * scale;
                Vector2 velocity = perpendicular * Main.rand.NextFloat(1.4f, FollowupSlash ? 6.2f : 4.4f);
                Color color = Main.rand.NextBool(3) ? BloodRed : DarkPurple;
                GeneralParticleHandler.SpawnParticle(new LineParticle(position, velocity, false,
                    Main.rand.Next(10, 18), Main.rand.NextFloat(0.36f, 0.7f), color));
            }

            if (bloodDamageMultiplier > 1f && Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(Owner.MountedCenter + direction * Main.rand.NextFloat(54f, BladeReach) * scale,
                    DustID.Blood, perpendicular * Main.rand.NextFloat(1f, 3.5f), 80, BloodRed, 1.2f);
                dust.noGravity = true;
            }
        }

        private void DrawBladeTrail()
        {
            if (bladeTipHistoryLength < 2 || slashOpacity <= 0.01f || Main.dedServ)
                return;

            Main.spriteBatch.EnterShaderRegion();
            var slashShader = GameShaders.Misc["CalamityMod:ExobladeSlash"];
            slashShader.SetShaderTexture(ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleGradients/VoronoiShapes"));
            slashShader.UseColor(bloodDamageMultiplier > 1f ? BloodRed : DarkPurple);
            slashShader.UseSecondaryColor(new Color(35, 0, 55));
            slashShader.Shader.Parameters["fireColor"].SetValue(PaleCore.ToVector3());
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
                Color slashColor = bloodDamageMultiplier > 1f ? BloodRed : DarkPurple;
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                float swipeDirection = Math.Sign(endAngle - startAngle);
                Main.EntitySpriteDraw(swooshTexture, drawPosition, null, slashColor with { A = 0 } * slashOpacity * (FollowupSlash ? 0.62f : 0.46f),
                    drawRotation + MathHelper.PiOver2 * swipeDirection, swooshTexture.Size() * 0.5f,
                    scale * (FollowupSlash ? 0.9f : 0.78f), SpriteEffects.None);

                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 3.1f * slashOpacity;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, slashColor with { A = 0 } * slashOpacity * 0.07f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                Vector2 bladeTip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale - Main.screenPosition;
                Main.EntitySpriteDraw(bloomTexture, bladeTip, null, PaleCore with { A = 0 } * slashOpacity * 0.48f,
                    0f, bloomTexture.Size() * 0.5f, scale * 0.44f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            DrawBladeTrail();
            return false;
        }
    }

    internal sealed class GaelGreatswordPlungeHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const int Duration = 36;
        private const float SwordVisualScale = 1.56f;
        private const float BladeReach = 142f;

        private static readonly Color DarkPurple = new(60, 20, 100);
        private static readonly Color BloodRed = new(175, 10, 30);

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private bool dashStarted;
        private bool impactEffectsPlayed;
        private float currentAngle;
        private float startAngle;
        private float endAngle;
        private float scale = 1f;
        private Vector2 lockedDirection = Vector2.UnitX;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 14;
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

            scale = Owner.GetMeleeScale() * SwordVisualScale;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.Center = Owner.MountedCenter;
            Projectile.timeLeft = 4;

            if (!dashStarted)
                StartPlunge();

            timer++;
            float progress = MathHelper.Clamp(timer / (float)Duration, 0f, 1f);
            currentAngle = MathHelper.Lerp(startAngle, endAngle, CalamityUtils.EaseInOutExp(progress, 6f, 2f));

            if (!impactEffectsPlayed && progress >= 0.52f)
            {
                impactEffectsPlayed = true;
                SpawnImpactEffects();
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.78f, Pitch = -0.38f }, Owner.Center);
            }

            float armAngle = currentAngle - MathHelper.ToRadians(130f);
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, armAngle);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = currentAngle;

            if (timer >= Duration)
            {
                Owner.GetModPlayer<GaelGreatswordPlayer>().FollowupSlashWindow = 45;
                Projectile.Kill();
            }
        }

        public override bool? CanDamage()
        {
            float progress = timer / (float)Duration;
            return progress >= 0.28f && progress <= 0.92f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 bladeTip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Owner.MountedCenter, bladeTip, 34f * scale, ref collisionPoint) ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.55f, MathHelper.Clamp(Projectile.numHits / 5f, 0f, 1f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Owner.GetModPlayer<GaelGreatswordPlayer>().FollowupSlashWindow = 45;
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 4.5f);

            if (Main.myPlayer == Projectile.owner)
            {
                int soulDamage = Math.Max(1, (int)(Projectile.damage * 0.22f));
                for (int i = 0; i < 2; i++)
                {
                    Vector2 spawnPosition = target.Center + Main.rand.NextVector2Circular(120f, 100f);
                    Vector2 velocity = spawnPosition.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(7f, 10f);
                    Projectile.NewProjectile(Projectile.GetSource_OnHit(target), spawnPosition, velocity,
                        ModContent.ProjectileType<GaelGreatswordDarkSoul>(), soulDamage, 1f, Projectile.owner, target.whoAmI);
                }
            }
        }

        private void StartPlunge()
        {
            dashStarted = true;
            lockedDirection = Projectile.velocity.SafeNormalize(Owner.MountedCenter.DirectionTo(NewLegendGaelsGreatsword.GetMouseWorld(Owner)));
            Owner.direction = Math.Sign(lockedDirection.X);
            if (Owner.direction == 0)
                Owner.direction = 1;

            Owner.velocity = lockedDirection * 12.5f + new Vector2(0f, -4.8f);
            Owner.fallStart = (int)(Owner.position.Y / 16f);

            float baseAngle = lockedDirection.ToRotation();
            startAngle = baseAngle + MathHelper.ToRadians(-145f * Owner.direction);
            endAngle = baseAngle + MathHelper.ToRadians(102f * Owner.direction);
            currentAngle = startAngle;

            SoundEngine.PlaySound(SoundID.Item7 with { Volume = 0.65f, Pitch = -0.5f }, Owner.Center);
        }

        private void SpawnImpactEffects()
        {
            if (Main.dedServ)
                return;

            Vector2 direction = currentAngle.ToRotationVector2();
            Vector2 tip = Owner.MountedCenter + direction * BladeReach * scale;
            for (int i = 0; i < 24; i++)
            {
                Vector2 velocity = direction.RotatedBy(Main.rand.NextFloat(-0.9f, 0.9f)) * Main.rand.NextFloat(2f, 8f);
                Dust dust = Dust.NewDustPerfect(tip + Main.rand.NextVector2Circular(18f, 18f), Main.rand.NextBool() ? DustID.Shadowflame : DustID.Blood,
                    velocity, 90, Main.rand.NextBool() ? DarkPurple : BloodRed, Main.rand.NextFloat(1f, 1.7f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = new(0f, swordTexture.Height);
            float drawRotation = currentAngle + MathHelper.PiOver4;
            Vector2 drawPosition = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float opacity = Utils.GetLerpValue(0f, 6f, timer, true) * Utils.GetLerpValue(Duration, Duration - 8f, timer, true);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(smear, drawPosition, null, BloodRed with { A = 0 } * opacity * 0.64f,
                drawRotation + MathHelper.PiOver2 * Math.Sign(endAngle - startAngle), smear.Size() * 0.5f, scale, SpriteEffects.None);
            Vector2 tip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, tip, null, Color.White with { A = 0 } * opacity * 0.5f,
                0f, bloom.Size() * 0.5f, scale * 0.56f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            return false;
        }
    }

    internal sealed class GaelGreatswordCapeFinisher : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const int Duration = 210;
        private const float CapeRadius = 150f;

        private static readonly Color SoulPurple = new(80, 30, 130);
        private static readonly Color BloodRed = new(165, 8, 36);

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private float spinRotation;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = (int)(CapeRadius * 2f);
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 11;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
            Projectile.ContinuouslyUpdateDamageStats = true;
        }

        public override void AI()
        {
            if (!Owner.active || Owner.dead)
            {
                Projectile.Kill();
                return;
            }

            timer++;
            Projectile.Center = Owner.Center;
            Projectile.timeLeft = 4;
            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 1.4f);

            float spinSpeed = MathHelper.Lerp(0.21f, 0.42f, Utils.GetLerpValue(0f, 60f, timer, true));
            spinRotation += spinSpeed;
            Projectile.rotation = spinRotation;

            EmitCapeParticles();
            FireDarkSouls();

            if (timer >= Duration)
                Projectile.Kill();
        }

        public override bool? CanDamage() => timer >= 12 && timer <= Duration - 10 ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = CapeRadius + MathF.Sin(timer * 0.11f) * 22f;
            Vector2 closest = targetHitbox.ClosestPointInRect(Projectile.Center);
            return closest.Distance(Projectile.Center) <= radius ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.42f, MathHelper.Clamp(Projectile.numHits / 12f, 0f, 1f));
        }

        private void FireDarkSouls()
        {
            if (Main.myPlayer != Projectile.owner || timer % 7 != 0)
                return;

            NPC target = FindTarget();
            Vector2 forward = target != null
                ? Projectile.Center.DirectionTo(target.Center).SafeNormalize(Vector2.UnitY)
                : (spinRotation + Main.rand.NextFloat(-0.7f, 0.7f)).ToRotationVector2();

            Vector2 spawnPosition = Projectile.Center - forward * Main.rand.NextFloat(80f, 140f) + Main.rand.NextVector2Circular(50f, 50f);
            Vector2 velocity = forward.RotatedBy(Main.rand.NextFloat(-0.35f, 0.35f)) * Main.rand.NextFloat(8f, 13f);
            int damage = Math.Max(1, (int)(Projectile.damage * 0.55f));
            Projectile.NewProjectile(Projectile.GetSource_FromThis(), spawnPosition, velocity,
                ModContent.ProjectileType<GaelGreatswordDarkSoul>(), damage, 1.2f, Projectile.owner, target?.whoAmI ?? -1);
        }

        private NPC FindTarget()
        {
            NPC closest = null;
            float closestDistance = 1200f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = npc.Distance(Projectile.Center);
                if (distance >= closestDistance)
                    continue;

                closestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private void EmitCapeParticles()
        {
            if (Main.dedServ)
                return;

            if (timer % 2 == 0)
            {
                Vector2 edge = Projectile.Center + spinRotation.ToRotationVector2().RotatedBy(Main.rand.NextFloat(-1.3f, 1.3f)) * Main.rand.NextFloat(80f, CapeRadius);
                Vector2 velocity = edge.DirectionFrom(Projectile.Center).RotatedBy(MathHelper.PiOver2) * Main.rand.NextFloat(2f, 5f);
                GeneralParticleHandler.SpawnParticle(new LineParticle(edge, velocity, false,
                    Main.rand.Next(14, 24), Main.rand.NextFloat(0.45f, 0.78f), Main.rand.NextBool(3) ? BloodRed : SoulPurple));
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(CapeRadius, CapeRadius),
                    DustID.Shadowflame, Main.rand.NextVector2Circular(2f, 2f), 120, SoulPurple, Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D capeTexture = GetCapeTexture().Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 center = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            Vector2 origin = capeTexture.Size() * 0.5f;
            float fadeIn = Utils.GetLerpValue(0f, 18f, timer, true);
            float fadeOut = Utils.GetLerpValue(Duration, Duration - 24f, timer, true);
            float opacity = fadeIn * fadeOut;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < 6; i++)
            {
                float angle = spinRotation + MathHelper.TwoPi * i / 6f;
                Vector2 offset = angle.ToRotationVector2() * 34f;
                Color color = i % 2 == 0 ? BloodRed : SoulPurple;
                Main.EntitySpriteDraw(capeTexture, center + offset, null, color with { A = 0 } * opacity * 0.28f,
                    angle + MathHelper.PiOver2, origin, 1.25f + i * 0.035f, SpriteEffects.None);
            }

            Main.EntitySpriteDraw(bloom, center, null, SoulPurple with { A = 0 } * opacity * 0.46f,
                0f, bloom.Size() * 0.5f, 3.1f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private static Asset<Texture2D> GetCapeTexture()
        {
            string[] candidates =
            {
                "CalamityMod/Items/Accessories/Wings/BloodflareWings",
                "CalamityMod/Items/Accessories/Wings/SilvaWings",
                "CalamityMod/Items/Accessories/Wings/TarragonWings",
                "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword",
            };

            foreach (string candidate in candidates)
            {
                if (ModContent.RequestIfExists(candidate, out Asset<Texture2D> asset))
                    return asset;
            }

            return ModContent.Request<Texture2D>("CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword");
        }
    }

    internal sealed class GaelGreatswordDarkSoul : ModProjectile
    {
        public override string Texture => "Terraria/Images/Projectile_" + ProjectileID.Skull;

        private static readonly Color SoulPurple = new(85, 30, 135);
        private static readonly Color BloodRed = new(150, 8, 32);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
            Projectile.extraUpdates = 1;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            Projectile.rotation += (MathF.Abs(Projectile.velocity.X) + MathF.Abs(Projectile.velocity.Y)) * 0.018f * Math.Sign(Projectile.velocity.X == 0f ? 1f : Projectile.velocity.X);
            Lighting.AddLight(Projectile.Center, 0.22f, 0.04f, 0.32f);

            if (Projectile.ai[1] > 12f)
            {
                NPC target = GetStoredTarget();
                if (target != null)
                    HomeToward(target);
                else
                    CalamityUtils.HomeInOnNPC(Projectile, true, 900f, 15f, 26f);
            }

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(4) ? DustID.Blood : DustID.Shadowflame,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.3f, 1.8f), 120,
                    Main.rand.NextBool(4) ? BloodRed : SoulPurple, Main.rand.NextFloat(0.8f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.55f, MathHelper.Clamp(Projectile.numHits / 3f, 0f, 1f));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 8; i++)
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool() ? DustID.Blood : DustID.Shadowflame,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.4f, 4.2f), 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(0.9f, 1.35f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = TextureAssets.Projectile[ProjectileID.Skull].Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = texture.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color drawColor = Color.Lerp(SoulPurple, BloodRed, 0.35f + MathF.Sin(Projectile.ai[1] * 0.08f) * 0.2f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, null, drawColor with { A = 0 } * completion * 0.35f,
                    Projectile.rotation, origin, Projectile.scale * (0.75f + completion * 0.35f), effects);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, drawColor with { A = 0 } * 0.42f,
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.72f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, null, Color.White,
                Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }

        private NPC GetStoredTarget()
        {
            int targetIndex = (int)Projectile.ai[0];
            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return null;

            NPC target = Main.npc[targetIndex];
            return target.CanBeChasedBy(Projectile) ? target : null;
        }

        private void HomeToward(NPC target)
        {
            Vector2 desiredVelocity = Projectile.SafeDirectionTo(target.Center) * 16f;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desiredVelocity, 0.075f);
        }
    }

    internal static class GaelGreatswordRageInterop
    {
        private const BindingFlags InstanceFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        private const BindingFlags StaticFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

        private static Type calamityPlayerType;
        private static Type calamityKeybindsType;
        private static readonly Dictionary<string, FieldInfo> PlayerFields = new();
        private static readonly Dictionary<string, PropertyInfo> PlayerProperties = new();

        public static float GetRageRatio(Player player)
        {
            float rageMax = GetRageMax(player);
            return rageMax <= 0f ? 0f : MathHelper.Clamp(GetRage(player) / rageMax, 0f, 1f);
        }

        public static void AddRage(Player player, float amount)
        {
            if (amount <= 0f)
                return;

            object calamityPlayer = player.Calamity();
            FieldInfo rageField = GetPlayerField(calamityPlayer, "rage");
            if (rageField == null)
                return;

            float rage = ReadFloat(calamityPlayer, rageField);
            float rageMax = Math.Max(1f, GetRageMax(player));
            WriteFloat(calamityPlayer, rageField, MathHelper.Clamp(rage + amount, 0f, rageMax));
        }

        public static bool RageHotKeyJustPressed()
        {
            object keybind = GetRageKeybind();
            if (keybind == null)
                return false;

            PropertyInfo justPressed = keybind.GetType().GetProperty("JustPressed", InstanceFlags);
            if (justPressed?.GetValue(keybind) is bool pressed)
                return pressed;

            PropertyInfo current = keybind.GetType().GetProperty("Current", InstanceFlags);
            return current?.GetValue(keybind) is bool held && held;
        }

        public static string GetRageKeyText()
        {
            object keybind = GetRageKeybind();
            if (keybind == null)
                return "Rage";

            if (TryGetAssignedKeys(keybind) is IEnumerable keys)
            {
                foreach (object key in keys)
                    return key?.ToString() ?? "Rage";
            }

            return "Rage";
        }

        private static IEnumerable TryGetAssignedKeys(object keybind)
        {
            foreach (MethodInfo method in keybind.GetType().GetMethods(InstanceFlags))
            {
                if (method.Name != "GetAssignedKeys" || method.ContainsGenericParameters)
                    continue;

                object[] arguments = BuildDefaultArguments(method);
                if (arguments == null)
                    continue;

                try
                {
                    if (method.Invoke(keybind, arguments) is IEnumerable keys)
                        return keys;
                }
                catch (TargetInvocationException)
                {
                }
                catch (TargetParameterCountException)
                {
                }
                catch (ArgumentException)
                {
                }
            }

            return null;
        }

        private static object[] BuildDefaultArguments(MethodInfo method)
        {
            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = new object[parameters.Length];

            for (int i = 0; i < parameters.Length; i++)
            {
                ParameterInfo parameter = parameters[i];
                if (parameter.HasDefaultValue && parameter.DefaultValue != DBNull.Value)
                {
                    arguments[i] = parameter.DefaultValue;
                    continue;
                }

                Type parameterType = parameter.ParameterType;
                if (parameterType.IsEnum)
                {
                    arguments[i] = GetDefaultEnumValue(parameterType);
                    if (arguments[i] == null)
                        return null;
                    continue;
                }

                arguments[i] = parameterType.IsValueType ? Activator.CreateInstance(parameterType) : null;
            }

            return arguments;
        }

        private static object GetDefaultEnumValue(Type enumType)
        {
            if (Enum.IsDefined(enumType, "Keyboard"))
                return Enum.Parse(enumType, "Keyboard");

            Array values = Enum.GetValues(enumType);
            return values.Length > 0 ? values.GetValue(0) : null;
        }

        private static float GetRage(Player player)
        {
            object calamityPlayer = player.Calamity();
            FieldInfo rageField = GetPlayerField(calamityPlayer, "rage");
            return rageField == null ? 0f : ReadFloat(calamityPlayer, rageField);
        }

        private static float GetRageMax(Player player)
        {
            object calamityPlayer = player.Calamity();
            FieldInfo rageMaxField = GetPlayerField(calamityPlayer, "rageMax");
            if (rageMaxField != null)
                return ReadFloat(calamityPlayer, rageMaxField);

            PropertyInfo rageMaxProperty = GetPlayerProperty(calamityPlayer, "RageMax");
            if (rageMaxProperty?.GetValue(calamityPlayer) is float rageMax)
                return rageMax;

            return 100f;
        }

        private static FieldInfo GetPlayerField(object calamityPlayer, string name)
        {
            if (calamityPlayer == null)
                return null;

            calamityPlayerType ??= calamityPlayer.GetType();
            if (PlayerFields.TryGetValue(name, out FieldInfo cached))
                return cached;

            FieldInfo field = calamityPlayerType.GetField(name, InstanceFlags);
            PlayerFields[name] = field;
            return field;
        }

        private static PropertyInfo GetPlayerProperty(object calamityPlayer, string name)
        {
            if (calamityPlayer == null)
                return null;

            calamityPlayerType ??= calamityPlayer.GetType();
            if (PlayerProperties.TryGetValue(name, out PropertyInfo cached))
                return cached;

            PropertyInfo property = calamityPlayerType.GetProperty(name, InstanceFlags);
            PlayerProperties[name] = property;
            return property;
        }

        private static float ReadFloat(object target, FieldInfo field)
        {
            object value = field.GetValue(target);
            return value switch
            {
                float f => f,
                double d => (float)d,
                int i => i,
                _ => 0f,
            };
        }

        private static void WriteFloat(object target, FieldInfo field, float value)
        {
            if (field.FieldType == typeof(float))
                field.SetValue(target, value);
            else if (field.FieldType == typeof(double))
                field.SetValue(target, (double)value);
            else if (field.FieldType == typeof(int))
                field.SetValue(target, (int)value);
        }

        private static object GetRageKeybind()
        {
            calamityKeybindsType ??= FindType("CalamityMod.CalamityKeybinds");
            if (calamityKeybindsType == null)
                return null;

            PropertyInfo property = calamityKeybindsType.GetProperty("RageHotKey", StaticFlags);
            if (property != null)
                return property.GetValue(null);

            FieldInfo field = calamityKeybindsType.GetField("RageHotKey", StaticFlags);
            return field?.GetValue(null);
        }

        private static Type FindType(string fullName)
        {
            Type direct = Type.GetType(fullName + ", CalamityMod");
            if (direct != null)
                return direct;

            string shortName = fullName[(fullName.LastIndexOf('.') + 1)..];
            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type type = assembly.GetType(fullName);
                if (type != null)
                    return type;

                try
                {
                    foreach (Type candidate in assembly.GetTypes())
                    {
                        if (candidate.Name == shortName)
                            return candidate;
                    }
                }
                catch (ReflectionTypeLoadException ex)
                {
                    foreach (Type candidate in ex.Types)
                    {
                        if (candidate?.Name == shortName)
                            return candidate;
                    }
                }
            }

            return null;
        }
    }
}
