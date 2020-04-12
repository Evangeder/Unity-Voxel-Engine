using System;

namespace VoxaNovus
{
    [Serializable]
    public struct BlockMetadata
    {
        public ushort ID { get; set; }
        public byte MarchedValue { get; set; }
        public BlockSwitches Switches { get; set; }

        public BlockMetadata(ushort ID, BlockSwitches switches, byte MarchedValue = 0)
        {
            this.ID = ID;
            Switches = switches;
            this.MarchedValue = MarchedValue;
        }

        /// <summary>
        /// Old version that is using float value, this gets converted to byte
        /// </summary>
        [Obsolete]
        public BlockMetadata(ushort ID, BlockSwitches switches, float MarchedValue = 0f)
        {
            this.ID = ID;
            Switches = switches;
            this.MarchedValue = (byte)(MarchedValue * 255f);
        }

        public BlockMetadata(BlockMetadata T)
        {
            this = T;
        }

        #region Methods

        public void SetMarchedValue(float f)
        {
            if (f >= 1f)
                MarchedValue = 254;
            else if (f <= 0f)
                MarchedValue = 0;
            else
                MarchedValue = (byte)(f * 255f);
        }

        public float GetMarchedValue()
        {
            return (float)(MarchedValue / 255f);
        }

        #endregion
        #region Operator overrides

        public static bool operator ==(BlockMetadata operand1, BlockMetadata operand2)
        {
            return
                operand1.ID == operand2.ID
                && operand1.Switches == operand2.Switches
                && operand1.MarchedValue == operand2.MarchedValue;
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() == typeof(BlockMetadata))
                return this == (BlockMetadata)obj;
            else
                return false;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        public static bool operator !=(BlockMetadata operand1, BlockMetadata operand2)
        {
            return
                !(operand1.ID == operand2.ID
                && operand1.Switches == operand2.Switches
                && operand1.MarchedValue == operand2.MarchedValue);
        }

        public static BlockMetadata EmptyPhysicsTrigger()
        {
            return new BlockMetadata
            {
                ID = 0,
                MarchedValue = 0,
                Switches = BlockSwitches.PhysicsTrigger
            };
        }

        #endregion
    }
}