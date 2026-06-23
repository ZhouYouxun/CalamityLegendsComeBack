using System;
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
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.Passive;

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
                    Main.projectile[spike].minionSlots = (modPlayer.CurrentMode == 0) ? 0.5f : 1.0f;
                }

                // 重新排布环绕角度
                RearrangeSpikes(player);
            }

            return false;
        }

        public static void RearrangeSpikes(Player player)
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            var circlingSpikes = new List<Projectile>();
            
            foreach (Projectile pro in Main.ActiveProjectiles)
            {
                if (pro.type == spikeType && pro.owner == player.whoAmI)
                {
                    var modProj = pro.ModProjectile as IceSpikeMinion;
                    if (modProj != null && modProj.IsCirclingPlayer())
                    {
                        circlingSpikes.Add(pro);
                    }
                }
            }

            int count = circlingSpikes.Count;
            if (count == 0) return;

            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            if (modPlayer.CurrentMode == 0)
            {
                // 斩击模式：冰刺分裂成两个对立的冰刺
                float angleVariance = MathHelper.TwoPi / count;
                float angle = 0f;
                for (int i = 0; i < count; i++)
                {
                    Projectile pro = circlingSpikes[i];
                    pro.ai[0] = angle;
                    pro.ai[1] = (i % 2 == 0) ? 0f : 1f; // 0 = inner (small), 1 = outer (large)
                    pro.netUpdate = true;
                    angle += angleVariance;
                }
            }
            else
            {
                // 突刺/打击模式：均等排布
                float angleVariance = MathHelper.TwoPi / count;
                float angle = 0f;
                for (int i = 0; i < count; i++)
                {
                    Projectile pro = circlingSpikes[i];
                    pro.ai[0] = angle;
                    pro.ai[1] = 0f; // 重置为默认
                    pro.netUpdate = true;
                    angle += angleVariance;
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            string modeKey = KeybindSystem.LegendaryWeaponFormSwitch?.GetAssignedKeys().FirstOrDefault() ?? "LeftControl";
            string skillKey = KeybindSystem.LegendarySkill?.GetAssignedKeys().FirstOrDefault() ?? "P";

            string modeName = this.GetLocalizedValue("ModeName" + modPlayer.CurrentMode);

            string intro = string.Format(this.GetLocalizedValue("GE_Intro"), modeName);
            string leftClick = this.GetLocalizedValue("GE_LeftClick");
            string spikeMode = this.GetLocalizedValue("GE_SpikeMode" + modPlayer.CurrentMode);

            string specialIntro = this.GetLocalizedValue("GE_SpecialIntro");
            string specialMode = this.GetLocalizedValue("GE_SpecialMode" + modPlayer.CurrentMode);

            string passiveRhythm = string.Format(this.GetLocalizedValue("GE_PassiveRhythm"), modeKey);
            string passiveCombo = string.Format(this.GetLocalizedValue("GE_PassiveCombo"),
                modPlayer.ComboCount, modPlayer.LifeRegenBonus, modPlayer.DefenseBonus);
            string passiveDivinity = this.GetLocalizedValue("GE_PassiveDivinity");
            string passiveAurora = this.GetLocalizedValue("GE_PassiveAurora");

            string ultimate = string.Format(this.GetLocalizedValue("GE_Ultimate"), skillKey, modPlayer.UltimateCharge);

            string lore = Main.keyState.PressingShift()
                ? this.GetLocalizedValue("LegendaryText")
                : this.GetLocalizedValue("LegendaryHint");

            var components = new List<string> {
                intro,
                leftClick,
                spikeMode,
                specialIntro,
                specialMode,
                passiveRhythm,
                passiveCombo,
                passiveDivinity,
                passiveAurora,
                ultimate,
                lore
            };

            var allLines = new List<string>();
            foreach (var comp in components)
            {
                if (string.IsNullOrEmpty(comp)) continue;
                var split = comp.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in split)
                {
                    string trimmed = line.Trim();
                    if (!string.IsNullOrEmpty(trimmed))
                    {
                        allLines.Add(trimmed);
                    }
                }
            }

            string finalText = string.Join("\n", allLines);
            tooltips.FindAndReplace("[GFB]", finalText);
        }
    }
}
