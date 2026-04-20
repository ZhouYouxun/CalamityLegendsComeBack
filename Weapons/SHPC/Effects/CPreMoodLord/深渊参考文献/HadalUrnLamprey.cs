using CalamityMod.Buffs.DamageOverTime;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMod.Projectiles.Magic
{
    public class HadalUrnLamprey : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.Magic";
        int invistimer = 0;
        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 8;
        }

        public override void SetDefaults()
        {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.friendly = true;
            Projectile.penetrate = 5;
            Projectile.alpha = 255;
            Projectile.DamageType = DamageClass.Magic;
            Projectile.timeLeft = 600;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 60;
        }

        public override void AI()
        {
            //Briefly invisible then quickly fades in after 4 frames so it doesn't appear out of your back
            invistimer++;
            if (invistimer > 4)
            {
                if (Projectile.alpha > 0)
                {
                    Projectile.alpha -= 50;
                }
            }
            if (Projectile.alpha < 0)
            {
                Projectile.alpha = 0;
            }
            if (Projectile.ai[0] == 0f)
            {
                Projectile.rotation = Projectile.velocity.ToRotation();
            }
            // Sticky behavior is implemented locally here because Calamity's helper is internal to that assembly.
            UpdateStickyAI(15);

            Projectile.frameCounter++;
            if (Projectile.frameCounter > 6)
            {
                Projectile.frame++;
                Projectile.frameCounter = 0;
            }
            //AI exclusive to when it isn't sticking
            if (Projectile.ai[0] == 0)
            {
                if (Projectile.frame >= Main.projFrames[Type] / 2)
                {
                    Projectile.frame = 0;
                }

                //The lamprey dies sooner without any food (very sad)
                if (Projectile.timeLeft <= 420)
                {
                    Projectile.velocity *= 0.98f;
                }

                if (Projectile.timeLeft <= 390)
                {
                    Projectile.Kill();
                }
            }
            //If it IS sticking
            else
            {
                if (Projectile.frame >= Main.projFrames[Type])
                {
                    Projectile.frame = Main.projFrames[Type] / 2;
                }
            }
        }

        public override void ModifyHitNPC(NPC target, ref NPC.HitModifiers modifiers) => ApplyStickyHit(target, 4);

        public override bool? Colliding(Rectangle projHitbox, Rectangle targetHitbox)
        {
            if (targetHitbox.Width > 8 && targetHitbox.Height > 8)
            {
                targetHitbox.Inflate(-targetHitbox.Width / 8, -targetHitbox.Height / 8);
            }
            return null;
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = Terraria.GameContent.TextureAssets.Projectile[Type].Value;
            int textureheight = tex.Height / Main.projFrames[Type];
            int y = textureheight * Projectile.frame;
            Main.EntitySpriteDraw(tex, Projectile.Center - Main.screenPosition, new Microsoft.Xna.Framework.Rectangle?(new Rectangle(0, y, tex.Width, textureheight)), Projectile.GetAlpha(lightColor), Projectile.rotation, new Vector2(tex.Width, tex.Height / 16f), Projectile.scale, SpriteEffects.None, 0);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            //Black Anurian bubble dust
            int inc;
            for (int i = 0; i < 25; i = inc + 1)
            {
                int hadalDust = Dust.NewDust(Projectile.position, Projectile.width, Projectile.height, DustID.Demonite, 0f, 0f, 0, default, 1f);
                Main.dust[hadalDust].position = (Main.dust[hadalDust].position + Projectile.position) / 2f;
                Main.dust[hadalDust].velocity = new Vector2((float)Main.rand.Next(-100, 101), (float)Main.rand.Next(-100, 101));
                Main.dust[hadalDust].velocity.Normalize();
                Dust dust = Main.dust[hadalDust];
                dust.velocity *= (float)Main.rand.Next(1, 30) * 0.1f;
                Main.dust[hadalDust].alpha = Projectile.alpha;
                inc = i;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 240);
        }

        public override void OnHitPlayer(Player target, Player.HurtInfo info)
        {
            target.AddBuff(ModContent.BuffType<CrushDepth>(), 240);
        }

        public override bool? CanHitNPC(NPC target)
        {
            return null;
        }

        public override bool CanHitPvp(Player target) => Projectile.ai[0] == 0f;

        private void UpdateStickyAI(int stickSeconds)
        {
            if (Projectile.ai[0] != 1f)
                return;

            Projectile.tileCollide = false;
            Projectile.localAI[0]++;

            int npcIndex = (int)Projectile.ai[1];
            if (Projectile.localAI[0] >= 60f * stickSeconds || npcIndex < 0 || npcIndex >= Main.maxNPCs)
            {
                Projectile.Kill();
                return;
            }

            NPC npc = Main.npc[npcIndex];
            if (!npc.active || npc.dontTakeDamage)
            {
                Projectile.Kill();
                return;
            }

            Projectile.Center = npc.Center - Projectile.velocity * 2f;
            Projectile.gfxOffY = npc.gfxOffY;

            if (Projectile.localAI[0] % 30f == 0f)
                npc.HitEffect(0, 1.0);
        }

        private void ApplyStickyHit(NPC target, int maxStick)
        {
            if (Projectile.owner != Main.myPlayer || target == null || !target.active || target.dontTakeDamage)
                return;

            Projectile.ai[0] = 1f;
            Projectile.ai[1] = target.whoAmI;
            Projectile.localAI[0] = 0f;
            Projectile.velocity = target.Center - Projectile.Center;
            Projectile.netUpdate = true;

            int oldestProjectile = -1;
            int lowestTimeLeft = int.MaxValue;
            int stuckCount = 0;

            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile other = Main.projectile[i];
                if (i == Projectile.whoAmI || !other.active || other.owner != Projectile.owner || other.type != Projectile.type)
                    continue;

                if (other.ai[0] != 1f || other.ai[1] != target.whoAmI)
                    continue;

                stuckCount++;
                if (other.timeLeft < lowestTimeLeft)
                {
                    lowestTimeLeft = other.timeLeft;
                    oldestProjectile = i;
                }
            }

            if (stuckCount >= maxStick && oldestProjectile != -1)
                Main.projectile[oldestProjectile].Kill();
        }
    }
}
