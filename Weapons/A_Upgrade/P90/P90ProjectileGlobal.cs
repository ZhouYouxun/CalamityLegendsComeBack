using System.IO;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;
using Terraria.ModLoader.IO;

namespace CalamityLegendsComeBack.Weapons.A_Upgrade.P90
{
    internal sealed class P90ProjectileGlobal : GlobalProjectile
    {
        private const float HomingRange = 720f;
        private const int HomingDelay = 5;
        private const float HomingInertia = 18f;

        public override bool InstancePerEntity => true;

        private bool fromP90;
        private bool homing;
        private bool strongKnockback;
        private int homingTimer;

        public void Configure(bool homing, bool strongKnockback)
        {
            fromP90 = true;
            this.homing = homing;
            this.strongKnockback = strongKnockback;
            homingTimer = 0;
        }

        public override void AI(Projectile projectile)
        {
            if (!fromP90)
                return;

            if (strongKnockback && Main.rand.NextBool(5))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.RedTorch,
                    -projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.4f, 1.8f),
                    100,
                    new Color(255, 74, 74),
                    Main.rand.NextFloat(0.55f, 0.95f));
                dust.noGravity = true;
            }

            if (!homing || projectile.velocity.LengthSquared() < 1f)
                return;

            homingTimer++;
            if (homingTimer <= HomingDelay)
                return;

            NPC target = FindTarget(projectile, HomingRange);
            if (target == null)
                return;

            float speed = projectile.velocity.Length();
            Vector2 currentDirection = projectile.velocity / speed;
            Vector2 desiredDirection = (target.Center - projectile.Center).SafeNormalize(currentDirection);
            float warmup = Utils.GetLerpValue(HomingDelay, HomingDelay + 30f, homingTimer, true);
            Vector2 blendedDirection = (currentDirection * HomingInertia + desiredDirection * warmup).SafeNormalize(currentDirection);
            projectile.velocity = blendedDirection * speed;

            if (Main.rand.NextBool(4))
            {
                Dust dust = Dust.NewDustPerfect(
                    projectile.Center + Main.rand.NextVector2Circular(4f, 4f),
                    DustID.GreenTorch,
                    -projectile.velocity.SafeNormalize(Vector2.UnitX) * Main.rand.NextFloat(0.4f, 1.7f),
                    100,
                    new Color(60, 255, 126),
                    Main.rand.NextFloat(0.55f, 0.95f));
                dust.noGravity = true;
            }
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!fromP90)
                return;

            if (strongKnockback)
                modifiers.Knockback *= 3f;

            if (target.HasBuff<P90ShockDebuff>())
                modifiers.SourceDamage *= 1.1f;
        }

        public override void SendExtraAI(Projectile projectile, BitWriter bitWriter, BinaryWriter binaryWriter)
        {
            bitWriter.WriteBit(fromP90);
            bitWriter.WriteBit(homing);
            bitWriter.WriteBit(strongKnockback);
            if (fromP90)
                binaryWriter.Write((short)homingTimer);
        }

        public override void ReceiveExtraAI(Projectile projectile, BitReader bitReader, BinaryReader binaryReader)
        {
            fromP90 = bitReader.ReadBit();
            homing = bitReader.ReadBit();
            strongKnockback = bitReader.ReadBit();
            homingTimer = fromP90 ? binaryReader.ReadInt16() : 0;
        }

        private static NPC FindTarget(Projectile projectile, float range)
        {
            NPC bestTarget = null;
            float bestScore = range;
            Vector2 currentDirection = projectile.velocity.SafeNormalize(Vector2.UnitX);

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float distance = Vector2.Distance(projectile.Center, npc.Center);
                if (distance > bestScore || !Collision.CanHitLine(projectile.Center, 1, 1, npc.Center, 1, 1))
                    continue;

                Vector2 toTarget = (npc.Center - projectile.Center).SafeNormalize(currentDirection);
                float alignmentBonus = Vector2.Dot(currentDirection, toTarget) * 110f;
                float score = distance - alignmentBonus;
                if (score >= bestScore)
                    continue;

                bestScore = score;
                bestTarget = npc;
            }

            return bestTarget;
        }
    }
}
