using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace rak.ecs.Systems
{
    public enum Needs { Hunger }

    public struct CreatureNeeds : IComponentData
    {
        public float Hunger;
        public Needs MostUrgent;
    }

    public class NeedsSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            NeedsJob job = new NeedsJob
            {
                delta = UnityEngine.Time.deltaTime
            };
            return job.Schedule(this, inputDeps);
        }

        [BurstCompile]
        struct NeedsJob : IJobForEach<CreatureNeeds>
        {
            public float delta;

            public void Execute(ref CreatureNeeds cn)
            {
                cn.Hunger += delta;
                cn.MostUrgent = Needs.Hunger;
            }
        }
    }
}
