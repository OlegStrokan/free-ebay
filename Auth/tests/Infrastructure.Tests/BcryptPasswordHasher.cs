using Infrastructure.Helpers;

namespace Infrastructure.Tests;

public class BcryptPasswordHasherTests
{
    [Fact]
    public void HashPassword_ShouldReturnsHasherPassword()
    {
        var hasher = new BcryptPasswordHasher();
        const string password = "securePassword228";
        
        var hashedPassword = hasher.HashPassword(password);
        
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
        Assert.StartsWith("$2", hashedPassword);
    }
    
    [Fact]
    public void HashPassword_ShouldGenerateDifferentHashesForSamePassword()
    {
        var hasher = new BcryptPasswordHasher();
        const string password = "securePassword228";
        
        var hash1 = hasher.HashPassword(password);
        var hash2 = hasher.HashPassword(password);
        
        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsNull()
    {
        var hasher = new BcryptPasswordHasher();
        
        var exception = Assert.Throws<ArgumentException>(() => hasher.HashPassword(null));
        
        Assert.Equal("password", exception.ParamName);
        Assert.Contains("Password cannot be null or empty", exception.Message);
    }

    [Fact]
    public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsEmpty()
    {
        var hasher = new BcryptPasswordHasher();

        var exception = Assert.Throws<ArgumentException>(() => hasher.HashPassword(string.Empty));
        
        Assert.Equal("password", exception.ParamName);
        Assert.Contains("Password cannot be null or empty", exception.Message);
    }

    [Fact]
    public void HashPassword_ShouldThrowArgumentException_WhenPasswordIsWhitespace()
    {
        var hasher = new BcryptPasswordHasher();
        
        var exception = Assert.Throws<ArgumentException>(() => hasher.HashPassword(" "));
        
        Assert.Equal("password", exception.ParamName);
        Assert.Contains("Password cannot be null or empty", exception.Message);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnTrue_WhenPasswordMatches()
    {
        var hasher = new BcryptPasswordHasher();
        const string password = "securePassword228";
        var hasherPassword = hasher.HashPassword(password);
        
        var result = hasher.VerifyPassword(password, hasherPassword);
        
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenPasswordDoesNotMatch()
    {
        var hasher = new BcryptPasswordHasher();
        const string password = "securePassword228";
        const string wrongPassword = "securePassword229";
        var hasherPassword = hasher.HashPassword(password);
        
        var result = hasher.VerifyPassword(wrongPassword, hasherPassword);
        
        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ShouldBeCaseSensitive()
    {
        var hasher = new BcryptPasswordHasher();
        var password = "securePassword228";
        var hashedPassword = hasher.HashPassword(password);
        
        var resultLowerCase = hasher.VerifyPassword(password.ToLower(), hashedPassword);
        var resultUpperCase = hasher.VerifyPassword(password.ToUpper(), hashedPassword);
        
        Assert.False(resultUpperCase);
        Assert.False(resultLowerCase);
    }

    [Fact]
    public void VerifyPassword_ShouldThrowArgumentException_WhenPasswordIsNull()
    {
        var hasher = new BcryptPasswordHasher();
        var validHash = hasher.HashPassword("securePassword228");
        
        var exception = Assert.Throws<ArgumentException>(() => hasher.VerifyPassword(null, validHash));
        
        Assert.Equal("password", exception.ParamName);
        Assert.Contains("Password cannot be null or empty", exception.Message);
    }

    [Fact]
    public void VerifyPassword_ShouldThrowArgumentException_WhenPasswordIsEmpty()
    {
        var hasher = new BcryptPasswordHasher();
        var validHash = hasher.HashPassword("securePassword228");
        
        var exception = Assert.Throws<ArgumentException>(() => hasher.VerifyPassword(string.Empty, validHash));
        
        Assert.Equal("password", exception.ParamName);
        Assert.Contains("Password cannot be null or empty", exception.Message);
    }

    [Fact]
    public void VerifyPassword_ShouldThrowArgumentException_WhenPasswordHashIsNull()
    {
        var hasher = new BcryptPasswordHasher();
        var exception = Assert.Throws<ArgumentException>(() => hasher.VerifyPassword("securePassword228", null ));
        
        Assert.Equal("passwordHash", exception.ParamName);
        Assert.Contains("Password hash cannot be null or empty", exception.Message);
    }
    
    [Fact]
    public void VerifyPassword_ShouldThrowArgumentException_WhenPasswordHashIsEmpty()
    {
        var hasher = new BcryptPasswordHasher();
        var exception = Assert.Throws<ArgumentException>(() => hasher.VerifyPassword("securePassword228", string.Empty ));
        
        Assert.Equal("passwordHash", exception.ParamName);
        Assert.Contains("Password hash cannot be null or empty", exception.Message);
    }
    
    [Fact]
    public void VerifyPassword_ShouldThrowArgumentException_WhenPasswordHashIsWhitespace()
    {
        var hasher = new BcryptPasswordHasher();
        var exception = Assert.Throws<ArgumentException>(() => hasher.VerifyPassword("securePassword228", "   " ));
        
        Assert.Equal("passwordHash", exception.ParamName);
        Assert.Contains("Password hash cannot be null or empty", exception.Message);
    }

    [Fact]
    public void VerifyPassword_ShouldReturnFalse_WhenHashFormatIsInvalid()
    {
        var hasher = new BcryptPasswordHasher();
        const string password = "securePassword228";
        const string invalidHash = "InvalidenkoSerhiy";
        
        var result = hasher.VerifyPassword(password, invalidHash);

        Assert.False(result);
    }

    [Fact]
    public void VerifyPassword_ShouldHandleLongPasswords()
    {
        var hasher = new BcryptPasswordHasher();
        var longPassword = new string('a', 200);
        var hashedPassword = hasher.HashPassword(longPassword);
        
        var result = hasher.VerifyPassword(longPassword, hashedPassword);
        
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ShouldHandleSpecialCharacters()
    {
        var hasher = new BcryptPasswordHasher();
        const string specialPassword = "P@$$w0rd!#%&*()_+{}[]|\\\\:;\\\"'<>,.?/~`";
        var hashedPassword = hasher.HashPassword(specialPassword);
        
        var result = hasher.VerifyPassword(specialPassword, hashedPassword);
        
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ShouldHandleUnicodeCharacters()
    {
        var hasher = new BcryptPasswordHasher();
        var unicodePassword = "пароль密码🔐مرور";
        var hasherPassword = hasher.HashPassword(unicodePassword);
        
        var result = hasher.VerifyPassword(unicodePassword, hasherPassword);
        
        Assert.True(result);
    }

    [Fact]
    public void HashPassword_ShouldProduceDifferentHashesForSimilarPasswords()
    {
        var hasher = new BcryptPasswordHasher();
        var password1 = "password123";
        var password2 = "password124";

        var hash1 = hasher.HashPassword(password1);
        var hash2 = hasher.HashPassword(password2);
        
        Assert.NotEqual(hash1, hash2);
        Assert.False(hasher.VerifyPassword(password1, hash2));
        Assert.False(hasher.VerifyPassword(password2, hash1));
    }

    [Fact]
    public void VerifyPassword_ShouldWorkWithHashGeneratedFromSamePassword()
    {
        var hasher = new BcryptPasswordHasher();
        var password = "securePassword228";
        
        var hash1 = hasher.HashPassword(password);
        var hash2 = hasher.HashPassword(password);
        var hash3 = hasher.HashPassword(password);
        
        Assert.True(hasher.VerifyPassword(password, hash1));
        Assert.True(hasher.VerifyPassword(password, hash2));
        Assert.True(hasher.VerifyPassword(password, hash3));
    }
    
}