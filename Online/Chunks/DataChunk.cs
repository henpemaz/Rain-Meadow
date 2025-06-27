using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace RainMeadow
{
    public interface IChunkDestination
    {
        enum DestinationType : byte
        {
            Unknown = 0,
            Resource,
            Entity, 
        }

        public void ProcessEntireChunk(IncomingDataChunk chunk);
        public void ProcessSlice(IncomingDataChunk chunk, Slice slice);
        public void ProcessOrderedSlice(IncomingDataChunk chunk, Slice slice, bool newest);
    }

    public class Slice : Serializer.ICustomSerializable
    {
        public byte index;
        public ArraySegment<byte> data;
        public Slice() { }
        public Slice(byte index, ArraySegment<byte> data)
        {
            this.index = index;
            this.data = data;
        }

        public void CustomSerialize(Serializer serializer)
        {
            serializer.Serialize(ref index);
            if (serializer.IsWriting)
            {
                serializer.writer.Write((ushort)data.Count);
                serializer.stream.Write(data.Array, data.Offset, data.Count);
            }
            else if (serializer.IsReading)
            {
                var size = serializer.reader.ReadUInt16();
                data = new ArraySegment<byte>(new byte[size], 0, size);
                serializer.stream.Read(data.Array, 0, data.Count);
            }
        }
    }

    

    public class OutgoingDataChunk
    {
        public readonly OnlinePlayer? toPlayer;
        public readonly byte chunkId;
        public readonly IChunkDestination destination;
        private SliceMessage[] outgoingSlices;

        public byte totalSlices => (byte)outgoingSlices.Length;
        public bool reliable => chunkId != 0;
        

        public OutgoingDataChunk(byte chunkId, IChunkDestination destination, ArraySegment<byte> data, OnlinePlayer? toPlayer, int sliceSize)
        {

            this.destination = destination;
            this.toPlayer = toPlayer;
            this.chunkId = chunkId;
            if (reliable)
            {
                checked
                {
                    // todo gzip data and keep track of gzip status
                    int sliceCount = (data.Count + sliceSize - 1) / sliceSize;
                    if (sliceCount > (int)byte.MaxValue) throw new InvalidProgrammerException("Too much data! Try increasing the slice count");
                    outgoingSlices = new SliceMessage[sliceCount];

                    
                    byte sliceIndex = 0;
                    int readData = 0;
                    for (byte i = 0; i < outgoingSlices.Length; i++)
                    {
                        var len = Math.Min(sliceSize, data.Count - readData);
                        if (len <= 0) break;
                        var slice = new Slice(i, new ArraySegment<byte>(data.Array, readData, len));
                        readData += len;
                        outgoingSlices[i] = new SliceMessage(slice);
                    }

                    RainMeadow.Debug(data.Count);
                    RainMeadow.Debug(readData);
                    RainMeadow.Debug(outgoingSlices.Length);
                    RainMeadow.Debug(sliceIndex);
                }
            }
            else
            {
                outgoingSlices = new SliceMessage[1];
                outgoingSlices[0] = new SliceMessage(new Slice(0, data));
            }
        }

        public OutgoingDataChunk(OutgoingDataChunk template, OnlinePlayer toPlayer, byte chunkId)
        {
            this.chunkId = chunkId;
            if (this.reliable != template.reliable) throw new InvalidProgrammerException("Reliability should match template (Wrong chunk ID?)");
            this.toPlayer = toPlayer;
            outgoingSlices = new SliceMessage[template.outgoingSlices.Length];
            for (byte i = 0; i < outgoingSlices.Length; i++)
            {
                outgoingSlices[i] = new SliceMessage(template.outgoingSlices[i].slice);
            }
        }

        public SliceMessage? GetSlice(int index) => outgoingSlices?[index];
        public SliceMessage? GetNextSlice()
        {
            foreach (SliceMessage slice in outgoingSlices)
            {
                if (!slice.ackd && !slice.sent) return slice;
            }
            return null;
        }

        public void Update() // sender logic for processing ackd slices
        {
            if (toPlayer is null) throw new InvalidProgrammerException("You provided null as toPlayer (was this a template?)");
            if (reliable) ReliableUpdate();
            else UnreliableUpdate();
        }

        public void ReliableUpdate()
        {
            if (toPlayer is null) throw new InvalidProgrammerException("You provided null as toPlayer (was this a template?)");
            bool allAckd = true;
            foreach (SliceMessage slice in outgoingSlices)
            {
                if (!slice.ackd && slice.sent)
                {
                    if (EventMath.IsNewerOrEqual(toPlayer.latestTickAck, slice.tick)) // should have arrived
                    {
                        if (EventMath.IsNewer(toPlayer.oldestTickToConsider, slice.tick)) // huge ack gap
                        {
                            slice.Resend();
                        }
                        else if (slice.tick == toPlayer.latestTickAck || toPlayer.recentlyAckdTicks.Contains(slice.tick))
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
                if (!this.doneSending)
                {
                    OnDone?.Invoke(this);
                    this.doneSending = true;
                }
            }
        }

        public void UnreliableUpdate()
        {
            bool allSent = true;
            foreach (SliceMessage slice in outgoingSlices)
            {
                if (!slice.sent) allSent = false;
            }

            if (allSent)
            {
                if (!this.doneSending)
                {
                    OnDone?.Invoke(this);
                    this.doneSending = true;
                }
            }
        }

        public event Action<OutgoingDataChunk> OnDone;
        public bool doneSending { get; private set; } = false;

        internal void Then(Action<OutgoingDataChunk> action)
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
    }

    
    public class IncomingDataChunk
    {
        public readonly OnlinePlayer fromPlayer;
        public readonly byte chunkId;
        public readonly IChunkDestination destination;
        public int slicesReceived { get; private set; } = 0;
        public byte totalSlices => (byte)incomingSlices.Length;
        private Slice[] incomingSlices;
     
        public IncomingDataChunk(byte chunkId, IChunkDestination destination, byte totalSlices, OnlinePlayer fromPlayer)
        {
            this.destination = destination;
            this.fromPlayer = fromPlayer;
            this.chunkId = chunkId;
            this.incomingSlices = new Slice[totalSlices];
        }

        private byte[] entireDataCache = null;
        public byte[] GetData()
        {
            // return incomingSlices.SelectMany(s => s.data).ToArray();
            if (!DoneReceiving()) throw new InvalidProgrammerException("we're not done yet");
            if (entireDataCache is null)
            {
                checked
                {
                    entireDataCache = new byte[incomingSlices.Aggregate<Slice, int>(0, (int size, Slice slice) => { return size + slice.data.Count; })];
                    int copiedData = 0;
                    foreach (Slice slice in incomingSlices)
                    {
                        var totalcopied = Mathf.Min(slice.data.Count, entireDataCache.Length - copiedData);
                        Buffer.BlockCopy(slice.data.Array, slice.data.Offset, entireDataCache, copiedData, totalcopied);
                        copiedData += totalcopied;
                    }
                    RainMeadow.Debug(copiedData);
                }
            }

            return entireDataCache;
        }

        public Slice? GetSlice(int id) {
            return incomingSlices?[id];
        }

        public void NewSlice(Slice slice)
        {
            if (incomingSlices[slice.index] != null) return;
            RainMeadow.Debug("Recieved " + slice.index + ":" + totalSlices + ":" + chunkId);
            Slice? oldestContinousSlice = null;
            for (int i = 0; i < totalSlices; i++)
            {
                if (GetSlice(i) is Slice continousSlice)
                {
                    oldestContinousSlice = continousSlice;
                }
                else
                {
                    break;
                }
            }

            List<Slice> incomingOrderedSlices = new();
            if (oldestContinousSlice != null && slice.index == (oldestContinousSlice.index + 1))
            {
                incomingOrderedSlices.Add(slice);
                for (int i = slice.index + 1; i < totalSlices; i++)
                {
                    if (GetSlice(i) is Slice continousSlice)
                    {
                        incomingOrderedSlices.Add(continousSlice);
                    }
                    else
                    {
                        break;
                    }
                }
            }
            

            incomingSlices[slice.index] = slice;
            slicesReceived++;


            destination.ProcessSlice(this, slice);
            for (int i = 0; i < incomingOrderedSlices.Count; i++)
            {
                destination.ProcessOrderedSlice(this, incomingOrderedSlices[i], i == (incomingOrderedSlices.Count - 1));
            }
            if (DoneReceiving())
            {
                destination.ProcessEntireChunk(this);
            }
        }
        public bool DoneReceiving() => slicesReceived == totalSlices;
    }
}