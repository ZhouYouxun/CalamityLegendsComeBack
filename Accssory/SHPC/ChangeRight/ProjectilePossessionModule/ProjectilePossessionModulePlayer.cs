using Terraria;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Accssory.SHPC.ChangeRight.ProjectilePossessionModule
{
    public sealed class ProjectilePossessionModulePlayer : ModPlayer
    {
        public const int MaxAbsorbedProjectiles = 15;
        private const int DetachedUiFadeTime = 60;

        public bool ProjectilePossessionModuleEquipped;
        public int AbsorbedProjectileCount;
        public int PossessionUiFadeTimer;
        public int PossessionBarPulseTimer;

        public override void ResetEffects()
        {
            ProjectilePossessionModuleEquipped = false;
        }

        public override void PostUpdate()
        {
            AbsorbedProjectileCount = SHPCProjectilePossessionGlobalProjectile.CountPossessedProjectiles(Player.whoAmI);

            bool holdingPossession = HasActivePossessionHoldout();
            if (holdingPossession || AbsorbedProjectileCount > 0)
                PossessionUiFadeTimer = DetachedUiFadeTime;
            else if (PossessionUiFadeTimer > 0)
                PossessionUiFadeTimer--;

            if (!ProjectilePossessionModuleEquipped && AbsorbedProjectileCount > 0 && Main.myPlayer == Player.whoAmI)
                SHPCProjectilePossessionGlobalProjectile.ReleaseAllForOwner(Player, Player.DirectionTo(Main.MouseWorld), 18f, 1);

            if (Main.myPlayer == Player.whoAmI && (holdingPossession || AbsorbedProjectileCount > 0 || PossessionUiFadeTimer > 0))
                EnsurePossessionUI();
        }

        public void RefreshAbsorbedCount()
        {
            AbsorbedProjectileCount = SHPCProjectilePossessionGlobalProjectile.CountPossessedProjectiles(Player.whoAmI);
        }

        public void TriggerPossessionBarPulse(int frames)
        {
            if (frames > PossessionBarPulseTimer)
                PossessionBarPulseTimer = frames;
        }

        public bool HasActivePossessionHoldout()
        {
            return Player.ownedProjectileCounts[ModContent.ProjectileType<ProjectilePossessionHoldout>()] > 0;
        }

        public float GetPossessionProgress()
        {
            return Utils.Clamp(AbsorbedProjectileCount / (float)MaxAbsorbedProjectiles, 0f, 1f);
        }

        public float GetPossessionUiOpacity()
        {
            return Utils.Clamp(PossessionUiFadeTimer / (float)DetachedUiFadeTime, 0f, 1f);
        }

        public int GetDisplayedPossessionLevel()
        {
            if (AbsorbedProjectileCount <= 0)
                return 0;

            return Utils.Clamp((int)System.MathF.Ceiling(GetPossessionProgress() * 5f), 1, 5);
        }

        private void EnsurePossessionUI()
        {
            int uiType = ModContent.ProjectileType<ProjectilePossessionUI>();
            if (Player.ownedProjectileCounts[uiType] > 0)
                return;

            Projectile.NewProjectile(
                Player.GetSource_FromThis(),
                Player.Top,
                Microsoft.Xna.Framework.Vector2.Zero,
                uiType,
                0,
                0f,
                Player.whoAmI);
        }
    }
}
