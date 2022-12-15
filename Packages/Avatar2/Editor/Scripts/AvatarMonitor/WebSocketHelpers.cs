using System;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

using System.Threading;
using System.Threading.Tasks;

using UnityEngine;
using UnityEditor;

// Web Socket RFC 6455: https://datatracker.ietf.org/doc/html/rfc6455
// For reference, web socket packet structure
//
//    0                   1                   2                   3
//    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
//   +-+-+-+-+-------+-+-------------+-------------------------------+
//   |F|R|R|R| opcode|M| Payload len |    Extended payload length    |
//   |I|S|S|S|  (4)  |A|     (7)     |             (16/64)           |
//   |N|V|V|V|       |S|             |   (if payload len==126/127)   |
//   | |1|2|3|       |K|             |                               |
//   +-+-+-+-+-------+-+-------------+ - - - - - - - - - - - - - - - +
//   |     Extended payload length continued, if payload len == 127  |
//   + - - - - - - - - - - - - - - - +-------------------------------+
//   |                               |Masking-key, if MASK set to 1  |
//   +-------------------------------+-------------------------------+
//   | Masking-key (continued)       |          Payload Data         |
//   +-------------------------------- - - - - - - - - - - - - - - - +
//   :                     Payload Data continued ...                :
//   + - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - - +
//   |                     Payload Data continued ...                |
//   +---------------------------------------------------------------+

class WSHelpers
{
    public enum Opcode
    {
        Fragment = 0,               // Continuation code
        Text = 1,                   // Text code
        Binary = 2,                 // Binary code
        Reserved1 = 3,              // Reserved, can be used by extensions
        Reserved2 = 4,
        Reserved3 = 5,
        Reserved4 = 6,
        Reserved5 = 7,
        ClosedConnection = 8,       // Closed connection
        Ping = 9,                   // Ping
        Pong = 10,                  // Pong
        ReservedB = 11,             // Reserved, can be used by extensions
        ReservedC = 12,
        ReservedD = 13,
        ReservedE = 14,
        ReservedF = 15,

        Unknown = 0xff              // Our own marker, outside range of 4 bits in packet
    }

    // Byte 0 masks (see above)
    private const byte kPacketExtensionMask = 0x70;
    private const byte kPacketFinalFragmentMask = 0x80;
    private const byte kPacketOpcodeMask = 0x0f;

    // Byte 1 masks (see above)
    private const byte kPacketLengthMask = 0x7f;
    private const byte kPacketMaskedMask = 0x80;

    private const Int64 kTwoByteLengthMarker = 126;
    private const Int64 kEightByteLengthMarker = 127;

    private const string kRFC6455Guid = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";         // standard WS handshake GUID
    private const string kWSResponseString = "HTTP/1.1 101 Switching Protocols\r\n" +
        "Connection: Upgrade\r\n" +
        "Upgrade: websocket\r\n" +
        "Sec-WebSocket-Accept: ";

    public static bool Handshake(TcpClient client, NetworkStream stream)
    {
        // Wait for data available. Don't check too often.
        while (!stream.DataAvailable)
        {
            Thread.Sleep(500);
        }

        // Wait til we get at least 3 bytes so we can detect "GET"
        Int32 waitCount = 0;
        while (client.Available < 3)
        {
            Thread.Sleep(1);
            waitCount++;
            if (waitCount == 200)           // Something is up here; return a false and retry
            {
                return false;
            }
        }

        // Grab all of the data from the connect request
        byte[] bytes = new byte[client.Available];
        stream.Read(bytes, 0, client.Available);
        string s = Encoding.UTF8.GetString(bytes);

        if (!Regex.IsMatch(s, "^GET", RegexOptions.IgnoreCase))
        {
            return false;       // This is not a WS connect
        }

        UnityEditor.AvatarMonitor.MessageLog.AddMessage("Handshaking from client\n{0}", s);

        // 1. Obtain the value of the "Sec-WebSocket-Key" request header without any leading or trailing whitespace
        // 2. Concatenate it with "258EAFA5-E914-47DA-95CA-C5AB0DC85B11" (a special GUID specified by RFC 6455)
        // 3. Compute SHA-1 and Base64 hash of the new value
        // 4. Write the hash back as the value of "Sec-WebSocket-Accept" response header in an HTTP response

        Match swkMatch = Regex.Match(s, "Sec-WebSocket-Key: (.*)");
        if (swkMatch.Groups.Count < 2)
        {
            return false;
        }
        string swk = swkMatch.Groups[1].Value.Trim();
        string swka = swk + kRFC6455Guid;         // RFC 6455 - standard WS handshake GUID
        byte[] swkaSha1 = SHA1.Create().ComputeHash(Encoding.UTF8.GetBytes(swka));
        string swkaSha1Base64 = Convert.ToBase64String(swkaSha1);

        // HTTP/1.1 defines the sequence CR LF as the end-of-line marker
        byte[] response = Encoding.UTF8.GetBytes(
            kWSResponseString + swkaSha1Base64 + "\r\n\r\n");

        try
        {
            stream.Write(response, 0, response.Length);
            return true;
        }
        catch (Exception e)
        {
            UnityEditor.AvatarMonitor.MessageLog.AddMessage($"Handshake fail:\n{e.Message}");
            return false;
        }
    }

