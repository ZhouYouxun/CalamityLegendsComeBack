using System.Collections.Generic;
using System.Linq;
using CalamityLegendsComeBack.Weapons;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public class NewLegendCosmicDischarge : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/CosmicDischarge/CosmicDischarge";

        private static int HoldoutType => ModContent.ProjectileType<CosmicDischargeComboHoldout>();

        public override void SetDefaults()
        {
            Item.width = 50;
            Item.height = 52;
            Item.damage = CD_Balance.GetInitialBaseDamage();
            Item.knockBack = 6f;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = false;
            Item.autoReuse = true;
            Item.DamageType = DamageClass.MeleeNoSpeed;
            Item.useTime = 12;
            Item.useAnimation = 12;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.UseSound = null;
            Item.shootSpeed = 0f;
            Item.shoot = HoldoutType;
            Item.value = CalamityGlobalItem.RarityDarkBlueBuyPrice;
            Item.rare = ModContent.RarityType<CosmicPurple>();
        }

        public override bool AltFunctionUse(Player player) => true;
        public override bool MeleePrefix() => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                if (!TryRequestQuickDraw(player))
                    player.GetModPlayer<CosmicDischargePlayer>().ToggleAttackMode();

                Item.useTime = Item.useAnimation = 8;
                Item.shoot = ProjectileID.None;
                return false;
            }

            Item.useTime = Item.useAnimation = 12;
            Item.shoot = HoldoutType;
            return player.ownedProjectileCounts[HoldoutType] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            CosmicDischargePlayer dischargePlayer = player.GetModPlayer<CosmicDischargePlayer>();
            CosmicDischargeAttackMode mode = dischargePlayer.AttackMode;
            int comboIndex = dischargePlayer.ConsumeComboIndex(mode);
            CosmicDischargeAttackKind kind = GetAttackKind(mode, comboIndex);
            Vector2 aimDirection = CosmicDischargeCommon.GetAimDirection(player, Vector2.UnitX * player.direction);

            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                Vector2.Zero,
                HoldoutType,
                damage,
                knockback,
                player.whoAmI,
                (int)kind,
                aimDirection.ToRotation());

            return false;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            CosmicDischargeAttackMode mode = player.GetModPlayer<CosmicDischargePlayer>().AttackMode;
            damage.Base += CD_Balance.GetBaseDamage(mode) - Item.damage;
        }

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            player.GetModPlayer<CosmicDischargePlayer>().SetHoldingCosmicDischarge();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string text =
                this.GetLocalizedValue("CosmicDischargeLeft") + "\n\n" +
                this.GetLocalizedValue("CosmicDischargeRight") + "\n\n" +
                string.Format(this.GetLocalizedValue("CosmicDischargeUltimate"), keyText) + "\n\n" +
                this.GetLocalizedValue("CosmicDischargePassive");

            tooltips.FindAndReplace("[GFB]", text);
        }

        public override void AddRecipes()
        {
        }

        private static CosmicDischargeAttackKind GetAttackKind(CosmicDischargeAttackMode mode, int comboIndex)
        {
            if (mode == CosmicDischargeAttackMode.Whip)
            {
                return comboIndex switch
                {
                    0 => CosmicDischargeAttackKind.WhipOver,
                    1 => CosmicDischargeAttackKind.WhipUnder,
                    _ => CosmicDischargeAttackKind.WhipThrust
                };
            }
            else if (mode == CosmicDischargeAttackMode.Sword)
            {
                return comboIndex switch
                {
                    0 => CosmicDischargeAttackKind.SwordSwingOne,
                    1 => CosmicDischargeAttackKind.SwordSwingTwo,
                    _ => CosmicDischargeAttackKind.SwordFinisher
                };
            }
            else
            {
                return comboIndex switch
                {
                    0 => CosmicDischargeAttackKind.ChainKnifeSingle,
                    1 => CosmicDischargeAttackKind.ChainKnifeScatter,
                    _ => CosmicDischargeAttackKind.ChainKnifeBiteAll
                };
            }
        }

        private static bool TryRequestQuickDraw(Player player)
        {
            if (Main.myPlayer != player.whoAmI)
                return false;

            foreach (Projectile projectile in Main.ActiveProjectiles)
            {
                if (!projectile.active || projectile.owner != player.whoAmI || projectile.type != HoldoutType)
                    continue;

                if (projectile.ModProjectile is CosmicDischargeComboHoldout holdout && holdout.TryRequestQuickDraw())
                {
                    SoundEngine.PlaySound(SoundID.Item119 with { Volume = 0.72f, Pitch = 0.25f }, player.Center);
                    return true;
                }
            }

            SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.42f, Pitch = player.GetModPlayer<CosmicDischargePlayer>().AttackMode == CosmicDischargeAttackMode.Whip ? 0.15f : -0.1f }, player.Center);
            return false;
        }
    }
}
