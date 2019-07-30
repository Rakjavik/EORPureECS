using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public struct CreatureAI : IComponentData
    {
        public float DistanceToInteract;
        public CreatureActionType CurrentAction;
        public CreatureTaskType TaskType;
        public CreatureTaskStatus TaskStatus;
        public TaskFailReason FailReason;
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
                memoryBuffers = GetBufferFromEntity<ShortMemoryBuffer>(),
                random = new Random((uint)UnityEngine.Random.Range(1, 100000))
            };
            return job.Schedule(this, inputDeps);
        }

        [BurstCompile]
        struct CreatureAIJob : IJobForEachWithEntity<CreatureAI,CreatureNeeds,Target,Translation>
        {
            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ShortMemoryBuffer> memoryBuffers;

            public Random random;

            public void Execute(Entity entity, int index,
                ref CreatureAI cai, ref CreatureNeeds cn, ref Target target, ref Translation trans)
            {
                NativeArray<ShortMemoryBuffer> memoryArray = memoryBuffers[entity].ToNativeArray(Allocator.Temp);
                if (cai.TaskStatus == CreatureTaskStatus.Complete || cai.TaskStatus == CreatureTaskStatus.None)
                {
                    startNewTask(CreatureTaskType.Food,ref cai,memoryArray,trans,ref target,entity);
                }
                // FAILURE //
                else if (cai.TaskStatus == CreatureTaskStatus.Failed)
                {
                    if(cai.FailReason == TaskFailReason.NoKnownFood)
                    {
                        startNewTask(CreatureTaskType.Explore, ref cai, memoryArray, trans, ref target,entity);
                    }
                }
                // IN PROGRESS //
                else if (cai.TaskStatus == CreatureTaskStatus.InProgress)
                {
                    // MOVE //
                    if (cai.CurrentAction == CreatureActionType.Move)
                    {
                        if (cai.TaskType == CreatureTaskType.Food)
                        {
                            if (math.distance(trans.Value, target.Position) <= cai.DistanceToInteract)
                            {
                                cai.CurrentAction = CreatureActionType.Eat;
                            }
                        }
                        else if (cai.TaskType == CreatureTaskType.Explore)
                        {
                            if (math.distance(trans.Value, target.Position) <= cai.DistanceToInteract*2)
                            {
                                cai.CurrentAction = CreatureActionType.None;
                                cai.TaskStatus = CreatureTaskStatus.Complete;
                            }
                        }
                    }
                    else if (cai.CurrentAction == CreatureActionType.Eat)
                    {
                        if(target.Entity == Entity.Null)
                        {
                            cai.CurrentAction = CreatureActionType.None;
                            cai.TaskStatus = CreatureTaskStatus.Complete;
                        }
                    }
                }
                
            }

            private void startNewTask(CreatureTaskType type,ref CreatureAI cai,
                NativeArray<ShortMemoryBuffer> memoryArray, Translation trans,ref Target target,Entity entity)
            {
                cai.TaskType = type;
                cai.TaskStatus = CreatureTaskStatus.Started;
                cai.FailReason = TaskFailReason.None;
                // START FOOD TASK //
                if(type == CreatureTaskType.Food)
                {
                    int closestIndex = setClosestFruitAsTarget(memoryArray, trans.Value, ref target);
                    if (target.Position.Equals(float3.zero) || closestIndex == -1)
                    {
                        cai.TaskStatus = CreatureTaskStatus.Failed;
                        cai.CurrentAction = CreatureActionType.Cancelled;
                        cai.FailReason = TaskFailReason.NoKnownFood;
                    }
                    else
                    {
                        cai.TaskStatus = CreatureTaskStatus.InProgress;
                        cai.CurrentAction = CreatureActionType.Move;
                    }
                }
                // START EXPLORE TASK //
                else if (type == CreatureTaskType.Explore)
                {
                    float3 explorePosition = trans.Value;
                    explorePosition.x += random.NextFloat(-50, 50);
                    explorePosition.z += random.NextFloat(-50, 50);
                    target.Position = explorePosition;
                    cai.TaskStatus = CreatureTaskStatus.InProgress;
                    cai.CurrentAction = CreatureActionType.Move;
                }
            }

            private int setClosestFruitAsTarget(NativeArray<ShortMemoryBuffer> memories,float3 origin,
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
                    return closestIndex;
                }
                return -1;
            }
        }
    }
}
