using CalamityLegendsComeBack.Weapons.SHPC.Effects.AAARules;
using CalamityMod.Items.Materials;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.GameContent;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.SHPC.Effects.DPreDog
{
    public class DarkPlasmaEffect : DefaultEffect
    {
        public override int EffectID => 32;

        public override int AmmoType => ModContent.ItemType<DarkPlasma>();

        public override Color ThemeColor => new Color(20, 20, 20);
        public override Color StartColor => new Color(80, 80, 80);
        public override Color EndColor => new Color(5, 5, 5);

        public override float SquishyLightParticleFactor => 1.85f;
        public override float ExplosionPulseFactor => 1.85f;

        private float portalTimer;

        // ================= OnSpawn =================
        public override void OnSpawn(Projectile projectile, Player owner)
        {
            portalTimer = 0f;

            projectile.velocity *= 0.6f; // 黑洞偏慢
            projectile.tileCollide = false;
            projectile.penetrate = -1;
        }

        // ================= AI =================
        public override void AI(Projectile projectile, Player owner)
        {
            portalTimer += 0.03f;

            // ===== 追踪鼠标（缓慢吸附）=====
            Vector2 targetPos = Main.MouseWorld;
            Vector2 toTarget = (targetPos - projectile.Center);
            float dist = toTarget.Length();

            if (dist > 10f)
            {
                Vector2 dir = toTarget / dist;
                projectile.velocity = (projectile.velocity * 25f + dir * 1.8f) / 26f;
            }

            // ===== 吸附敌人（参考 Cyclone）=====
            float range = 320f;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage)
                    continue;

                float distance = Vector2.Distance(projectile.Center, npc.Center);
                if (distance < range)
                {
                    Vector2 pull = (projectile.Center - npc.Center).SafeNormalize(Vector2.UnitY);

                    float strength = MathHelper.Lerp(0.05f, 0.25f, 1f - distance / range);

                    npc.velocity += pull * strength;

                    // 持续伤害
                    if (Main.rand.NextBool(6))
                        npc.StrikeNPC(npc.CalculateHitInfo(projectile.damage / 5, 0));
                }
            }

            // ===== 黑洞粒子 =====
            if (Main.rand.NextBool(2))
            {
                Vector2 circle = Main.rand.NextVector2CircularEdge(1f, 1f);
                Vector2 spawn = projectile.Center + circle * Main.rand.NextFloat(20f, 90f);

                Vector2 vel = (projectile.Center - spawn) * 0.08f;

                SquishyLightParticle p = new(
                    spawn,
                    vel,
                    Main.rand.NextFloat(0.4f, 0.9f),
                    Color.Lerp(StartColor, ThemeColor, Main.rand.NextFloat()),
                    Main.rand.Next(14, 24)
                );

                GeneralParticleHandler.SpawnParticle(p);
            }

            Lighting.AddLight(projectile.Center, ThemeColor.ToVector3() * 0.4f);
        }

        // ================= ModifyHitNPC =================
        public override void ModifyHitNPC(Projectile projectile, Player owner, NPC target, ref NPC.HitModifiers modifiers)
        {
        }

        // ================= OnHitNPC =================
        public override void OnHitNPC(Projectile projectile, Player owner, NPC target, NPC.HitInfo hit, int damageDone)
        {
        }

        // ================= OnKill =================
        public override void OnKill(Projectile projectile, Player owner, int timeLeft)
        {
            // ===== 大爆炸 =====
            int projIndex = Projectile.NewProjectile(
                projectile.GetSource_FromThis(),
                projectile.Center,
                Vector2.Zero,
                ModContent.ProjectileType<NewLegendSHPE>(),
                projectile.damage,
                projectile.knockBack,
                projectile.owner
            );

            Projectile proj = Main.projectile[projIndex];
            proj.width = 250;
            proj.height = 250;
        }

        // ================= PreDraw =================
        public override void PreDraw(Projectile projectile, Player owner, SpriteBatch spriteBatch)
        {
            Texture2D texture = ModContent.Request<Texture2D>("CalamityMod/ExtraTextures/GreyscaleVortex").Value;

            Vector2 drawPos = projectile.Center - Main.screenPosition;
            Vector2 origin = texture.Size() * 0.5f;

            float scale01;

            if (projectile.timeLeft > 360)
                scale01 = Utils.GetLerpValue(420f, 360f, projectile.timeLeft, true);
            else if (projectile.timeLeft >= 60)
                scale01 = 1f;
            else
                scale01 = Utils.GetLerpValue(0f, 60f, projectile.timeLeft, true);

            for (int i = 0; i < 13; i++)
            {
                Color c = Color.Lerp(new Color(30, 30, 30), Color.Black, i * 0.1f);
                c.A = 0;

                Main.EntitySpriteDraw(
                    texture,
                    drawPos,
                    null,
                    c * scale01 * 0.5f,
                    projectile.rotation * 3f - i * 0.15f,
                    origin,
                    MathHelper.Clamp(scale01 * 0.4f - i * 0.025f, 0f, 5f),
                    SpriteEffects.None
                );
            }
        }






    }
}