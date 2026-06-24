using System.Collections.Generic;
using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Tools.Tools.SoundProof
{
    public class SoundproofEarmuffs : ModItem, ILocalizedModType
    {
        public new string LocalizationCategory => "Items.Weapons";
        public override string Texture => "CalamityLegendsComeBack/Weapons/A_Tools/Tools/SoundProof/隔音耳罩";

        public bool Enabled;

        public override void SetDefaults()
        {
            Item.width = 32;
            Item.height = 32;
            Item.value = Item.sellPrice(silver: 50);
            Item.rare = ItemRarityID.Blue;
        }

        public override ModItem Clone(Item item)
        {
            SoundproofEarmuffs clone = (SoundproofEarmuffs)base.Clone(item);
            clone.Enabled = Enabled;
            return clone;
        }

        public override void SaveData(TagCompound tag) => tag["soundproofEnabled"] = Enabled;

        public override void LoadData(TagCompound tag) => Enabled = tag.GetBool("soundproofEnabled");

        public override void NetSend(BinaryWriter writer) => writer.Write(Enabled);

        public override void NetReceive(BinaryReader reader) => Enabled = reader.ReadBoolean();

        public override bool CanRightClick() => true;

        public override bool ConsumeItem(Player player) => false;

        public override void RightClick(Player player)
        {
            Enabled = !Enabled;
            Item.NetStateChanged();
            SoundproofEarmuffsAudioSystem.PlayToggleSound(player.Center, Enabled);
        }

        public override void UpdateInventory(Player player)
        {
            if (Enabled)
                player.GetModPlayer<SoundproofEarmuffsPlayer>().SoundEffectsMuffled = true;
        }

        public override void AddRecipes()
        {
            CreateRecipe()
                .AddIngredient(ItemID.IronBar, 2)
                .AddIngredient(ItemID.Wood, 5)
                .AddTile(TileID.WorkBenches)
                .Register();
        }

        public override void ModifyTooltips(List<TooltipLine> tooltips)
        {
            string state = Enabled ? this.GetLocalizedValue("EnabledState") : this.GetLocalizedValue("DisabledState");
            tooltips.FindAndReplace("[STATE]", state);
        }
    }

    internal sealed class SoundproofEarmuffsPlayer : ModPlayer
    {
        public bool SoundEffectsMuffled;

        public override void ResetEffects() => SoundEffectsMuffled = false;
    }

    internal sealed class SoundproofEarmuffsAudioSystem : ModSystem
    {
        internal const float SoundEffectVolumeMultiplier = 0.1f;

        private static int ignoreMufflingDepth;

        private static bool ShouldMuffleSoundEffects
        {
            get
            {
                if (ignoreMufflingDepth > 0 || Main.dedServ || Main.LocalPlayer is null || !Main.LocalPlayer.active)
                    return false;

                return Main.LocalPlayer.GetModPlayer<SoundproofEarmuffsPlayer>().SoundEffectsMuffled;
            }
        }

        public override void Load()
        {
            if (Main.dedServ)
                return;

            On_ActiveSound.Play += ActiveSound_Play;
            On_ActiveSound.Update += ActiveSound_Update;
        }

        public override void Unload()
        {
            if (Main.dedServ)
                return;

            On_ActiveSound.Play -= ActiveSound_Play;
            On_ActiveSound.Update -= ActiveSound_Update;
            ignoreMufflingDepth = 0;
        }

        internal static void PlayToggleSound(Vector2 position, bool enabled)
        {
            SoundStyle toggleSound = (enabled ? SoundID.MenuOpen : SoundID.MenuClose) with
            {
                Volume = 0.72f,
                Pitch = enabled ? 0.1f : -0.08f
            };

            ignoreMufflingDepth++;
            try
            {
                SoundEngine.PlaySound(toggleSound, position);
            }
            finally
            {
                ignoreMufflingDepth--;
            }
        }

        private static void ActiveSound_Play(On_ActiveSound.orig_Play orig, ActiveSound self)
        {
            orig(self);
            ApplyMuffling(self);
        }

        private static void ActiveSound_Update(On_ActiveSound.orig_Update orig, ActiveSound self)
        {
            orig(self);
            ApplyMuffling(self);
        }

        private static void ApplyMuffling(ActiveSound sound)
        {
            if (!ShouldMuffleSoundEffects || sound.Style.Type == SoundType.Music)
                return;

            SoundEffectInstance soundInstance = sound.Sound;
            if (soundInstance is null || soundInstance.IsDisposed)
                return;

            soundInstance.Volume *= SoundEffectVolumeMultiplier;
        }
    }
}
