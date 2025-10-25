using Scte35.Net.Constants;
using Scte35.Net.Core;
using Scte35.Net.Model.Enums;
using Scte35.Net.Model.SpliceCommand;
using Scte35.Net.Model.SpliceDescriptor;

namespace Scte35.Net.Model
{
    public sealed class SpliceInfoSection : IBinarySerializable
    {
        public SapType SapType { get; set; } = SapType.NotSpecified;
        public ushort SectionLength => (ushort)ComputeSectionLength();
        public int PayloadBytes => 3 + SectionLength;
        public byte ProtocolVersion { get; set; }
        public bool EncryptedPacket { get; set; }
        public EncryptionAlgorithm EncryptionAlgorithm { get; set; } = EncryptionAlgorithm.None;
        public ulong PtsAdjustment90K { get; set; }
        public byte CwIndex { get; set; }
        public ushort Tier { get; set; } = 0x0FFF;
        public ISpliceCommand? SpliceCommand { get; set; }
        public SpliceCommandType SpliceCommandType => SpliceCommand?.Type ?? SpliceCommandType.SpliceNull;
        public IList<ISpliceDescriptor> SpliceDescriptors { get; } = new List<ISpliceDescriptor>();
        public byte[] AlignmentStuffing { get; set; } = [];
        public uint? ECRC32 { get; set; }
        public uint CRC32 { get; private set; }
        public uint ComputedCRC32 { get; private set; }
        public bool Crc32Valid => CRC32 == ComputedCRC32;

        public Func<uint, bool>? PrivateCommandIdIsRecognized { get; set; }
        public bool SkipDueToUnknownPrivateIdentifier { get; private set; }

        public void Encode(Span<byte> dest)
        {
            var sectionLen = ComputeSectionLength();
            var total = 3 + sectionLen; // includes CRC_32
            PayloadValidator.RequireMinLength(dest, total);

            var w = new BitWriter(dest);

            w.WriteByte(Scte35Constants.TableId);
            w.WriteBit(Scte35Constants.SectionSyntaxIndicator);
            w.WriteBit(Scte35Constants.PrivateIndicator);
            w.WriteBits((byte)SapType, 2);
            w.WriteBits((uint)sectionLen, 12);

            if (ProtocolVersion != 0)
                throw new InvalidOperationException("protocol_version must be 0 per SCTE-35.");

            w.WriteByte(ProtocolVersion);
            w.WriteBit(EncryptedPacket);
            w.WriteBits((byte)EncryptionAlgorithm, 6);
            w.WritePts33(PtsAdjustment90K & Scte35Constants.PtsMax);
            w.WriteByte(CwIndex);
            w.WriteBits((uint)(Tier & 0x0FFF), 12);

            int cmdLen = SpliceCommandPayloadLength();
            w.WriteBits((uint)cmdLen, 12);
            byte typeByte = SpliceCommand is UnknownSpliceCommand uc ? uc.CommandType : (byte)SpliceCommandType;
            w.WriteByte(typeByte);

            if (cmdLen > 0)
            {
                var tmp = new byte[cmdLen];
                SpliceCommand!.Encode(tmp);
                w.WriteBytesAligned(tmp);
            }

            int descLen = DescriptorsLoopLength();
            w.WriteUInt16((ushort)descLen);
            if (descLen > 0)
            {
                var tmp = new byte[descLen];
                int off = 0;
                foreach (var d in SpliceDescriptors)
                {
                    int payloadLen = d.PayloadBytes;
                    PayloadValidator.RequireRange(payloadLen, 0, 255);
                    byte tagByteToWrite = d is PrivateDescriptor pd ? pd.PrivateTag : (byte)d.Tag;
                    tmp[off++] = tagByteToWrite; // splice_descriptor_tag
                    tmp[off++] = (byte)payloadLen;

                    if (payloadLen <= 0) continue;
                    
                    d.Encode(tmp.AsSpan(off, payloadLen));
                    off += payloadLen;
                }

                w.WriteBytesAligned(tmp);
            }

            // write stuffing always if provided (encrypted or not)
            if (AlignmentStuffing.Length > 0)
                w.WriteBytesAligned(AlignmentStuffing);

            if (!EncryptedPacket && ECRC32.HasValue)
                throw new InvalidOperationException("E_CRC_32 is only valid when EncryptedPacket == true.");
            
            // E_CRC_32 only when encrypted
            if (EncryptedPacket)
                w.WriteUInt32(ECRC32 ?? 0);

            // compute CRC over entire section up to, but excluding, CRC_32.
            int bytesBeforeCrc = total - 4;
            Span<byte> dataForCrc = dest.Slice(0, bytesBeforeCrc);
            uint crc = Crc32Mpeg2.Compute(dataForCrc);
            w.WriteUInt32(crc);

            // crc
            ComputedCRC32 = Crc32Mpeg2.Compute(dest.Slice(0, total - 4));
            CRC32 = crc;

            if (w.BitsWritten != total * 8)
                throw new InvalidOperationException("splice_info_section size mismatch after encode.");
        }

