// modified from Xamarin Essentials source
using System;
using System.Text;
using Android.Content;
using Android.Runtime;
using Android.Security;
using Android.Security.Keystore;
using Java.Security;
using Javax.Crypto;
using Javax.Crypto.Spec;
using Microsoft.Extensions.Logging;
using CipherMode = Javax.Crypto.CipherMode;

namespace Shiny.Stores;


public class AndroidKeyStore
{
    const string androidKeyStore = "AndroidKeyStore"; // this is an Android const value
    const string aesAlgorithm = "AES";
    const string cipherTransformationAsymmetric = "RSA/ECB/PKCS1Padding";
    const string cipherTransformationSymmetric = "AES/GCM/NoPadding";
    const string prefsMasterKey = "SecureStorageKey";
    const int initializationVectorLen = 12; // Android supports an IV of 12 for AES/GCM

    readonly ILogger logger;
    readonly IKeyValueStore clearStore;
    readonly Context appContext;
    readonly string alias;
    readonly bool alwaysUseAsymmetricKey;
    readonly KeyStore keyStore;


    // TODO: inject logger
    internal AndroidKeyStore(
        Context appContext,
        IKeyValueStore clearStore,
        ILogger logger,
        string keystoreAlias,
        bool alwaysUseAsymmetricKeyStorage
    )
    {
        this.appContext  = appContext;
        this.alwaysUseAsymmetricKey = alwaysUseAsymmetricKeyStorage;
        this.alias = keystoreAlias;
        this.logger = logger;

        this.clearStore = clearStore;
        this.keyStore = KeyStore.GetInstance(androidKeyStore)!;
        this.keyStore.Load(null);
    }


    ISecretKey GetKey()
    {
        // check to see if we need to get our key from past-versions or newer versions.
        // we want to use symmetric if we are >= 23 or we didn't set it previously.

        // If >= API 23 we can use the KeyStore's symmetric key
        if (!this.alwaysUseAsymmetricKey)
            return this.GetSymmetricKey();

        // NOTE: KeyStore in < API 23 can only store asymmetric keys
        // specifically, only RSA/ECB/PKCS1Padding
        // So we will wrap our symmetric AES key we just generated
        // with this and save the encrypted/wrapped key out to
        // preferences for future use.
        // ECB should be fine in this case as the AES key should be
        // contained in one block.

        // Get the asymmetric key pair
        var keyPair = this.GetAsymmetricKeyPair();
        var existingKeyStr = this.clearStore.Get<string>(prefsMasterKey);
        ISecretKey? secretKey = null;

        if (!existingKeyStr.IsEmpty())
        {
            try
            {
                var wrappedKey = Convert.FromBase64String(existingKeyStr);

                var unwrappedKey = this.UnwrapKey(wrappedKey, keyPair.Private);
                secretKey = unwrappedKey.JavaCast<ISecretKey>();
            }
            catch (InvalidKeyException ikEx)
            {
                this.logger?.LogDebug($"Unable to unwrap key: Invalid Key. This may be caused by system backup or upgrades. All secure storage items will now be removed. {ikEx.Message}");
            }
            catch (IllegalBlockSizeException ibsEx)
            {
                this.logger?.LogDebug($"Unable to unwrap key: Illegal Block Size. This may be caused by system backup or upgrades. All secure storage items will now be removed. {ibsEx.Message}");
            }
            catch (BadPaddingException paddingEx)
            {
                this.logger?.LogDebug($"Unable to unwrap key: Bad Padding. This may be caused by system backup or upgrades. All secure storage items will now be removed. {paddingEx.Message}");
            }
            this.clearStore.Clear(); // TODO: only want this to clear secure though
        }

        if (secretKey == null)
        {
            var keyGenerator = KeyGenerator.GetInstance(aesAlgorithm)!;
            secretKey = keyGenerator.GenerateKey();

            var newWrappedKey = this.WrapKey(secretKey, keyPair.Public);
            this.clearStore.Set(prefsMasterKey, Convert.ToBase64String(newWrappedKey));
        }
        return secretKey;
    }


    // API 23+ Only
    ISecretKey GetSymmetricKey()
    {
        var existingKey = this.keyStore.GetKey(this.alias, null);

        if (existingKey != null)
        {
            var existingSecretKey = existingKey.JavaCast<ISecretKey>();
            return existingSecretKey;
        }

        var keyGenerator = KeyGenerator.GetInstance(KeyProperties.KeyAlgorithmAes, androidKeyStore);
        var builder = new KeyGenParameterSpec.Builder(this.alias, KeyStorePurpose.Encrypt | KeyStorePurpose.Decrypt)
            .SetBlockModes(KeyProperties.BlockModeGcm)
            .SetEncryptionPaddings(KeyProperties.EncryptionPaddingNone)
            .SetRandomizedEncryptionRequired(false);

        keyGenerator.Init(builder.Build());

        return keyGenerator.GenerateKey();
    }


