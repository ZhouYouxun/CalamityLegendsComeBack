using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Magic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class NewLegendSHPC : LegendaryItem, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";

        public new string LocalizationCategory => "Items.Weapons.Magic";

        // ==================== 音效部分（完整保留骨架） ====================
        public static readonly SoundStyle FireSound = new("CalamityMod/Sounds/Item/AnomalysNanogunMPFBShot");
        public static readonly SoundStyle VacuumStart = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumStart") { Volume = 0.5f };
        public static readonly SoundStyle VacuumLoop = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumLoop") { Volume = 0.5f };
        public static readonly SoundStyle VacuumEnd = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumEnd") { Volume = 0.5f };

        // ==================== 基础常量 ====================
        // 一颗弹药能灌注多少发
        //public const int ShotsPerEffectAmmo = 50;

        // ==================== 当前灌注状态 ====================
        // 当前还剩多少发
        public int storedEffectPower = 0;

        // 当前灌注用的弹药类型
        public int storedAmmoType = ItemID.None;

        // 当前灌注得到的效果ID
        public int storedEffectID = 0;

        // 后坐力动画计数
        public int recoilProgress = 0;

        public override Color? TooltipExtensionColor => new Color(31, 251, 255);

        public override void SetDefaults()
        {
            Item.width = 124;
            Item.height = 52;
            Item.damage = 117;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 15;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            Item.UseSound = FireSound;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<NewLegendSHPB>();
            Item.shootSpeed = 20f;

            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ItemRarityID.Pink;
        }

        // ==================== 通用工具函数 ====================

        // 查找当前玩家背包/弹药栏中，是否存在一个“被注册表认可的灌注弹药”
        // 现在虽然你只会注册 EnergyCore_Effect 对应的那一种，
        // 但结构上已经允许以后扩成很多种。
        public static int FindEffectAmmo(Player player)
        {
            int ammoType = -1;
            bool foundInAmmoSlots = false;

            // 先检查弹药栏
            for (int i = 54; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && EffectRegistry.IsRegisteredAmmo(item.type))
                {
                    ammoType = item.type;
                    foundInAmmoSlots = true;
                    break;
                }
            }

            // 如果弹药栏没找到，再检查普通背包
            if (!foundInAmmoSlots)
            {
                for (int i = 0; i < 54; i++)
                {
                    Item item = player.inventory[i];
                    if (item != null && item.stack > 0 && EffectRegistry.IsRegisteredAmmo(item.type))
                    {
                        ammoType = item.type;
                        break;
                    }
                }
            }

            return ammoType;
        }

        // 根据当前效果ID获取主题色
        public Color FindColorForCurrentEffect()
        {
            RulesOfEffect effect = EffectRegistry.GetEffectByID(storedEffectID);
            if (effect != null)
                return effect.ThemeColor;

            return Color.DarkGray;
        }

        // 将当前效果ID传给弹幕
        public int TransferEffectToProj()
        {
            return storedEffectID;
        }

        // 获取当前灌注弹药显示名，用于 Tooltip / UI
        public string GetCurrentAmmoDisplayName()
        {
            if (storedAmmoType <= ItemID.None)
                return "None";

            return Lang.GetItemNameValue(storedAmmoType);
        }

        public override Vector2? HoldoutOffset() => new Vector2(-35f, -10f);

        // 右键暂时关闭
        public override bool AltFunctionUse(Player player) => false;

        public override void OnCreated(ItemCreationContext context)
        {
            // 和原版一样：通过合成生成的武器，默认自带满灌注
            if (context is RecipeItemCreationContext)
                storedEffectPower = EffectRegistry.GetEffectByID(-1).ShotsPerAmmo;
        }

        // ==================== 开火相关 ====================
        public override bool CanUseItem(Player player)
        {
            // 右键先完全关闭，所以这里只保留左键逻辑
            Item.channel = false;
            Item.noUseGraphic = false;
            Item.UseSound = FireSound;

            // 只要当前还有灌注次数，或者玩家包里还能找到可灌注弹药，就允许使用
            //return storedEffectPower > 0 || FindEffectAmmo(player) != -1;
            // 改主意了，没有弹药也允许开火，只是一切为默认
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // 左键开火时先消耗一发内部灌注
            if (storedEffectPower > 0)
                storedEffectPower--;

            // 如果已经打空，则自动重新装填一颗注册表认可的弹药
            if (storedEffectPower <= 0)
            {
                bool ammoConsumed = false;
                int ammoType = FindEffectAmmo(player);

                if (ammoType != -1)
                {
                    // 消耗这一颗弹药
                    player.ConsumeItem(ammoType);

                    // 记录当前灌注来源
                    storedAmmoType = ammoType;

                    // 从注册表查询它对应的效果ID
                    storedEffectID = EffectRegistry.GetEffectIDByAmmo(ammoType);

                    ammoConsumed = true;
                }

                // 成功装填后恢复发数
                if (ammoConsumed)
                    storedEffectPower = EffectRegistry.GetEffectByID(storedEffectID).ShotsPerAmmo;
            }

            return base.UseItem(player);
        }

        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            // 右键暂不做，这里先保持左键默认耗蓝
        }

        public override float UseSpeedMultiplier(Player player)
        {
            // 右键暂不做，保留默认速度
            return 1f;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 右键分支暂时关闭，只发射左键主能量球
            Projectile.NewProjectile(
                source,
                position + new Vector2(0f, -10f) + velocity * 3f,
                velocity,
                ModContent.ProjectileType<NewLegendSHPB>(),
                damage,
                knockback,
                player.whoAmI,
                storedEffectID > 0 ? storedEffectID : -1
            );

            return false;
        }

        // ==================== 背包UI显示 ====================
        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float barScale = 2.5f;
            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            Vector2 drawPos = position + new Vector2((frame.Width - barBG.Width * 0.5f) * scale, (frame.Height + 45f) * scale);
            int maxShots = EffectRegistry.GetEffectByID(storedEffectID).ShotsPerAmmo;
            Rectangle frameCrop = new Rectangle(
                0,
                0,
                (int)(storedEffectPower / (float)maxShots * barFG.Width),
                barFG.Height
            );

            Color colorBG = Color.Black;
            Color colorFG = Color.Lerp(Color.DarkGray, FindColorForCurrentEffect(), storedEffectPower / (float)maxShots);

            spriteBatch.Draw(barBG, drawPos, null, colorBG, 0f, origin, scale * barScale, 0, 0f);
            spriteBatch.Draw(barFG, drawPos, frameCrop, colorFG * 0.8f, 0f, origin, scale * barScale, 0, 0f);

            CalamityUtils.DrawBorderStringEightWay(
                spriteBatch,
                FontAssets.MouseText.Value,
                storedEffectPower.ToString(),
                drawPos + new Vector2(-200f, -60f) * scale,
                Color.GreenYellow,
                Color.Black,
                scale * 2.5f
            );
        }

        // ==================== 手持动画部分（完整保留核心） ====================
        public override void HoldItem(Player player)
        {
            // 鼠标世界坐标监听必须保留，动画和瞄准都靠它
            player.Calamity().mouseWorldListener = true;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // 始终朝向鼠标
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));

            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;
            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 35f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-35f, 0f);

            // 左键后坐力动画：完整保留
            recoilProgress++;
            if (recoilProgress < Item.useAnimation / 3)
            {
                itemPosition -= (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX) * (Item.useAnimation / 3 - recoilProgress) * 0.75f;
            }
            else
            {
                if (recoilProgress >= Item.useAnimation - 1)
                    recoilProgress = 0;
            }

            CalamityUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            // 前臂跟随鼠标方向，完整保留
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float rotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }



        // ==================== 额外伤害修正（完整保留） ====================
        public override void ModifyWeaponDamage(Player player, ref StatModifier damage)
        {
            // 从早到晚的成长阶段判定
            bool[] downStages =
            {
                NPC.downedBoss1, // 克眼
                NPC.downedBoss2, // 世吞 / 克脑
                DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator, // 腐巢 / 宿主
                NPC.downedBoss3, // 骷髅王
                DownedBossSystem.downedSlimeGod, // 史莱姆之神
                Main.hardMode, // 肉山后
                NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3, // 机械三王
                DownedBossSystem.downedCalamitasClone, // 灾厄之眼 / 灾影
                NPC.downedPlantBoss, // 世花
                NPC.downedGolemBoss, // 石巨人
                NPC.downedAncientCultist, // 拜月教徒
                NPC.downedMoonlord, // 月总
                DownedBossSystem.downedProvidence, // 亵渎天神
                DownedBossSystem.downedSignus && DownedBossSystem.downedStormWeaver && DownedBossSystem.downedCeaselessVoid, // 神使三兄弟
                DownedBossSystem.downedPolterghast, // 幽花
                DownedBossSystem.downedDoG, // 神吞
                DownedBossSystem.downedYharon, // 犽戎
                DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas, // 星流机甲 + 至尊灾厄
                DownedBossSystem.downedPrimordialWyrm // 始源妖龙
            };

            // 对应每个阶段的目标面板伤害
            int[] stageDamage =
            {
                117,  // 初始 / 克眼后
                128,  // 世吞 / 克脑
                140,  // 腐巢 / 宿主
                154,  // 骷髅王
                170,  // 史神
                188,  // 肉后
                210,  // 机械三王
                236,  // 灾影
                265,  // 世花
                298,  // 石巨人
                336,  // 拜月
                380,  // 月总
                430,  // 亵渎
                488,  // 神使三兄弟
                556,  // 幽花
                638,  // 神吞
                742,  // 犽戎
                870,  // 星流 + 女巫
                1020  // 始源妖龙
            };

            int finalDamage = 117; // 默认初始面板伤害

            // 从早到晚检查，取当前已到达的最高阶段面板
            for (int i = 0; i < downStages.Length; i++)
            {
                if (downStages[i])
                    finalDamage = stageDamage[i];
                else
                    break;
            }

            // 直接覆写面板基础伤害
            damage.Base = finalDamage;
        }





        // ==================== Tooltip 文本部分 ====================
        public override void ModifyTooltips(List<TooltipLine> list)
        {
            string currentAmmoName = storedEffectPower <= 0 ? "No Ammo Injected" : GetCurrentAmmoDisplayName();

            if (Main.zenithWorld)
            {
                list.FindAndReplace("[GFB]", Lang.SupportGlyphs(this.GetLocalizedValue("TooltipGFB")));
            }
            else
            {
                // 这里沿用原版动态替换的结构
                // 当前只显示“剩余灌注量 + 当前灌注弹药名”
                string text = $"Injected Ammo: {currentAmmoName}\nStored Power: {storedEffectPower}";
                list.FindAndReplace("[GFB]", text);
            }

            if (storedEffectID > 0)
            {
                string key = $"Mods.CalamityLegendsComeBack.SHPCAmmo{storedEffectID}";
                string text = Language.GetTextValue(key);

                list.Add(new TooltipLine(Mod, "EffectAmmoInfo", text)
                {
                    OverrideColor = FindColorForCurrentEffect()
                });
            }
        }



        public override bool CanRightClick()
        {
            return true; // 允许背包右键
        }

        public override void RightClick(Player player)
        {
            // 清空灌注
            storedEffectPower = 0;
            storedAmmoType = ItemID.None;
            storedEffectID = 0;

            // 播放一个提示音（可选）
            SoundEngine.PlaySound(SoundID.MenuClose, player.Center);
        }


        public override bool ConsumeItem(Player player)
        {
            return false; // ❗阻止右键把武器消耗掉
        }






        // ==================== 合成表（先保留原版） ====================
        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<PlasmaDriveCore>()
                .AddIngredient<SuspiciousScrap>(4)
                .AddRecipeGroup("AnyMythrilBar", 10)
                .AddIngredient(ItemID.SoulofFright, 5)
                .AddIngredient(ItemID.SoulofMight, 5)
                .AddIngredient(ItemID.SoulofSight, 5)
                .AddTile(TileID.MythrilAnvil)
                .Register();
        }

        // ==================== 物品复制 / 存档 / 联机同步（完整保留） ====================
        public override ModItem Clone(Item item)
        {
            ModItem clone = base.Clone(item);

            if (clone is NewLegendSHPC newItem && item.ModItem is NewLegendSHPC oldItem)
            {
                newItem.storedEffectPower = oldItem.storedEffectPower;
                newItem.storedAmmoType = oldItem.storedAmmoType;
                newItem.storedEffectID = oldItem.storedEffectID;
            }

            return clone;
        }

        public override void SaveData(TagCompound tag)
        {
            tag["storedEffectPower"] = storedEffectPower;
            tag["storedAmmoType"] = storedAmmoType;
            tag["storedEffectID"] = storedEffectID;
        }

        public override void LoadData(TagCompound tag)
        {
            storedEffectPower = tag.GetInt("storedEffectPower");
            storedAmmoType = tag.GetInt("storedAmmoType");
            storedEffectID = tag.GetInt("storedEffectID");
        }

        public override void NetSend(BinaryWriter writer)
        {
            writer.Write(storedEffectPower);
            writer.Write(storedAmmoType);
            writer.Write(storedEffectID);
        }

        public override void NetReceive(BinaryReader reader)
        {
            storedEffectPower = reader.ReadInt32();
            storedAmmoType = reader.ReadInt32();
            storedEffectID = reader.ReadInt32();
        }
    }
}