    static public bool ReadPacket(TcpClient client, NetworkStream stream, CancellationToken token, out byte[] payload, out Opcode opcode)
    {
        try
        {
            return ReadPacketInternal(client, stream, token, out payload, out opcode);
        }
        catch (Exception e)
        {
            UnityEditor.AvatarMonitor.MessageLog.AddMessage($"Network read failed: {e.Message}");
            payload = null;
        }
        opcode = Opcode.Unknown;
        return false;
    }

    static private byte[] ReadBytes(NetworkStream stream, Int32 count, CancellationToken token)
    {
        byte[] result = new byte[count];        // max size of frame header
        Task<Int32> readTask = stream.ReadAsync(result, 0, count, token);
        readTask.Wait(token);
        if (readTask.Result < count)
        {
            throw new Exception(String.Format("Read {0} bytes failed / returned too few bytes: {1}", count, readTask.Result));
        }
        return result;
    }

    static private bool ReadPacketInternal(TcpClient client, NetworkStream stream, CancellationToken token, out byte[] payload, out Opcode opcode)
    {
        payload = null;
        opcode = Opcode.Unknown;

        byte[] header = ReadBytes(stream, 2, token);

        Int32 extensionBits = header[0] & kPacketExtensionMask;
        bool finalFragment = (header[0] & kPacketFinalFragmentMask) > 0;
        opcode = (WSHelpers.Opcode)(header[0] & kPacketOpcodeMask);

        Int64 length = (header[1] & kPacketLengthMask);
        bool masked = (header[1] & kPacketMaskedMask) > 0;

        Int64 payloadLength;
        switch (length)
        {
            default:        // < 126 means use the size as is
                payloadLength = length;
                break;

            case kTwoByteLengthMarker:       // Means 2 byte length extension
                byte[] shortLength = ReadBytes(stream, 2, token);
                payloadLength = shortLength[1] | (Int64)(shortLength[0] << 8);
                break;

            case kEightByteLengthMarker:  // 8 byte length extension
                byte[] longLength = ReadBytes(stream, 8, token);
                payloadLength = 0;
                for (int i = 0; i < 8; i++)
                {
                    payloadLength |= (Int64)(longLength[i] << (i * 8));
                }
                break;
        }

        // Read the decode mask if the packet is masked (always, it seems)
        byte[] decodeMask = null;
        if (masked)
        {
            decodeMask = ReadBytes(stream, 4, token);
        }


        payload = new byte[payloadLength];

        Int64 readTotal = 0;
        while (readTotal < payloadLength)
        {
            Int64 remaining = payloadLength - readTotal;
            Int32 readAmount = (Int32)Math.Min(remaining, (Int64)Int32.MaxValue);
            Task<Int32> readTask = stream.ReadAsync(payload, (Int32)readTotal, readAmount, token);
            readTask.Wait(token);
            if (readTask.Result == 0)
            {
                throw new Exception(String.Format("Read {0} payload bytes failed / returned too few bytes {1}", readAmount, readTask.Result));
            }
            readTotal += readTask.Result;
        }

        // If masked, decode by xor'ing
        if (masked)
        {
            for (Int32 byteIndex = 0; byteIndex < payloadLength; byteIndex++)
            {
                payload[byteIndex] = (byte)(payload[byteIndex] ^ decodeMask[(byteIndex % 4)]);
            }
        }
        return true;
    }



    private static bool SendDataPacketInternal(NetworkStream stream, byte[] payload, bool mask)
    {
        if (stream == null || !stream.CanWrite)
        {
            return false;
        }

        byte one = (byte)Opcode.Binary;
        one |= kPacketFinalFragmentMask;        // Mark as not fragmented
        byte two = (byte)(mask ? kPacketMaskedMask : 0);    // Mark as masked or not

        stream.WriteByte(one);

        if (payload.Length < kTwoByteLengthMarker)      // payload size fits in small size
        {
            two |= (byte)payload.Length;
            stream.WriteByte(two);
        }
        else if (payload.Length < 65535)    // payload under 64
        {
            two |= (byte)kTwoByteLengthMarker;
            stream.WriteByte(two);
            stream.WriteByte((byte)(payload.Length >> 8));
            stream.WriteByte((byte)(payload.Length & 0xff));
        }
        else    // Payload is bigger than 64KB
        {
            two |= (byte)kEightByteLengthMarker;
            stream.WriteByte(two);

            Int64 longLength = payload.Length;
            for (Int32 index = 8; index > 0; index++)
            {
                stream.WriteByte((byte)((longLength >> (8 * index)) & 0xff));
            }
        }

        if (mask)
        {
            byte[] encodeMask = new byte[4];
            var rnd = new System.Random();
            rnd.NextBytes(encodeMask);

            for (Int32 index = 0; index < payload.Length; index++)
            {
                payload[index] = (byte)(payload[index] ^ encodeMask[(index % 4)]);
            }
        }

        stream.Write(payload, 0, payload.Length);

        return true;
    }


    public static bool SendDataPacket(NetworkStream stream, byte[] payload, bool mask = false)
    {
        try
        {
            return SendDataPacketInternal(stream, payload, mask);
        }
        catch (Exception e)
        {
            UnityEditor.AvatarMonitor.MessageLog.AddMessage($"Send data failed:\n{e.Message}");
            return false;
        }
    }
}
