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
        public float SustainHeight;
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
        struct FlightJob : IJobForEach<Flight, PhysicsVelocity,Translation,Target>
        {
            public float delta;

            public void Execute(ref Flight flight, ref PhysicsVelocity velocity,
                ref Translation trans, ref Target target)
            {
                //UnityEngine.Debug.Log("Since last update - " + flight.SinceLastUpdate);
                flight.SinceLastUpdate += delta;
                if (flight.SinceLastUpdate >= flight.UpdateEvery)
                {
                    float3 newVelocity = velocity.Linear;
                    if (trans.Value.y < flight.SustainHeight)
                    {
                        newVelocity.y += flight.Acceleration.y * delta;
                    }
                    float3 direction = math.normalize(target.Position - trans.Value);
                    newVelocity.z += direction.z * flight.Acceleration.z*delta;
                    newVelocity.x += direction.x * flight.Acceleration.x * delta;
                    newVelocity.y += direction.y * flight.Acceleration.y * delta;
                    velocity.Linear = newVelocity;
                    flight.SinceLastUpdate = 0;
                }
            }
        }
    }

}

