using System.Collections.Generic;
using Variants;

namespace VariantAnnotation.Interface.SA
{
    public interface INsiReader : ISaMetadata
    {
        ReportFor           ReportFor { get; }
        IEnumerable<string> GetAnnotation(IVariant variant, int customInsertionWindowSize = 0, int customBreakendWindowSize = 0);
    }
}