    KeyPair GetAsymmetricKeyPair()
    {
        // set that we generated keys on pre-m device.
        var asymmetricAlias = $"{this.alias}.asymmetric";

        var privateKey = this.keyStore.GetKey(asymmetricAlias, null)?.JavaCast<IPrivateKey>();
        var publicKey = this.keyStore.GetCertificate(asymmetricAlias)?.PublicKey;

        // Return the existing key if found
        if (privateKey != null && publicKey != null)
            return new KeyPair(publicKey, privateKey);

        //var originalLocale = Platform.GetLocale();
        try
        {
            // Force to english for known bug in date parsing:
            // https://issuetracker.google.com/issues/37095309
            //Platform.SetLocale(Java.Util.Locale.English);

            // Otherwise we create a new key
            var generator = KeyPairGenerator.GetInstance(KeyProperties.KeyAlgorithmRsa, androidKeyStore);

            var end = DateTime.UtcNow.AddYears(20);
            var startDate = new Java.Util.Date();
            var endDate = new Java.Util.Date(end.Year, end.Month, end.Day);

            var builder = new KeyPairGeneratorSpec.Builder(this.appContext)
                .SetAlias(asymmetricAlias)
                .SetSerialNumber(Java.Math.BigInteger.One)
                .SetSubject(new Javax.Security.Auth.X500.X500Principal($"CN={asymmetricAlias} CA Certificate"))
                .SetStartDate(startDate)
                .SetEndDate(endDate);

            generator.Initialize(builder.Build());

            return generator.GenerateKeyPair();
        }
        finally
        {
            //Platform.SetLocale(originalLocale);
        }
    }


    byte[] WrapKey(IKey keyToWrap, IKey withKey)
    {
        var cipher = Cipher.GetInstance(cipherTransformationAsymmetric);
        cipher.Init(CipherMode.WrapMode, withKey);
        return cipher.Wrap(keyToWrap);
    }


    IKey UnwrapKey(byte[] wrappedData, IKey withKey)
    {
        var cipher = Cipher.GetInstance(cipherTransformationAsymmetric);
        cipher.Init(CipherMode.UnwrapMode, withKey);
        var unwrapped = cipher.Unwrap(wrappedData, KeyProperties.KeyAlgorithmAes, KeyType.SecretKey);
        return unwrapped;
    }


    internal byte[] Encrypt(string data)
    {
        var key = this.GetKey();

        // Generate initialization vector
        var iv = new byte[initializationVectorLen];

        var sr = new SecureRandom();
        sr.NextBytes(iv);

        Cipher cipher;

        // Attempt to use GCMParameterSpec by default
        try
        {
            cipher = Cipher.GetInstance(cipherTransformationSymmetric);
            cipher.Init(CipherMode.EncryptMode, key, new GCMParameterSpec(128, iv));
        }
        catch (InvalidAlgorithmParameterException)
        {
            // If we encounter this error, it's likely an old bouncycastle provider version
            // is being used which does not recognize GCMParameterSpec, but should work
            // with IvParameterSpec, however we only do this as a last effort since other
            // implementations will error if you use IvParameterSpec when GCMParameterSpec
            // is recognized and expected.
            cipher = Cipher.GetInstance(cipherTransformationSymmetric);
            cipher.Init(CipherMode.EncryptMode, key, new IvParameterSpec(iv));
        }

        var decryptedData = Encoding.UTF8.GetBytes(data);
        var encryptedBytes = cipher.DoFinal(decryptedData);

        // Combine the IV and the encrypted data into one array
        var r = new byte[iv.Length + encryptedBytes.Length];
        Buffer.BlockCopy(iv, 0, r, 0, iv.Length);
        Buffer.BlockCopy(encryptedBytes, 0, r, iv.Length, encryptedBytes.Length);

        return r;
    }

    internal string Decrypt(byte[] data)
    {
        if (data.Length < initializationVectorLen)
            return null;

        var key = this.GetKey();

        // IV will be the first 16 bytes of the encrypted data
        var iv = new byte[initializationVectorLen];
        Buffer.BlockCopy(data, 0, iv, 0, initializationVectorLen);

        Cipher cipher;

        // Attempt to use GCMParameterSpec by default
        try
        {
            cipher = Cipher.GetInstance(cipherTransformationSymmetric);
            cipher.Init(CipherMode.DecryptMode, key, new GCMParameterSpec(128, iv));
        }
        catch (InvalidAlgorithmParameterException)
        {
            // If we encounter this error, it's likely an old bouncycastle provider version
            // is being used which does not recognize GCMParameterSpec, but should work
            // with IvParameterSpec, however we only do this as a last effort since other
            // implementations will error if you use IvParameterSpec when GCMParameterSpec
            // is recognized and expected.
            cipher = Cipher.GetInstance(cipherTransformationSymmetric);
            cipher.Init(CipherMode.DecryptMode, key, new IvParameterSpec(iv));
        }

        // Decrypt starting after the first 16 bytes from the IV
        var decryptedData = cipher.DoFinal(data, initializationVectorLen, data.Length - initializationVectorLen);
        var result = Encoding.UTF8.GetString(decryptedData);
        return result;
    }
}
