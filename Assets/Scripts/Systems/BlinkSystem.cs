using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public struct BlinkBubble : IComponentData
    {
        public float ChargeTime;
        public float CurrentCharge;
        public Entity Parent;
    }
    [UpdateAfter(typeof(BlinkSystem)),UpdateAfter(typeof(FlightSystem))]
    public class BlinkBubbleSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            BlinkBubbleJob job = new BlinkBubbleJob
            {
                positions = GetComponentDataFromEntity<Translation>(false),
                blinks = GetComponentDataFromEntity<BlinkMovement>(true)
            };
            return job.Schedule(this, inputDeps);
        }

        struct BlinkBubbleJob : IJobForEachWithEntity<BlinkBubble,NonUniformScale>
        {
            [NativeDisableParallelForRestriction]
            public ComponentDataFromEntity<Translation> positions;

            [ReadOnly]
            public ComponentDataFromEntity<BlinkMovement> blinks;

            public void Execute(Entity entity, int index, ref BlinkBubble bb, ref NonUniformScale nus)
            {
                positions[entity] = positions[bb.Parent];
                nus.Value = new float3(2, 2, 2) *
                    (blinks[bb.Parent].CoolDown - blinks[bb.Parent].CurrentCoolDown)/blinks[bb.Parent].CoolDown;
            }
        }
    }

    public struct BlinkMovement : IComponentData
    {
        public float CoolDown;
        public float CurrentCoolDown;
        public float JumpDistance;
        public Entity BubbleEntity;
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
                    if (distance > cai.DistanceToInteract)
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
