﻿// Copyright 2022 Crystal Ferrai
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using EpicGames.Compression;
using System.IO.Compression;

namespace NetPak.Internal
{
	/// <summary>
	/// Contains helper methods for compressing and decompressing data via different compression algorithms
	/// </summary>
	internal static class Compressor
	{
		public static int DecompressBlock(Stream compressed, byte[] decompressed, int startIndex, CompressionMethod compressionMethod)
		{
			int length = decompressed.Length - startIndex;

			using Stream stream = compressionMethod switch
			{
				CompressionMethod.None => compressed,
				CompressionMethod.Zlib => new ZLibStream(compressed, CompressionMode.Decompress, true),
				CompressionMethod.Gzip => new GZipStream(compressed, CompressionMode.Decompress, true),
				CompressionMethod.Oodle => new OodleStream(compressed, length, true),
				CompressionMethod.LZ4 => throw new NotImplementedException($"Compression method {compressionMethod} has not been implemented"),
				_ => throw new PakSerializerException($"Unrecognized compression method {compressionMethod}")
			};

			int remaining = length;
			while (remaining > 0)
			{
				int read = stream.Read(decompressed, startIndex, remaining);
				if (read == 0) break;
				startIndex += read;
				remaining -= read;
			}
			return length - remaining;
		}

		public static void CompressBlock(byte[] decompressed, Stream compressed, int startIndex, int length, CompressionMethod compressionMethod)
		{
			switch (compressionMethod)
			{
				case CompressionMethod.None:
					compressed.Write(decompressed, startIndex, length);
					break;
				case CompressionMethod.Zlib:
					using (ZLibStream stream = new(compressed, CompressionMode.Compress, true))
					{
						stream.Write(decompressed, startIndex, length);
					}
					break;
				case CompressionMethod.Gzip:
					using (GZipStream stream = new(compressed, CompressionMode.Compress, true))
					{
						stream.Write(decompressed, startIndex, length);
					}
					break;
				case CompressionMethod.Oodle:
					CompressBlock_Oodle(decompressed, compressed, startIndex, length, OodleCompressorType.Kraken, OodleCompressionLevel.Normal);
					break;
				case CompressionMethod.LZ4:
					throw new NotImplementedException($"Compression method {compressionMethod} has not been implemented");
				case CompressionMethod.Custom:
				case CompressionMethod.Unknown:
				default:
					throw new PakSerializerException($"Unrecognized compression method {compressionMethod}");
			}
		}

		public static void CompressBlock_Oodle(byte[] decompressed, Stream compressed, int startIndex, int length, OodleCompressorType type, OodleCompressionLevel level)
		{
			using OodleStream stream = new(compressed, type, level, true);
			stream.Write(decompressed, startIndex, length);
		}
	}
}
