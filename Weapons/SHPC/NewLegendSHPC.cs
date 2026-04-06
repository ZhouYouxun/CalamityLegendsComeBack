using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC.EXSkill;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Materials;
using CalamityMod.Items.Weapons.Magic;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.Localization;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;


namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class NewLegendSHPC : ModItem, ILocalizedModType
    {
        #region ===== 基础信息与运行时状态 =====

        #region ===== 资源与本地化 =====
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";
        public new string LocalizationCategory => "Items.Weapons";
        #endregion

        #region ===== 音效资源 =====

        // ==================== 音效部分 ====================
        public static readonly SoundStyle FireSound = new("CalamityMod/Sounds/Item/AnomalysNanogunMPFBShot");
        public static readonly SoundStyle VacuumStart = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumStart") { Volume = 0.5f };
        public static readonly SoundStyle VacuumLoop = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumLoop") { Volume = 0.5f };
        public static readonly SoundStyle VacuumEnd = new SoundStyle("CalamityMod/Sounds/Item/SHPCVacuumEnd") { Volume = 0.5f };

        public static readonly SoundStyle RocketLaunch = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/解放者机甲左手火箭弹") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle LightningChainRelease = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/雷霆开火与换弹") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle EnergyMinigunFire = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/双刃镰开火音效") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle EnergyMinigunSpinUp = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/双刃镰启动音效") { Volume = 1f, Pitch = 0f };

        public static readonly SoundStyle MortarSentryShot = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/迫击哨戒炮单次攻击") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle FinalUltimatumExplosion = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/最后通牒爆炸") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle Eagle500kgExplosion = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/飞鹰500KG爆炸") { Volume = 1f, Pitch = 0f };
        public static readonly SoundStyle AntiPersonnelMineExplosion = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/反步兵地雷爆炸") { Volume = 1f, Pitch = 0f };
        #endregion

        #region ===== 灌注与动画状态 =====

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
        #endregion

        #region ===== 天顶世界补射状态 =====
        // ===== 天顶世界三连发控制 =====
        private int zenithBurstTimer;
        private int zenithBurstCount;
        #endregion
        #endregion


        #region ===== 基础物品设定 =====
        public override void SetDefaults()
        {
            Item.width = 124;
            Item.height = 52;
            Item.damage = 11;
            Item.DamageType = DamageClass.Magic;
            Item.mana = 15;
            Item.useAnimation = 60;
            Item.useTime = 60;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.noMelee = true;
            Item.knockBack = 3f;
            if (Main.zenithWorld)
            {
                Item.UseSound = new SoundStyle("CalamityLegendsComeBack/Sound/SHPC/AWM开火")
                {
                    Volume = 1.5f,
                    Pitch = 0.1f
                };
            }
            else
            {
                Item.UseSound = FireSound;
            }
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<NewLegendSHPB>();
            Item.shootSpeed = 20f;
            Item.channel = false;

            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
            //Item.rare = ItemRarityID.Pink;
        }
        #endregion


        #region ===== 灌注读取与通用查询 =====

        #region ===== 弹药与效果查询 =====

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
        #endregion
        #endregion


        #region ===== 左键开火与通用发射流程 =====

        #region ===== 创建与手持偏移 =====
        public override Vector2? HoldoutOffset() => new Vector2(-35f, -10f);

        public override void OnCreated(ItemCreationContext context)
        {
            // 和原版一样：通过合成生成的武器，默认自带满灌注
            if (context is RecipeItemCreationContext)
                storedEffectPower = EffectRegistry.GetEffectByID(-1).ShotsPerAmmo;
        }
        #endregion

        #region ===== 使用条件与消耗 =====
        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.channel = true;         // ✅ 右键长按核心
                Item.noUseGraphic = true;    // ✅ 不画物品（Holdout接管）
                Item.UseSound = null;        // 可选
            }
            else
            {
                Item.channel = false;
                Item.noUseGraphic = false;
                Item.UseSound = FireSound;
            }

            // ===== 天顶世界三连发初始化 =====
            if (Main.zenithWorld)
            {
                zenithBurstCount = 2; // 还要补两发（总共3发）
                zenithBurstTimer = 8; // 间隔8帧
            }

            // 只要当前还有灌注次数，或者玩家包里还能找到可灌注弹药，就允许使用
            //return storedEffectPower > 0 || FindEffectAmmo(player) != -1;
            // 改主意了，没有弹药也允许开火，只是一切为默认
            return true;
        }

        public override bool? UseItem(Player player)
        {
            // ⭐ 右键：完全不参与弹药系统
            if (player.altFunctionUse == 2)
                return true;

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
        #endregion

        #region ===== 使用参数覆写 =====
        public override void ModifyManaCost(Player player, ref float reduce, ref float mult)
        {
            // 右键暂不做，这里先保持左键默认耗蓝
        }

        public override float UseSpeedMultiplier(Player player)
        {
            // 右键暂不做，保留默认速度
            return 1f;
        }
        #endregion

        #region ===== 左键发射与安全枪口 =====
        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {

            // ❌ 新增：左键冷却锁
            if (leftClickCooldown > 0)
                return false;

            // 右键 → 不发射左键弹幕
            if (player.altFunctionUse == 2)
            {
                return false;
            }

            var exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            if (exPlayer.EXValue >= NewLegend_EXPlayer.EXMax &&
                KeybindSystem.LegendarySkill.Current)
            {
                return false;
            }

            // 左键 → 正常发射
            Projectile.NewProjectile(
                source,
                GetSafeFirePosition(player, velocity) + new Vector2(0f, -10f) ,
                velocity,
                ModContent.ProjectileType<NewLegendSHPB>(),
                damage,
                knockback,
                player.whoAmI,
                storedEffectID > 0 ? storedEffectID : -1
            );
            leftClickCooldown = Item.useTime; // 60帧锁死
            exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            exPlayer.EXValue += (int)(NewLegend_EXPlayer.EXMax * 0.025f); // 左键对大技能的充能效果

            if (exPlayer.EXValue > NewLegend_EXPlayer.EXMax)
                exPlayer.EXValue = NewLegend_EXPlayer.EXMax;

            return false;
        }

        // ===== 左键安全开火位置 =====
        private Vector2 GetSafeFirePosition(Player player, Vector2 velocity)
        {
            // ===== 构造“虚拟枪口” =====
            Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float gunLength = 56f;

            Vector2 gunTip = player.Center + dir * gunLength;

            // ===== 1. 枪口卡墙 =====
            if (Collision.SolidCollision(gunTip, 1, 1))
                return player.Center;

            // ===== 2. 找最近敌人 =====
            NPC target = null;
            float maxDetect = 300f;
            float closestDist = maxDetect;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];

                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float dist = Vector2.Distance(player.Center, npc.Center);

                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = npc;
                }
            }

            // ===== 3. 贴脸判定 =====
            if (target != null && closestDist < gunLength)
                return player.Center;

            // ===== 4. 敌人在枪口前面 =====
            if (target != null)
            {
                float distToPlayer = Vector2.Distance(player.Center, target.Center);
                float distToGunTip = Vector2.Distance(gunTip, target.Center);

                if (distToPlayer < distToGunTip)
                    return player.Center;
            }

            return gunTip;
        }
        #endregion
        #endregion


        #region ===== 背包 UI 显示 =====
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
        #endregion

        
        #region ===== 手持表现、右键监听与 EX 技能 =====

        #region ===== 右键入口与阶段查询 =====
        public override bool AltFunctionUse(Player player) => true;

        // ===== 获取右键进度状态 =====
        public int GetRightClickProgressState()
        {
            int state = 0;
            if (NPC.downedMechBoss1)
                state = 1;
            if (DownedBossSystem.downedAstrumDeus)
                state = 2;
            if (DownedBossSystem.downedStormWeaver)
                state = 3;
            if (DownedBossSystem.downedExoMechs)
                state = 4;
            return state;
        }
        #endregion

        #region ===== HoldItem 主流程 =====
        // 左键独立冷却
        private int leftClickCooldown = 0;
        public override void HoldItem(Player player)
        {
            if (leftClickCooldown > 0)
                leftClickCooldown--;

            // ===== EX条UI同步 =====
            var exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            bool exUnlocked = exPlayer.EXUnlocked;

            if (exUnlocked)
            {
                if (player.Calamity().cooldowns.TryGetValue(SHPC_EXCooldown.ID, out var cooldown))
                {
                    cooldown.timeLeft = exPlayer.EXValue;
                }
                else
                {
                    player.AddCooldown(SHPC_EXCooldown.ID, 0);
                }
            }

            // ===== EX技能释放 =====
            if (exUnlocked && KeybindSystem.LegendarySkill.JustPressed && exPlayer.EXValue >= NewLegend_EXPlayer.EXMax)
            {
                // 防止重复生成
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.owner == player.whoAmI &&
                        proj.type == ModContent.ProjectileType<NL_SHPC_EXWeapon>())
                    {
                        return;
                    }
                }

                Vector2 dir = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);

                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    dir,
                    ModContent.ProjectileType<NL_SHPC_EXWeapon>(),
                    Item.damage * 5, // 先简单倍率
                    Item.knockBack,
                    player.whoAmI
                );

                // 清空EX条（如果你之后想改，可以删这句）
                exPlayer.EXValue = 0;
            }

            // ===== 天顶世界三连发补射 =====
            if (Main.zenithWorld && zenithBurstCount > 0)
            {
                zenithBurstTimer--;

                if (zenithBurstTimer <= 0)
                {
                    Vector2 shootDirection = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
                    Vector2 velocity = shootDirection * Item.shootSpeed;

                    Projectile.NewProjectile(
                        Item.GetSource_FromThis(),
                        player.Center + new Vector2(0f, -10f) + velocity * 3f,
                        velocity,
                        ModContent.ProjectileType<NewLegendSHPB>(),
                        GetCurrentRightDamage(player),
                        Item.knockBack,
                        player.whoAmI,
                        storedEffectID > 0 ? storedEffectID : -1
                    );

                    zenithBurstCount--;
                    zenithBurstTimer = 8; // 下一发间隔

                    // ❗手动触发音效（否则不会响）
                    SoundEngine.PlaySound(FireSound, player.Center);
                }
            }

            // 鼠标监听（原有）
            player.Calamity().mouseWorldListener = true;

            // ===== 关键：开启右键监听 =====
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            // ===== 右键长按逻辑 =====
            if (player.Calamity().mouseRight &&
                player.whoAmI == Main.myPlayer &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !(Main.playerInventory && Main.HoverItem.type == Item.type)) // ❗新增
            {
                // 🔥 强制打断左键动画
                //player.itemAnimation = 0;
                //player.itemTime = 0;
                //recoilProgress = 0;

                // ===== 防止重复生成 =====
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active &&
                        proj.owner == player.whoAmI &&
                        proj.type == ModContent.ProjectileType<SHPCRight_HoulOut>())
                    {
                        return;
                    }
                }

                // ===== 生成右键 Holdout =====
                Vector2 shootDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);

                int projIndex = Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    shootDirection,
                    ModContent.ProjectileType<SHPCRight_HoulOut>(),
                    GetCurrentRightDamage(player),
                    Item.knockBack,
                    player.whoAmI,
                    GetRightClickProgressState(),                 // ai[0]
                    (storedEffectPower > 0 && storedEffectID > 0)
                        ? storedEffectID
                        : EffectRegistry.GetEffectIDByAmmo(FindEffectAmmo(player)));
            }       

            if (player.itemAnimation > 0 && player.altFunctionUse != 2)
                return;
        }
        #endregion

        #region ===== 手持绘制与前臂姿态 =====
        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
            // 始终朝向鼠标
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));

            float itemRotation = player.compositeFrontArm.rotation + MathHelper.PiOver2 * player.gravDir;
            Vector2 itemPosition = player.MountedCenter + itemRotation.ToRotationVector2() * 35f;
            Vector2 itemSize = new Vector2(Item.width, Item.height);
            Vector2 itemOrigin = new Vector2(-35f, 0f);

            bool shouldHide = false;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile proj = Main.projectile[i];
                if (!proj.active || proj.owner != player.whoAmI)
                    continue;

                if (proj.type == ModContent.ProjectileType<SHPCRight_HoulOut>() ||
                    proj.type == ModContent.ProjectileType<NL_SHPC_EXWeapon>())
                {
                    shouldHide = true;
                    break;
                }
            }

            // 如果右键Holdout或大招已经存在，就把残留贴图扔到世界左上角
            if (shouldHide)
            {
                itemPosition = new Vector2(0f, 0f);
            }

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
        #endregion
        #endregion


        #region ===== 传奇成长与伤害覆盖 =====

        #region ===== 右键阶段伤害 =====

        // ===== 右键基础伤害（固定表）=====
        private int GetLegendaryRightBaseDamage(Player player)
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
                7,
                9,
                11,
                13,
                15,
                20,
                27,
                32,
                40,
                48,
                60,
                80,
                96,
                110,
                125,
                150,
                200,
                320,
                420
            };

            int finalDamage = 9;

            for (int i = 0; i < downStages.Length; i++)
            {
                if (downStages[i])
                    finalDamage = stageDamage[i];
                else
                    break;
            }

            return finalDamage;
        }
        // 应用右键最终伤害
        private int GetCurrentRightDamage(Player player)
        {
            int baseDamage = GetLegendaryRightBaseDamage(player);
            return (int)player.GetTotalDamage(Item.DamageType).ApplyTo(baseDamage);
        }
        #endregion

        #region ===== 左键传奇伤害覆写 =====

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
                15,     // 初始 / 克眼
                21,     // 世吞 / 克脑
                26,     // 腐巢 / 宿主
                31,     // 骷髅王
                34,     // 史莱姆之神
                42,     // 肉山后
                63,     // 机械三王
                76,     // 灾影
                91,     // 世花
                109,    // 石巨人
                120,    // 拜月教徒
                180,    // 月总
                216,    // 亵渎天神
                238,    // 神使三兄弟
                285,    // 幽花
                410,    // 神吞
                512,    // 犽戎
                768,    // 星流机甲 + 至尊灾厄
                1000   // 始源妖龙
            };

            int finalDamage = 10;

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
        #endregion
        #endregion


        #region ===== Tooltip 文本拼接 =====

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            // ===== 左键固定文案 =====
            string leftIntro = this.GetLocalizedValue("SHPC_LeftIntro");

            // ===== 左键弹药文案 =====
            string ammoText = "";
            if (storedEffectID > 0)
            {
                string key = $"Mods.CalamityLegendsComeBack.AMMO.SHPCAmmo{storedEffectID}";
                ammoText = Language.GetTextValue(key);
            }

            // ===== 右键固定文案 =====
            string rightIntro = this.GetLocalizedValue("SHPC_RightIntro");

            // ===== 右键阶段 =====
            int state = GetRightClickProgressState();
            string rightStateText = this.GetLocalizedValue($"SHPCRight{state + 1}");

            // ===== 最后一行 =====
            string finalLine = this.GetLocalizedValue("SHPC_Final");

            // ===== 传奇文本 =====
            string legendaryText = this.GetLocalizedValue("LegendaryText");

            // ===== Shift提示 =====
            string shiftHint = this.GetLocalizedValue("LegendaryHint");

            // ===== 明确检测 Shift 是否按下 =====
            string legendarySection = shiftHint;
            if (Main.keyState.PressingShift())
                legendarySection = legendaryText;

            string keyText = KeybindSystem.LegendarySkill.GetAssignedKeys().FirstOrDefault() ?? "Unbound";
            string exHint = string.Format(this.GetLocalizedValue("SHPC_EXHint"), keyText);

            // ===== 拼接 =====
            string finalText =
                leftIntro + "\n" +
                ">>> " + ammoText + " <<<" + 
                "\n\n" +
                rightIntro + "\n" +
                rightStateText + "\n\n" +
                exHint + "\n\n" +
                finalLine + "\n\n" +
                legendarySection + "\n";

            // ===== 替换 Tooltip =====
            tooltips.FindAndReplace("[GFB]", finalText);
        }
        #endregion


        #region ===== 背包右键清空灌注 =====

        #region ===== 背包右键入口 =====
        // 背包里点击右键，倒掉左键材料
        public override bool CanRightClick()
        {
            return true; // 允许背包右键
        }
        #endregion

        #region ===== 清空与返还逻辑 =====
        public override void RightClick(Player player)
        {
            // ===== 如果当前有装填 =====
            if (storedEffectID > 0 && storedAmmoType > ItemID.None && storedEffectPower > 0)
            {
                // 获取当前弹药总容量（动态）
                int maxShots = EffectRegistry.GetEffectByID(storedEffectID).ShotsPerAmmo;

                if (maxShots > 0)
                {
                    // 概率 = 当前剩余 / 总量
                    float returnChance = storedEffectPower / (float)maxShots;

                    // 判定是否返还
                    if (Main.rand.NextFloat() < returnChance)
                    {
                        // 返还材料（1个）
                        player.QuickSpawnItem(player.GetSource_FromThis(), storedAmmoType, 1);
                    }
                }
            }

            // ===== 清空灌注 =====
            storedEffectPower = 0;
            storedAmmoType = ItemID.None;
            storedEffectID = 0;

            // ===== 音效 =====
            SoundEngine.PlaySound(SoundID.MenuClose, player.Center);
        }
        #endregion

        #region ===== 阻止物品自身被消耗 =====
        public override bool ConsumeItem(Player player)
        {
            return false; // ❗阻止右键把武器消耗掉
        }
        #endregion
        #endregion


        #region ===== 克隆、存档与联机同步 =====

        #region ===== 预留合成表 =====
        // ==================== 合成表（先保留原版） ====================
        //public override void AddRecipes()
        //{
        //    CreateRecipe()
        //        .AddIngredient<PlasmaDriveCore>()
        //        .AddIngredient<SuspiciousScrap>(4)
        //        //.AddRecipeGroup("AnyMythrilBar", 10)
        //        //.AddIngredient(ItemID.SoulofFright, 5)
        //        //.AddIngredient(ItemID.SoulofMight, 5)
        //        //.AddIngredient(ItemID.SoulofSight, 5)
        //        .AddTile(TileID.Anvils)
        //        .Register();
        //}
        #endregion

        #region ===== 复制、存档与网络同步 =====
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
        #endregion
        #endregion
    }
}











