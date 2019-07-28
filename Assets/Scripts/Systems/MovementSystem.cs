using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

namespace rak.ecs.Systems
{

    public struct Movement : IComponentData
    {
        public float speed;
        public float3 direction;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public class MovementSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            MovementJob job = new MovementJob
            {
                delta = Time.deltaTime
            };

            return job.Schedule(this,inputDeps);
        }

        struct MovementJob : IJobForEach<Movement,Translation>
        {
            public float delta;

            public void Execute(ref Movement move, ref Translation trans)
            {
                if (!move.direction.Equals(float3.zero))
                {
                    float3 currentPosition = trans.Value;
                    currentPosition = currentPosition + (move.direction * (move.speed * delta));
                    trans.Value = currentPosition;
                }
            }
        }
    }
}
