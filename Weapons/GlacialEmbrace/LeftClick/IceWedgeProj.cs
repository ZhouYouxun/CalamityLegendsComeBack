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
    public class IceWedgeProj : ModProjectile
    {
        public override string Texture => "CalamityMod/Items/Weapons/Melee/Avalanche";

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.ignoreWater = true;
            Projectile.tileCollide = true;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 120;
            Projectile.DamageType = DamageClass.Summon;
        }

        public override void AI()
        {
            // 轻微寻敌追踪
            NPC target = CalamityUtils.MinionHoming(Projectile.Center, 500f, Main.player[Projectile.owner]);
            if (target != null)
            {
                Vector2 targetDir = (target.Center - Projectile.Center).SafeNormalize(Vector2.UnitX);
                Projectile.velocity = (Projectile.velocity * 15f + targetDir * 14f) / 16f;
            }

            Projectile.rotation = Projectile.velocity.ToRotation() + MathHelper.PiOver4;

            // 冰霜尾迹
            for (int i = 0; i < 3; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = -Projectile.velocity * Main.rand.NextFloat(0.15f, 0.35f);
                d.scale = Main.rand.NextFloat(0.8f, 1.3f);
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

            // 引爆该敌人身上所有嵌入冰刺
            int spikeType = ModContent.ProjectileType<IceSpikeMinion>();
            for (int i = 0; i < Main.maxProjectiles; i++)
            {
                Projectile p = Main.projectile[i];
                if (p.active && p.type == spikeType && p.owner == player.whoAmI)
                {
                    var spike = p.ModProjectile as IceSpikeMinion;
                    if (spike != null && spike.embedded && spike.embedNPCIndex == target.whoAmI)
                        spike.PierceThrust(Vector2.Zero);
                }
            }

            SoundEngine.PlaySound(SoundID.Item27 with { Pitch = 0.5f, Volume = 0.8f }, Projectile.Center);
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 22; i++)
            {
                int dType = Main.rand.NextBool(2) ? DustID.Frost : DustID.Ice;
                Dust d = Dust.NewDustDirect(Projectile.position, Projectile.width, Projectile.height, dType);
                d.velocity = Main.rand.NextVector2Circular(6f, 6f);
                d.scale = Main.rand.NextFloat(1.0f, 1.8f);
                d.noGravity = true;
            }
            SoundEngine.PlaySound(SoundID.Item27 with { Pitch = 0.3f, Volume = 0.6f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D tex = TextureAssets.Projectile[Type].Value;
            Vector2 drawPos = Projectile.Center - Main.screenPosition;
            float time = Main.GlobalTimeWrappedHourly;

            Color col = Color.Lerp(new Color(80, 210, 255, 0), new Color(255, 255, 255, 0), 0.3f + 0.2f * MathF.Sin(time * 6f));

            Main.spriteBatch.Draw(tex, drawPos, null, col * 0.95f,
                Projectile.rotation, tex.Size() * 0.5f, 0.9f, SpriteEffects.None, 0f);
            Main.spriteBatch.Draw(tex, drawPos, null, new Color(255, 255, 255, 0) * 0.3f,
                Projectile.rotation, tex.Size() * 0.5f, 0.7f, SpriteEffects.None, 0f);

            return false;
        }
    }
}
