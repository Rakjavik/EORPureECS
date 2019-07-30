using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using rak.ecs.Systems;
using Unity.Mathematics;
using System.Collections.Generic;

public class FollowCamera : MonoBehaviour
{
    private static Entity trackTarget;
    private EntityManager em;
    private bool initialized = false;
    private List<Entity> brokenEntities;

    public static void SetFollowTarget(Entity target)
    {
        trackTarget = target;
    }

    private void initialize(Entity brokenEntity)
    {
        if (brokenEntity != Entity.Null)
            brokenEntities.Add(brokenEntity);
        EntityQuery query = em.CreateEntityQuery(new ComponentType[] { typeof(Target) });
        NativeArray<Entity> entities = query.ToEntityArray(Allocator.TempJob);
        for(int count = 0; count < entities.Length; count++)
        {
            if (brokenEntities.Contains(entities[count]))
                continue;
            trackTarget = entities[count];
            
            Translation trans = em.GetComponentData<Translation>(trackTarget);
            if (float.IsNaN(trans.Value.x))
                break;
            Vector3 startPosition = trans.Value;
            startPosition -= transform.forward * 5;
            startPosition += transform.up * 2;
            transform.position = startPosition;
            initialized = true;
        }
        entities.Dispose();
        query.Dispose();
        Debug.Log("Track target - " + trackTarget);
    }

    void Start()
    {
        em = World.Active.EntityManager;
        brokenEntities = new List<Entity>();
        initialize(Entity.Null);
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialized)
        {
            initialize(trackTarget);
        }
        else
        {

            if (!em.Exists(trackTarget))
            {
                initialize(trackTarget);
            }
            Translation trans = em.GetComponentData<Translation>(trackTarget);
            if (double.IsNaN(trans.Value.x))
            {
                return;
            }
            float3 target = em.GetComponentData<Target>(trackTarget).Position;
            if (target.Equals(float3.zero))
                return;
            Vector3 startPosition = trans.Value;
            startPosition -= transform.forward * 5;
            startPosition += transform.up * 2;
            transform.position = startPosition;
            transform.LookAt(target);
        }
    }
}
