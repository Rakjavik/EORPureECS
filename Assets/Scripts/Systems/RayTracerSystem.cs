using System.Collections.Generic;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace rak.ecs.Systems
{
    public struct RayTracer : IComponentData
    {
        public float3 Up;
        public float3 Right;
        public float RayLength;
        public float3 Origin;
        public CollisionFilter CollisionFilter;
    }
    public struct RayCastResult
    {
        public float3 HitLocation;
        public float Distance;
    }

    [UpdateAfter(typeof(BuildPhysicsWorld))]
    public class RayTracerSystem : JobComponentSystem
    {
        public BuildPhysicsWorld bpw;

        private List<RayTracer> requests;
        private List<RayCastResult> results;

        protected override void OnCreate()
        {
            Enabled = false;
            requests = new List<RayTracer>();
            results = new List<RayCastResult>();
            bpw = World.GetOrCreateSystem<BuildPhysicsWorld>();
        }
        public int AddRayTraceRequest(RayTracer request)
        {
            requests.Add(request);
            return requests.Count - 1;
        }

        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            for (int count = 0; count < requests.Count; count++) {
                RayTracerJob job = new RayTracerJob
                {
                    CollisionWorld = bpw.PhysicsWorld.CollisionWorld,
                    CollisionFilter = requests[count].CollisionFilter,
                    RayLength = requests[count].RayLength,
                    Right = requests[count].Right,
                    Up = requests[count].Up,
                    Origin = requests[count].Origin
                };
                job.Schedule(requests.Count, count, inputDeps).Complete();
                results.Add(job.Result);
            }
            return inputDeps;
        }

        struct RayTracerJob : IJobParallelFor
        {
            public CollisionWorld CollisionWorld;
            public float3 Up;
            public float3 Right;
            public float RayLength;
            public CollisionFilter CollisionFilter;
            public float3 Origin;
            public RayCastResult Result;

            public void Execute(int index)
            {
                RaycastInput input = new RaycastInput
                {
                    Start = Origin,
                    End = Origin + (-Up * RayLength),
                    Filter = CollisionFilter
                };
                RaycastHit result;
                bool hasHit = CollisionWorld.CastRay(input, out result);
                if(hasHit)
                {
                    Result = new RayCastResult
                    {
                        HitLocation = result.Position,
                        Distance = UnityEngine.Vector3.Distance(Origin, result.Position),
                    };
                }
            }
        }
    }
}

