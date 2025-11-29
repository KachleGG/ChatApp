using Chatter.Helpers;

namespace Tests
{
    [TestClass]
    public class PasswordHasherTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void HashPassword_Null_Throws() {
            PasswordHasher.HashPassword(null!);
        }

        [TestMethod]
        public void HashPassword_ProducesValidFormat() {
            var password = "Test123!";
            var hash = PasswordHasher.HashPassword(password);

            var parts = hash.Split('$');
            Assert.AreEqual(4, parts.Length);
            Assert.AreEqual("pbkdf2_sha256", parts[0]);
            Assert.IsTrue(int.TryParse(parts[1], out var iters));
            Assert.IsFalse(string.IsNullOrEmpty(parts[2]));
            Assert.IsFalse(string.IsNullOrEmpty(parts[3]));
        }

        [TestMethod]
        public void VerifyPassword_CorrectPassword_ReturnsTrue() {
            var password = "SecretPass";
            var hash = PasswordHasher.HashPassword(password);

            Assert.IsTrue(PasswordHasher.VerifyPassword(password, hash));
        }

        [TestMethod]
        public void VerifyPassword_WrongPassword_ReturnsFalse() {
            var password = "SecretPass";
            var hash = PasswordHasher.HashPassword(password);

            Assert.IsFalse(PasswordHasher.VerifyPassword("WrongPass", hash));
        }

        [TestMethod]
        public void VerifyPassword_NullOrEmptyHash_ReturnsFalse() {
            Assert.IsFalse(PasswordHasher.VerifyPassword("password", null!));
            Assert.IsFalse(PasswordHasher.VerifyPassword("password", ""));
        }

        [TestMethod]
        public void VerifyPassword_InvalidFormat_ReturnsFalse() {
            var badHash = "not_valid_format";
            Assert.IsFalse(PasswordHasher.VerifyPassword("password", badHash));
        }

        [TestMethod]
        public void VerifyPassword_NonPbkdf2Algorithm_ReturnsFalse() {
            var fakeHash = "sha1$1000$abc$def";
            Assert.IsFalse(PasswordHasher.VerifyPassword("password", fakeHash));
        }

        [TestMethod]
        public void NeedsUpgrade_NullOrEmpty_ReturnsTrue() {
            Assert.IsTrue(PasswordHasher.NeedsUpgrade(null!));
            Assert.IsTrue(PasswordHasher.NeedsUpgrade(""));
        }

        [TestMethod]
        public void NeedsUpgrade_InvalidFormat_ReturnsTrue() {
            Assert.IsTrue(PasswordHasher.NeedsUpgrade("invalid$format"));
        }

        [TestMethod]
        public void NeedsUpgrade_LessIterations_ReturnsTrue() {
            var hash = "pbkdf2_sha256$1000$abc$def";
            Assert.IsTrue(PasswordHasher.NeedsUpgrade(hash));
        }

        [TestMethod]
        public void NeedsUpgrade_CurrentIterations_ReturnsFalse() {
            var currentIterations = 200_000;
            var hash = $"pbkdf2_sha256${currentIterations}$abc$def";
            Assert.IsFalse(PasswordHasher.NeedsUpgrade(hash));
        }
    }
}
