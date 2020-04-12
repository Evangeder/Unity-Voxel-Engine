namespace VoxaNovus.Configuration
{
    public class GraphicsDTO
    {
        public string Resolution { get; set; }
        public int FrameLimiter { get; set; }
        public int RefreshRate { get; set; }
        public int InterfaceSize { get; set; }
        public bool ScaleInterface { get; set; }
        public float FullScreenGamma { get; set; }
        public byte Animation { get; set; }
        public byte AntiAliasing { get; set; }
        public byte Environment { get; set; }
        public byte DistanceLOD { get; set; }
        public byte RenderSampling { get; set; }
        public byte Shadows { get; set; }
        public byte Shaders { get; set; }
        public byte PostProcessing { get; set; }
        public bool AmbientOcclusion { get; set; }
        public bool TextureFiltering { get; set; }
        public bool DepthBlur { get; set; }
        public bool VerticalSynch { get; set; }
        public int RenderDistance { get; set; }
    }
}
