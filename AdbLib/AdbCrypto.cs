using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace AdbLib
{
    public class AdbCrypto
    {
        RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();


        /** The ADB RSA key length in bits */
        public static int KEY_LENGTH_BITS = 2048;

        /** The ADB RSA key length in bytes */
        public static int KEY_LENGTH_BYTES = KEY_LENGTH_BITS / 8;

        /** The ADB RSA key length in words */
        public static int KEY_LENGTH_WORDS = KEY_LENGTH_BYTES / 4;

        /** The RSA signature padding as an int array */
        public static int[] SIGNATURE_PADDING_AS_INT = new int[]
                {
            0x00,0x01,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,
            0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0xff,0x00,
            0x30,0x21,0x30,0x09,0x06,0x05,0x2b,0x0e,0x03,0x02,0x1a,0x05,0x00,
            0x04,0x14
                };

        /** The RSA signature padding as a byte array */
        public static byte[] SIGNATURE_PADDING;

        static AdbCrypto()
        {

            SIGNATURE_PADDING = new byte[SIGNATURE_PADDING_AS_INT.Length];

            for (int i = 0; i < SIGNATURE_PADDING.Length; i++)
                SIGNATURE_PADDING[i] = (byte)SIGNATURE_PADDING_AS_INT[i];
        }

        private int Put(byte[] buffer,int offset,int value)
        {
            byte[] tmp = BitConverter.GetBytes(value);
            Array.Copy(tmp, 0, buffer, offset, tmp.Length);
            return tmp.Length;
        }

        /**
         * Converts a standard RSAPublicKey object to the special ADB format
         * @param pubkey RSAPublicKey object to convert
         * @return Byte array containing the converted RSAPublicKey object
         */
        private byte[] ConvertRsaPublicKeyToAdbFormat(String pubkey)
        {
            /*
                     * ADB literally just saves the RSAPublicKey struct to a file.
                     * 
                     * typedef struct RSAPublicKey {
                     * int len; // Length of n[] in number of uint32_t
                     * uint32_t n0inv;  // -1 / n[0] mod 2^32
                     * uint32_t n[RSANUMWORDS]; // modulus as little endian array
                     * uint32_t rr[RSANUMWORDS]; // R^2 as little endian array
                     * int exponent; // 3 or 65537
                     * } RSAPublicKey;
                     */

            RSAParameters keys = rsa.ExportParameters(true);
            /* ------ This part is a Java-ified version of RSA_to_RSAPublicKey from adb_host_auth.mContext ------ */
            decimal r32, r, rr, rem,  n0inv;
            decimal n;
            r32 = 1 << 32;
            n = Convert.ToDecimal( keys.Modulus);
            r = 1<<(KEY_LENGTH_WORDS * 32);
            rr = (r *r) %n;
            rem = Decimal.Remainder( n,r32);


            n0inv = Decimal.Divide(Decimal.One, rem) % r32;
            //n0inv =  rem.modInverse(r32);

            int[] myN = new int[KEY_LENGTH_WORDS];
            int[] myRr = new int[KEY_LENGTH_WORDS];

            for (int i = 0; i < KEY_LENGTH_WORDS; i++)
            {
                rem = Decimal.Remainder(rr, r32);
                rr = Decimal.Divide(rr, r32);

                myRr[i] = Decimal.ToInt32(rem);

                rem = Decimal.Remainder(n, r32);
                n = Decimal.Divide(n, r32);
                
                myN[i] = Decimal.ToInt32(rem);
            }

            byte[] buffer = new byte[524];
            int offset = 0;
            offset+=Put(buffer, offset, KEY_LENGTH_WORDS);
            offset += Put(buffer, offset, Convert.ToInt32(- n0inv));
            foreach (int i in myN)
                offset += Put(buffer, offset, i);
            foreach (int i in myRr)
                offset += Put(buffer, offset, i);
            Array.Copy(keys.P, 0, buffer, offset, keys.P.Length);
            return buffer;
            /* ------------------------------------------------------------------------------------------- */

            /*ByteBuffer bbuf = ByteBuffer.allocate(524).order(ByteOrder.LITTLE_ENDIAN);


            bbuf.putInt(KEY_LENGTH_WORDS);
            bbuf.putInt(n0inv.negate().intValue());
            for (int i : myN)
                bbuf.putInt(i);
            for (int i : myRr)
                bbuf.putInt(i);

            bbuf.putInt(pubkey.getPublicExponent().intValue());
            return bbuf.array();*/
        }

        /**
         * Creates a new AdbCrypto object from a key pair loaded from files.
         * @param base64 Implementation of base 64 conversion interface required by ADB 
         * @param privateKey File containing the RSA private key
         * @param publicKey File containing the RSA public key
         * @return New AdbCrypto object
         */
        public static AdbCrypto LoadAdbKeyPair(FileInfo privateKey, FileInfo publicKey)
        {
            AdbCrypto crypto = new AdbCrypto();

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            rsa.FromXmlString(File.ReadAllText(privateKey.FullName));
            crypto.rsa = rsa;

            /*int privKeyLength = (int)privateKey.length();
            int pubKeyLength = (int)publicKey.length();
            byte[] privKeyBytes = new byte[privKeyLength];
            byte[] pubKeyBytes = new byte[pubKeyLength];

            FileInputStream privIn = new FileInputStream(privateKey);
            FileInputStream pubIn = new FileInputStream(publicKey);

            privIn.read(privKeyBytes);
                pubIn.read(pubKeyBytes);

                privIn.close();
                pubIn.close();

                KeyFactory keyFactory = KeyFactory.getInstance("RSA");
            EncodedKeySpec privateKeySpec = new PKCS8EncodedKeySpec(privKeyBytes);
            EncodedKeySpec publicKeySpec = new X509EncodedKeySpec(pubKeyBytes);

            crypto.keyPair = new KeyPair(keyFactory.generatePublic(publicKeySpec),
                  keyFactory.generatePrivate(privateKeySpec));

                crypto.base64 = base64;*/

            return crypto;
        }

        /**
         * Creates a new AdbCrypto object by generating a new key pair.
         * @param base64 Implementation of base 64 conversion interface required by ADB 
         * @return A new AdbCrypto object
         */
        public static AdbCrypto GenerateAdbKeyPair()
        {
            AdbCrypto crypto = new AdbCrypto
            {
                rsa = new RSACryptoServiceProvider(KEY_LENGTH_BITS)
            };
            return crypto;
        }

        /**
         * Signs the ADB SHA1 payload with the private key of this object.
         * @param payload SHA1 payload to sign
         * @return Signed SHA1 payload
         */
        public byte[] SignAdbTokenPayload(byte[] payload)
        {
            /* Cipher c = Cipher.getInstance("RSA/ECB/NoPadding");

             c.init(Cipher.ENCRYPT_MODE, keyPair.getPrivate());

             c.update(SIGNATURE_PADDING);

                 return c.doFinal(payload);*/
            return rsa.Encrypt(payload, false);
        }

        /**
         * Gets the RSA public key in ADB format.
         * @return Byte array containing the RSA public key in ADB format.
         */
        public byte[] GetAdbPublicKeyPayload()
        {
            /*byte[]
        convertedKey = convertRsaPublicKeyToAdbFormat((RSAPublicKey)keyPair.getPublic());
        StringBuilder keyString = new StringBuilder(720);

    /* The key is base64 encoded with a user@host suffix and terminated with a NUL * /
    keyString.append(base64.encodeToString(convertedKey));
            keyString.append(" unknown@unknown");
            keyString.append('\0');

            return keyString.toString().getBytes("UTF-8");*/

            byte[]        convertedKey = ConvertRsaPublicKeyToAdbFormat(Convert.ToBase64String(rsa.ExportCspBlob(false)));

            StringBuilder keyString = new StringBuilder(720);
            //RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
            //string public_Key = Convert.ToBase64String(RSA.ExportCspBlob(false));
            //string private_Key = Convert.ToBase64String(RSA.ExportCspBlob(true));
            keyString.Append(Convert.ToBase64String(convertedKey));
            keyString.Append(" unknown@unknown");
            keyString.Append('\0');

            return Encoding.UTF8.GetBytes( keyString.ToString());
        }

        /**
         * Saves the AdbCrypto's key pair to the specified files.
         * @param privateKey The file to store the encoded private key
         * @param publicKey The file to store the encoded public key
         */
        public void SaveAdbKeyPair(FileInfo privateKey, FileInfo publicKey)
        {

            File.WriteAllText(privateKey.FullName, rsa.ToXmlString(true));
            File.WriteAllText(publicKey.FullName, rsa.ToXmlString(false));

        }
    }
}
