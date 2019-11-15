using System;
using System.IO;
using System.Text;

namespace mono_test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing Mono");
            string boundary = "9051914041544843365972754266";
            string body_input_with_cr = "--9051914041544843365972754266\r\nContent-Disposition: form-data; name=\"text\"\r\n\r\ntext default\r\n--9051914041544843365972754266\r\nContent-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\r\nContent-Type: text/plain\r\n\r\nContent of a.txt.\r\n\r\n--9051914041544843365972754266\r\nContent-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\r\nContent-Type: text/html\r\n\r\n<!DOCTYPE html><title>Content of a.html.</title>\r\n\r\n--9051914041544843365972754266--";
            string body_input_no_cr = "--9051914041544843365972754266\nContent-Disposition: form-data; name=\"text\"\n\ntext default\n--9051914041544843365972754266\nContent-Disposition: form-data; name=\"file1\"; filename=\"a.txt\"\nContent-Type: text/plain\n\nContent of a.txt.\n\n--9051914041544843365972754266\nContent-Disposition: form-data; name=\"file2\"; filename=\"a.html\"\nContent-Type: text/html\n\n<!DOCTYPE html><title>Content of a.html.</title>\n\n--9051914041544843365972754266--"; 
            
            string input = body_input_no_cr;
            
            byte[] bytes = Encoding.ASCII.GetBytes(input);
            var data = new MemoryStream();
            data.Write(bytes, 0, bytes.Length);
            data.Position = 0;
            var multipart = new HttpMultipart(data, boundary, Encoding.UTF8);
            HttpMultipart.Element ele;
            Console.WriteLine("\nBroken MONO Implementation\n");
            while ((ele = multipart.ReadNextElement ()) != null) {
                var val = System.Text.Encoding.UTF8.GetString(bytes, (int)ele.Start, (int)ele.Length);
                Console.WriteLine("Part Length: " + ele.Length + " Value:" + val);
            }

            data.Position = 0;
            var fix_multipart = new Fix_HttpMultipart(data, boundary, Encoding.UTF8);
            Fix_HttpMultipart.Element fixele;
            Console.WriteLine("\nFixed MONO Implementation\n");
            while ((fixele = fix_multipart.ReadNextElement ()) != null) {
                var val = System.Text.Encoding.UTF8.GetString(bytes, (int)fixele.Start, (int)fixele.Length);
                Console.WriteLine("Part Length: " + fixele.Length + " Value:" + val);
            }

            
            data.Position = 0;
            
            string prepended_boundary = "--9051914041544843365972754266";
            var net_multipart = new HttpMultipartContentTemplateParser(bytes, input.Length, Encoding.ASCII.GetBytes(prepended_boundary), Encoding.UTF8);
            Console.WriteLine("\nNET Implementation\n");
            net_multipart.ParseIntoElementList();
            foreach(MultipartContentElement element in net_multipart._elements){
                var val = System.Text.Encoding.UTF8.GetString(bytes, (int)element._offset, (int)element._length);
                Console.WriteLine("Part Length: " +  element._length  + " Value:" + val);
            }
        }

    }
}