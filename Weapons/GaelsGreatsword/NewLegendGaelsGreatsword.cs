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
        private static int GuardHoldoutType => ModContent.ProjectileType<GaelGreatswordGuardHoldout>();
        private static int CapeFinisherType => ModContent.ProjectileType<GaelGreatswordCapeFinisher>();
        private static int EmberUIType => ModContent.ProjectileType<GaelGreatswordEmberUI>();

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

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
                player.ownedProjectileCounts[GuardHoldoutType] > 0 ||
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
                GaelGreatswordPlayer gaelPlayer = player.GetModPlayer<GaelGreatswordPlayer>();
                bool canGuard = gaelPlayer.GuardCooldown <= 0;
                Item.channel = canGuard;
                Item.autoReuse = false;
                Item.useTime = canGuard ? 14 : 31;
                Item.useAnimation = Item.useTime;
                Item.shoot = canGuard ? GuardHoldoutType : PlungeHoldoutType;
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
                player.ownedProjectileCounts[GuardHoldoutType] <= 0 &&
                player.ownedProjectileCounts[CapeFinisherType] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDirection = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

            if (player.altFunctionUse == 2)
            {
                GaelGreatswordPlayer gaelState = player.GetModPlayer<GaelGreatswordPlayer>();
                if (gaelState.GuardCooldown <= 0)
                {
                    Projectile.NewProjectile(source, player.MountedCenter, aimDirection, GuardHoldoutType, damage, knockback, player.whoAmI);
                    return false;
                }

                Vector2 targetPoint = GetMouseWorld(player);
                int plungeDamage = (int)(damage * PlungeDamageMultiplier);
                Projectile.NewProjectile(source, player.MountedCenter, Vector2.Zero, PlungeHoldoutType, plungeDamage, knockback + 2f, player.whoAmI, targetPoint.X, targetPoint.Y);
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

            if (Main.myPlayer != player.whoAmI)
                return;

            if (player.ownedProjectileCounts[EmberUIType] <= 0)
                Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero, EmberUIType, 0, 0f, player.whoAmI);

            if (!GaelGreatswordRageInterop.RageHotKeyJustPressed())
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

            if (!gaelPlayer.ConsumeDarkEmbers(GaelGreatswordPlayer.DarkEmberMax))
            {
                CombatText.NewText(player.Hitbox, new Color(120, 56, 170), Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendGaelsGreatsword.FinisherNeedsEmbers"));
                return;
            }

            int damage = (int)(player.GetWeaponDamage(Item) * 0.92f);
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
        public const int DarkEmberMax = 100;
        public const int GuardCooldownMax = 5 * 60;
        public const int ParryWindowFrames = 14;
        private const int LeftComboLength = 6;

        public int FollowupSlashWindow;
        public int FinisherCooldown;
        public int DarkEmbers;
        public int DarkEmberFlashTimer;
        public int GuardCooldown;
        public int GuardFlashTimer;
        public int ParryFlashTimer;

        private int holdingTimer;
        private int guardActiveTimer;
        private int guardStanceAge;
        private bool parryConsumedThisStance;
        private int parryPendingTimer;
        private Vector2 parryPendingSource;
        private int blackBloodAfterglowTimer;
        private int darkSoulFlightTime;
        private int blackBloodLifeCostCooldown;
        private int darkEmberDecayDelay;
        private int leftComboIndex;
        private int leftComboResetTimer;
        private int guardKnockbackTimer;
        private bool darkEmberWasReady;
        private Vector2 guardSourceCenter;
        private Vector2 guardKnockbackVelocity;

        public bool HoldingGael => holdingTimer > 0;
        public bool GuardActive => guardActiveTimer > 0 && GuardCooldown <= 0;
        public float GuardCooldownRatio => GuardCooldown <= 0 ? 0f : GuardCooldown / (float)GuardCooldownMax;
        public bool BlackBloodActive => blackBloodAfterglowTimer > 0;
        public bool BlackHumanityActive => HoldingGael && Player.statLife <= Player.statLifeMax2 / 2;
        public bool DarkEmberReady => DarkEmbers >= DarkEmberMax;
        public float DarkEmberRatio => MathHelper.Clamp(DarkEmbers / (float)DarkEmberMax, 0f, 1f);

        public override void PreUpdate()
        {
            if (holdingTimer > 0)
                holdingTimer--;
            if (FollowupSlashWindow > 0)
                FollowupSlashWindow--;
            if (FinisherCooldown > 0)
                FinisherCooldown--;
            if (GuardCooldown > 0)
                GuardCooldown--;
            if (GuardFlashTimer > 0)
                GuardFlashTimer--;
            if (ParryFlashTimer > 0)
                ParryFlashTimer--;
            if (parryPendingTimer > 0)
                parryPendingTimer--;
            if (guardActiveTimer > 0)
                guardActiveTimer--;
            if (leftComboResetTimer > 0)
            {
                leftComboResetTimer--;
                if (leftComboResetTimer <= 0)
                    leftComboIndex = 0;
            }
            if (blackBloodAfterglowTimer > 0)
                blackBloodAfterglowTimer--;
            if (blackBloodLifeCostCooldown > 0)
                blackBloodLifeCostCooldown--;
            if (DarkEmberFlashTimer > 0)
                DarkEmberFlashTimer--;
            UpdateDarkEmbers();
        }

        public override void PostUpdate()
        {
            if (guardKnockbackTimer <= 0)
                return;

            guardKnockbackTimer--;
            Player.velocity = guardKnockbackVelocity;
            Player.fallStart = (int)(Player.position.Y / 16f);
        }

        public override void UpdateDead()
        {
            FollowupSlashWindow = 0;
            FinisherCooldown = 0;
            GuardCooldown = 0;
            GuardFlashTimer = 0;
            ParryFlashTimer = 0;
            DarkEmbers = 0;
            DarkEmberFlashTimer = 0;
            holdingTimer = 0;
            guardActiveTimer = 0;
            guardStanceAge = 0;
            parryConsumedThisStance = false;
            parryPendingTimer = 0;
            parryPendingSource = Vector2.Zero;
            blackBloodAfterglowTimer = 0;
            darkSoulFlightTime = 0;
            blackBloodLifeCostCooldown = 0;
            darkEmberDecayDelay = 0;
            leftComboIndex = 0;
            leftComboResetTimer = 0;
            guardKnockbackTimer = 0;
            darkEmberWasReady = false;
            guardSourceCenter = Vector2.Zero;
            guardKnockbackVelocity = Vector2.Zero;
        }

        public bool ConsumeFollowupSlash()
        {
            if (FollowupSlashWindow <= 0)
                return false;

            FollowupSlashWindow = 0;
            return true;
        }

        public int ConsumeLeftComboIndex(bool followupSlash)
        {
            leftComboResetTimer = 180;

            if (followupSlash)
                return LeftComboLength - 1;

            int combo = leftComboIndex;
            leftComboIndex = (leftComboIndex + 1) % LeftComboLength;
            return combo;
        }

        public bool ParryWindowOpen => GuardActive && !parryConsumedThisStance && guardStanceAge <= ParryWindowFrames;

        public void SetGuardActive(Vector2 sourceCenter, int stanceAge)
        {
            guardActiveTimer = 2;
            guardSourceCenter = sourceCenter;
            guardStanceAge = stanceAge;
            if (stanceAge <= 1)
                parryConsumedThisStance = false;
            holdingTimer = 2;
            Player.noKnockback = true;
            Lighting.AddLight(Player.Center, 0.26f, 0.04f, 0.34f);
        }

        public void ApplyHeldEffects()
        {
            holdingTimer = 2;
            ApplyDarkSoulPassive();
            ApplyBloodAndFire();
            ApplyBlackHumanity();
            ApplyDarkEmberPassive();
        }

        public float TryPayBlackBloodCost()
        {
            if (!BlackBloodActive || blackBloodLifeCostCooldown > 0 || Player.statLife <= 1)
                return 1f;

            int cost = Math.Max(4, (int)MathF.Ceiling(Player.statLifeMax2 * 0.035f));
            Player.statLife = Math.Max(1, Player.statLife - cost);
            blackBloodLifeCostCooldown = 6;
            blackBloodAfterglowTimer = Math.Max(blackBloodAfterglowTimer, 180);
            AddDarkEmbers(4 + GaelGreatswordProgression.GetStage(), true);

            CombatText.NewText(Player.Hitbox, new Color(160, 18, 34), cost, true);
            SpawnBlackBloodPaidEffects();
            return 1.32f + GaelGreatswordProgression.GetStage() * 0.03f + DarkEmberRatio * 0.08f;
        }

        public override void ModifyHitByNPC(NPC npc, ref Player.HurtModifiers modifiers)
        {
            TryBreakGuard(npc.Center, ref modifiers);
        }

        public override void ModifyHitByProjectile(Projectile proj, ref Player.HurtModifiers modifiers)
        {
            TryBreakGuard(proj.Center, ref modifiers);
        }

        public void RegisterGreatswordHit(NPC target, int baseEmbers, bool forceBloodWake = false)
        {
            AddDarkEmbers(baseEmbers + GaelGreatswordProgression.GetStage(), forceBloodWake);

            if (target != null && Main.myPlayer == Player.whoAmI && Main.rand.NextBool(3))
            {
                int echoDamage = Math.Max(1, (int)(Player.GetWeaponDamage(Player.HeldItem) * MathHelper.Lerp(0.16f, 0.28f, DarkEmberRatio)));
                Projectile.NewProjectile(Player.GetSource_FromThis(), target.Center, Vector2.Zero,
                    ModContent.ProjectileType<GaelGreatswordBloodEcho>(), echoDamage, 1.2f, Player.whoAmI, DarkEmberRatio);
            }
        }

        public bool ConsumeDarkEmbers(int amount)
        {
            if (DarkEmbers < amount)
                return false;

            DarkEmbers -= amount;
            DarkEmberFlashTimer = 20;
            darkEmberWasReady = false;
            SpawnDarkEmberBurst(28, 6.2f);
            return true;
        }

        public void AddDarkEmbers(int amount, bool forceBloodWake = false)
        {
            if (amount <= 0)
                return;

            bool wasReady = DarkEmberReady;
            DarkEmbers = Math.Clamp(DarkEmbers + amount, 0, DarkEmberMax);
            darkEmberDecayDelay = 6 * 60;
            DarkEmberFlashTimer = Math.Max(DarkEmberFlashTimer, 10);

            if (forceBloodWake)
                blackBloodAfterglowTimer = Math.Max(blackBloodAfterglowTimer, 180);

            if (DarkEmberReady && !wasReady)
                SpawnDarkEmberBurst(18, 4.4f);

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref darkEmberWasReady, DarkEmberReady);
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

        private void ApplyDarkEmberPassive()
        {
            if (DarkEmbers <= 0)
                return;

            float ratio = DarkEmberRatio;
            Player.GetDamage(DamageClass.Melee) += ratio * 0.06f;
            Player.GetAttackSpeed(DamageClass.Melee) += ratio * 0.08f;
            Player.endurance += ratio * 0.025f;
            Lighting.AddLight(Player.Center, 0.16f * ratio, 0.03f * ratio, 0.22f * ratio);

            if (DarkEmberReady && Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(Player.width * 0.8f, Player.height),
                    Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    Main.rand.NextVector2Circular(1.1f, 1.1f), 100, new Color(150, 20, 70), Main.rand.NextFloat(0.85f, 1.3f));
                dust.noGravity = true;
            }
        }

        private void UpdateDarkEmbers()
        {
            if (darkEmberDecayDelay > 0)
            {
                darkEmberDecayDelay--;
            }
            else if (!HoldingGael && DarkEmbers > 0 && Main.GameUpdateCount % 5 == 0)
            {
                DarkEmbers--;
            }

            LegendaryUltimateReadySound.PlayIfReadyTransition(Player, ref darkEmberWasReady, DarkEmberReady);
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

        private void SpawnDarkEmberBurst(int count, float speed)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(speed * 0.25f, speed);
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(18f, 28f),
                    Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    velocity, 90, Main.rand.NextBool() ? new Color(175, 18, 42) : new Color(92, 36, 150),
                    Main.rand.NextFloat(1f, 1.65f));
                dust.noGravity = true;
            }
        }

        public override bool FreeDodge(Player.HurtInfo info)
        {
            if (parryPendingTimer <= 0)
                return false;

            parryPendingTimer = 0;
            ExecutePerfectParry(parryPendingSource);
            return true;
        }

        private void TryBreakGuard(Vector2 sourceCenter, ref Player.HurtModifiers modifiers)
        {
            if (!GuardActive)
                return;

            Vector2 resolvedSource = sourceCenter == Vector2.Zero ? guardSourceCenter : sourceCenter;

            // 举剑瞬间的完美格挡窗口：完全格开这次攻击，姿态不中断，也不进入冷却。
            if (ParryWindowOpen)
            {
                parryConsumedThisStance = true;
                parryPendingTimer = 2;
                parryPendingSource = resolvedSource;
                return;
            }

            modifiers.FinalDamage *= 0.5f;
            BreakGuard(resolvedSource);
        }

        private void ExecutePerfectParry(Vector2 sourceCenter)
        {
            GuardFlashTimer = 26;
            ParryFlashTimer = 30;
            Player.immune = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 30);

            AddDarkEmbers(18 + GaelGreatswordProgression.GetStage() * 2, true);
            FollowupSlashWindow = 45;

            CombatText.NewText(Player.Hitbox, new Color(238, 214, 250), Language.GetTextValue("Mods.CalamityLegendsComeBack.Items.Weapons.NewLegendGaelsGreatsword.ParryText"));
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.95f, Pitch = 0.48f }, Player.Center);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = 0.6f, Pitch = -0.15f }, Player.Center);

            Vector2 counterDirection = Player.Center.DirectionTo(sourceCenter);
            if (counterDirection == Vector2.Zero || float.IsNaN(counterDirection.X))
                counterDirection = Vector2.UnitX * Player.direction;
            counterDirection = counterDirection.SafeNormalize(Vector2.UnitX * Player.direction);

            SpawnParryEffects(counterDirection);

            if (Main.myPlayer != Player.whoAmI || Player.HeldItem == null)
                return;

            Player.Calamity().GeneralScreenShakePower = Math.Max(Player.Calamity().GeneralScreenShakePower, 5f);

            int weaponDamage = Player.GetWeaponDamage(Player.HeldItem);
            Vector2 novaCenter = Player.Center + counterDirection * 64f;
            Projectile.NewProjectile(Player.GetSource_FromThis(), novaCenter, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), Math.Max(1, (int)(weaponDamage * 0.85f)),
                6f, Player.whoAmI, 1f);

            for (int i = 0; i < 3; i++)
            {
                float spread = MathHelper.Lerp(-0.3f, 0.3f, i / 2f);
                Vector2 velocity = counterDirection.RotatedBy(spread) * Main.rand.NextFloat(9.5f, 12f);
                Projectile.NewProjectile(Player.GetSource_FromThis(), Player.Center + counterDirection * 30f, velocity,
                    ModContent.ProjectileType<GaelGreatswordVengefulSoul>(), Math.Max(1, (int)(weaponDamage * 0.4f)),
                    2f, Player.whoAmI);
            }
        }

        private void SpawnParryEffects(Vector2 counterDirection)
        {
            if (Main.dedServ)
                return;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center + counterDirection * 48f,
                counterDirection * 3f, new Color(238, 214, 250), new Vector2(1.35f, 0.6f),
                counterDirection.ToRotation(), 0.14f, 0.9f, 22));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(Player.Center + counterDirection * 40f, Vector2.Zero,
                new Color(238, 214, 250) * 0.7f, 0.85f, 14));

            for (int i = 0; i < 14; i++)
            {
                Vector2 velocity = counterDirection.RotatedByRandom(0.9f) * Main.rand.NextFloat(3f, 11f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(Player.Center + counterDirection * 44f + Main.rand.NextVector2Circular(14f, 14f),
                    velocity, Color.White, Main.rand.NextBool() ? new Color(190, 18, 42) : new Color(122, 52, 182),
                    Main.rand.NextFloat(0.4f, 0.85f), Main.rand.Next(10, 18)));
            }
        }

        private void BreakGuard(Vector2 sourceCenter)
        {
            GuardCooldown = GuardCooldownMax;
            GuardFlashTimer = 28;
            guardActiveTimer = 0;

            Vector2 knockbackDirection = Player.Center.DirectionFrom(sourceCenter);
            if (knockbackDirection == Vector2.Zero)
                knockbackDirection = new Vector2(-Player.direction, -0.25f);
            knockbackDirection = knockbackDirection.SafeNormalize(new Vector2(-Player.direction, -0.25f));

            guardKnockbackVelocity = knockbackDirection * 14.5f + new Vector2(0f, -3.2f);
            guardKnockbackTimer = 5;
            Player.velocity = guardKnockbackVelocity;
            Player.fallStart = (int)(Player.position.Y / 16f);
            Player.immune = true;
            Player.immuneNoBlink = true;
            Player.immuneTime = Math.Max(Player.immuneTime, 12);

            int guardType = ModContent.ProjectileType<GaelGreatswordGuardHoldout>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == guardType)
                    projectile.Kill();
            }

            AddDarkEmbers(12 + GaelGreatswordProgression.GetStage(), true);
            SpawnGuardBreakEffects(knockbackDirection);
            SoundEngine.PlaySound(SoundID.Item37 with { Volume = 0.82f, Pitch = -0.28f }, Player.Center);
        }

        private void SpawnGuardBreakEffects(Vector2 knockbackDirection)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 22; i++)
            {
                Vector2 velocity = (-knockbackDirection).RotatedBy(Main.rand.NextFloat(-0.85f, 0.85f)) * Main.rand.NextFloat(2.2f, 8.5f);
                Dust dust = Dust.NewDustPerfect(Player.Center + Main.rand.NextVector2Circular(20f, 34f),
                    Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame, velocity, 90,
                    Main.rand.NextBool() ? new Color(190, 18, 42) : new Color(92, 36, 150),
                    Main.rand.NextFloat(1f, 1.7f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Player.Center, -knockbackDirection * 4f,
                new Color(190, 18, 42), new Vector2(1.6f, 0.52f), knockbackDirection.ToRotation(),
                0.18f, 0.05f, 24));
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
            Color impactColor = bloodDamageMultiplier > 1f ? BloodRed : DarkPurple;

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(target.Center, Vector2.Zero,
                impactColor, new Vector2(1.05f, 0.62f), direction.ToRotation(), 0.1f, crit ? 0.86f : 0.68f, 13));
            GeneralParticleHandler.SpawnParticle(new StrongBloom(target.Center, Vector2.Zero,
                PaleCore * (crit ? 0.62f : 0.45f), crit ? 0.62f : 0.44f, 10));

            int sparkCount = crit ? 10 : 6;
            for (int i = 0; i < sparkCount; i++)
            {
                Vector2 velocity = direction.RotatedByRandom(0.75f) * Main.rand.NextFloat(3f, crit ? 11f : 8.5f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(target.Center + Main.rand.NextVector2Circular(12f, 12f),
                    velocity, Color.White, Main.rand.NextBool() ? impactColor : PaleCore,
                    Main.rand.NextFloat(0.35f, 0.75f), Main.rand.Next(9, 15)));
            }
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
                    SpawnComboProjectile(ModContent.ProjectileType<GaelGreatswordCatastropheSlash>(),
                        basePosition + aim * 58f, aim * 9.4f, 0.62f, 1.05f, aim.ToRotation(), Math.Sign(endAngle - startAngle));
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

            // 剑尖闪星：斩击弧线的最外缘随行一缕苍白星光。
            if (Main.rand.NextBool(3))
            {
                Vector2 tipPosition = Owner.MountedCenter + direction * BladeReach * scale * Main.rand.NextFloat(0.88f, 1f);
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(tipPosition,
                    perpendicular * Main.rand.NextFloat(1.5f, 4f), Color.White,
                    bloodDamageMultiplier > 1f ? BloodRed : DarkPurple,
                    Main.rand.NextFloat(0.4f, 0.7f), Main.rand.Next(9, 14), Main.rand.NextFloat(-0.1f, 0.1f), 2.2f));
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
            Color rimBase = bloodDamageMultiplier > 1f ? BloodRed : DarkPurple;
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

    internal sealed class GaelGreatswordPlungeHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const int TelegraphFrames = 8;
        private const int DescentFrames = 18;
        private const int Duration = 48;
        private const float SwordVisualScale = 1.64f;
        private const float BladeReach = 158f;
        private const float ImpactRadius = 136f;

        private static readonly Color DarkPurple = new(60, 20, 100);
        private static readonly Color BloodRed = new(175, 10, 30);
        private static readonly Color PaleCore = new(226, 205, 245);

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private bool initialized;
        private bool impactEffectsPlayed;
        private float currentAngle;
        private float scale = 1f;
        private Vector2 startPoint;
        private Vector2 targetPoint;

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
            if (!initialized)
                InitializePlunge();

            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Projectile.timeLeft = 4;

            timer++;
            float descentProgress = MathHelper.Clamp((timer - TelegraphFrames) / (float)DescentFrames, 0f, 1f);
            float easedDescent = CalamityUtils.EaseInOutExp(descentProgress, 5f, 2f);
            Vector2 intendedCenter = Vector2.Lerp(startPoint, targetPoint, easedDescent);
            if (timer <= TelegraphFrames + DescentFrames + 2)
            {
                Owner.Center = intendedCenter;
                Owner.velocity = descentProgress < 1f ? new Vector2(0f, 18f) : Vector2.Zero;
                Owner.fallStart = (int)(Owner.position.Y / 16f);
            }
            else
            {
                Owner.velocity *= 0.72f;
            }

            Projectile.Center = Owner.MountedCenter;
            currentAngle = MathHelper.PiOver2 + MathHelper.Lerp(-0.42f * Owner.direction, 0.08f * Owner.direction, easedDescent);

            if (!impactEffectsPlayed && timer > TelegraphFrames)
                EmitDescentEffects(descentProgress);

            if (!impactEffectsPlayed && timer >= TelegraphFrames + DescentFrames)
            {
                impactEffectsPlayed = true;
                SpawnImpactEffects();
                Owner.GetModPlayer<GaelGreatswordPlayer>().FollowupSlashWindow = 45;
                Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 6.4f);
                SoundEngine.PlaySound(SoundID.Item71 with { Volume = 0.88f, Pitch = -0.5f }, targetPoint);
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
            return timer >= TelegraphFrames && timer <= Duration - 8 ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float collisionPoint = 0f;
            Vector2 hilt = Owner.MountedCenter - Vector2.UnitY * 10f;
            Vector2 bladeTip = hilt + currentAngle.ToRotationVector2() * BladeReach * scale;
            if (Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                hilt, bladeTip, 38f * scale, ref collisionPoint))
            {
                return null;
            }

            if (impactEffectsPlayed && targetHitbox.ClosestPointInRect(targetPoint).Distance(targetPoint) <= ImpactRadius)
                return null;

            return false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.55f, MathHelper.Clamp(Projectile.numHits / 5f, 0f, 1f));
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            GaelGreatswordPlayer gaelPlayer = Owner.GetModPlayer<GaelGreatswordPlayer>();
            gaelPlayer.FollowupSlashWindow = 45;
            gaelPlayer.RegisterGreatswordHit(target, 16, true);
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

        private void InitializePlunge()
        {
            initialized = true;
            targetPoint = new Vector2(Projectile.ai[0], Projectile.ai[1]);
            if (targetPoint == Vector2.Zero)
                targetPoint = NewLegendGaelsGreatsword.GetMouseWorld(Owner);

            startPoint = targetPoint + new Vector2(0f, -10f * 16f);
            Owner.direction = Math.Sign(targetPoint.X - Owner.Center.X);
            if (Owner.direction == 0)
                Owner.direction = 1;

            Owner.Center = startPoint;
            Owner.velocity = Vector2.Zero;
            Owner.fallStart = (int)(Owner.position.Y / 16f);
            currentAngle = MathHelper.PiOver2 - 0.42f * Owner.direction;

            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.58f, Pitch = -0.65f }, startPoint);
            SpawnArrivalEffects();
        }

        private void SpawnArrivalEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 18; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2f, 7f);
                Dust dust = Dust.NewDustPerfect(startPoint + Main.rand.NextVector2Circular(22f, 28f),
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.Blood, velocity, 90,
                    Main.rand.NextBool() ? DarkPurple : BloodRed, Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }
        }

        private void EmitDescentEffects(float descentProgress)
        {
            if (Main.dedServ)
                return;

            // 坠落拖尾：剑身两侧的血紫流线随下坠速度拉长，剑尖曳出火花。
            Vector2 bladeDirection = currentAngle.ToRotationVector2();
            for (int i = 0; i < 2; i++)
            {
                Vector2 position = Owner.MountedCenter + bladeDirection * Main.rand.NextFloat(30f, BladeReach * 0.9f) * scale +
                    Main.rand.NextVector2Circular(14f, 6f);
                Vector2 velocity = -Vector2.UnitY * Main.rand.NextFloat(3f, 7f) * (0.4f + descentProgress);
                GeneralParticleHandler.SpawnParticle(new LineParticle(position, velocity, false,
                    Main.rand.Next(10, 17), Main.rand.NextFloat(0.4f, 0.75f),
                    Main.rand.NextBool(3) ? BloodRed : DarkPurple));
            }

            if (Main.rand.NextBool(2))
            {
                Vector2 tipPosition = Owner.MountedCenter + bladeDirection * BladeReach * scale * Main.rand.NextFloat(0.85f, 1f);
                GeneralParticleHandler.SpawnParticle(new CritSpark(tipPosition,
                    -Vector2.UnitY * Main.rand.NextFloat(2f, 5f) + Main.rand.NextVector2Circular(1.5f, 1.5f),
                    Color.White, Main.rand.NextBool() ? BloodRed : DarkPurple,
                    Main.rand.NextFloat(0.35f, 0.7f), Main.rand.Next(8, 14)));
            }
        }

        private void SpawnImpactEffects()
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 38; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.5f, 10f);
                Dust dust = Dust.NewDustPerfect(targetPoint + Main.rand.NextVector2Circular(28f, 24f),
                    Main.rand.NextBool() ? DustID.Shadowflame : DustID.Blood, velocity, 90,
                    Main.rand.NextBool() ? DarkPurple : BloodRed, Main.rand.NextFloat(1f, 1.9f));
                dust.noGravity = true;
            }

            GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(targetPoint, -Vector2.UnitY * 4f,
                BloodRed, new Vector2(1.2f, 2.6f), -MathHelper.PiOver2, 0.22f, 0.04f, 28));

            // 落点冲击补强：白炽核心强光 + 沿地面两侧喷溅的火花与闪星。
            GeneralParticleHandler.SpawnParticle(new StrongBloom(targetPoint, Vector2.Zero, PaleCore * 0.75f, 1.1f, 16));
            for (int i = 0; i < 14; i++)
            {
                float side = Main.rand.NextBool() ? 1f : -1f;
                Vector2 velocity = new Vector2(side * Main.rand.NextFloat(2f, 9f), -Main.rand.NextFloat(2f, 7.5f));
                GeneralParticleHandler.SpawnParticle(new CritSpark(targetPoint + new Vector2(side * Main.rand.NextFloat(0f, 46f), Main.rand.NextFloat(-10f, 6f)),
                    velocity, Color.White, Main.rand.NextBool() ? BloodRed : DarkPurple,
                    Main.rand.NextFloat(0.45f, 0.9f), Main.rand.Next(11, 19)));
            }

            for (int i = 0; i < 4; i++)
            {
                GeneralParticleHandler.SpawnParticle(new GenericSparkle(targetPoint + Main.rand.NextVector2Circular(52f, 22f),
                    -Vector2.UnitY * Main.rand.NextFloat(1f, 3.5f), Color.White, BloodRed,
                    Main.rand.NextFloat(0.5f, 0.85f), Main.rand.Next(12, 18), Main.rand.NextFloat(-0.12f, 0.12f), 2.4f));
            }

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), targetPoint, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), Math.Max(1, (int)(Projectile.damage * 0.52f)),
                Projectile.knockBack, Projectile.owner, 0.85f);

            for (int i = 0; i < 6; i++)
            {
                float angle = -MathHelper.PiOver2 + MathHelper.Lerp(-1.25f, 1.25f, i / 5f);
                Vector2 velocity = angle.ToRotationVector2() * Main.rand.NextFloat(8f, 13f);
                int soulType = i % 2 == 0 ? ModContent.ProjectileType<GaelGreatswordDarkSoul>() : ModContent.ProjectileType<GaelGreatswordVengefulSoul>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), targetPoint + Main.rand.NextVector2Circular(36f, 24f),
                    velocity, soulType, Math.Max(1, (int)(Projectile.damage * 0.28f)), 1f, Projectile.owner);
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D smear = ModContent.Request<Texture2D>("CalamityMod/Particles/VerticalSmearLarge").Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;
            Vector2 origin = new(0f, swordTexture.Height);
            float drawRotation = currentAngle + MathHelper.PiOver4;
            Vector2 drawPosition = Owner.MountedCenter - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float opacity = Utils.GetLerpValue(0f, 6f, timer, true) * Utils.GetLerpValue(Duration, Duration - 8f, timer, true);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(smear, drawPosition, null, BloodRed with { A = 0 } * opacity * 0.78f,
                drawRotation, smear.Size() * 0.5f, scale * 1.15f, SpriteEffects.None);
            Vector2 tip = Owner.MountedCenter + currentAngle.ToRotationVector2() * BladeReach * scale - Main.screenPosition;
            Main.EntitySpriteDraw(bloom, tip, null, Color.White with { A = 0 } * opacity * 0.5f,
                0f, bloom.Size() * 0.5f, scale * 0.56f, SpriteEffects.None);
            if (impactEffectsPlayed)
            {
                float ringOpacity = Utils.GetLerpValue(Duration, Duration - 14f, timer, true);
                Main.EntitySpriteDraw(ring, targetPoint - Main.screenPosition, null, PaleCore with { A = 0 } * ringOpacity * 0.72f,
                    timer * 0.08f, ring.Size() * 0.5f, ImpactRadius / ring.Width * 2f, SpriteEffects.None);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(swordTexture, drawPosition, null, lightColor, drawRotation, origin, scale, SpriteEffects.None);
            return false;
        }
    }

    internal sealed class GaelGreatswordGuardHoldout : ModProjectile
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GaelsGreatsword/NewLegendGaelsGreatsword";

        private const float SwordVisualScale = 1.5f;
        private const float BladeReach = 132f;

        private static readonly Color DarkPurple = new(58, 18, 112);
        private static readonly Color BloodRed = new(175, 14, 40);
        private static readonly Color PaleCore = new(225, 207, 245);

        private Player Owner => Main.player[Projectile.owner];
        private int timer;
        private float guardAngle;
        private float scale = 1f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 72;
            Projectile.friendly = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 4;
            Projectile.noEnchantmentVisuals = true;
        }

        public override bool? CanDamage() => false;

        public override void AI()
        {
            if (!Owner.active || Owner.dead || Owner.HeldItem.type != ModContent.ItemType<NewLegendGaelsGreatsword>())
            {
                Projectile.Kill();
                return;
            }

            GaelGreatswordPlayer gaelPlayer = Owner.GetModPlayer<GaelGreatswordPlayer>();
            if (gaelPlayer.GuardCooldown > 0 || !IsRightHeld())
            {
                Projectile.Kill();
                return;
            }

            timer++;
            scale = Owner.GetMeleeScale() * SwordVisualScale;
            Vector2 aim = Owner.MountedCenter.DirectionTo(NewLegendGaelsGreatsword.GetMouseWorld(Owner))
                .SafeNormalize(Vector2.UnitX * Owner.direction);
            Owner.direction = aim.X >= 0f ? 1 : -1;
            guardAngle = aim.ToRotation() - MathHelper.PiOver2 * Owner.direction;

            Projectile.Center = Owner.MountedCenter + new Vector2(Owner.direction * 12f, -4f);
            Projectile.rotation = guardAngle;
            Projectile.timeLeft = 4;

            Owner.heldProj = Projectile.whoAmI;
            Owner.itemTime = Math.Max(Owner.itemTime, 2);
            Owner.itemAnimation = Math.Max(Owner.itemAnimation, 2);
            Owner.itemLocation = Owner.Center;
            Owner.itemRotation = guardAngle;
            Owner.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, guardAngle - MathHelper.ToRadians(116f));
            Owner.SetCompositeArmBack(true, Player.CompositeArmStretchAmount.Full, guardAngle - MathHelper.ToRadians(152f));

            Vector2 guardCenter = Projectile.Center + guardAngle.ToRotationVector2() * BladeReach * scale * 0.52f;
            gaelPlayer.SetGuardActive(guardCenter, timer);

            // 持续举剑会缓慢积攒黑暗余烬，奖励沉住气的防守。
            if (timer % 45 == 0)
                gaelPlayer.AddDarkEmbers(1);

            EmitGuardEffects(guardCenter);
        }

        private bool IsRightHeld()
        {
            if (!Owner.channel)
                return false;
            if (Main.myPlayer != Projectile.owner)
                return true;

            return (Owner.Calamity().mouseRight || Main.mouseRight) && !Main.mapFullscreen && !Main.blockMouse && !Owner.mouseInterface;
        }

        private void EmitGuardEffects(Vector2 guardCenter)
        {
            if (Main.dedServ)
                return;

            Lighting.AddLight(guardCenter, 0.28f, 0.04f, 0.34f);
            if (timer % 4 != 0)
                return;

            Vector2 bladeDirection = guardAngle.ToRotationVector2();
            Vector2 side = bladeDirection.RotatedBy(MathHelper.PiOver2);
            Vector2 position = Projectile.Center + bladeDirection * Main.rand.NextFloat(30f, BladeReach) * scale + side * Main.rand.NextFloat(-12f, 12f);
            Vector2 velocity = side * Main.rand.NextFloat(-1.2f, 1.2f) - bladeDirection * Main.rand.NextFloat(0.4f, 1.8f);
            GeneralParticleHandler.SpawnParticle(new GlowOrbParticle(position, velocity, false,
                Main.rand.Next(18, 26), Main.rand.NextFloat(0.14f, 0.22f), Main.rand.NextBool() ? BloodRed : DarkPurple));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            Texture2D swordTexture = ModContent.Request<Texture2D>(Texture).Value;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D slash = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/Trails/SwordSlashTexture").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;
            Vector2 origin = new(0f, swordTexture.Height);
            Vector2 drawPosition = Projectile.Center - Main.screenPosition + new Vector2(0f, Owner.gfxOffY);
            float drawRotation = guardAngle + MathHelper.PiOver4;
            float opacity = Utils.GetLerpValue(0f, 8f, timer, true);
            Vector2 bladeCenter = Projectile.Center + guardAngle.ToRotationVector2() * BladeReach * scale * 0.58f - Main.screenPosition;

            GaelGreatswordPlayer gaelPlayer = Owner.GetModPlayer<GaelGreatswordPlayer>();
            float parryWindow = gaelPlayer.ParryWindowOpen
                ? Utils.GetLerpValue(GaelGreatswordPlayer.ParryWindowFrames, 0f, timer, true)
                : 0f;
            float parryFlash = Utils.GetLerpValue(0f, 30f, gaelPlayer.ParryFlashTimer, true);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(slash, bladeCenter, null, DarkPurple with { A = 0 } * opacity * 0.32f,
                guardAngle, slash.Size() * 0.5f, new Vector2(0.48f, 1.1f) * scale, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, bladeCenter, null, BloodRed with { A = 0 } * opacity * 0.28f,
                0f, bloom.Size() * 0.5f, scale * 0.5f, SpriteEffects.None);
            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, PaleCore with { A = 0 } * opacity * 0.18f,
                0f, bloom.Size() * 0.5f, scale * 0.34f, SpriteEffects.None);

            // 完美格挡窗口：剑身泛起苍白流光，一圈收缩的光环提示时机正在流逝。
            if (parryWindow > 0.01f)
            {
                for (int i = 0; i < 8; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 8f).ToRotationVector2() * 3.4f * parryWindow;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, PaleCore with { A = 0 } * parryWindow * 0.24f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                float ringScale = MathHelper.Lerp(0.42f, 1.15f, parryWindow) * scale;
                Main.EntitySpriteDraw(ring, bladeCenter, null, PaleCore with { A = 0 } * parryWindow * 0.55f,
                    timer * 0.12f, ring.Size() * 0.5f, ringScale * 0.4f, SpriteEffects.None);
            }

            // 弹反成功的瞬间：整把剑爆发白炽闪光。
            if (parryFlash > 0.01f)
            {
                for (int i = 0; i < 10; i++)
                {
                    Vector2 offset = (MathHelper.TwoPi * i / 10f).ToRotationVector2() * 5.5f * parryFlash;
                    Main.EntitySpriteDraw(swordTexture, drawPosition + offset, null, Color.White with { A = 0 } * parryFlash * 0.3f,
                        drawRotation, origin, scale, SpriteEffects.None);
                }

                Main.EntitySpriteDraw(bloom, bladeCenter, null, Color.White with { A = 0 } * parryFlash * 0.6f,
                    0f, bloom.Size() * 0.5f, scale * (0.6f + parryFlash * 0.5f), SpriteEffects.None);
            }
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

            if (timer == Duration - 12)
                ReleaseFinaleBurst();

            if (timer >= Duration)
                Projectile.Kill();
        }

        private void ReleaseFinaleBurst()
        {
            // 斗篷旋至终点并不悄然散去，而是把积攒的血与火一次性掀出去。
            Owner.Calamity().GeneralScreenShakePower = Math.Max(Owner.Calamity().GeneralScreenShakePower, 9f);
            SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.9f, Pitch = -0.35f }, Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.65f, Pitch = -0.4f }, Projectile.Center);

            if (!Main.dedServ)
            {
                GeneralParticleHandler.SpawnParticle(new StrongBloom(Projectile.Center, Vector2.Zero, new Color(238, 214, 250) * 0.8f, 1.6f, 18));
                GeneralParticleHandler.SpawnParticle(new DirectionalPulseRing(Projectile.Center, Vector2.Zero,
                    BloodRed, new Vector2(1f, 1f), 0f, 0.3f, 1.4f, 26));

                for (int i = 0; i < 24; i++)
                {
                    Vector2 velocity = (MathHelper.TwoPi * i / 24f).ToRotationVector2().RotatedByRandom(0.16f) * Main.rand.NextFloat(4f, 12f);
                    GeneralParticleHandler.SpawnParticle(new CritSpark(Projectile.Center + Main.rand.NextVector2Circular(30f, 30f),
                        velocity, Color.White, Main.rand.NextBool() ? BloodRed : SoulPurple,
                        Main.rand.NextFloat(0.45f, 0.95f), Main.rand.Next(12, 20)));
                }
            }

            if (Main.myPlayer != Projectile.owner)
                return;

            Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), Math.Max(1, (int)(Projectile.damage * 1.45f)),
                Projectile.knockBack + 3f, Projectile.owner, 1f);

            for (int i = 0; i < 8; i++)
            {
                Vector2 velocity = (spinRotation + MathHelper.TwoPi * i / 8f).ToRotationVector2() * Main.rand.NextFloat(9f, 12f);
                int soulType = i % 2 == 0
                    ? ModContent.ProjectileType<GaelGreatswordDarkSoul>()
                    : ModContent.ProjectileType<GaelGreatswordVengefulSoul>();
                Projectile.NewProjectile(Projectile.GetSource_FromThis(), Projectile.Center + velocity.SafeNormalize(Vector2.UnitY) * 40f,
                    velocity, soulType, Math.Max(1, (int)(Projectile.damage * 0.5f)), 1.5f, Projectile.owner);
            }
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (Main.myPlayer != Projectile.owner || Projectile.numHits % 4 != 0)
                return;

            int echoDamage = Math.Max(1, (int)(Projectile.damage * 0.32f));
            Projectile.NewProjectile(Projectile.GetSource_OnHit(target), target.Center, Vector2.Zero,
                ModContent.ProjectileType<GaelGreatswordBloodEcho>(), echoDamage, 1.5f, Projectile.owner, 1f);
        }

        private void FireDarkSouls()
        {
            int interval = Math.Max(4, 8 - GaelGreatswordProgression.GetStage());
            if (Main.myPlayer != Projectile.owner || timer % interval != 0)
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
                "CalamityMod/Items/Armor/Empyrean/EmpyreanCloak_Back",
                "CalamityMod/Items/Accessories/SandCloak",
                "CalamityMod/Items/Accessories/Wings/SilvaWings_Wings",
                "CalamityMod/Items/Accessories/Wings/TarragonWings_Wings",
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

    internal sealed class GaelGreatswordEmberUI : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private static readonly Color EmptyColor = new(82, 32, 116);
        private static readonly Color FilledColor = new(190, 22, 54);
        private static readonly Color ReadyColor = new(244, 202, 92);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead || owner.HeldItem.type != ModContent.ItemType<NewLegendGaelsGreatsword>())
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = owner.Top + new Vector2(0f, -28f);
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.myPlayer != Projectile.owner)
                return false;

            Player owner = Main.player[Projectile.owner];
            GaelGreatswordPlayer gaelPlayer = owner.GetModPlayer<GaelGreatswordPlayer>();
            Texture2D barBack = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFront = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;
            float progress = gaelPlayer.DarkEmberRatio;
            bool ready = gaelPlayer.DarkEmberReady;
            float flash = Utils.GetLerpValue(0f, 18f, gaelPlayer.DarkEmberFlashTimer, true);
            float pulse = ready ? 0.86f + MathF.Sin(Main.GlobalTimeWrappedHourly * 8f) * 0.12f : 0.78f + flash * 0.18f;
            Color barColor = ready
                ? Color.Lerp(FilledColor, ReadyColor, 0.58f + MathF.Sin(Main.GlobalTimeWrappedHourly * 5f) * 0.16f)
                : Color.Lerp(EmptyColor, FilledColor, progress);

            Vector2 barPosition = owner.Top - Main.screenPosition + new Vector2(-barBack.Width * 0.5f, -34f);
            Rectangle frame = new(0, 0, (int)MathF.Ceiling(barFront.Width * progress), barFront.Height);

            Main.spriteBatch.Draw(barBack, barPosition, Color.Black * 0.58f);
            if (frame.Width > 0)
                Main.spriteBatch.Draw(barFront, barPosition, frame, barColor * pulse);

            if (gaelPlayer.GuardCooldown > 0 || gaelPlayer.GuardFlashTimer > 0)
            {
                float guardProgress = 1f - gaelPlayer.GuardCooldownRatio;
                float guardFlash = Utils.GetLerpValue(0f, 18f, gaelPlayer.GuardFlashTimer, true);
                Vector2 guardBarPosition = barPosition + new Vector2(0f, barBack.Height + 3f);
                Rectangle guardFrame = new(0, 0, (int)MathF.Ceiling(barFront.Width * guardProgress), barFront.Height);
                Color guardColor = Color.Lerp(new Color(110, 34, 145), new Color(236, 64, 82), 0.45f + guardFlash * 0.3f);

                Main.spriteBatch.Draw(barBack, guardBarPosition, Color.Black * 0.45f);
                if (guardFrame.Width > 0)
                    Main.spriteBatch.Draw(barFront, guardBarPosition, guardFrame, guardColor * (0.62f + guardFlash * 0.28f));
            }

            if (ready || flash > 0f)
            {
                Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
                Vector2 center = barPosition + new Vector2(barBack.Width * 0.5f, barBack.Height * 0.5f);
                Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
                Main.EntitySpriteDraw(bloom, center, null, barColor with { A = 0 } * (ready ? 0.22f : 0.12f) * Math.Max(flash, 0.5f),
                    0f, bloom.Size() * 0.5f, ready ? 0.42f : 0.25f + flash * 0.14f, SpriteEffects.None);
                Main.spriteBatch.ExitShaderRegion();
            }

            return false;
        }
    }

    internal sealed class GaelGreatswordBloodEcho : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private const int Lifetime = 30;

        private static readonly Color SoulPurple = new(86, 28, 140);
        private static readonly Color BloodRed = new(176, 14, 42);
        private static readonly Color PaleCore = new(230, 214, 245);

        private float Power => MathHelper.Clamp(Projectile.ai[0], 0f, 1f);

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 260;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = Lifetime;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = -1;
            Projectile.noEnchantmentVisuals = true;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            Projectile.rotation += 0.08f + Power * 0.04f;
            float progress = Projectile.ai[1] / Lifetime;
            Lighting.AddLight(Projectile.Center, 0.28f + Power * 0.22f, 0.03f, 0.32f + Power * 0.22f);

            if (Projectile.ai[1] == 1f)
            {
                SoundEngine.PlaySound(SoundID.Item14 with { Volume = 0.38f, Pitch = -0.28f }, Projectile.Center);
                SpawnOpeningDust();
            }

            if (!Main.dedServ && Main.rand.NextBool(3))
            {
                float radius = GetRadius(progress);
                Vector2 position = Projectile.Center + Main.rand.NextVector2CircularEdge(radius, radius);
                Dust dust = Dust.NewDustPerfect(position, Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    position.DirectionFrom(Projectile.Center) * Main.rand.NextFloat(0.8f, 2.4f), 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(0.9f, 1.3f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage()
        {
            float progress = Projectile.ai[1] / Lifetime;
            return progress >= 0.12f && progress <= 0.55f ? null : false;
        }

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            float radius = GetRadius(Projectile.ai[1] / Lifetime);
            Vector2 closest = targetHitbox.ClosestPointInRect(Projectile.Center);
            return closest.Distance(Projectile.Center) <= radius ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.45f, MathHelper.Clamp(Projectile.numHits / 5f, 0f, 1f));
        }

        public override void OnKill(int timeLeft)
        {
            if (Main.dedServ)
                return;

            for (int i = 0; i < 10; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.2f, 4.2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(70f, 70f),
                    Main.rand.NextBool() ? DustID.Blood : DustID.Shadowflame, velocity, 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (Main.dedServ)
                return false;

            float progress = MathHelper.Clamp(Projectile.ai[1] / Lifetime, 0f, 1f);
            float opacity = Utils.GetLerpValue(1f, 5f, Projectile.ai[1], true) * Utils.GetLerpValue(Lifetime, Lifetime - 10f, Projectile.ai[1], true);
            float radius = GetRadius(progress);
            Color echoColor = Color.Lerp(SoulPurple, BloodRed, 0.48f + Power * 0.25f);
            Vector2 center = Projectile.Center - Main.screenPosition;
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ring = ModContent.Request<Texture2D>("CalamityMod/Particles/HollowCircleHardEdge").Value;

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, center, null, echoColor with { A = 0 } * opacity * 0.38f,
                0f, bloom.Size() * 0.5f, radius / bloom.Width * (1.15f + Power * 0.24f), SpriteEffects.None);
            Main.EntitySpriteDraw(ring, center, null, PaleCore with { A = 0 } * opacity * 0.72f,
                Projectile.rotation, ring.Size() * 0.5f, radius / ring.Width * 2f, SpriteEffects.None);
            Main.EntitySpriteDraw(ring, center, null, BloodRed with { A = 0 } * opacity * 0.44f,
                -Projectile.rotation * 0.7f, ring.Size() * 0.5f, radius / ring.Width * 1.35f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();
            return false;
        }

        private float GetRadius(float progress)
        {
            float eased = CalamityUtils.EaseInOutExp(MathHelper.Clamp(progress, 0f, 1f), 3f, 2f);
            return MathHelper.Lerp(28f, 132f + Power * 46f, eased);
        }

        private void SpawnOpeningDust()
        {
            if (Main.dedServ)
                return;

            int count = 16 + (int)(Power * 10f);
            for (int i = 0; i < count; i++)
            {
                Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, 5.4f + Power * 2f);
                Dust dust = Dust.NewDustPerfect(Projectile.Center, Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    velocity, 95, Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(1f, 1.55f));
                dust.noGravity = true;
            }
        }
    }

    internal sealed class GaelGreatswordBrimstoneDart : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/BrimstoneBarrage";

        private static readonly Color BloodRed = new(185, 14, 38);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 8;
            ProjectileID.Sets.TrailingMode[Type] = 0;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 44;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.32f, 0.02f, 0.05f);

            if (Projectile.ai[0] > 10f)
                CalamityUtils.HomeInOnNPC(Projectile, true, 760f, 16f, 18f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 12f,
                    DustID.Blood, -Projectile.velocity * 0.14f, 100, BloodRed, Main.rand.NextFloat(0.8f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.58f, MathHelper.Clamp(Projectile.numHits / 2f, 0f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], lightColor, 1);
            return false;
        }
    }

    internal sealed class GaelGreatswordVengefulSoul : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Magic/RedirectingVengefulSoul";

        private static readonly Color SoulPurple = new(95, 36, 150);
        private static readonly Color BloodRed = new(170, 12, 42);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 34;
            Projectile.height = 48;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 3;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 13;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Lighting.AddLight(Projectile.Center, 0.18f, 0.04f, 0.32f);

            if (Projectile.ai[0] > 14f)
                CalamityUtils.HomeInOnNPC(Projectile, true, 920f, 14f, 22f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 6 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center + Main.rand.NextVector2Circular(9f, 12f),
                    Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    -Projectile.velocity.SafeNormalize(Vector2.UnitY) * Main.rand.NextFloat(0.4f, 1.6f), 120,
                    Main.rand.NextBool() ? SoulPurple : BloodRed, Main.rand.NextFloat(0.85f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.52f, MathHelper.Clamp(Projectile.numHits / 3f, 0f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(lightColor, SoulPurple, 0.35f), 1);
            return false;
        }
    }

    internal sealed class GaelGreatswordCatastropheSlash : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Boss/SupremeCatastropheSlash";

        private static readonly Color BloodRed = new(190, 18, 38);
        private static readonly Color SoulPurple = new(95, 28, 150);

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 4;
            ProjectileID.Sets.TrailCacheLength[Type] = 7;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 116;
            Projectile.height = 70;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 4;
            Projectile.timeLeft = 42;
            Projectile.scale = 1.08f;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 10;
        }

        public override void AI()
        {
            Projectile.ai[1]++;
            Projectile.rotation = Projectile.ai[0] == 0f ? Projectile.velocity.ToRotation() : Projectile.ai[0];
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Projectile.velocity *= 0.985f;
            Lighting.AddLight(Projectile.Center, 0.36f, 0.02f, 0.18f);

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 4 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Main.rand.NextBool(2))
            {
                Vector2 side = Projectile.rotation.ToRotationVector2().RotatedBy(MathHelper.PiOver2);
                Dust dust = Dust.NewDustPerfect(Projectile.Center + side * Main.rand.NextFloat(-52f, 52f),
                    Main.rand.NextBool(3) ? DustID.Blood : DustID.Shadowflame,
                    side * Main.rand.NextFloat(-1.8f, 1.8f), 100,
                    Main.rand.NextBool() ? BloodRed : SoulPurple, Main.rand.NextFloat(1f, 1.45f));
                dust.noGravity = true;
            }
        }

        public override bool? CanDamage() => Projectile.ai[1] >= 3f && Projectile.ai[1] <= 34f ? null : false;

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            Vector2 direction = Projectile.rotation.ToRotationVector2();
            float collisionPoint = 0f;
            return Collision.CheckAABBvLineCollision(targetHitbox.TopLeft(), targetHitbox.Size(),
                Projectile.Center - direction * 82f, Projectile.Center + direction * 104f,
                42f * Projectile.scale, ref collisionPoint) ? null : false;
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers)
        {
            modifiers.SourceDamage *= MathHelper.Lerp(1f, 0.5f, MathHelper.Clamp(Projectile.numHits / 4f, 0f, 1f));
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipVertically : SpriteEffects.None;
            float opacity = Utils.GetLerpValue(42f, 30f, Projectile.timeLeft, true);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, frame, BloodRed with { A = 0 } * completion * 0.36f * opacity,
                    Projectile.rotation, origin, Projectile.scale * (1f + completion * 0.25f), effects);
            }
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.Lerp(lightColor, Color.White, 0.35f) * opacity,
                Projectile.rotation, origin, Projectile.scale, effects);
            return false;
        }
    }

    internal sealed class GaelGreatswordCondemnationArrow : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Ranged/CondemnationArrow";

        private static readonly Color BloodRed = new(180, 18, 40);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 9;
            ProjectileID.Sets.TrailingMode[Type] = 2;
        }

        public override void SetDefaults()
        {
            Projectile.width = 20;
            Projectile.height = 54;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = 2;
            Projectile.timeLeft = 150;
            Projectile.extraUpdates = 1;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void AI()
        {
            Projectile.ai[0]++;
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2;
            Lighting.AddLight(Projectile.Center, 0.2f, 0.02f, 0.16f);

            if (Projectile.ai[0] > 18f)
                CalamityUtils.HomeInOnNPC(Projectile, true, 860f, 18f, 14f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustPerfect(Projectile.Center - Projectile.velocity.SafeNormalize(Vector2.UnitY) * 10f,
                    DustID.Blood, -Projectile.velocity * 0.12f, 100, BloodRed, Main.rand.NextFloat(0.8f, 1.1f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            CalamityUtils.DrawAfterimagesCentered(Projectile, ProjectileID.Sets.TrailingMode[Type], Color.Lerp(lightColor, BloodRed, 0.3f), 1);
            return false;
        }
    }

    internal sealed class GaelGreatswordDarkSoul : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/Melee/GaelSkull";

        private static readonly Color SoulPurple = new(85, 30, 135);
        private static readonly Color BloodRed = new(150, 8, 32);

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Type] = 10;
            ProjectileID.Sets.TrailingMode[Type] = 2;
            Main.projFrames[Type] = 5;
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
            Projectile.spriteDirection = Projectile.velocity.X >= 0f ? 1 : -1;
            Lighting.AddLight(Projectile.Center, 0.22f, 0.04f, 0.32f);

            if (Projectile.ai[1] > 12f)
            {
                NPC target = GetStoredTarget();
                if (target != null)
                    HomeToward(target);
                else
                    CalamityUtils.HomeInOnNPC(Projectile, true, 900f, 15f, 26f);
            }

            Projectile.frameCounter++;
            if (Projectile.frameCounter % 5 == 0)
                Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];

            if (Projectile.scale > 1.2f)
            {
                Projectile.velocity *= 1.012f;
                Projectile.alpha += 2;
                if (Projectile.alpha >= 245)
                    Projectile.Kill();
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

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Main.player[Projectile.owner].GetModPlayer<GaelGreatswordPlayer>().AddDarkEmbers(3 + GaelGreatswordProgression.GetStage() / 2);
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
            Texture2D texture = ModContent.Request<Texture2D>(Texture).Value;
            Rectangle frame = texture.Frame(1, Main.projFrames[Type], 0, Projectile.frame);
            Texture2D bloom = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Vector2 origin = frame.Size() * 0.5f;
            SpriteEffects effects = Projectile.spriteDirection == -1 ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
            Color drawColor = Color.Lerp(SoulPurple, BloodRed, 0.35f + MathF.Sin(Projectile.ai[1] * 0.08f) * 0.2f);

            Main.spriteBatch.EnterShaderRegion(BlendState.Additive);
            for (int i = 0; i < Projectile.oldPos.Length; i++)
            {
                float completion = 1f - i / (float)Projectile.oldPos.Length;
                Vector2 drawPosition = Projectile.oldPos[i] + Projectile.Size * 0.5f - Main.screenPosition;
                Main.EntitySpriteDraw(texture, drawPosition, frame, drawColor with { A = 0 } * completion * 0.35f,
                    Projectile.rotation, origin, Projectile.scale * (0.75f + completion * 0.35f), effects);
            }

            Main.EntitySpriteDraw(bloom, Projectile.Center - Main.screenPosition, null, drawColor with { A = 0 } * 0.42f,
                0f, bloom.Size() * 0.5f, Projectile.scale * 0.72f, SpriteEffects.None);
            Main.spriteBatch.ExitShaderRegion();

            Main.EntitySpriteDraw(texture, Projectile.Center - Main.screenPosition, frame, Color.White,
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
