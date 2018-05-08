﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using IO;
using VariantAnnotation.Interface.SA;
using VariantAnnotation.SA;
using VariantAnnotation.Interface.GeneAnnotation;
using VariantAnnotation.GeneAnnotation;
using VariantAnnotation.Interface.Providers;

namespace SAUtils.MergeInterimTsvs
{
    public sealed class GeneDatabaseWriter : IDisposable
    {
        private readonly Stream _stream;
        private readonly ExtendedBinaryWriter _writer;
        private readonly bool _leaveOpen;

        public GeneDatabaseWriter(Stream stream, ISupplementaryAnnotationHeader header, bool leaveOpen = false)
        {
            _leaveOpen = leaveOpen;
            _stream = stream;
            _writer = new ExtendedBinaryWriter(stream, new UTF8Encoding(false, true), _leaveOpen);
            WriteHeader(header);
        }

        private void WriteHeader(IProvider header)
        {
            _writer.Write(SaCommon.DataHeader);
            _writer.Write(SaCommon.DataVersion);
            _writer.Write(SaCommon.SchemaVersion);
            _writer.Write((byte)header.Assembly);
            _writer.Write(DateTime.Now.Ticks);

            var dataSourceVersions = header.DataSourceVersions.ToList();
            _writer.WriteOpt(dataSourceVersions.Count);
            foreach (var version in dataSourceVersions) version.Write(_writer);
            _writer.Write(SaCommon.GuardInt);
        }

        public void Dispose()
        {
            Flush();
            if (!_leaveOpen) _stream.Dispose();
            _writer.Dispose();

        }

        public void Write(IAnnotatedGene annotatedGene)
        {
            annotatedGene.Write(_writer);
        }

        private void Flush()
        {
            var nullGene = AnnotatedGene.CreateEmptyGene();
            nullGene.Write(_writer);
            _stream.Flush();
        }


    }
}
