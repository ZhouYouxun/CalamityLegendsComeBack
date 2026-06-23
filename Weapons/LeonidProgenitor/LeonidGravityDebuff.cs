using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    public class LeonidGravityDebuff : ModBuff
    {
        public override string Texture => "CalamityLegendsComeBack/Weapons/GlacialEmbrace/Passive/GlacialEmbraceBuff";

        public override void SetStaticDefaults()
        {
            Main.buffNoTimeDisplay[Type] = false;
            Main.debuff[Type] = true;
            Main.pvpBuff[Type] = true;
            Main.buffNoSave[Type] = true;
            BuffID.Sets.NurseCannotRemoveDebuff[Type] = true;
        }

        public override void Update(NPC npc, ref int buffIndex)
        {
            // Zero horizontal velocity
            npc.velocity.X = 0f;

            // Constantly apply downward acceleration
            if (npc.velocity.Y < 14f)
            {
                npc.velocity.Y += 0.45f;
            }

            // Spawn purple dusts falling down
            if (Main.rand.NextBool(4))
            {
                Dust d = Dust.NewDustPerfect(
                    npc.Center + new Vector2(Main.rand.NextFloat(-npc.width * 0.5f, npc.width * 0.5f), Main.rand.NextFloat(-npc.height * 0.5f, npc.height * 0.5f)),
                    DustID.TintableDustLighted,
                    new Vector2(0f, Main.rand.NextFloat(2f, 4f)),
                    100,
                    new Color(180, 100, 255),
                    Main.rand.NextFloat(0.7f, 1.1f));
                d.noGravity = true;
            }
        }
    }
}
