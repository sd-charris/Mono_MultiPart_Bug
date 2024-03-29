using System;
using System.Globalization;
using System.IO;
using System.Text;
public class Fix_HttpMultipart
{

    public class Element
    {
        public string ContentType;
        public string Name;
        public string Filename;
        public long Start;
        public long Length;

        public override string ToString()
        {
            return "ContentType " + ContentType + ", Name " + Name + ", Filename " + Filename + ", Start " +
                Start.ToString() + ", Length " + Length.ToString();
        }
    }

    Stream data;
    string boundary;
    byte[] boundary_bytes;
    byte[] buffer;
    bool at_eof;
    Encoding encoding;
    StringBuilder sb;

    const byte HYPHEN = (byte)'-', LF = (byte)'\n', CR = (byte)'\r';

    // See RFC 2046 
    // In the case of multipart entities, in which one or more different
    // sets of data are combined in a single body, a "multipart" media type
    // field must appear in the entity's header.  The body must then contain
    // one or more body parts, each preceded by a boundary delimiter line,
    // and the last one followed by a closing boundary delimiter line.
    // After its boundary delimiter line, each body part then consists of a
    // header area, a blank line, and a body area.  Thus a body part is
    // similar to an RFC 822 message in syntax, but different in meaning.

    public Fix_HttpMultipart(Stream data, string b, Encoding encoding)
    {
        this.data = data;
        boundary = b;
        boundary_bytes = encoding.GetBytes(b);
        buffer = new byte[boundary_bytes.Length + 2]; // CRLF or '--'
        this.encoding = encoding;
        sb = new StringBuilder();
    }

    string ReadLine()
    {
        // CRLF or LF are ok as line endings.
        bool got_cr = false;
        int b = 0;
        sb.Length = 0;
        while (true)
        {
            b = data.ReadByte();
            if (b == -1)
            {
                return null;
            }

            if (b == LF)
            {
                break;
            }
            got_cr = (b == CR);
            sb.Append((char)b);
        }

        if (got_cr)
            sb.Length--;

        return sb.ToString();

    }

    static string GetContentDispositionAttribute(string l, string name)
    {
        int idx = l.IndexOf(name + "=");
        if (idx < 0)
            return null;
        int begin = idx + name.Length + "=".Length;
        int end;
        if (l.Length > begin && l[begin] == '"')
        {
            begin++;
            end = l.IndexOf('"', begin);
        }
        else
        {
            end = l.IndexOf(';', begin);
            if (end == -1)
                end = l.Length;
        }
        if (end < 0)
            return null;
        if (begin == end)
            return "";
        return l.Substring(begin, end - begin);
    }

    string GetContentDispositionAttributeWithEncoding(string l, string name)
    {
        int idx = l.IndexOf(name + "=");
        if (idx < 0)
            return null;
        int begin = idx + name.Length + "=".Length;
        int end;
        if (l.Length > begin && l[begin] == '"')
        {
            begin++;
            end = l.IndexOf('"', begin);
        }
        else
        {
            end = l.IndexOf(';', begin);
            if (end == -1)
                end = l.Length;
        }
        if (end < 0)
            return null;
        if (begin == end)
            return "";

        string temp = l.Substring(begin, end - begin);
        byte[] source = new byte[temp.Length];
        for (int i = temp.Length - 1; i >= 0; i--)
            source[i] = (byte)temp[i];

        return encoding.GetString(source);
    }

    bool ReadBoundary()
    {
        try
        {
            string line = ReadLine();
            while (line == "")
                line = ReadLine();
            if (line[0] != '-' || line[1] != '-')
                return false;

            if (!StrUtils.EndsWith(line, boundary, false))
                return true;
        }
        catch
        {
        }

        return false;
    }

    string ReadHeaders()
    {
        string s = ReadLine();
        if (s == "")
            return null;

        return s;
    }

    bool CompareBytes(byte[] orig, byte[] other)
    {
        for (int i = orig.Length - 1; i >= 0; i--)
            if (orig[i] != other[i])
                return false;

        return true;
    }

    long MoveToNextBoundary()
    {
        long retval = 0;
        bool got_cr = false;

        int state = 0;
        int c = data.ReadByte();
        while (true)
        {
            if (c == -1)
                return -1;

            if (state == 0 && c == LF)
            {
                retval = data.Position - 1;
                if (got_cr)
                    retval--;
                state = 1;
                c = data.ReadByte();
            }
            else if (state == 0)
            {
                got_cr = (c == CR);
                c = data.ReadByte();
            }
            else if (state == 1 && c == '-')
            {
                c = data.ReadByte();
                if (c == -1)
                    return -1;

                if (c != '-')
                {
                    state = 0;
                    got_cr = false;
                    continue; // no ReadByte() here
                }

                int nread = data.Read(buffer, 0, buffer.Length);
                int bl = buffer.Length;
                if (nread != bl)
                    return -1;

                if (!CompareBytes(boundary_bytes, buffer))
                {
                    state = 0;
                    data.Position = retval + 2;
                    if (got_cr)
                    {
                        data.Position++;
                        got_cr = false;
                    }
                    c = data.ReadByte();
                    continue;
                }
                
                if (buffer[bl - 2] == '-' && buffer[bl - 1] == '-')
                {
                    at_eof = true;
                }
                else if (!(buffer[bl - 2] == CR && buffer[bl - 1] == LF) && buffer[bl - 2] != LF) //CRLF or LF
                {
                    state = 0;
                    data.Position = retval + 2;
                    if (got_cr)
                    {
                        data.Position++;
                        got_cr = false;
                    }
                    c = data.ReadByte();
                    continue;
                }

                data.Position = retval + 2;
                if (got_cr)
                    data.Position++;
                else {                  // LF not a CRLF
                    data.Position--;
                }
                break;
            }
            else
            {
                // state == 1
                state = 0; // no ReadByte() here
            }
        }

        return retval;
    }

    public Element ReadNextElement()
    {
        if (at_eof || ReadBoundary())
            return null;

        Element elem = new Element();
        string header;
        while ((header = ReadHeaders()) != null)
        {
            if (StrUtils.StartsWith(header, "Content-Disposition:", true))
            {
                elem.Name = GetContentDispositionAttribute(header, "name");
                elem.Filename = StripPath(GetContentDispositionAttributeWithEncoding(header, "filename"));
            }
            else if (StrUtils.StartsWith(header, "Content-Type:", true))
            {
                elem.ContentType = header.Substring("Content-Type:".Length).Trim();
            }
        }

        long start = data.Position;
        elem.Start = start;
        long pos = MoveToNextBoundary();
        if (pos == -1)
            return null;

        elem.Length = pos - start;
        return elem;
    }

    static string StripPath(string path)
    {
        if (path == null || path.Length == 0)
            return path;

        if (path.IndexOf(":\\") != 1 && !path.StartsWith("\\\\"))
            return path;
        return path.Substring(path.LastIndexOf('\\') + 1);
    }
}