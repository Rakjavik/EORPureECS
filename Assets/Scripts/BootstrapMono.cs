using rak.ecs.Systems;
using System.Collections.Generic;
using Unity.Build;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using UnityEngine;
using Random = Unity.Mathematics.Random;

namespace rak.ecs.mono
{
    [RequiresEntityConversion]
    public class BootstrapMono : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        private BlobAssetStore blobAssetStore;
        public GameObject MonoPrefabThing;
        public GameObject MonoPrefabTree;
        public GameObject MonoPrefabFruit;
        public GameObject MonoPrefabBubble;
        public GameObject MonoPrefabGround;
        public GameObject MonoPrefabBlink;
        public GameObject MonoPrefabBullet;
        public GameObject MonoPrefabAntiGrav;
        public GameObject MonoPrefabGnat;
        public GameObject MonoPrefabSphere;
        public GameObject MonoPrefabBox;
        public GameObject MonoPrefabSpherePhysics;

        private void OnDestroy()
        {
            blobAssetStore.Dispose();
        }

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            float squareBounds = 128;
            float2x2 bounds = new float2x2
            {
                c0 = new float2(-squareBounds, -squareBounds),
                c1 = new float2(squareBounds, squareBounds)
            };
            dstManager.AddComponentData(entity, new PhysicsStep
            {
                Gravity = PhysicsStep.Default.Gravity,
                SimulationType = SimulationType.UnityPhysics,
                SolverIterationCount = PhysicsStep.Default.SolverIterationCount,
                ThreadCountHint = PhysicsStep.Default.ThreadCountHint
            });
            //Entity eventSystem = dstManager.CreateEntity();
            //DynamicBuffer<EventBuffer> eventBuffer = dstManager.AddBuffer<EventBuffer>(eventSystem);
            blobAssetStore = new BlobAssetStore();
            GameObjectConversionSettings settings = GameObjectConversionSettings.FromWorld(World.DefaultGameObjectInjectionWorld, blobAssetStore);

