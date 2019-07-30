using rak.ecs.Systems;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using UnityEngine;

namespace rak.ecs.mono
{
    [RequiresEntityConversion]
    public class BootstrapMono : MonoBehaviour, IDeclareReferencedPrefabs, IConvertGameObjectToEntity
    {
        public GameObject MonoPrefabThing;
        public GameObject MonoPrefabTree;
        public GameObject MonoPrefabFruit;
        public GameObject MonoPrefabBubble;
        public GameObject MonoPrefabGround;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            float2x2 bounds = new float2x2
            {
                c0 = new float2(-256, -256),
                c1 = new float2(256, 256)
            };

            dstManager.AddComponentData(entity, new PhysicsStep
            {
                Gravity = PhysicsStep.Default.Gravity,
                SimulationType = SimulationType.UnityPhysics,
                SolverIterationCount = PhysicsStep.Default.SolverIterationCount,
                ThreadCountHint = PhysicsStep.Default.ThreadCountHint
            });

            // Create a prefab entity //
            Entity prefabEntityThing = GameObjectConversionUtility.ConvertGameObjectHierarchy(MonoPrefabThing,
                World.Active);
            Entity prefabEntityTree = GameObjectConversionUtility.ConvertGameObjectHierarchy(MonoPrefabTree,
                World.Active);
            Entity prefabEntityFruit = GameObjectConversionUtility.ConvertGameObjectHierarchy(MonoPrefabFruit,
                World.Active);
            Entity prefabEntityBubble = GameObjectConversionUtility.ConvertGameObjectHierarchy(MonoPrefabBubble,
                World.Active);
            Entity prefabEntityGround = GameObjectConversionUtility.ConvertGameObjectHierarchy(MonoPrefabGround,
                World.Active);
            // Get it's collider //
            BlobAssetReference<Unity.Physics.Collider> prefabColliderThing =
                dstManager.GetComponentData<PhysicsCollider>(prefabEntityThing).Value;
            BlobAssetReference<Unity.Physics.Collider> prefabColliderTree =
                dstManager.GetComponentData<PhysicsCollider>(prefabEntityTree).Value;
            BlobAssetReference<Unity.Physics.Collider> prefabColliderFruit =
                dstManager.GetComponentData<PhysicsCollider>(prefabEntityFruit).Value;

            // Store these in a unique component for other systems to use later /
            PrefabContainer container = new PrefabContainer
            {
                prefabEntityCreature = prefabEntityThing,
                prefabEntityFruit = prefabEntityFruit,
                prefabEntityTree = prefabEntityTree,
                prefabEntityBubble = prefabEntityBubble,
                prefabColliderCreature = prefabColliderThing,
                prefabColliderFruit = prefabColliderFruit,
                prefabColliderTree = prefabColliderTree,
                prefabEntityGround = prefabEntityGround,
            };
            Entity containerEntity = dstManager.CreateEntity();
            dstManager.AddComponentData(containerEntity, container);
            World.Active.GetOrCreateSystem<ThingSpawnerSystem>().SetSingleton(container);

            Entity creatureSpawnerEntity = dstManager.CreateEntity();
            ThingSpawner creatureSpawner = new ThingSpawner
            {
                PrefabEntity = prefabEntityThing,
                ToSpawn = 5,
                PrefabCollider = prefabColliderThing,
                ThingToSpawn = ThingType.Creature,
                SpawnPerCycle = 5,
                MinMaxSpawnPositions = new float3x2
                {
                    c0 = new float3
                    {
                        x = -128,
                        y = 5,
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

            Entity terrainSpawnerEntity = dstManager.CreateEntity();
            dstManager.AddComponentData(terrainSpawnerEntity, new TerrainSpawner
            {
                Bounds = bounds,
                NumOfBubbles = 50,
                NumOfTrees = 500,
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
        }
    }
}

