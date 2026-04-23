using Terraria;
using Terraria.DataStructures;
using Terraria.ModLoader;

namespace CalamityLegendsComeBack.Weapons.A_Dev.Miao
{
    internal sealed class MiaoTraceProjectile : GlobalProjectile
    {
        public override bool InstancePerEntity => true;

        public bool IsTrackedRoot;
        public int TraceId = -1;
        public int Generation = -1;
        public int ParentProjectileType = -1;
        public string RootProjectileName = "";
        public int RootProjectileType = -1;

        public void MarkAsRoot(Projectile projectile)
        {
            IsTrackedRoot = true;
            TraceId = MiaoTraceSystem.AllocateTraceId();
            Generation = 0;
            ParentProjectileType = -1;
            RootProjectileType = projectile.type;
            RootProjectileName = MiaoTraceSystem.GetProjectileName(projectile.type);

            MiaoTraceSystem.RecordSpawn(TraceId, projectile.type);
            MiaoTraceSystem.Log($"[Shot {TraceId}] ROOT {RootProjectileName}");
        }

        public override void OnSpawn(Projectile projectile, IEntitySource source)
        {
            if (projectile.owner < 0 || projectile.owner >= Main.maxPlayers)
                return;

            if (source is not EntitySource_Parent parentSource || parentSource.Entity is not Projectile parent)
                return;

            MiaoTraceProjectile parentTracker = parent.GetGlobalProjectile<MiaoTraceProjectile>();
            if (parentTracker.TraceId < 0)
                return;

            TraceId = parentTracker.TraceId;
            Generation = parentTracker.Generation + 1;
            ParentProjectileType = parent.type;
            RootProjectileType = parentTracker.RootProjectileType;
            RootProjectileName = parentTracker.RootProjectileName;

            MiaoTraceSystem.RecordSpawn(TraceId, projectile.type);
            MiaoTraceSystem.Log(
                $"[Shot {TraceId}] GEN {Generation} {MiaoTraceSystem.GetProjectileName(parent.type)} -> {MiaoTraceSystem.GetProjectileName(projectile.type)}");
        }
    }
}
