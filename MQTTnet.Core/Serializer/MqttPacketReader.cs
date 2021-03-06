﻿using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Core.Adapter;
using MQTTnet.Core.Exceptions;
using MQTTnet.Core.Protocol;
using MQTTnet.Core.Packets;

namespace MQTTnet.Core.Serializer
{
    public sealed class MqttPacketReader : BinaryReader
    {
        private readonly ReceivedMqttPacket _receivedMqttPacket;
        
        public MqttPacketReader(ReceivedMqttPacket receivedMqttPacket)
            : base(receivedMqttPacket.Body, Encoding.UTF8, true)
        {
            _receivedMqttPacket = receivedMqttPacket;
        }

        public bool EndOfRemainingData => BaseStream.Position == _receivedMqttPacket.Header.BodyLength;

        public static MqttPacketHeader ReadHeaderFromSource(Stream stream, CancellationToken cancellationToken)
        {
            var fixedHeader = (byte)stream.ReadByte();
            var controlPacketType = (MqttControlPacketType)(fixedHeader >> 4);
            var bodyLength = ReadBodyLengthFromSource(stream, cancellationToken);

            return new MqttPacketHeader
            {
                FixedHeader = fixedHeader,
                ControlPacketType = controlPacketType,
                BodyLength = bodyLength
            };
        }

        public override ushort ReadUInt16()
        {
            var buffer = ReadBytes(2);

            var temp = buffer[0];
            buffer[0] = buffer[1];
            buffer[1] = temp;

            return BitConverter.ToUInt16(buffer, 0);
        }

        public string ReadStringWithLengthPrefix()
        {
            var buffer = ReadWithLengthPrefix();
            return Encoding.UTF8.GetString(buffer, 0, buffer.Length);
        }

        public byte[] ReadWithLengthPrefix()
        {
            var length = ReadUInt16();
            return ReadBytes(length);
        }

        public byte[] ReadRemainingData()
        {
            return ReadBytes(_receivedMqttPacket.Header.BodyLength - (int)BaseStream.Position);
        }

        private static int ReadBodyLengthFromSource(Stream stream, CancellationToken cancellationToken)
        {
            // Alorithm taken from http://docs.oasis-open.org/mqtt/mqtt/v3.1.1/os/mqtt-v3.1.1-os.html.
            var multiplier = 1;
            var value = 0;
            byte encodedByte;
            do
            {
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new TaskCanceledException();
                }

                encodedByte = (byte)stream.ReadByte();
                value += (encodedByte & 127) * multiplier;
                multiplier *= 128;
                if (multiplier > 128 * 128 * 128)
                {
                    throw new MqttProtocolViolationException("Remaining length is invalid.");
                }
            } while ((encodedByte & 128) != 0);
            return value;
        }
    }
}
