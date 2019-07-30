using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace rak.ecs.Systems
{
    [UpdateAfter(typeof(CreatureAISystem))]
    public class ConsumptionSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            Enabled = true;
            commandBufferSystem =
                World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ConsumptionJob job = new ConsumptionJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                memoryBuffers = GetBufferFromEntity<ShortMemoryBuffer>(),
                validEntities = GetEntityQuery(new ComponentType[] { typeof(Observable)}).
                ToEntityArray(Allocator.TempJob)
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

            [ReadOnly]
            [DeallocateOnJobCompletion]
            public NativeArray<Entity> validEntities;

            public void Execute(Entity entity, int index,ref Target target, ref CreatureAI cai,
                ref CreatureNeeds needs)
            {
                if(cai.CurrentAction == CreatureActionType.Eat)
                {
                    if (target.MemoryIndex == -1)
                    {
                        UnityEngine.Debug.LogError("Memory index -1 in Consumption system");
                        return;
                    }
                    DynamicBuffer<ShortMemoryBuffer> memories = memoryBuffers[entity];
                    if (validEntities.Contains(target.Entity))
                    {
                        needs.Hunger -= 20;
                        CommandBuffer.DestroyEntity(index, target.Entity);
                    }
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
