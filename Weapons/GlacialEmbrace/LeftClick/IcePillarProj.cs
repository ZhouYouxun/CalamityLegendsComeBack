using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;
using CalamityMod;
using CalamityLegendsComeBack.Weapons.GlacialEmbrace.General;

namespace CalamityLegendsComeBack.Weapons.GlacialEmbrace.LeftClick
{
    public class IcePillarProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Rogue/Icebreaker";

        public override void SetDefaults()
        {
            Projectile.width = 36;
            Projectile.height = 36;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            // 轻微重力下坠
            Projectile.velocity.Y += 0.12f;

            // 贴图倾斜45°，旋转补偿 PiOver4
            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver2 + MathHelper.PiOver4;

            // 拖尾粒子
            for (int i = 0; i < 3; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = -Projectile.velocity * Main.rand.NextFloat(0.2f, 0.4f);
                d.scale = Main.rand.NextFloat(1.0f, 1.6f);
                d.noGravity = true;
            }
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            Player player = Main.player[Projectile.owner];
            var modPlayer = player.GetModPlayer<GlacialEmbracePlayer>();

            target.AddBuff(BuffID.Frostburn2, 300);
            modPlayer.UltimateCharge = Math.Min(GlacialEmbracePlayer.MaxUltimateCharge, modPlayer.UltimateCharge + 4);
            modPlayer.OnSpecialHitNPC();

            // 强制击退（Boss较弱，血肉墙和Boss仆从跳过）
            bool wallOfFlesh = target.type == NPCID.WallofFlesh || target.type == NPCID.WallofFleshEye;
            bool bossServant = target.realLife >= 0 && !target.boss;
            if (!wallOfFlesh && !bossServant)
            {
                float pushStrength = target.boss ? 3.2f : 10.5f;
                target.velocity += Projectile.velocity.SafeNormalize(Vector2.UnitX) * pushStrength;
            }

            // 引爆所有冰刺产生冲击波并休眠
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == spikeType && p.owner == player.whoAmI)
                {
                    var spike = p.ModProjectile as IceSpikeMinion;
                    if (spike != null && !spike.hibernating)
                        spike.ForceShockwaveAndHibernate(player);
                }
            }

            // 大范围爆炸
            var source = player.GetSource_ItemUse(player.HeldItem);
            int expl = Projectile.NewProjectile(source, Projectile.Center, Vector2.Zero,
                ProjectileID.SolarWhipSwordExplosion,
                (int)(Projectile.damage * 1.5f), 8f, player.whoAmI);
            if (Main.projectile.IndexInRange(expl))
            {
                Main.projectile[expl].friendly = true;
                Main.projectile[expl].hostile = false;
                Main.projectile[expl].DamageType = DamageClass.Summon;
            }

            SoundEngine.PlaySound(SoundID.Item62 with { Volume = 1f, Pitch = -0.2f }, Projectile.Center);
            player.Calamity().GeneralScreenShakePower = Math.Max(player.Calamity().GeneralScreenShakePower, 4.5f);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 35; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = Main.rand.NextVector2Circular(8f, 8f);
                d.scale = Main.rand.NextFloat(1.3f, 2.4f);
                d.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;

            Color col = Color.Lerp(new Color(100, 220, 255), Color.White, 0.25f + 0.15f * MathF.Sin(time * 5f));

            // 主贴图
            Main.spriteBatch.Draw(tex, drawPos, null, col * 0.95f,
                Projectile.rotation, tex.Size() * 0.5f, 1.1f, SpriteEffects.None, 0f);
            // 内层高光
            Main.spriteBatch.Draw(tex, drawPos, null, Color.White * 0.35f,
                Projectile.rotation, tex.Size() * 0.5f, 0.85f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
