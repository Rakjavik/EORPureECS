using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace rak.ecs.Systems
{
    public class TargetValidSystem : JobComponentSystem
    {
        private EntityQuery observableQuery;
        private EntityQuery targetQuery;
        private float UpdateEvery;
        private float SinceLastUpdate;

        protected override void OnCreate()
        {
            Enabled = false;
            UpdateEvery = 1;
            SinceLastUpdate = 0;
            observableQuery = GetEntityQuery(new ComponentType[] { typeof(Observable) });
            targetQuery = GetEntityQuery(new ComponentType[] { typeof(Target) });
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            NativeArray<Entity> observables = observableQuery.ToEntityArray(Allocator.TempJob);
            NativeArray<Target> targets = targetQuery.ToComponentDataArray<Target>(Allocator.TempJob);

            for(int count = 0; count < targets.Length; count++)
            {
                if (!observables.Contains(targets[count].Entity))
                {
                    targets[count] = new Target
                    {
                        Entity = Entity.Null,
                        MemoryIndex = -1,
                        Position = float3.zero
                    };
                }
            }
            targets.Dispose();
            observables.Dispose();
            return inputDeps;
        }
        /*protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SinceLastUpdate += UnityEngine.Time.deltaTime;
            if (SinceLastUpdate >= UpdateEvery)
            {
                SinceLastUpdate = 0;
                TargetValidJob job = new TargetValidJob
                {
                    entities = query.ToEntityArray(Allocator.TempJob)
                };
                return job.Schedule(this, inputDeps);
            }
            else
            {
                return inputDeps;
            }
        }

        [BurstCompile]
        struct TargetValidJob : IJobForEachWithEntity<Target,CreatureAI>
        {
            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> entities;

            public void Execute(Entity entity, int index, ref Target target, ref CreatureAI cai)
            {
                if (target.Entity.Equals(Entity.Null)) return;
                if (!entities.Contains(target.Entity))
                {
                    //UnityEngine.Debug.Log("Target invalid - " + target.Entity);
                    target.Entity = Entity.Null;
                    target.MemoryIndex = -1;
                    target.Position = float3.zero;
                    cai.CurrentAction = CreatureActionType.Cancelled;
                }
            }
        }*/
    }
}
