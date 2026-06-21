using System.IO;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.BossAI.ReBack.Prime2041
{
    internal class Particle
    {
    }

    internal sealed class DestroyerReticleTelegraph : Particle
    {
        public DestroyerReticleTelegraph(NPC npc, Color telegraphColor, float scale, float opacity, int lifetime)
        {
        }
    }

    internal sealed class DestroyerSparkTelegraph : Particle
    {
        public DestroyerSparkTelegraph(NPC npc, Color telegraphColor, Color centerColor, float scale, int lifetime, float rotationOffset)
        {
        }
    }

    internal static class GeneralParticleHandler
    {
        public static void SpawnParticle(Particle particle)
        {
        }
    }

    public class Destroyer2041Head : Mech2041NPC
    {
        public override string Texture => $"Terraria/Images/NPC_{NPCID.TheDestroyer}";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.TheDestroyer];
            HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.TheDestroyer);
            NPC.aiStyle = -1;
            NPC.lifeMax = 80000;
            NPC.damage = 140;
            NPC.defense = 0;
            NPC.boss = true;
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.DR_NERD(0.1f);
            AnimationType = NPCID.TheDestroyer;
            Music = MusicID.Boss3;
        }

        public override void AI() => Destroyer2041AI.BuffedDestroyerAI(NPC, Mod);
    }

    public class Destroyer2041Body : Mech2041NPC
    {
        public override string Texture => $"Terraria/Images/NPC_{NPCID.TheDestroyerBody}";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.TheDestroyerBody];
            HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.TheDestroyerBody);
            NPC.aiStyle = -1;
            NPC.lifeMax = 80000;
            NPC.damage = 70;
            NPC.defense = 30;
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.DR_NERD(0.2f);
            AnimationType = NPCID.TheDestroyerBody;
        }

        public override void AI() => Destroyer2041AI.BuffedDestroyerAI(NPC, Mod);
    }

    public class Destroyer2041Tail : Mech2041NPC
    {
        public override string Texture => $"Terraria/Images/NPC_{NPCID.TheDestroyerTail}";

        public override void SetStaticDefaults()
        {
            Main.npcFrameCount[Type] = Main.npcFrameCount[NPCID.TheDestroyerTail];
            HideFromBestiary();
        }

        public override void SetDefaults()
        {
            NPC.CloneDefaults(NPCID.TheDestroyerTail);
            NPC.aiStyle = -1;
            NPC.lifeMax = 80000;
            NPC.damage = 45;
            NPC.defense = 35;
            NPC.Calamity().canBreakPlayerDefense = true;
            NPC.DR_NERD(0.35f);
            AnimationType = NPCID.TheDestroyerTail;
        }

        public override void AI() => Destroyer2041AI.BuffedDestroyerAI(NPC, Mod);
    }
}
