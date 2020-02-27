using Lime.Messaging.Contents;
using System;
using System.IO;
using System.Reflection;

namespace Take.Builder.Tester
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            var json = File.ReadAllText($"{path}\\application.json");
            var tester = new FlowTester.FlowTester(json);
            var test = tester.TestInputAsync(new Lime.Protocol.Message() { Content = new PlainText() { Text = "oi" }, To = "teste", From = "teste" }).Result;
            Console.WriteLine(test);
            test = tester.TestInputAsync(new Lime.Protocol.Message() { Content = new PlainText() { Text = "Sim 😍" }, To = "teste", From = "teste" }).Result;
            Console.WriteLine(test);

            Console.ReadLine();
        }
    }
}
