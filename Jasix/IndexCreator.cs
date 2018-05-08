﻿using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using CommandLine.Utilities;
using Compression.FileHandling;
using ErrorHandling.Exceptions;
using IO;
using Jasix.DataStructures;
using Newtonsoft.Json;
using OptimizedCore;


namespace Jasix
{
    public sealed class IndexCreator : IDisposable
    {
        #region members
        private readonly BgzipTextReader _reader;
        private readonly Stream _writeStream;
        private readonly HashSet<string> _processedChromosome;

        private readonly Benchmark _chromBenchmark;
        private readonly Benchmark _benchmark;

        #endregion

       
        public IndexCreator(BlockGZipStream readStream, Stream writeStream)
        {
            _reader              = new BgzipTextReader(readStream);
            _writeStream         = writeStream;
            _processedChromosome = new HashSet<string>();
            _chromBenchmark      = new Benchmark();
            _benchmark           = new Benchmark();
        }

        public IndexCreator(string fileName)
            : this(
                new BlockGZipStream(FileUtilities.GetReadStream(fileName), CompressionMode.Decompress),
                FileUtilities.GetCreateStream(fileName + JasixCommons.FileExt))
        {}

        public void CreateIndex()
        {
            string searchTag = $"\"{JasixCommons.PositionsSectionTag}\":[";
            string headerTag = $"{{\"{JasixCommons.HeaderSectionTag}\":";
            var index = new JasixIndex();
            string line;

            long previousPosition = _reader.Position;
            while ((line = _reader.ReadLine()) != null)
            {
                if (line.StartsWith(headerTag))
                {
                    index.BeginSection(JasixCommons.HeaderSectionTag, previousPosition);
                    Console.WriteLine($"section:{JasixCommons.HeaderSectionTag} starts at {previousPosition}");
                }
                if (line.EndsWith(searchTag))
                {
                    {
                        index.EndSection(JasixCommons.HeaderSectionTag, previousPosition);
                        Console.WriteLine($"section:{JasixCommons.HeaderSectionTag} ends at {previousPosition}");
                    }
                    break;
                }

                previousPosition = _reader.Position;
            }

            // we need the location before accessing the line
            long linePosition = _reader.Position;
            index.BeginSection(JasixCommons.PositionsSectionTag, linePosition);
            Console.WriteLine($"section:{JasixCommons.PositionsSectionTag} starts at {linePosition}");

            var previousChr = "";
            var previousPos = 0;
            while ((line = _reader.ReadLine()) != null)
            {
                if (line.OptimizedStartsWith(']'))
                {
                    index.EndSection(JasixCommons.PositionsSectionTag, linePosition);
                    Console.WriteLine($"section:{JasixCommons.PositionsSectionTag} ends at {linePosition}");
                    break;
                }
                line = line.TrimEnd(',');
                var chrPos = GetChromPosition(line);

                CheckSorting(chrPos.chr, chrPos.position, previousChr, previousPos);

                index.Add(chrPos.chr, chrPos.position, chrPos.end, linePosition);
                linePosition = _reader.Position;
                previousChr = chrPos.chr;
                previousPos = chrPos.position;

            }

            //go through the genes section
            do
            {
                linePosition = _reader.Position;
                if (line == null) break;
                if (line.EndsWith($",\"{JasixCommons.GenesSectionTag}\":["))
                {
                    index.BeginSection(JasixCommons.GenesSectionTag, _reader.Position);
                    Console.WriteLine($"section:{JasixCommons.GenesSectionTag} starts at {_reader.Position}");
                }

                if (line.EndsWith("]}"))
                {
                    index.EndSection(JasixCommons.GenesSectionTag, linePosition);
                    Console.WriteLine($"section:{JasixCommons.GenesSectionTag} ends at {linePosition}");
                    break;
                }
                
            } while ((line = _reader.ReadLine()) != null);

            index.Write(_writeStream);

            Console.WriteLine();

            long peakMemoryUsageBytes = MemoryUtilities.GetPeakMemoryUsage();
            var wallTimeSpan = _benchmark.GetElapsedTime();
            Console.WriteLine();
            if (peakMemoryUsageBytes > 0) Console.WriteLine("Peak memory usage: {0}", MemoryUtilities.ToHumanReadable(peakMemoryUsageBytes));
            Console.WriteLine("Time: {0}", Benchmark.ToHumanReadable(wallTimeSpan));
        }

        // ReSharper disable once UnusedParameter.Local
        // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
        private void CheckSorting(string chr, int pos, string previousChr, int previousPos)
        {
            if (chr != previousChr && _processedChromosome.Contains(chr))
            {
                throw new UserErrorException($"the Json file is not sorted at {chr}: {pos}");
            }

            if (chr == previousChr && pos < previousPos)
            {
                throw new UserErrorException($"the Json file is not sorted at {chr}: {pos}");
            }

            if (chr == previousChr || previousChr == "") return;

            Console.WriteLine($"Ref Sequence {previousChr} indexed in {Benchmark.ToHumanReadable(_chromBenchmark.GetElapsedTime())}");
            _chromBenchmark.Reset();
        }

        internal static (string chr, int position, int end) GetChromPosition(string line)
        {
            JsonSchema jsonEntry;
            try
            {
                jsonEntry = JsonConvert.DeserializeObject<JsonSchema>(line);
            }
            catch (Exception)
            {
                Console.WriteLine($"Error in line:\n{line}");
                throw;
            }

			int end = Utilities.GetJsonEntryEnd(jsonEntry);

            return (jsonEntry.chromosome, jsonEntry.position, end);
        }

        public void Dispose()
        {
            _reader?.Dispose();
            _writeStream?.Dispose();
        }
    }
}
