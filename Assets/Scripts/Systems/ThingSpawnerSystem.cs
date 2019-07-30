using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public struct PrefabContainer : IComponentData
    {
        public Entity prefabEntityCreature;
        public Entity prefabEntityTree;
        public Entity prefabEntityFruit;
        public Entity prefabEntityBubble;
        public Entity prefabEntityGround;

        public BlobAssetReference<Collider> prefabColliderCreature;
        public BlobAssetReference<Collider> prefabColliderTree;
        public BlobAssetReference<Collider> prefabColliderFruit;
    }

    public enum ThingType { None, Creature, Tree, Fruit }

    public struct ThingSpawner : IComponentData
    {
        public Entity PrefabEntity;
        public int ToSpawn;
        public BlobAssetReference<Collider> PrefabCollider;
        public float3x2 MinMaxSpawnPositions;
        public ThingType ThingToSpawn;
        public int SpawnPerCycle;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class ThingSpawnerSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            commandBufferSystem =
                World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SpawnerJob job = new SpawnerJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                random = new Random((uint)UnityEngine.Random.Range(1, 100000)),
                prefabs = GetSingleton<PrefabContainer>()
        };
            JobHandle handle = job.Schedule(this, inputDeps);
            commandBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }

        //[BurstCompile]
        struct SpawnerJob : IJobForEachWithEntity<ThingSpawner>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public Random random;

            public PrefabContainer prefabs;

            public void Execute(Entity entity, int index, ref ThingSpawner spawner)
            {
                if(spawner.ToSpawn > 0)
                {
                    for (int count = 0; count < spawner.SpawnPerCycle; count++)
                    {
                        Entity newEntity = CommandBuffer.Instantiate(index, spawner.PrefabEntity);
                        initializeThing(index, newEntity, spawner,prefabs);
                    }
                    spawner.ToSpawn -= spawner.SpawnPerCycle;
                }
            }

            private void initializeThing(int index, Entity newEntity,ThingSpawner spawner,
                PrefabContainer prefabs)
            {
                float3 position = getRandomPosition(spawner);
                CommandBuffer.SetComponent(index, newEntity, new Translation
                {
                    Value = position
                });
                if (spawner.ThingToSpawn != ThingType.None)
                {
                    CommandBuffer.SetComponent(index, newEntity, new PhysicsCollider
                    {
                        Value = spawner.PrefabCollider
                    });
                }

                // CREATURE //
                if (spawner.ThingToSpawn == ThingType.Creature)
                {
                    CommandBuffer.AddComponent(index, newEntity, new Flight
                    {
                        Acceleration = new float3(100, random.NextFloat(60, 100), 100),
                        SinceLastUpdate = 0,
                        UpdateEvery = .1f,
                        SustainHeight = 6
                    });
                    CommandBuffer.AddComponent(index, newEntity, new SpeedLimit
                    {
                        Linear = new float3(random.NextFloat(10,30), 5, random.NextFloat(10, 30)),
                        Angular = float3.zero,
                        BrakeStrength = .1f,
                        EngageWhenThisCloseToTarget = random.NextFloat(2, 15),
                        MinimumSpeed = random.NextFloat(2, 50)
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Memory
                    {
                        CurrentMemoryIndex = 0,
                        MaxMemories = 100,
                    });
                    CommandBuffer.AddComponent(index, newEntity, new MemoryAllocationNeededTag { });
                    CommandBuffer.AddBuffer<ShortMemoryBuffer>(index, newEntity);

                    CommandBuffer.AddComponent(index, newEntity, new Observer
                    {
                        ObservationDistance = random.NextFloat(35,150),
                        ObserveEvery = 1,
                        SinceLastObservation = random.NextFloat(1)
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Observable
                    {
                        ThingType = ThingType.Creature
                    });
                    /*CommandBuffer.AddComponent(index, newEntity, new BlinkMovement
                    {
                        CoolDown = random.NextFloat(.5f, 3),
                        CurrentCoolDown = 0,
                        JumpDistance = random.NextFloat(2, 10)
                    });*/
                    CommandBuffer.AddComponent(index, newEntity, new Target
                    {
                        Position = float3.zero
                    });
                    CommandBuffer.AddComponent(index, newEntity, new CreatureNeeds
                    {
                        Hunger = 0
                    });
                    CommandBuffer.AddComponent(index, newEntity, new CreatureAI
                    {
                        CurrentAction = CreatureActionType.None,
                        DistanceToInteract = 3
                    });
                }

                // FRUIT //
                else if (spawner.ThingToSpawn == ThingType.Fruit)
                {
                    CommandBuffer.AddComponent(index, newEntity, new Observable
                    {
                        ThingType = ThingType.Fruit
                    });
                }
            }

            private float3 getRandomPosition(ThingSpawner spawner)
            {
                return random.NextFloat3
                    (spawner.MinMaxSpawnPositions.c0, spawner.MinMaxSpawnPositions.c1);
            }
            private float3 getRandomPosition(float3x2 minMax)
            {
                return random.NextFloat3
                    (minMax.c0, minMax.c1);
            }

        }
    }

}

