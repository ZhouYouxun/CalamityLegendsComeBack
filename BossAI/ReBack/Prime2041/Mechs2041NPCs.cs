using System.IO;
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

    public abstract class Mech2041NPC : ModNPC
    {
        protected void HideFromBestiary()
        {
            NPCID.Sets.NPCBestiaryDrawModifiers bestiaryData = new NPCID.Sets.NPCBestiaryDrawModifiers() { Hide = true };
            NPCID.Sets.NPCBestiaryDrawOffset.Add(Type, bestiaryData);
        }

        public override void SendExtraAI(BinaryWriter writer)
        {
            writer.Write(NPC.localAI[0]);
            writer.Write(NPC.localAI[1]);
            writer.Write(NPC.localAI[2]);
            writer.Write(NPC.localAI[3]);
            writer.Write(NPC.DestroyerLaserColor());
            for (int i = 0; i < 4; i++)
                writer.Write(NPC.Calamity().newAI[i]);
        }

        public override void ReceiveExtraAI(BinaryReader reader)
        {
            NPC.localAI[0] = reader.ReadSingle();
            NPC.localAI[1] = reader.ReadSingle();
            NPC.localAI[2] = reader.ReadSingle();
            NPC.localAI[3] = reader.ReadSingle();
            NPC.SetDestroyerLaserColor(reader.ReadInt32());
            for (int i = 0; i < 4; i++)
                NPC.Calamity().newAI[i] = reader.ReadSingle();
        }

        public override void ApplyDifficultyAndPlayerScaling(int numPlayers, float balance, float bossAdjustment)
        {
            NPC.lifeMax = (int)(NPC.lifeMax * balance * bossAdjustment);
            NPC.damage = (int)(NPC.damage * NPC.GetExpertDamageMultiplier());
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
