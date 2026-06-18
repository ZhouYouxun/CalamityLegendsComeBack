using Microsoft.Xna.Framework;
using CalamityLegendsComeBack.Weapons.SHPC;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.Skill.CtrlChip
{
    public class CtrlChipPlayer : ModPlayer
    {
        public const float ReticleLockRange = 20f * 16f;
        private const float ReticleMinEaseSpeed = 2.5f;
        private const float ReticleMaxEaseSpeed = 72f;
        private const float ReticleFullSpeedDistance = 900f;
        private const int ReticleMouseCatchupFrames = 10;

        public bool CtrlChipEquipped;
        public bool CtrlChipVisualsHidden;
        public Vector2 ReticleWorld;
        public int ReticleTargetIndex = -1;
        private int reticleMouseCatchupTimer;

        public NPC ReticleTarget =>
            Main.npc.IndexInRange(ReticleTargetIndex) && Main.npc[ReticleTargetIndex].CanBeChasedBy()
                ? Main.npc[ReticleTargetIndex]
                : null;

        public bool ReticleHasTarget => ReticleTarget != null;

        public override void ResetEffects()
        {
            CtrlChipEquipped = false;
            CtrlChipVisualsHidden = false;
        }

        public override void UpdateDead()
        {
            CtrlChipEquipped = false;
            CtrlChipVisualsHidden = false;
            ReticleWorld = Vector2.Zero;
            ReticleTargetIndex = -1;
            reticleMouseCatchupTimer = 0;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (!CtrlChipEquipped || Player.dead)
            {
                ReticleWorld = Vector2.Zero;
                ReticleTargetIndex = -1;
                reticleMouseCatchupTimer = 0;
                KillNewReticleVisual();
                return;
            }

            UpdateReticlePosition();

            if (CtrlChipVisualsHidden)
            {
                KillNewReticleVisual();
                return;
            }

            EnsureNewReticleVisual();
        }

        public static Vector2 GetAimWorld(Player player, Vector2 fallback)
        {
            CtrlChipPlayer ctrlPlayer = player.GetModPlayer<CtrlChipPlayer>();
            if (ctrlPlayer.CtrlChipEquipped && ctrlPlayer.ReticleWorld != Vector2.Zero)
                return ctrlPlayer.ReticleWorld;

            return fallback != Vector2.Zero ? fallback : Main.MouseWorld;
        }

        private void UpdateReticlePosition()
        {
            Vector2 mouseWorld = Main.MouseWorld;
            if (ReticleWorld == Vector2.Zero)
                ReticleWorld = Player.Center;

            NPC target = PlayerIsHoldingSHPC() ? FindTargetNearMouse(mouseWorld) : null;
            if (target != null)
            {
                ReticleTargetIndex = target.whoAmI;
                reticleMouseCatchupTimer = ReticleMouseCatchupFrames;
                ReticleWorld = EaseTowards(ReticleWorld, target.Center);
                return;
            }

            ReticleTargetIndex = -1;
            if (reticleMouseCatchupTimer > 0)
            {
                reticleMouseCatchupTimer--;
                ReticleWorld = EaseTowards(ReticleWorld, mouseWorld);
                return;
            }

            ReticleWorld = mouseWorld;
        }

        private NPC FindTargetNearMouse(Vector2 mouseWorld)
        {
            NPC closest = null;
            float bestDistance = ReticleLockRange;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy())
                    continue;

                float distance = DistanceToHitbox(mouseWorld, npc);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                closest = npc;
            }

            return closest;
        }

        private static float DistanceToHitbox(Vector2 point, NPC npc)
        {
            Rectangle hitbox = npc.Hitbox;
            float closestX = MathHelper.Clamp(point.X, hitbox.Left, hitbox.Right);
            float closestY = MathHelper.Clamp(point.Y, hitbox.Top, hitbox.Bottom);
            return Vector2.Distance(point, new Vector2(closestX, closestY));
        }

        private static Vector2 EaseTowards(Vector2 current, Vector2 target)
        {
            Vector2 offset = target - current;
            float distance = offset.Length();
            if (distance <= 0.5f)
                return target;

            float distanceInterpolant = Utils.GetLerpValue(0f, ReticleFullSpeedDistance, distance, true);
            float easeOut = 1f - (float)System.Math.Pow(1f - distanceInterpolant, 3);
            float step = MathHelper.Lerp(ReticleMinEaseSpeed, ReticleMaxEaseSpeed, easeOut);

            if (distance <= step)
                return target;

            return current + offset / distance * step;
        }

        private bool PlayerIsHoldingSHPC()
        {
            return Player.HeldItem?.type == ModContent.ItemType<NewLegendSHPC>();
        }

        private void EnsureNewReticleVisual()
        {
            int visualType = ModContent.ProjectileType<CtrlChipNEWReticle>();
            if (Player.ownedProjectileCounts[visualType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_Misc("CtrlChipNEWReticle"),
                ReticleWorld,
                Vector2.Zero,
                visualType,
                0,
                0f,
                Player.whoAmI);
        }

        private void KillNewReticleVisual()
        {
            int visualType = ModContent.ProjectileType<CtrlChipNEWReticle>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile projectile = Main.projectile[i];
                if (projectile.active && projectile.owner == Player.whoAmI && projectile.type == visualType)
                    projectile.Kill();
            }
        }

        private void EnsureReticleVisual()
        {
            int visualType = ModContent.ProjectileType<CtrlChipReticle>();
            if (Player.ownedProjectileCounts[visualType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_Accessory(Player.HeldItem),
                ReticleWorld,
                Vector2.Zero,
                visualType,
                0,
                0f,
                Player.whoAmI);
        }
    }
}
