using System;
using System.Collections.Generic;
using Genome;
using VariantAnnotation.Interface.AnnotatedPositions;

namespace VariantAnnotation.Interface.Providers
{
	public interface IAnnotationProviderNSA : IAnnotationProvider
    {
		void AnnotateNSA(IAnnotatedPosition annotatedPosition, int customInsertionWindowSize, int customBreakendWindowSize);
    }
}