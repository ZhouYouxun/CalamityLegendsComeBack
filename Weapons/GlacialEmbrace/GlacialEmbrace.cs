using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityMod.Items;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace
{
    [LegacyName("ColdDivinity")]
    public class GlacialEmbrace : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons.Summon";

        public override void SetStaticDefaults()
        {
            CalamityMod.Systems.Collections.CalamityItemSets.ExtraDebuffTooltip_Enemy[Type] = [BuffID.Frostburn2];
        }

        public override void SetDefaults()
        {
            Item.width = 52;
            Item.height = 50;
            Item.damage = 48;
            Item.mana = 10;
            Item.useAnimation = Item.useTime = 30;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.knockBack = 4.5f;
            Item.UseSound = SoundID.Item30;
            Item.autoReuse = true;
            Item.buffType = ModContent.BuffType<GlacialEmbraceBuff>();
            Item.shoot = ModContent.ProjectileType<IceSpikeMinion>();
            Item.shootSpeed = 10f;
            Item.DamageType = DamageClass.Summon;
            Item.channel = true; // 支持蓄力模式

            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ItemRarityID.Pink;
        }

        public override bool AltFunctionUse(Player player)
        {
            return false;
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 确保 Buff 在使用时处于激活状态
            player.AddBuff(Item.buffType, 2);
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            // 1. 如果处于打击形态且冰刺已经对齐，左键点击直接触发碰撞轰击！
            if (modPlayer.CurrentMode == 2 && modPlayer.StrikeAligned)
            {
                modPlayer.StrikeAligned = false;
                modPlayer.StrikeAlignCooldown = 360; // 6秒冷却，随后进入休眠

                int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
                bool smashed = false;
                for (int i = 0; i < Main.maxProjectiles; i++)
                {
                    Projectile p = Main.projectile[i];
                    if (p.active && p.type == spikeType && p.owner == player.whoAmI)
                    {
                        var spike = p.ModProjectile as IceSpikeMinion;
                        if (spike != null && !spike.hibernating && !spike.smashing)
                        {
                            spike.ExecuteSmash();
                            smashed = true;
                        }
                    }
                }
                
                if (smashed)
                {
                    SoundEngine.PlaySound(SoundID.Item30 with { Pitch = -0.1f, Volume = 0.8f }, player.Center);
                }
                return false;
            }

            // 2. 统计当前召唤物槽位占用情况
            float occupiedSlots = 0f;
            foreach (Projectile pro in Main.ActiveProjectiles)
            {
                if (pro.active && pro.minion && pro.owner == player.whoAmI)
                {
                    occupiedSlots += pro.minionSlots;
                }
            }

            // 3. 如果尚未召唤“不占仆从栏的冰块” (Ice Block)，则优先召唤它
            if (player.ownedProjectileCounts[ModContent.ProjectileType<IceBlockMinion>()] == 0)
            {
                Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<IceBlockMinion>(), damage, knockback, player.whoAmI);
            }

            // 4. 判定是否达到仆从上限
            if (occupiedSlots >= player.maxMinions)
            {
                // 已达上限，开始左键蓄力特殊攻击
                if (player.ownedProjectileCounts[ModContent.ProjectileType<GlacialEmbraceChargeHoldout>()] == 0)
                {
                    Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<GlacialEmbraceChargeHoldout>(), damage, knockback, player.whoAmI);
                }
            }
            else
            {
                // 未达上限，召唤正常的围绕冰刺弹幕
                int spike = Projectile.NewProjectile(source, player.ClampedMouseWorld(), Vector2.Zero, ModContent.ProjectileType<IceSpikeMinion>(), damage, knockback, player.whoAmI);
                if (Main.projectile.IndexInRange(spike))
                {
                    Main.projectile[spike].originalDamage = Item.damage;
                }

                // 重新排布环绕角度
                RearrangeSpikes(player);
            }

            return false;
        }

        public static void RearrangeSpikes(Player player)
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            int spikeCount = 0;
            
            foreach (Projectile pro in Main.ActiveProjectiles)
            {
                if (pro.type == spikeType && pro.owner == player.whoAmI)
                {
                    var modProj = pro.ModProjectile as IceSpikeMinion;
                    if (modProj != null && modProj.IsCirclingPlayer())
                    {
                        spikeCount++;
                    }
                }
            }

            if (spikeCount == 0) return;

            float angleVariance = MathHelper.TwoPi / spikeCount;
            float angle = 0f;

            foreach (Projectile pro in Main.ActiveProjectiles)
            {
                if (pro.type == spikeType && pro.owner == player.whoAmI)
                {
                    var modProj = pro.ModProjectile as IceSpikeMinion;
                    if (modProj != null && modProj.IsCirclingPlayer())
                    {
                        pro.ai[0] = angle;
                        pro.netUpdate = true;
                        angle += angleVariance;
                    }
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            string modeName = modPlayer.CurrentMode switch
            {
                0 => "[斩击模式 - Cleave Mode]",
                1 => "[突刺模式 - Pierce Mode]",
                2 => "[打击模式 - Strike Mode]",
                _ => "[未知模式]"
            };

            string modeKey = KeybindSystem.LegendaryWeaponFormSwitch?.GetAssignedKeys().FirstOrDefault() ?? "LeftControl";
            string skillKey = KeybindSystem.LegendarySkill?.GetAssignedKeys().FirstOrDefault() ?? "P";

            Color modeColor = modPlayer.CurrentMode switch
            {
                0 => Color.LimeGreen,
                1 => Color.Cyan,
                2 => Color.OrangeRed,
                _ => Color.White
            };

            tooltips.Add(new TooltipLine(Mod, "GlacialMode", $"当前战斗形态: {modeName}") { OverrideColor = modeColor });
            tooltips.Add(new TooltipLine(Mod, "GlacialKeys", $"[按住左键] 释放特殊蓄力技能 | [按下 {modeKey}] 切换攻击形态\n[按下 {skillKey}] 释放终结技：至臻极冰之樊寒神钻 (满240能: {modPlayer.UltimateCharge}/240)") { OverrideColor = Color.LightSkyBlue });
            tooltips.Add(new TooltipLine(Mod, "GlacialCombo", $"霜冻连击: x{modPlayer.ComboCount} (+{modPlayer.LifeRegenBonus} 回血, +{modPlayer.DefenseBonus} 防御)") { OverrideColor = Color.LightGoldenrodYellow });
        }
    }
}
