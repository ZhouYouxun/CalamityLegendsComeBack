using CalamityMod;
using CalamityMod.Particles;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.LeafProj
{
    internal sealed class BFRecoveryFlash : ModProjectile, ILocalizedModType
    {
        public new string LocalizationCategory => "Projectiles.BlossomFlux";
        public override string Texture => "Terraria/Images/Projectile_0";

        private ref float HealAmount => ref Projectile.ai[0];
        private ref float PureFlash => ref Projectile.ai[1];
        private ref float Time => ref Projectile.localAI[0];
        private bool IsPure => PureFlash == 1f;

        public override void SetDefaults()
        {
            Projectile.width = 28;
            Projectile.height = 28;
            Projectile.friendly = true;
            Projectile.hostile = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = 54;
            Projectile.hide = true;
        }

        public override bool? CanDamage() => false;

        public override void OnSpawn(Terraria.DataStructures.IEntitySource source)
        {
            if (IsPure)
            {
                Projectile.scale = 1.4f;
                Projectile.timeLeft = 72;
            }

            SoundEngine.PlaySound(SoundID.Item4 with { Volume = IsPure ? 0.3f : 0.18f, Pitch = 0.58f }, Projectile.Center);
        }

        public override void AI()
        {
            Time++;
            Projectile.velocity *= 0.94f;
            Projectile.velocity.Y -= 0.035f;
            Projectile.rotation += 0.08f;

            Lighting.AddLight(Projectile.Center, new Vector3(0.12f, 0.42f, 0.12f) * Projectile.Opacity);

            if (Main.dedServ || !Main.rand.NextBool(IsPure ? 1 : 2))
                return;

            Dust dust = Dust.NewDustPerfect(
                Projectile.Center + Main.rand.NextVector2Circular(13f, 13f) * Projectile.scale,
                Main.rand.NextBool(3) ? DustID.GemEmerald : DustID.TerraBlade,
                -Vector2.UnitY.RotatedByRandom(0.48f) * Main.rand.NextFloat(0.35f, 1.2f),
                100,
                Color.Lerp(new Color(95, 255, 122), Color.White, Main.rand.NextFloat(0.14f, 0.46f)),
                Main.rand.NextFloat(0.72f, 1.18f) * Projectile.scale);
            dust.noGravity = true;
        }

        public override void OnKill(int timeLeft)
        {
            Player target = FindHealTarget();
            int heal = System.Math.Max(1, (int)HealAmount);
            if (target != null && target.active && !target.dead && target.statLife < target.statLifeMax2)
            {
                heal = System.Math.Min(heal, target.statLifeMax2 - target.statLife);
                if (heal > 0)
                {
                    target.statLife += heal;
                    target.HealEffect(heal, true);
                    if (!IsPure)
                        target.GetModPlayer<BFRecoveryEcologyPlayer>().AddRecoveryLeaf(5 * 60);
                }
            }

            SpawnBurst(target?.Center ?? Projectile.Center);
            SoundEngine.PlaySound(SoundID.Item29 with { Volume = IsPure ? 0.32f : 0.24f, Pitch = 0.42f }, Projectile.Center);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            Texture2D bloom = TextureAssets.Extra[98].Value;
            Texture2D spark = ModContent.Request<Texture2D>("CalamityMod/Particles/GlowSpark").Value;
            Vector2 drawPosition = Projectile.Center - Main.screenPosition;
            float fadeIn = Utils.GetLerpValue(0f, 12f, Time, true);
            float fadeOut = Utils.GetLerpValue(0f, 18f, Projectile.timeLeft, true);
            float opacity = fadeIn * fadeOut;
            float pulse = 0.86f + 0.14f * (float)System.Math.Sin(Main.GlobalTimeWrappedHourly * 9f + Projectile.identity);
            Color green = new Color(92, 255, 118, 0) * opacity;
            Color white = Color.White with { A = 0 } * opacity;

            Main.spriteBatch.SetBlendState(BlendState.Additive);
            Main.EntitySpriteDraw(bloom, drawPosition, null, green * 0.72f, 0f, bloom.Size() * 0.5f, 0.18f * Projectile.scale * pulse, SpriteEffects.None, 0);

            for (int i = 0; i < 4; i++)
            {
                float rotation = Projectile.rotation + MathHelper.PiOver2 * i;
                Main.EntitySpriteDraw(
                    spark,
                    drawPosition,
                    null,
                    Color.Lerp(green, white, 0.35f) * 0.7f,
                    rotation,
                    spark.Size() * 0.5f,
                    new Vector2(0.08f, 0.24f) * Projectile.scale * pulse,
                    SpriteEffects.None,
                    0);
            }

            Main.spriteBatch.SetBlendState(BlendState.AlphaBlend);
            return false;
        }

        private Player FindHealTarget()
        {
            if (Main.netMode == NetmodeID.SinglePlayer)
                return Main.player[Projectile.owner];

            Player bestPlayer = null;
            float bestScore = float.MinValue;
            for (int i = 0; i < Main.maxPlayers; i++)
            {
                Player player = Main.player[i];
                if (!player.active || player.dead)
                    continue;

                float distance = Vector2.Distance(player.Center, Projectile.Center);
                if (distance > 1200f)
                    continue;

                float missingLifeRatio = player.statLifeMax2 <= 0 ? 0f : 1f - player.statLife / (float)player.statLifeMax2;
                float leafUrgency = 1f - MathHelper.Clamp(player.GetModPlayer<BFRecoveryEcologyPlayer>().LeafTimeLeft / (10f * 60f), 0f, 1f);
                float score = missingLifeRatio * 5f + leafUrgency * (IsPure ? 0.5f : 1.4f) - distance / 1800f;
                if (score <= bestScore)
                    continue;

                bestScore = score;
                bestPlayer = player;
            }

            return bestPlayer ?? Main.player[Projectile.owner];
        }

        private void SpawnBurst(Vector2 center)
        {
            if (Main.dedServ)
                return;

            Color green = new(92, 255, 118);
            GeneralParticleHandler.SpawnParticle(new StrongBloom(center, Vector2.Zero, Color.Lerp(green, Color.White, 0.28f), IsPure ? 0.68f : 0.42f, IsPure ? 15 : 10));

            for (int i = 0; i < (IsPure ? 16 : 9); i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    center + Main.rand.NextVector2Circular(8f, 8f),
                    Main.rand.NextBool(3) ? DustID.GemEmerald : DustID.TerraBlade,
                    Main.rand.NextVector2CircularEdge(1f, 1f) * Main.rand.NextFloat(1.6f, IsPure ? 5.2f : 3.7f),
                    100,
                    Color.Lerp(green, Color.White, Main.rand.NextFloat(0.12f, 0.55f)),
                    Main.rand.NextFloat(0.78f, 1.28f) * Projectile.scale);
                dust.noGravity = true;
            }
        }
    }
}
