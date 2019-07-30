using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public enum ScaleChangeAxis { X,Y,Z }

    public struct ScaleChange : IComponentData
    {
        public ScaleChangeAxis Axis;
        public float Speed;
        public float2 MinMax;
        public byte Shrinking;
        public float SpeedRatioWhenShrinking;
    }

    public class ScaleChangeSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            ScaleChangeJob job = new ScaleChangeJob
            {
                delta = UnityEngine.Time.deltaTime
            };
            return job.Schedule(this, inputDeps);
        }

        struct ScaleChangeJob : IJobForEach<NonUniformScale,ScaleChange>
        {
            public float delta;

            public void Execute(ref NonUniformScale nus,ref ScaleChange sc)
            {
                float3 scale = nus.Value;
                float increment;
                float speed = sc.Speed;
                if (sc.Shrinking == 0)
                    increment = speed * delta;
                else
                    increment = -(speed * delta * sc.SpeedRatioWhenShrinking);

                if (sc.Axis == ScaleChangeAxis.X)
                    scale.x += increment;
                else if (sc.Axis == ScaleChangeAxis.Y)
                    scale.y += increment;
                else
                    scale.z += increment;
                nus.Value = scale;

                if(sc.Axis == ScaleChangeAxis.X)
                {
                    if (sc.Shrinking == 0 && scale.x > sc.MinMax.y)
                        sc.Shrinking = 1;
                    else if (sc.Shrinking == 1 && scale.x < sc.MinMax.x)
                        sc.Shrinking = 0;
                }
                else if (sc.Axis == ScaleChangeAxis.Y)
                {
                    if (sc.Shrinking == 0 && scale.y > sc.MinMax.y)
                        sc.Shrinking = 1;
                    else if (sc.Shrinking == 1 && scale.y < sc.MinMax.x)
                        sc.Shrinking = 0;
                }
                else
                {
                    if (sc.Shrinking == 0 && scale.z > sc.MinMax.y)
                        sc.Shrinking = 1;
                    else if (sc.Shrinking == 1 && scale.z < sc.MinMax.x)
                        sc.Shrinking = 0;
                }
            }
        }
    }
}
