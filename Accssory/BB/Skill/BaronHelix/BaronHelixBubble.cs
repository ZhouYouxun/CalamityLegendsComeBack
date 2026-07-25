using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.BB.Skill.BaronHelix
{
    public class BaronHelixBubble : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BrinyBaron";
        public override string Texture => "CalamityMod/Projectiles/InvisibleProj";

        public bool HealPlayer => Projectile.ai[0] == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 16;
            Projectile.height = 16;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.DamageType = DamageClass.Melee;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 300;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
        }

        public override void AI()
        {
            Player owner = Main.player[Projectile.owner];
            if (!owner.active || owner.dead)
            {
                Projectile.Kill();
                return;
            }

            if (HealPlayer)
            {
                // 追向玩家
                Vector2 targetPos = owner.Center;
                Vector2 direction = (targetPos - Projectile.Center).SafeNormalize(Vector2.UnitY);
                Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 12f, 0.15f);

                if (Vector2.Distance(Projectile.Center, targetPos) < 24f)
                {
                    if (Main.myPlayer == Projectile.owner)
                    {
                        owner.Heal(2);
                    }
                    SoundEngine.PlaySound(SoundID.Item3, Projectile.Center);
                    Projectile.Kill();
                }
            }
            else
            {
                // 追向敌人
                NPC target = null;
                float maxDist = 800f;
                for (int i = 0; i < Main.maxNPCs; i++)
                {
                    NPC npc = Main.npc[i];
                    if (npc.CanBeChasedBy(Projectile))
                    {
                        float dist = Vector2.Distance(Projectile.Center, npc.Center);
                        if (dist < maxDist)
                        {
                            maxDist = dist;
                            target = npc;
                        }
                    }
                }

                if (target != null)
                {
                    Vector2 direction = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitY);
                    Projectile.velocity = Vector2.Lerp(Projectile.velocity, direction * 14f, 0.18f);
                }
            }

            if (!Main.dedServ)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.Water,
                    Projectile.velocity * 0.2f,
                    100,
                    new Color(80, 200, 255),
                    1.2f);
                dust.noGravity = true;
            }
        }
    }
}
