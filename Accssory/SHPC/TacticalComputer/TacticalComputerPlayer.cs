using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.TacticalComputer
{
    public class TacticalComputerPlayer : ModPlayer
    {
        public const float ReticleLockRange = 15f * 16f;
        private const float ReticleSnapSpeed = 30f;

        public bool TacticalComputerEquipped;
        public Vector2 ReticleWorld;
        public int ReticleTargetIndex = -1;

        public NPC ReticleTarget =>
            Main.npc.IndexInRange(ReticleTargetIndex) && Main.npc[ReticleTargetIndex].CanBeChasedBy()
                ? Main.npc[ReticleTargetIndex]
                : null;

        public bool ReticleHasTarget => ReticleTarget != null;

        public override void ResetEffects()
        {
            TacticalComputerEquipped = false;
        }

        public override void UpdateDead()
        {
            TacticalComputerEquipped = false;
            ReticleWorld = Vector2.Zero;
            ReticleTargetIndex = -1;
        }

        public override void PostUpdate()
        {
            if (Player.whoAmI != Main.myPlayer)
                return;

            if (!TacticalComputerEquipped || Player.dead)
            {
                ReticleWorld = Vector2.Zero;
                ReticleTargetIndex = -1;
                return;
            }

            UpdateReticlePosition();
            EnsureReticleVisual();
        }

        public static Vector2 GetAimWorld(Player player, Vector2 fallback)
        {
            TacticalComputerPlayer tacticalPlayer = player.GetModPlayer<TacticalComputerPlayer>();
            if (tacticalPlayer.TacticalComputerEquipped && tacticalPlayer.ReticleWorld != Vector2.Zero)
                return tacticalPlayer.ReticleWorld;

            return fallback != Vector2.Zero ? fallback : Main.MouseWorld;
        }

        private void UpdateReticlePosition()
        {
            Vector2 mouseWorld = Main.MouseWorld;
            if (ReticleWorld == Vector2.Zero || Vector2.DistanceSquared(ReticleWorld, mouseWorld) > 2400f * 2400f)
                ReticleWorld = mouseWorld;

            NPC target = FindTargetNearMouse(mouseWorld);
            ReticleTargetIndex = target?.whoAmI ?? -1;

            if (target == null)
            {
                ReticleWorld = mouseWorld;
                return;
            }

            ReticleWorld = MoveTowards(ReticleWorld, target.Center, ReticleSnapSpeed);
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

        private static Vector2 MoveTowards(Vector2 current, Vector2 target, float maxDistance)
        {
            Vector2 offset = target - current;
            float distance = offset.Length();
            if (distance <= maxDistance || distance <= 0f)
                return target;

            return current + offset / distance * maxDistance;
        }

        private void EnsureReticleVisual()
        {
            int visualType = ModContent.ProjectileType<TacticalComputerReticle>();
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

    internal sealed class TacticalComputerReticle : ModProjectile
    {
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 2;
            Projectile.height = 2;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 2;
        }

        public override bool ShouldUpdatePosition() => false;
        public override bool? CanDamage() => false;

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            TacticalComputerPlayer tacticalPlayer = owner.GetModPlayer<TacticalComputerPlayer>();
            if (!tacticalPlayer.TacticalComputerEquipped || tacticalPlayer.ReticleWorld == Vector2.Zero)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = tacticalPlayer.ReticleWorld;
            Projectile.timeLeft = 2;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Player owner = Main.player[Projectile.owner];
            if (owner.whoAmI != Main.myPlayer)
                return false;

            TacticalComputerPlayer tacticalPlayer = owner.GetModPlayer<TacticalComputerPlayer>();
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            bool locked = tacticalPlayer.ReticleHasTarget;
            float time = Main.GlobalTimeWrappedHourly;
            float pulse = 0.78f + 0.22f * (float)System.Math.Sin(time * (locked ? 10f : 7f));

            Texture2D glow = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomCircle").Value;
            Texture2D ringA = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_03").Value;
            Texture2D ringB = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/magic_04").Value;
            Texture2D line = ModContent.Request<Texture2D>("CalamityMod/Particles/BloomLineSoftEdge").Value;

            Color techBlue = new(70, 190, 255, 0);
            Color cyan = new(150, 245, 255, 0);
            Color white = new(235, 255, 255, 0);
            Color outerColor = Color.Lerp(techBlue, cyan, locked ? 0.7f : 0.35f);
            Color innerColor = Color.Lerp(cyan, white, locked ? 0.65f : 0.35f);
            float lockInterpolant = locked ? 1f : 0f;
            float ringScale = MathHelper.Lerp(0.34f, 0.48f, lockInterpolant) * pulse;
            float tickRadius = MathHelper.Lerp(28f, 42f, lockInterpolant);

            Main.EntitySpriteDraw(glow, drawPosition, null, outerColor * 0.52f, 0f, glow.Size() * 0.5f, ringScale, SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(glow, drawPosition, null, innerColor * 0.34f, 0f, glow.Size() * 0.5f, ringScale * 0.48f, SpriteEffects.None, 0f);

            Main.EntitySpriteDraw(ringA, drawPosition, null, outerColor * 0.88f, time * 0.95f, ringA.Size() * 0.5f, MathHelper.Lerp(0.46f, 0.58f, lockInterpolant), SpriteEffects.None, 0f);
            Main.EntitySpriteDraw(ringB, drawPosition, null, cyan * 0.76f, -time * 0.72f, ringB.Size() * 0.5f, MathHelper.Lerp(0.42f, 0.54f, lockInterpolant), SpriteEffects.FlipHorizontally, 0f);

            for (int i = 0; i < 4; i++)
            {
                float angle = time * (locked ? 2.4f : 1.5f) + MathHelper.PiOver2 * i;
                Vector2 direction = angle.ToRotationVector2();
                Vector2 tickPosition = drawPosition + direction * tickRadius;
                Main.EntitySpriteDraw(
                    line,
                    tickPosition,
                    null,
                    outerColor * 0.85f,
                    angle + MathHelper.PiOver2,
                    line.Size() * 0.5f,
                    new Vector2(0.036f, MathHelper.Lerp(0.14f, 0.22f, lockInterpolant)),
                    SpriteEffects.None,
                    0f);
            }

            if (locked)
                Main.EntitySpriteDraw(glow, drawPosition, null, white * 0.32f, 0f, glow.Size() * 0.5f, 0.14f * pulse, SpriteEffects.None, 0f);

            return false;
        }
    }
}
