
using System.Text;



namespace Sorth.Interpreter.Runtime.DataStructures
{

    public class ByteBuffer
    {
        private byte[] Buffer;

        public int Count { get { return Buffer.Length; } }
        public int Position { get; set; }

        public ByteBuffer(int size)
        {
            Buffer = new byte[size];
            Position = 0;

            for (int i = 0; i < size; ++i)
            {
                Buffer[i] = 0;
            }
        }

        public void WriteInt(int size, long value)
        {
            WriteBytes(size, BitConverter.GetBytes(value));
        }

        public long ReadInt(int size, bool is_signed)
        {
            byte[] bytes = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 };
            long result = 0;

            switch (size)
            {
                case 8:
                    result = is_signed ? BitConverter.ToInt64(ReadBytes(size), 0)
                                       : (long)BitConverter.ToUInt64(ReadBytes(size), 0);
                    break;

                case 4:
                    result = is_signed ? BitConverter.ToInt32(ReadBytes(size), 0)
                                       : BitConverter.ToUInt32(ReadBytes(size), 0);
                    break;

                case 2:
                    result = is_signed ? BitConverter.ToInt16(ReadBytes(size), 0)
                                       : BitConverter.ToUInt16(ReadBytes(size), 0);
                    break;

                case 1:
                    result = ReadBytes(1)[0];
                    break;
            }

            return result;
        }

        public void WriteDouble(int size, double value)
        {
            if (size == 4)
            {
                WriteBytes(size, BitConverter.GetBytes((float)value));
            }
            else
            {
                WriteBytes(size, BitConverter.GetBytes(value));
            }
        }

        public double ReadDouble(int size)
        {
            double result = 0.0;

            switch (size)
            {
                case 8:
                    result = BitConverter.ToDouble(ReadBytes(size), 0);
                    break;

                case 4:
                    result = BitConverter.ToSingle(ReadBytes(size), 0);
                    break;
            }

            return result;
        }

        public void WriteString(int max_size, string value)
        {
            WriteBytes(max_size, Encoding.UTF8.GetBytes(value));
        }

        public string ReadString(int max_size)
        {
            return Encoding.UTF8.GetString(ReadBytes(max_size), 0, max_size);
        }

        private void WriteBytes(int count, byte[] bytes)
        {
            for (int i = 0; i < Math.Min(count, bytes.Length); ++i)
            {
                Buffer[Position + i] = bytes[i];
            }

            Position += count;
        }

        private byte[] ReadBytes(int count)
        {
            byte[] bytes = new byte[count];

            for (int i = 0; i < count; ++i)
            {
                bytes[i] = Buffer[Position + i];
            }

            Position += count;

            return bytes;
        }

        public ByteBuffer Clone()
        {
            return new ByteBuffer(Count);
        }

        public override string ToString()
        {
            string ByteString(int start, int stop)
            {
                if (stop == 0)
                {
                    return "";
                }

                var spaces = ((16 - (stop - start)) * 3);
                var result = new string(' ', spaces);

                for (int i = start; i < stop; ++i)
                {
                    char next = (char)Buffer[i];
                    bool is_ctrl = char.IsControl(next) || ((next & 0x80) != 0);

                    result += is_ctrl ? "." : $"{next}";
                }

                return result;
            }

            string result = "          00 01 02 03 04 05 06 07 08 09 0a 0b 0c 0d 0e 0f";

            for (int i = 0; i < Buffer.Length; ++i)
            {
                if ((i == 0) || ((i % 16) == 0))
                {
                    string byte_string = ByteString(i - 16, i);

                    result += string.Format(" {0}\n{1:X8}  ", byte_string, i);
                }

                result += string.Format("{0:X2}", Buffer[i]) + " ";
            }

            var left_over = Buffer.Length % 16 == 0 ? 16 : Buffer.Length % 16;
            var index = Buffer.Length - left_over;

            result += " " + ByteString(index, Buffer.Length);

            return result;
        }

        public override int GetHashCode()
        {
            return Buffer.GetHashCode();
        }

        public override bool Equals(object? obj)
        {
            if (   (obj != null)
                && (obj is ByteBuffer buffer)
                && (Count == buffer.Count))
            {
                for (int i = 0; i < buffer.Count; ++i)
                {
                    if (Buffer[i] != buffer.Buffer[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            return false;
        }
    }

}
