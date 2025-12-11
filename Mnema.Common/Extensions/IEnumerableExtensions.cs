namespace Mnema.Common.Extensions;

public static class EnumerableExtensions
{

    extension<T>(IEnumerable<T?> source)
    {

        public IEnumerable<T> WhereNotNull()
        {
            if (source == null)
                throw new NullReferenceException();

            return source.Where(el => el != null)!;
        }
        
    }

    extension<T>(IEnumerable<T> source)
    {

        public IEnumerable<T> WhereIf(bool guard, Predicate<T> predicate)
        {
            if (source == null)
                throw new NullReferenceException();

            return guard ? source.Where(predicate.Invoke) : source;
        }
        
    }
    
}