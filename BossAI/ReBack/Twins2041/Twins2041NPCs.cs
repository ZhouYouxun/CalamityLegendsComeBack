using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    internal static class Twins2041State
    {
        public static int retinazer = -1;
        public static int spazmatism = -1;
    }

    public abstract class Twins2041NPC : Mech2041NPC
    {
        protected abstract int VanillaType { get; }

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[VanillaType];
            HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(VanillaType);
            NPC.aiStyle = -1;
            NPC.lifeMax = 28000;
            NPC.damage = 85;
            NPC.defense = 10;
            NPC.boss = true;
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.DR_NERD(0.2f);
            AnimationType = VanillaType;
            Music = MusicID.Boss2;
        }
    }

    public class Twins2041Retinazer : Twins2041NPC
    {
        public override string Texture => $"Terraria/Images/NPC_{NPCID.Retinazer}";
        protected override int VanillaType => NPCID.Retinazer;

        public override void AI() => Twins2041AI.BuffedRetinazerAI(NPC, Mod);
    }

    public class Twins2041Spazmatism : Twins2041NPC
    {
        public override string Texture => $"Terraria/Images/NPC_{NPCID.Spazmatism}";
        protected override int VanillaType => NPCID.Spazmatism;

        public override void AI() => Twins2041AI.BuffedSpazmatismAI(NPC, Mod);
    }
}
