using System;

namespace Mnema.Common.Extensions;

public static class FloatExtensions
{

    extension(float? f)
    {
        
        public bool SafeEquals(float? y, double tolerance = 1e-10)
        {
            if (f == null) return y == null;

            if (y == null) return false;
            
            var diff = Math.Abs(f.Value- y.Value);
            return diff <= tolerance ||
                   diff <= Math.Max(Math.Abs(f.Value), Math.Abs(y.Value)) * tolerance;
        }
        
    }
    
}