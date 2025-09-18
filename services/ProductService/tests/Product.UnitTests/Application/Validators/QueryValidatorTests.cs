using FluentAssertions;
using Product.Application.Queries;
using Product.Application.Queries.Validators;
using Xunit;

namespace Product.UnitTests.Application.Validators;

/// <summary>
/// Unit Tests cho Query Validators
/// Test các business rules validation của queries
/// </summary>
public class QueryValidatorTests
{
    private readonly GetProductsQueryValidator _getProductsValidator;
    private readonly GetProductByIdQueryValidator _getProductByIdValidator;
    private readonly GetCategoriesQueryValidator _getCategoriesValidator;
    private readonly GetCategoryByIdQueryValidator _getCategoryByIdValidator;

    public QueryValidatorTests()
    {
        _getProductsValidator = new GetProductsQueryValidator();
        _getProductByIdValidator = new GetProductByIdQueryValidator();
        _getCategoriesValidator = new GetCategoriesQueryValidator();
        _getCategoryByIdValidator = new GetCategoryByIdQueryValidator();
    }

    #region GetProductsQuery Validation Tests

    [Fact]
    public void GetProductsQuery_WithValidData_ShouldBeValid()
    {
        // Arrange
        var query = new GetProductsQuery
        {
            Page = 1,
            PageSize = 20,
            MinPrice = 10.0m,
            MaxPrice = 100.0m,
            SearchTerm = "laptop",
            SortBy = "name",
            SortDirection = "asc"
        };

        // Act
        var result = _getProductsValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetProductsQuery_WithInvalidPage_ShouldBeInvalid(int page)
    {
        // Arrange
        var query = new GetProductsQuery { Page = page };

        // Act
        var result = _getProductsValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void GetProductsQuery_WithInvalidPageSize_ShouldBeInvalid(int pageSize)
    {
        // Arrange
        var query = new GetProductsQuery { PageSize = pageSize };

        // Act
        var result = _getProductsValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageSize");
    }

    [Fact]
    public void GetProductsQuery_WithMinPriceGreaterThanMaxPrice_ShouldBeInvalid()
    {
        // Arrange
        var query = new GetProductsQuery
        {
            MinPrice = 100.0m,
            MaxPrice = 50.0m
        };

        // Act
        var result = _getProductsValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.ErrorMessage.Contains("MinPrice phải nhỏ hơn hoặc bằng MaxPrice"));
    }

    [Theory]
    [InlineData("invalid<script>")]
    [InlineData("test & dangerous")]
    [InlineData("search with %")]
    public void GetProductsQuery_WithDangerousSearchTerm_ShouldBeInvalid(string searchTerm)
    {
        // Arrange
        var query = new GetProductsQuery { SearchTerm = searchTerm };

        // Act
        var result = _getProductsValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SearchTerm");
    }

    [Theory]
    [InlineData("invalid_sort")]
    [InlineData("price_desc")]
    [InlineData("")]
    public void GetProductsQuery_WithInvalidSortBy_ShouldBeInvalid(string sortBy)
    {
        // Arrange
        var query = new GetProductsQuery { SortBy = sortBy };

        // Act
        var result = _getProductsValidator.Validate(query);

        // Assert
        if (!string.IsNullOrEmpty(sortBy))
        {
            result.IsValid.Should().BeFalse();
            result.Errors.Should().Contain(e => e.PropertyName == "SortBy");
        }
    }

    #endregion

    #region GetProductByIdQuery Validation Tests

    [Fact]
    public void GetProductByIdQuery_WithValidId_ShouldBeValid()
    {
        // Arrange
        var query = new GetProductByIdQuery(Guid.NewGuid());

        // Act
        var result = _getProductByIdValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetProductByIdQuery_WithEmptyId_ShouldBeInvalid()
    {
        // Arrange
        var query = new GetProductByIdQuery(Guid.Empty);

        // Act
        var result = _getProductByIdValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "ProductId");
    }

    #endregion

    #region GetCategoriesQuery Validation Tests

    [Fact]
    public void GetCategoriesQuery_WithValidData_ShouldBeValid()
    {
        // Arrange
        var query = new GetCategoriesQuery
        {
            PageNumber = 1,
            PageSize = 10,
            SearchTerm = "electronics"
        };

        // Act
        var result = _getCategoriesValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void GetCategoriesQuery_WithInvalidPageNumber_ShouldBeInvalid(int pageNumber)
    {
        // Arrange
        var query = new GetCategoriesQuery { PageNumber = pageNumber };

        // Act
        var result = _getCategoriesValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "PageNumber");
    }

    #endregion

    #region GetCategoryByIdQuery Validation Tests

    [Fact]
    public void GetCategoryByIdQuery_WithValidId_ShouldBeValid()
    {
        // Arrange
        var query = new GetCategoryByIdQuery { Id = Guid.NewGuid() };

        // Act
        var result = _getCategoryByIdValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void GetCategoryByIdQuery_WithEmptyId_ShouldBeInvalid()
    {
        // Arrange
        var query = new GetCategoryByIdQuery { Id = Guid.Empty };

        // Act
        var result = _getCategoryByIdValidator.Validate(query);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Id");
    }

    #endregion
}