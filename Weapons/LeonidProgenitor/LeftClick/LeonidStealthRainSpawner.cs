using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Core;
using CalamityLegendsComeBack.Weapons.LeonidProgenitor.Effects;
using CalamityMod;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.LeonidProgenitor
{
    // The cursor selects a target; the reticle and the whole rain follow that same enemy.
    public class LeonidStealthRainSpawner : ModProjectile, ILocalizedModType
    {
        public const int TotalComets = 7;
        public const int SpawnInterval = 5;
        public const float RainHeight = 800f; // 50 tiles
        public const float TargetSearchRange = 900f;

        public override string Texture => "Terraria/Images/Projectile_533";
        public new string LocalizationCategory => "Projectiles.LeonidProgenitor";

        private int ThrowDirection => Projectile.ai[0] >= 0f ? 1 : -1;
        private int MaxComets => TotalComets;
        private Player Owner => Main.player[Projectile.owner];

        private int SpawnedComets
        {
            get => (int)Projectile.ai[2];
            set => Projectile.ai[2] = value;
        }

        private int SpawnTimer
        {
            get => (int)Projectile.localAI[1];
            set => Projectile.localAI[1] = value;
        }

        public override void SetDefaults()
        {
            Projectile.width = 8;
            Projectile.height = 8;
            Projectile.friendly = false;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.penetrate = -1;
            Projectile.timeLeft = (TotalComets + 2) * SpawnInterval + 160;
            Projectile.alpha = 255;
        }

        public override bool? CanDamage() => false;
        public override bool ShouldUpdatePosition() => false;

        public override void OnSpawn(IEntitySource source)
        {
            if (Main.myPlayer == Projectile.owner)
            {
                Projectile.ai[1] = FindTarget(Projectile.Center, TargetSearchRange, Projectile);
                Projectile.netUpdate = true;
            }
        }

        // Store NPC index + 1 so zero always means no target, including on newly received projectiles.
        public static NPC GetTarget(float encodedTarget, Projectile seeker)
        {
            int index = (int)encodedTarget - 1;
            return index >= 0 && index < Main.maxNPCs && Main.npc[index].CanBeChasedBy(seeker)
                ? Main.npc[index] : null;
        }

        public static int FindTarget(Vector2 center, float range, Projectile seeker)
        {
            int result = 0;
            float nearest = range * range;
            foreach (NPC npc in Main.ActiveNPCs)
            {
                float distance = Vector2.DistanceSquared(center, npc.Center);
                if (npc.CanBeChasedBy(seeker) && distance < nearest)
                {
                    nearest = distance;
                    result = npc.whoAmI + 1;
                }
            }
            return result;
        }

        public override void AI()
        {
            Projectile.velocity = Vector2.Zero;
            NPC target = GetTarget(Projectile.ai[1], Projectile);
            if (target == null && Main.myPlayer == Projectile.owner)
            {
                int nextTarget = FindTarget(Projectile.Center, TargetSearchRange, Projectile);
                if (Projectile.ai[1] != nextTarget)
                {
                    Projectile.ai[1] = nextTarget;
                    Projectile.netUpdate = true;
                }
                target = GetTarget(Projectile.ai[1], Projectile);
            }
            if (target != null)
                Projectile.Center = target.Center;

            if (Main.myPlayer != Projectile.owner)
                return;

            SpawnTimer++;

            // 落点标记：潜伏流星雨一召出来就在目标点张开一圈星辉，
            // 之后每落一颗流星补一粒，读得出"这块地已经被点名了"。
            if (SpawnedComets == 0 && SpawnTimer == 1)
            {
                LeonidStarlight.Ring(Projectile.Center, 10, 64f, LeonidVisualUtils.GetReadyGold(),
                    LeonidStarlightShape.Mote, speed: 2.2f, scale: 0.85f, hoverTime: 44, lifetime: 190, lanceSpeed: 18f);
            }

            // 第一颗立即掉落，之后每 5 帧一颗
            if ((SpawnedComets == 0 || SpawnTimer >= SpawnInterval) && SpawnedComets < MaxComets)
            {
                SpawnTimer = 0;
                SpawnedComets++;

                if (Main.myPlayer == Projectile.owner)
                {
                    Vector2 spawnPos = Projectile.Center + new Vector2(
                        Main.rand.Next(-250, -100) * ThrowDirection,
                        -RainHeight - Main.rand.Next(0, 64));
                    Vector2 targetPos = Projectile.Center + Main.rand.NextVector2Circular(80f, 80f);
                    Vector2 velocity = (targetPos - spawnPos).SafeNormalize(Vector2.UnitY) * (Owner.GetModPlayer<Core.LeonidConstellationPlayer>().IsUnlocked(Core.LeonidStar.Algieba) ? 19.2f : 16f);

                    int p = Projectile.NewProjectile(
                        Projectile.GetSource_FromThis(),
                        spawnPos,
                        velocity,
                        ModContent.ProjectileType<LeonidCometSmall>(),
                        Projectile.damage,
                        Projectile.knockBack,
                        Projectile.owner,
                        Projectile.ai[1],
                        Projectile.identity + 1,
                        LeonidCometSmall.FromStealthFlag);

                    if (p.WithinBounds(Main.maxProjectiles))
                    {
                        Main.projectile[p].Calamity().stealthStrike = true;
                        Main.projectile[p].localAI[1] = 0f; // no launch delay
                    }
                    Projectile.netUpdate = true;
                }
            }

            // Keep the mark visible and authoritative until the last rain comet has finished.
            if (SpawnedComets >= MaxComets)
            {
                foreach (Projectile comet in Main.ActiveProjectiles)
                    if (comet.owner == Projectile.owner && comet.ModProjectile is LeonidCometSmall small
                        && small.FromStealthRain && comet.ai[1] == Projectile.identity + 1)
                        return;
                Projectile.Kill();
            }
        }

        public override bool PreDraw(ref Color lightColor)
        {
            // 落点准星：随着流星一颗颗落下逐渐收紧，最后一颗落地时刚好收到最小。
            float progress = MaxComets <= 0 ? 0f : SpawnedComets / (float)MaxComets;
            float fade = Utils.GetLerpValue(0f, 8f, Projectile.timeLeft, true);

            LeonidStarlight.DrawReticle(
                Projectile.Center,
                Color.Lerp(LeonidVisualUtils.StratusBlue, LeonidVisualUtils.StarGold, progress),
                0.5f * fade,
                MathHelper.Lerp(0.5f, 0.2f, progress),
                -Main.GlobalTimeWrappedHourly * 1.1f);

            LeonidStarlight.DrawOrbitingStars(
                Projectile.Center,
                LeonidVisualUtils.GetReadyGold(),
                0.42f * fade,
                0.045f,
                MathHelper.Lerp(74f, 30f, progress),
                4,
                2.2f);

            return false;
        }
    }
}
