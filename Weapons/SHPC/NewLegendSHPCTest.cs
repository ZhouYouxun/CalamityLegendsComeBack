using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityLegendsComeBack.Weapons.SHPC.EXSkill;
using CalamityLegendsComeBack.Weapons.SHPC.RightClick;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Rarities;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.SHPC
{
    public class NewLegendSHPCTest : ModItem, ILocalizedModType
    {
        public override string Texture => "CalamityMod/Items/Weapons/Magic/SHPC";
        public new string LocalizationCategory => "Items.Weapons";

        public static readonly SoundStyle FireSound = new("CalamityMod/Sounds/Item/AnomalysNanogunMPFBShot");

        public int storedEffectPower;
        public int storedAmmoType = ItemID.None;
        public int storedEffectID;
        public int recoilProgress;

        private int leftClickCooldown;

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
            Item.UseSound = FireSound;
            Item.autoReuse = true;
            Item.shoot = ModContent.ProjectileType<NewLegendSHPB>();
            Item.shootSpeed = 20f;
            Item.channel = false;
            Item.value = CalamityGlobalItem.RarityPinkBuyPrice;
            Item.rare = ModContent.RarityType<BurnishedAuric>();
        }

        public static int FindEffectAmmo(Player player)
        {
            for (int i = 54; i < 58; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && EffectRegistry.IsRegisteredAmmo(item.type))
                    return item.type;
            }

            for (int i = 0; i < 54; i++)
            {
                Item item = player.inventory[i];
                if (item != null && item.stack > 0 && EffectRegistry.IsRegisteredAmmo(item.type))
                    return item.type;
            }

            return -1;
        }

        public Color FindColorForCurrentEffect()
        {
            RulesOfEffect effect = EffectRegistry.GetEffectByID(storedEffectID);
            return effect?.ThemeColor ?? Color.DarkGray;
        }

        public int TransferEffectToProj() => storedEffectID;

        public string GetCurrentAmmoDisplayName()
        {
            if (storedAmmoType <= ItemID.None)
                return "None";

            return Lang.GetItemNameValue(storedAmmoType);
        }

        public override Vector2? HoldoutOffset() => new Vector2(-35f, -10f);

        public override void OnCreated(ItemCreationContext context)
        {
            if (context is RecipeItemCreationContext)
                storedEffectPower = EffectRegistry.GetEffectByID(-1).ShotsPerAmmo;
        }

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player)
        {
            if (player.altFunctionUse == 2)
            {
                Item.channel = true;
                Item.noUseGraphic = true;
                Item.UseSound = null;
            }
            else
            {
                Item.channel = false;
                Item.noUseGraphic = false;
                Item.UseSound = FireSound;
            }

            return true;
        }

        public override bool? UseItem(Player player)
        {
            if (player.altFunctionUse == 2)
                return true;

            if (storedEffectPower > 0)
                storedEffectPower--;

            if (storedEffectPower <= 0)
            {
                int ammoType = FindEffectAmmo(player);
                if (ammoType != -1)
                {
                    player.ConsumeItem(ammoType);
                    storedAmmoType = ammoType;
                    storedEffectID = EffectRegistry.GetEffectIDByAmmo(ammoType);
                    storedEffectPower = EffectRegistry.GetEffectByID(storedEffectID).ShotsPerAmmo;
                }
            }

            return base.UseItem(player);
        }

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback)
        {
            if (leftClickCooldown > 0 || player.altFunctionUse == 2)
                return false;

            var exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            if (exPlayer.EXValue >= NewLegend_EXPlayer.EXMax && KeybindSystem.LegendarySkill.Current)
                return false;

            Projectile.NewProjectile(
                source,
                GetSafeFirePosition(player, velocity) + new Vector2(0f, -10f),
                velocity,
                ModContent.ProjectileType<NewLegendSHPB>(),
                damage,
                knockback,
                player.whoAmI,
                storedEffectID > 0 ? storedEffectID : -1
            );

            leftClickCooldown = Item.useTime;
            exPlayer.EXValue += (int)(NewLegend_EXPlayer.EXMax * 0.55f);
            if (exPlayer.EXValue > NewLegend_EXPlayer.EXMax)
                exPlayer.EXValue = NewLegend_EXPlayer.EXMax;

            return false;
        }

        private Vector2 GetSafeFirePosition(Player player, Vector2 velocity)
        {
            Vector2 dir = velocity.SafeNormalize(Vector2.UnitX * player.direction);
            float gunLength = 56f;
            Vector2 gunTip = player.Center + dir * gunLength;

            if (Collision.SolidCollision(gunTip, 1, 1))
                return player.Center;

            NPC target = null;
            float closestDist = 300f;

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

            if (target != null && closestDist < gunLength)
                return player.Center;

            if (target != null)
            {
                float distToPlayer = Vector2.Distance(player.Center, target.Center);
                float distToGunTip = Vector2.Distance(gunTip, target.Center);
                if (distToPlayer < distToGunTip)
                    return player.Center;
            }

            return gunTip;
        }

        public override void PostDrawInInventory(SpriteBatch spriteBatch, Vector2 position, Rectangle frame, Color drawColor, Color itemColor, Vector2 origin, float scale)
        {
            float barScale = 2.5f;
            Texture2D barBG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarBack").Value;
            Texture2D barFG = ModContent.Request<Texture2D>("CalamityMod/UI/MiscTextures/GenericBarFront").Value;

            RulesOfEffect effect = EffectRegistry.GetEffectByID(storedEffectID) ?? EffectRegistry.GetEffectByID(-1);
            int maxShots = Math.Max(1, effect?.ShotsPerAmmo ?? 1);
            float ratio = Utils.Clamp(storedEffectPower / (float)maxShots, 0f, 1f);

            Vector2 drawPos = position + new Vector2((frame.Width - barBG.Width * 0.5f) * scale, (frame.Height + 45f) * scale);
            Rectangle frameCrop = new Rectangle(0, 0, (int)(ratio * barFG.Width), barFG.Height);

            spriteBatch.Draw(barBG, drawPos, null, Color.Black, 0f, origin, scale * barScale, 0, 0f);
            spriteBatch.Draw(barFG, drawPos, frameCrop, Color.Lerp(Color.DarkGray, FindColorForCurrentEffect(), ratio) * 0.8f, 0f, origin, scale * barScale, 0, 0f);

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

        public override void HoldItem(Player player)
        {
            if (leftClickCooldown > 0)
                leftClickCooldown--;

            var exPlayer = player.GetModPlayer<NewLegend_EXPlayer>();
            if (player.Calamity().cooldowns.TryGetValue(SHPC_EXCooldown.ID, out var cooldown))
                cooldown.timeLeft = exPlayer.EXValue;
            else
                player.AddCooldown(SHPC_EXCooldown.ID, 0);

            if (KeybindSystem.LegendarySkill.JustPressed && exPlayer.EXValue >= NewLegend_EXPlayer.EXMax)
            {
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.owner == player.whoAmI && proj.type == ModContent.ProjectileType<NL_SHPC_EXWeapon>())
                        return;
                }

                Vector2 dir = (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    dir,
                    ModContent.ProjectileType<NL_SHPC_EXWeapon>(),
                    Item.damage * 5,
                    Item.knockBack,
                    player.whoAmI
                );
                exPlayer.EXValue = 0;
            }

            player.Calamity().mouseWorldListener = true;
            if (Main.myPlayer == player.whoAmI)
                player.Calamity().rightClickListener = true;

            if (player.Calamity().mouseRight &&
                player.whoAmI == Main.myPlayer &&
                !Main.mapFullscreen &&
                !Main.blockMouse &&
                !(Main.playerInventory && Main.HoverItem.type == Item.type))
            {
                foreach (Projectile proj in Main.projectile)
                {
                    if (proj.active && proj.owner == player.whoAmI && proj.type == ModContent.ProjectileType<SHPCRight_HoulOut>())
                        return;
                }

                Vector2 shootDirection = (player.Calamity().mouseWorld - player.MountedCenter).SafeNormalize(Vector2.UnitX * player.direction);
                Projectile.NewProjectile(
                    Item.GetSource_FromThis(),
                    player.Center,
                    shootDirection,
                    ModContent.ProjectileType<SHPCRight_HoulOut>(),
                    GetCurrentRightDamage(player),
                    Item.knockBack,
                    player.whoAmI,
                    GetRightClickProgressState(),
                    (storedEffectPower > 0 && storedEffectID > 0) ? storedEffectID : EffectRegistry.GetEffectIDByAmmo(FindEffectAmmo(player))
                );
            }

            if (player.itemAnimation > 0 && player.altFunctionUse != 2)
                return;
        }

        public override void UseStyle(Player player, Rectangle heldItemFrame)
        {
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

                if (proj.type == ModContent.ProjectileType<SHPCRight_HoulOut>() || proj.type == ModContent.ProjectileType<NL_SHPC_EXWeapon>())
                {
                    shouldHide = true;
                    break;
                }
            }

            if (shouldHide)
                itemPosition = Vector2.Zero;

            recoilProgress++;
            if (recoilProgress < Item.useAnimation / 3)
                itemPosition -= (player.Calamity().mouseWorld - player.Center).SafeNormalize(Vector2.UnitX) * (Item.useAnimation / 3 - recoilProgress) * 0.75f;
            else if (recoilProgress >= Item.useAnimation - 1)
                recoilProgress = 0;

            CalamityUtils.CleanHoldStyle(player, itemRotation, itemPosition, itemSize, itemOrigin);
            base.UseStyle(player, heldItemFrame);
        }

        public override void UseItemFrame(Player player)
        {
            player.ChangeDir(Math.Sign((player.Calamity().mouseWorld - player.Center).X));
            float rotation = (player.Center - player.Calamity().mouseWorld).ToRotation() * player.gravDir + MathHelper.PiOver2;
            player.SetCompositeArmFront(true, Player.CompositeArmStretchAmount.Full, rotation);
        }

        private int GetCurrentRightDamage(Player player) => (int)player.GetTotalDamage(Item.DamageType).ApplyTo(Item.damage);

        public override bool CanRightClick() => true;

        public override void RightClick(Player player)
        {
            if (storedEffectID > 0 && storedAmmoType > ItemID.None && storedEffectPower > 0)
            {
                int maxShots = EffectRegistry.GetEffectByID(storedEffectID).ShotsPerAmmo;
                if (maxShots > 0 && Main.rand.NextFloat() < storedEffectPower / (float)maxShots)
                    player.QuickSpawnItem(player.GetSource_FromThis(), storedAmmoType, 1);
            }

            storedEffectPower = 0;
            storedAmmoType = ItemID.None;
            storedEffectID = 0;
            SoundEngine.PlaySound(SoundID.MenuClose, player.Center);
        }

        public override bool ConsumeItem(Player player) => false;

        public override ModItem Clone(Item item)
        {
            ModItem clone = base.Clone(item);
            if (clone is NewLegendSHPCTest newItem && item.ModItem is NewLegendSHPCTest oldItem)
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
