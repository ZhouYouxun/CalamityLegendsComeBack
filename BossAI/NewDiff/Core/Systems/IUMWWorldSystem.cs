using System.IO;
using CalamityLegendsComeBack.BossAI.NewDiff.Core.Netcode;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.BossAI.NewDiff.Core.Systems
{
    public class IUMWWorldSystem : ModSystem
    {
        private static bool iumwModeEnabled;

        public static bool IUMWModeEnabled
        {
            get => iumwModeEnabled;
            set => SetModeEnabled(value);
        }

        public static void SetModeEnabled(bool value, bool sync = true)
        {
            if (iumwModeEnabled == value)
                return;

            iumwModeEnabled = value;

            if (!value)
                IUMWDebugSystem.Clear();

            if (sync && Main.netMode != NetmodeID.SinglePlayer)
                IUMWPacketHandler.SendModeSync();
        }

        public override void SaveWorldHeader(TagCompound tag)
        {
            if (IUMWModeEnabled)
                tag["IUMWModeActive"] = true;
        }

        public override void SaveWorldData(TagCompound tag)
        {
            if (IUMWModeEnabled)
                tag["IUMWModeActive"] = true;
        }

        public override void LoadWorldData(TagCompound tag)
        {
            SetModeEnabled(tag.GetBool("IUMWModeActive"), sync: false);
        }

        public override void OnWorldLoad() => SetModeEnabled(false, sync: false);

        public override void OnWorldUnload() => SetModeEnabled(false, sync: false);

        public override void PostWorldGen() => SetModeEnabled(false, sync: false);

        public override void NetSend(BinaryWriter writer)
        {
            BitsByte flags = new()
            {
                [0] = IUMWModeEnabled
            };

            writer.Write(flags);
        }

        public override void NetReceive(BinaryReader reader)
        {
            BitsByte flags = reader.ReadByte();
            SetModeEnabled(flags[0], sync: false);
        }
    }
}
