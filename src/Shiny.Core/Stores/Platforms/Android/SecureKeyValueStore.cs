//using System;
//using Java.Security;
//using Javax.Crypto;
//using Javax.Crypto.Spec;
//using Shiny.Infrastructure;


//namespace Shiny.Stores
//{
//    public class SecureKeyValueStore : IKeyValueStore
//    {
//        readonly SettingsKeyValueStore settingsStore;


//        public SecureKeyValueStore(IAndroidContext context, ISerializer serializer)
//        {
//            this.settingsStore = new SettingsKeyValueStore(context, serializer);
//        }


//        public void Clear() => throw new NotImplementedException();
//        public bool Contains(string key) => throw new NotImplementedException();
//        public T? Get<T>(string key) => throw new NotImplementedException();
//        public object Get(Type type, string key) => throw new NotImplementedException();
//        public bool Remove(string key) => throw new NotImplementedException();
//        public void Set<T>(string key, T value) => throw new NotImplementedException();
//        public void Set(string key, object value) => throw new NotImplementedException();


//        const string SETTINGS_KEY = "CRYPTO";
//        const string MODE = "AES/ECB/PKCS7Padding";
//        const string PROVIDER = "BC";
//        const string ALGORITHM = "AES";

//        protected virtual string Decrypt()
//        {
//            var cipher = Cipher.GetInstance(MODE, "BC")!;
//            cipher.Init(CipherMode.DecryptMode, this.GetSecretKey());

//            return null;
//        }


//        protected Key GetSecretKey()
//        {
//            var enryptedKeyB64 = this.settingsStore.Get<string>(SETTINGS_KEY);
//            var encryptedKey = Convert.FromBase64String(enryptedKeyB64);

//            var key = new byte[0];
//            //var key = rsaDecrypt(encryptedKey);
//            return new SecretKeySpec(key, ALGORITHM);
//        }


////        private static final String     AndroidKeyStore = "AndroidKeyStore";
////keyStore = KeyStore.getInstance(AndroidKeyStore);
////keyStore.load(null);
////// Generate the RSA key pairs
////if (!keyStore.containsAlias(KEY_ALIAS)) {
////    // Generate a key pair for encryption
////    Calendar start = Calendar.getInstance();
////        Calendar end = Calendar.getInstance();
////        end.add(Calendar.YEAR, 30);
////    KeyPairGeneratorSpec spec = new KeyPairGeneratorSpec.Builder(context)
////            .setAlias(KEY_ALIAS)
////            .setSubject(new X500Principal("CN=" + KEY_ALIAS))
////            .setSerialNumber(BigInteger.TEN)
////            .setStartDate(start.getTime())
////            .setEndDate(end.getTime())
////            .build();
////        KeyPairGenerator kpg = KeyPairGenerator.getInstance(KeyProperties.KEY_ALGORITHM_RSA, AndroidKeyStore);
////        kpg.initialize(spec);
////    kpg.generateKeyPair();
////}

//    //    protected byte[] rsaEncrypt(byte[] secret) throws Exception
//    //    {
//    //        KeyStore.PrivateKeyEntry privateKeyEntry = (KeyStore.PrivateKeyEntry) keyStore.getEntry(KEY_ALIAS, null);
//    //        // Encrypt the text
//    //        Cipher inputCipher = Cipher.getInstance(RSA_MODE, "AndroidOpenSSL");
//    //        inputCipher.init(Cipher.ENCRYPT_MODE, privateKeyEntry.getCertificate().getPublicKey());

//    //    ByteArrayOutputStream outputStream = new ByteArrayOutputStream();
//    //        CipherOutputStream cipherOutputStream = new CipherOutputStream(outputStream, inputCipher);
//    //        cipherOutputStream.write(secret);
//    //    cipherOutputStream.close();

//    //    byte[] vals = outputStream.toByteArray();
//    //    return vals;
//    //}

//        protected byte[] rsaDecrypt(byte[] encrypted)
//        {
//            KeyStore.PrivateKeyEntry privateKeyEntry = (KeyStore.PrivateKeyEntry)keyStore.getEntry(KEY_ALIAS, null);
//            Cipher output = Cipher.getInstance(RSA_MODE, "AndroidOpenSSL");
//            output.init(Cipher.DECRYPT_MODE, privateKeyEntry.getPrivateKey());
//            CipherInputStream cipherInputStream = new CipherInputStream(
//                    new ByteArrayInputStream(encrypted), output);
//            ArrayList<Byte> values = new ArrayList<>();
//            int nextByte;
//            while ((nextByte = cipherInputStream.read()) != -1)
//            {
//                values.add((byte)nextByte);
//            }

//            byte[] bytes = new byte[values.size()];
//            for (int i = 0; i < bytes.length; i++)
//            {
//                bytes[i] = values.get(i).byteValue();
//            }
//            return bytes;
//        }
//    }
//}
