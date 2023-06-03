using System.IO;
using System.Threading.Tasks;
using UnityEngine;

namespace Rollo.Client
{
    public static class NetworkStreamExtensions
    {
        private static async Task<int> ReadSafelyAsync(this Stream stream, byte[] buffer, int offset, int size)
        {
            Debug.Assert(stream != null);
            try
            {
                return await stream.ReadAsync(buffer, offset, size).ConfigureAwait(false);
            }
            catch (IOException)
            {
                return 0;
            }
        }

        public static async Task<bool> ReadExactlyAsync(this Stream stream, byte[] buffer, int amount)
        {
            Debug.Assert(stream != null);
            int bytesRead = 0;
            while (bytesRead < amount)
            {
                int remaining = amount - bytesRead;
                int result = await stream.ReadSafelyAsync(buffer, bytesRead, remaining).ConfigureAwait(false);

                if (result == 0)
                    return false;

                bytesRead += result;
            }

            return true;
        }
    }
}