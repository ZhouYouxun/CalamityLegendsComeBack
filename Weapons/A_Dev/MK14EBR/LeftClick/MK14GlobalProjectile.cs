using Microsoft.Xna.Framework;
using System;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.MK14EBR
{
    internal sealed class MK14GlobalProjectile : GlobalProjectile
    {
        private int age;
        private int originalPenetrate;
        private bool penetrationRestored;

        public override bool InstancePerEntity => true;

        public bool FiredByMK14 { get; private set; }
        public MK14Barrel Barrel { get; private set; }
        public MK14Muzzle Muzzle { get; private set; }
        public MK14Underbarrel Underbarrel { get; private set; }
        public MK14Stock Stock { get; private set; }
        public MK14Sight Sight { get; private set; }
        public float ArmorPenetration { get; private set; }
        public int InfinitePenetrationFrames { get; private set; }
        public bool ForceSingleHitAndDoubleStrike { get; private set; }
        public bool Homing { get; private set; }
        public bool NightDamageBonus { get; private set; }
        public bool RedDotRangeProfile { get; private set; }
        public bool HighPowerRangeProfile { get; private set; }
        public bool SpiderSlowOnHit { get; private set; }

        public void Configure(Projectile projectile, NewLegendMK14EBR weapon, MK14RuntimeStats stats, float spreadDegrees)
        {
            FiredByMK14 = true;
            Barrel = weapon.Barrel;
            Muzzle = weapon.Muzzle;
            Underbarrel = weapon.Underbarrel;
            Stock = weapon.Stock;
            Sight = weapon.Sight;
            ArmorPenetration = stats.ArmorPenetration;
            InfinitePenetrationFrames = stats.InfinitePenetrationFrames;
            ForceSingleHitAndDoubleStrike = stats.ForceSingleHitAndDoubleStrike;
            Homing = stats.Homing;
            NightDamageBonus = stats.NightDamageBonus;
            RedDotRangeProfile = stats.RedDotRangeProfile;
            HighPowerRangeProfile = stats.HighPowerRangeProfile;
            SpiderSlowOnHit = stats.SpiderSlowOnHit;
            age = 0;

            if (stats.ExtraPenetration > 0)
            {
                if (projectile.penetrate > 0)
                    projectile.penetrate += stats.ExtraPenetration;

                if (projectile.maxPenetrate > 0)
                    projectile.maxPenetrate += stats.ExtraPenetration;
            }

            originalPenetrate = projectile.penetrate;
            penetrationRestored = false;

            if (ForceSingleHitAndDoubleStrike)
            {
                projectile.penetrate = 1;
                projectile.maxPenetrate = 1;
            }
        }

        public override void AI(Projectile projectile)
        {
            if (!FiredByMK14)
                return;

            age++;
            if (InfinitePenetrationFrames > 0)
            {
                int infiniteTicks = InfinitePenetrationFrames * (projectile.extraUpdates + 1);
                if (age <= infiniteTicks)
                    projectile.penetrate = -1;
                else if (!penetrationRestored && !ForceSingleHitAndDoubleStrike)
                {
                    projectile.penetrate = originalPenetrate;
                    projectile.maxPenetrate = originalPenetrate;
                    penetrationRestored = true;
                }
            }

            if (Homing)
                ApplyHoming(projectile);
        }

        public override void ModifyHitNPC(Projectile projectile, NPC target, ref NPC.HitModifiers modifiers)
        {
            if (!FiredByMK14)
                return;

            if (ArmorPenetration > 0f)
                modifiers.ArmorPenetration += ArmorPenetration;

            Player owner = GetOwner(projectile);
            float distance = owner != null ? Vector2.Distance(owner.Center, target.Center) : 0f;

            if (RedDotRangeProfile)
            {
                if (distance <= 25f * 16f)
                    modifiers.FinalDamage *= 1.15f;
                else if (distance >= 60f * 16f)
                    modifiers.FinalDamage *= 0.9f;
            }

            if (HighPowerRangeProfile)
            {
                float rangeBonus = 1f + Utils.GetLerpValue(0f, 75f * 16f, distance, true) * 0.3f;
                modifiers.FinalDamage *= rangeBonus;
            }

            if (NightDamageBonus && (!Main.dayTime || Main.eclipse))
                modifiers.FinalDamage *= 1.9f;

            if (target.GetGlobalNPC<MK14DragonBreathGlobalNPC>().IsMarkedBy(projectile.owner))
                modifiers.FinalDamage *= 1.16f;
        }

        public override void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (!FiredByMK14)
                return;

            if (ForceSingleHitAndDoubleStrike && projectile.owner == Main.myPlayer && target.active)
            {
                int extraDamage = Math.Max(1, projectile.damage * 2);
                target.StrikeNPC(target.CalculateHitInfo(extraDamage, hit.HitDirection));
            }

            if (SpiderSlowOnHit)
                target.GetGlobalNPC<MK14DragonBreathGlobalNPC>().ApplySpiderSlow(30);

            if (target.GetGlobalNPC<MK14DragonBreathGlobalNPC>().IsMarkedBy(projectile.owner))
                target.AddBuff(BuffID.OnFire3, 180);
        }

        private static Player GetOwner(Projectile projectile)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return null;

            Player owner = Main.player[projectile.owner];
            return owner.active ? owner : null;
        }

        private void ApplyHoming(Projectile projectile)
        {
            NPC target = FindTarget(projectile, 900f);
            if (target == null)
                return;

            float speed = projectile.velocity.Length();
            if (speed <= 0.01f)
                return;

            Vector2 desiredVelocity = (target.Center - projectile.Center).SafeNormalize(projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            float unlock = Utils.GetLerpValue(0f, 90f, age / (float)(projectile.extraUpdates + 1), true);
            float maxTurn = MathHelper.Lerp(MathHelper.ToRadians(4f), MathHelper.Pi, unlock);
            float turnStrength = MathHelper.Lerp(0.16f, 1f, unlock);

            float currentRotation = projectile.velocity.ToRotation();
            float rotationOffset = MathHelper.WrapAngle(projectile.rotation - currentRotation);
            float desiredRotation = desiredVelocity.ToRotation();
            float turn = MathHelper.Clamp(MathHelper.WrapAngle(desiredRotation - currentRotation), -maxTurn, maxTurn);
            float newRotation = currentRotation + turn * turnStrength;
            projectile.velocity = newRotation.ToRotationVector2() * speed;
            projectile.rotation = projectile.velocity.ToRotation() + rotationOffset;
        }

        private static NPC FindTarget(Projectile projectile, float range)
        {
            NPC bestTarget = null;
            float bestDistance = range;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.CanBeChasedBy(projectile))
                    continue;

                float distance = Vector2.Distance(projectile.Center, npc.Center);
                if (distance >= bestDistance)
                    continue;

                bestDistance = distance;
                bestTarget = npc;
            }

            return bestTarget;
        }
    }

    internal sealed class MK14DragonBreathGlobalNPC : GlobalNPC
    {
        private readonly int[] markTimers = new int[Main.maxPlayers];
        private int spiderSlowTimer;

        public override bool InstancePerEntity => true;

        public void ApplyMark(int owner, int timeLeft)
        {
            if (owner < 0 || owner >= Main.maxPlayers)
                return;

            if (markTimers[owner] < timeLeft)
                markTimers[owner] = timeLeft;
        }

        public void ApplySpiderSlow(int timeLeft)
        {
            if (spiderSlowTimer < timeLeft)
                spiderSlowTimer = timeLeft;
        }

        public bool IsMarkedBy(int owner)
        {
            return owner >= 0 && owner < Main.maxPlayers && markTimers[owner] > 0;
        }

        public override void PostAI(NPC npc)
        {
            for (int i = 0; i < markTimers.Length; i++)
            {
                if (markTimers[i] > 0)
                    markTimers[i]--;
            }

            if (spiderSlowTimer > 0)
            {
                npc.position -= npc.velocity * 0.1f;
                spiderSlowTimer--;
            }
        }

        public override void DrawEffects(NPC npc, ref Color drawColor)
        {
            if (spiderSlowTimer > 0)
                drawColor = Color.Lerp(drawColor, new Color(155, 220, 255), 0.18f);

            if (!HasAnyMark())
                return;

            drawColor = Color.Lerp(drawColor, new Color(255, 130, 72), 0.26f);
            Lighting.AddLight(npc.Center, new Vector3(0.35f, 0.11f, 0.03f));
            if (Main.rand.NextBool(4))
            {
                Dust ember = Dust.NewDustPerfect(
                    npc.Center + Main.rand.NextVector2Circular(npc.width * 0.45f, npc.height * 0.45f),
                    DustID.Torch,
                    Main.rand.NextVector2Circular(1.2f, 1.2f),
                    120,
                    Color.OrangeRed,
                    Main.rand.NextFloat(0.65f, 1.05f));
                ember.noGravity = true;
            }
        }

        private bool HasAnyMark()
        {
            for (int i = 0; i < markTimers.Length; i++)
            {
                if (markTimers[i] > 0)
                    return true;
            }

            return false;
        }
    }
}
