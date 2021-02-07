﻿using Microsoft.EntityFrameworkCore.DataEncryption.Providers;
using Microsoft.EntityFrameworkCore.DataEncryption.Test.Context;
using Microsoft.EntityFrameworkCore.DataEncryption.Test.Helpers;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Xunit;

namespace Microsoft.EntityFrameworkCore.DataEncryption.Test.Providers
{
    public class AesProviderTest
    {
        [Theory]
        [InlineData(AesKeySize.AES128Bits)]
        [InlineData(AesKeySize.AES192Bits)]
        [InlineData(AesKeySize.AES256Bits)]
        public void EncryptDecryptStringTest(AesKeySize keySize)
        {
            string input = StringHelper.RandomString(20);
            AesKeyInfo encryptionKeyInfo = AesProvider.GenerateKey(keySize);
            var provider = new AesProvider(encryptionKeyInfo.Key);

            string encryptedData = provider.Encrypt(input);
            Assert.NotNull(encryptedData);

            string decryptedData = provider.Decrypt(encryptedData);
            Assert.NotNull(decryptedData);

            Assert.Equal(input, decryptedData);
        }

        [Theory]
        [InlineData(AesKeySize.AES128Bits)]
        [InlineData(AesKeySize.AES192Bits)]
        [InlineData(AesKeySize.AES256Bits)]
        public void GenerateAesKeyTest(AesKeySize keySize)
        {
            AesKeyInfo encryptionKeyInfo = AesProvider.GenerateKey(keySize);

            Assert.NotNull(encryptionKeyInfo.Key);
            Assert.NotNull(encryptionKeyInfo.IV);
            Assert.Equal((int)keySize / 8, encryptionKeyInfo.Key.Length);
        }

        [Theory]
        [InlineData(AesKeySize.AES128Bits)]
        [InlineData(AesKeySize.AES192Bits)]
        [InlineData(AesKeySize.AES256Bits)]
        public void CompareTwoAesKeysInstancesTest(AesKeySize keySize)
        {
            AesKeyInfo encryptionKeyInfo1 = AesProvider.GenerateKey(keySize);
            AesKeyInfo encryptionKeyInfo2 = AesProvider.GenerateKey(keySize);
            AesKeyInfo encryptionKeyInfoCopy = encryptionKeyInfo1;

            Assert.NotNull(encryptionKeyInfo1.Key);
            Assert.NotNull(encryptionKeyInfo1.IV);
            Assert.NotNull(encryptionKeyInfo2.Key);
            Assert.NotNull(encryptionKeyInfo2.IV);
            Assert.True(encryptionKeyInfo1 == encryptionKeyInfoCopy);
            Assert.True(encryptionKeyInfo1.Equals(encryptionKeyInfoCopy));
            Assert.True(encryptionKeyInfo1 != encryptionKeyInfo2);
            Assert.True(encryptionKeyInfo1.GetHashCode() != encryptionKeyInfo2.GetHashCode());
            Assert.False(encryptionKeyInfo1.Equals(0));
        }

        [Fact]
        public void CreateDataContextWithoutProvider()
        {
            using (var contextFactory = new DatabaseContextFactory())
            {
                Assert.Throws<ArgumentNullException>(() => contextFactory.CreateContext<SimpleEncryptedDatabaseContext>());
            }
        }

        [Fact]
        public void EncryptUsingAes128Provider()
        {
            ExecuteAesEncryptionTest<Aes128EncryptedDatabaseContext>(AesKeySize.AES128Bits);
        }

        [Fact]
        public void EncryptUsingAes192Provider()
        {
            ExecuteAesEncryptionTest<Aes192EncryptedDatabaseContext>(AesKeySize.AES192Bits);
        }

        [Fact]
        public void EncryptUsingAes256Provider()
        {
            ExecuteAesEncryptionTest<Aes256EncryptedDatabaseContext>(AesKeySize.AES256Bits);
        }

        private void ExecuteAesEncryptionTest<TContext>(AesKeySize aesKeyType) where TContext : DatabaseContext
        {
            AesKeyInfo encryptionKeyInfo = AesProvider.GenerateKey(aesKeyType);
            var provider = new AesProvider(encryptionKeyInfo.Key, CipherMode.CBC, PaddingMode.Zeros);
            var publisher = new PublisherEntity("Rita Book Publishing", "161 Dakota Street")
            {
                Authors = new List<AuthorEntity> {
                    new AuthorEntity("John", "Doe", 42)
                    {
                        Books = new List<BookEntity>()
                        {
                            new BookEntity("Lorem Ipsum", 300),
                            new BookEntity("Dolor sit amet", 390)
                        }
                    }
                }
            };

            using (var contextFactory = new DatabaseContextFactory())
            {
                // Save data to an encrypted database context
                using (var dbContext = contextFactory.CreateContext<TContext>(provider))
                {
                    dbContext.Publishers.Add(publisher);
                    dbContext.SaveChanges();
                }

                // Read decrypted data and compare with original data
                using (var dbContext = contextFactory.CreateContext<TContext>(provider))
                {
                    var publisherFromDb = dbContext.Publishers
                        .Include(x => x.Authors)
                        .ThenInclude(x => x.Books)
                        .FirstOrDefault();

                    Assert.NotNull(publisherFromDb);
                    Assert.Equal(publisher.Name, publisherFromDb.Name);
                    Assert.Equal(publisher.Address, publisherFromDb.Address);
                    Assert.NotNull(publisherFromDb.Authors);
                    Assert.NotEmpty(publisherFromDb.Authors);
                    Assert.Equal(1, publisherFromDb.Authors.Count);

                    Assert.NotNull(publisherFromDb.Authors.FirstOrDefault());
                    Assert.Equal(publisher.Authors.First().FirstName, publisherFromDb.Authors.FirstOrDefault().FirstName);
                    Assert.Equal(publisher.Authors.First().LastName, publisherFromDb.Authors.FirstOrDefault().LastName);
                    Assert.NotNull(publisherFromDb.Authors.FirstOrDefault().Books);
                    Assert.NotEmpty(publisherFromDb.Authors.FirstOrDefault().Books);
                    Assert.Equal(2, publisherFromDb.Authors.FirstOrDefault().Books.Count);
                    Assert.Equal(publisher.Authors.First().Books.First().Name, publisherFromDb.Authors.FirstOrDefault().Books.First().Name);
                    Assert.Equal(publisher.Authors.First().Books.Last().Name, publisherFromDb.Authors.FirstOrDefault().Books.Last().Name);
                }
            }
        }

        public class Aes128EncryptedDatabaseContext : DatabaseContext
        {
            public Aes128EncryptedDatabaseContext(DbContextOptions options, IEncryptionProvider encryptionProvider = null)
                : base(options, encryptionProvider) { }
        }

        public class Aes192EncryptedDatabaseContext : DatabaseContext
        {
            public Aes192EncryptedDatabaseContext(DbContextOptions options, IEncryptionProvider encryptionProvider = null)
                : base(options, encryptionProvider) { }
        }

        public class Aes256EncryptedDatabaseContext : DatabaseContext
        {
            public Aes256EncryptedDatabaseContext(DbContextOptions options, IEncryptionProvider encryptionProvider = null)
                : base(options, encryptionProvider) { }
        }

        public class SimpleEncryptedDatabaseContext : DatabaseContext
        {
            public SimpleEncryptedDatabaseContext(DbContextOptions options, IEncryptionProvider encryptionProvider = null)
                : base(options, encryptionProvider) { }
        }
    }
}