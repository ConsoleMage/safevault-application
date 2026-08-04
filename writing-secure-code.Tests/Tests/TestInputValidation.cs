using NUnit.Framework;
using writing_secure_code.Helpers;

[TestFixture]
public class TestInputValidation
{
    [Test]
    public void TestForSQLInjection()
    {
        var input = "SELECT * FROM Users WHERE Username = 'admin'";

        var isValid = ValidationHelpers.IsValidInput(input);

        Assert.That(isValid, Is.False, "Input should be rejected for SQL injection-like content.");
    }

    [Test]
    public void TestForXSS()
    {
        var input = "<script>alert('XSS')</script>";

        var isValid = ValidationHelpers.IsValidXSSInput(input);

        Assert.That(isValid, Is.False, "Input should be rejected for XSS content.");
    }
}
