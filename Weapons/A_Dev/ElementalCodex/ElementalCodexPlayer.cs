using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.ElementalCodex
{
    internal sealed class ElementalCodexPlayer : ModPlayer
    {
        public bool ElementalCodexEquipped;
        public int NeutralizationTimer;
        public int ControlTimer;
        public int ControlledTarget = -1;

        public override void ResetEffects()
        {
            ElementalCodexEquipped = false;
        }

        public override void UpdateDead()
        {
            NeutralizationTimer = 0;
            ControlTimer = 0;
            ControlledTarget = -1;
        }

        public override void PostUpdate()
        {
            if (NeutralizationTimer > 0)
                NeutralizationTimer--;

            if (ControlTimer > 0)
            {
                ControlTimer--;
                UpdateForcedControl();
            }
            else
                ControlledTarget = -1;
        }

        public void ApplyNeutralization(int duration)
        {
            NeutralizationTimer = System.Math.Max(NeutralizationTimer, duration);
        }

        public void ApplyControl(NPC target, int duration)
        {
            if (target == null || !target.active)
                return;

            ControlledTarget = target.whoAmI;
            ControlTimer = System.Math.Max(ControlTimer, duration);
        }

        public bool IsControllingTarget(NPC target)
        {
            return ControlTimer > 0 &&
                target != null &&
                target.active &&
                ControlledTarget == target.whoAmI;
        }

        private void UpdateForcedControl()
        {
            if (!ElementalCodexEquipped ||
                Player.dead ||
                ControlledTarget < 0 ||
                ControlledTarget >= Main.maxNPCs)
                return;

            NPC target = Main.npc[ControlledTarget];
            if (!target.active || target.friendly || target.dontTakeDamage)
            {
                ControlledTarget = -1;
                ControlTimer = 0;
                return;
            }

            Vector2 toTarget = target.Center - Player.Center;
            if (toTarget.LengthSquared() > 1600f * 1600f)
                return;

            Player.ChangeDir(toTarget.X >= 0f ? 1 : -1);
            Player.controlUseItem = true;

            if (Main.myPlayer == Player.whoAmI)
            {
                Main.mouseX = (int)(target.Center.X - Main.screenPosition.X);
                Main.mouseY = (int)(target.Center.Y - Main.screenPosition.Y);
            }
        }
    }
}
