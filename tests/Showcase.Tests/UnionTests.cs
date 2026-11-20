using System.Runtime.CompilerServices;
using Showcase.Language;

namespace Showcase.Tests;

public class UnionTests
{
    [Fact]
    public void Case_types_convert_implicitly_to_the_union()
    {
        Pet pet = new Cat("Tom");

        Assert.IsType<Cat>(pet.Value);
        Assert.Equal("Tom the cat says meow", UnionsDemo.Describe(pet));
    }

    [Theory]
    [InlineData(1, "200 OK: Ada")]
    [InlineData(7, "404: user 7 not found")]
    [InlineData(0, "400: Id must be positive")]
    public void Result_union_is_matched_exhaustively(int id, string expected)
    {
        Assert.Equal(expected, UnionsDemo.Render(UnionsDemo.FindUser(id)));
    }

    [Fact]
    public void Property_pattern_with_case_type_inspects_the_contents()
    {
        Assert.Equal("Rex the labrador fetches", UnionsDemo.Describe(new Dog("Rex", "Labrador")));
        Assert.Equal("Fido the Beagle barks", UnionsDemo.Describe(new Dog("Fido", "Beagle")));
    }

    [Fact]
    public void Default_union_matches_the_null_pattern()
    {
        Pet pet = default;

        Assert.True(pet is null);
        Assert.Null(pet.Value);
        Assert.Equal("no pet (default union, Value is null)", UnionsDemo.Describe(pet));
    }

    [Fact]
    public void Declared_union_is_a_struct_that_boxes_value_type_cases()
    {
        IntOrString value = 42;

        Assert.True(typeof(IntOrString).IsValueType);
        Assert.True(typeof(IUnion).IsAssignableFrom(typeof(IntOrString)));
        object? stored = value.Value;
        Assert.IsType<int>(stored); // the int lives in an object? field, i.e. it is boxed
    }
}
