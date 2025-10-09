using System;
using System.Text;

namespace Obfuscar
{
    public static class TeaEncryptor
    {
        public static uint[] Key;

        private const uint Delta = 0x9E3779B9;
        private const int Rounds = 32;

        static TeaEncryptor()
        {
            InitKey();
        }

        public static void InitKey()
        {
            Key = new uint[] { 0xA56BABCD, 0x0000FFFF, 0xABCDEF01, 0x12345678 }; // Placeholder, will be patched later.
        }

        /// <summary>
        /// Encrypts arbitrary data with TEA/XTEA and PKCS7-style padding.
        /// </summary>
        public static byte[] Encrypt(byte[] data)
        {
            // Add 4 bytes for length + pad to multiple of 8
            int totalLength = data.Length + 4;
            int paddedLength = NextMultipleOf8(totalLength);

            byte[] buffer = new byte[paddedLength];

            // Write original length
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            Array.Copy(lengthBytes, buffer, 4);
            Array.Copy(data, 0, buffer, 4, data.Length);

            // Encrypt each 8-byte block
            for (int i = 0; i < buffer.Length; i += 8)
            {
                uint v0 = BitConverter.ToUInt32(buffer, i);
                uint v1 = BitConverter.ToUInt32(buffer, i + 4);
                EncryptBlock(v0, v1, out v0, out v1);
                Array.Copy(BitConverter.GetBytes(v0), 0, buffer, i, 4);
                Array.Copy(BitConverter.GetBytes(v1), 0, buffer, i + 4, 4);
            }

            return buffer;
        }

        /// <summary>
        /// Decrypts TEA/XTEA data and removes padding.
        /// </summary>
        public static byte[] Decrypt(byte[] encryptedData)
        {
            if (encryptedData.Length % 8 != 0)
                throw new ArgumentException("Encrypted data length must be multiple of 8 bytes.");

            byte[] buffer = new byte[encryptedData.Length];
            Array.Copy(encryptedData, buffer, encryptedData.Length);

            for (int i = 0; i < buffer.Length; i += 8)
            {
                uint v0 = BitConverter.ToUInt32(buffer, i);
                uint v1 = BitConverter.ToUInt32(buffer, i + 4);
                DecryptBlock(v0, v1, out v0, out v1);
                Array.Copy(BitConverter.GetBytes(v0), 0, buffer, i, 4);
                Array.Copy(BitConverter.GetBytes(v1), 0, buffer, i + 4, 4);
            }

            // Read original length
            int originalLength = BitConverter.ToInt32(buffer, 0);
            if (originalLength > buffer.Length - 4)
                throw new ArgumentException("Invalid encrypted data length.");

            byte[] result = new byte[originalLength];
            Array.Copy(buffer, 4, result, 0, originalLength);
            return result;
        }

        private static int NextMultipleOf8(int length)
        {
            return (length + 7) / 8 * 8;
        }

        public static int GetEncryptedMessageSize(int messageSize)
        {
            return NextMultipleOf8(messageSize + 4);
        }

        #region Block Encryption
        private static void EncryptBlock(uint v0, uint v1, out uint outV0, out uint outV1)
        {
            uint sum = 0;

            for (uint i = 0; i < Rounds; i++)
            {
                v0 += (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + Key[sum & 3]);
                sum += Delta;
                v1 += (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + Key[(sum >> 11) & 3]);
            }

            outV0 = v0;
            outV1 = v1;
        }

        private static void DecryptBlock(uint v0, uint v1, out uint outV0, out uint outV1)
        {
            uint sum = unchecked(Delta * Rounds);

            for (uint i = 0; i < Rounds; i++)
            {
                v1 -= (((v0 << 4) ^ (v0 >> 5)) + v0) ^ (sum + Key[(sum >> 11) & 3]);
                sum -= Delta;
                v0 -= (((v1 << 4) ^ (v1 >> 5)) + v1) ^ (sum + Key[sum & 3]);
            }

            outV0 = v0;
            outV1 = v1;
        }
        #endregion
    }
}

