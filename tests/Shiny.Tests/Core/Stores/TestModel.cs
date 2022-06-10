using Shiny.Stores;

namespace Shiny.Tests.Core.Stores;


public class TestModel : IStoreEntity
{
    public string Identifier { get; set; }
    public int IntValue { get; set; }
}


public class TestModelStore : IStoreConverter<TestModel>
{
    public TestModel FromStore(IDictionary<string, object> values) => new TestModel
    {
        Identifier = (string)values[nameof(TestModel.Identifier)],
        IntValue = (int)values[nameof(TestModel.IntValue)],
    };


    public IEnumerable<(string Property, object Value)> ToStore(TestModel entity)
    {
        yield return (nameof(TestModel.Identifier), entity.Identifier);
        yield return (nameof(TestModel.IntValue), entity.IntValue);
    }
}