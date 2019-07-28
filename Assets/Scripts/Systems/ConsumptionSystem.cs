using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace rak.ecs.Systems
{
    public class ConsumptionSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            commandBufferSystem =
                World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ConsumptionJob job = new ConsumptionJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                memoryBuffers = GetBufferFromEntity<ShortMemoryBuffer>()
            };
            JobHandle handle = job.Schedule(this, inputDeps);
            commandBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }

        struct ConsumptionJob : IJobForEachWithEntity<Target, CreatureAI,CreatureNeeds>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ShortMemoryBuffer> memoryBuffers;

            public void Execute(Entity entity, int index,ref Target target, ref CreatureAI cai,
                ref CreatureNeeds needs)
            {
                if(cai.CurrentAction == CreatureActions.Eat)
                {
                    DynamicBuffer<ShortMemoryBuffer> memories = memoryBuffers[entity];
                    needs.Hunger = 0;
                    cai.CurrentAction = CreatureActions.None;
                    CommandBuffer.DestroyEntity(index, target.Entity);
                    target.Entity = Entity.Null;
                    target.Position = float3.zero;
                    memories[target.MemoryIndex] = new ShortMemoryBuffer
                    {
                        memory = MemoryInstance.Empty
                    };
                    target.MemoryIndex = -1;
                }
            }
        }
    }
}
