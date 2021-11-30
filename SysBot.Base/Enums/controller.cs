using System;
using System.Collections.Generic;
using System.Text;

namespace SysBot.Base
{
   public enum controller
    {
        HidDeviceType_JoyRight1 = 1,   ///< ::HidDeviceTypeBits_JoyRight
        HidDeviceType_JoyLeft2 = 2,   ///< ::HidDeviceTypeBits_JoyLeft
        HidDeviceType_FullKey3 = 3,   ///< ::HidDeviceTypeBits_FullKey
        HidDeviceType_JoyLeft4 = 4,   ///< ::HidDeviceTypeBits_JoyLeft
        HidDeviceType_JoyRight5 = 5,   ///< ::HidDeviceTypeBits_JoyRight
        HidDeviceType_FullKey6 = 6,   ///< ::HidDeviceTypeBits_FullKey
        HidDeviceType_LarkHvcLeft = 7,   ///< ::HidDeviceTypeBits_LarkHvcLeft, ::HidDeviceTypeBits_HandheldLarkHvcLeft
        HidDeviceType_LarkHvcRight = 8,   ///< ::HidDeviceTypeBits_LarkHvcRight, ::HidDeviceTypeBits_HandheldLarkHvcRight
        HidDeviceType_LarkNesLeft = 9,   ///< ::HidDeviceTypeBits_LarkNesLeft, ::HidDeviceTypeBits_HandheldLarkNesLeft
        HidDeviceType_LarkNesRight = 10,  ///< ::HidDeviceTypeBits_LarkNesRight, ::HidDeviceTypeBits_HandheldLarkNesRight
        HidDeviceType_Lucia = 11,  ///< ::HidDeviceTypeBits_Lucia
        HidDeviceType_Palma = 12,  ///< [9.0.0+] ::HidDeviceTypeBits_Palma
        HidDeviceType_FullKey13 = 13,  ///< ::HidDeviceTypeBits_FullKey
        HidDeviceType_FullKey15 = 15,  ///< ::HidDeviceTypeBits_FullKey
        HidDeviceType_DebugPad = 17,  ///< ::HidDeviceTypeBits_DebugPad
        HidDeviceType_System19 = 19,  ///< ::HidDeviceTypeBits_System with \ref HidNpadStyleTag |= ::HidNpadStyleTag_NpadFullKey.
        HidDeviceType_System20 = 20,  ///< ::HidDeviceTypeBits_System with \ref HidNpadStyleTag |= ::HidNpadStyleTag_NpadJoyDual.
        HidDeviceType_System21 = 21,  ///< ::HidDeviceTypeBits_System with \ref HidNpadStyleTag |= ::HidNpadStyleTag_NpadJoyDual.
        HidDeviceType_Lagon = 22,  ///< ::HidDeviceTypeBits_Lagon
        HidDeviceType_Lager = 28,  ///< ::HidDeviceTypeBits_Lager
    }

}
