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

        // Encrypts a 64-bit block (8 bytes)
        public static byte[] EncryptBlock(byte[] data)
        {
            if (data.Length != 8) throw new ArgumentException("Block size must be 8 bytes");

            uint v0 = BitConverter.ToUInt32(data, 0);
            uint v1 = BitConverter.ToUInt32(data, 4);
            uint sum = 0;

            for (int i = 0; i < Rounds; i++)
            {
                sum += Delta;
                v0 += ((v1 << 4) + Key[0]) ^ (v1 + sum) ^ ((v1 >> 5) + Key[1]);
                v1 += ((v0 << 4) + Key[2]) ^ (v0 + sum) ^ ((v0 >> 5) + Key[3]);
            }

            byte[] encrypted = new byte[8];
            Array.Copy(BitConverter.GetBytes(v0), 0, encrypted, 0, 4);
            Array.Copy(BitConverter.GetBytes(v1), 0, encrypted, 4, 4);
            return encrypted;
        }

        // Decrypts a 64-bit block (8 bytes)
        public static byte[] DecryptBlock(byte[] data)
        {
            if (data.Length != 8) throw new ArgumentException("Block size must be 8 bytes");

            uint v0 = BitConverter.ToUInt32(data, 0);
            uint v1 = BitConverter.ToUInt32(data, 4);
            uint sum = unchecked(Delta * (uint)Rounds);

            for (int i = 0; i < Rounds; i++)
            {
                v1 -= ((v0 << 4) + Key[2]) ^ (v0 + sum) ^ ((v0 >> 5) + Key[3]);
                v0 -= ((v1 << 4) + Key[0]) ^ (v1 + sum) ^ ((v1 >> 5) + Key[1]);
                sum -= Delta;
            }

            byte[] decrypted = new byte[8];
            Array.Copy(BitConverter.GetBytes(v0), 0, decrypted, 0, 4);
            Array.Copy(BitConverter.GetBytes(v1), 0, decrypted, 4, 4);
            return decrypted;
        }

        // Encrypt string using TEA block encryption
        public static byte[] EncryptString(string input)
        {
            byte[] data = PadTo8Bytes(Encoding.UTF8.GetBytes(input));
            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i += 8)
            {
                byte[] block = new byte[8];
                Array.Copy(data, i, block, 0, 8);
                byte[] encrypted = EncryptBlock(block);
                Array.Copy(encrypted, 0, result, i, 8);
            }

            return result;
        }

        public static byte[] Encrypt(byte[] input)
        {
            byte[] data = PadTo8Bytes(input);
            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i += 8)
            {
                byte[] block = new byte[8];
                Array.Copy(data, i, block, 0, 8);
                byte[] encrypted = EncryptBlock(block);
                Array.Copy(encrypted, 0, result, i, 8);
            }

            return result;
        }

        public static byte[] Decrypt(byte[] input)
        {
            byte[] data = input;
            byte[] result = new byte[data.Length];

            for (int i = 0; i < data.Length; i += 8)
            {
                byte[] block = new byte[8];
                Array.Copy(data, i, block, 0, 8);
                byte[] decrypted = DecryptBlock(block);
                Array.Copy(decrypted, 0, result, i, 8);
            }

            return TrimPadding(result);
        }

        public static byte[] PadTo8Bytes(byte[] data)
        {
            int padding = 8 - (data.Length % 8);
            byte[] padded = new byte[data.Length + padding];
            Array.Copy(data, padded, data.Length);
            padded[padded.Length - 1] = (byte)padding; // store padding length in last byte
            return padded;
        }

        public static byte[] TrimPadding(byte[] data)
        {
            int padding = data[data.Length - 1];
            byte[] result = new byte[data.Length - padding];
            Array.Copy(data, 0, result, 0, result.Length);
            return result;
        }
    }
}
