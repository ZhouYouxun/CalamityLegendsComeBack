using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal sealed class P90ShockDebuff : ModBuff
    {
        public override string Texture => "CalamityMod/Buffs/StatDebuffs/GalvanicCorrosion";

        public override void SetStaticDefaults()
        {
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.LongerExpertDebuff[Type] = false;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            Lighting.AddLight(npc.Center, new Vector3(0.35f, 0.55f, 1f) * 0.55f);

            if (Main.rand.NextBool(3))
            {
                Dust dust = Dust.NewDustDirect(npc.position, npc.width, npc.height, DustID.Electric, 0f, 0f, 100, default, Main.rand.NextFloat(0.7f, 1.15f));
                dust.velocity = npc.velocity * 0.4f + Main.rand.NextVector2Circular(2.4f, 2.4f);
                dust.noGravity = true;
            }

            if (Main.rand.NextBool(6))
            {
                Vector2 sparkPosition = npc.Center + Main.rand.NextVector2Circular(npc.width * 0.5f, npc.height * 0.5f);
                Particle spark = new ElectricSpark(
                    sparkPosition,
                    Main.rand.NextVector2CircularEdge(6f, 6f),
                    Color.Lerp(Color.White, new Color(120, 200, 255), 0.5f),
                    new Color(90, 180, 255),
                    Main.rand.NextFloat(0.55f, 0.95f),
                    Main.rand.Next(16, 26),
                    bloomScale: 1.6f);
                GeneralParticleHandler.SpawnParticle(spark);
            }
        }
    }
}
