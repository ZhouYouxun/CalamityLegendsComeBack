using CalamityMod;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Tools.ShenGao
{
    public class ShenGao : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/Tools/ShenGao/神镐";

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Item.type] = true;
            ItemID.Sets.UsesBetterMeleeItemLocation[Item.type] = true;
        }

        public override void SetDefaults()
        {
            Item.damage = GetDamage();
            Item.DamageType = DamageClass.Melee;
            Item.width = 52;
            Item.height = 52;
            Item.useTime = GetUseTime();
            Item.useAnimation = GetUseTime();   // 始终维持 20 帧挥动节奏
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7;
            Item.value = Item.buyPrice(gold: 50);
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.scale = 1.43f;    // 原 1.1f 放大 30%
            Item.pick = GetPickPower();
            Item.tileBoost = GetTileBoost();
            Item.axe = 0;
            Item.hammer = 0;
            Item.useTurn = true;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-28, -28);

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            int t = GetUseTime();
            if (player.altFunctionUse == 2)
            {
                // 右键：锤 + 斧
                Item.useTime = t;
                Item.useAnimation = t;
                Item.damage = GetDamage();
                Item.pick = 0;
                Item.axe = GetAxePower();
                Item.hammer = GetHammerPower();
            }
            else
            {
                // 左键：纯镐
                Item.useTime = t;
                Item.useAnimation = t;
                Item.damage = GetDamage();
                Item.pick = GetPickPower();
                Item.axe = 0;
                Item.hammer = 0;
            }
            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            Color c = GetTierColor();
            Lighting.AddLight(player.Center, new Vector3(c.R / 255f, c.G / 255f, c.B / 255f) * 0.40f);

            // 挥动保持 20 帧，但方块破坏按 2 帧工具的效率结算。
            player.pickSpeed *= MiningSpeedMultiplier;
        }

        public override void UpdateInventory(Player player)
        {
            Item.damage = GetDamage();
            int t = GetUseTime();
            Item.useTime = t;
            Item.useAnimation = t;
            Item.pick = GetPickPower();
            Item.tileBoost = GetTileBoost();
        }

        public override void MeleeEffects(Player player, Rectangle hitbox)
        {
            if (Main.dedServ)
                return;

            int tier = GetTier();
            Color primary = GetTierColor();
            Color accent = GetTierAccent();
            Vector2 center = hitbox.Center.ToVector2();

            // 强化光源——颜色随阶位演变，照亮挥舞范围
            Color c = primary;
            Lighting.AddLight(center, new Vector3(c.R / 255f, c.G / 255f, c.B / 255f) * 1.0f);
            Lighting.AddLight(player.Center, new Vector3(c.R / 255f, c.G / 255f, c.B / 255f) * 0.35f);

            // 弧线线条粒子——每帧稳定生成，模拟镐凿击时的能量火花
            {
                int count = tier >= 8 ? 4 : 2;
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = center + Main.rand.NextVector2Circular(24f, 24f);
                    Vector2 dir = (pos - player.Center).SafeNormalize(Main.rand.NextVector2Unit());
                    float speed = Main.rand.NextFloat(3f, 7f + tier * 0.2f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        pos,
                        dir * speed,
                        false,
                        Main.rand.Next(6, 13),
                        Main.rand.NextFloat(0.10f, 0.24f),
                        Main.rand.NextBool(3) ? accent : primary));
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "ShenGaoHint", "左键：采掘    右键：锤击 + 伐木"));
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient<MysteriousCircuitry>(4)
                  .AddIngredient<DubiousPlating>(4)
                  .AddIngredient<PlasmaDriveCore>(1)
                  .AddTile(TileID.WorkBenches)
                  .Register();
        }

        // ─── 阶位检测 ─────────────────────────────────────────────────────────────────

        public static int GetTier()
        {
            if (DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas) return 13;  // 星流巨械 + 至尊女巫
            if (DownedBossSystem.downedYharon)        return 12;  // 犽戎
            if (DownedBossSystem.downedDoG)           return 11;  // 噬神者
            if (DownedBossSystem.downedPolterghast)   return 10;  // 噬魂幽花
            if (DownedBossSystem.downedProvidence)    return 9;   // 普罗维登斯
            if (NPC.downedMoonlord)                   return 8;   // 月球领主
            if (NPC.downedGolemBoss)                  return 7;   // 石巨人
            if (NPC.downedPlantBoss)                  return 6;   // 世纪之花
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) return 5;
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3) return 4;
            if (Main.hardMode)                        return 3;   // 血肉墙
            if (DownedBossSystem.downedSlimeGod)      return 2;   // 史莱姆之神
            if (DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator || NPC.downedBoss2) return 1;
            return 0;
        }

        // ─── 属性计算 ─────────────────────────────────────────────────────────────────

        private static int GetPickPower()
        {
            int power = GetTier() switch
            {
                0      => 59,                                      // 初始
                1 or 2 => 100,                                     // 世吞/克脑 —— 熔岩镐
                3 or 4 => 190,                                     // 肉山进入困难模式 —— 钛金镐
                5 or 6 => 200,                                     // 三机械 —— 神圣镐斧 / 叶绿镐
                7      => 210,                                     // 石巨人 —— 锯刃镐
                8      => 250,
                9      => 268,
                10     => 288,
                11     => 310,
                12     => 335,
                _      => 365
            };

            // 越阶击杀的补正——始终取最高档，不会因为跳过前置 Boss 而卡在低档
            if (NPC.downedGolemBoss || DownedBossSystem.downedRavager)   // 石巨人 / 毁灭魔像
                power = Math.Max(power, 210);
            if (NPC.downedAncientCultist)                                // 拜月邪教徒 —— 激光钻
                power = Math.Max(power, 230);

            return power;
        }

        private static int GetTileBoost() => GetTier() switch
        {
            0  => 10,
            1  => 16,
            2  => 22,
            3  => 36,
            4  => 46,
            5  => 62,
            6  => 72,
            7  => 88,
            8  => 115,
            9  => 140,
            10 => 168,
            11 => 196,
            12 => 224,
            _  => 255
        };

        private const int AttackUseTime = 20;
        private const float MiningSpeedMultiplier = 2f / AttackUseTime;

        private static int GetUseTime() => AttackUseTime;

        private static int GetDamage()
        {
            int baseDamage = GetTier() switch
            {
                0  => 20,
                1  => 32,
                2  => 48,
                3  => 72,
                4  => 92,
                5  => 120,
                6  => 142,
                7  => 168,
                8  => 202,
                9  => 262,
                10 => 325,
                11 => 405,
                12 => 508,
                _  => 650
            };

            return (int)Math.Round(baseDamage * 0.8f, MidpointRounding.AwayFromZero);
        }

        private static int GetAxePower() => GetTier() switch
        {
            0  => 100,
            1  => 125,
            2  => 145,
            3  => 175,
            4  => 192,
            5  => 212,
            6  => 222,
            7  => 232,
            8  => 258,
            9  => 278,
            10 => 298,
            11 => 322,
            12 => 348,
            _  => 380
        };

        private static int GetHammerPower() => GetTier() switch
        {
            0 => 70,
            1 => 76,
            2 => 84,
            3 => 94,
            4 => 108,
            5 => 128,
            6 => 145,
            7 => 162,
            _ => 500
        };

        // ─── 特效调色板 ───────────────────────────────────────────────────────────────

        private static Color GetTierColor() => GetTier() switch
        {
            >= 13 => new Color(210, 155, 255),   // 终极：超凡紫白
            >= 12 => new Color(118, 214, 255),   // 犽戎：龙焰蓝
            >= 11 => new Color(88, 62, 228),     // 噬神者：深空靛蓝
            >= 10 => new Color(188, 228, 255),   // 噬魂幽花：幽灵冰蓝
            >= 9  => new Color(255, 218, 105),   // 普罗维登斯：神圣金
            >= 8  => new Color(182, 148, 255),   // 月球领主：幽灵紫
            >= 5  => new Color(84, 198, 255),    // 机械/植物/石巨人：明钴蓝
            >= 3  => new Color(56, 218, 198),    // 肉壁/机械先锋：赤道青
            _     => new Color(78, 228, 255)     // 初始/前硬模式：极地冰蓝
        };

        private static Color GetTierAccent() => GetTier() switch
        {
            >= 13 => new Color(255, 218, 255),   // 柔粉紫
            >= 12 => new Color(198, 255, 255),   // 白蓝冰霜
            >= 11 => new Color(145, 102, 255),   // 深幽蓝紫
            >= 10 => new Color(220, 242, 255),   // 极淡幽蓝
            >= 9  => new Color(255, 246, 192),   // 淡金白光
            >= 8  => new Color(255, 226, 255),   // 薰衣草白
            >= 5  => new Color(194, 242, 255),   // 淡天蓝
            >= 3  => new Color(178, 255, 234),   // 淡薄荷
            _     => new Color(200, 246, 255)    // 淡极地冰
        };
    }
}
