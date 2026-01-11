using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Mnema.Common.Exceptions;

namespace Mnema.Common.Helpers;

using GraphQL;

public delegate GraphQLQuery GraphQlQueryLoader(string name);

public static class GraphQlHelper
{
    public static GraphQlQueryLoader CreateLoaderForNamespace(Assembly assembly, string ns)
    {
        return name => LoadQuery(assembly, ns, name);
    }

    private static GraphQLQuery LoadQuery(Assembly assembly, string ns, string name)
    {
        var resourceName = $"{ns}.{name}.graphql";
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
            throw new FileNotFoundException($"Resource {resourceName} not found.");

        using var reader = new StreamReader(stream);
        var query = reader.ReadToEnd();

        return new GraphQLQuery(query);
    }
}
