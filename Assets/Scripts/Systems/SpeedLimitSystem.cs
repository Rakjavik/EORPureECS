using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public struct SpeedLimit : IComponentData
    {
        public float3 Linear;
        public float3 Angular;
        public float BrakeStrength;
        public float EngageWhenThisCloseToTarget;
        public float MinimumSpeed;
    }

    public class SpeedLimitSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            SpeedLimitJob job = new SpeedLimitJob();
            return job.Schedule(this, inputDeps);
        }
    }

    [BurstCompile]
    public struct SpeedLimitJob : IJobForEach<PhysicsVelocity,SpeedLimit,Translation,Target>
    {
        public void Execute(ref PhysicsVelocity vel, ref SpeedLimit sl,ref Translation trans, ref Target target)
        {
            float brake = sl.BrakeStrength;
            float3 newVelocity = vel.Linear;
            float distance = math.distance(trans.Value, target.Position);
            if (distance <= sl.EngageWhenThisCloseToTarget)
            {
                if(vel.Linear.x > sl.MinimumSpeed)
                    newVelocity.x *= brake;
                if(vel.Linear.z > sl.MinimumSpeed)
                    newVelocity.z *= brake;
            }
            else
            {
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
                
            }   
            vel.Linear = newVelocity;
            float3 newAngular = vel.Angular;
            if (vel.Angular.x > sl.Angular.x || vel.Angular.x < -sl.Angular.x)
            {
                newAngular.x *= brake;
            }
            if (vel.Angular.y > sl.Angular.y || vel.Angular.y < -sl.Angular.y)
            {
                newAngular.y *= brake;
            }
            if (vel.Angular.z > sl.Angular.z || vel.Angular.z < -sl.Angular.z)
            {
                newAngular.z *= brake;
            }
            
            vel.Angular = newAngular;
        }
    }
}
