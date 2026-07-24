using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using CalamityLegendsComeBack.Weapons.A_Tools.DebugTools;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Tools.EmergencyRecall
{
    public class EmergencyRecall : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "Terraria/Images/Item_" + ItemID.PotionOfReturn;

        public override bool PreDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame,
            Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            DebugToolOutline.Draw(spriteBatch, TextureAssets.Item[Type].Value, position, frame, origin, scale, new Color(130, 220, 255));
            return true;
        }

        public override void SetDefaults()
        {
            Item.width = 20;
            Item.height = 26;
            Item.value = Item.sellPrice(gold: 2);
            Item.rare = ItemRarityID.LightRed;
            Item.maxStack = 1;
        }

        public override void UpdateInventory(Player player)
        {
            if (Item.favorited)
            {
                player.GetModPlayer<EmergencyRecallPlayer>().HasEmergencyRecall = true;
            }
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.PotionOfReturn, 1)
                .AddIngredient(ItemID.RecallPotion, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }
    }

    public class EmergencyRecallPlayer : ModPlayer
    {
        public bool HasEmergencyRecall;

        public override void ResetEffects()
        {
            HasEmergencyRecall = false;
        }

        public override bool PreKill(double damage, int hitDirection, bool pvp, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource)
        {
            if (!HasEmergencyRecall)
                return true;

            // 如果有 Boss 存活，不触发应急回城
            if (AnyBossAlive())
                return true;

            // 扫描背包中的回城道具，优先级：
            // 1. 返回药水 (Potion of Return) - 消耗 1
            // 2. 回城药水 (Recall Potion) - 消耗 1
            // 3. 魔镜 / 冰雪镜 / 电话 / 贝壳 (Magic Mirror et al.) - 不消耗

            int slotToConsume = -1;
            int recallType = 0; // 1: 返回药水, 2: 回城药水, 3: 魔镜/无限道具

            // Priority 1: 返回药水
            for (int i = 0; i < 50; i++)
            {
                Item inv = Player.inventory[i];
                if (!inv.IsAir && inv.type == ItemID.PotionOfReturn && inv.stack > 0)
                {
                    slotToConsume = i;
                    recallType = 1;
                    break;
                }
            }

            // Priority 2: 回城药水
            if (recallType == 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    Item inv = Player.inventory[i];
                    if (!inv.IsAir && inv.type == ItemID.RecallPotion && inv.stack > 0)
                    {
                        slotToConsume = i;
                        recallType = 2;
                        break;
                    }
                }
            }

            // Priority 3: 魔镜及无限回城道具
            if (recallType == 0)
            {
                for (int i = 0; i < 50; i++)
                {
                    Item inv = Player.inventory[i];
                    if (!inv.IsAir && IsMirrorItem(inv.type))
                    {
                        slotToConsume = i;
                        recallType = 3;
                        break;
                    }
                }
            }

            // 如果背包里没有任何回城道具，不触发
            if (recallType == 0)
                return true;

            // 扣除消耗性药水
            if (recallType == 1 || recallType == 2)
            {
                Player.inventory[slotToConsume].stack--;
                if (Player.inventory[slotToConsume].stack <= 0)
                    Player.inventory[slotToConsume] = new Item();
            }

            // 恢复血量避免死亡
            Player.statLife = Player.statLifeMax2;

            // 传送前特效与音效
            SoundEngine.PlaySound(SoundID.Item6, Player.Center);
            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.MagicMirror, 0f, 0f, 150, default, 1.5f);
                d.velocity *= 2f;
            }

            // 执行回城传送
            Player.Spawn(PlayerSpawnContext.RecallFromItem);

            // 传送后到达 spawn 点特效与音效
            SoundEngine.PlaySound(SoundID.Item6, Player.Center);
            for (int i = 0; i < 30; i++)
            {
                Dust d = Dust.NewDustDirect(Player.position, Player.width, Player.height, DustID.MagicMirror, 0f, 0f, 150, default, 1.5f);
                d.velocity *= 2f;
            }

            // 战况文本提示
            CombatText.NewText(new Rectangle((int)Player.Center.X, (int)Player.Center.Y - 20, 1, 1), new Color(130, 220, 255), "应急回城！", true);

            playSound = false;
            genGore = false;

            return false; // 拦截死亡
        }

        private static bool IsMirrorItem(int itemType)
        {
            return itemType == ItemID.MagicMirror ||
                   itemType == ItemID.IceMirror ||
                   itemType == ItemID.CellPhone ||
                   itemType == ItemID.Shellphone ||
                   itemType == ItemID.ShellphoneSpawn ||
                   itemType == ItemID.ShellphoneOcean ||
                   itemType == ItemID.ShellphoneHell ||
                   itemType == ItemID.DemonConch ||
                   itemType == ItemID.MagicConch;
        }

        private static bool AnyBossAlive()
        {
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (npc.active && (npc.boss || npc.type == NPCID.EaterofWorldsHead || npc.type == NPCID.EaterofWorldsBody || npc.type == NPCID.EaterofWorldsTail))
                {
                    return true;
                }
            }
            return false;
        }
    }
}
