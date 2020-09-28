//
//
//		Sharpdoor Crypto Class
//
//

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace SharpDoor_Client
{
    class Crypto
    {
        public static byte[] GenerateSalt(int length)   //Thank you Microsoft for this one makes random salt of 'lenght' bytes
        {
            // Create a buffer
            byte[] randBytes;
            if (length >= 1)
            {
                randBytes = new byte[length];
            }
            else
            {
                randBytes = new byte[1];
            }
            // Create a new RNGCryptoServiceProvider.
            RNGCryptoServiceProvider rand = new RNGCryptoServiceProvider();
            // Fill the buffer with random bytes.
            rand.GetBytes(randBytes);
            // return the bytes.
            return randBytes;
        }
        public static string GeneratePassword(int length)     //Generates a random string password
        {
            // creating a StringBuilder object()
            StringBuilder str_build = new StringBuilder();
            Random random = new Random();
            char letter;
            for (int i = 0; i < length; i++)
            {
                double flt = random.NextDouble();
                int shift = Convert.ToInt32(Math.Floor(25 * flt));
                letter = Convert.ToChar(shift + 65);
                str_build.Append(letter);
            }
            return str_build.ToString();
        }
        public static PasswordDeriveBytes GenerateKey(string password, byte[] salt)     //Generates a random key from the password and salt
        {
            byte[] pwd = Encoding.Unicode.GetBytes(password);
            return new PasswordDeriveBytes(pwd, salt);
        }
        public static byte[] Encrypt(byte[] input, PasswordDeriveBytes pdb) //Encrypts input bytes with a password key
        {
            MemoryStream ms = new MemoryStream();   //Create new memory stream
            Aes aes = new AesManaged(); //Create new aes key object
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            CryptoStream cs = new CryptoStream(ms, aes.CreateEncryptor(), CryptoStreamMode.Write);  //Create a crypto stream with the memory stream and aes key
            cs.Write(input, 0, input.Length);   //Write the unencrypted bytes to the stream
            cs.Close(); //Close the crypto stream and therefore writing the encrypted bytes to the memory stream
            return ms.ToArray();    //return a byte array of the encrypted data
        }
        
        public static byte[] Decrypt(byte[] input, PasswordDeriveBytes pdb) //Decryts input bytes with a password key
        {
            MemoryStream ms = new MemoryStream();
            Aes aes = new AesManaged();
            aes.Key = pdb.GetBytes(aes.KeySize / 8);
            aes.IV = pdb.GetBytes(aes.BlockSize / 8);
            CryptoStream cs = new CryptoStream(ms, aes.CreateDecryptor(), CryptoStreamMode.Write);
            cs.Write(input, 0, input.Length);
            cs.Close();
            return ms.ToArray();
        }
    }
}
