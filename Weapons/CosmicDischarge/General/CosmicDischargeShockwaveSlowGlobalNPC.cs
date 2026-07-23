using System;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.CosmicDischarge
{
    public sealed class CosmicDischargeShockwaveSlowGlobalNPC : GlobalNPC
    {
        private int slowTime;
        private float speedLimit;

        public override bool InstancePerEntity => true;

        public void ApplyShockwave(NPC npc, int duration, float speedMultiplier)
        {
            float currentSpeed = npc.velocity.Length();
            speedLimit = slowTime > 0
                ? Math.Min(speedLimit, Math.Max(3f, currentSpeed * speedMultiplier))
                : Math.Max(3f, currentSpeed * speedMultiplier);
            slowTime = Math.Max(slowTime, duration);
            npc.velocity *= speedMultiplier;
            npc.netUpdate = true;
        }

        public override void PostAI(NPC npc)
        {
            if (slowTime <= 0)
                return;

            slowTime--;
            if (npc.velocity.LengthSquared() > speedLimit * speedLimit)
                npc.velocity = npc.velocity.SafeNormalize(Microsoft.Xna.Framework.Vector2.Zero) * speedLimit;
        }
    }
}
