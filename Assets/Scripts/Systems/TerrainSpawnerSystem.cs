using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public enum TerrainType { None, Ground, Bubble, Tree }

    public struct TerrainSpawner : IComponentData
    {
        public int NumOfTrees;
        public int NumOfBubbles;
        public byte TerrainBuilt;
        public float2x2 Bounds;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class TerrainSpawnerSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        protected override void OnCreate()
        {
            commandBufferSystem =
                World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            TerrainSpawnerJob job = new TerrainSpawnerJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                prefabs = World.GetOrCreateSystem<ThingSpawnerSystem>().GetSingleton<PrefabContainer>(),
                random = new Random((uint)UnityEngine.Random.Range(1, 100000))
            };
            JobHandle handle = job.Schedule(this, inputDeps);
            commandBufferSystem.AddJobHandleForProducer(handle);
            return handle;
        }

        struct TerrainSpawnerJob : IJobForEachWithEntity<TerrainSpawner>
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            public PrefabContainer prefabs;
            public Random random;

            public void Execute(Entity entity, int index, ref TerrainSpawner ts)
            {
                if(ts.TerrainBuilt == 0)
                {
                    ts.TerrainBuilt = 1;
                    Entity groundEntity = CommandBuffer.Instantiate(index, prefabs.prefabEntityGround);
                    CommandBuffer.SetComponent(index, groundEntity, new Translation
                    {
                        Value = new float3(0, 0, 0)
                    });
                    CommandBuffer.AddComponent(index, groundEntity, new NonUniformScale
                    {
                        Value = new float3(ts.Bounds.c1.x-ts.Bounds.c0.x,1, ts.Bounds.c1.y - ts.Bounds.c0.y)
                    });

                    for (int count = 0; count < ts.NumOfTrees; count++)
                    {
                        createTree(index,prefabs.prefabEntityTree,prefabs.prefabColliderTree,ts.Bounds);
                    }

                    
                    for(int count = 0; count < ts.NumOfBubbles; count++)
                    {
                        createBubble(index, prefabs.prefabEntityBubble,ts.Bounds);
                    }

                    CommandBuffer.RemoveComponent<TerrainSpawner>(index, entity);
                }
            }
            // BUBBLE //
            private void createBubble(int index, Entity prefab, float2x2 bounds)
            {
                float3x2 minMaxPositions = new float3x2
                {
                    c0 = new float3(bounds.c0.x+20, -10, bounds.c0.y + 20),
                    c1 = new float3(bounds.c1.x-20, -4, bounds.c1.y - 20)
                };
                Entity newEntity = CommandBuffer.Instantiate(index, prefab);
                float3 position = getRandomPosition(minMaxPositions);
                CommandBuffer.SetComponent(index, newEntity, new Translation
                {
                    Value = position
                });
                CommandBuffer.AddComponent(index, newEntity, new ScaleChange
                {
                    Axis = ScaleChangeAxis.Y,
                    MinMax = new float2(22, 28),
                    Shrinking = 0,
                    Speed = random.NextFloat(1,4),
                    SpeedRatioWhenShrinking = random.NextFloat(.1f,.9f)
                });
                CommandBuffer.SetComponent(index, newEntity, new NonUniformScale
                {
                    Value = new float3(random.NextFloat(10,50), random.NextFloat(22, 28), random.NextFloat(10, 50))
                });
            }
            // TREE //
            private void createTree(int index,Entity prefab,BlobAssetReference<Collider> collider,float2x2 bounds)
            {
                float3x2 minMaxPositions = new float3x2
                {
                    c0 = new float3
                    {
                        x = bounds.c0.x,
                        y = 4,
                        z = bounds.c0.y
                    },
                    c1 = new float3
                    {
                        x = bounds.c1.x,
                        y = 0,
                        z = bounds.c1.y
                    }
                };
                Entity newEntity = CommandBuffer.Instantiate(index, prefab);
                float3 position = getRandomPosition(minMaxPositions);
                CommandBuffer.SetComponent(index, newEntity, new Translation
                {
                    Value = position
                });
                CommandBuffer.SetComponent(index, newEntity, new PhysicsCollider
                {
                    Value = collider
                });

                float3 fruitSpawnPositionMin = position;
                float3 fruitSpawnPositionMax = position;
                fruitSpawnPositionMin.y = 8;
                fruitSpawnPositionMin.x -= 5;
                fruitSpawnPositionMax.x += 5;
                fruitSpawnPositionMin.z -= 5;
                fruitSpawnPositionMax.z += 5;
                fruitSpawnPositionMax.y = 10;
                CommandBuffer.AddComponent(index, newEntity, new ThingSpawner
                {
                    PrefabCollider = prefabs.prefabColliderFruit,
                    PrefabEntity = prefabs.prefabEntityFruit,
                    SpawnPerCycle = 1,
                    ThingToSpawn = ThingType.Fruit,
                    ToSpawn = 0,
                    MinMaxSpawnPositions = new float3x2
                    {
                        c0 = fruitSpawnPositionMin,
                        c1 = fruitSpawnPositionMax
                    }
                });
                float spawnFruitEvery = 300;
                CommandBuffer.AddComponent(index, newEntity, new Tree
                {
                    ProducesEvery = spawnFruitEvery,
                    SinceLastProduction = random.NextFloat(spawnFruitEvery)
                });
            }

            private float3 getRandomPosition(float3x2 minMax)
            {
                return random.NextFloat3
                    (minMax.c0, minMax.c1);
            }
        }
    }

}

