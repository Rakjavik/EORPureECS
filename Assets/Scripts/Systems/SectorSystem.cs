using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace rak.ecs.Systems
{
    public class SectorManager
    {
        public struct Sector
        {
            public float2x2 Bounds;
            public int Index;
        }

        private bool initialized = false;
        public float2x2 WorldBounds;
        public float2 WorldSize;
        

        public void Initialize()
        {
            if(!initialized)
            {
                initialized = true;
                WorldBounds = new float2x2
                {
                    c0 = new float2(-128, -128),
                    c1 = new float2(128, 128)
                };
                WorldSize = new float2(256, 256);
                int numOfSectors = 4;
                Sector[] sectors = new Sector[numOfSectors];
                int xRows = numOfSectors / 2;
                int yColumns = numOfSectors / 2;
                float sectorSizeX = WorldSize.x / xRows;
                float sectorSizeY = WorldSize.y / yColumns;

                int currentSector = 0;

                for (int x = 0; x < xRows; x++)
                {
                    for(int y = 0; y < yColumns; y++)
                    {
                        float startX = WorldBounds.c0.x + (x * sectorSizeX);
                        float startY = WorldBounds.c0.y + (y * sectorSizeY);
                        float endX = startX + sectorSizeX;
                        float endY = startY + sectorSizeY;
                        sectors[currentSector] = new Sector
                        {
                            Bounds = new float2x2
                            {
                                c0 = new float2(startX, startY),
                                c1 = new float2(endX, endY)
                            },
                            Index = currentSector
                        };

                        currentSector++;
                    }
                }
            }
        }
    }

}
