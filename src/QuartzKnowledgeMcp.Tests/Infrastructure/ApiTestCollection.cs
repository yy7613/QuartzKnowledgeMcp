namespace QuartzKnowledgeMcp.Tests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class ApiTestCollection : ICollectionFixture<ApiTestFactory>
{
    public const string Name = "ApiTests";
}