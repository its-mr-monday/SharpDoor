//
//
//		Sharpdoor Crypto Class Module Version 1.0
//
//

using System;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Net.Sockets;
using System.Xml.Serialization;

namespace SharpDoor_Client
{
    class Crypto
    {
        public static RSAParameters PrivateKey(RSACryptoServiceProvider csp)    //Create a private key
        {
            return csp.ExportParameters(true);
        }
        public static RSAParameters PublicKey(RSACryptoServiceProvider csp) //Create a public key
        {
            return csp.ExportParameters(false);
        }
        public static string PublicKeyString(RSAParameters _publicKey)  //Convert public key to string
        {
            var sw = new StringWriter();
            var xs = new XmlSerializer(typeof(RSAParameters));
            xs.Serialize(sw, _publicKey);
            return sw.ToString();
        }
        public static RSAParameters PublicKeyRSA(string publicKeyString)    //Convert a public key from string formay to RSAParamaters
        {
            byte[] lDer;
            //Set RSAKeyInfo to the public key values. 
            int lBeginStart = "-----BEGIN PUBLIC KEY-----".Length;
            int lEndLenght = "-----END PUBLIC KEY-----".Length;
            string KeyString = publicKeyString.Substring(lBeginStart, (publicKeyString.Length - lBeginStart - lEndLenght));
            lDer = Convert.FromBase64String(KeyString);
            //Create a new instance of the RSAParameters structure.
            RSAParameters lRSAKeyInfo = new RSAParameters();
            lRSAKeyInfo.Modulus = GetModulus(lDer);
            lRSAKeyInfo.Exponent = GetExponent(lDer);
            return lRSAKeyInfo;
        }
        private static byte[] GetModulus(byte[] pDer)   // Get modulus for shared public key
        {
            //Size header is 29 bits
            //The key size modulus is 128 bits, but in hexa string the size is 2 digits => 256 
            string lModulus = BitConverter.ToString(pDer).Replace("-", "").Substring(58, 256);
            return StringHexToByteArray(lModulus);
        }

        private static byte[] GetExponent(byte[] pDer)      //Get exponent for shared public key
        {
            int lExponentLenght = pDer[pDer.Length - 3];
            string lExponent = BitConverter.ToString(pDer).Replace("-", "").Substring((pDer.Length * 2) - lExponentLenght * 2, lExponentLenght * 2);
            return StringHexToByteArray(lExponent);
        }

        public static byte[] StringHexToByteArray(string hex)       // Go from string hex to byte form
        {
            return Enumerable.Range(0, hex.Length)
                             .Where(x => x % 2 == 0)
                             .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                             .ToArray();
        }
        public byte[] Encrypt(byte[] plainData, RSAParameters _publicKey)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);
            csp.ImportParameters(_publicKey);
            var cypher = csp.Encrypt(plainData, false);
            return cypher;
        }
        public byte[] Decrypt(byte[] encryptedData, RSAParameters _privateKey)
        {
            RSACryptoServiceProvider csp = new RSACryptoServiceProvider(2048);
            csp.ImportParameters(_privateKey);
            byte[] plainData = csp.Decrypt(encryptedData, false);
            return plainData;
        }
        public static void SendPubKey(NetworkStream stream, TcpClient client, string pubKey)      //Send public key to target
        {
            byte[] sendData = Encoding.ASCII.GetBytes(pubKey);
            stream.Write(sendData, 0, sendData.Length);
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            if (message == "PUB KEY RECEIVED") //pub key check
            {
                Console.WriteLine("serv: " + message);
                return;
            }
        }
        public static string ReceivePubKey(NetworkStream stream, TcpClient client)   //Receive public key from target
        {
            byte[] buffer = new byte[client.ReceiveBufferSize];
            int bytesRead = stream.Read(buffer, 0, client.ReceiveBufferSize);
            string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            string conf = "PUB KEY RECEIVED";
            byte[] sendData = Encoding.ASCII.GetBytes(conf);
            stream.Write(sendData, 0, sendData.Length);
            return message;
        }
    }
}
