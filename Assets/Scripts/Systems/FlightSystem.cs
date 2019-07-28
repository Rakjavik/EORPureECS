using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public struct Flight : IComponentData
    {
        public float3 Acceleration;
        public float SinceLastUpdate;
        public float UpdateEvery;
    }

    public class FlightSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            FlightJob job = new FlightJob
            {
                delta = UnityEngine.Time.deltaTime,
            };
            return job.Schedule(this, inputDeps);
        }

        [BurstCompile]
        struct FlightJob : IJobForEach<Flight, PhysicsVelocity,Translation>
        {
            public float delta;

            public void Execute(ref Flight flight, ref PhysicsVelocity velocity,
                ref Translation trans)
            {
                //UnityEngine.Debug.Log("Since last update - " + flight.SinceLastUpdate);
                flight.SinceLastUpdate += delta;
                if (flight.SinceLastUpdate >= flight.UpdateEvery)
                {
                    float3 newVelocity = velocity.Linear;
                    if (trans.Value.y < 10)
                    {
                        newVelocity.y += flight.Acceleration.y * delta;
                    }
                    
                    velocity.Linear = newVelocity;
                    flight.SinceLastUpdate = 0;
                }
            }
        }
    }

}

