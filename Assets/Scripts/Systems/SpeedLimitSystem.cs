using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;

namespace rak.ecs.Systems
{
    public struct SpeedLimit : IComponentData
    {
        public float3 Linear;
        public float3 Angular;
        public float BrakeStrength;
    }

    public class SpeedLimitSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SpeedLimitJob job = new SpeedLimitJob();
            return job.Schedule(this, inputDeps);
        }
    }

    public struct SpeedLimitJob : IJobForEach<PhysicsVelocity,SpeedLimit>
    {
        public void Execute(ref PhysicsVelocity vel, ref SpeedLimit sl)
        {
            float brake = sl.BrakeStrength;
            float3 newVelocity = vel.Linear;
            if (vel.Linear.x > sl.Linear.x || vel.Linear.x < -sl.Linear.x)
            {
                newVelocity.x *= brake;
            }
            if (vel.Linear.y > sl.Linear.y || vel.Linear.y < -sl.Linear.y)
            {
                newVelocity.y *= brake;
            }
            if (vel.Linear.z > sl.Linear.z || vel.Linear.z < -sl.Linear.z)
            {
                newVelocity.z *= brake;
            }
            vel.Linear = newVelocity;
            
            newVelocity = vel.Linear;
            if (vel.Angular.x > sl.Angular.x || vel.Angular.x < -sl.Angular.x)
            {
                newVelocity.x *= brake;
            }
            if (vel.Angular.y > sl.Angular.y || vel.Angular.y < -sl.Angular.y)
            {
                newVelocity.y *= brake;
            }
            if (vel.Angular.z > sl.Angular.z || vel.Angular.z < -sl.Angular.z)
            {
                newVelocity.z *= brake;
            }
            vel.Angular = newVelocity;
        }
    }
}
