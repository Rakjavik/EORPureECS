using Unity.Burst;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace rak.ecs.Systems
{

    public struct BlinkMovement : IComponentData
    {
        public float CoolDown;
        public float CurrentCoolDown;
        public float JumpDistance;
    }

    public class BlinkSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            BlinkJob job = new BlinkJob
            {
                delta = UnityEngine.Time.deltaTime
            };
            return job.Schedule(this, inputDeps);
        }

        [BurstCompile]
        struct BlinkJob : IJobForEach<Target, BlinkMovement,Translation,CreatureAI>
        {
            public float delta;

            public void Execute(ref Target target, ref BlinkMovement bm, ref Translation trans, ref CreatureAI cai)
            {
                if (bm.CurrentCoolDown > 0)
                    bm.CurrentCoolDown -= delta;
                else
                {
                    // No valid target, exit job //
                    if (cai.CurrentAction != CreatureActionType.Move || target.Position.Equals(float3.zero)) return;
                    float distance = math.distance(trans.Value, target.Position);
                    if(distance > cai.DistanceToInteract)
                    {
                        float3 newPosition = UnityEngine.Vector3.MoveTowards(trans.Value, target.Position, bm.JumpDistance);
                        trans.Value = newPosition;
                        bm.CurrentCoolDown = bm.CoolDown;
                    }
                }
            }
        }
    }
}
