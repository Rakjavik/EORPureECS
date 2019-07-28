using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace rak.ecs.Systems
{

    [UpdateBefore(typeof(ObservationSystem))]
    public class MemoryAllocationSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            commandBufferSystem =
                World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            AllocationJob job = new AllocationJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                MemoryBuffers = GetBufferFromEntity<ShortMemoryBuffer>(),
                MedTermBuffers = GetBufferFromEntity<MediumTermMemoryBuffer>()
            };
            JobHandle handle = job.Schedule(this, inputDeps);
            commandBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }

        struct AllocationJob : IJobForEachWithEntity<MemoryAllocationNeededTag, Memory>
        {
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ShortMemoryBuffer> MemoryBuffers;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<MediumTermMemoryBuffer> MedTermBuffers;

            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(Entity entity, int index, [ReadOnly] ref MemoryAllocationNeededTag c0,
                ref Memory memory)
            {
                DynamicBuffer<ShortMemoryBuffer> memories = MemoryBuffers[entity];
                for(int count = 0; count < memory.MaxMemories; count++)
                {
                    memories.Add(new ShortMemoryBuffer
                    {
                        memory = new MemoryInstance
                        {
                            Subject = Entity.Null,
                            Verb = Verb.None
                        }
                    });
                }

                CommandBuffer.RemoveComponent<MemoryAllocationNeededTag>(index, entity);
            }
        }
    }
}
