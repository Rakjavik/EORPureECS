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
        public Entity prefabEntityBlink;
        public Entity prefabEntityBullet;
        public Entity prefabEntityAntiGrav;
        public Entity prefabEntityGnat;

        public BlobAssetReference<Collider> prefabColliderCreature;
        public BlobAssetReference<Collider> prefabColliderTree;
        public BlobAssetReference<Collider> prefabColliderFruit;
        public BlobAssetReference<Collider> prefabColliderBullet;
        public BlobAssetReference<Collider> prefabColliderGnat;
    }

    public enum ThingType { None,Event, Creature, Tree, Fruit, SquareGuy }

    public struct ThingSpawner : IComponentData
    {
        public Entity PrefabEntity;
        public int ToSpawn;
        public BlobAssetReference<Collider> PrefabCollider;
        public float3x2 MinMaxSpawnPositions;
        public ThingType ThingToSpawn;
        public int SpawnPerCycle;
        public Entity SpawnedFrom;
        public Entity SpawnForCiv;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class ThingSpawnerSystem : JobComponentSystem
    {
        private BeginInitializationEntityCommandBufferSystem commandBufferSystem;

        private float updateEvery = .5f;
        private float sinceLastUpdate = 0;

        protected override void OnCreate()
        {
            commandBufferSystem =
                World.GetOrCreateSystem<BeginInitializationEntityCommandBufferSystem>();
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            sinceLastUpdate += Time.DeltaTime;
            if (sinceLastUpdate < updateEvery)
                return inputDeps;
            sinceLastUpdate = 0;
            SpawnerJob job = new SpawnerJob
            {
                CommandBuffer = commandBufferSystem.CreateCommandBuffer().ToConcurrent(),
                random = new Random((uint)UnityEngine.Random.Range(1, 100000)),
                prefabs = GetSingleton<PrefabContainer>(),
                blinks = GetComponentDataFromEntity<BlinkMovement>(true)
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

            [ReadOnly]
            public ComponentDataFromEntity<BlinkMovement> blinks;

            public void Execute(Entity entity, int index, ref ThingSpawner spawner)
            {
                if(spawner.ToSpawn > 0)
                {
                    for (int count = 0; count < spawner.SpawnPerCycle; count++)
                    {
                        Entity newEntity = CommandBuffer.Instantiate(index, spawner.PrefabEntity);
                        //UnityEngine.Debug.Log("NewE - " + newEntity);
                        initializeThing(index, newEntity, spawner,prefabs,blinks);
                    }
                    spawner.ToSpawn -= spawner.SpawnPerCycle;
                }
                else
                {
                    if (spawner.ThingToSpawn == ThingType.Creature)
                    {
                        CommandBuffer.DestroyEntity(index, entity);
                    }
                }
            }

            private void initializeThing(int index, Entity newEntity,ThingSpawner spawner,
                PrefabContainer prefabs, ComponentDataFromEntity<BlinkMovement> blinks)
            {
                float3 position = getRandomPosition(spawner);
                CommandBuffer.SetComponent(index, newEntity, new Translation
                {
                    Value = position
                });
                if (spawner.ThingToSpawn != ThingType.None)
                {
                    CommandBuffer.AddComponent(index, newEntity, new PhysicsCollider
                    {
                        Value = spawner.PrefabCollider
                    });
                }
                CommandBuffer.AddComponent(index, newEntity, new SpawnedFrom
                {
                    Parent = spawner.SpawnedFrom
                });

                // CREATURE //
                if (spawner.ThingToSpawn == ThingType.Creature)
                {
                    CommandBuffer.AddComponent(index, newEntity, new MemberOfCiv
                    {
                        Civ = spawner.SpawnForCiv
                    });
                    CommandBuffer.AddComponent(index, newEntity, new DistanceToGround { });
                    CommandBuffer.AddComponent(index, newEntity, new DistanceToCollision
                    {
                        SinceLastUpdated = 0,
                        UpdateEvery = .5f
                    });
                    CommandBuffer.AddComponent(index, newEntity, new DistanceInFront { });
                    CommandBuffer.AddComponent(index, newEntity, new Flight
                    {
                        Acceleration = new float3(25,125,50), //new float3(15, random.NextFloat(50, 300), random.NextFloat(50, 300)),
                        SinceLastUpdate = 0,
                        UpdateEvery = .05f,
                        SustainHeight = 4,
                        CruiseSpeed = 10
                    });
                    CommandBuffer.AddComponent(index, newEntity, new AntiGravity
                    {
                        Active = 0,
                        CoolDown = 1,
                        CurrentCoolDown = 0,
                        PrefabBubble = prefabs.prefabEntityAntiGrav,
                        FreezePosition = float3.zero,
                        WrongDirectionSensitivity = .5f,
                    });
                    CommandBuffer.AddComponent(index, newEntity, new TractorBeam
                    {
                        Active = 0,
                        BeamStrength = 30,
                        Range = 10
                    });
                    CommandBuffer.AddComponent(index, newEntity, new CanAttack
                    {
                        Range = new int2(8,20),
                        RequestFire = 0
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Cannon
                    {
                        Cooldown = .2f,
                        CurrentCooldown = 0,
                        ProjectilePrefab = prefabs.prefabEntityBullet,
                        ProjectileSpeed = 30,
                        MountOffset = float3.zero,
                    });
                    CommandBuffer.AddComponent(index, newEntity, new SpeedLimit
                    {
                        Linear = new float3(150, 150, 500),
                        Angular = float3.zero,
                        BrakeStrength = .1f,
                        EngageWhenThisCloseToTarget = 2,
                        MinimumSpeed = 10
                    });
                    /*CommandBuffer.AddComponent(index, newEntity, new Memory
                    {
                        CurrentMemoryIndex = 0,
                        MaxMemories = 100,
                    });
                    CommandBuffer.AddComponent(index, newEntity, new MemoryAllocationNeededTag { });
                    CommandBuffer.AddBuffer<ShortMemoryBuffer>(index, newEntity);*/

                    CommandBuffer.AddComponent(index, newEntity, new Observer
                    {
                        ObservationDistance = random.NextFloat(35,150),
                        ObserveEvery = 3,
                        SinceLastObservation = random.NextFloat(3)
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Observable
                    {
                        ThingType = ThingType.Creature
                    });
                    CommandBuffer.AddComponent(index, newEntity, new TurnSpeed
                    {
                        Value = 2
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Target
                    {
                        Position = float3.zero,
                        Entity = Entity.Null
                    });
                    CommandBuffer.AddComponent(index, newEntity, new CreatureNeeds
                    {
                        Hunger = 0,
                        HungerFactor = .1f
                    });
                    /*CommandBuffer.AddComponent(index, newEntity, new CreatureAI
                    {
                        CurrentAction = CreatureActionType.None,
                        DistanceToInteract = 2
                    });*/
                    CommandBuffer.AddComponent(index, newEntity, new AICreature
                    {
                        AfterMoveAction = CreatureActionType.None,
                        ElapsedTaskTime = 0,
                        FailReason = TaskFailReason.None,
                        TaskStatus = CreatureTaskStatus.Complete,
                        TaskType = CreatureTaskType.None
                    });
                    CommandBuffer.AddComponent(index, newEntity, new CreatureCurrentActionType
                    {
                        CurrentAction = CreatureActionType.None
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Age
                    {
                        CurrentAge = 0,
                        MaxAge = 600
                    });
                    CommandBuffer.AddComponent(index, newEntity, new ChildComponentsNeeded
                    {
                        ExecutesIn = .2f,
                        Prefab = prefabs.prefabEntityGnat
                    });

                    /*Entity blinkEntity = CommandBuffer.Instantiate(index, prefabs.prefabEntityBlink);
                    if (!spawner.SpawnedFrom.Equals(Entity.Null))
                    {
                        BlinkMovement bm = blinks[spawner.SpawnedFrom];
                        bm.BubbleEntity = blinkEntity;
                        CommandBuffer.AddComponent(index, newEntity, bm);
                    }
                    else
                    {
                        CommandBuffer.AddComponent(index, newEntity, new BlinkMovement
                        {
                            CoolDown = random.NextFloat(.5f, 10),
                            CurrentCoolDown = 0,
                            JumpDistance = random.NextFloat(.5f, 10),
                            BubbleEntity = blinkEntity
                        });
                    }
                    CommandBuffer.AddComponent(index, blinkEntity, new BlinkBubble
                    {
                        ChargeTime = 5,
                        CurrentCharge = 0,
                        Parent = newEntity
                    });
                    CommandBuffer.AddComponent(index, blinkEntity, new NonUniformScale
                    {
                        Value = new float3(.5f, .5f, .5f)
                    });
                     */
                }

                // SQUAREGUY //
                else if (spawner.ThingToSpawn == ThingType.SquareGuy)
                {
                    CommandBuffer.AddComponent(index, newEntity, new Flight
                    {
                        Acceleration = new float3(5, 1, 5), //new float3(15, random.NextFloat(50, 300), random.NextFloat(50, 300)),
                        SinceLastUpdate = 0,
                        UpdateEvery = .05f,
                        SustainHeight = 4,
                        CruiseSpeed = 10
                    });
                    CommandBuffer.AddComponent(index, newEntity, new TurnSpeed
                    {
                        Value = 2
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Target
                    {
                        Position = float3.zero,
                        Entity = Entity.Null
                    });
                    CommandBuffer.AddComponent(index, newEntity, new CreatureNeeds
                    {
                        Hunger = 0,
                        HungerFactor = 1f
                    });
                    CommandBuffer.AddComponent(index, newEntity, new CreatureAI
                    {
                        CurrentAction = CreatureActionType.None,
                        DistanceToInteract = 2
                    });
                    CommandBuffer.AddComponent(index, newEntity, new DistanceToGround { });
                    CommandBuffer.AddComponent(index, newEntity, new DistanceInFront { });
                    CommandBuffer.AddComponent(index, newEntity, new Age
                    {
                        CurrentAge = 0,
                        MaxAge = 600
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Observer
                    {
                        ObservationDistance = 100,
                        ObserveEvery = 3,
                        SinceLastObservation = random.NextFloat(3)
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Observable
                    {
                        ThingType = ThingType.Creature
                    });
                    /*CommandBuffer.AddComponent(index, newEntity, new Memory
                    {
                        CurrentMemoryIndex = 0,
                        MaxMemories = 100,
                    });
                    CommandBuffer.AddComponent(index, newEntity, new MemoryAllocationNeededTag { });
                    CommandBuffer.AddBuffer<ShortMemoryBuffer>(index, newEntity);*/

                }

                // FRUIT //
                else if (spawner.ThingToSpawn == ThingType.Fruit)
                {
                    CommandBuffer.AddComponent(index, newEntity, new Observable
                    {
                        ThingType = ThingType.Fruit,
                        Available = 1
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Age
                    {
                        CurrentAge = 0,
                        MaxAge = 600
                    });
                    CommandBuffer.AddComponent(index, newEntity, new Claimable { Claimed = 0 });
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

