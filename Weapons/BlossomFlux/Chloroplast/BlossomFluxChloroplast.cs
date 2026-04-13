using Microsoft.Xna.Framework;
using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.BlossomFlux.Chloroplast
{
    internal enum BlossomFluxChloroplastPresetType
    {
        Chlo_ABreak = 0,
        Chlo_BRecov = 1,
        Chlo_CDetec = 2,
        Chlo_DBomb = 3,
        Chlo_EPlague = 4
    }

    // 叶绿体战术的统一接口，后面五套预设都从这里接入。
    internal abstract class BlossomFluxChloroplastPreset
    {
        public abstract void OnSpawn(Projectile projectile, IEntitySource source);
        public abstract void AI(Projectile projectile);
        public abstract void OnHitNPC(Projectile projectile, NPC target, NPC.HitInfo hit, int damageDone);
        public abstract void OnKill(Projectile projectile, int timeLeft);
        public abstract bool OnTileCollide(Projectile projectile, Vector2 oldVelocity);
        public abstract bool? CanDamage(Projectile projectile);
        public abstract bool PreDraw(Projectile projectile, ref Color lightColor);
    }

    // 左键 AAAAB 节奏里的 B，本体只负责基础动画和把逻辑分发给当前战术。
    internal class BlossomFluxChloroplast : ModProjectile, ILocalizedModType
    {
        private const int PresetCount = 5;
        private const int DefaultPreset = (int)BlossomFluxChloroplastPresetType.Chlo_ABreak;
        private const int FrameSpeed = 5;

        private static readonly BlossomFluxChloroplastPreset[] PresetBehaviors =
        {
            Chlo_ABreak.Instance,
            Chlo_BRecov.Instance,
            Chlo_CDetec.Instance,
            Chlo_DBomb.Instance,
            Chlo_EPlague.Instance
        };

        public override string Texture => "CalamityLegendsComeBack/Weapons/BlossomFlux/Chloroplast/BlossomFluxChloroplast";
        public new string LocalizationCategory => "Projectiles.BlossomFlux";

        public ref float Preset => ref Projectile.ai[0];
        public ref float Timer => ref Projectile.ai[1];

        private BlossomFluxChloroplastPreset CurrentPresetBehavior
        {
            get
            {
                int presetIndex = NormalizePresetIndex();
                if ((int)Preset != presetIndex)
                    Preset = presetIndex;

                return PresetBehaviors[presetIndex];
            }
        }

        public override void SetStaticDefaults()
        {
            Main.projFrames[Type] = 5;
        }

        public override void SetDefaults()
        {
            Projectile.width = 32;
            Projectile.height = 32;
            Projectile.friendly = true;
            Projectile.DamageType = DamageClass.Ranged;
            Projectile.penetrate = 1;
            Projectile.timeLeft = 210;
            Projectile.tileCollide = false;
            Projectile.ignoreWater = true;
            Projectile.aiStyle = 0;
            Projectile.usesLocalNPCImmunity = true;
            Projectile.localNPCHitCooldown = 12;
        }

        public override void OnSpawn(IEntitySource source)
        {
            Preset = NormalizePresetIndex();
            CurrentPresetBehavior.OnSpawn(Projectile, source);
        }

        public override void AI()
        {
            Timer++;
            AnimateFrames();
            CurrentPresetBehavior.AI(Projectile);
        }

        public override void OnHitNPC(NPC target, NPC.HitInfo hit, int damageDone)
        {
            CurrentPresetBehavior.OnHitNPC(Projectile, target, hit, damageDone);
        }

        public override void OnKill(int timeLeft)
        {
            CurrentPresetBehavior.OnKill(Projectile, timeLeft);
        }

        public override bool OnTileCollide(Vector2 oldVelocity)
        {
            return CurrentPresetBehavior.OnTileCollide(Projectile, oldVelocity);
        }

        public override bool? CanDamage()
        {
            return CurrentPresetBehavior.CanDamage(Projectile);
        }

        public override bool PreDraw(ref Color lightColor)
        {
            return CurrentPresetBehavior.PreDraw(Projectile, ref lightColor);
        }

        private void AnimateFrames()
        {
            Projectile.frameCounter++;
            if (Projectile.frameCounter < FrameSpeed)
                return;

            Projectile.frameCounter = 0;
            Projectile.frame = (Projectile.frame + 1) % Main.projFrames[Type];
        }

        private int NormalizePresetIndex()
        {
            int presetIndex = (int)Preset;
            if (presetIndex < 0 || presetIndex >= PresetCount)
                presetIndex = DefaultPreset;

            return presetIndex;
        }
    }
}
