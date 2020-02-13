using rak.ecs.Systems;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace rak.ecs.mono
{
    public class EntityDebugMono : MonoBehaviour
    {
        private EntityManager em;
        private float updateEvery = .5f;
        private float sinceLastUpdate = 0;

        private void Start()
        {
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
        }

        private void Update()
        {
            NativeArray<Entity> targets = em.CreateEntityQuery(new ComponentType[]
                { typeof(Target) }).ToEntityArray(Allocator.TempJob);

            for(int count = 0; count < targets.Length; count++)
            {
                Vector3 start = em.GetComponentData<Translation>(targets[count]).Value;
                Vector3 end = em.GetComponentData<Target>(targets[count]).Position;
                if(!end.Equals(float3.zero))
                    Debug.DrawLine(start, end, Color.yellow);
            }
            targets.Dispose();
        }
    }
}

