using Unity.Collections;
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
            return job.Schedule(this, inputDeps);
        }

        struct CreatureDeathJob : IJobForEachWithEntity<CreatureNeeds,BlinkMovement>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(Entity entity, int index, ref CreatureNeeds cn, ref BlinkMovement bm)
            {
                if (cn.Death == 1)
                {
                    CommandBuffer.DestroyEntity(index, entity);
                    CommandBuffer.DestroyEntity(index, bm.BubbleEntity);
                }
            }

        }
    }
}
