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

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
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
                prefabColliderCreature = prefabColliderThing,
                prefabColliderFruit = prefabColliderFruit,
                prefabColliderTree = prefabColliderTree
            };
            Entity containerEntity = dstManager.CreateEntity();
            dstManager.AddComponentData(containerEntity, container);
            World.Active.GetOrCreateSystem<SpawnerSystem>().SetSingleton(container);

            Entity spawnerEntity = dstManager.CreateEntity();
            Spawner creatureSpawner = new Spawner
            {
                PrefabEntity = prefabEntityThing,
                ToSpawn = 1500,
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
            dstManager.AddComponentData(spawnerEntity, creatureSpawner);

            Entity treeSpawnerEntity = dstManager.CreateEntity();
            float3x2 minMaxPositions = new float3x2
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
                    y = 2,
                    z = 128
                }
            };
            Spawner treeSpawner = new Spawner
            {
                PrefabCollider = prefabColliderTree,
                PrefabEntity = prefabEntityTree,
                ToSpawn = 200,
                MinMaxSpawnPositions = minMaxPositions,
                ThingToSpawn = ThingType.Tree,
                SpawnPerCycle = 10
            };
            dstManager.AddComponentData(treeSpawnerEntity, treeSpawner);

        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            referencedPrefabs.Add(MonoPrefabThing);
            referencedPrefabs.Add(MonoPrefabTree);
        }
    }
}

