using CalamityMod.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.CPreMoodLord
{
    public class PlagueCell_Marked : ModProjectile
    {
        public new string LocalizationCategory => "Projectiles.SHPC";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public override void SetDefaults()
        {
            Projectile.width = 10;
            Projectile.height = 10;

            Projectile.timeLeft = 35;
            Projectile.tileCollide = false;
            Projectile.penetrate = -1;

            Projectile.friendly = false;
            Projectile.hostile = false;
        }

        public override void AI()
        {
            int targetIndex = (int)Projectile.ai[0];

            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return;

            NPC target = Main.npc[targetIndex];

            if (!target.active)
                return;

            // 锁定目标
            Projectile.Center = target.Center;

            // ===== 出现时：音效 + 向上喷射红色粒子 =====
            if (Projectile.timeLeft == 5)
            {
                // 嘟一声
                SoundStyle fullCharge = new("CalamityMod/Sounds/Custom/PlagueSounds/PBGAttackSwitchShort");
                SoundEngine.PlaySound(fullCharge with { Volume = 0.9f }, Projectile.Center);

                Vector2 upward = -Vector2.UnitY;

                // 模仿火箭口：单方向喷射
                for (int i = 0; i < 12; i++)
                {
                    Vector2 velocity =
                        upward * Main.rand.NextFloat(4f, 10f) +
                        Main.rand.NextVector2Circular(0.6f, 0.6f);

                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.GemRuby,
                        velocity
                    );

                    dust.scale = Main.rand.NextFloat(0.45f, 0.75f);
                    dust.noGravity = true;
                }
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return false;
        }

        public override bool? CanDamage()
        {
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            int targetIndex = (int)Projectile.ai[0];

            if (targetIndex < 0 || targetIndex >= Main.maxNPCs)
                return;

            NPC target = Main.npc[targetIndex];

            if (!target.active)
                return;

            // ===== 起始位置：上方 + 左右随机偏移 =====
            Vector2 spawnPos = target.Center
                               + new Vector2(Main.rand.NextFloat(-16f, 16f) * 16f, -36f * 16f);

            // ===== 精准指向目标（不是垂直）=====
            Vector2 velocity = (target.Center - spawnPos).SafeNormalize(Vector2.UnitY) * 10f;

            int projID = Projectile.NewProjectile(
                Projectile.GetSource_FromThis(),
                spawnPos,
                velocity,
                ModContent.ProjectileType<HiveNuke>(),
                Projectile.damage,
                0f,
                Projectile.owner,
                ItemID.RocketI
            );

            if (Main.projectile.IndexInRange(projID))
            {
                Projectile missile = Main.projectile[projID];
                missile.friendly = true;
                missile.hostile = false;
                missile.DamageType = DamageClass.Magic;
                missile.usesLocalNPCImmunity = true;
                missile.localNPCHitCooldown = 10;
            }
        }




    }
}
