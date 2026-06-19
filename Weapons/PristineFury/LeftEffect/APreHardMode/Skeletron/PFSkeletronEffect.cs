using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.PristineFury.LeftEffect
{
    internal static class PFSkeletronEffect
    {
        private const int FireInterval = 28;
        private const float FanAngle = MathHelper.PiOver2; // 90° 扇形搜索
        private const float SearchRange = 720f;
        private const int MaxTargets = 3;
        private const int BurstPerTarget = 10;

        internal static void Update(NewLegendPristineFuryHoldOut holdout, bool held, bool justPressed, bool justReleased)
        {
            if (!held)
            {
                PristineFuryLeftEffectRegistry.Reset(holdout);
                return;
            }

            holdout.LeftTimer++;
            if (holdout.LeftTimer < FireInterval)
                return;

            holdout.LeftTimer = 0;
            FireFromEnemies(holdout);
        }

        private static void FireFromEnemies(NewLegendPristineFuryHoldOut holdout)
        {
            if (Main.myPlayer != holdout.Projectile.owner)
                return;

            Vector2 forward = holdout.AimDirection.SafeNormalize(Vector2.UnitX);
            Vector2 origin = holdout.Owner.MountedCenter;
            float cosHalfFan = System.MathF.Cos(FanAngle * 0.5f);

            // 水库采样随机挑选最多 MaxTargets 个扇形内敌人
            NPC[] targets = new NPC[MaxTargets];
            int count = 0;
            int seen = 0;

            foreach (NPC npc in Main.ActiveNPCs)
            {
                if (!npc.CanBeChasedBy(holdout.Projectile))
                    continue;

                Vector2 toTarget = npc.Center - origin;
                if (toTarget.Length() > SearchRange)
                    continue;

                float dot = Vector2.Dot(toTarget.SafeNormalize(forward), forward);
                if (dot < cosHalfFan)
                    continue;

                seen++;
                if (count < MaxTargets)
                    targets[count++] = npc;
                else
                {
                    int replace = Main.rand.Next(seen);
                    if (replace < MaxTargets)
                        targets[replace] = npc;
                }
            }

            if (count == 0)
                return;

            int projType = ModContent.ProjectileType<PFSkeletronWhiteBurst>();
            int damage = holdout.GetScaledDamage(0.28f);

            for (int t = 0; t < count; t++)
            {
                Vector2 center = targets[t].Center;
                for (int i = 0; i < BurstPerTarget; i++)
                {
                    Vector2 velocity = Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.8f, 7.2f);
                    int proj = Projectile.NewProjectile(
                        holdout.Projectile.GetSource_FromThis(),
                        center + Main.rand.NextVector2Circular(10f, 10f),
                        velocity,
                        projType,
                        damage,
                        holdout.Projectile.knockBack * 0.25f,
                        holdout.Projectile.owner);
                    PFLeftEffectRules.ApplyTheme(proj, holdout.CurrentMark);
                }
            }

            holdout.LeftBurstIndex += BurstPerTarget * count;
            SoundEngine.PlaySound(SoundID.Item8 with { Volume = 0.55f, Pitch = -0.22f }, holdout.GunTipPosition);
            SoundEngine.PlaySound(SoundID.NPCDeath2 with { Volume = 0.38f, Pitch = 0.18f }, holdout.GunTipPosition);
        }
    }

    internal sealed class PFSkeletronWhiteBurst : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.PristineFury";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        private ref float Timer => ref Projectile.localAI[0];
        private bool IsHoming => Timer > 42f;

        public override void SetDefaults()
        {
            Projectile.width = Projectile.height = 12;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 180;
            Projectile.extraUpdates = 1;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 16;
        }

        public override void AI()
        {
            Timer++;

            if (!IsHoming)
            {
                // 胡乱飞行阶段：随机扰动 + 轻微减速
                Projectile.velocity += Main.rand.NextVector2Circular(0.55f, 0.55f);
                Projectile.velocity *= 0.972f;
            }
            else
            {
                // 强行追踪阶段
                StrongHome();
            }

            Projectile.Opacity = MathHelper.Clamp(Timer / 8f, 0f, 1f) * Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            Lighting.AddLight(Projectile.Center, Color.White.ToVector3() * 0.22f);

            if (!Main.dedServ && Projectile.numUpdates == 0)
                EmitWhiteDust();
        }

        private void StrongHome()
        {
            NPC target = Projectile.Center.ClosestNPCAt(860f);
            if (target == null)
                return;

            float trackPower = Utils.GetLerpValue(42f, 85f, Timer, true);
            float speed = MathHelper.Lerp(5f, 17f, trackPower);
            Vector2 desired = (target.Center - Projectile.Center).SafeNormalize(Projectile.velocity.SafeNormalize(Vector2.UnitX)) * speed;
            Projectile.velocity = Vector2.Lerp(Projectile.velocity, desired, 0.10f + trackPower * 0.16f);
        }

        private void EmitWhiteDust()
        {
            for (int i = 0; i < 2; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(3f, 3f),
                    DustID.WhiteTorch,
                    -Projectile.velocity * Main.rand.NextFloat(0.1f, 0.35f) + Main.rand.NextVector2Circular(0.4f, 0.4f),
                    0, Color.White,
                    Main.rand.NextFloat(0.55f, 0.95f));
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<Shadowflame>(), 150);
            if (!Main.dedServ)
                ReleasePulse(target.Center, 7);
        }

        public override void OnKill(int timeLeft)
        {
            if (!Main.dedServ)
                ReleasePulse(Projectile.Center, 9);
        }

        private void ReleasePulse(Vector2 center, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Dust d = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(6f, 6f),
                    DustID.WhiteTorch,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(2.2f, 6.5f),
                    0, Color.White,
                    Main.rand.NextFloat(0.65f, 1.15f));
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor) => false;
    }
}
