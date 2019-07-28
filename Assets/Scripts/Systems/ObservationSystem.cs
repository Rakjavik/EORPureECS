using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace rak.ecs.Systems
{

    public struct Observer : IComponentData
    {
        public float ObservationDistance;
        public float ObserveEvery;
        public float SinceLastObservation;
    }

    public struct Observable : IComponentData
    {
        public ThingType ThingType;
    }

    public class ObservationSystem : JobComponentSystem
    {
        private NativeArray<Translation> translations;
        private NativeArray<Entity> entities;
        private NativeArray<Observable> observables;
        private EntityQuery query;
        private EntityQuery chunkQuery;
        private float SinceLastQueryUpdate;
        private float UpdateQueryEvery = 1;

        protected override void OnCreate()
        {
            Enabled = true;
            query = GetEntityQuery(new ComponentType[] { typeof(Observable), typeof(Translation) });
            chunkQuery = GetEntityQuery(new ComponentType[] { typeof(Observer) });
            updateQuery(true);
            SinceLastQueryUpdate = 0;
        }
        protected override void OnDestroy()
        {
            translations.Dispose();
            entities.Dispose();
            observables.Dispose();
        }

        private void updateQuery(bool onCreate)
        {
            if (!onCreate)
            {
                translations.Dispose();
                entities.Dispose();
                observables.Dispose();
            }
            translations = query.ToComponentDataArray<Translation>(Allocator.Persistent);
            entities = query.ToEntityArray(Allocator.Persistent);
            observables = query.ToComponentDataArray<Observable>(Allocator.Persistent);
        }
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SinceLastQueryUpdate += UnityEngine.Time.deltaTime;
            if(SinceLastQueryUpdate >= UpdateQueryEvery)
            {
                updateQuery(false);
            }

            ObservationChunkJob job = new ObservationChunkJob
            {
                delta = UnityEngine.Time.deltaTime,
                MemoryBuffers = GetBufferFromEntity<ShortMemoryBuffer>(false),
                ObservableEntities = entities,
                ObservablePositions = translations,
                ObservableTypes = observables,

                EntityType = GetArchetypeChunkEntityType(),
                MemoryType = GetArchetypeChunkComponentType<Memory>(false),
                ObserverType = GetArchetypeChunkComponentType<Observer>(false),
                TranslationType = GetArchetypeChunkComponentType<Translation>(true)
            };

            return job.Schedule(chunkQuery, inputDeps);
        }
        [BurstCompile]
        struct ObservationChunkJob : IJobChunk
        {
            public ArchetypeChunkComponentType<Observer> ObserverType;

            [ReadOnly]
            public ArchetypeChunkComponentType<Translation> TranslationType;

            public ArchetypeChunkComponentType<Memory> MemoryType;

            [ReadOnly]
            public ArchetypeChunkEntityType EntityType;

            [ReadOnly]
            public NativeArray<Translation> ObservablePositions;

            [ReadOnly]
            public NativeArray<Entity> ObservableEntities;

            [ReadOnly]
            public NativeArray<Observable> ObservableTypes;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ShortMemoryBuffer> MemoryBuffers;

            public float delta;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                NativeArray<Observer> observers = chunk.GetNativeArray(ObserverType);
                NativeArray<Translation> translations = chunk.GetNativeArray(TranslationType);
                NativeArray<Memory> memories = chunk.GetNativeArray(MemoryType);
                NativeArray<Entity> entities = chunk.GetNativeArray(EntityType);
                for (int entCount = 0; entCount < chunk.Count; entCount++)
                {
                    Observer obs = observers[entCount];
                    Translation trans = translations[entCount];
                    Memory memory = memories[entCount];
                    DynamicBuffer<ShortMemoryBuffer> buffer = MemoryBuffers[entities[entCount]];

                    obs.SinceLastObservation += delta;

                    if(obs.SinceLastObservation >= obs.ObserveEvery)
                    {
                        obs.SinceLastObservation = 0;

                        for (int count = 0; count < ObservableEntities.Length; count++)
                        {
                            float distance = math.distance(ObservablePositions[count].Value, trans.Value);
                            if (distance <= obs.ObservationDistance)
                            {
                                int existingIndex = getExistingMemory(ObservableEntities[count],
                                    Verb.Saw, buffer.ToNativeArray(Allocator.Temp));
                                if (existingIndex == -1)
                                {
                                    buffer[memory.CurrentMemoryIndex] = new ShortMemoryBuffer
                                    {
                                        memory = new MemoryInstance
                                        {
                                            Subject = ObservableEntities[count],
                                            Verb = Verb.Saw,
                                            Type = ObservableTypes[count].ThingType,
                                            Position = ObservablePositions[count].Value
                                        }
                                    };

                                    memory.CurrentMemoryIndex++;
                                    if (memory.CurrentMemoryIndex >= memory.MaxMemories)
                                        memory.CurrentMemoryIndex = 0;
                                }
                                else
                                {
                                    ShortMemoryBuffer existingMemory = buffer[existingIndex];
                                    existingMemory.memory.Iterations++;
                                    existingMemory.memory.Position = ObservablePositions[count].Value;
                                    buffer[existingIndex] = existingMemory;
                                }
                            }
                        }
                        memories[entCount] = memory;
                    }
                    observers[entCount] = obs;
                }
            }

            private int getExistingMemory(Entity subject, Verb verb, NativeArray<ShortMemoryBuffer> memories)
            {
                for (int count = 0; count < memories.Length; count++)
                {
                    if (memories[count].memory.Subject == subject &&
                        memories[count].memory.Verb == verb)
                    {
                        return count;
                    }
                }
                return -1;
            }
        }
        /*
        struct ObservationJob : IJobForEachWithEntity<Observer,Translation,Memory>
        {
            //[DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<Translation> ObservablePositions;

            //[DeallocateOnJobCompletion]
            [ReadOnly]
            public NativeArray<Entity> ObservableEntities;

            [NativeDisableParallelForRestriction]
            public BufferFromEntity<ShortMemoryBuffer> Memories;

            public float delta;

            public void Execute(Entity entity, int index,ref Observer obs, ref Translation trans,
                ref Memory memory)
            {
                obs.SinceLastObservation += delta;
                if (obs.SinceLastObservation >= obs.ObserveEvery)
                {
                    obs.SinceLastObservation = 0;
                    DynamicBuffer<ShortMemoryBuffer> buffer = Memories[entity];
                    for (int count = 0; count < ObservableEntities.Length; count++)
                    {
                        float distance = math.distance(ObservablePositions[count].Value, trans.Value);
                        if (distance <= obs.ObservationDistance)
                        {
                            int existingIndex = getExistingMemory(ObservableEntities[count],
                                Verb.Saw, buffer);
                            if (existingIndex == -1)
                            {
                                buffer[memory.CurrentMemoryIndex] = new ShortMemoryBuffer
                                {
                                    memory = new MemoryInstance
                                    {
                                        Subject = ObservableEntities[count],
                                        Verb = Verb.Saw
                                    }
                                };

                                memory.CurrentMemoryIndex++;
                                if (memory.CurrentMemoryIndex >= memory.MaxMemories)
                                    memory.CurrentMemoryIndex = 0;
                            }
                            else
                            {
                                ShortMemoryBuffer existingMemory = buffer[existingIndex];
                                existingMemory.memory.Iterations++;
                                buffer[existingIndex] = existingMemory;
                            }
                        }
                    }
                }
            }

            private int getExistingMemory(Entity subject, Verb verb,DynamicBuffer<ShortMemoryBuffer> memories)
            {
                for(int count = 0; count < memories.Length; count++)
                {
                    if(memories[count].memory.Subject == subject &&
                        memories[count].memory.Verb == verb)
                    {
                        return count;
                    }
                }
                return -1;
            }
        }*/
    }
}
