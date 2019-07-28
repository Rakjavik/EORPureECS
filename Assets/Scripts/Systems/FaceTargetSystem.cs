using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace rak.ecs.Systems
{
    struct Target : IComponentData
    {
        public float3 Position;
        public Entity Entity;
        public int MemoryIndex;
    }

    struct TurnSpeed : IComponentData
    {
        public float Value;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class FaceTargetSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            FaceTargetJob job = new FaceTargetJob
            {
                delta = Time.deltaTime
            };
            return job.Schedule(this, inputDeps);
        }

        [BurstCompile]
        struct FaceTargetJob : IJobForEach<Target, Rotation, TurnSpeed,Translation,LocalToWorld>
        {
            public float delta;

            public void Execute(ref Target ft, ref Rotation rot, ref TurnSpeed ts,
                ref Translation trans, ref LocalToWorld ltw)
            {
                float3 direction = math.normalize(ft.Position-trans.Value);
                quaternion lookRotation = quaternion.LookRotation(direction, math.up());
                
            }
        }
    }
}
