using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.BPrePlantera.Essence
{
    internal class EssenceofHavoc_INV : ModProjectile, ILocalizedModType
    {
        private const float HomingRange = 900f;
        private const float HomingTurnStrength = 0.072f;

        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 3;
            Projectile.height = 3;
            Projectile.friendly = true;
            Projectile.ignoreWater = false;
            Projectile.tileCollide = false;
            Projectile.penetrate = 6;
            Projectile.timeLeft = 90;
            Projectile.extraUpdates = 3;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 15;
        }
        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override void AI()
        {
            NPC target = FindTarget();
            if (target is null)
                return;

            float speed = Math.Max(Projectile.velocity.Length(), 1f);
            Vector2 currentDirection = Projectile.velocity.SafeNormalize(Vector2.UnitX);
            Vector2 desiredDirection = (target.Center - Projectile.Center).SafeNormalize(currentDirection);
            Projectile.velocity = Vector2.Lerp(currentDirection, desiredDirection, HomingTurnStrength).SafeNormalize(currentDirection) * speed;
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            // 冰冻效果（强化控制）
            target.AddBuff(BuffID.OnFire3, 180);
        }

        private NPC FindTarget()
        {
            NPC bestTarget = null;
            float bestDistance = HomingRange;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(Projectile))
                    continue;

                float distance = Vector2.Distance(Projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }
    }
}
