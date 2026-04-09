using Microsoft.Xna.Framework;
using Terraria;
using Terraria.Audio;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // E 战术右键箭：扎附在目标或地形后，持续朝周围排出毒云。
    internal class BFArrow_EPlague : ModProjectile
    {
        private int gasTimer;
        private Vector2 stickOffset;

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 8;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 300, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => State == 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State == 0f ? null : false;

        public override void AI()
        {
            Lighting.AddLight(Projectile.Center, new Color(172, 228, 92).ToVector3() * 0.42f);

            if (State == 0f)
            {
                BFArrowCommon.FaceForward(Projectile);

                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.GreenTorch,
                        -Projectile.velocity * 0.09f + Main.rand.NextVector2Circular(0.8f, 0.8f),
                        100,
                        new Color(172, 228, 92),
                        Main.rand.NextFloat(0.9f, 1.2f));
                    dust.noGravity = true;
                }

                return;
            }

            Projectile.friendly = false;
            Projectile.tileCollide = false;

            if (State == 1f)
            {
                if (!BFArrowCommon.InBounds(AttachedNpcIndex, Main.maxNPCs))
                {
                    Projectile.Kill();
                    return;
                }

                NPC attachedNpc = Main.npc[(int)AttachedNpcIndex];
                if (!attachedNpc.active || attachedNpc.dontTakeDamage)
                {
                    Projectile.Kill();
                    return;
                }

                Projectile.Center = attachedNpc.Center + stickOffset;
                Projectile.gfxOffY = attachedNpc.gfxOffY;
            }
            else
            {
                Projectile.velocity = Vector2.Zero;
            }

            EmitPlagueGas();
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            stickOffset = Projectile.Center - target.Center;
            State = 1f;
            AttachedNpcIndex = target.whoAmI;
            Projectile.velocity = Vector2.Zero;
            Projectile.damage = 0;
            Projectile.timeLeft = 180;
            Projectile.netUpdate = true;

            target.AddBuff(BuffID.Poisoned, 180);
            target.AddBuff(BuffID.Venom, 120);
            SoundEngine.PlaySound(SoundID.NPCDeath13 with { Volume = 0.38f, Pitch = 0.1f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            State = 2f;
            Projectile.velocity = Vector2.Zero;
            Projectile.timeLeft = 180;
            Projectile.netUpdate = true;

            SoundEngine.PlaySound(SoundID.Item10 with { Volume = 0.22f, Pitch = 0.1f }, Projectile.Center);
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 14; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.GreenTorch,
                    Main.rand.NextVector2CircularEdge(2.6f, 2.6f) * Main.rand.NextFloat(1.2f, 4.2f),
                    100,
                    new Color(172, 228, 92),
                    Main.rand.NextFloat(0.9f, 1.25f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (State == 0f)
                BFArrowCommon.DrawAfterimagesThenProjectile(Projectile, lightColor);
            else
                BFArrowCommon.DrawProjectile(Projectile, Terraria.GameContent.TextureAssets.Projectile[Projectile.type].Value, Projectile.GetAlpha(lightColor), Projectile.rotation, Projectile.scale);

            return false;
        }

        private void EmitPlagueGas()
        {
            gasTimer++;

            if (Main.rand.NextBool(2))
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center + Main.rand.NextVector2Circular(8f, 8f),
                    DustID.GreenTorch,
                    Main.rand.NextVector2Circular(0.6f, 0.6f),
                    100,
                    new Color(172, 228, 92),
                    Main.rand.NextFloat(0.8f, 1.15f));
                dust.noGravity = true;
            }

            if (gasTimer % 14 != 0 || Projectile.owner != Main.myPlayer)
                return;

            for (int i = 0; i < 2; i++)
            {
                Projectile.NewProjectile(
                    Projectile.GetSource_FromThis(),
                    Projectile.Center + Main.rand.NextVector2Circular(10f, 10f),
                    Main.rand.NextVector2Circular(1.4f, 1.4f) + new Vector2(0f, -0.5f),
                    ModContent.ProjectileType<BFArrow_EPlagueGas>(),
                    (int)(Projectile.damage * 0.35f),
                    0f,
                    Projectile.owner,
                    Main.rand.Next(3),
                    Main.rand.NextFloat(0.9f, 1.2f));
            }
        }
    }
}
