using Bo.Models;
using Dal;
using Dal.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Tests.Repositories
{
    [TestClass]
    public class PasskeyRepositoryTests
    {
        private EsnDevContext _context = null!;
        private PasskeyRepository _repository = null!;

        [TestInitialize]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<EsnDevContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new EsnDevContext(options);
            _repository = new PasskeyRepository(_context);

            SeedData();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context.Dispose();
        }

        private void SeedData()
        {
            var user = new UserBo
            {
                Id = 1,
                Email = "test@example.com",
                PasswordHash = "hash",
                FirstName = "Test",
                LastName = "User",
                BirthDate = DateTime.UtcNow.AddYears(-25),
                StudentType = "exchange"
            };
            _context.Users.Add(user);

            var passkeys = new List<UserPasskeyBo>
            {
                new()
                {
                    Id = 1, UserId = 1, CredentialId = "cred-1",
                    PublicKey = new byte[] { 1, 2, 3 }, SignCount = 0,
                    DisplayName = "Windows Hello", CreatedAt = DateTime.UtcNow.AddDays(-2)
                },
                new()
                {
                    Id = 2, UserId = 1, CredentialId = "cred-2",
                    PublicKey = new byte[] { 4, 5, 6 }, SignCount = 5,
                    DisplayName = "YubiKey", CreatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };
            _context.UserPasskeys.AddRange(passkeys);
            _context.SaveChanges();
        }

        [TestMethod]
        public async Task GetByUserIdAsync_ReturnsUserPasskeys()
        {
            // Act
            var result = await _repository.GetByUserIdAsync(1);

            // Assert
            Assert.AreEqual(2, result.Count());
        }

        [TestMethod]
        public async Task GetByUserIdAsync_NoPasskeys_ReturnsEmpty()
        {
            // Act
            var result = await _repository.GetByUserIdAsync(999);

            // Assert
            Assert.AreEqual(0, result.Count());
        }

        [TestMethod]
        public async Task GetByCredentialIdAsync_ExistingCredential_ReturnsPasskey()
        {
            // Act
            var result = await _repository.GetByCredentialIdAsync("cred-1");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("cred-1", result.CredentialId);
            Assert.AreEqual("Windows Hello", result.DisplayName);
        }

        [TestMethod]
        public async Task GetByCredentialIdAsync_NonExisting_ReturnsNull()
        {
            // Act
            var result = await _repository.GetByCredentialIdAsync("nonexistent");

            // Assert
            Assert.IsNull(result);
        }

        [TestMethod]
        public async Task CredentialIdExistsAsync_ExistingCredential_ReturnsTrue()
        {
            // Act
            var result = await _repository.CredentialIdExistsAsync("cred-1");

            // Assert
            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task CredentialIdExistsAsync_NonExisting_ReturnsFalse()
        {
            // Act
            var result = await _repository.CredentialIdExistsAsync("nonexistent");

            // Assert
            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task GetByUserIdAsync_OrderedByCreatedAtDescending()
        {
            // Act
            var result = (await _repository.GetByUserIdAsync(1)).ToList();

            // Assert
            Assert.AreEqual(2, result.Count);
            // Most recent first
            Assert.AreEqual("cred-2", result[0].CredentialId);
            Assert.AreEqual("cred-1", result[1].CredentialId);
        }
    }
}
