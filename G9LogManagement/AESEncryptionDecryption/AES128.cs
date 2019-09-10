using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace G9LogManagement.AESEncryptionDecryption
{
    /// <summary>
    ///     Encryption Decryption AES 128
    /// </summary>
    internal static class AES128
    {
        private static readonly Aes CustomAes;

        /// <summary>
        ///     Constructor
        /// </summary>

        #region AES128

        static AES128()
        {
            CustomAes = Aes.Create();
            CustomAes.KeySize = 128;
            CustomAes.BlockSize = 128;
            CustomAes.Padding = PaddingMode.PKCS7;
            CustomAes.Mode = CipherMode.CBC;
        }

        #endregion

        /// <summary>
        ///     Encrypt string
        /// </summary>

        #region EncryptString

        public static string EncryptString(string plainText, string privateKey, string iv,
            out string message)
        {
            try
            {
                using var encryptor =
                    CustomAes.CreateEncryptor(Encoding.UTF8.GetBytes(privateKey), Encoding.UTF8.GetBytes(iv));
                message = null;
                var encryptData = PerformCryptography(Encoding.UTF8.GetBytes(plainText), encryptor);
                return Convert.ToBase64String(encryptData, 0, encryptData.Length);
            }
            catch (Exception ex)
            {
                message = ex.Message;
                return null;
            }
        }

        #endregion

        /// <summary>
        ///     Decrypt string
        /// </summary>

        #region DecryptString

        public static string DecryptString(string cipherText, string privateKey, string iv,
            out string message)
        {
            try
            {
                using var decryptor =
                    CustomAes.CreateDecryptor(Encoding.UTF8.GetBytes(privateKey), Encoding.UTF8.GetBytes(iv));
                message = null;
                return Encoding.UTF8.GetString(PerformCryptography(Convert.FromBase64String(cipherText), decryptor));
            }
            catch (Exception ex)
            {
                message = ex.Message == "The input data is not a complete block."
                    ? "The input data is not a complete block.\nCipher text not correct"
                    : ex.Message;
                return null;
            }
        }

        #endregion

        /// <summary>
        ///     Helper for encrypt and decrypt
        /// </summary>

        #region PerformCryptography

        private static byte[] PerformCryptography(byte[] data, ICryptoTransform cryptoTransform)
        {
            using var ms = new MemoryStream();
            using var cryptoStream = new CryptoStream(ms, cryptoTransform, CryptoStreamMode.Write);
            cryptoStream.Write(data, 0, data.Length);
            cryptoStream.FlushFinalBlock();
            return ms.ToArray();
        }

        #endregion
    }
}