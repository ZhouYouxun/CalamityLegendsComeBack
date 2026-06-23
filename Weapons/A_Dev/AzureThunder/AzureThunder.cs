using System.Collections.Generic;
using CalamityLegendsComeBack.Accssory.TS;
using CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder.Passive;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.AzureThunder
{
    // 青霆剑物品本体：负责基础属性、左键挥舞入口、右键召剑入口和 tooltip 组装。
    public class AzureThunder : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";

        public override void SetDefaults()
        {
            // 基础武器参数：魔法伤害，使用自定义 holdout 弹幕承载挥舞逻辑。
            Item.width = 86;
            Item.height = 86;
            Item.damage = 175;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 0;
            Item.useTime = 20;
            Item.useAnimation = 20;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.autoReuse = true;
            Item.knockBack = 5.5f;
            Item.shoot = ModContent.ProjectileType<AzureThunderSwingHoldout>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;
            Item.rare = ItemRarityID.Red;
            Item.value = Item.sellPrice(0, 20);
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            // 右键不走 tModLoader 默认 Shoot 流程，统一在 HoldItem 里监听鼠标右键。
            if (player.altFunctionUse == 2)
                return false;

            // 每次左键前重置使用样式，避免其他逻辑修改物品状态后残留。
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.noUseGraphic = true;
            Item.channel = true;
            Item.shoot = ModContent.ProjectileType<AzureThunderSwingHoldout>();
            Item.shootSpeed = 0f;
            Item.UseSound = null;

            // 同一玩家只允许存在一个挥舞 holdout，防止连点生成多个近战判定。
            if (player.ownedProjectileCounts[ModContent.ProjectileType<AzureThunderSwingHoldout>()] > 0)
                return false;

            // 左键每段攻击都要消耗魔力，CanUseItem 先做一次可用性检查。
            return player.CheckMana(Item, AzureThunderPlayer.LeftAttackManaCost, false, false);
        }

        public override bool CanShoot(Player player)
        {
            return player.altFunctionUse != 2 &&
                player.ownedProjectileCounts[ModContent.ProjectileType<AzureThunderSwingHoldout>()] <= 0;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 右键由手动监听处理，这里只生成左键挥舞弹幕。
            if (player.altFunctionUse == 2)
                return false;

            Projectile.NewProjectile(
                source,
                player.MountedCenter,
                Vector2.Zero,
                ModContent.ProjectileType<AzureThunderSwingHoldout>(),
                damage,
                knockback,
                player.whoAmI);

            return false;
        }

        public override void HoldItem(Player player)
        {
            // 标记玩家正在手持青霆剑，供被动、冷却 UI 和终极技系统读取。
            AzureThunderPlayer thunderPlayer = player.GetModPlayer<AzureThunderPlayer>();
            thunderPlayer.SetHoldingAzureThunder();
            player.GetModPlayer<AzureThunderPassivePlayer>().SetHoldingAzureThunder();

            // 开启灾厄扩展鼠标监听，右键逻辑需要读取精确鼠标世界坐标。
            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            // 只有本地玩家处理输入，避免多人同步时远端重复触发右键。
            if (Main.myPlayer != player.whoAmI)
                return;

            // 地图、方块交互、物品栏悬停本物品时跳过右键施法，避免误触。
            if (!player.Calamity().mouseRight ||
                !Main.mouseRightRelease ||
                Main.mapFullscreen ||
                Main.blockMouse ||
                Main.playerInventory && Main.HoverItem.type == Item.type)
            {
                return;
            }

            TryUseRightClick(player, thunderPlayer);
        }

        private void TryUseRightClick(Player player, AzureThunderPlayer thunderPlayer)
        {
            // 右键能力由荒漠灾虫进度解锁。
            if (!AzureThunderProgression.RightClickUnlocked)
                return;

            // 右键有独立冷却，防止短时间堆满地剑和序列雷击。
            if (thunderPlayer.RightClickCooldown > 0)
                return;

            // 右键是大消耗技能，魔力不足时给出通用缺魔提示。
            if (!thunderPlayer.TrySpendMana(AzureThunderPlayer.RightClickManaCost))
            {
                CombatText.NewText(player.Hitbox, new Color(255, 80, 80), Language.GetTextValue("Mods.CalamityLegendsComeBack.Common.NoMana"));
                return;
            }

            bool harmony = thunderPlayer.HarmonyActive;
            int consumedCharge = thunderPlayer.ConsumeThunderCharge();
            thunderPlayer.RestoreManaFromConsumedCharge(consumedCharge);
            if (harmony)
                AzureThunderPlayer.MakeRoomForGroundSwords(player, 3);

            int existingGroundSwords = AzureThunderPlayer.CountOwnedGroundSwords(player);

            // 天理真和期间仍沿用右键造剑逻辑，只改变后续雷击表现和范围。
            int spawnCount = harmony ? 3 : consumedCharge + (existingGroundSwords < 3 ? 1 : 0);
            spawnCount = Utils.Clamp(spawnCount, 0, AzureThunderGroundSword.MaxGroundSwords - existingGroundSwords);

            for (int i = 0; i < spawnCount; i++)
            {
                // 地剑围绕玩家随机落点生成，避免所有剑堆在同一位置。
                float angle = MathHelper.TwoPi * (i / (float)System.Math.Max(1, spawnCount)) + Main.rand.NextFloat(-0.2f, 0.2f);
                float radius = Main.rand.NextFloat(130f, 250f);
                Vector2 spawnPosition = player.Center + angle.ToRotationVector2() * radius;

                AzureThunderPlayer.SpawnGroundSword(player, spawnPosition, player.GetWeaponDamage(Item), Item.knockBack);
            }

            thunderPlayer.RestoreManaForOwnedSwords();
            thunderPlayer.RestoreLifeFromFourSymbols();
            player.Calamity().GeneralScreenShakePower = System.Math.Max(player.Calamity().GeneralScreenShakePower, harmony ? 7f : 5f);

            // 右键命中序列优先锁定鼠标附近目标；没有目标时只完成召剑和消耗。
            NPC target = AzureThunderPlayer.FindMouseNearestTarget(player);
            int activeSwordCount = AzureThunderPlayer.CountOwnedGroundSwords(player);
            player.GetModPlayer<AzureThunderAccessoryPlayer>().OnConsumeThunderCharge(consumedCharge, harmony, activeSwordCount, target);

            if (target != null)
            {
                // 序列器读取附近地剑数量决定雷击次数，ai[2] 同时编码消耗层数和终极模式。
                float swordEffectRadius = AzureThunderAccessoryPlayer.GetGroundSwordEffectRadius(player);
                int swordCount = System.Math.Max(1, AzureThunderPlayer.CountGroundSwordsNear(player, player.Center, swordEffectRadius));

                int encodedMode = (consumedCharge * 10) + (harmony ? 1 : 0);
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    target.Center,
                    Vector2.Zero,
                    ModContent.ProjectileType<AzureThunderStrikeSequencer>(),
                    player.GetWeaponDamage(Item),
                    Item.knockBack,
                    player.whoAmI,
                    target.whoAmI,
                    swordCount,
                    encodedMode);
            }

            // 成功施放后写入冷却并播放双层音效，终极模式会略微压低/抬高音调。
            thunderPlayer.RightClickCooldown = AzureThunderPlayer.RightClickCooldownMax;
            SoundEngine.PlaySound(SoundID.Item68 with { Volume = 0.82f, Pitch = harmony ? -0.08f : 0.02f }, player.Center);
            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.65f, Pitch = harmony ? 0.2f : 0f }, player.Center);
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // tooltip 中展示当前传奇技能键位；没有绑定时显示 Unbound。
            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().Count > 0
                ? KeybindSystem.LegendarySkill.GetAssignedKeys()[0]
                : "Unbound";

            // [GFB] 是本武器本地化文本里的占位符，这里按当前进度拼成完整说明。
            string text =
                this.GetLocalizedValue("AzureThunderLeft") + "\n\n" +
                this.GetLocalizedValue("AzureThunderRight") + "\n\n" +
                string.Format(this.GetLocalizedValue("AzureThunderUltimate"), keyText) + "\n\n" +
                this.GetLocalizedValue(GetHeavenEarthTooltipKey()) + "\n" +
                this.GetLocalizedValue(GetHeavensWardTooltipKey()) + "\n" +
                this.GetLocalizedValue(GetFourSymbolsTooltipKey()) + "\n\n" +
                this.GetLocalizedValue("AzureThunderThunderCharge") + "\n" +
                this.GetLocalizedValue("AzureThunderDot") + "\n\n" +
                this.GetLocalizedValue("AzureThunderFinal");
            tooltips.FindAndReplace("[GFB]", text);
        }

        private static string GetHeavenEarthTooltipKey()
        {
            // 天地造化随灾厄进度逐段升级，tooltip 只返回对应的本地化键。
            if (AzureThunderProgression.DownedYharon)
                return "AzureThunderPassiveHeavenEarth4";
            if (AzureThunderProgression.DownedFishron)
                return "AzureThunderPassiveHeavenEarth3";
            if (AzureThunderProgression.DownedEvilTier2)
                return "AzureThunderPassiveHeavenEarth2";
            if (AzureThunderProgression.DownedDesertScourge)
                return "AzureThunderPassiveHeavenEarth1";

            return "AzureThunderPassiveHeavenEarth0";
        }

        private static string GetHeavensWardTooltipKey()
        {
            // 承天之佑未解锁时显示灰色锁定说明，解锁后按进度显示治疗量。
            if (!AzureThunderProgression.DodgeUnlocked)
                return "AzureThunderPassiveHeavensWard0";
            if (AzureThunderProgression.DownedYharon)
                return "AzureThunderPassiveHeavensWard5";
            if (AzureThunderProgression.DownedMoonLord)
                return "AzureThunderPassiveHeavensWard4";
            if (AzureThunderProgression.DownedPlantera)
                return "AzureThunderPassiveHeavensWard3";
            if (AzureThunderProgression.DownedWallOfFlesh)
                return "AzureThunderPassiveHeavensWard2";

            return "AzureThunderPassiveHeavensWard1";
        }

        private static string GetFourSymbolsTooltipKey()
        {
            // 和合四象控制自动地剑数量和生命回复说明。
            if (!AzureThunderProgression.FourSymbolsUnlocked)
                return "AzureThunderPassiveFourSymbols0";
            if (AzureThunderProgression.DownedMoonLord)
                return "AzureThunderPassiveFourSymbols3";
            if (AzureThunderProgression.DownedFishron)
                return "AzureThunderPassiveFourSymbols2";

            return "AzureThunderPassiveFourSymbols1";
        }
    }
}
