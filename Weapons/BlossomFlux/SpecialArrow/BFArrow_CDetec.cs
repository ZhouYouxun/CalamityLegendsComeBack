using CalamityLegendsComeBack.Weapons.BlossomFlux.RightUI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Terraria;
using Terraria.Audio;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.SpecialArrow
{
    // C 战术右键箭：负责扫描、标记，并在命中后扎附在敌人身上。
    internal class BFArrow_CDetec : ModProjectile
    {
        private const float ScanGrowthPerFrame = 3.2f;
        private const float MaxScanRadius = 220f;

        private ref float State => ref Projectile.ai[0];
        private ref float AttachedNpcIndex => ref Projectile.ai[1];
        private ref float ScanRadius => ref Projectile.localAI[0];
        private ref float LifeTimer => ref Projectile.localAI[1];

        public override void SetStaticDefaults()
        {
            ProjectileID.Sets.TrailCacheLength[Projectile.type] = 10;
            ProjectileID.Sets.TrailingMode[Projectile.type] = 2;
        }

        public override void SetDefaults()
        {
            BFArrowCommon.SetBaseArrowDefaults(Projectile, width: 14, height: 34, timeLeft: 270, penetrate: -1, extraUpdates: 1, tileCollide: true);
            Projectile.localNPCHitCooldown = -1;
        }

        public override bool? CanDamage() => State == 0f ? null : false;

        public override bool? CanHitNPC(NPC target) => State == 0f ? null : false;

        public override void AI()
        {
            LifeTimer++;
            Lighting.AddLight(Projectile.Center, new Color(255, 92, 92).ToVector3() * 0.46f);

            if (State == 0f)
            {
                BFArrowCommon.FaceForward(Projectile);
                ScanRadius = MathHelper.Clamp(ScanRadius <= 0f ? 24f : ScanRadius + ScanGrowthPerFrame, 24f, MaxScanRadius);

                if (Main.rand.NextBool(2))
                {
                    Dust dust = Dust.NewDustPerfect(
                        Projectile.Center,
                        DustID.RedTorch,
                        -Projectile.velocity * 0.08f + Main.rand.NextVector2Circular(0.7f, 0.7f),
                        100,
                        new Color(255, 92, 92),
                        Main.rand.NextFloat(0.85f, 1.1f));
                    dust.noGravity = true;
                }

                if ((int)LifeTimer % 7 == 0)
                {
                    if (Projectile.owner == Main.myPlayer)
                        SoundEngine.PlaySound(SoundID.Item9 with { Volume = 0.18f, Pitch = 0.45f }, Projectile.Center);

                    PerformReconScan();
                }

                return;
            }

            Projectile.friendly = false;
            Projectile.tileCollide = false;

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

            Projectile.Center = attachedNpc.Center - Projectile.velocity * 0.75f;
            Projectile.gfxOffY = attachedNpc.gfxOffY;
            ScanRadius = MathHelper.Lerp(ScanRadius, 0f, 0.18f);
            if (ScanRadius < 2f)
                ScanRadius = 0f;

            attachedNpc.GetGlobalNPC<BFArrow_CDetecNPC>().ApplyPriorityMark(Projectile.owner, 180);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            if (State != 0f)
                return;

            BFArrow_CDetecNPC markData = target.GetGlobalNPC<BFArrow_CDetecNPC>();
            markData.ApplyPriorityMark(Projectile.owner, 180);
            Main.player[Projectile.owner].GetModPlayer<BFRightUIPlayer>().SetReconPriorityTarget(target.whoAmI, 180);

            State = 1f;
            AttachedNpcIndex = target.whoAmI;
            Projectile.velocity = target.Center - Projectile.Center;
            Projectile.damage = 0;
            Projectile.netUpdate = true;
            Projectile.timeLeft = Utils.Clamp(Projectile.timeLeft, 180, 270);

            SoundEngine.PlaySound(SoundID.Item122 with { Volume = 0.45f, Pitch = 0.15f }, target.Center);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            SoundEngine.PlaySound(SoundID.Dig with { Volume = 0.25f, Pitch = 0.35f }, Projectile.Center);

            if (Projectile.velocity.X != oldVelocity.X)
                Projectile.velocity.X = -oldVelocity.X * 0.96f;

            if (Projectile.velocity.Y != oldVelocity.Y)
                Projectile.velocity.Y = -oldVelocity.Y * 0.96f;

            Projectile.netUpdate = true;
            return false;
        }

        public override void OnKill(int timeLeft)
        {
            for (int i = 0; i < 12; i++)
            {
                Dust dust = Dust.NewDustPerfect(
                    Projectile.Center,
                    DustID.RedTorch,
                    Main.rand.NextVector2CircularEdge(3f, 3f) * Main.rand.NextFloat(1.1f, 4.5f),
                    100,
                    new Color(255, 96, 96),
                    Main.rand.NextFloat(0.85f, 1.2f));
                dust.noGravity = true;
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            if (ScanRadius > 3f)
            {
                Texture2D circleTexture = ModContent.Request<Texture2D>("CalamityLegendsComeBack/Texture/KsTexture/circle_03").Value;
                float scale = ScanRadius * 2f / circleTexture.Width;
                Color ringColor = new Color(255, 90, 90, 0) * 0.22f;

                Main.EntitySpriteDraw(
                    circleTexture,
                    Projectile.Center - Main.screenPosition,
                    null,
                    ringColor,
                    Main.GlobalTimeWrappedHourly * 0.5f,
                    circleTexture.Size() * 0.5f,
                    scale,
                    SpriteEffects.None,
                    0);

                Main.EntitySpriteDraw(
                    circleTexture,
                    Projectile.Center - Main.screenPosition,
                    null,
                    ringColor * 0.65f,
                    -Main.GlobalTimeWrappedHourly * 0.8f,
                    circleTexture.Size() * 0.5f,
                    scale * 0.82f,
                    SpriteEffects.None,
                    0);
            }

            BFArrowCommon.DrawAfterimagesThenProjectile(Projectile, lightColor);
            return false;
        }

        private void PerformReconScan()
        {
            bool foundNewTarget = false;

            for (int i = 0; i < Main.maxNPCs; i++)
            {
                NPC npc = Main.npc[i];
                if (!npc.active || npc.friendly || npc.dontTakeDamage || !npc.CanBeChasedBy(this))
                    continue;

                if (Vector2.Distance(Projectile.Center, npc.Center) > ScanRadius)
                    continue;

                BFArrow_CDetecNPC markData = npc.GetGlobalNPC<BFArrow_CDetecNPC>();
                if (markData.ApplyMark(Projectile.owner, 180))
                    foundNewTarget = true;
            }

            if (foundNewTarget && Projectile.owner == Main.myPlayer)
                SoundEngine.PlaySound(SoundID.Item4 with { Volume = 0.28f, Pitch = 0.35f }, Projectile.Center);
        }
    }
}
