using System;
using System.IO;
using System.Threading;

namespace Biathlon.Replay
{
    /// <summary>Single producer journal stream. Compression and disk writes belong
    /// to the worker. Exhausting the bounded queue fails the optional recording
    /// instead of blocking the race. Complete drains it after the race ends.</summary>
    public sealed class ReplayWriteQueue : Stream
    {
        private const int BlockSize = 64 * 1024;
        private const int BlockCount = 16;
        private readonly Stream destination;
        private readonly Thread worker;
        private readonly object gate = new object();
        private readonly byte[][] blocks = new byte[BlockCount][];
        private readonly int[] lengths = new int[BlockCount];
        private int head, tail, queued, used;
        private bool closing, cancel, disposed;
        private volatile Exception failure;

        public ReplayWriteQueue(Stream destination)
        {
            if (destination == null || !destination.CanWrite) throw new ArgumentException("Writable replay destination required.", nameof(destination));
            this.destination = destination;
            for (int i = 0; i < BlockCount; i++) blocks[i] = new byte[BlockSize];
            worker = new Thread(Drain) { IsBackground = true, Name = "Biathlon replay writer" };
            worker.Start();
        }

        public override bool CanRead => false;
        public override bool CanSeek => false;
        public override bool CanWrite => !disposed && !closing;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();

        public override void Write(byte[] buffer, int offset, int count) => Write(new ReadOnlySpan<byte>(buffer, offset, count));

        public override void Write(ReadOnlySpan<byte> buffer)
        {
            CheckOpen();
            while (!buffer.IsEmpty)
            {
                int count = Math.Min(BlockSize - used, buffer.Length);
                buffer.Slice(0, count).CopyTo(blocks[tail].AsSpan(used));
                used += count;
                buffer = buffer.Slice(count);
                if (used == BlockSize) Enqueue();
            }
        }

        public override void WriteByte(byte value)
        {
            CheckOpen();
            blocks[tail][used++] = value;
            if (used == BlockSize) Enqueue();
        }

        public override void Flush()
        {
            CheckOpen();
            if (used > 0) Enqueue();
        }

        public void Complete()
        {
            Flush();
            Stop(drain: true);
            if (failure != null) throw new IOException("Replay write failed.", failure);
        }

        private void CheckOpen()
        {
            if (disposed || closing) throw new ObjectDisposedException(nameof(ReplayWriteQueue));
            if (failure != null) throw new IOException("Replay write failed.", failure);
        }

        private void Enqueue()
        {
            lock (gate)
            {
                // Keep the next producer slot private, even while the worker is
                // writing the oldest block outside the lock.
                if (queued == BlockCount - 1)
                {
                    failure = new IOException("Replay writer could not keep up with the race.");
                    throw failure;
                }
                lengths[tail] = used;
                tail = (tail + 1) % BlockCount;
                used = 0;
                queued++;
                Monitor.Pulse(gate);
            }
        }

        private void Drain()
        {
            try
            {
                using (destination)
                {
                    while (true)
                    {
                        lock (gate)
                        {
                            while (queued == 0 && !closing) Monitor.Wait(gate);
                            if (cancel || queued == 0) break;
                        }
                        destination.Write(blocks[head], 0, lengths[head]);
                        lock (gate)
                        {
                            head = (head + 1) % BlockCount;
                            queued--;
                        }
                    }
                }
            }
            catch (Exception error) { failure = error; }
        }

        private void Stop(bool drain)
        {
            lock (gate)
            {
                closing = true;
                cancel = !drain;
                Monitor.Pulse(gate);
            }
            worker.Join();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && !disposed)
            {
                disposed = true;
                if (!closing) Stop(drain: false);
            }
            base.Dispose(disposing);
        }
    }
}
