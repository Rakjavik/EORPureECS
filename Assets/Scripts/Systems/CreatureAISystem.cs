using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace rak.ecs.Systems
{

    public enum CreatureActions { None, Move, Eat }

    public struct CreatureAI : IComponentData
    {
        public CreatureActions CurrentAction;
        public float DistanceToInteract;
        public Entity ToConsume;
    }

    public class CreatureAISystem : JobComponentSystem
    {
        protected override void OnCreate()
        {
            Enabled = true;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            CreatureAIJob job = new CreatureAIJob
            {
                memoryBuffers = GetBufferFromEntity<ShortMemoryBuffer>()
            };
            return job.Schedule(this, inputDeps);
        }

        [BurstCompile]
        struct CreatureAIJob : IJobForEachWithEntity<CreatureAI,CreatureNeeds,
            Target,Translation>
        {
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ShortMemoryBuffer> memoryBuffers;

            public void Execute(Entity entity, int index,
                ref CreatureAI cai, ref CreatureNeeds cn, ref Target target, ref Translation trans)
            {
                // NO CURRENT ACTION //
                if(cai.CurrentAction == CreatureActions.None)
                {
                    DynamicBuffer<ShortMemoryBuffer> memories = memoryBuffers[entity];
                    NativeArray<ShortMemoryBuffer> memoryArray = memories.ToNativeArray(Allocator.Temp);
                    if(cn.MostUrgent == Needs.Hunger)
                    {
                        setClosestFruitAsTarget(memoryArray, trans.Value, ref target);
                        if (!target.Position.Equals(float3.zero))
                        {
                            cai.CurrentAction = CreatureActions.Move;
                        }
                    }
                }
                // CURRENTLY MOVING //
                else if (cai.CurrentAction == CreatureActions.Move)
                {
                    if(math.distance(trans.Value,target.Position) <= cai.DistanceToInteract)
                    {
                        cai.CurrentAction = CreatureActions.Eat;
                        cai.ToConsume = target.Entity;
                    }
                }
            }

            private void setClosestFruitAsTarget(NativeArray<ShortMemoryBuffer> memories,float3 origin,
                ref Target target)
            {
                float closestDistance = float.MaxValue;
                Entity closestEntity = Entity.Null;
                float3 closestPosition = float3.zero;
                int closestIndex = -1;

                for(int count = 0; count < memories.Length; count++)
                {
                    if (memories[count].memory.Type == ThingType.Fruit)
                    {
                        float distance = math.distance(origin, memories[count].memory.Position);
                        if (distance < closestDistance)
                        {
                            closestDistance = distance;
                            closestEntity = memories[count].memory.Subject;
                            closestPosition = memories[count].memory.Position;
                            closestIndex = count;
                        }
                    }
                }
                // Target found //
                if (closestIndex > -1)
                {
                    target.Entity = closestEntity;
                    target.Position = closestPosition;
                    target.MemoryIndex = closestIndex;
                }
            }
        }
    }
}