            // Create a prefab entity //
            Entity prefabEntitySqGy = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabThing, settings);
            Entity prefabEntityTree = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabTree, settings);
            Entity prefabEntityFruit = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabFruit, settings);
            Entity prefabEntityBubble = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabBubble, settings);
            Entity prefabEntityGround = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabGround, settings);
            Entity prefabEntityBlink = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabBlink, settings);
            Entity prefabEntityBullet = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabBullet, settings);
            Entity prefabEntityAntiGrav = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabAntiGrav, settings);
            Entity prefabEntityGnat = GameObjectConversionUtility.ConvertGameObjectHierarchy(
                MonoPrefabGnat, settings);
            // Get it's collider //
            BlobAssetReference <Unity.Physics.Collider> prefabColliderSqGy =
                dstManager.GetComponentData<PhysicsCollider>(prefabEntitySqGy).Value;
            BlobAssetReference<Unity.Physics.Collider> prefabColliderTree =
                dstManager.GetComponentData<PhysicsCollider>(prefabEntityTree).Value;
            BlobAssetReference<Unity.Physics.Collider> prefabColliderFruit =
                dstManager.GetComponentData<PhysicsCollider>(prefabEntityFruit).Value;
            BlobAssetReference<Unity.Physics.Collider> prefabColliderGnat =
                dstManager.GetComponentData<PhysicsCollider>(prefabEntityGnat).Value;

            // Store these in a unique component for other systems to use later /
            PrefabContainer container = new PrefabContainer
            {
                prefabEntityCreature = prefabEntitySqGy,
                prefabEntityFruit = prefabEntityFruit,
                prefabEntityTree = prefabEntityTree,
                prefabEntityBubble = prefabEntityBubble,
                prefabColliderCreature = prefabColliderSqGy,
                prefabColliderFruit = prefabColliderFruit,
                prefabColliderTree = prefabColliderTree,
                prefabEntityGround = prefabEntityGround,
                prefabEntityBlink = prefabEntityBlink,
                prefabEntityBullet = prefabEntityBullet,
                prefabEntityAntiGrav = prefabEntityAntiGrav,
                prefabEntityGnat = prefabEntityGnat,
                prefabColliderGnat = prefabColliderGnat,
            };
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<AntiGravBubbleSystem>().Prefab = prefabEntityAntiGrav;
            Entity containerEntity = dstManager.CreateEntity();
            dstManager.AddComponentData(containerEntity, container);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<ThingSpawnerSystem>().SetSingleton(container);
            TerrainBounds terrainBounds = new TerrainBounds { Bounds = bounds };
            Entity tbEnt = dstManager.CreateEntity();
            dstManager.AddComponentData(tbEnt, terrainBounds);
            World.DefaultGameObjectInjectionWorld.GetOrCreateSystem<TerrainSpawnerSystem>().SetSingleton(terrainBounds);

            Entity civ = dstManager.CreateEntity();
            Debug.Log("Civ created - " + civ);
            dstManager.AddComponentData(civ, new Civilization
            {
                
            });
            dstManager.AddComponentData(civ, new Building
            {
                Civ = civ,
                Type = TaskTargetType.None
            });
            dstManager.AddBuffer<CivBuildings>(civ);
            dstManager.AddBuffer<CivTaskList>(civ);
            dstManager.AddBuffer<ResourcesNeeded>(civ);

            // VARIABLES //
            int numOfCreats = 1;
            int numOfThings = 0;
            int numOfTrees = 500;
            int spawnPerFrame = 1;

            if (numOfCreats >= 500 || numOfThings >= 500)
                spawnPerFrame = 25;
            else if (numOfCreats >= 100 || numOfThings >= 100)
                spawnPerFrame = 20;
            // VARIABLES //
            Entity creatureSpawnerEntity = dstManager.CreateEntity();
            ThingSpawner creatureSpawner = new ThingSpawner
            {
                PrefabEntity = prefabEntityGnat,
                ToSpawn = numOfCreats,
                PrefabCollider = prefabColliderGnat,
                ThingToSpawn = ThingType.Creature,
                SpawnPerCycle = spawnPerFrame,
                SpawnForCiv = civ,
                MinMaxSpawnPositions = new float3x2
                {
                    c0 = new float3
                    {
                        x = -128,
                        y = 2,
                        z = -128
                    },
                    c1 = new float3
                    {
                        x = 128,
                        y = 5,
                        z = 128
                    }
                }
            };
            dstManager.AddComponentData(creatureSpawnerEntity, creatureSpawner);

            Entity thingSpawnerEntity = dstManager.CreateEntity();
            ThingSpawner thingSpawner = new ThingSpawner
            {
                PrefabEntity = prefabEntitySqGy,
                ToSpawn = numOfThings,
                PrefabCollider = prefabColliderSqGy,
                ThingToSpawn = ThingType.SquareGuy,
                SpawnPerCycle = spawnPerFrame,
                SpawnForCiv = civ,
                MinMaxSpawnPositions = new float3x2
                {
                    c0 = new float3
                    {
                        x = -128,
                        y = 2,
                        z = -128
                    },
                    c1 = new float3
                    {
                        x = 128,
                        y = 5,
                        z = 128
                    }
                }
            };
            dstManager.AddComponentData(thingSpawnerEntity, thingSpawner);

            Entity terrainSpawnerEntity = dstManager.CreateEntity();
            dstManager.AddComponentData(terrainSpawnerEntity, new TerrainSpawner
            {
                NumOfBubbles = 0,
                NumOfTrees = numOfTrees,
                TerrainBuilt = 0
            });
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(MonoPrefabThing);
            referencedPrefabs.Add(MonoPrefabTree);
            referencedPrefabs.Add(MonoPrefabFruit);
            referencedPrefabs.Add(MonoPrefabBubble);
            referencedPrefabs.Add(MonoPrefabGround);
            referencedPrefabs.Add(MonoPrefabBlink);
        }
    }
}

