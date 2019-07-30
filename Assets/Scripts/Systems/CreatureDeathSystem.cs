using Unity.Entities;
using Unity.Jobs;

namespace rak.ecs.Systems
{
    public class CreatureDeathSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            commandBufferSystem =
                World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            CreatureDeathJob job = new CreatureDeathJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent()
            };
            JobHandle handle = job.Schedule(this, inputDeps);
            commandBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }

        struct CreatureDeathJob : IJobForEachWithEntity<CreatureNeeds>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(Entity entity, int index, ref CreatureNeeds cn)
            {
                if (cn.Death == 1)
                    CommandBuffer.DestroyEntity(index,entity);
            }

        }
    }
}
