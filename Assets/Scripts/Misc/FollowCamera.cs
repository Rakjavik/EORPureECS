using UnityEngine;
using System.Collections;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using rak.ecs.Systems;
using Unity.Mathematics;
using System.Collections.Generic;
using System;
using rak.UI;

public class FollowCamera : MonoBehaviour
{
    public bool IgnoreTargets;

    private static Entity trackTarget;
    public static int trackTargetIndex;
    private static float FOLLOWDISTANCEZ = 1f;
    private static float FOLLOWDISTANCEY = .3f;
    private EntityManager em;
    private bool initialized = false;
    private List<Entity> brokenEntities;
    private EntityQuery targetQuery;

    public static void SetFollowTarget(Entity target)
    {
        trackTarget = target;
        Debug.Log("FollowCamera - " + target);
    }

    private void initialize(Entity brokenEntity)
    {
        if (brokenEntity != Entity.Null)
            brokenEntities.Add(brokenEntity);
        targetQuery = em.CreateEntityQuery(new ComponentType[] { typeof(Cannon) });
        NativeArray<Entity> entities = targetQuery.ToEntityArray(Allocator.TempJob);
        for(int count = 0; count < entities.Length; count++)
        {
            if (brokenEntities.Contains(entities[count]))
                continue;
            trackTarget = entities[count];
            trackTargetIndex = count;
            Translation targetTrans = em.GetComponentData<Translation>(trackTarget);
            Rotation targetRot = em.GetComponentData<Rotation>(trackTarget);
            LocalToWorld targetLTW = em.GetComponentData<LocalToWorld>(trackTarget);
            if (float.IsNaN(targetTrans.Value.x))
                break;
            Vector3 startPosition = targetTrans.Value - targetLTW.Forward*FOLLOWDISTANCEZ;
            transform.position = startPosition;
            transform.LookAt(targetTrans.Value);
            initialized = true;
            //Debug.Log("Track target - " + trackTarget + " at " + targetTrans.Value);
        }
        //Debug.Log("FollowCamera - " + trackTarget);
        entities.Dispose();
    }

    void Start()
    {
        em = World.DefaultGameObjectInjectionWorld.EntityManager;
        brokenEntities = new List<Entity>();
        initialize(Entity.Null);
    }

    private void NextTarget(bool back)
    {
        NativeArray<Entity> entities = targetQuery.ToEntityArray(Allocator.TempJob);
        int newIndex = trackTargetIndex;
        if (back)
            newIndex -= 1;
        else
            newIndex++;
        if (newIndex < 0)
            newIndex = entities.Length - 1;
        else if (newIndex > entities.Length - 1)
            newIndex = 0;
        if(entities.Length > 0)
            trackTarget = entities[newIndex];
        trackTargetIndex = newIndex;
        entities.Dispose();
    }
    // Update is called once per frame
    void Update()
    {
        //trackTarget = CreatureBrowserMono.SelectedCreature;
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
            if (Input.GetKeyUp(KeyCode.A))
            {
                NextTarget(true);
            }
            else if (Input.GetKeyUp(KeyCode.D))
            {
                NextTarget(false);
            }
            NativeArray<Entity> allEntities = em.GetAllEntities(Allocator.TempJob);
            if(!allEntities.Contains(trackTarget))
            {
                Debug.Log("Entity not in array");
                NextTarget(false);
                allEntities.Dispose();
                return;
            }
            allEntities.Dispose();

            Translation targetTrans = new Translation
            {
                Value = float3.zero
            };

            try
            {
                targetTrans = em.GetComponentData<Translation>(trackTarget);
            }
            catch(ArgumentException ex)
            {
                NextTarget(false);
                return;
            }

            if (double.IsNaN(targetTrans.Value.x))
            {
                return;
            }
            Rotation targetRot = em.GetComponentData<Rotation>(trackTarget);
            LocalToWorld targetLTW = em.GetComponentData<LocalToWorld>(trackTarget);
            Target targetData = em.GetComponentData<Target>(trackTarget);
            transform.position = targetTrans.Value - targetLTW.Forward * FOLLOWDISTANCEZ + new float3(0,1,0)*FOLLOWDISTANCEY;

            if (targetData.Entity == Entity.Null || IgnoreTargets)
                transform.LookAt(targetTrans.Value);
            else
                transform.LookAt(targetData.Position);
        }
    }
}
