// 该文件由Cursor 自动生成
namespace GameLogic
{
    /// <summary>
    /// Quality-tier-dependent shadow configuration.
    /// Values come from Luban TbRenderConfig in production;
    /// this struct is the runtime representation.
    /// </summary>
    public struct ShadowRTConfig
    {
        public int Resolution;
        public int CascadeCount;
        public bool SoftShadows;
        public float ShadowDistance;
        public float DepthBias;
        public float NormalBias;

        public static ShadowRTConfig High => new ShadowRTConfig
        {
            Resolution = 2048,
            CascadeCount = 4,
            SoftShadows = true,
            ShadowDistance = 8f,
            DepthBias = 0.5f,
            NormalBias = 0.3f
        };

        public static ShadowRTConfig Medium => new ShadowRTConfig
        {
            Resolution = 1024,
            CascadeCount = 2,
            SoftShadows = true,
            ShadowDistance = 8f,
            DepthBias = 0.5f,
            NormalBias = 0.3f
        };

        public static ShadowRTConfig Low => new ShadowRTConfig
        {
            Resolution = 512,
            CascadeCount = 1,
            SoftShadows = false,
            ShadowDistance = 8f,
            DepthBias = 0.5f,
            NormalBias = 0.3f
        };
    }

    public enum QualityTier : byte
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
}
