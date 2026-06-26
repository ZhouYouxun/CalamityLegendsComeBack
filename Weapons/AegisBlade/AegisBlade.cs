using CalamityLegendsComeBack.Weapons.AegisBlade.EXSkill;
using CalamityLegendsComeBack.Weapons.AegisBlade.Projectiles;
using CalamityMod;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
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
        private static int UltimateHandType  => ModContent.ProjectileType<AegisUltimateHand>();

        public override void SetDefaults()
        {
            Item.width  = 78;
            Item.height = 78;
            Item.damage = BalanceAegisBlade.GetInitialLeftClickDamage();
            Item.DamageType  = DamageClass.MeleeNoSpeed;
            Item.noMelee     = true;
            Item.noUseGraphic = true;
            Item.knockBack   = 8f;
            Item.channel     = true;
            Item.useStyle    = ItemUseStyleID.Shoot;
            Item.useAnimation = 28;
            Item.useTime      = 28;
            Item.autoReuse    = true;
            Item.shoot        = SwingHoldoutType;
            Item.shootSpeed   = 0f;
            Item.UseSound     = null;
            Item.value        = Item.sellPrice(0, 20);
            Item.rare         = ItemRarityID.Red;
        }

        public override bool CanUseItem(Player player)
        {
            AegisBladePlayer bp = player.GetModPlayer<AegisBladePlayer>();
            // 举盾中（含蓄力状态）不允许触发左键挥舞
            if (bp.ShieldRaising || bp.ShieldRaised || bp.ShieldCharging || bp.ShieldFullyCharged)
                return false;
            if (player.ownedProjectileCounts[SwingHoldoutType] > 0)
                return false;
            return base.CanUseItem(player);
        }

        public override bool CanShoot(Player player)
        {
            AegisBladePlayer bp = player.GetModPlayer<AegisBladePlayer>();
            return !bp.ShieldRaising && !bp.ShieldRaised &&
                   !bp.ShieldCharging && !bp.ShieldFullyCharged &&
                   player.ownedProjectileCounts[SwingHoldoutType] == 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source,
            Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            Vector2 aimDir = (GetMouseWorld(player) - player.MountedCenter)
                             .SafeNormalize(Vector2.UnitX * player.direction);
            Projectile.NewProjectile(source, player.MountedCenter, aimDir,
                SwingHoldoutType, damage, knockback, player.whoAmI);
            return false;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            player.Calamity().rightClickListener = true;

            // 大招能量显示：同步到CalamityMod CooldownHandler（左上角技能图标）
            if (Main.myPlayer == player.whoAmI)
            {
                AegisBladePlayer bp = player.GetModPlayer<AegisBladePlayer>();
                if (player.Calamity().cooldowns.TryGetValue(AegisEXCooldown.ID, out var exCooldown))
                    exCooldown.timeLeft = (int)bp.AegisEnergy;
                else
                    player.AddCooldown(AegisEXCooldown.ID, (int)BalanceAegisBlade.EnergyMax).timeLeft = (int)bp.AegisEnergy;
            }

            if (Main.myPlayer != player.whoAmI) return;

            AegisBladePlayer bp = player.GetModPlayer<AegisBladePlayer>();

            // ── 右键举盾（在挥剑中不允许起手） ──────────────────────────
            if (CanStartShieldHoldout(player, bp))
            {
                int shieldDamage = (int)player.GetTotalDamage(DamageClass.Melee)
                                         .ApplyTo(balance.GetShieldBashDamage());
                Vector2 aimDir = (GetMouseWorld(player) - player.MountedCenter)
                                 .SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(Item.GetSource_FromThis(), player.MountedCenter, aimDir,
                    ShieldHoldoutType, shieldDamage, Item.knockBack, player.whoAmI);
            }

            // ── 终结技激活 ───────────────────────────────────────────────
            if (KeybindSystem.LegendarySkill.JustPressed &&
                bp.CanActivateUltimate &&
                player.ownedProjectileCounts[UltimateHandType] == 0)
            {
                Vector2 spawnOffset = new Vector2(Main.rand.NextFloat(-1f, 1f), -1f)
                                      .SafeNormalize(Vector2.UnitY) * 450f;
                Projectile.NewProjectile(Item.GetSource_FromThis(),
                    player.Center + spawnOffset, Vector2.Zero,
                    UltimateHandType, 0, 0f, player.whoAmI);
                SoundEngine.PlaySound(SoundID.Item74 with { Volume = 0.85f, Pitch = -0.4f }, player.Center);
            }
        }

        private bool CanStartShieldHoldout(Player player, AegisBladePlayer bp)
        {
            return player.Calamity().mouseRight
                   && !bp.IsSwinging
                   && player.ownedProjectileCounts[ShieldHoldoutType] == 0
                   && player.ownedProjectileCounts[SwingHoldoutType] == 0
                   && !player.noItems && !player.CCed
                   && !Main.mapFullscreen && !Main.blockMouse && !player.mouseInterface;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            damage.Base += balance.GetLeftClickBaseDamage() - Item.damage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            bool shiftPressed = Main.keyState.PressingShift();
            string legendarySection = shiftPressed
                ? this.GetLocalizedValue("LegendaryText")
                : this.GetLocalizedValue("LegendaryHint");

            if (shiftPressed)
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            else
                tooltips.FindAndReplace("[GFB]", this.GetLocalizedValue("FunctionalTooltip"));

            tooltips.Add(new TooltipLine(Mod, "AegisBladeHolyKnightLegendaryText", legendarySection));
        }

        internal static Vector2 GetMouseWorld(Player player)
        {
            Vector2 mouseWorld = player.Calamity().mouseWorld;
            return mouseWorld == Vector2.Zero ? Main.MouseWorld : mouseWorld;
        }
    }
}
