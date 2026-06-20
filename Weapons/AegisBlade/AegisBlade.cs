using CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.AegisBlade
{
    public class AegisBlade : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        private readonly BalanceAegisBlade balance = new();

        private static int SwingHoldoutType  => ModContent.ProjectileType<AegisSwingHoldout>();
        private static int ShieldHoldoutType => ModContent.ProjectileType<AegisShieldHoldout>();
        private static int EnergyUIType      => ModContent.ProjectileType<AegisEnergyUI>();
        private static int UltimateHandType  => ModContent.ProjectileType<AegisUltimateHand>();

        public override void SetDefaults()
        {
            Item.width = 78;
            Item.height = 78;
            Item.damage = BalanceAegisBlade.GetInitialLeftClickDamage();
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.knockBack = 8f;
            Item.channel = true;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.useAnimation = 28;
            Item.useTime = 28;
            Item.autoReuse = true;
            Item.shoot = SwingHoldoutType;
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.value = Item.sellPrice(0, 20);
            Item.rare = ItemRarityID.Red;
        }

        public override bool CanUseItem(Player player)
        {
            AegisBladePlayer bladePlayer = player.GetModPlayer<AegisBladePlayer>();
            if (bladePlayer.ShieldRaising || bladePlayer.ShieldRaised)
                return false;
            if (player.ownedProjectileCounts[SwingHoldoutType] > 0)
                return false;
            return base.CanUseItem(player);
        }

        public override bool CanShoot(Player player)
        {
            AegisBladePlayer bladePlayer = player.GetModPlayer<AegisBladePlayer>();
            return !bladePlayer.ShieldRaising && !bladePlayer.ShieldRaised &&
                   player.ownedProjectileCounts[SwingHoldoutType] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDir = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(source, player.MountedCenter, aimDir,
                SwingHoldoutType, damage, knockback, player.whoAmI);
            return false;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            player.Calamity().rightClickListener = true;  // 启用 CalamityMod 的右键长按追踪

            // 能量条 UI 只在本地玩家侧生成
            if (Main.myPlayer == player.whoAmI &&
                player.ownedProjectileCounts[EnergyUIType] == 0)
            {
                Projectile.NewProjectile(Item.GetSource_FromThis(), player.Center, Vector2.Zero,
                    EnergyUIType, 0, 0f, player.whoAmI);
            }

            if (Main.myPlayer != player.whoAmI)
                return;

            AegisBladePlayer bladePlayer = player.GetModPlayer<AegisBladePlayer>();

            // ── 右键举盾（SHPC 模式：不走 AltFunctionUse/Shoot，直接在 HoldItem 检测） ──
            if (CanStartShieldHoldout(player, bladePlayer))
            {
                int shieldDamage = (int)player.GetTotalDamage(DamageClass.Melee).ApplyTo(balance.GetShieldPhantomDamage());
                Vector2 aimDir = (GetMouseWorld(player) - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(Item.GetSource_FromThis(), player.MountedCenter, aimDir,
                    ShieldHoldoutType, shieldDamage, Item.knockBack, player.whoAmI);
            }

            // ── 终结技激活 ─────────────────────────────────────────────────
            if (KeybindSystem.LegendarySkill.JustPressed &&
                bladePlayer.CanActivateUltimate &&
                player.ownedProjectileCounts[UltimateHandType] == 0)
            {
                Vector2 spawnOffset = new Vector2(Main.rand.NextFloat(-1f, 1f), -1f).SafeNormalize(Vector2.UnitY) * 450f;
                Projectile.NewProjectile(Item.GetSource_FromThis(),
                    player.Center + spawnOffset, Vector2.Zero,
                    UltimateHandType, 0, 0f, player.whoAmI);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.4f }, player.Center);
            }
        }

        private bool CanStartShieldHoldout(Player player, AegisBladePlayer bladePlayer)
        {
            return player.Calamity().mouseRight           // 右键确实被按住（CalamityMod 长按追踪）
                   && !bladePlayer.IsSwinging             // 挥剑中的剑盾联动由 AegisSwingHoldout 内部处理
                   && player.ownedProjectileCounts[ShieldHoldoutType] == 0
                   && !player.noItems && !player.CCed
                   && !Main.mapFullscreen && !Main.blockMouse && !player.mouseInterface;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            // 同步 Item.damage 到当前进度阶段的基础伤害
            damage.Base += balance.GetLeftClickBaseDamage() - Item.damage;
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }
}
