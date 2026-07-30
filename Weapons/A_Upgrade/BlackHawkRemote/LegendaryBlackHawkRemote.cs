using System;
using System.Collections.Generic;
using System.IO;
using CalamityMod;
using CalamityMod.Items;
using CalamityMod.Items.Weapons.Summon;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.BlackHawkRemote
{
    public sealed class LegendaryBlackHawkRemote : ModItem, ILocalizedModType
    {
        internal const int BaseDamage = 25;

        private sbyte selectedLoadout = (sbyte)BlackHawkLoadout.Auto;

        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityMod/Items/Weapons/Summon/BlackHawkRemote";

        internal BlackHawkLoadout SelectedLoadout => BlackHawkLoadoutInfo.Sanitize(selectedLoadout);

        public override void SetStaticDefaults()
        {
            ItemID.Sets.ItemsThatAllowRepeatedRightClick[Type] = true;
        }

        public override void SetDefaults()
        {
            Item.width = 38;
            Item.height = 40;
            Item.damage = BaseDamage;
            Item.mana = 10;
            Item.useAnimation = 36;
            Item.useTime = 36;
            Item.useStyle = ItemUseStyleID.HoldUp;
            Item.noMelee = true;
            Item.knockBack = 1f;
            Item.value = CalamityGlobalItem.RarityLightRedBuyPrice;
            Item.UseSound = SoundID.Item15 with { Volume = 0.68f, Pitch = 0.08f };
            Item.autoReuse = true;
            Item.buffType = ModContent.BuffType<LegendaryBlackHawkBuff>();
            Item.shoot = ModContent.ProjectileType<LegendaryBlackHawkFighter>();
            Item.shootSpeed = 0f;
            Item.DamageType = DamageClass.Summon;
            Item.rare = ItemRarityID.LightRed;
        }

        public override ModItem Clone(Item item)
        {
            LegendaryBlackHawkRemote clone = (LegendaryBlackHawkRemote)base.Clone(item);
            clone.selectedLoadout = selectedLoadout;
            return clone;
        }

        public override void SaveData(TagCompound tag) => tag["selectedLoadout"] = (int)selectedLoadout;

        public override void LoadData(TagCompound tag)
        {
            selectedLoadout = tag.ContainsKey("selectedLoadout")
                ? (sbyte)BlackHawkLoadoutInfo.Sanitize(tag.GetInt("selectedLoadout"))
                : (sbyte)BlackHawkLoadout.Auto;
        }

        public override void NetSend(BinaryWriter writer) => writer.Write(selectedLoadout);

        public override void NetReceive(BinaryReader reader) => selectedLoadout = (sbyte)BlackHawkLoadoutInfo.Sanitize(reader.ReadSByte());

        public override bool AltFunctionUse(Player player) => true;

        public override bool CanUseItem(Player player) => player.altFunctionUse != 2;

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity,
            int type, int damage, float knockback)
        {
            if (player.altFunctionUse == 2)
                return false;

            player.AddBuff(Item.buffType, 2);
            Projectile minion = Projectile.NewProjectileDirect(
                source,
                player.ClampedMouseWorld(),
                Vector2.Zero,
                type,
                damage,
                knockback,
                player.whoAmI);
            minion.originalDamage = Item.damage;
            minion.netUpdate = true;
            return false;
        }

        public override void HoldItem(Player player)
        {
            BlackHawkCommandPlayer commandPlayer = player.GetModPlayer<BlackHawkCommandPlayer>();
            commandPlayer.AdoptCommand(SelectedLoadout);

            if (Main.myPlayer != player.whoAmI)
                return;

            player.Calamity().rightClickListener = true;
            if (!Main.mouseRight || !Main.mouseRightRelease || Main.mapFullscreen || Main.drawingPlayerChat || Main.gameMenu)
                return;

            int wheelType = ModContent.ProjectileType<BlackHawkLoadoutWheel>();
            if (player.ownedProjectileCounts[wheelType] > 0)
                return;

            Main.mouseRightRelease = false;
            Projectile.NewProjectile(
                Item.GetSource_FromThis(),
                player.Center,
                Vector2.Zero,
                wheelType,
                0,
                0f,
                player.whoAmI);
            SoundEngine.PlaySound(SoundID.MenuOpen with { Volume = 0.52f, Pitch = 0.12f }, player.Center);
        }

        internal void SetLoadout(Player player, BlackHawkLoadout loadout)
        {
            loadout = BlackHawkLoadoutInfo.Sanitize((int)loadout);
            selectedLoadout = (sbyte)loadout;
            player.GetModPlayer<BlackHawkCommandPlayer>().ApplyCommand(loadout);
            Item.NetStateChanged();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string current = string.Format(this.GetLocalizedValue("CurrentLoadout"), BlackHawkLoadoutInfo.Name(SelectedLoadout));
            tooltips.Add(new TooltipLine(Mod, "BlackHawkCurrentLoadout", current)
            {
                OverrideColor = BlackHawkLoadoutInfo.Color(SelectedLoadout)
            });

            if (Main.keyState.PressingShift())
            {
                tooltips.Add(new TooltipLine(Mod, "BlackHawkLoadoutDetails", this.GetLocalizedValue("LoadoutDetails"))
                {
                    OverrideColor = new Color(202, 222, 232)
                });
            }
            else
            {
                tooltips.Add(new TooltipLine(Mod, "BlackHawkDetailsHint", this.GetLocalizedValue("DetailsHint"))
                {
                    OverrideColor = new Color(145, 169, 182)
                });
            }

            tooltips.Add(new TooltipLine(Mod, "BlackHawkLegendary", this.GetLocalizedValue("LegendaryText"))
            {
                OverrideColor = new Color(255, 174, 76)
            });
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient<CalamityMod.Items.Weapons.Summon.BlackHawkRemote>()
                .AddIngredient(ItemID.HellstoneBar, 10)
                .AddTile(TileID.Hellforge)
                .Register();
        }
    }

    public sealed class LegendaryBlackHawkBuff : ModBuff, ILocalizedModType
    {
        public new string LocalizationCategory => "Buffs";
        public override string Texture => "CalamityMod/Buffs/Summon/BlackHawkBuff";

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = true;
            Main.buffNoSave[Type] = true;
        }

        public override void Update(Player player, ref int buffIndex)
        {
            if (player.ownedProjectileCounts[ModContent.ProjectileType<LegendaryBlackHawkFighter>()] > 0)
            {
                player.GetModPlayer<BlackHawkCommandPlayer>().BlackHawkSquadronActive = true;
                player.buffTime[buffIndex] = 18000;
                return;
            }

            player.DelBuff(buffIndex);
            buffIndex--;
        }
    }

    internal sealed class BlackHawkCommandPlayer : ModPlayer
    {
        internal BlackHawkLoadout Command { get; private set; } = BlackHawkLoadout.Auto;
        internal int CommandRevision { get; private set; }
        internal bool BlackHawkSquadronActive { get; set; }

        private int radioCooldown;
        private ulong nextAnyDispatchFrame;
        private readonly ulong[] nextTargetDispatchFrames = new ulong[Main.maxNPCs];

        public override void ResetEffects() => BlackHawkSquadronActive = false;

        public override void PostUpdate()
        {
            if (radioCooldown > 0)
                radioCooldown--;
        }

        internal void AdoptCommand(BlackHawkLoadout loadout)
        {
            loadout = BlackHawkLoadoutInfo.Sanitize((int)loadout);
            if (loadout != Command)
                ApplyCommand(loadout);
        }

        internal void ApplyCommand(BlackHawkLoadout loadout)
        {
            Command = BlackHawkLoadoutInfo.Sanitize((int)loadout);
            CommandRevision++;
            nextAnyDispatchFrame = Main.GameUpdateCount + 15;
        }

        internal bool TryClaimDispatch(int targetIndex)
        {
            if (!Main.npc.IndexInRange(targetIndex) || Main.GameUpdateCount < nextAnyDispatchFrame ||
                Main.GameUpdateCount < nextTargetDispatchFrames[targetIndex])
            {
                return false;
            }

            nextAnyDispatchFrame = Main.GameUpdateCount + 1;
            nextTargetDispatchFrames[targetIndex] = Main.GameUpdateCount + 15;
            return true;
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (proj.owner != Player.whoAmI || !ProjectileID.Sets.IsAWhip[proj.type] ||
                Player.ownedProjectileCounts[ModContent.ProjectileType<LegendaryBlackHawkFighter>()] <= 0)
            {
                return;
            }

            target.GetGlobalNPC<BlackHawkTargetStatusNPC>().Illuminate(Player.whoAmI, 240);
            Player.MinionAttackTargetNPC = target.whoAmI;

            if (Main.myPlayer != Player.whoAmI)
                return;

            BlackHawkVFX.SpawnPulse(target.Center, new Color(255, 92, 66), 0.08f, 0.46f, 14,
                new Vector2(1f, 0.66f));
            if (radioCooldown <= 0)
            {
                SoundEngine.PlaySound(SoundID.MenuTick with { Volume = 0.42f, Pitch = 0.28f }, target.Center);
                radioCooldown = 16;
            }
        }
    }

    internal sealed class BlackHawkTargetStatusNPC : GlobalNPC
    {
        public override bool InstancePerEntity => true;

        private readonly int[] illuminationTimers = new int[Main.maxPlayers];
        private readonly int[] empTimers = new int[Main.maxPlayers];
        private readonly int[] cryogenicTimers = new int[Main.maxPlayers];

        internal void Illuminate(int owner, int duration)
        {
            if (owner >= 0 && owner < illuminationTimers.Length)
                illuminationTimers[owner] = Math.Max(illuminationTimers[owner], duration);
        }

        internal void ApplyEMP(int owner, int duration)
        {
            if (owner >= 0 && owner < empTimers.Length)
                empTimers[owner] = Math.Max(empTimers[owner], duration);
        }

        internal void ApplyCryogenic(int owner, int duration)
        {
            if (owner >= 0 && owner < cryogenicTimers.Length)
                cryogenicTimers[owner] = Math.Max(cryogenicTimers[owner], duration);
        }

        internal bool IsIlluminated(int owner) => owner >= 0 && owner < illuminationTimers.Length && illuminationTimers[owner] > 0;
        internal bool IsEMPd(int owner) => owner >= 0 && owner < empTimers.Length && empTimers[owner] > 0;
        internal bool IsCryogenic(int owner) => owner >= 0 && owner < cryogenicTimers.Length && cryogenicTimers[owner] > 0;

        public override void PostAI(NPC npc)
        {
            bool empActive = false;
            bool cryogenicActive = false;
            for (int i = 0; i < illuminationTimers.Length; i++)
            {
                if (illuminationTimers[i] > 0)
                    illuminationTimers[i]--;
                if (empTimers[i] > 0)
                {
                    empTimers[i]--;
                    empActive = true;
                }
                if (cryogenicTimers[i] > 0)
                {
                    cryogenicTimers[i]--;
                    cryogenicActive = true;
                }
            }

            if (npc.boss)
            {
                if (cryogenicActive)
                    npc.velocity *= 0.985f;
                return;
            }

            if (npc.knockBackResist <= 0f)
                return;

            float velocityMultiplier = empActive ? 0.89f : cryogenicActive ? 0.93f : 1f;
            npc.velocity *= velocityMultiplier;
        }

        public override void PostDraw(NPC npc, SpriteBatch spriteBatch, Vector2 screenPos, Color drawColor)
        {
            if (Main.dedServ || Main.myPlayer < 0 || Main.myPlayer >= illuminationTimers.Length || illuminationTimers[Main.myPlayer] <= 0)
                return;

            float pulse = 0.86f + 0.08f * (float)Math.Sin(Main.GlobalTimeWrappedHourly * 7f);
            float radius = Math.Max(npc.width, npc.height) * 0.52f + 18f;
            Color color = new Color(255, 90, 62);
            BlackHawkVFX.DrawRing(npc.Center, color, radius * pulse, 0.40f,
                Main.GlobalTimeWrappedHourly * 0.55f, new Vector2(1f, 0.62f));

            Vector2 center = npc.Center;
            float bracket = radius * 0.72f;
            float tick = 8f;
            for (int i = 0; i < 4; i++)
            {
                Vector2 radial = (MathHelper.PiOver4 + MathHelper.PiOver2 * i).ToRotationVector2();
                Vector2 tangent = radial.RotatedBy(MathHelper.PiOver2);
                Vector2 anchor = center + radial * bracket;
                BlackHawkVFX.DrawWorldLine(anchor - tangent * tick, anchor + tangent * tick,
                    BlackHawkVFX.Additive(color) * 0.62f, 1.5f);
            }
        }
    }
}
