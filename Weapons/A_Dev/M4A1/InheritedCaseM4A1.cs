using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.M4A1
{
    /// <summary>
    /// 传承武器箱·M4A1（Inherited Case, M4A1）。
    /// 非常态手持：左键呼出 M4A1 自动步枪持械（暖机自动射击 + 火箭），右键从武器箱展开便携重炮，
    /// 大招键在完全同步后展开武器箱进行复仇印记齐射。核心资源为战术同步率。
    /// </summary>
    public class InheritedCaseM4A1 : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Dev/M4A1/InheritedCase";

        private static int LeftHoldoutType => ModContent.ProjectileType<M4A1LeftHoldout>();
        private static int CannonType => ModContent.ProjectileType<M4A1CannonHoldout>();
        private static int UltimateType => ModContent.ProjectileType<M4A1UltimateHoldout>();

        // 右键长按检测状态（照 SHPC 手动读 Main.mouseRight，channel 只跟随左键）。
        private bool suppressWorldRightClickUntilRelease;
        private bool wasWorldRightClickInteractionActive;

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 104;
            Item.height = 44;
            Item.damage = BalanceM4A1.GetInitialItemDamage();
            Item.DamageType = DamageClass.Ranged;
            Item.useTime = 2;
            Item.useAnimation = 2;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noUseGraphic = true;
            Item.noMelee = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 4f;
            Item.UseSound = null;
            Item.shoot = LeftHoldoutType;
            Item.shootSpeed = 30f; // 更快的发射初速（弹速）
            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
        }

        public override bool AltFunctionUse(Player player) => true;
        public override bool CanUseItem(Player player) => false;
        public override bool CanShoot(Player player) => false;
        public override bool ConsumeItem(Player player) => false;

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            // 让 GetWeaponDamage(Item) 直接返回“当前进度的子弹基础伤害 + 玩家远程加成”。
            damage.Base += BalanceM4A1.GetBulletBaseDamage() - Item.damage;
        }

        public override void UpdateInventory(Player player)
        {
            Item.noUseGraphic = true;
        }

        public override void HoldItem(Player player)
        {
            M4A1Player mp = M4A1Player.Get(player);
            mp.SetHolding();

            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer != player.whoAmI)
                return;
            player.Calamity().rightClickListener = true;

            EnsureSyncBar(player);

            // ===== 大招：完全同步 + 大招键 =====
            if (mp.FullySynced &&
                KeybindSystem.LegendarySkill.JustPressed &&
                !HasActive(player, UltimateType))
            {
                InterruptAllHoldouts(player);
                Vector2 dir = AimDirection(player);
                SpawnHoldout(player, UltimateType, dir, player.GetWeaponDamage(Item), 0);
                return;
            }
            if (HasActive(player, UltimateType))
                return; // 大招进行中，屏蔽普通左右键

            // ===== 右键：便携重炮 =====
            if (CanStartRightClick(player))
            {
                if (!HasActive(player, CannonType))
                {
                    KillHoldout(player, LeftHoldoutType);
                    Vector2 dir = AimDirection(player);
                    // ai[0] 交由重炮在生成时读取消耗前阶段（Phase 3 内部消耗同步率）。
                    SpawnHoldout(player, CannonType, dir, player.GetWeaponDamage(Item), mp.SyncStage);
                }
                return; // 右键时不生成左键持械
            }

            // ===== 左键：暖机自动射击 =====
            bool leftHeld = Main.mouseLeft && CanUseWorldInput(player);
            if (leftHeld && !HasActive(player, LeftHoldoutType) && !HasActive(player, CannonType))
            {
                Vector2 dir = AimDirection(player);
                SpawnHoldout(player, LeftHoldoutType, dir, player.GetWeaponDamage(Item), 0);
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            System.Collections.Generic.List<string> assignedKeys = KeybindSystem.LegendarySkill.GetAssignedKeys();
            string keyText = assignedKeys.Count > 0
                ? assignedKeys[0]
                : (Language.ActiveCulture.Name.StartsWith("zh") ? "未绑定" : "Unbound");

            string intro = this.GetLocalizedValue("M4A1_Intro");
            string left = this.GetLocalizedValue("M4A1_LeftClick");
            string right = this.GetLocalizedValue("M4A1_RightClick");
            string mark = this.GetLocalizedValue("M4A1_Mark");
            string passive = this.GetLocalizedValue("M4A1_Passive");
            string ultimate = string.Format(this.GetLocalizedValue("M4A1_Ultimate"), keyText);

            string finalText = intro + "\n" + left + "\n" + right + "\n" + mark + "\n" + passive + "\n" + ultimate + "\n";
            tooltips.FindAndReplace("[GFB]", finalText);
        }

        // ===================================================================
        //  弹幕生成 / 伤害缩放
        // ===================================================================
        private void SpawnHoldout(Player player, int type, Vector2 direction, int damage, int aiState)
        {
            int index = Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.MountedCenter,
                direction,
                type,
                damage,
                Item.knockBack,
                player.whoAmI,
                aiState);

            if (Main.projectile.IndexInRange(index))
                Main.projectile[index].CritChance = player.GetWeaponCrit(Item);
        }

        private void EnsureSyncBar(Player player)
        {
            int barType = ModContent.ProjectileType<M4A1SyncBarProjectile>();
            if (player.ownedProjectileCounts[barType] > 0)
                return;

            Projectile.NewProjectile(Item.GetSource_FromThis(), player.Top, Vector2.Zero, barType, 0, 0f, player.whoAmI);
        }

        /// <summary>把任意弹种的“原始基础伤害”缩放到当前玩家远程加成后的最终伤害。</summary>
        public static int ScaledDamage(Player player, Item item, int rawBase)
        {
            int weaponBulletDamage = player.GetWeaponDamage(item);
            int bulletBase = Math.Max(1, BalanceM4A1.GetBulletBaseDamage());
            return Math.Max(1, (int)Math.Round(weaponBulletDamage * (rawBase / (float)bulletBase)));
        }

        private static Vector2 AimDirection(Player player) =>
            (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

        // ===================================================================
        //  持械存在性 / 清理
        // ===================================================================
        private static bool HasActive(Player player, int type)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                    return true;
            }
            return false;
        }

        private static void KillHoldout(Player player, int type)
        {
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.owner == player.whoAmI && p.type == type)
                {
                    p.Kill();
                    p.netUpdate = true;
                }
            }
        }

        private static void InterruptAllHoldouts(Player player)
        {
            KillHoldout(player, ModContent.ProjectileType<M4A1LeftHoldout>());
            KillHoldout(player, ModContent.ProjectileType<M4A1CannonHoldout>());
        }

        // ===================================================================
        //  输入判定（右键长按 = SHPC 同款抑制逻辑）
        // ===================================================================
        private bool CanStartRightClick(Player player)
        {
            if (player.whoAmI != Main.myPlayer)
                return false;

            bool rightClickHeld = player.Calamity().mouseRight || Main.mouseRight;
            bool interactionActive = IsWorldRightClickInteractionActive(player);

            if (!rightClickHeld)
            {
                suppressWorldRightClickUntilRelease = false;
                wasWorldRightClickInteractionActive = interactionActive;
                return false;
            }

            if (interactionActive || wasWorldRightClickInteractionActive)
                suppressWorldRightClickUntilRelease = true;

            wasWorldRightClickInteractionActive = interactionActive;

            if (!player.Calamity().mouseRight)
                return false;

            if (suppressWorldRightClickUntilRelease)
                return false;

            if (!CanUseWorldRightClick(player))
            {
                suppressWorldRightClickUntilRelease = true;
                return false;
            }

            return true;
        }

        internal static bool CanUseWorldInput(Player player)
        {
            if (player.noItems || player.CCed || Main.mapFullscreen || player.mouseInterface)
                return false;
            if (Main.blockMouse)
                return false;
            if (Main.playerInventory && !Main.HoverItem.IsAir)
                return false;
            return true;
        }

        internal static bool CanUseWorldRightClick(Player player)
        {
            if (player.noItems || player.CCed || Main.mapFullscreen || Main.blockMouse ||
                player.mouseInterface || IsWorldRightClickInteractionActive(player))
                return false;
            return true;
        }

        private static bool IsWorldRightClickInteractionActive(Player player)
        {
            return (Main.playerInventory && Main.HoverItem.type == ModContent.ItemType<InheritedCaseM4A1>()) ||
                   player.chest != -1 ||
                   player.sleeping.isSleeping ||
                   player.TalkNPC != null;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }
}
