using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace rak.ecs.Systems
{
    public struct TargetInvalidate : IComponentData
    {
        public byte Active;
    }
    public struct DestroyThis : IComponentData
    {
        public byte dummy;
    }


    //[UpdateAfter(typeof(ConsumptionSystem))]
    public class TargetValidSystem : JobComponentSystem
    {
        private EntityQuery query;
        private float updateEvery = .5f;
        private float sinceLastUpdate = 0;

        protected override void OnCreate()
        {
            query = GetEntityQuery(new ComponentType[] { typeof(Target) });
            Enabled = false;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            sinceLastUpdate += Time.DeltaTime;
            if (sinceLastUpdate < updateEvery)
                return inputDeps;
            sinceLastUpdate = 0;
            TargetValidJob job = new TargetValidJob
            {
                allEntities = EntityManager.GetAllEntities(Allocator.TempJob),
            };
            JobHandle handle = job.Schedule(this, inputDeps);
            return handle;
        }
        [BurstCompile]
        struct TargetValidJob : IJobForEachWithEntity<Target>
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> allEntities;

            public void Execute(Entity entity, int index, ref Target target)
            {
                if (target.Entity == Entity.Null)
                    return;
                byte found = 0;
                for (int count = 0; count < allEntities.Length; count++)
                {
                    if (allEntities[count].Equals(target.Entity))
                    {
                        found = 1;
                        break;
                    }
                }
                if(found == 0)
                {
                    target.Entity = Entity.Null;
                }
            }
        }
    }
}
