namespace VoxaNovus.Configuration
{
    public class ControlsDTO
    {
        public string Forward { get; set; }
        public string Backward { get; set; }
        public string Left { get; set; }
        public string Right { get; set; }
        public string Jump { get; set; }
        public bool InvertMouseX { get; set; }
        public bool InvertMouseY { get; set; }
        public float MouseSensitivity { get; set; }
        public string BlockPlacement { get; set; }
        public string BlockDestruction { get; set; }
        public string PickBlock { get; set; }
        public string PlaceSmoothBlocks { get; set; }
        public string InteractNPC { get; set; }
        public string InteractBlock { get; set; }
        public string Inventory { get; set; }
        public string DropItem { get; set; }
    }
}