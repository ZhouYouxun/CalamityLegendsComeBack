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

        public override bool AltFunctionUse(Player player) => true;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            // 右键：QTE由Player.PreUpdate处理，此处不射击
            if (player.altFunctionUse == 2)
                return false;

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

            // 4. 判定是否达到仆从上限。斩击形态一次召唤一对 0.5 栏位大小冰钉。
            float summonCost = 1f;
            if (occupiedSlots + summonCost > player.maxMinions + 0.001f)
            {
                // 已达上限，开始左键蓄力特殊攻击
                if (player.ownedProjectileCounts[ModContent.ProjectileType<GlacialEmbraceChargeHoldout>()] == 0)
                {
                    Projectile.NewProjectile(source, player.Center, Vector2.Zero, ModContent.ProjectileType<GlacialEmbraceChargeHoldout>(), damage, knockback, player.whoAmI);
                }
            }
            else
            {
                float baseAngle = Main.rand.NextFloat(MathHelper.TwoPi);
                if (modPlayer.CurrentMode == 0)
                {
                    SpawnSpike(source, player, damage, knockback, baseAngle, 0f, 0.5f);
                    SpawnSpike(source, player, damage, knockback, baseAngle + MathHelper.Pi, 1f, 0.5f);
                }
                else
                {
                    SpawnSpike(source, player, damage, knockback, baseAngle, 0f, 1f);
                }

                // 重新排布环绕角度
                RearrangeSpikes(player);
            }

            return false;
        }

        private void SpawnSpike(EntitySource_ItemUse_WithAmmo source, Player player, int damage, float knockback, float angle, float slashRole, float slots)
        {
            int spike = Projectile.NewProjectile(source, player.ClampedMouseWorld(), Vector2.Zero,
                ModContent.ProjectileType<IceSpikeMinion>(), damage, knockback, player.whoAmI, angle, slashRole);
            if (Main.projectile.IndexInRange(spike))
            {
                Main.projectile[spike].originalDamage = Item.damage;
                Main.projectile[spike].minionSlots = slots;
            }
        }

        public static void SyncSpikesForMode(Player player)
        {
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            var spikes = new List<Projectile>();
            foreach (Projectile pro in Main.ActiveProjectiles)
            {
                if (pro.active && pro.type == spikeType && pro.owner == player.whoAmI)
                    spikes.Add(pro);
            }

            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();
            if (modPlayer.CurrentMode == 0)
            {
                var baseSpikes = new List<Projectile>();
                foreach (Projectile pro in spikes)
                {
                    if (pro.ModProjectile is not IceSpikeMinion spike)
                        continue;

                    spike.ResetForModeChange(0);
                    pro.minionSlots = 0.5f;
                    if (!spike.IsLargeSlashSpike)
                        baseSpikes.Add(pro);
                }

                float occupiedSlots = 0f;
                foreach (Projectile pro in Main.ActiveProjectiles)
                {
                    if (pro.active && pro.minion && pro.owner == player.whoAmI)
                        occupiedSlots += pro.minionSlots;
                }

                var source = player.GetSource_FromThis();
                foreach (Projectile pro in baseSpikes)
                {
                    if (occupiedSlots + 0.5f > player.maxMinions + 0.001f)
                        break;

                    int twin = Projectile.NewProjectile(source, pro.Center, Vector2.Zero, spikeType,
                        pro.damage, pro.knockBack, player.whoAmI, pro.ai[0] + MathHelper.Pi, 1f);
                    if (Main.projectile.IndexInRange(twin))
                    {
                        Main.projectile[twin].originalDamage = pro.originalDamage;
                        Main.projectile[twin].minionSlots = 0.5f;
                        occupiedSlots += 0.5f;
                    }
                }
            }
            else
            {
                foreach (Projectile pro in spikes)
                {
                    if (pro.ModProjectile is not IceSpikeMinion spike)
                        continue;

                    if (spike.IsLargeSlashSpike && pro.minionSlots <= 0.5f)
                    {
                        pro.Kill();
                        continue;
                    }

                    spike.ResetForModeChange(modPlayer.CurrentMode);
                    pro.ai[1] = 0f;
                    pro.minionSlots = 1f;
                    pro.netUpdate = true;
                }
            }

            RearrangeSpikes(player);
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
                // 斩击模式：每个逻辑冰钉拆成一小一大两个对置轨道。
                int pairCount = Math.Max(1, (count + 1) / 2);
                for (int i = 0; i < count; i++)
                {
                    Projectile pro = circlingSpikes[i];
                    bool large = i % 2 == 1;
                    int pairIndex = i / 2;
                    float baseAngle = MathHelper.Pi * pairIndex / pairCount;
                    pro.ai[0] = baseAngle + (large ? MathHelper.Pi : 0f);
                    pro.ai[1] = large ? 1f : 0f;
                    pro.minionSlots = 0.5f;
                    if (pro.ModProjectile is IceSpikeMinion spike)
                        spike.SetSpikeIndex(i);
                    pro.netUpdate = true;
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
                    pro.minionSlots = 1f;
                    if (pro.ModProjectile is IceSpikeMinion spike)
                        spike.SetSpikeIndex(i);
                    pro.netUpdate = true;
                    angle += angleVariance;
                }
            }
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            Player player = Main.LocalPlayer;
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            string skillKey = KeybindSystem.LegendarySkill?.GetAssignedKeys().FirstOrDefault() ?? "P";

            string modeName = this.GetLocalizedValue("ModeName" + modPlayer.CurrentMode);

            string intro = string.Format(this.GetLocalizedValue("GE_Intro"), modeName);
            string leftClick = this.GetLocalizedValue("GE_LeftClick");
            string spikeMode = this.GetLocalizedValue("GE_SpikeMode" + modPlayer.CurrentMode);

            string specialIntro = this.GetLocalizedValue("GE_SpecialIntro");
            string specialMode = this.GetLocalizedValue("GE_SpecialMode" + modPlayer.CurrentMode);

            string passiveRhythm = this.GetLocalizedValue("GE_PassiveRhythm");
            string passiveCombo = string.Format(this.GetLocalizedValue("GE_PassiveCombo"),
                modPlayer.ComboCount, modPlayer.LifeRegenBonus, modPlayer.DefenseBonus);
            string passiveDivinity = this.GetLocalizedValue("GE_PassiveDivinity");
            string passiveAurora = this.GetLocalizedValue("GE_PassiveAurora");

            string ultimate = string.Format(this.GetLocalizedValue("GE_Ultimate"), skillKey, modPlayer.UltimateCharge);

            bool shiftPressed = Main.keyState.PressingShift();
            string lore = shiftPressed
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
            if (shiftPressed)
                tooltips.RemoveAll(t => t.Text == "[GFB]");
            else
                tooltips.FindAndReplace("[GFB]", finalText);
            tooltips.Add(new TooltipLine(Mod, "GlacialEmbraceAuroraFrostLegendaryText", lore));
        }
    }
}