        public void Decode(ReadOnlySpan<byte> data)
        {
            PayloadValidator.RequireMinLength(data, 3 + 4); // header + crc at minimum

            var r = new BitReader(data);

            byte tid = r.ReadByte();
            if (tid != Scte35Constants.TableId)
                throw new InvalidOperationException(
                    $"table_id must be 0x{Scte35Constants.TableId:X2}, got 0x{tid:X2}.");

            bool ssi = r.ReadBit();
            bool priv = r.ReadBit();

            if (ssi || priv)
                throw new InvalidOperationException(
                    "section_syntax_indicator/private_indicator must be 0 for SCTE-35.");

            SapType = (SapType)r.ReadBits(2);
            int sectionLen = (int)r.ReadBits(12);
            int total = 3 + sectionLen;

            PayloadValidator.RequireMinLength(data, total);

            ProtocolVersion = r.ReadByte();
            EncryptedPacket = r.ReadBit();
            EncryptionAlgorithm = (EncryptionAlgorithm)r.ReadBits(6);
            PtsAdjustment90K = r.ReadPts33();
            CwIndex = r.ReadByte();
            Tier = (ushort)r.ReadBits(12);

            int cmdLen = (int)r.ReadBits(12);
            byte cmdTypeByte = r.ReadByte();

            SpliceCommand = InstantiateCommand(cmdTypeByte);
            if (cmdLen > 0)
            {
                ReadOnlySpan<byte> cmdBytes = r.ReadBytesAligned(cmdLen);
                SpliceCommand!.Decode(cmdBytes);
            }

            int descLen = r.ReadUInt16();
            PayloadValidator.RequireRange(descLen, 0, r.BitsRemaining / 8);

            SpliceDescriptors.Clear();

            if (descLen > 0)
            {
                var descBytes = r.ReadBytesAligned(descLen);
                var dr = new BitReader(descBytes);
                while (dr.BitsRemaining >= 16)
                {
                    var tagByte = dr.ReadByte();
                    var len = dr.ReadByte();

                    PayloadValidator.RequireRange(len, 0, dr.BitsRemaining / 8);

                    var payload = dr.ReadBytesAligned(len);
                    var desc = InstantiateDescriptor(tagByte);

                    desc.Decode(payload);
                    SpliceDescriptors.Add(desc);
                }

                if (dr.BitsRemaining != 0)
                    throw new InvalidOperationException("Trailing bits within descriptor loop.");
            }

            int bytesReadSoFar = total - r.BitsRemaining / 8;
            int remainingBeforeCRC = total - 4 - bytesReadSoFar; // bytes between here and CRC_32

            int requiredTail = EncryptedPacket ? 4 : 0; // need 4 bytes for E_CRC_32 when encrypted
            if (remainingBeforeCRC < requiredTail)
                throw new InvalidOperationException(
                    EncryptedPacket ? "Insufficient bytes for E_CRC_32." : "Length accounting error before CRC_32.");

            int stuffingLen = remainingBeforeCRC - requiredTail;
            AlignmentStuffing = stuffingLen > 0 ? r.ReadBytesAligned(stuffingLen).ToArray() : [];

            if (EncryptedPacket)
                ECRC32 = r.ReadUInt32();
            else
                ECRC32 = null;

            CRC32 = r.ReadUInt32();
            ComputedCRC32 = Crc32Mpeg2.Compute(data[..(total - 4)]);

            SkipDueToUnknownPrivateIdentifier = false;
            if (SpliceCommand is PrivateCommand pc && PrivateCommandIdIsRecognized is not null)
            {
                if (!PrivateCommandIdIsRecognized(pc.Identifier))
                    SkipDueToUnknownPrivateIdentifier = true;
            }

            if (r.BitsRemaining != 0)
                throw new InvalidOperationException("Trailing bits after splice_info_section.");
        }

        private int SpliceCommandPayloadLength()
        {
            if (SpliceCommand is null) return 0;
            return SpliceCommand!.PayloadBytes;
        }

        private int DescriptorsLoopLength()
        {
            int len = 0;
            foreach (var d in SpliceDescriptors)
            {
                var p = d.PayloadBytes;
                PayloadValidator.RequireRange(p, 0, 255);
                len += 2 + p; // tag + length + payload
            }

            return len;
        }

        private int ComputeSectionLength()
        {
            int bytes =
                1 + // protocol_version (8)
                5 + // encrypted_packet(1) + encryption_algorithm(6) + pts_adjustment(33)
                1 + // cw_index (8)
                4; // tier(12) + splice_command_length(12) + splice_command_type(8) = 32 bits

            bytes += SpliceCommandPayloadLength();

            bytes += 2; // descriptor_loop_length
            bytes += DescriptorsLoopLength();

            bytes += AlignmentStuffing.Length; // stuffing may be present even if not encrypted
            if (EncryptedPacket)
                bytes += 4; // E_CRC_32

            bytes += 4; // CRC_32
            return bytes;
        }

        private static ISpliceCommand InstantiateCommand(byte type) =>
            type switch
            {
                (byte)SpliceCommandType.SpliceNull => new SpliceNullCommand(),
                (byte)SpliceCommandType.SpliceSchedule => new SpliceScheduleCommand(),
                (byte)SpliceCommandType.SpliceInsert => new SpliceInsertCommand(),
                (byte)SpliceCommandType.TimeSignal => new TimeSignalCommand(),
                (byte)SpliceCommandType.BandwidthReservation => new BandwidthReservationCommand(),
                (byte)SpliceCommandType.PrivateCommand => new PrivateCommand(),
                _ => new UnknownSpliceCommand(type)
            };

        private static ISpliceDescriptor InstantiateDescriptor(byte tag) =>
            tag switch
            {
                (byte)SpliceDescriptorTag.Avail => new AvailDescriptor(),
                (byte)SpliceDescriptorTag.Dtmf => new DtmfDescriptor(),
                (byte)SpliceDescriptorTag.Segmentation => new SegmentationDescriptor(),
                (byte)SpliceDescriptorTag.Time => new TimeDescriptor(),
                (byte)SpliceDescriptorTag.Audio => new AudioDescriptor(),
                _ => new PrivateDescriptor(tag) // unknown/reserved → opaque
            };
    }
}