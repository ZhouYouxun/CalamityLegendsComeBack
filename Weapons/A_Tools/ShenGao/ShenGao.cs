using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Tools.ShenGao
{
    public class ShenGao : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/ShenGao/神镐";

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
            Item.useAnimation = 18;
            Item.useStyle = ItemUseStyleID.Swing;
            Item.knockBack = 7;
            Item.value = Item.buyPrice(gold: 50);
            Item.rare = ItemRarityID.Purple;
            Item.UseSound = SoundID.Item1;
            Item.autoReuse = true;
            Item.scale = 1.1f;
            Item.pick = GetPickPower();
            Item.tileBoost = GetTileBoost();
            Item.axe = GetAxePower();
            Item.hammer = GetHammerPower();
            Item.useTurn = true;
        }

        public override Vector2? HoldoutOffset() => new Vector2(-28, -28);

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                // 右键：纯锤子模式
                Item.useTime = GetUseTime();
                Item.useAnimation = 8;
                Item.damage = GetDamage();
                Item.axe = 0;
                Item.hammer = GetHammerPower();
                Item.pick = 0;
            }
            else
            {
                // 左键：镐 + 斧
                Item.useTime = GetUseTime();
                Item.useAnimation = 18;
                Item.damage = GetDamage();
                Item.axe = GetAxePower();
                Item.hammer = 0;
                Item.pick = GetPickPower();
            }
            return base.CanUseItem(player);
        }

        public override void HoldItem(Player player)
        {
            Color c = GetTierColor();
            Lighting.AddLight(player.Center, new Vector3(c.R / 255f, c.G / 255f, c.B / 255f) * 0.18f);
        }

        public override void UpdateInventory(Player player)
        {
            Item.damage = GetDamage();
            Item.useTime = GetUseTime();
            Item.pick = GetPickPower();
            Item.axe = GetAxePower();
            Item.hammer = GetHammerPower();
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

            // 动态光源，颜色随阶位变化
            Color c = primary;
            Lighting.AddLight(center, new Vector3(c.R / 255f, c.G / 255f, c.B / 255f) * 0.55f);

            // 挥舞弧线线条粒子——从镐尖向外弹射，视觉上像碎石火花但更精致
            if (Main.rand.NextBool(2))
            {
                int count = tier >= 8 ? 2 : 1;
                for (int i = 0; i < count; i++)
                {
                    Vector2 pos = center + Main.rand.NextVector2Circular(22f, 22f);
                    Vector2 dir = (pos - player.Center).SafeNormalize(Main.rand.NextVector2Unit());
                    float speed = Main.rand.NextFloat(2.5f, 5.8f + tier * 0.18f);
                    GeneralParticleHandler.SpawnParticle(new LineParticle(
                        pos,
                        dir * speed,
                        false,
                        Main.rand.Next(5, 11),
                        Main.rand.NextFloat(0.08f, 0.20f),
                        Main.rand.NextBool(3) ? accent : primary));
                }
            }

            // 微型辉光粒子——点缀在挥舞路径上，不密集
            if (Main.rand.NextBool(5))
            {
                GeneralParticleHandler.SpawnParticle(new SquishyLightParticle(
                    center + Main.rand.NextVector2Circular(13f, 13f),
                    Main.rand.NextVector2Circular(0.7f, 0.7f),
                    Main.rand.NextFloat(0.05f, 0.11f + tier * 0.003f),
                    Color.Lerp(primary, Color.White, 0.38f),
                    Main.rand.Next(4, 9)));
            }

            // 月球领主后：偶发扩散脉冲环，视觉上像宇宙涟漪
            if (tier >= 8 && Main.rand.NextBool(12))
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    center,
                    Vector2.Zero,
                    primary with { A = 0 },
                    "CalamityMod/Particles/BloomRing",
                    Vector2.One * 0.55f,
                    Main.rand.NextFloat(MathHelper.TwoPi),
                    0.014f,
                    0.09f,
                    9));
            }

            // 噬神者后：虚空微辉，极稀疏的深色内爆光点
            if (tier >= 11 && Main.rand.NextBool(16))
            {
                GeneralParticleHandler.SpawnParticle(new CustomPulse(
                    center + Main.rand.NextVector2Circular(10f, 10f),
                    Vector2.Zero,
                    accent with { A = 0 },
                    "CalamityMod/Particles/BloomCircle",
                    Vector2.One * 0.14f,
                    0f,
                    0.40f,
                    0.03f,
                    7));
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            tooltips.Add(new TooltipLine(Mod, "ShenGaoHint", "左键：采掘 + 伐木    右键：锤击"));
        }

        public override void AddRecipes()
        {
            Recipe recipe = CreateRecipe();
            recipe.AddIngredient(ItemID.Emerald, 5);
            recipe.AddIngredient(ItemID.Wood, 2);
            recipe.AddTile(TileID.WorkBenches);
            recipe.Register();
        }

        // ─── 阶位检测 ─────────────────────────────────────────────────────────────────

        public static int GetTier()
        {
            if (DownedBossSystem.downedExoMechs && DownedBossSystem.downedCalamitas) return 13;  // 星流巨械 + 至尊女巫
            if (DownedBossSystem.downedYharon)        return 12;  // 犽戎，龙的体现
            if (DownedBossSystem.downedDoG)           return 11;  // 噬神者
            if (DownedBossSystem.downedPolterghast)   return 10;  // 噬魂幽花
            if (DownedBossSystem.downedProvidence)    return 9;   // 亵渎天神，普罗维登斯
            if (NPC.downedMoonlord)                   return 8;   // 月球领主
            if (NPC.downedGolemBoss)                  return 7;   // 石巨人
            if (NPC.downedPlantBoss)                  return 6;   // 世纪之花
            if (NPC.downedMechBoss1 && NPC.downedMechBoss2 && NPC.downedMechBoss3) return 5;  // 三王
            if (NPC.downedMechBoss1 || NPC.downedMechBoss2 || NPC.downedMechBoss3) return 4;  // 机械先锋
            if (Main.hardMode)                        return 3;   // 血肉墙
            if (DownedBossSystem.downedSlimeGod)      return 2;   // 史莱姆之神
            if (DownedBossSystem.downedHiveMind || DownedBossSystem.downedPerforator || NPC.downedBoss2) return 1;  // 腐化/猩红
            return 0;
        }

        // ─── 属性计算 ─────────────────────────────────────────────────────────────────

        private static int GetPickPower() => GetTier() switch
        {
            0  => 65,
            1  => 80,
            2  => 96,
            3  => 190,
            4  => 200,
            5  => 212,
            6  => 218,
            7  => 228,
            8  => 250,
            9  => 268,
            10 => 288,
            11 => 310,
            12 => 335,
            _  => 365
        };

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

        private static int GetUseTime() => GetTier() switch
        {
            0 or 1 or 2 => 8,
            3 or 4      => 7,
            5 or 6      => 6,
            7 or 8      => 5,
            9 or 10     => 4,
            11 or 12    => 3,
            _           => 2
        };

        private static int GetDamage() => GetTier() switch
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
            _ => 500   // 月球领主及更高：毫无阻力地锤平一切
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
