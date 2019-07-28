using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace rak.ecs.Systems
{
    public struct MemoryAllocationNeededTag : IComponentData { }

    public struct Memory : IComponentData
    {
        public int CurrentMemoryIndex;
        public int MaxMemories;
    }

    [InternalBufferCapacity(0)]
    public struct ShortMemoryBuffer : IBufferElementData
    {
        public MemoryInstance memory;
    }

    public struct MemoryInstance
    {
        public Entity Subject;
        public Verb Verb;
        public int Iterations;
        public ThingType Type;
        public float3 Position;

        public static MemoryInstance Empty { get
            {
                return new MemoryInstance
                {
                    Iterations = 0,
                    Position = float3.zero,
                    Subject = Entity.Null,
                    Type = ThingType.None,
                    Verb = Verb.None
                };
            }
        }
    }

    public enum Verb { None, Saw }

    [InternalBufferCapacity(0)]
    public struct MediumTermMemoryBuffer : IBufferElementData
    {
        public MemoryInstance memory;
    }

    public struct MediumTermMemory : IComponentData
    {
        public int CurrentMemoryIndex;
        public int MaxMemories;
        public Entity CreatureLink;
    }

    public class MediumTermMemorySystem : JobComponentSystem
    {
        protected override void OnCreate()
        {
            Enabled = false;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            CopyShortToMediumJob job = new CopyShortToMediumJob
            {
                buffers = GetBufferFromEntity<ShortMemoryBuffer>(true)
            };
            return job.Schedule(this, inputDeps);
        }

        struct CopyShortToMediumJob : IJobForEachWithEntity_EBC<MediumTermMemoryBuffer,MediumTermMemory>
        {
            public BufferFromEntity<ShortMemoryBuffer> buffers;

            public void Execute(Entity entity, int index,DynamicBuffer<MediumTermMemoryBuffer> mtmb,
                ref MediumTermMemory mtm)
            {
                NativeArray<ShortMemoryBuffer> creatureMemories = buffers[mtm.CreatureLink].ToNativeArray(Allocator.Temp);
                for(int count = 0; count < creatureMemories.Length; count++)
                {
                    
                }

            }
        }
    }
}
