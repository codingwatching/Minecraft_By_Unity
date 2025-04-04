using UnityEngine;
using NoiseMapTest;
using UnityEngine.ProBuilder;

namespace NoiseMapTest
{

    public static class GenNoise 
    {

        public enum NormalizeMode
        {
            Local, Global
        };

        /// <summary>
        /// 生成噪声图
        /// </summary>
        /// <param name="chunkCoord">区块坐标</param>
        /// <param name="mapWidth"></param>
        /// <param name="mapHeight"></param>
        /// <param name="scale"></param>
        /// <param name="octaves">八度音阶的数量</param>
        /// <param name="persistance">持久性</param>
        /// <param name="lacunarity">间隙度</param>
        /// <returns></returns>
        public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, int seed, float scale, int octaves, float persistance, float lacunarity, Vector2 offset, NormalizeMode normalizeMode)
        {
            //设定种子
            System.Random prng = new System.Random(seed);

            //八度音阶偏移
            Vector2[] octaveOffsets = new Vector2[octaves];

            float maxPossibleHeight = 0f;
            float amplitude = 1; //振幅
            float frequency = 1; //频率

            for (int i = 0; i < octaves; i++)
            {
                float offsetX = prng.Next(-100000, 100000) + offset.x;
                float offsetY = prng.Next(-100000, 100000) + offset.y;
                octaveOffsets[i] = new Vector2(offsetX, offsetY);

                maxPossibleHeight += amplitude;
                amplitude += persistance;
            }

            //噪声数组
            float[,] noiseMap = new float[mapWidth, mapHeight];

            //特殊情况-scale为0的情况
            if (scale <= 0)
                scale = 0.0001f;

            //找到最大和最低噪声值
            float maxLocalNoiseHeight = float.MinValue;
            float minLocalNoiseHeight = float.MaxValue;

            float halfWidth = mapWidth / 2;
            float halfHeight = mapHeight / 2;

            //遍历二维数组,创建噪声值
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {

                    //分形参数
                    amplitude = 1; //振幅
                    frequency = 1; //频率
                    float noiseHeight = 0; //跟踪当前的高度值

                    //遍历所有八度音阶
                    for (int i = 0; i < octaves; i++)
                    {
                        //采样点                
                        float sampleX = (x - halfWidth + octaveOffsets[i].x) / scale * frequency;  //频率越高，采样点之间的距离越远
                        float sampleY = (y - halfHeight + octaveOffsets[i].y) / scale * frequency;

                        //获取PerlinNoise
                        float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1; //将范围保持在[-1, 1]

                        //计算总贡献值
                        // noiseHeight = 0.6 * <1> + 0.4 * <0.5> + 0.2 * <0.25> = 0.85。每一层的贡献值由振幅决定
                        noiseHeight += perlinValue * amplitude;

                        //调整下一层噪声的振幅和频率
                        amplitude *= persistance; //逐渐减小每一层的振幅，控制噪声图的整体平滑程度和细节保留。
                        frequency *= lacunarity; //逐渐增加每一层的频率，控制噪声图的细节密度，增加高频噪声的影响。
                    }

                    //寻找最大最小噪声值
                    if (noiseHeight > maxLocalNoiseHeight)
                        maxLocalNoiseHeight = noiseHeight;
                    else if (noiseHeight < minLocalNoiseHeight)
                        minLocalNoiseHeight = noiseHeight;

                    //赋值
                    noiseMap[x, y] = noiseHeight;

                }
            }

            //归一化处理
            //值会超过01范围
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    if (normalizeMode == NormalizeMode.Local)
                    {
                        noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                    }
                    else if (normalizeMode == NormalizeMode.Global)
                    {
                        float normalizeHeight = (noiseMap[x,y] + 1) / (2f * maxPossibleHeight / 16f);
                        noiseMap[x, y] = normalizeHeight;
                    }

                    
                }
            }

            //返回
            return noiseMap;
        }


        /// <summary>
        /// 生成 Voronoi 图，保证 Chunk 之间连续
        /// </summary>
        /// <param name="chunkCoord">当前 Chunk 坐标</param>
        /// <param name="mapWidth">地图宽度</param>
        /// <param name="mapHeight">地图高度</param>
        /// <param name="seed">随机种子</param>
        /// <param name="pointCount">Voronoi 细胞的种子点数量</param>
        /// <param name="offset">偏移量，用于保持 Chunk 之间衔接自然</param>
        /// <returns>返回生成的 Voronoi 贴图数据</returns>
        public static float[,] GenerateVoronoiMap(Vector2Int chunkCoord, int mapWidth, int mapHeight, int seed, int pointCount, Vector2 offset)
        {
            // 设定随机种子
            System.Random prng = new System.Random(seed);

            // 计算当前 Chunk 在全局坐标系中的偏移
            Vector2 globalOffset = new Vector2(chunkCoord.x * mapWidth, chunkCoord.y * mapHeight) + offset;

            // 生成 Voronoi 细胞的种子点（基于全局坐标）
            Vector2[] points = new Vector2[pointCount];
            for (int i = 0; i < pointCount; i++)
            {
                float pointX = prng.Next(-100000, 100000);
                float pointY = prng.Next(-100000, 100000);
                points[i] = new Vector2(pointX, pointY);
            }

            // 创建 Voronoi 图数据
            float[,] voronoiMap = new float[mapWidth, mapHeight];

            // 遍历每个像素，计算最近的种子点
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // 计算当前像素的全局坐标
                    Vector2 samplePos = new Vector2(x, y) + globalOffset;

                    int closestPointIndex = 0;
                    float minDistance = float.MaxValue;

                    // 找到离当前像素最近的 Voronoi 细胞种子
                    for (int i = 0; i < pointCount; i++)
                    {
                        float dist = Vector2.SqrMagnitude(samplePos - points[i]); // 计算欧几里得距离平方
                        if (dist < minDistance)
                        {
                            minDistance = dist;
                            closestPointIndex = i;
                        }
                    }

                    // 归一化处理
                    voronoiMap[x, y] = closestPointIndex / (float)pointCount;
                }
            }

            return voronoiMap;
        }





    }

}
