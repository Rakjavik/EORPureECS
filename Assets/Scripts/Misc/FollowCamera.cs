using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using rak.ecs.Systems;

public class FollowCamera : MonoBehaviour
{
    private Entity trackTarget;
    private EntityManager em;
    private bool initialized = false;

    private void initialize()
    {
        EntityQuery query = em.CreateEntityQuery(new ComponentType[] { typeof(Movement) });
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        if (entities.Length > 0 && !entities[0].Equals(Entity.Null))
        {
            trackTarget = entities[0];
            Translation trans = em.GetComponentData<Translation>(trackTarget);
            Vector3 startPosition = trans.Value;
            startPosition -= transform.forward * 2;
            transform.position = startPosition;
            initialized = true;
        }
        entities.Dispose();
        query.Dispose();
    }

    void Start()
    {
        em = World.Active.EntityManager;
        initialize();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized)
        {
            initialize();
        }
        else
        {
            Translation trans = em.GetComponentData<Translation>(trackTarget);
            Vector3 startPosition = trans.Value;
            startPosition -= transform.forward * 5;
            transform.position = startPosition;
            transform.LookAt(trans.Value);
        }
    }
}
