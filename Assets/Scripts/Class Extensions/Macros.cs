using UnityEngine;

namespace Macros
{
    public static class Coroutine
    {
        public static WaitForSeconds WaitFor_1_Second = new WaitForSeconds(1);
        public static WaitForSeconds WaitFor_2_Seconds = new WaitForSeconds(2);
        public static WaitForSeconds WaitFor_3_Seconds = new WaitForSeconds(3);
        public static WaitForSeconds WaitFor_4_Seconds = new WaitForSeconds(4);
        public static WaitForSeconds WaitFor_5_Seconds = new WaitForSeconds(5);
        public static WaitForSeconds WaitFor_10_Seconds = new WaitForSeconds(10);

        public static WaitForFixedUpdate WaitFor_FixedUpdate = new WaitForFixedUpdate();
        public static WaitForEndOfFrame WaitFor_EndOfFrame = new WaitForEndOfFrame();
    }
}