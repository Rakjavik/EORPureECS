using Unity.Entities;
using Unity.Jobs;

namespace rak.ecs.Systems
{
    public struct Tree : IComponentData
    {
        public float ProducesEvery;
        public float SinceLastProduction;
    }

    public class TreeSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            TreeJob job = new TreeJob
            {
                delta = UnityEngine.Time.deltaTime
            };
            return job.Schedule(this, inputDeps);
        }

        struct TreeJob : IJobForEach<Tree, Spawner>
        {
            public float delta;

            public void Execute(ref Tree tree, ref Spawner spawner)
            {
                tree.SinceLastProduction += delta;
                if(tree.SinceLastProduction >= tree.ProducesEvery)
                {
                    spawner.ToSpawn += 1;
                    tree.SinceLastProduction = 0;
                }
            }
        }
    }
}

