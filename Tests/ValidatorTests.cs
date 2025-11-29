using Chatter.Helpers;

namespace Tests
{
    [TestClass]
    public class ValidatorTest
    {
        [TestMethod]
        public void IsValidName_ValidAndInvalid() {
            Assert.IsTrue(Validator.IsValidName("John Doe"));
            Assert.IsTrue(Validator.IsValidName("O'Connor"));
            Assert.IsTrue(Validator.IsValidName("Anne-Marie"));

            Assert.IsFalse(Validator.IsValidName(null));
            Assert.IsFalse(Validator.IsValidName(""));
            Assert.IsFalse(Validator.IsValidName(" "));
            Assert.IsFalse(Validator.IsValidName("A")); // too short
            Assert.IsFalse(Validator.IsValidName("ThisNameIsWayTooLongToBeValidBecauseItExceedsFiftyCharacters"));
            Assert.IsFalse(Validator.IsValidName("Invalid123"));
        }

        [TestMethod]
        public void IsValidEmail_ValidAndInvalid() {
            Assert.IsTrue(Validator.IsValidEmail("test@example.com"));
            Assert.IsTrue(Validator.IsValidEmail("user.name+tag@domain.co.uk"));

            Assert.IsFalse(Validator.IsValidEmail(null));
            Assert.IsFalse(Validator.IsValidEmail(""));
            Assert.IsFalse(Validator.IsValidEmail("invalid-email"));
            Assert.IsFalse(Validator.IsValidEmail("user@.com"));
        }

        [TestMethod]
        public void IsValidPassword_ValidAndInvalid() {
            Assert.IsTrue(Validator.IsValidPassword("123456"));
            Assert.IsTrue(Validator.IsValidPassword("abcdef"));

            Assert.IsFalse(Validator.IsValidPassword(null));
            Assert.IsFalse(Validator.IsValidPassword(""));
            Assert.IsFalse(Validator.IsValidPassword("123")); // too short
        }

        [TestMethod]
        public void IsStrongPassword_ValidAndInvalid() {
            Assert.IsTrue(Validator.IsStrongPassword("Abcdef1!"));
            Assert.IsTrue(Validator.IsStrongPassword("StrongP@ss123"));

            Assert.IsFalse(Validator.IsStrongPassword(null));
            Assert.IsFalse(Validator.IsStrongPassword(""));
            Assert.IsFalse(Validator.IsStrongPassword("abcdefg")); // missing upper, digit, special
            Assert.IsFalse(Validator.IsStrongPassword("ABCDEFG1")); // missing lower, special
            Assert.IsFalse(Validator.IsStrongPassword("Abcdefgh")); // missing digit, special
        }

        [TestMethod]
        public void GetPasswordStrengthMessage_Cases() {
            Assert.AreEqual("Password is required.", Validator.GetPasswordStrengthMessage(null));
            Assert.AreEqual("Password is required.", Validator.GetPasswordStrengthMessage(""));

            Assert.AreEqual("Password must be at least 6 characters long.", Validator.GetPasswordStrengthMessage("123"));
            Assert.AreEqual("Password should be at least 8 characters long for better security.", Validator.GetPasswordStrengthMessage("123456"));

            string msg = Validator.GetPasswordStrengthMessage("abcdefg");
            StringAssert.Contains(msg, "an uppercase letter");
            StringAssert.Contains(msg, "a number");
            StringAssert.Contains(msg, "a special character");

            string strongMsg = Validator.GetPasswordStrengthMessage("Abcdef1!");
            Assert.AreEqual("Password is strong.", strongMsg);
        }

        [TestMethod]
        public void IsValidMessage_Cases() {
            Assert.IsTrue(Validator.IsValidMessage("Hello"));
            Assert.IsFalse(Validator.IsValidMessage(null));
            Assert.IsFalse(Validator.IsValidMessage(""));
        }

        [TestMethod]
        public void IsValidUserId_Cases() {
            Assert.IsTrue(Validator.IsValidUserId(1));
            Assert.IsFalse(Validator.IsValidUserId(0));
            Assert.IsFalse(Validator.IsValidUserId(-10));
        }

        [TestMethod]
        public void SanitizeEmail_TrimsAndLowercases() {
            var email = "  USER@Example.COM ";
            var sanitized = Validator.SanitizeEmail(email);
            Assert.AreEqual("user@example.com", sanitized);
        }

        [TestMethod]
        public void SanitizeName_TrimsAndNormalizesSpaces() {
            var name = "  John   Doe   ";
            var sanitized = Validator.SanitizeName(name);
            Assert.AreEqual("John Doe", sanitized);

            var name2 = " Anne-Marie  O'Connor ";
            Assert.AreEqual("Anne-Marie O'Connor", Validator.SanitizeName(name2));
        }
    }
}
