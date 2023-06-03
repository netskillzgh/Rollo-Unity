// Add your commands here
public enum OpCodeList
{
    // reserved for internal use
    HeartBeat = 0,
    //------//
    Hello
}

class OpCode
{
    // GetOpCodeEnum, cast int to OpCodeList
    public static OpCodeList GetOpCodeEnum(int cmd)
    {
        return (OpCodeList)cmd;
    }

    // Get op code from enum
    public static int GetOpCodeNumber(OpCodeList opCode)
    {
        return (int)opCode;
    }
}