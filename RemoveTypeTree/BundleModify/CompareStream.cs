using UnityFS;

namespace BundleCrafter
{
    public class CompareStream : Stream
    {
        private MemoryStream targetStream;
        private byte[] originBytes;
        private EndianBinaryReader reader = null;
        private bool isCompare = true;
        public CompareStream(MemoryStream targetStream, byte[] originBytes)
        {
            this.targetStream = targetStream;
            this.originBytes = originBytes;
            this.reader = new EndianBinaryReader(new MemoryStream(originBytes), EndianType.LittleEndian);
        }
        public override void Flush()
        {
            this.targetStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            CompareBytes();
            int readCount = this.targetStream.Read(buffer, offset, count);
            CompareBytes();
            return readCount;
        }

        private long targetlastCorrectIndex = 0;
        private long originLastCorrectIndex = 0;

        private void CompareBytes()
        {
            if (isCompare)
            {
                var buffer = targetStream.GetBuffer();
                for (long i = targetlastCorrectIndex; i < targetStream.Position; i++)
                {
                    var diff = i - targetlastCorrectIndex;
                    var originIndex = originLastCorrectIndex + diff;
                    /* if (buffer[i] != originBytes[originIndex])
                     {
                         throw new Exception("Error: Failed last correct "+targetlastCorrectIndex);
                     }*/
                }

                originLastCorrectIndex += targetStream.Position - targetlastCorrectIndex;
            }

            targetlastCorrectIndex = targetStream.Position;

        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            CompareBytes();
            var seekLength = targetStream.Seek(offset, origin);
            CompareBytes();
            return seekLength;
        }

        public override void SetLength(long value)
        {
            targetStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            CompareBytes();
            targetStream.Write(buffer, offset, count);
            CompareBytes();
        }

        public override bool CanRead
        {
            get
            {
                return targetStream.CanRead;
            }
        }
        public override bool CanSeek
        {
            get
            {
                return targetStream.CanSeek;
            }
        }
        public override bool CanWrite
        {
            get
            {
                return targetStream.CanWrite;
            }
        }

        public override long Length
        {
            get
            {
                return targetStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                return targetStream.Position;
            }
            set
            {
                targetStream.Position = value;
            }
        }

        public void PauseAndRead(long value)
        {
            isCompare = false;

            reader.Position = originLastCorrectIndex;
            reader.ReadInt64();
            originLastCorrectIndex = reader.Position;
        }

        public void PauseAndRead(uint value)
        {
            isCompare = false;

            reader.Position = originLastCorrectIndex;
            reader.ReadUInt32();
            originLastCorrectIndex = reader.Position;
        }

        public void PauseAndReadBytes(int length)
        {
            isCompare = false;

            reader.Position = originLastCorrectIndex;
            reader.ReadBytes(length);
            originLastCorrectIndex = reader.Position;
        }
        public void Continue()
        {
            isCompare = true;
        }


        public void ReInit()
        {
            targetlastCorrectIndex = 0;
            originLastCorrectIndex = 0;
        }

        public void PauseAndAlign(int alignment)
        {
            isCompare = false;

            reader.Position = originLastCorrectIndex;
            var pos = reader.Position;
            var mod = pos % alignment;
            if (mod != 0)
            {
                reader.Position += alignment - mod;
            }

            originLastCorrectIndex = reader.Position;
        }
    }
}