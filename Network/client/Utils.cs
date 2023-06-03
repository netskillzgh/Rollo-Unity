using System;

namespace Rollo.Client
{
    public static class Utils
    {
        public static void IntToBytesBigEndianNonAlloc(int value, byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length < 4)
            {
                throw new ArgumentException("The byte array must have a length of at least 4.", nameof(bytes));
            }

            bytes[0] = (byte)(value >> 24);
            bytes[1] = (byte)(value >> 16);
            bytes[2] = (byte)(value >> 8);
            bytes[3] = (byte)value;
        }

        public static int BytesToIntBigEndian(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length < 4)
            {
                throw new ArgumentException("The byte array must have a length of at least 4.", nameof(bytes));
            }

            return
                (bytes[0] << 24) |
                (bytes[1] << 16) |
                (bytes[2] << 8) |
                bytes[3];
        }

        public static void CommandToBytesBigEndianNonAlloc(int value, byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length < 2)
            {
                throw new ArgumentException("The byte array must have a length of at least 2.", nameof(bytes));
            }

            bytes[0] = (byte)(value >> 8);
            bytes[1] = (byte)value;
        }

        public static int BytesToIntBigEndianCommand(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            if (bytes.Length < 2)
            {
                throw new ArgumentException("The byte array must have a length of at least 2.", nameof(bytes));
            }

            return
                (bytes[0] << 8) |
                bytes[1];
        }

        static Byte[] bytesX = new byte[8];

        public static Byte[] UintToBigEndianX(long value)
        {
            bytesX[0] = (byte)(value >> 56);
            bytesX[1] = (byte)(value >> 48);
            bytesX[2] = (byte)(value >> 40);
            bytesX[3] = (byte)(value >> 32);
            bytesX[4] = (byte)(value >> 24);
            bytesX[5] = (byte)(value >> 16);
            bytesX[6] = (byte)(value >> 8);
            bytesX[7] = (byte)value;

            return bytesX;
        }
    }
}