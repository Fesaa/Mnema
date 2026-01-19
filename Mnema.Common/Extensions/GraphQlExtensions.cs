using GraphQL;

namespace Mnema.Common.Extensions;

public static class GraphQlExtensions
{

    extension(GraphQLQuery query)
    {
        public GraphQLRequest ToRequest(object? variables = null)
        {
            return new GraphQLRequest(query, variables);
        }
    }

}
