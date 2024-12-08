using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public class DataChunk
    {
        public OnlinePlayer player;
        public byte chunkId;
        public ChunkType chunkType;
        public byte totalSlices;
        const int sliceSize = 4096;

        private int slicesReceived;
        public List<Slice> incomingSlices;
        public List<SliceMessage> outgoingSlices;

        public enum ChunkType : byte
        {
            None = 0, // zeroes are an easy to catch mistake
            LobbyData,
            Other,
        }

        /// <summary>
        /// for sending
        /// </summary>
        public DataChunk(byte chunkId, ChunkType chunkType, byte[] data, OnlinePlayer toPlayer)
        {
            this.chunkType = chunkType;
            this.player = toPlayer;
            this.chunkId = chunkId;

            // todo gzip data and keep track of gzip status
            var reader = new BinaryReader(new MemoryStream(data));
            outgoingSlices = new List<SliceMessage>((int)Mathf.Ceil(data.Length / (float)sliceSize));

            byte sliceIndex = 0;
            while (true)
            {
                byte[] buffer = new byte[sliceSize];
                var read = reader.Read(buffer, 0, sliceSize);
                if( read != sliceSize)
                {
                    Array.Resize(ref buffer, read);
                }
                var slice = new Slice(sliceIndex, buffer);
                outgoingSlices.Add(new SliceMessage(slice));
                sliceIndex++;
                if (reader.BaseStream.Position == data.Length) break;
            }
            this.totalSlices = sliceIndex;
        }

        /// <summary>
        /// for receiving
        /// </summary>
        public DataChunk(byte chunkId, ChunkType chunkType, byte sliceCount, OnlinePlayer fromPlayer)
        {
            this.player = fromPlayer;
            this.chunkId = chunkId;
            this.chunkType = chunkType;
            this.incomingSlices = new List<Slice>(new Slice[sliceCount]);
            this.totalSlices = sliceCount;
            this.slicesReceived = 0;
        }

        public SliceMessage? GetNextSlice()
        {
            foreach (SliceMessage slice in outgoingSlices)
            {
                if (!slice.ackd && !slice.sent) return slice;
            }
            return null;
        }

        public void NewSlice(Slice slice)
        {
            if (incomingSlices[slice.index] == null)
            {
                RainMeadow.Debug(slice.index + " out of " + totalSlices);
                incomingSlices[slice.index] = slice;
                slicesReceived++;
            }
        }
        public bool DoneReceiving() => slicesReceived == totalSlices;

        public void Update() // sender logic for processing ackd slices
        {
            bool allAckd = true;
            foreach (SliceMessage slice in outgoingSlices)
            {
                if (!slice.ackd && slice.sent)
                {
                    if (NetIO.IsNewerOrEqual(player.latestTickAck, slice.tick)) // should have arrived
                    {
                        if (NetIO.IsNewer(player.oldestTickToConsider, slice.tick)) // huge ack gap
                        {
                            slice.Resend();
                        }
                        else if (slice.tick == player.latestTickAck || player.recentlyAckdTicks.Contains(slice.tick))
                        {
                            slice.Acked();
                        }
                        else
                        {
                            slice.Resend();
                        }
                    }
                }
                if (!slice.ackd) allAckd = false;
            }
            if (allAckd)
            {
                this.doneSending = true;
                // then what?
            }
        }

        public event Action OnDone;
        public bool doneSending;
        public bool DoneSending()
        {
            if (doneSending)
            {
                OnDone?.Invoke();
                return true;
            }
            return false;
        }

        public byte[] GetData()
        {
            return incomingSlices.SelectMany(s => s.data).ToArray();
        }

        internal void Then(Action action)
        {
            OnDone += action;
        }

        public class SliceMessage
        {
            public bool ackd;
            public bool sent;
            public uint tick;
            public Slice slice;

            public SliceMessage(Slice slice)
            {
                this.slice = slice;
            }

            public void Sent()
            {
                RainMeadow.Debug(slice.index);
                sent = true;
                tick = OnlineManager.mePlayer.tick;
            }

            public void Unsent()
            {
                //sent = false; // irrelevant since GetNextSlice doesn't mutate slice
            }

            public void Acked()
            {
                RainMeadow.Debug(slice.index);
                ackd = true;
            }

            public void Resend()
            {
                RainMeadow.Debug(slice.index);
                sent = false;
                tick = 0;
            }
        }

        public class Slice : Serializer.ICustomSerializable
        {
            public byte index;
            public byte[] data;

            public Slice() { }
            public Slice(byte index, byte[] data)
            {
                this.index = index;
                this.data = data;
            }

            public void CustomSerialize(Serializer serializer)
            {
                serializer.Serialize(ref index);
                serializer.SerializeLongArray(ref data);
            }
        }
    }
}