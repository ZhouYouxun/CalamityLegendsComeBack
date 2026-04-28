using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using CalamityMod.World;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Text;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.SHPBow
{
    public class SHPBow : ModItem, ILocalizedModType
    {
        public const string TextureAssetPath = "CalamityLegendsComeBack/Weapons/A_Dev/SHPBow/SHPB";

        public override string Texture => TextureAssetPath;
        public new string LocalizationCategory => "Items.Weapons";

        private static int HoldoutType => ModContent.ProjectileType<SHPBowHoldout>();

        public override void SetDefaults()
        {
            Item.width = 68;
            Item.height = 126;
            Item.damage = 18;
            Item.DamageType = DamageClass.Ranged;
            Item.useAnimation = 10;
            Item.useTime = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 3.25f;
            Item.UseSound = null;
            Item.shoot = HoldoutType;
            Item.shootSpeed = 18f;
            Item.useAmmo = AmmoID.Arrow;
            Item.value = CalamityGlobalItem.RarityVioletBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            Item.Calamity().devItem = true;
        }

        public override bool CanUseItem(Player player) => false;

        public override bool CanShoot(Player player) => false;

        public override bool ConsumeItem(Player player) => false;

        public override void HoldItem(Player player)
        {
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            player.GetModPlayer<SHPBowPlayer>().SetHoldingSHPBow();

            if (Main.myPlayer == player.whoAmI && player.ownedProjectileCounts[HoldoutType] <= 0)
            {
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    Vector2.UnitX * player.direction,
                    HoldoutType,
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI);
            }
        }

        public override void UpdateInventory(Player player)
        {
            if (player.HeldItem.type != Type && player.ownedProjectileCounts[HoldoutType] <= 0)
                Item.noUseGraphic = true;
        }

        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            bool[] downStages =
            {
                NPC.downedBoss1,
                NPC.downedBoss2,
                DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator,
                NPC.downedBoss3,
                DownedBossSystem.downedSlimeGod,
                Main.hardMode,
                NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3,
                DownedBossSystem.downedCalamitasClone,
                NPC.downedPlantBoss,
                NPC.downedGolemBoss,
                NPC.downedAncientCultist,
                NPC.downedMoonlord,
                DownedBossSystem.downedProvidence,
                DownedBossSystem.downedSignus && DownedBossSystem.downedStormWeaver && DownedBossSystem.downedCeaselessVoid,
                DownedBossSystem.downedPolterghast,
                DownedBossSystem.downedDoG,
                DownedBossSystem.downedYharon,
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas,
                DownedBossSystem.downedPrimordialWyrm
            };

            int[] stageDamage =
            {
                18,
                24,
                30,
                38,
                48,
                76,
                110,
                135,
                168,
                198,
                235,
                310,
                380,
                445,
                520,
                650,
                820,
                1200,
                7777
            };

            int finalDamage = 18;
            for (int i = 0; i < downStages.Length; i++)
            {
                if (downStages[i])
                    finalDamage = stageDamage[i];
                else
                    break;
            }

            damage.Base = finalDamage;
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            SHPBowPlayer bowPlayer = Main.LocalPlayer.GetModPlayer<SHPBowPlayer>();
            string sequenceText = BuildSequenceText(bowPlayer);

            string merged =
                string.Format(this.GetLocalizedValue("SHPB_CurrentSequence"), sequenceText) + "\n" +
                this.GetLocalizedValue("SHPB_Left") + "\n" +
                this.GetLocalizedValue("SHPB_StackHint") + "\n\n" +
                this.GetLocalizedValue("SHPB_Right") + "\n\n" +
                this.GetLocalizedValue("SHPB_Mode0") + "\n" +
                this.GetLocalizedValue("SHPB_Mode1") + "\n" +
                this.GetLocalizedValue("SHPB_Mode2") + "\n" +
                this.GetLocalizedValue("SHPB_Mode3") + "\n\n" +
                this.GetLocalizedValue("SHPB_EX") + "\n\n" +
                this.GetLocalizedValue("SHPB_Final") + "\n";

            tooltips.FindAndReplace("[GFB]", merged);
        }

        private string BuildSequenceText(SHPBowPlayer bowPlayer)
        {
            StringBuilder builder = new();

            for (int i = 0; i < bowPlayer.SequenceLength; i++)
            {
                if (i > 0)
                    builder.Append(" > ");

                builder.Append(this.GetLocalizedValue($"ModeName{(int)bowPlayer.GetSequenceMode(i)}"));
            }

            return builder.ToString();
        }
    }
}
